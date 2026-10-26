# VietGuide — Hệ thống Thuyết minh Du lịch Đa ngôn ngữ

Tài liệu hướng dẫn phát triển và xây dựng hệ thống bám sát **Đặc tả yêu cầu sản phẩm (PRD - Product Requirements Document)** và phân công chi tiết cho nhóm 3 thành viên phát triển đồng thời không xung đột mã nguồn.

---

## 1. Tổng quan Dự án & Kiến trúc

Hệ thống cung cấp giải pháp thuyết minh du lịch tự động đa ngôn ngữ cho khách tham quan qua nền tảng Web, kết hợp định vị GPS, bản đồ tương tác, tổng hợp giọng nói AI và bảng điều khiển quản trị tập trung.

### 1.1. Công nghệ chính
- **Backend**: ASP.NET Core (.NET 10) theo kiến trúc 3 lớp phân tách nghiêm ngặt:
  - `ThuyetMinhDaNgonNgu.API`: Tiếp nhận HTTP Request, DTOs, xác thực JWT, phân quyền, rate limiting.
  - `ThuyetMinhDaNgonNgu.BLL`: Thực thi nghiệp vụ, kiểm tra tính hợp lệ (BR-01 → BR-21), điều phối AI & Storage, tổng hợp thống kê.
  - `ThuyetMinhDaNgonNgu.DAL`: Truy xuất PostgreSQL (Supabase) qua EF Core, Adapter Gemini AI & Supabase Storage.
- **Cơ sở dữ liệu & Lưu trữ**:
  - **Supabase PostgreSQL**: Lưu trữ địa điểm, ngôn ngữ, nội dung thuyết minh và dữ liệu sự kiện thống kê.
  - **Supabase Storage**: Bucket âm thanh ở chế độ **Private**, chỉ Backend thao tác bằng `ServiceRoleKey`.
- **Dịch vụ AI**:
  - Sinh nội dung văn bản: `Gemini 3.8 Flash`.
  - Chuyển văn bản thành giọng nói (TTS): `Gemini TTS API` (hoặc `gemini-2.5-flash-preview-tts`).
- **Frontend**: Vanilla HTML5, CSS3, JavaScript (ES6+), Leaflet Map API, Web Geolocation API.
- **Môi trường triển khai**: Render (Docker container).

### 1.2. Các Quyết định Nghiệp vụ Bắt buộc (PRD Phụ lục A & B)
1. **Cửa sổ chống đếm trùng truy cập (`AD01`)**: **30 phút**.
2. **Tiêu chuẩn tính lượt phát thuyết minh (`AD02 - AD05`)**: Chỉ tính là 1 lượt phát hợp lệ khi khách nghe đạt thời lượng tối thiểu **15 giây**.
3. **Mô hình bản ghi thuyết minh**: Mỗi cặp `(LocationId, LanguageCode)` chỉ lưu **1 bản ghi hiện hành** duy nhất.
4. **Bảo mật âm thanh**: Bucket Storage luôn ở chế độ **Private**. Không public bucket ra ngoài Internet; cung cấp URL có chữ ký ngắn hạn (Signed URL) hoặc proxy qua Backend.
5. **Loại trừ phạm vi (Out of Scope - Mục 1.5.5)**: Tính năng *"Lưu lịch sử nghe riêng cho từng khách"* **không thuộc phạm vi** của dự án; không triển khai và phải dọn bỏ mã nguồn liên quan.

---

## 2. Cấu hình Môi trường & Chạy Local

### 2.1. Biến môi trường

