import 'package:boundingbox/api/api_core.dart';
import 'package:boundingbox/app/navigation_service.dart';
import 'package:boundingbox/screens/blank_screen.dart';
import 'package:boundingbox/screens/login_screen.dart';
import 'package:boundingbox/screens/media_detail_screen.dart';
import 'package:boundingbox/screens/media_list_screen.dart';
import 'package:boundingbox/screens/register_screen.dart';
import 'package:go_router/go_router.dart';

import '../screens/home_screen.dart';
import '../screens/detector_screen.dart';

final GoRouter router = GoRouter(
  navigatorKey: NavigationService.navigatorKey,
  initialLocation: '/login',
  redirect: (context, state) async {
    bool isToken = await ApiCore().isToken();
    if (!isToken && state.matchedLocation != '/register') {
      return '/login';
    }
    if (isToken && state.matchedLocation == '/login') {
      return '/';
    }
    return null;
  },
  errorBuilder: (context, state) {
    return BlankScreen(message: 'Strona ${state.path} jest błędna.');
  },
  routes: [
    GoRoute(path: '/', builder: (context, state) => const HomeScreen()),
    GoRoute(
      path: '/register',
      builder: (context, state) => const RegisterScreen(),
    ),
    GoRoute(path: '/login', builder: (context, state) => const LoginScreen()),
    GoRoute(
      path: '/detector',
      builder: (context, state) => const DetectorScreen(),
    ),
    GoRoute(
      path: '/media',
      builder: (context, state) => const MediaListScreen(),
    ),
    GoRoute(
      path: '/media/:mediaId',
      builder: (context, state) => MediaDetailScreen(
        mediaId: int.parse(state.pathParameters['mediaId']!),
      ),
    ),
  ],
);
