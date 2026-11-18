import 'package:boundingbox/constants.dart';
import 'package:boundingbox/widgets/drawer.dart';
import 'package:flutter/material.dart';

class BlankScreen extends StatelessWidget {
  final String message;

  const BlankScreen({super.key, required this.message});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text(applicationName), centerTitle: true),
      drawer: AppDrawer(),
      body: Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          crossAxisAlignment: CrossAxisAlignment.center,
          children: [Icon(Icons.error), SizedBox(height: 8), Text(message)],
        ),
      ),
    );
  }
}
