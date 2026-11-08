class PlateReading {
  const PlateReading({
    required this.text,
    required this.region,
    required this.serial,
    required this.rawText,
    required this.confidence,
    required this.capturedAt,
  });

  const PlateReading.empty()
      : text = '',
        region = '',
        serial = '',
        rawText = '',
        confidence = 0,
        capturedAt = null;

  final String text;
  final String region;
  final String serial;
  final String rawText;
  final double confidence;
  final DateTime? capturedAt;

  bool get hasText => text.isNotEmpty;
  bool get hasRegion => region.isNotEmpty;
}
