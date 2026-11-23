import 'package:boundingbox/constants.dart';
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
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
  late Future<MediaItem?> _mediaFuture;

  VideoPlayerController? _videoController;
  String? _videoError;
  String? _initializedVideoUrl;

  @override
  void initState() {
    super.initState();
    _mediaFuture = _fetchMedia();
  }

  Future<MediaItem?> _fetchMedia() async {
    final response = await ApiMedia().getMediaItem(widget.mediaId);
    if (response == null) {
      return null;
    }
    return MediaItem.fromResponse(response);
  }

  Future<void> _initializeVideo(String url) async {
    if (_initializedVideoUrl == url) return;

    try {
      _initializedVideoUrl = url;
      final controller = VideoPlayerController.networkUrl(Uri.parse(url))
        ..initialize().then((_) {
          if (mounted) {
            setState(() {});
          }
        });
      _videoController = controller;
    } catch (e) {
      if (mounted) {
        setState(() {
          _videoError = 'Nie udało się wczytać nagrania.';
        });
      }
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
      body: FutureBuilder<MediaItem?>(
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
                        _initializedVideoUrl = null;
                        _videoError = null;
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

          if (snapshot.data == null) {
            return const Center(child: Text('Szukany plik nie istnieje.'));
          }

          return _buildMediaDetails(snapshot.data!);
        },
      ),
    );
  }

  Widget _buildMediaDetails(MediaItem item) {
    final theme = Theme.of(context);
    final extension = item.url.split('.').last.toLowerCase();
    final isVideo = ['mp4', 'mov', 'avi', 'mkv', 'webm'].contains(extension);

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
                Text(item.fileName, style: theme.textTheme.headlineSmall),

                const SizedBox(height: 16),

                _buildInfoSection(item),

                const SizedBox(height: 24),

                Text('Rejestracje', style: theme.textTheme.titleLarge),

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
      _initializeVideo(item.url);
      final controller = _videoController;

      if (_videoError != null) {
        return SizedBox(
          height: 250,
          child: Center(
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                const Icon(Icons.error_outline, size: 48),
                const SizedBox(height: 12),
                Text(_videoError!),
                TextButton(
                  onPressed: () {
                    setState(() {
                      _videoController?.dispose();
                      _videoController = null;
                      _initializedVideoUrl = null;
                      _videoError = null;
                    });

                    _initializeVideo(item.url);
                  },
                  child: const Text('Spróbuj ponownie'),
                ),
              ],
            ),
          ),
        );
      }

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
                if (controller.value.isPlaying) {
                  controller.pause();
                } else {
                  controller.play();
                }
                setState(() {});
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
      loadingBuilder: (context, child, loadingProgress) {
        if (loadingProgress == null) return child;

        return const SizedBox(
          height: 250,
          child: Center(child: CircularProgressIndicator()),
        );
      },
      errorBuilder: (context, error, stackTrace) {
        return const SizedBox(
          height: 250,
          child: Center(
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                Icon(Icons.broken_image, size: 64),
                SizedBox(height: 16),
                Text('Nie udało się wczytać obrazu.'),
              ],
            ),
          ),
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
              Expanded(child: Center(child: Text('${item.latitude}°'))),
              Expanded(child: Center(child: Text('${item.longitude}°'))),
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
          onTap: () {
            context.push('/registration/$registration');
          },
          title: Text(registration),
          trailing: rating != null
              ? Chip(
                  avatar: const Icon(Icons.star, color: Colors.amber, size: 18),
                  label: Text(rating.rating.toString()),
                )
              : const Text('Brak oceny.', style: TextStyle(color: Colors.grey)),
        );
      }).toList(),
    );
  }
}
