import 'package:boundingbox/api/models/response/location_point_response.dart';
import 'package:boundingbox/api/models/response/media_item_response.dart';
import 'package:boundingbox/api/models/response/registration_rating_response.dart';
import 'package:json_annotation/json_annotation.dart';

part 'registration_item_response.g.dart';

@JsonSerializable()
class RegistrationItemResponse {
  final String number;
  final List<MediaItemResponse> videos;
  final List<LocationPointResponse> locations;
  final List<DateTime> recordedAt;
  final List<RegistrationRatingResponse> ratings;

  const RegistrationItemResponse({
    required this.number,
    required this.videos,
    required this.locations,
    required this.recordedAt,
    required this.ratings,
  });

  factory RegistrationItemResponse.fromJson(Map<String, dynamic> json) =>
      _$RegistrationItemResponseFromJson(json);
  Map<String, dynamic> toJson() => _$RegistrationItemResponseToJson(this);
}
