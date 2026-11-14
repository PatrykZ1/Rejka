import 'dart:io';

import 'package:boundingbox/api/models/request/refresh_request.dart';
import 'package:boundingbox/app/navigation_service.dart';
import 'package:boundingbox/constants.dart';
import 'package:boundingbox/exceptions/api_response_exception.dart';
import 'package:dio/dio.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:go_router/go_router.dart';

import 'models/response/login_response.dart';

class ApiCore {
  static final ApiCore _instance = ApiCore._internal();
  factory ApiCore() => _instance;
  ApiCore._internal() {
    _dio = Dio(
      BaseOptions(
        baseUrl: backendUrl + apiUrl,
        connectTimeout: const Duration(seconds: 10),
        receiveTimeout: const Duration(seconds: 10),
        headers: {
          'Content-Type': 'application/json',
          'Accept': 'application/json',
        },
      ),
    );

    _dio.interceptors.add(
      InterceptorsWrapper(
          onRequest: (options, handler) async {
            if (await isToken()) {
              final token = await getToken(key: _accessTokenKey);
              options.headers['Authorization'] = 'Bearer $token';
            }
            return handler.next(options);
          },
          // If unauthorized then try to refresh JWT token
          onResponse: (response, handler) async {
            if (response.statusCode == 401) {
              try {
                await refresh();
              } catch (e) {
                // If refresh unsuccessful then reroute to login screen
                final context = NavigationService.navigatorKey.currentContext;
                if (context != null && context.mounted) {
                  context.go('/login');
                }
              }
            }

            handler.next(response);
          }
      ),
    );
  }

  late final Dio _dio;

  static const FlutterSecureStorage _storage = FlutterSecureStorage();
  static const String _accessTokenKey = 'jwt_access_token';
  static const String _refreshTokenKey = 'jwt_refresh_token';

  Future<bool> isToken() async {
    final token = await getToken(key: _accessTokenKey);
    return !(token == null || token.isEmpty);
  }

  Future<void> saveToken({required String key, required String token}) async {
    await _storage.write(key: key, value: token);
  }
  Future<String?> getToken({required String key}) async {
    return await _storage.read(key: key);
  }
  Future<void> removeToken({required String key}) async {
    await _storage.delete(key: key);
  }

  Future<void> saveAccessToken(String token) async => await saveToken(key: _accessTokenKey, token: token);
  Future<void> saveRefreshToken(String token) async => await saveToken(key: _refreshTokenKey, token: token);

  Future<Response> get(String path, {Map<String, dynamic>? queryParameters}) async {
    try {
      return await _dio.get(path, queryParameters: queryParameters);
    } on DioException catch(e) {
      if (e.type == DioExceptionType.badResponse) {
        rethrow;
      }
      throw ApiResponseException('Błąd połączenia z serwerem');
    }
  }

  Future<Response> post(String path, {Map<String, dynamic>? queryParameters, dynamic data}) async {
    try {
      return await _dio.post(path, queryParameters: queryParameters, data: data);
    } on DioException catch(e) {
      if (e.type == DioExceptionType.badResponse) {
        rethrow;
      }
      throw ApiResponseException('Błąd połączenia z serwerem');
    }
  }

  Future<Response> patch(String path, {Map<String, dynamic>? queryParameters, dynamic data}) async {
    try {
      return await _dio.patch(path, queryParameters: queryParameters, data: data);
    } on DioException catch(e) {
      if (e.type == DioExceptionType.badResponse) {
        rethrow;
      }
      throw ApiResponseException('Błąd połączenia z serwerem');
    }
  }

  Future<Response> delete(String path, {Map<String, dynamic>? queryParameters, dynamic data}) async {
    try {
      return await _dio.delete(path, queryParameters: queryParameters, data: data);
    } on DioException catch(e) {
      if (e.type == DioExceptionType.badResponse) {
        rethrow;
      }
      throw ApiResponseException('Błąd połączenia z serwerem');
    }
  }

  Future<void> refresh() async {
    try {
      final token = await getToken(key: _refreshTokenKey);
      if (token == null || token.isEmpty) {
        return;
      }

      final request = RefreshRequest(refreshToken: token);
      final response = await post('/account/refresh', data: request);
      final tokenResponse = LoginResponse.fromJson(response.data);
      saveAccessToken(tokenResponse.accessToken);
      saveRefreshToken(tokenResponse.refreshToken);
      return;

    } on DioException catch (e) {
      switch(e.response?.statusCode) {
        case 400: {
          throw ApiResponseException('Błędne żądanie odświeżenia tokenu.');
        }
        case 401: {
          throw ApiResponseException('Nieuprawnione działanie.');
        }
        case 403: {
          throw ApiResponseException('Token wygasł.');
        }
      }
    } catch (e) {
      throw ApiResponseException('Nieznany błąd odświeżenia tokenu.');
    }

    // If error occurred then clear user tokens
    await removeToken(key: _accessTokenKey);
    await removeToken(key: _refreshTokenKey);
  }
}