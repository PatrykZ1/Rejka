
import 'package:boundingbox/api/api_core.dart';
import 'package:boundingbox/app/navigation_service.dart';
import 'package:boundingbox/screens/login_screen.dart';
import 'package:boundingbox/screens/register_screen.dart';
import 'package:go_router/go_router.dart';

import '../screens/home_screen.dart';
import '../screens/detector_screen.dart';

final GoRouter router = GoRouter(
  navigatorKey: NavigationService.navigatorKey,
  initialLocation: '/',
  redirect: (context, state) {
    print('token: ${ApiCore.tokenValid}');
    if (!ApiCore.tokenValid && state.matchedLocation != '/register') {
      return '/login';
    }
    if (ApiCore.tokenValid && state.matchedLocation == '/login') {
      return '/';
    }
    return null;
  },
  routes: [
    GoRoute(
      path: '/',
      builder: (context, state) => const HomeScreen(),
    ),
    GoRoute(path: '/register',
      builder: (context, state) => const RegisterScreen(),
    ),
    GoRoute(path: '/login',
      builder: (context, state) => const LoginScreen(),
    ),
    GoRoute(
      path: '/detector',
      builder: (context, state) => const DetectorScreen(),
    ),
  ],
);
