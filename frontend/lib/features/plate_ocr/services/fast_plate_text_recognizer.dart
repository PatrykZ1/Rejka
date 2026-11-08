import 'dart:async';
import 'dart:typed_data';

import 'package:flutter_onnxruntime/flutter_onnxruntime.dart';
import 'package:image/image.dart' as img;

import '../models/plate_reading.dart';
import 'plate_text_recognizer.dart';

class FastPlateTextRecognizer {
  static const modelAsset = 'assets/models/cct_s_v2_global.onnx';
  static const _inputName = 'input';
  static const _plateOutputName = 'plate';
  static const _imageWidth = 128;
  static const _imageHeight = 64;
  static const _channels = 3;
  static const _maxPlateSlots = 10;
  static const _alphabet = '0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ_';

  bool _closed = false;
  int _activeReads = 0;
  Completer<void>? _readsDrained;
  OrtSession? _session;
  Future<OrtSession>? _sessionFuture;

  Future<PlateReading> readPlateBytes(
    Uint8List jpeg,
    double detectionConfidence,
  ) async {
    if (_closed) return const PlateReading.empty();
    if (_activeReads++ == 0) _readsDrained = Completer<void>();

    try {
      final decoded = img.decodeImage(jpeg);
      if (decoded == null) {
        throw const FormatException(
          'Could not decode the detected plate image.',
        );
      }

      final resized = img.copyResize(
        decoded,
        width: _imageWidth,
        height: _imageHeight,
        interpolation: img.Interpolation.linear,
      );
      final pixels = Uint8List(_imageWidth * _imageHeight * _channels);
      var offset = 0;
      for (var y = 0; y < _imageHeight; y++) {
        for (var x = 0; x < _imageWidth; x++) {
          final pixel = resized.getPixel(x, y);
          pixels[offset++] = pixel.r.toInt();
          pixels[offset++] = pixel.g.toInt();
          pixels[offset++] = pixel.b.toInt();
        }
      }

      final session = await _getSession();
      final input = await OrtValue.fromList(pixels, [
        1,
        _imageHeight,
        _imageWidth,
        _channels,
      ]);
      Map<String, OrtValue> outputs = {};
      try {
        outputs = await session.run({_inputName: input});
        final plateOutput = outputs[_plateOutputName];
        if (plateOutput == null) {
          throw StateError(
            'ONNX model does not expose the "$_plateOutputName" output.',
          );
        }
        final expectedValues = _maxPlateSlots * _alphabet.length;
        if (plateOutput.shape.fold<int>(
              1,
              (size, dimension) => size * dimension,
            ) !=
            expectedValues) {
          throw StateError(
            'Unexpected OCR output shape: ${plateOutput.shape}. '
            'Expected $_maxPlateSlots x ${_alphabet.length} values.',
          );
        }
        final rawText = decodePlateOutput(await plateOutput.asFlattenedList());
        final cleaned = PlateTextRecognizer.normalizePlateText(rawText);
        if (cleaned.isEmpty) return const PlateReading.empty();

        final combined = PlateTextRecognizer.combineSegments([
          PlateSegment(text: cleaned, cx: 0),
        ]);
        return PlateReading(
          text: combined.text,
          region: combined.region,
          serial: combined.serial,
          rawText: rawText,
          confidence: detectionConfidence,
          capturedAt: DateTime.now(),
        );
      } finally {
        await input.dispose();
        for (final output in outputs.values) {
          await output.dispose();
        }
      }
    } finally {
      _activeReads--;
      if (_activeReads == 0) _readsDrained?.complete();
    }
  }

  static String decodePlateOutput(List<dynamic> values) {
    final expectedValues = _maxPlateSlots * _alphabet.length;
    if (values.length != expectedValues) {
      throw FormatException(
        'Expected $expectedValues OCR values, received ${values.length}.',
      );
    }

    final characters = StringBuffer();
    for (var slot = 0; slot < _maxPlateSlots; slot++) {
      final start = slot * _alphabet.length;
      var bestIndex = 0;
      var bestValue = double.negativeInfinity;
      for (var character = 0; character < _alphabet.length; character++) {
        final value = values[start + character];
        if (value is! num || !value.isFinite) {
          throw const FormatException(
            'OCR model output contains an invalid value.',
          );
        }
        if (value > bestValue) {
          bestValue = value.toDouble();
          bestIndex = character;
        }
      }
      characters.write(_alphabet[bestIndex]);
    }
    return characters.toString().replaceFirst(RegExp(r'_+$'), '');
  }

  Future<OrtSession> _getSession() async {
    if (_closed) throw StateError('OCR recognizer has been disposed.');
    final session = _session;
    if (session != null) return session;

    final loading = _sessionFuture ??= OnnxRuntime().createSessionFromAsset(
      modelAsset,
    );
    late final OrtSession loaded;
    try {
      loaded = await loading;
    } catch (error, stackTrace) {
      if (identical(_sessionFuture, loading)) _sessionFuture = null;
      Error.throwWithStackTrace(error, stackTrace);
    }
    _session = loaded;
    if (_closed) throw StateError('OCR recognizer has been disposed.');
    if (!loaded.inputNames.contains(_inputName)) {
      _session = null;
      _sessionFuture = null;
      await loaded.close();
      throw StateError('ONNX model does not expose the "$_inputName" input.');
    }
    if (!loaded.outputNames.contains(_plateOutputName)) {
      _session = null;
      _sessionFuture = null;
      await loaded.close();
      throw StateError(
        'ONNX model does not expose the "$_plateOutputName" output.',
      );
    }
    return loaded;
  }

  Future<void> dispose() async {
    _closed = true;
    final readsDrained = _readsDrained;
    if (_activeReads > 0) await readsDrained!.future;
    final session = _session;
    _session = null;
    if (session != null) await session.close();
  }
}
