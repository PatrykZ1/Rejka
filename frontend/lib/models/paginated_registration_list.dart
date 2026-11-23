import 'package:boundingbox/api/models/response/paginated_registration_response.dart';
import 'package:boundingbox/models/registration_item.dart';

class PaginatedRegistrationList {
  final List<RegistrationItem> items;
  final int totalCount;
  final int currentPage;
  final int totalPages;

  const PaginatedRegistrationList({
    required this.items,
    required this.totalCount,
    required this.currentPage,
    required this.totalPages,
  });
  PaginatedRegistrationList.fromResponse(PaginatedRegistrationResponse response)
    : items = response.items
          .map((e) => RegistrationItem.fromResponse(e))
          .toList(),
      totalCount = response.totalCount,
      currentPage = response.currentPage,
      totalPages = response.totalPages;
}
