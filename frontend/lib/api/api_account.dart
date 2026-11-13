import 'package:boundingbox/api/api_core.dart';
import 'package:boundingbox/api/models/request/register_request.dart';
import 'package:dio/dio.dart';

class ApiAccount {
  final ApiCore _api = ApiCore();

  Future<void> register({required String username, required String email, required String password}) async {
    try {
      final request = RegisterRequest(
          username: username, email: email, password: password);
      await _api.post('/account/register', data: request);

    } on DioException catch(e) {
      switch (e.response?.statusCode) {
        case 400:
          {
            throw Exception('Słabe hasło lub nieprawidłowe dane.');
          }
        case 409:
          {
            throw Exception('Adres email jest już zarejestrowany.');
          }
        default: {
          throw Exception('Nieznany błąd.');
        }
      }
    }
  }
}