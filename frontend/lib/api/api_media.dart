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
    2: MediaItemResponse(
      id: 2,
      userId: 0,
      url: 'https://wallpapercave.com/wp/wp4695480.jpg',
      fileName: 'koenigsegg-jesko.jpg',
      latitude: 63,
      longitude: 16,
      recordedAt: DateTime.now().subtract(Duration(days: 10)),
      uploadedAt: DateTime.now(),
      comment: 'Jesko Absolut hiperauto :)',
      registrations: ['WY7655F'],
      ratings: [
        RegistrationRatingResponse(registration: 'WY7655F', rating: 10),
      ],
    ),
    3: MediaItemResponse(
      id: 3,
      userId: 0,
      url:
          'https://www.segond-automobiles.com/wp-content/uploads/2024/11/Bugatti-Veyron-Grand-Sport-Sang-Blanc-1.jpg',
      fileName: 'bugatti-veyron.jpg',
      latitude: 41,
      longitude: 17,
      recordedAt: DateTime.now(),
      uploadedAt: DateTime.now().subtract(Duration(days: 2)),
      comment: 'Fajne bugatti. Polecam.',
      registrations: ['PO45678'],
      ratings: [RegistrationRatingResponse(registration: 'PO45678', rating: 8)],
    ),
    4: MediaItemResponse(
      id: 4,
      userId: 0,
      url: 'https://wallpapercave.com/wp/wp10175467.jpg',
      fileName: 'hyundai-i30.jpg',
      latitude: 36,
      longitude: 127,
      recordedAt: DateTime.now(),
      uploadedAt: DateTime.now().subtract(Duration(days: 2)),
      comment: 'Hyundai i30, taki jakim nagrano dane treningowe.',
      registrations: ['BI2077X'],
      ratings: [RegistrationRatingResponse(registration: 'BI2077X', rating: 9)],
    ),
    5: MediaItemResponse(
      id: 5,
      userId: 0,
      url: 'https://wallpapercave.com/wp/wp13540305.jpg',
      fileName: 'hyundai-i20N.jpg',
      latitude: 35,
      longitude: 127,
      recordedAt: DateTime.now(),
      uploadedAt: DateTime.now().subtract(Duration(days: 2)),
      comment: 'Wersja i20 zrobiona pod eurobeat.',
      registrations: ['WX6958H'],
      ratings: [],
    ),
    6: MediaItemResponse(
      id: 6,
      userId: 0,
      url:
          'https://cdn.galleries.smcloud.net/thumbs/gf-DnR9-Nzuz-KyEn_autobus-12-j.webp',
      fileName: 'autobus.jpg',
      latitude: 52,
      longitude: 21,
      recordedAt: DateTime.now(),
      uploadedAt: DateTime.now().subtract(Duration(days: 2)),
      comment: 'Autobus na tle warszawskiego centrum.',
      registrations: ['WX79765', 'WE512DX', 'WA99919'],
      ratings: [
        RegistrationRatingResponse(registration: 'WX79765', rating: 5),
        RegistrationRatingResponse(registration: 'WA99919', rating: 3),
      ],
    ),
    7: MediaItemResponse(
      id: 7,
      userId: 0,
      url:
          'https://external-content.duckduckgo.com/iu/?u=https%3A%2F%2Fi.ytimg.com%2Fvi%2FLqQ2QvX-KIs%2Fmaxresdefault.jpg&f=1&nofb=1&ipt=1052b4d968c2b67794c74bccbab4886c8bfa0d1e2437434a32e0d29922a4771f&ipo=images',
      fileName: 'samochody.jpg',
      latitude: 51,
      longitude: 19,
      recordedAt: DateTime.now(),
      uploadedAt: DateTime.now().subtract(Duration(days: 2)),
      comment: 'Samochody na ulicy.',
      registrations: ['SZO45T5', 'SK753LU'],
      ratings: [RegistrationRatingResponse(registration: 'SZO45T5', rating: 7)],
    ),
    8: MediaItemResponse(
      id: 8,
      userId: 0,
      url: 'https://wallpapercave.com/wp/wp2425363.jpg',
      fileName: 'pagani-zonda.jpg',
      latitude: 45,
      longitude: 10,
      recordedAt: DateTime.now(),
      uploadedAt: DateTime.now().subtract(Duration(days: 2)),
      comment: 'Eleganckie włoskie Pagani Zonda.',
      registrations: ['E0ZONDA'],
      ratings: [RegistrationRatingResponse(registration: 'E0ZONDA', rating: 7)],
    ),
    9: MediaItemResponse(
      id: 9,
      userId: 0,
      url: 'https://wallpapercave.com/wp/wp2425380.jpg',
      fileName: 'mclaren.jpg',
      latitude: 37,
      longitude: -110,
      recordedAt: DateTime.now(),
      uploadedAt: DateTime.now().subtract(Duration(days: 2)),
      comment: 'Amerykańska maszyna wolna niczym orzeł.',
      registrations: ['TX001MC'],
      ratings: [RegistrationRatingResponse(registration: 'TX001MC', rating: 6)],
    ),
    10: MediaItemResponse(
      id: 10,
      userId: 0,
      url: 'https://wallpapercave.com/wp/wp2425387.jpg',
      fileName: 'lambo.jpg',
      latitude: 37,
      longitude: -110,
      recordedAt: DateTime.now(),
      uploadedAt: DateTime.now().subtract(Duration(days: 2)),
      comment: 'Żółte Lamborgini.',
      registrations: ['WI86767'],
      ratings: [RegistrationRatingResponse(registration: 'WI86767', rating: 8)],
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