| Tên biến | Bắt buộc | Mục đích |
|---|:---:|---|
| `ConnectionStrings__DefaultConnection` | **Có** | Connection string Supabase PostgreSQL (`Host=...;Database=postgres;...;SSL Mode=Require`) |
| `Gemini__ApiKey` | Khuyến nghị | API key Gemini cho sinh nội dung và TTS |
| `Gemini__Model` | Tùy chọn | Model AI văn bản (mặc định: `gemini-3.8-flash`) |
| `Gemini__TtsModel` | Tùy chọn | Model AI giọng nói (mặc định: `gemini-2.5-flash-preview-tts`) |
| `Supabase__Url` | Khuyến nghị | URL HTTPS của project Supabase |
| `Supabase__ServiceRoleKey` | Khuyến nghị | Service role key Supabase (chỉ dùng phía Backend) |
| `Supabase__AudioBucket` | Tùy chọn | Tên bucket âm thanh (mặc định: `narration-audio`) |
| `Admin__Username` | Khuyến nghị | Tên đăng nhập Admin |
| `Admin__Password` | Khuyến nghị | Mật khẩu Admin |
| `Jwt__SigningKey` | Khuyến nghị | Khóa bí mật ký JWT (tối thiểu 32 byte) |
| `Jwt__Issuer` | Tùy chọn | Issuer JWT (mặc định: `VietGuide`) |
| `Jwt__Audience` | Tùy chọn | Audience JWT (mặc định: `VietGuideAdmin`) |

### 2.2. Thiết lập Secret trên máy Local (.NET User Secrets)

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<supabase-postgres-connection-string>"
dotnet user-secrets set "Gemini:ApiKey" "<gemini-api-key>"
dotnet user-secrets set "Supabase:Url" "https://<your-project>.supabase.co"
dotnet user-secrets set "Supabase:ServiceRoleKey" "<supabase-service-role-key>"
dotnet user-secrets set "Admin:Username" "admin"
dotnet user-secrets set "Admin:Password" "<strong-admin-password>"
dotnet user-secrets set "Jwt:SigningKey" "<at-least-32-characters-secret-key>"
```

### 2.3. Khởi chạy ứng dụng

```powershell
# Restore và build
dotnet restore .\ThuyetMinhDaNgonNgu.sln
dotnet build .\ThuyetMinhDaNgonNgu.sln

# Chạy ứng dụng API & Frontend tĩnh
dotnet run --launch-profile https --project .\src\ThuyetMinhDaNgonNgu.API\ThuyetMinhDaNgonNgu.API.csproj
```
- Truy cập giao diện Khách: `https://localhost:7138/index.html`
- Truy cập giao diện Quản trị: `https://localhost:7138/admin.html`

---

## 3. Chiến lược Phối hợp Nhóm 3 Người (Zero-Conflict Protocol)

Để 3 thành viên cùng làm việc mà **hoàn toàn không bị đụng code (merge conflict)**:

```
[Nhánh main]
    │
    ├── Giai đoạn 0: Thành viên 2 tạo Migration Database cơ sở ──> Merge vào main
    │
    ├── Sau khi main đã có DB chuẩn:
    │     ├── [Branch TV1]: feature/client-experience (CL01 - CL07)
    │     ├── [Branch TV2]: feature/analytics-events  (AD01 - AD05)
    │     └── [Branch TV3]: feature/admin-crud-mgmt   (AD06 - AD14)
    │
    └── Tạo Pull Request -> Review chéo -> Merge tuần tự vào main
```

### 🛡️ Nguyên tắc vàng tránh xung đột mã nguồn:
1. **Tuyệt đối không sửa trực tiếp `Program.cs`**:
   - `Program.cs` đã được gắn sẵn 2 Extension Method:
     - `builder.Services.AddAnalyticsModule(...)` -> Thành viên 2 chỉ code trong `src/ThuyetMinhDaNgonNgu.API/Extensions/AnalyticsExtensions.cs`.
     - `builder.Services.AddManagementModule(...)` -> Thành viên 3 chỉ code trong `src/ThuyetMinhDaNgonNgu.API/Extensions/ManagementExtensions.cs`.
2. **Quản lý Migration EF Core**:
   - Chỉ **Thành viên 2** phụ trách tạo Migration schema chung cho toàn bộ dự án ở Giai đoạn 0. Sau khi merge vào `main`, các thành viên khác kéo về dùng.
