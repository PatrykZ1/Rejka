import 'package:boundingbox/constants.dart';
import 'package:boundingbox/models/media_filter.dart';
import 'package:boundingbox/models/media_item.dart';
import 'package:boundingbox/models/paginated_media_list.dart';
import 'package:boundingbox/widgets/drawer.dart';
import 'package:boundingbox/widgets/media_thumbnail.dart';
import 'package:flutter/material.dart';

import 'package:boundingbox/api/api_media.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';

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
    final response = await ApiMedia().getPagedMedia(
      page: currentPage,
      pageSize: 10,
      filter: filter,
    );
    if (response == null) {
      return null;
    }
    return PaginatedMediaList.fromResponse(response);
  }

  void _goToPage(int page) {
    setState(() {
      currentPage = page;
      _mediaListFuture = _fetchMediaList();
    });
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

          return _buildMediaList(context, snapshot.data!);
        },
      ),
    );
  }

  Widget _buildMediaList(BuildContext context, PaginatedMediaList list) {
    final itemCount = list.items.length + 2;

    return ListView.builder(
      itemCount: itemCount,
      itemBuilder: (context, index) {
        if (index == 0) {
          return Padding(
            padding: const EdgeInsets.all(12),
            child: Align(
              alignment: Alignment.centerLeft,
              child: OutlinedButton.icon(
                onPressed: _showFilterDialog,
                icon: const Icon(Icons.filter_list),
                label: const Text('Filtry'),
              ),
            ),
          );
        }

        if (index == itemCount - 1) {
          return _buildPaginationBar(list);
        }

        final item = list.items[index - 1];
        return _buildMediaTile(context, item);
      },
    );
  }

  void _showFilterDialog() {
    showDialog(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Filtry'),
        content: const Text(''),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(context).pop(),
            child: const Text('Gotowe'),
          ),
        ],
      ),
    );
  }

  Widget _buildMediaTile(BuildContext context, MediaItem item) {
    final formatter = DateFormat.yMMMMd('pl_PL').add_Hms();
    return ListTile(
      leading: SizedBox(
        width: 64,
        height: 64,
        child: ClipRRect(
          borderRadius: BorderRadius.circular(8),
          child: MediaThumbnail(url: item.url, fileName: item.fileName),
        ),
      ),
      onTap: () {
        context.push('/media/${item.id}');
      },
      title: Text(item.fileName, maxLines: 1, overflow: TextOverflow.ellipsis),
      subtitle: Text(formatter.format(item.recordedAt)),
    );
  }

  Widget _buildPaginationBar(PaginatedMediaList data) {
    final totalPages = data.totalPages < 1 ? 1 : data.totalPages;
    final hasPrev = currentPage > 0;
    final hasNext = currentPage < totalPages - 1;

    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 16, horizontal: 8),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          IconButton(
            tooltip: 'Pierwsza strona',
            onPressed: hasPrev ? () => _goToPage(0) : null,
            icon: const Icon(Icons.first_page),
          ),
          IconButton(
            tooltip: 'Poprzednia strona',
            onPressed: hasPrev ? () => _goToPage(currentPage - 1) : null,
            icon: const Icon(Icons.chevron_left),
          ),
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 12),
            child: Text('Strona ${currentPage + 1} z $totalPages'),
          ),
          IconButton(
            tooltip: 'Następna strona',
            onPressed: hasNext ? () => _goToPage(currentPage + 1) : null,
            icon: const Icon(Icons.chevron_right),
          ),
          IconButton(
            tooltip: 'Ostatnia strona',
            onPressed: hasNext ? () => _goToPage(totalPages - 1) : null,
            icon: const Icon(Icons.last_page),
          ),
        ],
      ),
    );
  }
}
