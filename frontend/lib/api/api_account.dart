import 'package:boundingbox/api/api_core.dart';
import 'package:boundingbox/api/models/request/register_request.dart';
import 'package:boundingbox/exceptions/api_response_exception.dart';
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
            throw ApiResponseException('Słabe hasło lub nieprawidłowe dane.');
          }
        case 409:
          {
            throw ApiResponseException('Adres email jest już zarejestrowany.');
          }
        default: {
          throw ApiResponseException('Nieznany błąd.');
        }
      }
    }
  }
}