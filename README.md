# VietGuide - Hệ thống thuyết minh du lịch đa ngôn ngữ

VietGuide là ứng dụng web giúp du khách nghe thuyết minh về địa điểm du lịch bằng nhiều ngôn ngữ. Hệ thống kết hợp dữ liệu địa điểm, nội dung do AI tạo, audio đọc bằng giọng nói, vị trí GPS và bảng thống kê cho quản trị viên.

## Tính năng

### Cổng dành cho du khách

- Xem danh sách và chi tiết địa điểm du lịch.
- Chọn ngôn ngữ thuyết minh.
- Nghe nội dung bằng audio được tạo bởi Gemini TTS.
- Xem bản đồ Leaflet và vùng bán kính thuyết minh của từng địa điểm.
- Cấp GPS để tìm địa điểm gần và tự phát thuyết minh khi vào vùng phù hợp.
- Vẫn có thể chọn địa điểm/ngôn ngữ thủ công khi trình duyệt từ chối GPS.
- Ghi nhận visitor, heartbeat và lượt nghe qua Supabase/PostgreSQL.

### Khu vực quản trị

- Thêm, sửa, xóa địa điểm.
- Thêm, sửa, xóa ngôn ngữ hỗ trợ.
- Tạo bản nháp thuyết minh bằng Gemini.
- Chỉnh sửa nội dung và tạo audio công bố.
- Xem, tải hoặc xóa bản thuyết minh hiện hành.
- Xem tổng lượt truy cập, lượt nghe, thống kê theo địa điểm và ngôn ngữ.

### Kỹ thuật

- `ASP.NET Core .NET 10` cho REST API.
- Kiến trúc `API -> BLL -> DAL`.
- `PostgreSQL/Npgsql/Entity Framework Core` lưu địa điểm, ngôn ngữ và thuyết minh.
- `Supabase Storage` lưu audio trong bucket riêng tư.
- `Gemini` tạo nội dung và chuyển văn bản thành giọng nói.
- `JWT Bearer` cấp token đăng nhập quản trị.
- `HTML/CSS/JavaScript/Leaflet` cho giao diện web.

## Cấu trúc thư mục

```text
ThuyetMinhDaNgonNgu/
├── src/
│   ├── ThuyetMinhDaNgonNgu.API/       # Controllers, DTO, giao diện wwwroot
│   ├── ThuyetMinhDaNgonNgu.BLL/       # Nghiệp vụ và interface provider
│   └── ThuyetMinhDaNgonNgu.DAL/       # EF Core, PostgreSQL, Gemini, Supabase
├── ThuyetMinhDaNgonNgu.sln
└── README.md
```

## Yêu cầu

- .NET SDK 10.
- PostgreSQL hoặc dịch vụ Supabase PostgreSQL.
- Tài khoản Supabase và bucket Storage.
- Gemini API key.
- Trình duyệt hỗ trợ JavaScript, audio và tùy chọn Geolocation.

## Cấu hình

Không commit khóa bí mật, connection string hoặc service-role key.

Khởi tạo User Secrets cho API:

```powershell
dotnet user-secrets init --project .\src\ThuyetMinhDaNgonNgu.API\ThuyetMinhDaNgonNgu.API.csproj
```

