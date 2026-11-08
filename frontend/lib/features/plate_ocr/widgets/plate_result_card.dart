import 'package:flutter/material.dart';

import '../models/tracked_plate.dart';
import '../services/plate_pipeline.dart';

class PlateResultCard extends StatelessWidget {
  const PlateResultCard({
    super.key,
    required this.plateText,
    required this.region,
    required this.tracks,
    required this.pipelineState,
    required this.onClear,
  });

  final String plateText;
  final String region;
  final List<TrackedPlate> tracks;
  final PlatePipelineState pipelineState;
  final VoidCallback onClear;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    final hasPlate = plateText.isNotEmpty;
    final anyReading = tracks.any((t) => t.ocrInFlight);

    String status;
    switch (pipelineState) {
      case PlatePipelineState.reading:
        status = anyReading ? 'Odczytuję tablicę...' : 'Wykryto tablicę';
        break;
      case PlatePipelineState.idle:
        status = hasPlate
            ? 'Tablica odczytana'
            : 'Czekam na tablicę rejestracyjną';
        break;
    }

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
      decoration: BoxDecoration(
        color: Colors.black.withValues(alpha: 0.62),
        borderRadius: BorderRadius.circular(16),
        border: Border.all(
          color: hasPlate
              ? scheme.primary.withValues(alpha: 0.55)
              : Colors.white.withValues(alpha: 0.12),
        ),
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.center,
        children: [
          if (pipelineState == PlatePipelineState.reading)
            const SizedBox(
              width: 18,
              height: 18,
              child: CircularProgressIndicator(strokeWidth: 2.2),
            )
          else
            Icon(
              hasPlate ? Icons.confirmation_number_outlined : Icons.text_fields,
              color: hasPlate ? scheme.primary : Colors.white70,
              size: 20,
            ),
          const SizedBox(width: 8),
          Expanded(
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                if (hasPlate && region.isNotEmpty)
                  Container(
                    padding: const EdgeInsets.symmetric(
                      horizontal: 8,
                      vertical: 2,
                    ),
                    decoration: BoxDecoration(
                      color: scheme.primary.withValues(alpha: 0.22),
                      borderRadius: BorderRadius.circular(8),
                    ),
                    child: Text(
                      region,
                      style: TextStyle(
                        color: scheme.primary,
                        fontSize: 11,
                        fontWeight: FontWeight.w800,
                        letterSpacing: 1.5,
                      ),
                    ),
                  ),
                if (hasPlate && region.isNotEmpty)
                  const SizedBox(height: 2),
                Text(
                  hasPlate ? plateText : status,
                  style: TextStyle(
                    color: Colors.white,
                    fontSize: hasPlate ? 18 : 13,
                    fontWeight: hasPlate ? FontWeight.w800 : FontWeight.w600,
                    letterSpacing: hasPlate ? 2.0 : 0,
                    fontFamily: hasPlate ? 'monospace' : null,
                  ),
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                ),
                Text(
                  hasPlate && region.isNotEmpty
                      ? 'Region: $region - $status'
                      : status,
                  style: const TextStyle(
                    color: Colors.white60,
                    fontSize: 11,
                  ),
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                ),
              ],
            ),
          ),
          IconButton(
            tooltip: 'Clear readings',
            visualDensity: VisualDensity.compact,
            padding: EdgeInsets.zero,
            constraints: const BoxConstraints(minWidth: 36, minHeight: 36),
            onPressed: onClear,
            icon: const Icon(
              Icons.delete_sweep_outlined,
              color: Colors.white70,
            ),
          ),
        ],
      ),
    );
  }
}
