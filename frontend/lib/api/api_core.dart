import 'dart:io';

import 'package:boundingbox/constants.dart';
import 'package:dio/dio.dart';

class ApiCore {

  late final Dio _dio;

  ApiCore() {
    _dio = Dio(
      BaseOptions(
        baseUrl: backendUrl + apiUrl,
        connectTimeout: const Duration(seconds: 10),
        receiveTimeout: const Duration(seconds: 10),
        headers: {
          'Content-Type': 'appliaction/json',
          'Accept': 'application/json',
        },
      ),
    );

    _dio.interceptors.add(
      InterceptorsWrapper(
        onRequest: (options, handler) {
          return handler.next(options);
        },
      ),
    );
  }

  Future<Response> get(String path, {Map<String, dynamic>? queryParameters}) async {
    try {
      return await _dio.get(path, queryParameters: queryParameters);
    } on SocketException catch(e) {
      throw Exception('Brak połączenia z serwerem');
    }
  }

  Future<Response> post(String path, {Map<String, dynamic>? queryParameters, dynamic data}) async {
      try {
        return await _dio.post(path, queryParameters: queryParameters, data: data);
      } on DioException catch(e) {
        rethrow;
        //throw Exception('Brak połączenia z serwerem');
      }
  }

  Future<Response> patch(String path, {Map<String, dynamic>? queryParameters, dynamic data}) async {
      return await _dio.patch(path, queryParameters: queryParameters, data: data);
  }

  Future<Response> delete(String path, {Map<String, dynamic>? queryParameters, dynamic data}) async {
      return await _dio.delete(path, queryParameters: queryParameters, data: data);
  }
}