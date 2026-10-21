# VietGuide — Thuyết minh du lịch đa ngôn ngữ

Ứng dụng ASP.NET Core .NET 10 gồm ba project API/BLL/DAL, giao diện HTML/CSS/JavaScript, PostgreSQL trên Supabase, Gemini cho nội dung và audio, Supabase Storage cho audio đã công bố. API chạy cùng frontend tĩnh trên Render.

## Cấu hình

Không lưu connection string, Gemini API key, Supabase service role key, mật khẩu Admin hoặc JWT signing key trong Git. Có thể cấu hình bằng environment variables (Render) hoặc .NET User Secrets (máy local).

| Environment variable | Mục đích |
|---|---|
| `ConnectionStrings__DefaultConnection` | Connection string PostgreSQL Supabase hiện đang dùng. Giữ nguyên endpoint/kiểu kết nối đã xác nhận hoạt động; ứng dụng dùng Npgsql và SSL theo connection string đó. |
| `Gemini__ApiKey` | Gemini API key phía Backend |
| `Gemini__Model` | Model sinh nội dung; mặc định `gemini-3.8-flash` |
| `Gemini__TtsModel` | Model TTS; mặc định `gemini-2.5-flash-preview-tts` |
| `Supabase__Url` | URL project Supabase |
| `Supabase__ServiceRoleKey` | Service role key chỉ dùng phía Backend |
| `Supabase__AudioBucket` | Tên bucket; mặc định `narration-audio` |
| `Admin__Username` | Tên đăng nhập Admin |
| `Admin__Password` | Mật khẩu Admin |
| `Jwt__SigningKey` | Secret ký JWT, tối thiểu 32 byte |
| `Jwt__Issuer` | Issuer, mặc định `VietGuide` |
| `Jwt__Audience` | Audience, mặc định `VietGuideAdmin` |

`ConnectionStrings__DefaultConnection` phải là **PostgreSQL connection string**, không phải `Supabase__Url` (URL HTTPS của project). Ứng dụng chấp nhận dạng `Host=...;Port=5432;Database=postgres;Username=...;Password=...;SSL Mode=Require` hoặc URI PostgreSQL do Supabase cấp dạng `postgresql://username:password@host:5432/database?sslmode=require`. Nếu mật khẩu có ký tự đặc biệt trong URI, phải URL-encode theo định dạng URI; dạng `Host=...` thường dễ nhập chính xác hơn.

Ví dụ đặt secret local (thay giá trị bằng secret của môi trường riêng; không đưa giá trị thật vào source):

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<connection-string-Supabase>"
dotnet user-secrets set "Gemini:ApiKey" "<Gemini-API-key>"
dotnet user-secrets set "Supabase:Url" "<Supabase-project-URL>"
dotnet user-secrets set "Supabase:ServiceRoleKey" "<Supabase-service-role-key>"
dotnet user-secrets set "Admin:Username" "<admin-username>"
dotnet user-secrets set "Admin:Password" "<strong-admin-password>"
dotnet user-secrets set "Jwt:SigningKey" "<random-secret-at-least-32-bytes>"
```

`Supabase__ServiceRoleKey` không được đưa vào HTML/JavaScript. API upload/delete audio bằng key này. Tạo bucket `narration-audio` trong Supabase Storage và bật public read cho bucket để trình duyệt phát audio bằng `AudioUrl`; không cấp quyền upload/delete cho `anon` hoặc `authenticated`. Ứng dụng chỉ lưu object path, MIME type và metadata trong PostgreSQL, không ghi audio vào filesystem Render.

`Gemini__ApiKey`, `Supabase__Url`, `Supabase__ServiceRoleKey`, `Admin__Username`, `Admin__Password` và `Jwt__SigningKey` không bắt buộc để khởi động ứng dụng hoặc tải danh sách địa điểm/ngôn ngữ. Kết nối PostgreSQL (`ConnectionStrings__DefaultConnection`) vẫn bắt buộc. Nếu thiếu Gemini key, endpoint tạo thuyết minh/trình tạo nháp trả `503 Service Unavailable`; nếu thiếu cấu hình Storage, thao tác tạo audio không thể lưu. Admin cần đủ các cấu hình Admin/JWT nêu trên để đăng nhập; khi thiếu signing key, server dùng key ngẫu nhiên chỉ trong tiến trình hiện tại nên không phát hành token Admin dùng được. Để bật đầy đủ AI, audio online và Admin local, đặt các secret bằng User Secrets hoặc biến môi trường.

## Chạy local

```powershell
dotnet restore .\ThuyetMinhDaNgonNgu.sln
dotnet build .\ThuyetMinhDaNgonNgu.sln
dotnet run --launch-profile https --project .\src\ThuyetMinhDaNgonNgu.API\ThuyetMinhDaNgonNgu.API.csproj
```

Launch profile giữ nguyên HTTPS `https://localhost:7138` và HTTP `http://localhost:5094`. Khi kết nối PostgreSQL hoặc áp dụng migration không thành công, API dừng khi khởi động thay vì trả dữ liệu thành công giả.

## PostgreSQL schema và migrations