3. **Phân tách Frontend**:
   - **Thành viên 1**: Toàn quyền trên `wwwroot/index.html`, `wwwroot/js/app.js`, `wwwroot/css/style.css`.
   - **Thành viên 2 & 3**: Thao tác trên `admin.html`. Tách JS thành `admin-analytics.js` (Thành viên 2) và `admin-crud.js` (Thành viên 3).

---

## 4. Hướng dẫn Chi tiết Cho Từng Thành Viên

### 👤 THÀNH VIÊN 1: Trải nghiệm Khách tham quan & Client Flow
- **Mã chức năng phụ trách**: `CL01` → `CL07`
- **Nhánh Git**: `feature/client-experience`
- **File phụ trách chính**:
  - `src/ThuyetMinhDaNgonNgu.API/wwwroot/index.html`
  - `src/ThuyetMinhDaNgonNgu.API/wwwroot/js/app.js`
  - `src/ThuyetMinhDaNgonNgu.API/wwwroot/css/style.css`
  - `src/ThuyetMinhDaNgonNgu.API/Controllers/NarrationsController.cs`

#### Nhiệm vụ cụ thể từng bước:
1. **Dọn dẹp code ngoài phạm vi (Mục 1.5.5)**:
   - Xóa bỏ chức năng *"Lịch sử nghe"* (`vietguide_history_v1`, `#history`, `historyList`, `clearHistoryButton`) khỏi `index.html` và `app.js`.
2. **Triển khai `CL01` (Tự động chọn ngôn ngữ theo trình duyệt)**:
   - Trong `app.js`, khi tải danh sách ngôn ngữ (`/api/languages`), đọc `navigator.language` (ví dụ: `vi-VN` -> `vi`, `en-US` -> `en`).
   - Nếu mã ngôn ngữ khớp với ngôn ngữ được hỗ trợ, tự động chọn làm mặc định trên giao diện; nếu không khớp, áp dụng ngôn ngữ mặc định (`vi`).
3. **Triển khai `CL02` & `CL06` (Chọn ngôn ngữ & địa điểm thủ công)**:
   - Đảm bảo khi người dùng chủ động chọn ngôn ngữ hoặc địa điểm mới, giao diện lập tức cập nhật phiên hiện tại và không phát nhầm audio cũ (BR-02, BR-04).
4. **Triển khai `CL03` & `CL05` (Bản đồ Leaflet & Định vị GPS)**:
   - Sử dụng `navigator.geolocation.getCurrentPosition` khi người dùng nhấn nút *"Tìm địa điểm gần tôi"*.
   - Áp dụng công thức khoảng cách Haversine so sánh tọa độ GPS người dùng với danh sách địa điểm (`CL05`).
   - Khi khoảng cách $\le 50m$ (ngưỡng phát hiện), hiển thị thông báo và cho phép kích hoạt phát bài thuyết minh tương ứng.
   - Xử lý mượt mà khi người dùng từ chối quyền định vị (vẫn cho chọn thủ công bình thường - BR-14).
5. **Triển khai `CL07` & Lắng nghe mốc 15 giây**:
   - Sử dụng phần tử `<audio>` để phát bài thuyết minh.
   - Thêm bộ đếm thời gian: khi audio phát thực tế liên tục đạt **15 giây**, tự động gửi request ghi nhận sự kiện phát:
     ```http
     POST /api/playback
     Content-Type: application/json
     { "narrationId": "...", "locationId": 1, "languageCode": "vi" }
     ```
   - Cũng gửi request ghi nhận phiên truy cập khi người dùng vừa mở trang:
     ```http
     POST /api/visits
     Content-Type: application/json
     { "visitorToken": "...", "source": "qr_or_web" }
     ```

---

