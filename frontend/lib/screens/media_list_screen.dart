import 'package:boundingbox/constants.dart';
import 'package:boundingbox/models/media_filter.dart';
import 'package:boundingbox/models/paginated_media_list.dart';
import 'package:boundingbox/widgets/drawer.dart';
import 'package:flutter/material.dart';

import 'package:boundingbox/api/api_media.dart';

class MediaListScreen extends StatefulWidget {
  const MediaListScreen({super.key});

  @override
  State<MediaListScreen> createState() => _MediaListScreenState();
}

class _MediaListScreenState extends State<MediaListScreen> {
  late Future<PaginatedMediaList?> _mediaListFuture;

  int totalCount = 1;
  int currentPage = 0;
  int totalPages = 1;
  MediaFilter filter = MediaFilter();

  @override
  void initState() {
    super.initState();
    _mediaListFuture = _fetchMediaList();
  }

  Future<PaginatedMediaList?> _fetchMediaList() async {
    final response = await ApiMedia().getPagedMedia(page: currentPage, pageSize: 10, filter: filter);
    if (response == null) {
      return null;
    }
    return PaginatedMediaList.fromResponse(response);
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text(applicationName)),
      drawer: AppDrawer(),
      body: FutureBuilder<PaginatedMediaList?>(
        future: _mediaListFuture,
        builder: (context, snapshot) {
          if (snapshot.connectionState == ConnectionState.waiting) {
            return const Center(child: CircularProgressIndicator());
          }

          if (snapshot.hasError || !snapshot.hasData) {
            return Center(
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  const Icon(Icons.error_outline, size: 48, color: Colors.red),
                  const SizedBox(height: 12),
                  const Text('Błąd przy wczytywaniu listy plików.'),
                  const SizedBox(height: 12),
                  ElevatedButton(
                    onPressed: () {
                      setState(() {
                        _mediaListFuture = _fetchMediaList();
                      });
                    },
                    child: const Text('Spróbuj ponownie'),
                  ),
                ],
              ),
            );
          }

          if (snapshot.data == null) {
            return const Center(child: Text('Nie wczytano danych.'));
          }

          return _buildMediaList(snapshot.data!);
        },
      ),
    );
  }
}
