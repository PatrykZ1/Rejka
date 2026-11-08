import 'package:flutter/foundation.dart';
import 'package:image/image.dart' as img;

class CropRequest {
  const CropRequest({
    required this.jpeg,
    required this.left,
    required this.top,
    required this.right,
    required this.bottom,
  });

  final Uint8List jpeg;
  final double left;
  final double top;
  final double right;
  final double bottom;

  Map<String, dynamic> toMap() => {
    'bytes': jpeg,
    'l': left,
    't': top,
    'r': right,
    'b': bottom,
  };
}

class CropOutput {
  const CropOutput({
    required this.jpeg,
    required this.width,
    required this.height,
  });

  final Uint8List jpeg;
  final int width;
  final int height;

  Map<String, dynamic> toMap() => {'bytes': jpeg, 'w': width, 'h': height};

  static CropOutput? fromMap(Map<String, dynamic>? m) {
    if (m == null) return null;
    final bytes = m['bytes'];
    if (bytes is! Uint8List) return null;
    return CropOutput(
      jpeg: bytes,
      width: (m['w'] as num?)?.toInt() ?? 0,
      height: (m['h'] as num?)?.toInt() ?? 0,
    );
  }
}

Future<CropOutput?> cropPlateCrop(CropRequest request) async {
  final map = await compute(_cropEntry, request.toMap());
  return CropOutput.fromMap(map);
}

Map<String, dynamic>? _cropEntry(Map<String, dynamic> m) {
  final bytes = m['bytes'] as Uint8List;
  final double l = (m['l'] as num).toDouble();
  final double t = (m['t'] as num).toDouble();
  final double r = (m['r'] as num).toDouble();
  final double b = (m['b'] as num).toDouble();

  final decoded = img.decodeJpg(bytes);
  if (decoded == null) return null;

  final w = decoded.width;
  final h = decoded.height;

  double padX = (r - l) * 0.25;
  double padY = (b - t) * 0.12;
  double x0 = (l - padX) * w;
  double y0 = (t - padY) * h;
  double x1 = (r + padX) * w;
  double y1 = (b + padY) * h;

  double insetX = 0;
  double insetY = (y1 - y0) * 0.06;
  x0 += insetX;
  y0 += insetY;
  x1 -= insetX;
  y1 -= insetY;

  int ix = x0.floor().clamp(0, w - 1);
  int iy = y0.floor().clamp(0, h - 1);
  int iw = (x1.ceil() - ix).clamp(1, w - ix);
  int ih = (y1.ceil() - iy).clamp(1, h - iy);

  if (iw < 20 || ih < 8) return null;

  img.Image cropped = img.copyCrop(
    decoded,
    x: ix,
    y: iy,
    width: iw,
    height: ih,
  );

  final out = img.encodeJpg(cropped, quality: 92);
  return {
    'bytes': Uint8List.fromList(out),
    'w': cropped.width,
    'h': cropped.height,
  };
}
