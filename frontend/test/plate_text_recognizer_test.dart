import 'package:boundingbox/features/plate_ocr/services/plate_text_recognizer.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  group('PlateTextRecognizer', () {
    test('preserves OCR output without plate-format validation', () {
      expect(PlateTextRecognizer.normalizePlateText('KMY12_45'), 'KMY12_45');
      expect(
        PlateTextRecognizer.combineSegments([
          const PlateSegment(text: 'KMY12_45', cx: 0),
        ]).text,
        'KMY12_45',
      );
    });

    test('joins spatial segments from left to right without validation', () {
      final combined = PlateTextRecognizer.combineSegments(const [
        PlateSegment(text: '4567_', cx: 80),
        PlateSegment(text: 'KMY', cx: 20),
      ]);

      expect(combined.text, 'KMY4567_');
      expect(combined.region, isEmpty);
      expect(combined.serial, isEmpty);
    });

    test('confirms only the required identical consecutive readings', () {
      expect(
        PlateTextRecognizer.confirmedConsecutiveText([
          'KMY1234',
          'ABC1234',
          'ABC1234',
        ], 2),
        'ABC1234',
      );
      expect(
        PlateTextRecognizer.confirmedConsecutiveText([
          'KMY1234',
          'ABC1234',
          'KMY1234',
        ], 2),
        isNull,
      );
    });

    test('an empty or differing reading breaks the consecutive sequence', () {
      expect(
        PlateTextRecognizer.confirmedConsecutiveText([
          'KMY1234',
          '',
          'KMY1234',
        ], 2),
        isNull,
      );
      expect(
        PlateTextRecognizer.confirmedConsecutiveText(['KMY1234'], 2),
        isNull,
      );
    });
  });
}
