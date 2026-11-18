import 'dart:io';

import 'package:boundingbox/api/api_core.dart';
import 'package:boundingbox/api/models/response/media_item_response.dart';
import 'package:boundingbox/api/models/response/registration_rating_response.dart';

class ApiMedia {
  // Mock instance used for tests only
  static ApiMedia? mockInstance;

  // Factory constructor for mock tests
  factory ApiMedia() {
    return mockInstance ?? ApiMedia._internal();
  }
  ApiMedia._internal();

  final ApiCore _api = ApiCore();

  Future<MediaItemResponse> getMediaItem(int id) async {
    await Future.delayed(const Duration(milliseconds: 500));
    return MediaItemResponse(
      id: id,
      userId: 0,
      url: 'asdf',
      fileName: 'asdf',
      latitude: 21,
      longitude: 37,
      recordedAt: DateTime.now(),
      uploadedAt: DateTime.now().subtract(Duration(days: 2)),
      comment: 'asdf',
      registrations: ['asdf', 'qwer', 'zxcv'],
      ratings: [
        RegistrationRatingResponse(registration: 'asdf', rating: 5),
        RegistrationRatingResponse(registration: 'qwer', rating: 9),
      ],
    );
  }
}
