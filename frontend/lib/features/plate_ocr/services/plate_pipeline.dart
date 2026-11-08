import 'dart:typed_data';

import 'package:flutter/material.dart';
import 'package:ultralytics_yolo/ultralytics_yolo.dart';

import '../models/tracked_plate.dart';
import 'fast_plate_text_recognizer.dart';
import 'plate_cropper.dart';
import 'plate_text_recognizer.dart';

enum PlatePipelineState { idle, reading }

class PlatePipeline {
  PlatePipeline({
    required this.onTracks,
    required this.onState,
    this.minConfidence = 0.40,
    this.minRelativeArea = 0.002,
    this.maxTracks = 8,
    this.matchIou = 0.30,
    this.trackTtl = const Duration(milliseconds: 1000),
    this.ocrCooldown = const Duration(milliseconds: 1000),
    this.ocrVotesRequired = 2,
    this.ocrMaxAttempts = 8,
    required Future<Uint8List?> Function() captureFrame,
  }) : _recognizer = FastPlateTextRecognizer(),
       _captureFrame = captureFrame;

  final void Function(List<TrackedPlate> tracks) onTracks;
  final void Function(PlatePipelineState state) onState;

  final double minConfidence;
  final double minRelativeArea;
  final int maxTracks;
  final double matchIou;
  final Duration trackTtl;
  final Duration ocrCooldown;
  final int ocrVotesRequired;
  final int ocrMaxAttempts;
  final Future<Uint8List?> Function() _captureFrame;

  final FastPlateTextRecognizer _recognizer;

  final Map<int, TrackedPlate> _tracks = {};
  int _nextId = 1;
  bool _disposed = false;
  final Map<int, DateTime> _lastOcrAt = {};

  final Map<int, List<String>> _ocrVotes = {};

  void handleDetections(List<YOLOResult> results) {
    if (_disposed) return;
    final now = DateTime.now();

    _tracks.removeWhere((id, t) => now.difference(t.lastSeen) > trackTtl);
    _lastOcrAt.removeWhere((id, _) => !_tracks.containsKey(id));

    final candidates =
        results
            .where(
              (r) =>
                  r.confidence >= minConfidence &&
                  r.normalizedBox.width * r.normalizedBox.height >=
                      minRelativeArea,
            )
            .toList()
          ..sort((a, b) {
            final aa = a.normalizedBox.width * a.normalizedBox.height;
            final bb = b.normalizedBox.width * b.normalizedBox.height;
            return bb.compareTo(aa);
          });

    final limited = candidates.take(maxTracks).toList();
    final matchedIds = <int>{};

    for (final det in limited) {
      final box = _clampBox(det.normalizedBox);
      int? bestId;
      double bestIou = 0;
      for (final entry in _tracks.entries) {
        if (matchedIds.contains(entry.key)) continue;
        final iou = _iou(entry.value.box, box);
        if (iou > bestIou) {
          bestIou = iou;
          bestId = entry.key;
        }
      }
      if (bestId != null && bestIou >= matchIou) {
        final prev = _tracks[bestId]!;
        _tracks[bestId] = prev.copyWith(
          box: box,
          confidence: det.confidence,
          lastSeen: now,
        );
        matchedIds.add(bestId);
      } else {
        final id = _nextId++;
        _tracks[id] = TrackedPlate(
          id: id,
          box: box,
          confidence: det.confidence,
          text: '',
          region: '',
          serial: '',
          reading: false,
          lastSeen: now,
          ocrInFlight: false,
          selected: false,
        );
        matchedIds.add(id);
      }
    }

    for (final entry in _tracks.entries) {
      final id = entry.key;
      final t = entry.value;
      if (t.hasText || t.ocrInFlight) continue;

      final attempts = _ocrVotes[id]?.length ?? 0;
      if (attempts >= ocrMaxAttempts) continue;

      final last = _lastOcrAt[id];
      if (last != null && now.difference(last) < ocrCooldown) continue;
      _lastOcrAt[id] = now;
      _startOcr(id);
    }

    onTracks(_tracks.values.toList());
    final anyReading = _tracks.values.any((t) => t.ocrInFlight);
    onState(anyReading ? PlatePipelineState.reading : PlatePipelineState.idle);
  }

