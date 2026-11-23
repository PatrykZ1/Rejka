import 'package:boundingbox/api/models/response/registration_item_response.dart';
import 'package:boundingbox/api/models/response/registration_rating_response.dart';
import 'package:boundingbox/models/location_point.dart';
import 'package:boundingbox/models/media_item.dart';
import 'package:boundingbox/models/registration_rating.dart';

class RegistrationItem {
  final String number;
  final List<MediaItem> videos;
  final List<LocationPoint> locations;
  final List<DateTime> recordedAt;
  final List<RegistrationRating> ratings;

  const RegistrationItem({
    required this.number,
    required this.videos,
    required this.locations,
    required this.recordedAt,
    required this.ratings,
  });
  RegistrationItem.fromResponse(RegistrationItemResponse response)
    : number = response.number,
      videos = response.videos.map((e) => MediaItem.fromResponse(e)).toList(),
      locations = response.locations
          .map((e) => LocationPoint.fromResponse(e))
          .toList(),
      recordedAt = response.recordedAt,
      ratings = response.ratings
          .map((e) => RegistrationRating.fromResponse(e))
          .toList();
}