Thiết lập các giá trị cần thiết:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<PostgreSQL-connection-string>" --project .\src\ThuyetMinhDaNgonNgu.API\ThuyetMinhDaNgonNgu.API.csproj
dotnet user-secrets set "Gemini:ApiKey" "<Gemini-API-key>" --project .\src\ThuyetMinhDaNgonNgu.API\ThuyetMinhDaNgonNgu.API.csproj
dotnet user-secrets set "Supabase:Url" "https://<project>.supabase.co" --project .\src\ThuyetMinhDaNgonNgu.API\ThuyetMinhDaNgonNgu.API.csproj
dotnet user-secrets set "Supabase:ServiceRoleKey" "<Supabase-service-role-key>" --project .\src\ThuyetMinhDaNgonNgu.API\ThuyetMinhDaNgonNgu.API.csproj
dotnet user-secrets set "AdminAuth:Username" "<admin-username>" --project .\src\ThuyetMinhDaNgonNgu.API\ThuyetMinhDaNgonNgu.API.csproj
dotnet user-secrets set "AdminAuth:Password" "<admin-password>" --project .\src\ThuyetMinhDaNgonNgu.API\ThuyetMinhDaNgonNgu.API.csproj
dotnet user-secrets set "Jwt:SigningKey" "<secret-key-it-nhat-32-byte>" --project .\src\ThuyetMinhDaNgonNgu.API\ThuyetMinhDaNgonNgu.API.csproj
```

Các cấu hình tùy chọn trong `appsettings.json`:

| Khóa | Mặc định | Ý nghĩa |
|---|---|---|
| `Gemini:Model` | `gemini-3.8-flash` | Model tạo nội dung thuyết minh |
| `Gemini:TtsModel` | `gemini-2.5-flash-preview-tts` | Model tạo giọng nói |
| `Supabase:AudioBucket` | `narration-audio` | Bucket chứa audio |
| `Jwt:Issuer` | `VietGuide` | Issuer của JWT |
| `Jwt:Audience` | `VietGuideAdmin` | Audience của JWT |

Nên tạo bucket `narration-audio` ở chế độ `Private`.

## Chạy hệ thống

Từ thư mục gốc repository:

```powershell
dotnet restore .\ThuyetMinhDaNgonNgu.sln
dotnet build .\ThuyetMinhDaNgonNgu.sln
dotnet run --launch-profile https --project .\src\ThuyetMinhDaNgonNgu.API\ThuyetMinhDaNgonNgu.API.csproj
```

Khi API khởi động, hệ thống tự chạy migration và seed dữ liệu mẫu nếu bảng dữ liệu chưa có.

Mở:

- Khách: `https://localhost:7138/index.html`
- Quản trị: `https://localhost:7138/admin.html`
- Dashboard: `https://localhost:7138/dashboard.html`
- OpenAPI (chỉ Development): `https://localhost:7138/openapi/v1.json`

Nếu dùng profile HTTP, địa chỉ mặc định là `http://localhost:5094`.

## Sử dụng cổng khách

1. Mở trang khách.
2. Chọn địa điểm hoặc nhấn biểu tượng ghim trên bản đồ.
3. Chọn ngôn ngữ thuyết minh.
4. Nhấn nghe thuyết minh để tải nội dung và audio hiện hành.
5. Có thể nhấn “Bắt đầu hành trình” và cấp quyền GPS.
6. Khi vào vùng bán kính đã cấu hình của địa điểm, trang sẽ tự tìm và phát thuyết minh phù hợp.

Nếu không có thuyết minh cho cặp địa điểm/ngôn ngữ, API trả `404`. Quản trị viên cần tạo và công bố bản thuyết minh trước.

## Sử dụng khu vực quản trị

1. Mở `/admin.html`.
2. Quản lý địa điểm: tên, thành phố, mô tả, tọa độ và bán kính tự động tính bằng mét.
3. Quản lý ngôn ngữ: thêm mã và tên hiển thị.
4. Chọn địa điểm và ngôn ngữ.
5. Nhấn tạo nội dung để nhận bản nháp từ Gemini.
6. Chỉnh sửa nội dung rồi nhấn “Tạo audio và công bố”.

Khi công bố lại, hệ thống tải audio mới lên Supabase và cập nhật bản thuyết minh hiện hành cho cặp địa điểm/ngôn ngữ.

## API chính

| Phương thức | Endpoint | Mô tả |
|---|---|---|
| `GET` | `/api/locations` | Danh sách địa điểm |
| `GET` | `/api/locations/{id}` | Chi tiết địa điểm |
| `POST` | `/api/locations` | Tạo địa điểm |
| `PUT` | `/api/locations/{id}` | Cập nhật địa điểm |
| `DELETE` | `/api/locations/{id}` | Xóa địa điểm và narration liên quan |
| `GET` | `/api/languages` | Danh sách ngôn ngữ |
| `POST` | `/api/languages` | Tạo ngôn ngữ |
| `PUT` | `/api/languages/{code}` | Cập nhật tên ngôn ngữ |
| `DELETE` | `/api/languages/{code}` | Xóa ngôn ngữ chưa được narration tham chiếu |
| `GET` | `/api/narrations/{locationId}/{languageCode}` | Lấy thuyết minh hiện hành |
| `POST` | `/api/admin/auth/login` | Đăng nhập quản trị và nhận JWT |
| `GET` | `/api/admin/narrations` | Danh sách thuyết minh hiện hành |
| `POST` | `/api/admin/narrations/draft` | Tạo nội dung nháp bằng AI |
| `PUT` | `/api/admin/narrations/{locationId}/{languageCode}` | Tạo audio và công bố nội dung |
| `DELETE` | `/api/admin/narrations/{locationId}/{languageCode}` | Xóa thuyết minh |
| `POST` | `/api/visitor` | Ghi nhận lượt truy cập |
| `POST` | `/api/visitor-activity/heartbeat` | Cập nhật visitor đang hoạt động |
| `POST` | `/api/visitor-activity/leave` | Ghi nhận visitor rời trang |
| `POST` | `/api/audio-listens` | Ghi nhận lượt nghe |
| `GET` | `/api/dashboard/analytics` | Lấy dữ liệu thống kê |

