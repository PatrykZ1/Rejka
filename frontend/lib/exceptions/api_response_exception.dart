class ApiResponseException implements Exception {
  final String message;

  ApiResponseException(this.message);

  @override
  String toString() {
    return message;
  }
}