import 'package:boundingbox/api/api_account.dart';
import 'package:boundingbox/constants.dart';
import 'package:boundingbox/widgets/snackbars.dart';
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

class RegisterScreen extends StatefulWidget {
  const RegisterScreen({super.key});

  @override
  State<RegisterScreen> createState() => _RegisterScreenState();
}

class _RegisterScreenState extends State<RegisterScreen> {
  final ApiAccount api = ApiAccount();

  final _formKey = GlobalKey<FormState>();

  final TextEditingController usernameController = TextEditingController();
  final TextEditingController emailController = TextEditingController();
  final TextEditingController passwordController = TextEditingController();

  @override
  void dispose() {
    usernameController.dispose();
    emailController.dispose();
    passwordController.dispose();
    super.dispose();
  }

  Future<void> register(BuildContext context) async {
    if (!_formKey.currentState!.validate()) {
      return;
    }

    final username = usernameController.text.trim();
    final email = emailController.text.trim();
    final password = passwordController.text;

    try {
      await api.register(
        username: username,
        email: email,
        password: password,
      );

      if (context.mounted) {
        context.go('/login');
        ScaffoldMessenger.of(context).showSnackBar(snackBarInfo('Zarejestrowano konto.'));
      }
    } catch (e) {
      if (context.mounted) {
        ScaffoldMessenger.of(context).showSnackBar(snackBarError('Błąd: ${e.toString()}'));
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text(applicationName),
        centerTitle: true,
      ),

      body: Padding(
        padding: const EdgeInsets.all(20.0),
        child: Form(
          key: _formKey,
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              const Text(
                'Załóż konto',
                style: TextStyle(
                  fontSize: 28,
                  fontWeight: FontWeight.bold,
                ),
              ),

              const SizedBox(height: 30),

              TextFormField(
                controller: usernameController,
                decoration: InputDecoration(
                  labelText: 'Nazwa',
                  prefixIcon: const Icon(Icons.person),
                  border: OutlineInputBorder(
                    borderRadius: BorderRadius.circular(12),
                  ),
                ),
                validator: (value) {
                  if (value == null || value.trim().isEmpty) {
                    return 'Wpisz nazwę użytkownika';
                  }
                  return null;
                },
              ),

              const SizedBox(height: 20),

              TextFormField(
                controller: emailController,
                keyboardType: TextInputType.emailAddress,
                decoration: InputDecoration(
                  labelText: 'Email',
                  prefixIcon: const Icon(Icons.email),
                  border: OutlineInputBorder(
                    borderRadius: BorderRadius.circular(12),
                  ),
                ),
                validator: (value) {
                  if (value == null || value.trim().isEmpty) {
                    return 'Wpisz adres email';
                  }

                  final emailRegex = RegExp(
                    r'^[^@\s]+@[^@\s]+\.[^@\s]+$',
                  );

                  if (!emailRegex.hasMatch(value.trim())) {
                    return 'Wpisz poprawny adres email';
                  }

                  return null;
                },
              ),

              const SizedBox(height: 20),

              TextFormField(
                controller: passwordController,
                obscureText: true,
                decoration: InputDecoration(
                  labelText: 'Hasło',
                  prefixIcon: const Icon(Icons.lock),
                  border: OutlineInputBorder(
                    borderRadius: BorderRadius.circular(12),
                  ),
                ),
                validator: (value) {
                  if (value == null || value.isEmpty) {
                    return 'Wpisz hasło';
                  }

                  if (value.length < 8) {
                    return 'Hasło musi mieć minimum 8 znaków';
                  }

                  return null;
                },
              ),

              const SizedBox(height: 30),

              SizedBox(
                width: double.infinity,
                height: 50,
                child: ElevatedButton(
                  onPressed: () => register(context),
                  child: const Text(
                    'Załóż',
                    style: TextStyle(fontSize: 18),
                  ),
                ),
              ),

              const SizedBox(height: 20),

              TextButton(onPressed: () => context.go('/login'), child: Text('Zaloguj się na istniejące konto'))
            ],
          ),
        ),
      ),
    );
  }
}
