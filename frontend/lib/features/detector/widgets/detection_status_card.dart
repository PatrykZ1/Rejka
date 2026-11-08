import 'package:flutter/material.dart';

class DetectionStatusCard extends StatelessWidget {
  const DetectionStatusCard({
    super.key,
    required this.detectionCount,
    required this.lastDetection,
    required this.isStreaming,
  });

  final int detectionCount;
  final String lastDetection;
  final bool isStreaming;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    final hasDetection = detectionCount > 0;

    return Container(
      margin: const EdgeInsets.all(16),
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
      decoration: BoxDecoration(
        color: Colors.black.withValues(alpha: 0.65),
        borderRadius: BorderRadius.circular(18),
        border: Border.all(
          color: hasDetection
              ? scheme.primary.withValues(alpha: 0.6)
              : Colors.white.withValues(alpha: 0.12),
          width: 1,
        ),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: 0.35),
            blurRadius: 16,
            offset: const Offset(0, 6),
          ),
        ],
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Container(
            width: 42,
            height: 42,
            decoration: BoxDecoration(
              color: hasDetection
                  ? scheme.primary.withValues(alpha: 0.2)
                  : Colors.white.withValues(alpha: 0.08),
              shape: BoxShape.circle,
            ),
            child: Icon(
              hasDetection
                  ? Icons.directions_car_filled
                  : Icons.videocam_outlined,
              color: hasDetection ? scheme.primary : Colors.white70,
            ),
          ),
          const SizedBox(width: 12),
          Flexible(
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Container(
                      width: 8,
                      height: 8,
                      decoration: BoxDecoration(
                        shape: BoxShape.circle,
                        color: isStreaming ? Colors.greenAccent : Colors.orange,
                      ),
                    ),
                    const SizedBox(width: 6),
                    Text(
                      'Detekcje: $detectionCount',
                      style: const TextStyle(
                        fontSize: 16,
                        fontWeight: FontWeight.bold,
                        color: Colors.white,
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 2),
                Text(
                  lastDetection,
                  style: const TextStyle(
                    fontSize: 13.5,
                    color: Colors.white70,
                  ),
                  overflow: TextOverflow.ellipsis,
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}