### 👤 THÀNH VIÊN 2: Phân hệ Thống kê & Dữ liệu Sự kiện (Analytics Core)
- **Mã chức năng phụ trách**: `AD01` → `AD05` + Database Schema chung
- **Nhánh Git**: `feature/analytics-events`
- **File phụ trách chính**:
  - `src/ThuyetMinhDaNgonNgu.DAL/Entities/VisitEventEntity.cs` *(tạo mới)*
  - `src/ThuyetMinhDaNgonNgu.DAL/Entities/PlaybackEventEntity.cs` *(tạo mới)*
  - `src/ThuyetMinhDaNgonNgu.DAL/Persistence/AppDbContext.cs` & thư mục `Migrations/`
  - `src/ThuyetMinhDaNgonNgu.DAL/Repositories/PostgresAnalyticsRepository.cs` *(tạo mới)*
  - `src/ThuyetMinhDaNgonNgu.BLL/Services/AnalyticsService.cs` *(tạo mới)*
  - `src/ThuyetMinhDaNgonNgu.API/Controllers/AnalyticsController.cs` *(tạo mới)*
  - `src/ThuyetMinhDaNgonNgu.API/Controllers/EventsController.cs` *(tạo mới cho Visits/Playback)*
  - `src/ThuyetMinhDaNgonNgu.API/Extensions/AnalyticsExtensions.cs`
  - `src/ThuyetMinhDaNgonNgu.API/wwwroot/js/admin-analytics.js` *(tạo mới)*

#### Nhiệm vụ cụ thể từng bước:
1. **Thiết kế Entities & Migration cơ sở (Ưu tiên làm đầu tiên)**:
   - Tạo `VisitEventEntity`: `Id`, `VisitorToken`, `IpHash`, `CreatedAtUtc`.
   - Tạo `PlaybackEventEntity`: `Id`, `NarrationId`, `LocationId`, `LanguageCode`, `DurationSeconds`, `CreatedAtUtc`.
   - Bổ sung `IsActive`, `CreatedAtUtc`, `UpdatedAtUtc` cho `LocationEntity` và `LanguageEntity` (theo PRD 6.3).
   - Chạy lệnh tạo Migration:
     ```powershell
     dotnet ef migrations add AddAnalyticsAndAuditFields --project .\src\ThuyetMinhDaNgonNgu.DAL\ThuyetMinhDaNgonNgu.DAL.csproj --startup-project .\src\ThuyetMinhDaNgonNgu.API\ThuyetMinhDaNgonNgu.API.csproj
     ```
   - Báo cho cả nhóm pull về sau khi merge Migration vào `main`.
2. **Nghiệp vụ Chống đếm trùng 30 phút (`AD01`)**:
   - Viết API `POST /api/visits`: Kiểm tra xem `VisitorToken` (hoặc IP hash) đã gửi request trong vòng **30 phút** trước đó chưa. Nếu chưa, tạo bản ghi `VisitEvent`. Nếu đã có trong 30 phút, không tăng lượt truy cập (BR-16 & Phụ lục A).
3. **Nghiệp vụ Ghi nhận lượt phát (`CL07 / AD02`)**:
   - Viết API `POST /api/playback`: Lưu bản ghi `PlaybackEvent` khi Client gửi xác nhận đã nghe $\ge 15s$.
4. **Nghiệp vụ Tổng hợp Báo cáo Thống kê (`AD01 - AD05`)**:
   - Viết các phương thức tổng hợp trong `AnalyticsService`:
     - `AD01`: Tổng lượt truy cập & ước tính khách duy nhất theo ngày/tuần/tháng.
     - `AD02`: Tổng số lượt nghe bài thuyết minh.
     - `AD03`: Số lượt nghe phân nhóm theo Địa điểm (Location).
     - `AD04`: Số lượt nghe phân nhóm theo Ngôn ngữ (Language).
     - `AD05`: Số lượt nghe phân nhóm theo Cặp `(Location - Language)`.
   - Tạo `AnalyticsController` với quyền `[Authorize(Roles = "Admin")]` cung cấp endpoint `GET /api/admin/analytics/...`.
5. **Đăng ký DI & Xây dựng Dashboard UI**:
   - Khai báo Repository & Service trong file `AnalyticsExtensions.cs`.
   - Viết `admin-analytics.js` hiển thị các thẻ KPI và bảng số liệu trực quan trên tab Thống kê của `admin.html`.

---

