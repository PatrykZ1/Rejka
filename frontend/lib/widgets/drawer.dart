
import 'package:boundingbox/api/api_account.dart';
import 'package:boundingbox/widgets/snackbars.dart';
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

class AppDrawer extends StatelessWidget {
  const AppDrawer({super.key});

  @override
  Widget build(BuildContext context) {

    return Drawer(
      child: ListView(
        padding: EdgeInsets.zero,
        children: [
          const DrawerHeader(
            decoration: BoxDecoration(
              color: Colors.blue,
            ),
            child: Align(
              alignment: Alignment.bottomLeft,
              child: Text(
                'Rejka',
                style: TextStyle(
                  color: Colors.white,
                  fontSize: 24,
                  fontWeight: FontWeight.bold,
                ),
              ),
            ),
          ),

          ListTile(
            leading: const Icon(Icons.sensors),
            title: const Text('Strona główna'),
            onTap: () {
              Navigator.pop(context);
              context.go('/');
            },
          ),

          ListTile(
            leading: const Icon(Icons.sensors),
            title: const Text('Odczyt na żywo'),
            onTap: () {
              Navigator.pop(context);
              context.go('/detector');
            },
          ),

          ListTile(
            leading: const Icon(Icons.logout),
            title: const Text('Wyloguj się'),
            onTap: () async {
              try {
                final api = ApiAccount();
                await api.logout();

                if (context.mounted) {
                  context.go('/login');
                  ScaffoldMessenger.of(context).showSnackBar(snackBarInfo('Wylogowano pomyślnie.'));
                }
              } catch (e) {
                if (context.mounted) {
                  ScaffoldMessenger.of(context).showSnackBar(snackBarError('Błąd: ${e.toString()}'));
                }
              }
            },
          ),
        ],
      ),
    );
  }
}
