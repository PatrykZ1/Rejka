import 'package:json_annotation/json_annotation.dart';

part 'location_point_response.g.dart';

@JsonSerializable()
class LocationPointResponse {
  final double latitude;
  final double longitude;

  const LocationPointResponse({
    required this.latitude,
    required this.longitude,
  });

  factory LocationPointResponse.fromJson(Map<String, dynamic> json) =>
      _$LocationPointResponseFromJson(json);
  Map<String, dynamic> toJson() => _$LocationPointResponseToJson(this);
}
