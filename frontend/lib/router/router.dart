
import 'package:boundingbox/screens/register_screen.dart';
import 'package:go_router/go_router.dart';

import '../screens/home_screen.dart';
import '../screens/detector_screen.dart';

final GoRouter router = GoRouter(
  initialLocation: '/register',
  routes: [
    GoRoute(
      path: '/',
      builder: (context, state) => const HomeScreen(),
    ),
    GoRoute(path: '/register',
      builder: (context, state) => const RegisterScreen(),
    ),
    GoRoute(
      path: '/detector',
      builder: (context, state) => const DetectorScreen(),
    ),
  ],
);
