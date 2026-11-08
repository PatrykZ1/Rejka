import 'package:flutter/material.dart';

import '../models/tracked_plate.dart';

class PlateOverlay extends StatelessWidget {
  const PlateOverlay({
    super.key,
    required this.tracks,
  });

  final List<TrackedPlate> tracks;

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (context, constraints) {
        final w = constraints.maxWidth;
        final h = constraints.maxHeight;
        return Stack(
          clipBehavior: Clip.none,
          children: [
            for (final t in tracks)
              Positioned(
                left: (t.box.left * w).clamp(0.0, w - 24),
                top: (t.box.top * h).clamp(8.0, h - 14),
                width: (t.box.width * w).clamp(44.0, w),
                height: (t.box.height * h).clamp(28.0, h),
                child: _PlateBox(track: t),
              ),
          ],
        );
      },
    );
  }
}

class _PlateBox extends StatelessWidget {
  const _PlateBox({required this.track});

  final TrackedPlate track;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    final accent = track.selected ? scheme.primary : Colors.white70;
    return Container(
      decoration: BoxDecoration(
        color: track.selected
            ? scheme.primary.withValues(alpha: 0.18)
            : Colors.transparent,
        border: Border.all(
          color: accent,
          width: track.selected ? 3 : 2,
        ),
        borderRadius: BorderRadius.circular(4),
      ),
      child: Stack(
        clipBehavior: Clip.none,
        children: [
          Positioned(
            left: -2,
            top: -24,
            child: Container(
              padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
              decoration: BoxDecoration(
                color: track.hasText
                    ? Colors.black.withValues(alpha: 0.85)
                    : Colors.black.withValues(alpha: 0.72),
                borderRadius: BorderRadius.circular(6),
                border: Border.all(
                    color: track.selected
                        ? scheme.primary
                        : Colors.white.withValues(alpha: 0.35)),
              ),
              child: track.ocrInFlight
                  ? const SizedBox(
                      width: 14,
                      height: 14,
                      child: CircularProgressIndicator(
                        strokeWidth: 2,
                        color: Colors.white,
                      ),
                    )
                  : Row(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        Text(
                          track.hasText ? track.text : 'SCAN',
                          style: TextStyle(
                            color: Colors.white,
                            fontSize: 12,
                            fontWeight: FontWeight.w800,
                            letterSpacing: 1.0,
                            fontFamily:
                                track.hasText ? 'monospace' : null,
                          ),
                        ),
                      ],
                    ),
            ),
          ),
        ],
      ),
    );
  }
}
