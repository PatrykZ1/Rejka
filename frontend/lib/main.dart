import 'package:flutter/material.dart';
import 'package:intl/date_symbol_data_local.dart';
import 'app/app.dart';

void main() {
  WidgetsFlutterBinding.ensureInitialized();
  initializeDateFormatting(
    'pl_PL',
    null,
  ).then((_) => runApp(const BoundingBoxApp()));
}