`Program.cs` chạy `Database.MigrateAsync()` trước khi phục vụ HTTP requests. Migration hiện tại tạo `Narrations`, unique constraint trên `(LocationId, LanguageCode)` và hai cột tọa độ nullable cho `Locations`; dữ liệu `Locations`/`Languages` hiện có không bị xóa hay dựng lại. Seed chỉ thêm dữ liệu mẫu khi bảng tương ứng trống, bổ sung tọa độ cho các địa danh mẫu đã biết khi tọa độ còn thiếu, và đồng bộ identity sequence sau các ID seed tường minh. Audio WAV được tạo theo PCM little-endian và lưu dưới prefix phiên bản `narrations/v2/`; bản audio cũ sẽ được dựng lại từ văn bản đã lưu khi được yêu cầu sau deploy.

Để tạo migration tiếp theo:

```powershell
$env:ConnectionStrings__DefaultConnection = "<connection-string-Supabase>"
dotnet ef migrations add <MigrationName> --project .\src\ThuyetMinhDaNgonNgu.DAL\ThuyetMinhDaNgonNgu.DAL.csproj --startup-project .\src\ThuyetMinhDaNgonNgu.API\ThuyetMinhDaNgonNgu.API.csproj
```

Trước lần deploy đầu tiên với một database đang có dữ liệu, kiểm tra bảng `"__EFMigrationsHistory"` trong Supabase. Migration ban đầu `20261021005949_InitialPostgresSchema` tạo hai bảng hiện có; **không** để EF chạy migration khởi tạo đó trên các bảng đã được tạo thủ công nếu chưa baseline. Nếu history chưa có migration nhưng `"Locations"` và `"Languages"` đã tồn tại, trước tiên xác minh schema, khóa chính, cột và kiểu dữ liệu của hai bảng khớp chính xác với migration ban đầu trong `src/ThuyetMinhDaNgonNgu.DAL/Persistence/Migrations/20261021005949_InitialPostgresSchema.cs`. Sau khi xác minh và sao lưu, baseline migration ban đầu bằng cách ghi nhận đúng migration đó trong history; không chạy script tạo lại bảng, không xóa/reset dữ liệu. Nếu schema khác, dừng deploy và tạo baseline migration phù hợp sau khi đối chiếu schema thực tế. Các migration mới sẽ được áp dụng tuần tự bởi EF Core.

Chỉ khi schema đã được xác minh khớp và history chưa ghi nhận migration ban đầu, có thể baseline bằng SQL sau trong Supabase SQL Editor:

```sql
CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261021005949_InitialPostgresSchema', '10.0.13')
ON CONFLICT ("MigrationId") DO NOTHING;
```

Không chạy đoạn baseline chỉ dựa trên việc bảng đã tồn tại; phải kiểm tra đầy đủ schema trước, nếu không migration tiếp theo có thể giả định thiếu/sai cấu trúc hiện có.

## API

| Method | Endpoint | Truy cập | Mô tả |
|---|---|---|---|
| GET | `/api/locations` | Public | Địa điểm, gồm `latitude`/`longitude` nullable |
| GET | `/api/locations/{id}` | Public | Chi tiết địa điểm |
| POST | `/api/locations` | Admin JWT | Thêm địa điểm |
| GET | `/api/languages` | Public | Danh sách ngôn ngữ |
| POST | `/api/languages` | Admin JWT | Thêm ngôn ngữ |
| POST | `/api/narrations` | Public, giới hạn 5 request/phút/IP | Lấy bản hiện hành hoặc tạo nội dung + audio |
| POST | `/api/admin/auth/login` | Public, giới hạn đăng nhập | Đăng nhập lấy JWT role `Admin` |
| GET | `/api/admin/narrations` | Admin JWT | Danh sách bản hiện hành |
| GET | `/api/admin/narrations/{locationId}/{languageCode}` | Admin JWT | Lấy một bản hiện hành |
| POST | `/api/admin/narrations/draft` | Admin JWT, giới hạn 5 request/phút/IP | Sinh nháp văn bản bằng AI |
| PUT | `/api/admin/narrations/{locationId}/{languageCode}` | Admin JWT | Tạo audio từ văn bản cuối và công bố/upsert |

Ví dụ yêu cầu thuyết minh:

```http
POST /api/narrations
Content-Type: application/json

{
  "locationId": 1,
  "languageCode": "en"
}
```

Response gồm `id`, `locationId`, `languageCode`, `text`, `audioUrl`, `status`, `createdAtUtc`, `updatedAtUtc` và `isCached`. Audio cache được xác minh còn truy cập được trước khi trả về; nếu thiếu audio, hệ thống dùng lại văn bản đã lưu và chỉ tạo lại audio. Admin phải đăng nhập và gửi `Authorization: Bearer <accessToken>` cho các thao tác quản trị. Draft AI chưa được lưu/công bố; khi PUT, hệ thống tạo audio cho chính văn bản cuối trước, upload thành công rồi mới upsert bản hiện hành. Unique constraint bảo vệ mỗi cặp địa điểm/ngôn ngữ khỏi bản hiện hành trùng lặp.

## Kiến trúc và deploy

- `src/ThuyetMinhDaNgonNgu.API/`: Controllers, DTOs, authentication/authorization, rate limiting, HTTP và DI.
- `src/ThuyetMinhDaNgonNgu.BLL/`: interfaces, models và nghiệp vụ.
- `src/ThuyetMinhDaNgonNgu.DAL/`: EF entities/DbContext/migrations, PostgreSQL repositories, Gemini/TTS và Supabase Storage.

Render cần đặt các biến môi trường trong bảng cấu hình ở trên; riêng connection string phải dùng `ConnectionStrings__DefaultConnection` và giữ nguyên connection string Supabase đang hoạt động. `.dockerfile` tiếp tục dùng `PORT` do Render cấp. Không cần persistent disk cho audio.
