import 'dart:typed_data';

import 'package:flutter/material.dart';
import 'package:video_thumbnail/video_thumbnail.dart';

class MediaThumbnail extends StatefulWidget {
  final String url;
  final String fileName;

  const MediaThumbnail({super.key, required this.url, required this.fileName});

  @override
  State<MediaThumbnail> createState() => _MediaThumbnailState();
}

class _MediaThumbnailState extends State<MediaThumbnail> {
  static const _videoExtensions = [
    '.mp4',
    '.mov',
    '.avi',
    '.mkv',
    '.webm',
    '.3gp',
    '.m4v',
  ];

  Future<Uint8List?>? _videoThumbFuture;

  bool get _isVideo {
    final name = widget.fileName.toLowerCase();
    return _videoExtensions.any(name.endsWith);
  }

  @override
  void initState() {
    super.initState();
    _initThumb();
  }

  @override
  void didUpdateWidget(covariant MediaThumbnail oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.url != widget.url) {
      _initThumb();
    }
  }

  void _initThumb() {
    _videoThumbFuture = _isVideo
        ? VideoThumbnail.thumbnailData(
            video: widget.url,
            imageFormat: ImageFormat.JPEG,
            maxWidth: 200,
            quality: 60,
          )
        : null;
  }

  Widget _placeholder(IconData icon) {
    return Container(
      color: Colors.grey.shade300,
      child: Icon(icon, color: Colors.grey.shade600),
    );
  }

  @override
  Widget build(BuildContext context) {
    if (_isVideo) {
      return FutureBuilder<Uint8List?>(
        future: _videoThumbFuture,
        builder: (context, snapshot) {
          if (snapshot.connectionState == ConnectionState.waiting) {
            return _placeholder(Icons.videocam);
          }
          if (snapshot.hasError || snapshot.data == null) {
            return _placeholder(Icons.videocam_off);
          }
          return Stack(
            fit: StackFit.expand,
            children: [
              Image.memory(snapshot.data!, fit: BoxFit.cover),
              const Center(
                child: Icon(
                  Icons.play_circle_fill,
                  color: Colors.white70,
                  size: 28,
                ),
              ),
            ],
          );
        },
      );
    }

    return Image.network(
      widget.url,
      fit: BoxFit.cover,
      cacheWidth: 200,
      loadingBuilder: (context, child, progress) {
        if (progress == null) return child;
        return _placeholder(Icons.image);
      },
      errorBuilder: (context, error, stackTrace) =>
          _placeholder(Icons.broken_image),
    );
  }
}
