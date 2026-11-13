FROM debian:bookworm-slim AS build-env

RUN apt-get update && apt-get install -y curl git unzip xz-utils libglu1-mesa
RUN git clone https://github.com/flutter/flutter.git -b stable /usr/local/flutter

ENV PATH="/usr/local/flutter/bin:/usr/local/flutter/bin/cache/dart-sdk/bin:${PATH}"
ENV TAR_OPTIONS="--no-same-owner"

RUN flutter doctor

WORKDIR /app
COPY . .

RUN dart run build_runner build --delete-conflicting-outputs
RUN flutter build web --release -t lib/main.dart

FROM nginx:alpine

COPY --from=build-env /app/build/web /usr/share/nginx/html

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]