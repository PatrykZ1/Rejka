import 'package:boundingbox/api/models/response/registration_item_response.dart';
import 'package:json_annotation/json_annotation.dart';

part 'paginated_registration_response.g.dart';

@JsonSerializable()
class PaginatedRegistrationResponse {
  final List<RegistrationItemResponse> items;
  final int totalCount;
  final int currentPage;
  final int totalPages;

  const PaginatedRegistrationResponse({
    required this.items,
    required this.totalCount,
    required this.currentPage,
    required this.totalPages,
  });

  factory PaginatedRegistrationResponse.fromJson(Map<String, dynamic> json) =>
      _$PaginatedRegistrationResponseFromJson(json);
  Map<String, dynamic> toJson() => _$PaginatedRegistrationResponseToJson(this);
}
