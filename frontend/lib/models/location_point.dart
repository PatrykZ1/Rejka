import 'package:boundingbox/api/models/response/location_point_response.dart';

class LocationPoint {
  final double latitude;
  final double longitude;

  const LocationPoint({required this.latitude, required this.longitude});
  LocationPoint.fromResponse(LocationPointResponse response)
    : latitude = response.latitude,
      longitude = response.longitude;
}
