import 'package:boundingbox/api/api_registration.dart';
import 'package:boundingbox/constants.dart';
import 'package:boundingbox/models/paginated_registration_list.dart';
import 'package:boundingbox/models/registration_filter.dart';
import 'package:boundingbox/models/registration_item.dart';
import 'package:boundingbox/models/registration_rating.dart';
import 'package:boundingbox/widgets/drawer.dart';
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

class RegistrationListScreen extends StatefulWidget {
  const RegistrationListScreen({super.key});

  @override
  State<RegistrationListScreen> createState() => _RegistrationListScreenState();
}

class _RegistrationListScreenState extends State<RegistrationListScreen> {
  late Future<PaginatedRegistrationList?> _future;

  int currentPage = 0;
  RegistrationFilter filter = RegistrationFilter();

  @override
  void initState() {
    super.initState();
    _future = _fetch();
  }

  Future<PaginatedRegistrationList?> _fetch() async {
    final response = await ApiRegistration().getPagedRegistrations(
      page: currentPage,
      pageSize: 10,
      filter: filter,
    );
    if (response == null) return null;
    return PaginatedRegistrationList.fromResponse(response);
  }

  void _goToPage(int page) {
    setState(() {
      currentPage = page;
      _future = _fetch();
    });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text(applicationName)),
      drawer: AppDrawer(),
      body: FutureBuilder<PaginatedRegistrationList?>(
        future: _future,
        builder: (context, snapshot) {
          if (snapshot.connectionState == ConnectionState.waiting) {
            return const Center(child: CircularProgressIndicator());
          }

          if (snapshot.hasError || !snapshot.hasData) {
            return Center(
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  const Icon(Icons.error_outline, size: 48, color: Colors.red),
                  const SizedBox(height: 12),
                  const Text('Błąd przy wczytywaniu listy rejestracji.'),
                  const SizedBox(height: 12),
                  ElevatedButton(
                    onPressed: () => setState(() => _future = _fetch()),
                    child: const Text('Spróbuj ponownie'),
                  ),
                ],
              ),
            );
          }

          return _buildList(snapshot.data!);
        },
      ),
    );
  }

  Widget _buildList(PaginatedRegistrationList list) {
    final itemCount = list.items.length + 2;

    return ListView.builder(
      itemCount: itemCount,
      itemBuilder: (context, index) {
        if (index == 0) {
          return Padding(
            padding: const EdgeInsets.all(12),
            child: Align(
              alignment: Alignment.centerLeft,
              child: OutlinedButton.icon(
                onPressed: _showFilterDialog,
                icon: const Icon(Icons.filter_list),
                label: const Text('Filtry'),
              ),
            ),
          );
        }

        if (index == itemCount - 1) return _buildPaginationBar(list);

        return _buildTile(list.items[index - 1]);
      },
    );
  }

  void _showFilterDialog() {
    showDialog(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Filtry'),
        content: const Text(''),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(context).pop(),
            child: const Text('Gotowe'),
          ),
        ],
      ),
    );
  }

  Widget _buildTile(RegistrationItem item) {
    return ListTile(
      contentPadding: EdgeInsets.zero,
      onTap: () {
        context.push('/registration/${item.number}');
      },
      title: Text(item.number),
      trailing: item.ratings.isNotEmpty
          ? Chip(
        avatar: const Icon(Icons.star, color: Colors.amber, size: 18),
        label: Text(
          (item.ratings
              .reduce(
                (a, b) => RegistrationRating(
              registration: a.registration,
              rating: a.rating + b.rating,
            ),
          )
              .rating /
              item.ratings.length.toDouble())
              .toStringAsFixed(1),
        ),
      )
          : const Text('Brak oceny.', style: TextStyle(color: Colors.grey)),
    );

    return ListTile(
      leading: Text(item.number, style: TextStyle(fontSize: 23)),
      onTap: () {
        context.push('/registration/${item.number}');
      },
      trailing: item.ratings.isNotEmpty
          ? Chip(
              avatar: const Icon(Icons.star, color: Colors.amber, size: 18),
              label: Text(
                (item.ratings
                            .reduce(
                              (a, b) => RegistrationRating(
                                registration: a.registration,
                                rating: a.rating + b.rating,
                              ),
                            )
                            .rating /
                        item.ratings.length.toDouble())
                    .toStringAsFixed(1),
              ),
            )
          : const Text('Brak oceny.', style: TextStyle(color: Colors.grey)),
    );
  }

  Widget _buildPaginationBar(PaginatedRegistrationList data) {
    final totalPages = data.totalPages < 1 ? 1 : data.totalPages;
    final hasPrev = currentPage > 0;
    final hasNext = currentPage < totalPages - 1;

    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 16, horizontal: 8),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          IconButton(
            tooltip: 'Pierwsza strona',
            onPressed: hasPrev ? () => _goToPage(0) : null,
            icon: const Icon(Icons.first_page),
          ),
          IconButton(
            tooltip: 'Poprzednia strona',
            onPressed: hasPrev ? () => _goToPage(currentPage - 1) : null,
            icon: const Icon(Icons.chevron_left),
          ),
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 12),
            child: Text('Strona ${currentPage + 1} z $totalPages'),
          ),
          IconButton(
            tooltip: 'Następna strona',
            onPressed: hasNext ? () => _goToPage(currentPage + 1) : null,
            icon: const Icon(Icons.chevron_right),
          ),
          IconButton(
            tooltip: 'Ostatnia strona',
            onPressed: hasNext ? () => _goToPage(totalPages - 1) : null,
            icon: const Icon(Icons.last_page),
          ),
        ],
      ),
    );
  }
}
