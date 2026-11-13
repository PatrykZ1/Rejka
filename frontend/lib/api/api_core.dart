import 'dart:io';

import 'package:boundingbox/constants.dart';
import 'package:boundingbox/exceptions/api_response_exception.dart';
import 'package:dio/dio.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

class ApiCore {
  late final Dio _dio;

  static const FlutterSecureStorage _storage = FlutterSecureStorage();
  static const String _tokenKey = 'jwt_token';

  ApiCore() {
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
          final token = await getToken();
          if (token != null && token.isNotEmpty) {
            options.headers['Authorization'] = 'Bearer $token';
          }
          return handler.next(options);
        },
      ),
    );
  }

  Future<void> saveToken(String token) async {
    await _storage.write(key: _tokenKey, value: token);
  }

  Future<String?> getToken() async {
    return await _storage.read(key: _tokenKey);
  }

  Future<void> removeToken() async {
    await _storage.delete(key: _tokenKey);
  }

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
}