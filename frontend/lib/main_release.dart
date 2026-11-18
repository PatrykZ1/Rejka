import 'package:boundingbox/constants.dart';
import 'package:flutter/material.dart';
import 'package:intl/date_symbol_data_local.dart';
import 'app/app.dart';

void main() {
  backendUrl = 'https://rejka-backend.proudwater-f47bc384.polandcentral.azurecontainerapps.io';
  WidgetsFlutterBinding.ensureInitialized();
  initializeDateFormatting(
    'pl_PL',
    null,
  ).then((_) => runApp(const BoundingBoxApp()));
}
