# ThuyetMinhDaNgonNgu — Giai đoạn 2: SQLite

Backend ASP.NET Core .NET 10 theo cấu trúc 3 project API, BLL và DAL.

Ở giai đoạn này, dữ liệu địa điểm và ngôn ngữ đã chuyển từ danh sách trong bộ nhớ sang SQLite thông qua Entity Framework Core. Chức năng tạo thuyết minh vẫn trả về văn bản mô phỏng; chưa gọi AI, chưa tạo âm thanh và chưa lưu lịch sử thuyết minh.

## Yêu cầu

- .NET 10 SDK
- Kết nối Internet trong lần đầu chạy `dotnet restore` để tải NuGet package
- Postman hoặc REST Client của VS Code để kiểm thử API

## Chạy ứng dụng

Mở Terminal ở thư mục chứa `ThuyetMinhDaNgonNgu.sln`:

```powershell
dotnet restore .\ThuyetMinhDaNgonNgu.sln
dotnet build .\ThuyetMinhDaNgonNgu.sln
dotnet run --launch-profile https --project .\src\ThuyetMinhDaNgonNgu.API\ThuyetMinhDaNgonNgu.API.csproj
```

Theo `launchSettings.json`, địa chỉ HTTPS dự kiến là `https://localhost:7138` và HTTP là `http://localhost:5094`. Nếu Terminal hiển thị cổng khác, hãy dùng đúng địa chỉ đang lắng nghe.

## SQLite được lưu ở đâu?

Database sẽ được tạo tự động tại:

`src/ThuyetMinhDaNgonNgu.API/App_Data/multilingual-narration.db`

Khi ứng dụng khởi động lần đầu, `DatabaseSeeder` tạo schema bằng `EnsureCreated()` và thêm dữ liệu mẫu nếu bảng tương ứng còn trống. Các lần khởi động sau sẽ đọc lại database hiện có, vì vậy việc dừng/chạy lại API không làm mất dữ liệu.

Hai bảng hiện tại:

- `Locations`: Id, Name, City, Description
- `Languages`: Code, Name

Để giữ file database ngoài Git, `.gitignore` loại trừ các file `.db`, `.db-shm`, `.db-wal` trong thư mục `App_Data`.

> Giai đoạn này dùng `EnsureCreated()` để có thể học và kiểm thử nhanh. Khi schema cần thay đổi trong quá trình phát triển, không nên tiếp tục sửa schema thủ công; hãy chuyển sang EF Core Migrations. Xóa file database chỉ nên là biện pháp phát triển cục bộ khi bạn chấp nhận mất dữ liệu mẫu đã lưu.

## API

| Method | Endpoint | Mục đích |
|---|---|---|
| GET | `/api/locations` | Lấy tất cả địa điểm từ SQLite |
| GET | `/api/locations/{id}` | Lấy một địa điểm từ SQLite |
| GET | `/api/languages` | Lấy danh sách ngôn ngữ từ SQLite |
| POST | `/api/languages` | Thêm ngôn ngữ mới vào SQLite |
| POST | `/api/narrations` | Kiểm tra địa điểm/ngôn ngữ từ SQLite rồi trả văn bản thuyết minh mô phỏng |

Ví dụ thêm tiếng Tây Ban Nha:

```http
POST /api/languages
Content-Type: application/json

{
  "code": "es",
  "name": "Español"
}
```

API trả `201 Created`. Gọi lại `GET /api/languages` để thấy ngôn ngữ vừa thêm. `code` dài từ 2 đến 10 ký tự được chuẩn hóa thành chữ thường; `name` là tên hiển thị. Nếu mã đã tồn tại, API trả `409 Conflict`.

Ví dụ body cho `POST /api/narrations`:

```json
{
  "locationId": 1,
  "languageCode": "en"
}
```

Ngôn ngữ mẫu: `vi`, `en`, `ja`.

## Kiểm thử bằng Postman

Dùng `http://localhost:5094` hoặc `https://localhost:7138` theo cổng được hiển thị trong Terminal.

1. `GET /api/locations` — phải trả về 3 địa điểm mẫu.
2. `GET /api/languages` — phải trả về 3 ngôn ngữ mẫu.
3. `GET /api/locations/1` — phải trả về Hồ Hoàn Kiếm.
4. `POST /api/narrations` với body trên — phải trả `200 OK` và `status` là `demo-text-only`.
5. Gửi `POST` với `locationId: 999` — mong đợi `404`.
6. Gửi `POST` với `languageCode: "fr"` — mong đợi `400`.

Để xác nhận dữ liệu thật sự được đọc từ SQLite, dừng ứng dụng, chạy lại, rồi gửi lại các request GET. Dữ liệu mẫu vẫn phải còn.

## Trách nhiệm theo 3 lớp

- **API**: Controller, DTO, HTTP response, khởi tạo ứng dụng và đăng ký Dependency Injection.
- **BLL**: models, interface và logic nghiệp vụ.
- **DAL**: Entity, `AppDbContext`, seed data và các repository dùng EF Core/SQLite.

## Giai đoạn sau

Khi dữ liệu SQLite ổn định, bước tiếp theo là tạo abstraction cho dịch vụ sinh nội dung AI, tích hợp Text-to-Speech, rồi mới cân nhắc lưu lịch sử thuyết minh.
