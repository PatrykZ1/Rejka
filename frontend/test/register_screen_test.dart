import 'package:boundingbox/api/api_account.dart';
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

  testWidgets('Register screen validates email format and password length', (
    WidgetTester tester,
  ) async {
    await tester.pumpWidget(createTestWidget());

    router.go('/register');
    await tester.pumpAndSettle();

    expect(find.text('Załóż konto'), findsOneWidget);

    await tester.enterText(find.byType(TextFormField).at(0), 'Jan');
    await tester.enterText(find.byType(TextFormField).at(1), 'zly-email');
    await tester.enterText(find.byType(TextFormField).at(2), 'short');

    await tester.tap(find.byType(ElevatedButton));
    await tester.pump();

    expect(find.text('Wpisz poprawny adres email'), findsOneWidget);
    expect(find.text('Hasło musi mieć minimum 8 znaków'), findsOneWidget);
  });

  testWidgets('Successful register navigates to login screen', (
    WidgetTester tester,
  ) async {
    when(
      () => mockApiAccount.register(
        username: 'Karol',
        email: 'karol.krawczyk@tw.waw.pl',
        password: 'k0cham_tr@mwaje',
      ),
    ).thenAnswer((_) async => {});

    await tester.pumpWidget(createTestWidget());
    router.go('/register');
    await tester.pumpAndSettle();

    await tester.enterText(find.byType(TextFormField).at(0), 'Karol');
    await tester.enterText(
      find.byType(TextFormField).at(1),
      'karol.krawczyk@tw.waw.pl',
    );
    await tester.enterText(find.byType(TextFormField).at(2), 'k0cham_tr@mwaje');
    await tester.pump();

    await tester.tap(find.byType(ElevatedButton));
    await tester.pumpAndSettle();

    expect(find.text('Zaloguj się'), findsWidgets);

    // This is for snackbar
    expect(find.text('Zarejestrowano konto.'), findsWidgets);
  });

  testWidgets(
    'Login screen is available (no automatic redirection to login screen)',
    (WidgetTester tester) async {
      await tester.pumpWidget(createTestWidget());

      router.go('/register');
      await tester.pumpAndSettle();

      router.go('/login');
      await tester.pumpAndSettle();

      expect(find.text('Zaloguj się'), findsOneWidget);
    },
  );
}
