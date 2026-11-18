import 'package:boundingbox/api/api_account.dart';
import 'package:boundingbox/api/api_core.dart';
import 'package:boundingbox/api/models/response/login_response.dart';
import 'package:boundingbox/constants.dart';
import 'package:boundingbox/router/router.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';

class MockApiAccount extends Mock implements ApiAccount {}

void main() {
  late MockApiAccount mockApiAccount;
  final Map<String, String> fakeSecureStorage = {};

  setUp(() {
    TestWidgetsFlutterBinding.ensureInitialized();
    mockApiAccount = MockApiAccount();
    ApiAccount.mockInstance = mockApiAccount;

    const MethodChannel channel = MethodChannel(
      'plugins.it_nomads.com/flutter_secure_storage',
    );
    TestDefaultBinaryMessengerBinding.instance.defaultBinaryMessenger
        .setMockMethodCallHandler(channel, (MethodCall methodCall) async {
          final Map? args = methodCall.arguments as Map?;
          switch (methodCall.method) {
            case 'write':
              fakeSecureStorage[args?['key']] = args?['value'];
              return null;
            case 'read':
              return fakeSecureStorage[args?['key']];
            case 'delete':
              fakeSecureStorage.remove(args?['key']);
              return null;
            default:
              return null;
          }
        });
    fakeSecureStorage.clear();
  });

  tearDown(() {
    ApiAccount.mockInstance = null;
  });

  Widget createTestWidget() {
    return MaterialApp.router(routerConfig: router);
  }

  testWidgets('Login screen displays correctly and validates empty fields', (
    WidgetTester tester,
  ) async {
    await tester.pumpWidget(createTestWidget());

    router.go('/login');
    await tester.pumpAndSettle();

    expect(find.text('Zaloguj się'), findsOneWidget);
    expect(find.byType(TextFormField), findsNWidgets(2));

    await tester.tap(find.byType(ElevatedButton));
    await tester.pump();

    expect(find.text('Wpisz adres email'), findsOneWidget);
    expect(find.text('Wpisz hasło'), findsOneWidget);
  });

  testWidgets(
    'Screen change when no refresh token is saved results in redirection to login screen',
    (WidgetTester tester) async {
      await tester.pumpWidget(createTestWidget());

      router.go('/login');
      await tester.pumpAndSettle();

      router.go('/');
      await tester.pumpAndSettle();

      expect(find.text('Zaloguj się'), findsOneWidget);
    },
  );

  testWidgets(
    'Register screen is available (no automatic redirection to login screen)',
    (WidgetTester tester) async {
      await tester.pumpWidget(createTestWidget());

      router.go('/login');
      await tester.pumpAndSettle();

      router.go('/register');
      await tester.pumpAndSettle();

      expect(find.text('Załóż konto'), findsOneWidget);
    },
  );

  testWidgets(
    'Login screen automatically redirects to home screen if token available',
    (WidgetTester tester) async {
      await ApiCore().saveRefreshToken('mock_refresh_token');
      await tester.pumpWidget(createTestWidget());

      router.go('/login');
      await tester.pumpAndSettle();

      expect(find.text(applicationName), findsExactly(2));
    },
  );

  testWidgets('Login screen successful login navigates to home', (
    WidgetTester tester,
  ) async {
    when(
      () => mockApiAccount.login(
        email: 'tadeusz.norek@mpwik.com.pl',
        password: 'k0cham_k@naly',
      ),
    ).thenAnswer((_) async {
      await ApiCore().saveRefreshToken('mock_refresh_token');
      return LoginResponse(accessToken: 'access', refreshToken: 'refresh');
    });

    await tester.pumpWidget(createTestWidget());
    router.go('/login');
    await tester.pumpAndSettle();

    await tester.enterText(
      find.byType(TextFormField).at(0),
      'tadeusz.norek@mpwik.com.pl',
    );
    await tester.enterText(find.byType(TextFormField).at(1), 'k0cham_k@naly');
    await tester.pump();

    await tester.tap(find.byType(ElevatedButton));
    await tester.pumpAndSettle();

    expect(find.text(applicationName), findsExactly(2));
    // For snackbar with info
    expect(find.text('Zalogowano pomyślnie'), findsWidgets);
  });
}
