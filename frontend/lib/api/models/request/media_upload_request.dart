import 'package:dio/dio.dart';
import 'package:json_annotation/json_annotation.dart';

part 'media_upload_request.g.dart';

@JsonSerializable(
  createFactory: false,
  includeIfNull: false,
  fieldRename: FieldRename.pascal,
)
class MediaUploadRequest {
  @JsonKey(includeToJson: false)
  final String filePath;
  final double? latitude;
  final double? longitude;
  final DateTime? recordedAt;
  final String? comment;

  MediaUploadRequest({
    required this.filePath,
    this.latitude,
    this.longitude,
    this.recordedAt,
    this.comment,
  });

  Map<String, dynamic> toJson() => _$MediaUploadRequestToJson(this);

  Future<FormData> toFormData() async => FormData.fromMap({
    ...toJson(),
    'File': await MultipartFile.fromFile(filePath),
  });
}
