import 'package:boundingbox/features/detector/widgets/detection_status_card.dart';
import 'package:boundingbox/features/plate_ocr/widgets/plate_result_card.dart';
import 'package:boundingbox/features/plate_ocr/services/plate_pipeline.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  testWidgets('DetectionStatusCard renders detection count and status correctly', (WidgetTester tester) async {
    await tester.pumpWidget(
      const MaterialApp(
        home: Scaffold(
          body: DetectionStatusCard(
            detectionCount: 2,
            lastDetection: 'WA 12345 95.5%',
            isStreaming: true,
          ),
        ),
      ),
    );

    expect(find.text('Detekcje: 2'), findsOneWidget);
    expect(find.text('WA 12345 95.5%'), findsOneWidget);
    expect(find.byIcon(Icons.directions_car_filled), findsOneWidget);
  });

  testWidgets('PlateResultCard renders plate text and handles clear action', (WidgetTester tester) async {
    bool cleared = false;

    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: PlateResultCard(
            plateText: 'BI2137X',
            region: 'BI',
            tracks: const [],
            pipelineState: PlatePipelineState.idle,
            onClear: () {
              cleared = true;
            },
          ),
        ),
      ),
    );

    expect(find.text('BI2137X'), findsOneWidget);
    expect(find.text('Region: BI - Tablica odczytana'), findsOneWidget);

    await tester.tap(find.byType(IconButton));
    await tester.pump();

    expect(cleared, isTrue);
  });
}