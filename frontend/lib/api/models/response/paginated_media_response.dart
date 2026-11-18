import 'package:boundingbox/api/models/response/media_item_response.dart';
import 'package:json_annotation/json_annotation.dart';

part 'paginated_media_response.g.dart';

@JsonSerializable()
class PaginatedMediaResponse {
  final List<MediaItemResponse> items;
  final int totalCount;
  final int currentPage;
  final int totalPages;

  PaginatedMediaResponse({
    required this.items,
    required this.totalCount,
    required this.currentPage,
    required this.totalPages,
  });

  factory PaginatedMediaResponse.fromJson(Map<String, dynamic> json) =>
      _$PaginatedMediaResponseFromJson(json);
  Map<String, dynamic> toJson() => _$PaginatedMediaResponseToJson(this);
}
