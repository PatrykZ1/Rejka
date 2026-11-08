import 'package:boundingbox/features/plate_ocr/services/fast_plate_text_recognizer.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  group('FastPlateTextRecognizer.decodePlateOutput', () {
    test('decodes characters from per-slot scores and strips padding', () {
      const plate = 'KR123AB';
      final values = List<double>.filled(10 * 37, -1);
      for (var slot = 0; slot < plate.length; slot++) {
        final index = '0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ_'.indexOf(
          plate[slot],
        );
        values[slot * 37 + index] = 1;
      }
      values[7 * 37 + 36] = 1;
      values[8 * 37 + 36] = 1;
      values[9 * 37 + 36] = 1;

      expect(FastPlateTextRecognizer.decodePlateOutput(values), plate);
    });

    test('throws when output length does not match the model shape', () {
      expect(
        () => FastPlateTextRecognizer.decodePlateOutput([0]),
        throwsFormatException,
      );
    });

    test('throws when model output contains non-numeric values', () {
      final values = List<double>.filled(10 * 37, 0);
      values[0] = double.nan;

      expect(
        () => FastPlateTextRecognizer.decodePlateOutput(values),
        throwsFormatException,
      );
    });
  });
}
