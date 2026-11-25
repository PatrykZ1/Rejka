import 'package:boundingbox/api/api_media.dart';
import 'package:boundingbox/api/models/request/media_upload_request.dart';
import 'package:file_picker/file_picker.dart';
import 'package:flutter/material.dart';

class MediaUploadScreen extends StatefulWidget {
  const MediaUploadScreen({super.key});

  @override
  State<MediaUploadScreen> createState() => _MediaUploadScreenState();
}

class _MediaUploadScreenState extends State<MediaUploadScreen> {
  final _comment = TextEditingController();
  String? _path;
  bool _loading = false;

  void _msg(String text) {
    if (!mounted) return;
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(text)));
  }

  Future<void> _pick() async {
    final result = await FilePicker.pickFile(type: FileType.media);
    if (result != null) setState(() => _path = result.path);
  }

  Future<void> _send() async {
    setState(() => _loading = true);
    try {
      await ApiMedia().mediaUpload(
        MediaUploadRequest(
          filePath: _path!,
          recordedAt: DateTime.now(),
          comment: _comment.text.isEmpty ? null : _comment.text,
        ),
      );
      _msg('Wysłano!');
    } catch (e) {
      _msg('Błąd wysyłania: $e');
    }
    if (mounted) setState(() => _loading = false);
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Wgraj plik')),
      body: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          children: [
            ElevatedButton(onPressed: _pick, child: const Text('Wybierz plik')),
            Text(_path ?? 'Nie wybrano pliku'),
            TextField(
              controller: _comment,
              decoration: const InputDecoration(labelText: 'Komentarz'),
            ),
            const SizedBox(height: 16),
            _loading
                ? const CircularProgressIndicator()
                : ElevatedButton(
              onPressed: _path == null ? null : _send,
              child: const Text('Wyślij'),
            ),
          ],
        ),
      ),
    );
  }
}