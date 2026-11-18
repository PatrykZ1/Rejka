import 'package:boundingbox/constants.dart';
import 'package:boundingbox/widgets/drawer.dart';
import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import 'package:video_player/video_player.dart';

import 'package:boundingbox/api/api_media.dart';
import 'package:boundingbox/models/media_item.dart';

class MediaDetailScreen extends StatefulWidget {
  final int mediaId;

  const MediaDetailScreen({super.key, required this.mediaId});

  @override
  State<MediaDetailScreen> createState() => _MediaDetailScreenState();
}

class _MediaDetailScreenState extends State<MediaDetailScreen> {
  late Future<MediaItem> _mediaFuture;

  VideoPlayerController? _videoController;

  @override
  void initState() {
    super.initState();
    _mediaFuture = _fetchMedia();
  }

  Future<MediaItem> _fetchMedia() async {
    final response = await ApiMedia().getMediaItem(widget.mediaId);
    return MediaItem.fromResponse(response);
  }

  Future<void> _initializeVideo(String url) async {
    final controller = VideoPlayerController.networkUrl(Uri.parse(url));

    _videoController = controller;

    await controller.initialize();

    if (mounted) {
      setState(() {});
    }
  }

  @override
  void dispose() {
    _videoController?.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text(applicationName)),
      drawer: AppDrawer(),
      body: FutureBuilder<MediaItem>(
        future: _mediaFuture,
        builder: (context, snapshot) {
          if (snapshot.connectionState == ConnectionState.waiting) {
            return const Center(child: CircularProgressIndicator());
          }

          if (snapshot.hasError) {
            return Center(
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  const Icon(Icons.error_outline, size: 48, color: Colors.red),
                  const SizedBox(height: 12),
                  const Text('Błąd przy wczytywaniu danych.'),
                  const SizedBox(height: 12),
                  ElevatedButton(
                    onPressed: () {
                      setState(() {
                        _videoController?.dispose();
                        _videoController = null;
                        _mediaFuture = _fetchMedia();
                      });
                    },
                    child: const Text('Spróbuj ponownie'),
                  ),
                ],
              ),
            );
          }

          if (!snapshot.hasData) {
            return const Center(child: Text('Nie znaleziono szukanego pliku.'));
          }

          return _buildMediaDetails(snapshot.data!);
        },
      ),
    );
  }

  Widget _buildMediaDetails(MediaItem item) {
    final theme = Theme.of(context);
    final extension = item.fileName.split('.').last.toLowerCase();
    final isVideo = ['mp4', 'mov', 'avi', 'mkv', 'webm'].contains(extension);

    if (isVideo && _videoController == null) {
      _initializeVideo(item.url);
    }

    return SingleChildScrollView(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          _buildMediaPreview(item, isVideo),

          Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text('Plik:', style: theme.textTheme.labelSmall),
                const SizedBox(height: 4),
                Text(
                  item.fileName,
                  style: Theme.of(context).textTheme.headlineSmall,
                ),

                const SizedBox(height: 16),

                _buildInfoSection(item),

                const SizedBox(height: 24),

                Text(
                  'Rejestracje',
                  style: Theme.of(context).textTheme.titleLarge,
                ),

                const SizedBox(height: 8),

                _buildRegistrationsList(item),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildMediaPreview(MediaItem item, bool isVideo) {
    if (isVideo) {
      final controller = _videoController;

      if (controller == null || !controller.value.isInitialized) {
        return const AspectRatio(
          aspectRatio: 16 / 9,
          child: Center(child: CircularProgressIndicator()),
        );
      }

      return AspectRatio(
        aspectRatio: controller.value.aspectRatio,
        child: Stack(
          alignment: Alignment.center,
          children: [
            VideoPlayer(controller),
            IconButton(
              iconSize: 64,
              color: Colors.white,
              icon: Icon(
                controller.value.isPlaying
                    ? Icons.pause_circle
                    : Icons.play_circle,
              ),
              onPressed: () {
                setState(() {
                  controller.value.isPlaying
                      ? controller.pause()
                      : controller.play();
                });
              },
            ),
          ],
        ),
      );
    }

    return Image.network(
      item.url,
      width: double.infinity,
      fit: BoxFit.contain,
      errorBuilder: (context, error, stackTrace) {
        return const SizedBox(
          height: 250,
          child: Center(child: Icon(Icons.broken_image, size: 64)),
        );
      },
    );
  }

  Widget _buildInfoSection(MediaItem item) {
    final formatter = DateFormat.yMMMMd('pl_PL').add_Hms();
    return Column(
      children: [
        _buildInfoTile(
          'Położenie',
          Row(
            children: [
              Expanded(child: Center(child: Text('${item.latitude.toString()}°'))),
              Expanded(child: Center(child: Text('${item.longitude.toString()}°'))),
            ],
          ),
        ),
        _buildInfoTile('Nagrano dnia', Text(formatter.format(item.recordedAt))),
        _buildInfoTile('Wgrano dnia', Text(formatter.format(item.uploadedAt))),
        _buildInfoTile(
          'Komentarz',
          Text(item.comment.isEmpty ? 'Brak komentarza.' : item.comment),
        ),
      ],
    );
  }

  Widget _buildInfoTile(String title, Widget widget) {
    return ListTile(
      contentPadding: EdgeInsets.zero,
      title: Text(title),
      subtitle: widget,
    );
  }

  Widget _buildRegistrationsList(MediaItem item) {
    return Column(
      children: item.registrations.map((registration) {
        final matchingRatings = item.ratings.where(
          (r) => r.registration == registration,
        );

        final rating = matchingRatings.isEmpty ? null : matchingRatings.first;

        return ListTile(
          contentPadding: EdgeInsets.zero,
          title: Text(registration),
          trailing: rating != null
              ? Chip(
                  avatar: const Icon(Icons.star, color: Colors.amber, size: 18),
                  label: Text(rating.rating.toString()),
                )
              : const Text('No rating', style: TextStyle(color: Colors.grey)),
        );
      }).toList(),
    );
  }
}
