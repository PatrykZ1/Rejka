import 'package:flutter/material.dart';

SnackBar snackBarInfo(String text) {
  return SnackBar(content: Text(text));
}

SnackBar snackBarError(String text) {
  return SnackBar(
    content: Text(
      text,
      style: TextStyle(
        color: Colors.white,
      ),
    ),
    backgroundColor: Colors.red,
  );
}