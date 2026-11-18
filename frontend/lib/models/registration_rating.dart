import 'package:boundingbox/api/models/response/registration_rating_response.dart';

class RegistrationRating {
  final String registration;
  final int rating;

  RegistrationRating({required this.registration, required this.rating});
  RegistrationRating.fromResponse(RegistrationRatingResponse response)
    : registration = response.registration,
      rating = response.rating;
}
