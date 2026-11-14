import 'package:flutter/material.dart';

import '../router/router.dart';
import 'theme.dart';

class BoundingBoxApp extends StatelessWidget {
  const BoundingBoxApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp.router(
      debugShowCheckedModeBanner: false,
      title: 'Rejka',
      theme: AppTheme.dark,
      routerConfig: router,
    );
  }
}
