import 'package:flutter/material.dart';
import 'package:ultralytics_yolo/ultralytics_yolo.dart';

import '../features/drawer.dart';
import '../features/plate_ocr/models/tracked_plate.dart';
import '../features/plate_ocr/services/plate_pipeline.dart';
import '../features/plate_ocr/widgets/plate_overlay.dart';
import '../features/plate_ocr/widgets/plate_result_card.dart';
import '../features/detector/widgets/detection_status_card.dart';

class DetectorScreen extends StatefulWidget {
  const DetectorScreen({super.key});

  @override
  State<DetectorScreen> createState() => _DetectorScreenState();
}

class _DetectorScreenState extends State<DetectorScreen> {
  final YOLOViewController _yoloController = YOLOViewController();

  int detectionCount = 0;
  String lastDetection = 'Brak detekcji';
  bool isStreaming = true;

  String plateText = '';
  String plateRegion = '';
  List<TrackedPlate> tracks = [];
  PlatePipelineState pipelineState = PlatePipelineState.idle;
  PlatePipeline? _pipeline;

  @override
  void initState() {
    super.initState();
    _pipeline = PlatePipeline(
      captureFrame: () => _yoloController.captureFrame(),
      onTracks: (List<TrackedPlate> updated) {
        if (!mounted) return;
        setState(() {
          tracks = updated;
          final withText =
              updated.where((t) => t.hasText).toList(growable: false);
          if (withText.isNotEmpty) {
            withText.sort((a, b) => b.lastSeen.compareTo(a.lastSeen));
            plateText = withText.first.text;
            plateRegion = withText.first.region;
          }
        });
      },
      onState: (PlatePipelineState s) {
        if (!mounted) return;
        setState(() => pipelineState = s);
      },
    );
  }

  @override
  void dispose() {
    _pipeline?.dispose();
    _yoloController.dispose();
    super.dispose();
  }

  void _handleResults(List<YOLOResult> results) {
    if (!mounted) return;
    setState(() {
      detectionCount = results.length;
      if (results.isEmpty) {
        lastDetection = 'Brak detekcji';
      } else {
        final first = results.first;
        lastDetection =
            '${first.className} ${(first.confidence * 100).toStringAsFixed(1)}%';
      }
    });
    _pipeline?.handleDetections(results);
  }

  void _clearReadings() {
    _pipeline?.clearTexts();
    setState(() {
      plateText = '';
      plateRegion = '';
      tracks = List<TrackedPlate>.from(tracks
          .map((t) => t.copyWith(
                text: '',
                region: '',
                serial: '',
                reading: false,
                ocrInFlight: false,
              ))
          .toList(growable: false));
    });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      drawer: const AppDrawer(),

      body: Stack(
        fit: StackFit.expand,
        children: [
          YOLOView(
            modelPath: 'assets/models/best.tflite',
            task: YOLOTask.detect,
            controller: _yoloController,
            lensFacing: LensFacing.back,
            cameraResolution: '640x480',
            confidenceThreshold: 0.20,
            iouThreshold: 0.45,
            useGpu: true,
            onModelLoad: (String modelPath, YOLOTask? task) {
              debugPrint('YOLO MODEL LOADED: $modelPath task=$task');
            },
            onModelError: (Object error, String modelPath, YOLOTask? task) {
              debugPrint('YOLO MODEL ERROR: $error model=$modelPath task=$task');
            },
            onResult: _handleResults,
            onPerformanceMetrics: (metrics) {
              debugPrint('YOLO FPS: ${metrics.fps.toStringAsFixed(1)}');
            },
          ),

          Positioned(
            top: MediaQuery.of(context).padding.top + 8,
            left: 12,
            child: Builder(
              builder: (context) => Material(
                color: Colors.black54,
                shape: const CircleBorder(),
                child: IconButton(
                  icon: const Icon(
                    Icons.menu,
                    color: Colors.white,
                  ),
                  onPressed: () {
                    Scaffold.of(context).openDrawer();
                  },
                ),
              ),
            ),
          ),

          SafeArea(
            child: Align(
              alignment: Alignment.topCenter,
              child: DetectionStatusCard(
                detectionCount: detectionCount,
                lastDetection: lastDetection,
                isStreaming: isStreaming,
              ),
            ),
          ),
          if (tracks.isNotEmpty)
            Positioned.fill(
              child: Padding(
                padding: const EdgeInsets.only(top: 96),
                child: PlateOverlay(
                  tracks: tracks,
                ),
              ),
            ),
          Positioned(
            left: 12,
            right: 12,
            bottom: 0,
            child: SafeArea(
              top: false,
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  PlateResultCard(
                    plateText: plateText,
                    region: plateRegion,
                    tracks: tracks,
                    pipelineState: pipelineState,
                    onClear: _clearReadings,
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
