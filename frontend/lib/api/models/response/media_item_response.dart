import 'package:boundingbox/api/models/response/registration_rating_response.dart';
import 'package:json_annotation/json_annotation.dart';

part 'media_item_response.g.dart';

@JsonSerializable()
class MediaItemResponse {
  final int id;
  final int userId;
  final String url;
  final String fileName;
  final double latitude;
  final double longitude;
  final DateTime recordedAt;
  final DateTime uploadedAt;
  final String comment;
  final List<String> registrations;
  final List<RegistrationRatingResponse> ratings;

  MediaItemResponse({
    required this.id,
    required this.userId,
    required this.url,
    required this.fileName,
    required this.latitude,
    required this.longitude,
    required this.recordedAt,
    required this.uploadedAt,
    required this.comment,
    required this.registrations,
    required this.ratings,
  });

  factory MediaItemResponse.fromJson(Map<String, dynamic> json) =>
      _$MediaItemResponseFromJson(json);
  Map<String, dynamic> toJson() => _$MediaItemResponseToJson(this);
}