### 👤 THÀNH VIÊN 3: Quản trị CRUD Dữ liệu & AI / Storage
- **Mã chức năng phụ trách**: `AD06` → `AD14`
- **Nhánh Git**: `feature/admin-crud-mgmt`
- **File phụ trách chính**:
  - `src/ThuyetMinhDaNgonNgu.API/Controllers/LocationsController.cs`
  - `src/ThuyetMinhDaNgonNgu.API/Controllers/LanguagesController.cs`
  - `src/ThuyetMinhDaNgonNgu.API/Controllers/AdminNarrationsController.cs`
  - `src/ThuyetMinhDaNgonNgu.BLL/Services/LocationService.cs`, `LanguageService.cs`, `NarrationService.cs`
  - `src/ThuyetMinhDaNgonNgu.DAL/Repositories/PostgresLocationRepository.cs`, `PostgresLanguageRepository.cs`
  - `src/ThuyetMinhDaNgonNgu.DAL/Storage/SupabaseAudioStorage.cs`
  - `src/ThuyetMinhDaNgonNgu.API/Extensions/ManagementExtensions.cs`
  - `src/ThuyetMinhDaNgonNgu.API/wwwroot/admin.html`
  - `src/ThuyetMinhDaNgonNgu.API/wwwroot/js/admin-crud.js` *(tạo mới)*

#### Nhiệm vụ cụ thể từng bước:
1. **Hoàn thiện CRUD Địa điểm (`AD06 - AD08`)**:
   - Bổ sung `UpdateAsync` và `DeleteAsync` trong DAL và BLL.
   - Thêm endpoint `PUT /api/locations/{id}` và `DELETE /api/locations/{id}` trong `LocationsController` (yêu cầu quyền `Admin`).
   - Kiểm tra ràng buộc: Không cho phép xóa địa điểm nếu đang có bài thuyết minh hoạt động (BR-12).
2. **Hoàn thiện CRUD Ngôn ngữ (`AD09 - AD11`)**:
   - Bổ sung `UpdateAsync` và `DeleteAsync` trong DAL và BLL.
   - Thêm endpoint `PUT /api/languages/{code}` và `DELETE /api/languages/{code}` trong `LanguagesController` (yêu cầu quyền `Admin`).
   - Kiểm tra ràng buộc: Không cho phép xóa ngôn ngữ nếu đang có bài thuyết minh tham chiếu đến (BR-12).
3. **Hoàn thiện Quản lý Thuyết minh & Storage (`AD12 - AD14`)**:
   - Viết endpoint `DELETE /api/admin/narrations/{locationId}/{languageCode}` (AD14).
   - Khi xóa bản ghi thuyết minh, kiểm tra xem file audio có còn được bản ghi nào khác tham chiếu không; nếu không, gọi `SupabaseAudioStorage` để xóa file audio dọn dẹp bộ nhớ (BR-11).
   - Cập nhật cơ chế sinh Signed URL ngắn hạn cho audio thay vì để public URL cố định.
4. **Đăng ký DI & Xây dựng Giao diện Admin CRUD**:
   - Khai báo các service bổ sung trong file `ManagementExtensions.cs`.
   - Cập nhật giao diện `admin.html`: Bổ sung form Thêm/Sửa/Xóa Địa điểm, form Sửa/Xóa Ngôn ngữ, nút Xóa Thuyết minh (kèm hộp thoại xác nhận modal theo PRD Mục 5.2).
   - Viết toàn bộ logic xử lý gọi API CRUD trong `admin-crud.js`.

---

## 5. Danh mục API Toàn hệ thống (API Reference)

