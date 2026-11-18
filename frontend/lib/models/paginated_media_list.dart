import 'package:boundingbox/api/models/response/paginated_media_response.dart';

import 'media_item.dart';

class PaginatedMediaList {
  final List<MediaItem> items;
  final int totalCount;
  final int currentPage;
  final int totalPages;

  PaginatedMediaList({
    required this.items,
    required this.totalCount,
    required this.currentPage,
    required this.totalPages,
  });
  PaginatedMediaList.fromResponse(PaginatedMediaResponse response)
    : items = response.items.map((e) => MediaItem.fromResponse(e)).toList(),
      totalCount = response.totalCount,
      currentPage = response.currentPage,
      totalPages = response.totalPages;
}
