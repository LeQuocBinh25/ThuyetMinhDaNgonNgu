
# GIAI ĐOẠN 1: BUILD - Sử dụng .NET SDK để khôi phục thư viện và biên dịch ứng dụng
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

# Thiết lập thư mục làm việc bên trong container
WORKDIR /src

# Copy file Solution vào container
COPY ThuyetMinhDaNgonNgu.sln ./

# Copy toàn bộ thư mục src chứa các project vào container
COPY src/ src/

# Khôi phục các package và thư viện cần thiết cho Solution
RUN dotnet restore ThuyetMinhDaNgonNgu.sln

# Publish project API ở chế độ Release
# --no-restore: Không khôi phục package lại vì đã thực hiện ở bước trên
# --output /out: Xuất các file đã publish vào thư mục /out
RUN dotnet publish src/ThuyetMinhDaNgonNgu.API/ThuyetMinhDaNgonNgu.API.csproj \
    --configuration Release \
    --no-restore \
    --output /out

# GIAI ĐOẠN 2: RUNTIME - Sử dụng môi trường ASP.NET Runtime để chạy ứng dụng
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

# Thiết lập thư mục chạy ứng dụng
WORKDIR /app

# Copy các file đã publish từ giai đoạn build sang thư mục /app
COPY --from=build /out .

# Thiết lập môi trường chạy là Production
ENV ASPNETCORE_ENVIRONMENT=Production

# Cấu hình ứng dụng lắng nghe trên tất cả địa chỉ tại cổng 8080
ENV ASPNETCORE_URLS=http://+:8080

# Khai báo cổng 8080 mà container sử dụng
EXPOSE 8080

# Lệnh chạy ứng dụng khi container khởi động
ENTRYPOINT ["dotnet", "ThuyetMinhDaNgonNgu.API.dll"]
