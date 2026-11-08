class PlateSegment {
  const PlateSegment({required this.text, required this.cx});

  final String text;
  final double cx;
}

class PlateTextRecognizer {
  static Combined combineSegments(List<PlateSegment> segments) {
    final spatial = segments.where((segment) => segment.cx >= 0).toList()
      ..sort((a, b) => a.cx.compareTo(b.cx));
    final ordered = spatial.isNotEmpty ? spatial : segments;
    return Combined(
      text: ordered.map((segment) => segment.text).join(),
      region: '',
      serial: '',
    );
  }

  static String normalizePlateText(String raw) => raw;

  static String? confirmedConsecutiveText(List<String> readings, int required) {
    if (required < 1 || readings.length < required) return null;

    final candidate = readings.last;
    if (candidate.isEmpty) return null;

    for (final reading in readings.skip(readings.length - required)) {
      if (reading != candidate) return null;
    }
    return candidate;
  }
}

class Combined {
  const Combined({
    required this.text,
    required this.region,
    required this.serial,
  });

  final String text;
  final String region;
  final String serial;

  bool get isEmpty => text.isEmpty;
}
