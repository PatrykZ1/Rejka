import 'package:boundingbox/api/api_core.dart';
import 'package:boundingbox/api/models/request/register_request.dart';

class ApiAccount {
  final ApiCore _api = ApiCore();

  Future<void> register({required String username, required String email, required String password}) async {
    final request = RegisterRequest(username: username, email: email, password: password);
    final response = await _api.post('/account/register', data: request);

    switch(response.statusCode) {
      case 400:{
        throw Exception('Słabe hasło lub nieprawidłowe dane.');
      }
      case 409: {
        throw Exception('Adres email jest już zarejestrowany.');
      }
    }
  }
}