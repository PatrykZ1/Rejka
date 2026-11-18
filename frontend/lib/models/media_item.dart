import 'package:boundingbox/api/models/response/media_item_response.dart';
import 'package:boundingbox/models/registration_rating.dart';

class MediaItem {
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
  final List<RegistrationRating> ratings;

  MediaItem({
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
  MediaItem.fromResponse(MediaItemResponse response)
    : id = response.id,
      userId = response.userId,
      url = response.url,
      fileName = response.fileName,
      latitude = response.latitude,
      longitude = response.longitude,
      recordedAt = response.recordedAt,
      uploadedAt = response.uploadedAt,
      comment = response.comment,
      registrations = response.registrations,
      ratings = response.ratings
          .map((r) => RegistrationRating.fromResponse(r))
          .toList();
}
