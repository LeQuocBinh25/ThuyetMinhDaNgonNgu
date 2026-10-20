
# Giai đoạn 1: Build ứng dụng
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Sao chép source code vào container
COPY . .

# Restore các thư viện và project được tham chiếu
RUN dotnet restore "src/ThuyetMinhDaNgonNgu.API/ThuyetMinhDaNgonNgu.API.csproj"

# Build và publish API cùng các project phụ thuộc
RUN dotnet publish "src/ThuyetMinhDaNgonNgu.API/ThuyetMinhDaNgonNgu.API.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore

# Giai đoạn 2: Chạy ứng dụng
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

# Sử dụng PORT do Render cung cấp
EXPOSE 10000
ENTRYPOINT ["sh", "-c", "dotnet ThuyetMinhDaNgonNgu.API.dll --urls http://0.0.0.0:${PORT:-10000}"]
