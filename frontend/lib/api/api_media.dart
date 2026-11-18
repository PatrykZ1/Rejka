import 'package:boundingbox/api/models/response/media_item_response.dart';
import 'package:boundingbox/api/models/response/paginated_media_response.dart';
import 'package:boundingbox/api/models/response/registration_rating_response.dart';
import 'package:boundingbox/models/media_filter.dart';

class ApiMedia {
  // Mock instance used for tests only
  static ApiMedia? mockInstance;

  // Factory constructor for mock tests
  factory ApiMedia() {
    return mockInstance ?? ApiMedia._internal();
  }
  ApiMedia._internal();

  //final ApiCore _api = ApiCore();

  static Map<int, MediaItemResponse> medias = {
    0: MediaItemResponse(
      id: 0,
      userId: 0,
      url:
          'https://flutter.github.io/assets-for-api-docs/assets/videos/bee.mp4',
      fileName: 'pszczolka.mp4',
      latitude: 51,
      longitude: 32,
      recordedAt: DateTime.now(),
      uploadedAt: DateTime.now().subtract(Duration(days: 3)),
      comment:
          'Pszczółka Maja sobie lata (ooo)\nZbiera nektar gdzieś na kwiatach\nA Tam Gucio w tulipanie\nCzeka sobie na śniadanie',
      registrations: ['BIA4298'],
      ratings: [RegistrationRatingResponse(registration: 'BIA4298', rating: 9)],
    ),

    1: MediaItemResponse(
      id: 1,
      userId: 0,
      url:
          'https://a.allegroimg.com/s720/110df3/d3ba2a364210b098bfb06c2a744f/Obraz-na-plotnie-Samochod-BMW-3-M3-E30-E-30-320-318-325-316-Auto-Grafika',
      fileName: 'super-fajne-auto.jpg',
      latitude: 21,
      longitude: 37,
      recordedAt: DateTime.now(),
      uploadedAt: DateTime.now().subtract(Duration(days: 2)),
      comment:
          'Spod tamtego drzewa holowałem Renatkę w 96. aż do Suchowoli! To były czasy. A teraz co mamy? Pizzę Książula i kaucję.',
      registrations: ['WX456CJ', 'XD2137G', 'BI170EG'],
      ratings: [
        RegistrationRatingResponse(registration: 'BI170EG', rating: 5),
        RegistrationRatingResponse(registration: 'XD2137G', rating: 10),
      ],
    ),
  };

  Future<MediaItemResponse?> getMediaItem(int id) async {
    await Future.delayed(const Duration(milliseconds: 500));
    return medias[id];
  }

  Future<PaginatedMediaResponse?> getPagedMedia({
    required int page,
    required int pageSize,
    required MediaFilter filter,
  }) async {
    await Future.delayed(const Duration(milliseconds: 1000));
    final list = medias.entries.map((entry) => entry.value).toList();
    final response = PaginatedMediaResponse(
      items: list,
      totalCount: list.length,
      currentPage: 0,
      totalPages: 1,
    );
    return response;
  }
}