  void _startOcr(int id) {
    if (_disposed || _tracks[id] == null || _tracks[id]!.ocrInFlight) return;
    final cur = _tracks[id]!;
    _tracks[id] = cur.copyWith(ocrInFlight: true, reading: true);
    if (!_disposed) onTracks(_tracks.values.toList());
    if (!_disposed) onState(PlatePipelineState.reading);

    _doOcr(id);
  }

  Future<void> _doOcr(int id) async {
    try {
      final frame = await _captureFrame();
      if (frame == null || _disposed) {
        _finishFlight(id);
        return;
      }

      final cur = _tracks[id];
      if (cur == null) {
        _finishFlight(id);
        return;
      }
      final out = await cropPlateCrop(
        CropRequest(
          jpeg: frame,
          left: cur.box.left,
          top: cur.box.top,
          right: cur.box.right,
          bottom: cur.box.bottom,
        ),
      );
      if (out == null || _disposed) {
        _finishFlight(id);
        return;
      }
      final reading = await _recognizer.readPlateBytes(
        out.jpeg,
        cur.confidence,
      );
      if (_disposed) return;
      final cur2 = _tracks[id];
      if (cur2 == null) return;

      _ocrVotes.putIfAbsent(id, () => []);
      if (reading.hasText) {
        _ocrVotes[id]!.add(reading.text);
      } else {
        _ocrVotes[id]!.add('');
      }

      final confirmed = _getConfirmedText(id);
      final votes = _ocrVotes[id]!;

      if (confirmed != null) {
        _tracks[id] = cur2.copyWith(
          text: confirmed.text,
          region: confirmed.region,
          serial: confirmed.serial,
          ocrInFlight: false,
          reading: false,
          lastSeen: DateTime.now(),
        );
        _ocrVotes.remove(id);
      } else if (votes.length >= ocrMaxAttempts) {
        _tracks[id] = cur2.copyWith(ocrInFlight: false, reading: false);
        _ocrVotes.remove(id);
      } else {
        _tracks[id] = cur2.copyWith(ocrInFlight: false, reading: false);
      }

      onTracks(_tracks.values.toList());
      onState(PlatePipelineState.idle);
    } catch (e) {
      debugPrint('PlatePipeline track $id error: $e');
      _finishFlight(id);
    }
  }

  Combined? _getConfirmedText(int id) {
    final votes = _ocrVotes[id];
    if (votes == null || votes.isEmpty) return null;

    final confirmedText = PlateTextRecognizer.confirmedConsecutiveText(
      votes,
      ocrVotesRequired,
    );
    if (confirmedText == null) return null;

    return Combined(text: confirmedText, region: '', serial: '');
  }

  void _finishFlight(int id) {
    final cur = _tracks[id];
    if (cur != null && cur.ocrInFlight) {
      _tracks[id] = cur.copyWith(ocrInFlight: false, reading: false);
      if (!_disposed) onTracks(_tracks.values.toList());
    }
    if (!_disposed) onState(PlatePipelineState.idle);
  }

  void clearTexts() {
    for (final entry in _tracks.entries) {
      _tracks[entry.key] = entry.value.copyWith(
        text: '',
        region: '',
        serial: '',
        reading: false,
        ocrInFlight: false,
        selected: false,
      );
    }
    _lastOcrAt.clear();
    _ocrVotes.clear();
    if (!_disposed) onTracks(_tracks.values.toList());
  }

  Rect _clampBox(Rect b) {
    double l = b.left.clamp(0.0, 1.0);
    double t = b.top.clamp(0.0, 1.0);
    double r = b.right.clamp(0.0, 1.0);
    double bb = b.bottom.clamp(0.0, 1.0);
    if (r <= l) r = (l + 0.01).clamp(0.0, 1.0);
    if (bb <= t) bb = (t + 0.01).clamp(0.0, 1.0);
    return Rect.fromLTRB(l, t, r, bb);
  }

  double _iou(Rect a, Rect b) {
    final x0 = a.left > b.left ? a.left : b.left;
    final y0 = a.top > b.top ? a.top : b.top;
    final x1 = a.right < b.right ? a.right : b.right;
    final y1 = a.bottom < b.bottom ? a.bottom : b.bottom;
    final iw = x1 - x0;
    final ih = y1 - y0;
    if (iw <= 0 || ih <= 0) return 0;
    final inter = iw * ih;
    final union = a.width * a.height + b.width * b.height - inter;
    if (union <= 0) return 0;
    return inter / union;
  }

  Future<void> dispose() async {
    _disposed = true;
    await _recognizer.dispose();
  }
}
