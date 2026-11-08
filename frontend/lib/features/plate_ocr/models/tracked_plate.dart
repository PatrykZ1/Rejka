import 'package:flutter/material.dart';

class TrackedPlate {
  const TrackedPlate({
    required this.id,
    required this.box,
    required this.confidence,
    required this.text,
    required this.region,
    required this.serial,
    required this.reading,
    required this.lastSeen,
    required this.ocrInFlight,
    this.selected = false,
  });

  final int id;
  final Rect box;
  final double confidence;
  final String text;
  final String region;
  final String serial;
  final bool reading;
  final DateTime lastSeen;
  final bool ocrInFlight;
  final bool selected;

  bool get hasText => text.isNotEmpty;

  TrackedPlate copyWith({
    Rect? box,
    double? confidence,
    String? text,
    String? region,
    String? serial,
    bool? reading,
    DateTime? lastSeen,
    bool? ocrInFlight,
    bool? selected,
  }) {
    return TrackedPlate(
      id: id,
      box: box ?? this.box,
      confidence: confidence ?? this.confidence,
      text: text ?? this.text,
      region: region ?? this.region,
      serial: serial ?? this.serial,
      reading: reading ?? this.reading,
      lastSeen: lastSeen ?? this.lastSeen,
      ocrInFlight: ocrInFlight ?? this.ocrInFlight,
      selected: selected ?? this.selected,
    );
  }
}