| Phân hệ | Method | Endpoint | Xác thực | Mô tả | Mã CN |
|---|:---:|---|:---:|---|:---:|
| **Client** | `GET` | `/api/locations` | Public | Lấy danh sách địa điểm hiển thị | `CL04` |
| | `GET` | `/api/locations/{id}` | Public | Lấy chi tiết 1 địa điểm | `CL04` |
| | `GET` | `/api/languages` | Public | Lấy danh sách ngôn ngữ hỗ trợ | `CL01, CL02` |
| | `POST` | `/api/narrations` | Public | Lấy bản thuyết minh hiện hành | `CL07` |
| | `POST` | `/api/visits` | Public | Ghi nhận phiên truy cập (chống trùng 30p) | `AD01` |
| | `POST` | `/api/playback` | Public | Ghi nhận lượt phát (khi nghe $\ge$ 15s) | `AD02` |
| **Admin Auth** | `POST` | `/api/admin/auth/login` | Public | Đăng nhập Admin lấy JWT | - |
| **Quản trị Địa điểm** | `POST` | `/api/locations` | Admin | Thêm mới địa điểm | `AD06` |
| | `PUT` | `/api/locations/{id}` | Admin | Cập nhật địa điểm | `AD07` |
| | `DELETE`| `/api/locations/{id}` | Admin | Xóa địa điểm | `AD08` |
| **Quản trị Ngôn ngữ** | `POST` | `/api/languages` | Admin | Thêm mới ngôn ngữ | `AD09` |
| | `PUT` | `/api/languages/{code}` | Admin | Cập nhật tên ngôn ngữ | `AD10` |
| | `DELETE`| `/api/languages/{code}` | Admin | Xóa ngôn ngữ | `AD11` |
| **Quản trị Thuyết minh** | `GET` | `/api/admin/narrations` | Admin | Danh sách các bản thuyết minh | `AD12` |
| | `GET` | `/api/admin/narrations/{locId}/{langCode}` | Admin | Chi tiết 1 bản thuyết minh | `AD12` |
| | `POST` | `/api/admin/narrations/draft` | Admin | Sinh văn bản nháp qua Gemini 3.8 Flash | `AD12` |
| | `PUT` | `/api/admin/narrations/{locId}/{langCode}` | Admin | Tạo giọng nói AI và công bố bài | `AD13` |
| | `DELETE`| `/api/admin/narrations/{locId}/{langCode}` | Admin | Xóa bài thuyết minh & dọn dẹp Storage | `AD14` |
| **Quản trị Thống kê** | `GET` | `/api/admin/analytics/visits` | Admin | Thống kê lượt truy cập & khách duy nhất | `AD01` |
| | `GET` | `/api/admin/analytics/playbacks` | Admin | Tổng lượt phát thuyết minh | `AD02` |
| | `GET` | `/api/admin/analytics/by-location` | Admin | Thống kê lượt phát theo địa điểm | `AD03` |
| | `GET` | `/api/admin/analytics/by-language` | Admin | Thống kê lượt phát theo ngôn ngữ | `AD04` |
| | `GET` | `/api/admin/analytics/by-pair` | Admin | Thống kê lượt phát theo cặp địa điểm-ngôn ngữ | `AD05` |

---

## 6. Kiểm thử & Tiêu chí Nghiệm thu (Acceptance Criteria)

Trước khi tạo Pull Request hợp nhất vào nhánh `main`, các thành viên cần đối chiếu các tiêu chí sau:

- [ ] **Build & Compile**: Chạy `dotnet build` đạt 0 lỗi, 0 cảnh báo.
- [ ] **Bảo mật**: Không commit file chứa mật khẩu thật, chuỗi kết nối hay API Key. Frontend không chứa `ServiceRoleKey`.
- [ ] **Kiến trúc 3 lớp**: Controller không chứa logic nghiệp vụ, DAL không gọi Gemini/TTS.
- [ ] **Quy tắc 30 phút**: F5/reload trang nhiều lần không làm tăng lượt truy cập (`VisitEvent`).
- [ ] **Quy tắc 15 giây**: Chỉ gửi sự kiện `PlaybackEvent` khi trình duyệt đã phát audio liên tục từ 15 giây trở lên.
- [ ] **Toàn vẹn dữ liệu**: Không cho xóa địa điểm hoặc ngôn ngữ nếu đang được tham chiếu bởi bản ghi thuyết minh.
- [ ] **Không sót tính năng ngoài phạm vi**: Trang khách không còn mục lịch sử nghe.
