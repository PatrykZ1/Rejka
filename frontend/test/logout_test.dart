import 'package:boundingbox/api/api_account.dart';
import 'package:boundingbox/api/api_core.dart';
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

    const MethodChannel channel = MethodChannel('plugins.it_nomads.com/flutter_secure_storage');
    TestDefaultBinaryMessengerBinding.instance.defaultBinaryMessenger.setMockMethodCallHandler(
      channel,
          (MethodCall methodCall) async {
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
      },
    );
    fakeSecureStorage.clear();
    ApiCore().saveRefreshToken('mock_refresh_token');
  });

  tearDown(() {
    ApiAccount.mockInstance = null;
  });

  Widget createTestWidget() {
    return MaterialApp.router(
      routerConfig: router,
    );
  }

  testWidgets('Successful logout navigates to login screen', (WidgetTester tester) async {
    when(() => mockApiAccount.logout()).thenAnswer((_) async {
      await ApiCore().removeTokens();
    });

    await tester.pumpWidget(createTestWidget());
    router.go('/');
    await tester.pumpAndSettle();

    // Should be on home screen
    expect(find.text(applicationName), findsExactly(2));

    await tester.tap(find.byIcon(Icons.menu));
    await tester.pumpAndSettle();

    expect(find.text('Wyloguj się'), findsOneWidget);

    await tester.tap(find.text('Wyloguj się'));
    await tester.pumpAndSettle();

    expect(find.text('Zaloguj się'), findsWidgets);
    // This is for snackbar
    expect(find.text('Wylogowano pomyślnie.'), findsWidgets);
  });
}