import 'dart:math';

import 'package:flutter/material.dart';

class RegistrationPlate extends StatelessWidget {
  final String number;

  const RegistrationPlate(this.number, {super.key});

  @override
  Widget build(BuildContext context) {
    return AspectRatio(
      aspectRatio: 520 / 114,
      child: CustomPaint(painter: _PlatePainter(number)),
    );
  }
}

class _PlatePainter extends CustomPainter {
  final String number;
  const _PlatePainter(this.number);

  static const _blue = Color(0xFF003399);
  static const _gold = Color(0xFFFFCC00);
  static const _black = Color(0xFF111111);

  @override
  void paint(Canvas canvas, Size size) {
    canvas.scale(size.width / 520, size.height / 114);

    canvas.drawRRect(
      RRect.fromRectAndRadius(Rect.fromLTWH(0, 0, 520, 114), const Radius.circular(8)),
      Paint()..color = _black,
    );
    final inner = RRect.fromRectAndRadius(
      Rect.fromLTWH(2.5, 2.5, 515, 109),
      const Radius.circular(5.5),
    );
    canvas.drawRRect(inner, Paint()..color = Colors.white);

    canvas.save();
    canvas.clipRRect(inner);
    canvas.drawRect(Rect.fromLTWH(0, 0, 41, 114), Paint()..color = _blue);
    canvas.restore();

    for (var i = 0; i < 12; i++) {
      final a = i * pi / 6;
      _star(canvas, Offset(21.75 + 11.5 * sin(a), 38 - 11.5 * cos(a)), 3.4);
    }
    _text(canvas, 'PL', 28, Colors.white, 21.75, 92);

    final digit = number.indexOf(RegExp(r'\d'));
    final split = digit == 1 ? 2 : digit;
    final hasGap = split > 0 && split < number.length;
    final gap = hasGap ? 36.0 : 0.0;
    final pitch = min(58.0, (460 - gap) / number.length);
    var x = 41 + (476.5 - number.length * pitch - gap) / 2 + pitch / 2;
    for (var k = 0; k < number.length; k++) {
      if (hasGap && k == split) {
        _sticker(canvas, x - pitch / 2 + gap / 2);
        x += gap;
      }
      _text(canvas, number[k], 112, _black, x, 97, pitch / 67);
      x += pitch;
    }
  }

  void _text(Canvas canvas, String s, double size, Color color, double cx, double baseline, [double sx = 1]) {
    final tp = TextPainter(
      text: TextSpan(
        text: s,
        style: TextStyle(color: color, fontSize: size, fontWeight: FontWeight.w700),
      ),
      textDirection: TextDirection.ltr,
    )..layout();
    canvas.save();
    canvas.translate(cx, baseline);
    canvas.scale(sx, 1);
    tp.paint(canvas, Offset(-tp.width / 2, -tp.computeDistanceToActualBaseline(TextBaseline.alphabetic)));
    canvas.restore();
  }

  void _star(Canvas canvas, Offset c, double r) {
    final path = Path();
    for (var i = 0; i < 10; i++) {
      final a = -pi / 2 + i * pi / 5;
      final p = c + Offset(cos(a), sin(a)) * (i.isEven ? r : r * 0.382);
      if (i == 0) {
        path.moveTo(p.dx, p.dy);
      } else {
        path.lineTo(p.dx, p.dy);
      }
    }
    canvas.drawPath(path..close(), Paint()..color = _gold);
  }

  void _sticker(Canvas canvas, double cx) {
    final r = RRect.fromRectAndRadius(
      Rect.fromCenter(center: Offset(cx, 57), width: 22, height: 30),
      const Radius.circular(2),
    );
    final shader = const LinearGradient(
      begin: Alignment.topLeft,
      end: Alignment.bottomRight,
      colors: [Color(0xFFE8E8E8), Color(0xFFB9D7EA), Color(0xFFE9C9E9), Color(0xFFCFEBC9), Color(0xFFE8E8E8)],
    ).createShader(r.outerRect);
    canvas.drawRRect(r, Paint()..shader = shader);
    canvas.drawRRect(
      r,
      Paint()
        ..style = PaintingStyle.stroke
        ..strokeWidth = 0.6
        ..color = const Color(0x66000000),
    );
  }

  @override
  bool shouldRepaint(_PlatePainter old) => old.number != number;
}