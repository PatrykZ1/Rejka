import 'package:boundingbox/api/api_core.dart';
import 'package:boundingbox/api/models/request/login_request.dart';
import 'package:boundingbox/api/models/request/register_request.dart';
import 'package:boundingbox/api/models/response/login_response.dart';
import 'package:boundingbox/api/urls.dart';
import 'package:boundingbox/exceptions/api_response_exception.dart';
import 'package:dio/dio.dart';

import 'models/request/logout_request.dart';

class ApiAccount {
  final ApiCore _api = ApiCore();

  Future<void> register({required String username, required String email, required String password}) async {
    try {
      final request = RegisterRequest(
          username: username, email: email, password: password);
      await _api.post(apiUrlRegister, data: request);

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

  Future<LoginResponse> login({required String email, required String password}) async {
    try {
      final request = LoginRequest(email: email, password: password);
      final response = await _api.post(apiUrlLogin, data: request);

      final loginResponse = LoginResponse.fromJson(response.data);
      _api.saveAccessToken(loginResponse.accessToken);
      _api.saveRefreshToken(loginResponse.refreshToken);

      return loginResponse;

    } on DioException catch(e) {
      switch (e.response?.statusCode) {
        case 400:
          {
            throw ApiResponseException('Brakujące lub nieprawidłowe dane żądania.');
          }
        case 401:
          {
            throw ApiResponseException('Błedne dane logowania.');
          }
        case 403:
          {
            throw ApiResponseException('Użytkownik został zablokowany lub usunięty.');
          }
        default: {
          throw ApiResponseException('Nieznany błąd.');
        }
      }
    }
  }

  Future<void> logout() async {
    try {
      final refreshToken = await _api.getRefreshToken();

      if (refreshToken != null) {
        final request = LogoutRequest(refreshToken: refreshToken);
        await _api.post(apiUrlLogout, data: request);
      }

      await _api.removeTokens();

    } on DioException catch(e) {
      switch (e.response?.statusCode) {
        case 401:
          {
            throw ApiResponseException('Użytkownik nie jeset zalogowany.');
          }
        case 403:
          {
            throw ApiResponseException('Brak uprawnień.');
          }
        default: {
          throw ApiResponseException('Nieznany błąd.');
        }
      }
    }
  }
}