Có thể dùng file `src/ThuyetMinhDaNgonNgu.API/ThuyetMinhDaNgonNgu.API.http` để thử các endpoint bằng Visual Studio hoặc VS Code.

## Bảo mật và trạng thái hiện tại

- Supabase bucket phải là `Private`; service-role key chỉ được dùng ở máy chủ.
- Audio được cung cấp bằng signed URL ngắn hạn; không công khai bucket.
- Endpoint tạo bản nháp có giới hạn tần suất theo địa chỉ IP.
- Giao diện quản trị hiện chưa lưu và gửi JWT tự động cho mọi thao tác CRUD. Một số route quản trị chưa có `[Authorize(Roles = "Admin")]` đầy đủ. Trước khi triển khai công khai, cần bật xác thực cho toàn bộ endpoint tạo/sửa/xóa và gửi token từ giao diện.
- Không lưu secrets trong repository, log, ảnh chụp màn hình hoặc file cấu hình được commit.

## Kiểm thử thủ công

1. `dotnet build .\ThuyetMinhDaNgonNgu.sln`.
2. Khởi động API bằng profile HTTPS.
3. Kiểm tra danh sách địa điểm và ngôn ngữ.
4. Chọn địa điểm/ngôn ngữ và tải thuyết minh.
5. Kiểm tra phát audio, từ chối cấp GPS và chế độ chọn thủ công.
6. Thử tạo, công bố, tải và xóa narration từ trang quản trị.
7. Kiểm tra dashboard sau khi ghi nhận visitor và lượt nghe.
8. Kiểm tra:
   - Không thể xóa ngôn ngữ đang được thuyết minh tham chiếu.
   - Xóa địa điểm sẽ xóa narration phụ thuộc.
   - Audio chỉ được dọn khi không còn bản ghi nào tham chiếu.
   - Bucket Storage không phải Public.

## CI/CD

Repository có GitHub Actions tại `.github/workflows/ci-cd.yml`.

- Push hoặc pull request vào `main`, `master` hoặc `test` chạy restore, build, test và publish.
- Push trực tiếp vào `main`/`master` chạy job deploy lên production.
- Mỗi lần build tạo artifact `vietguide-api` có thời hạn lưu 14 ngày.
- Docker có thể build bằng:

```powershell
docker build -t vietguide-api .
docker run --rm -p 8080:8080 --env-file .env vietguide-api
```

Không commit file `.env`. Khi chạy container, truyền các biến cấu hình cần thiết bằng environment variables; ví dụ các khóa:

```text
ConnectionStrings__DefaultConnection
Gemini__ApiKey
Supabase__Url
Supabase__ServiceRoleKey
Supabase__AudioBucket
AdminAuth__Username
AdminAuth__Password
```

Để bật CD sang Azure App Service, thêm các GitHub repository secrets sau:

| Secret | Nội dung |
|---|---|
| `AZURE_WEBAPP_NAME` | Tên Azure App Service |
| `AZURE_WEBAPP_PUBLISH_PROFILE` | Nội dung publish profile của App Service |
| `AZURE_WEBAPP_URL` | URL production để hiển thị trong GitHub Environment (không bắt buộc) |

Cấu hình cùng các giá trị kết nối ở trên trong Azure App Settings bằng quy tắc kép gạch dưới cho ASP.NET Core, ví dụ `ConnectionStrings__DefaultConnection`.
