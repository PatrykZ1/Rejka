import 'package:json_annotation/json_annotation.dart';

part 'registration_rating_response.g.dart';

@JsonSerializable()
class RegistrationRatingResponse {
  final String registration;
  final int rating;

  RegistrationRatingResponse({
    required this.registration,
    required this.rating,
  });

  factory RegistrationRatingResponse.fromJson(Map<String, dynamic> json) =>
      _$RegistrationRatingResponseFromJson(json);
  Map<String, dynamic> toJson() => _$RegistrationRatingResponseToJson(this);
}
