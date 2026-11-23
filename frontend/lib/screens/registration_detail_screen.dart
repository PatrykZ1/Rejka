import 'package:boundingbox/api/api_registration.dart';
import 'package:boundingbox/constants.dart';
import 'package:boundingbox/models/location_point.dart';
import 'package:boundingbox/models/media_item.dart';
import 'package:boundingbox/models/registration_item.dart';
import 'package:boundingbox/models/registration_rating.dart';
import 'package:boundingbox/widgets/media_thumbnail.dart';
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';

class RegistrationDetailScreen extends StatefulWidget {
  final String registrationNumber;

  const RegistrationDetailScreen({super.key, required this.registrationNumber});

  @override
  State<RegistrationDetailScreen> createState() =>
      _RegistrationDetailScreenState();
}

class _RegistrationDetailScreenState extends State<RegistrationDetailScreen> {
  late Future<RegistrationItem?> _future;

  @override
  void initState() {
    super.initState();
    _future = _fetch();
  }

  Future<RegistrationItem?> _fetch() async {
    final response = await ApiRegistration().getRegistrationItem(
      widget.registrationNumber,
    );
    if (response == null) return null;
    return RegistrationItem.fromResponse(response);
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text(applicationName)),
      body: FutureBuilder<RegistrationItem?>(
        future: _future,
        builder: (context, snapshot) {
          if (snapshot.connectionState == ConnectionState.waiting) {
            return const Center(child: CircularProgressIndicator());
          }

          if (snapshot.hasError) {
            return Center(
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  const Icon(Icons.error_outline, size: 48, color: Colors.red),
                  const SizedBox(height: 12),
                  const Text('Błąd przy wczytywaniu danych.'),
                  const SizedBox(height: 12),
                  ElevatedButton(
                    onPressed: () => setState(() => _future = _fetch()),
                    child: const Text('Spróbuj ponownie'),
                  ),
                ],
              ),
            );
          }

          if (snapshot.data == null) {
            return const Center(
              child: Text('Szukana rejestracja nie istnieje.'),
            );
          }

          return _buildDetails(snapshot.data!);
        },
      ),
    );
  }

  Widget _buildDetails(RegistrationItem item) {
    final titleStyle = Theme.of(context).textTheme.titleLarge;
    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        _buildPlate(item.number),
        const SizedBox(height: 24),
        Text('Nagrania', style: titleStyle),
        const SizedBox(height: 8),
        if (item.videos.isEmpty) const Text('Brak nagrań.'),
        ...item.videos.map(_buildMediaTile),
        const SizedBox(height: 24),
        Text('Oceny', style: titleStyle),
        const SizedBox(height: 8),
        if (item.ratings.isEmpty) const Text('Brak ocen.'),
        ...item.ratings.map(_buildRatingTile),
        const SizedBox(height: 24),
        Text('Lokalizacje', style: titleStyle),
        const SizedBox(height: 8),
        if (item.locations.isEmpty) const Text('Brak lokalizacji.'),
        for (var i = 0; i < item.locations.length; i++)
          _buildLocationTile(
            item.locations[i],
            i < item.recordedAt.length ? item.recordedAt[i] : null,
          ),
      ],
    );
  }

  // Polska tablica rejestracyjna
  Widget _buildPlate(String number) {
    return Center(
      child: Container(
        height: 110,
        constraints: const BoxConstraints(maxWidth: 520),
        clipBehavior: Clip.antiAlias,
        decoration: BoxDecoration(
          color: Colors.white,
          border: Border.all(color: Colors.black, width: 3),
          borderRadius: BorderRadius.circular(10),
        ),
        child: Row(
          children: [
            Container(
              width: 44,
              color: const Color(0xFF003399),
              child: const Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Icon(Icons.star, color: Colors.yellow, size: 16),
                  Text(
                    'PL',
                    style: TextStyle(
                      color: Colors.white,
                      fontWeight: FontWeight.bold,
                      fontSize: 18,
                    ),
                  ),
                ],
              ),
            ),
            Expanded(
              child: Padding(
                padding: const EdgeInsets.symmetric(horizontal: 12),
                child: FittedBox(
                  child: Text(
                    number,
                    style: const TextStyle(
                      color: Colors.black,
                      fontSize: 72,
                      fontWeight: FontWeight.bold,
                      letterSpacing: 4,
                    ),
                  ),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildMediaTile(MediaItem item) {
    final formatter = DateFormat.yMMMMd('pl_PL').add_Hms();
    return ListTile(
      contentPadding: EdgeInsets.zero,
      leading: SizedBox(
        width: 64,
        height: 64,
        child: ClipRRect(
          borderRadius: BorderRadius.circular(8),
          child: MediaThumbnail(url: item.url, fileName: item.fileName),
        ),
      ),
      onTap: () => context.push('/media/${item.id}'),
      title: Text(item.fileName, maxLines: 1, overflow: TextOverflow.ellipsis),
      subtitle: Text(formatter.format(item.recordedAt)),
    );
  }

  Widget _buildRatingTile(RegistrationRating r) {
    return ListTile(
      contentPadding: EdgeInsets.zero,
      leading: const Icon(Icons.star, color: Colors.amber),
      title: Text('Ocena: ${r.rating}'),
    );
  }

  Widget _buildLocationTile(LocationPoint loc, DateTime? recordedAt) {
    final formatter = DateFormat.yMMMMd('pl_PL').add_Hms();
    return ListTile(
      contentPadding: EdgeInsets.zero,
      leading: const Icon(Icons.location_on),
      title: Text('${loc.latitude}°, ${loc.longitude}°'),
      subtitle: recordedAt == null ? null : Text(formatter.format(recordedAt)),
    );
  }
}
