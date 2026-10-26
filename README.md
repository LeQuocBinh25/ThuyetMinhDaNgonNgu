# VietGuide — Hệ thống Thuyết minh Du lịch Đa ngôn ngữ

README này là hướng dẫn làm việc chung cho nhóm 3 thành viên. Mục tiêu là xây dựng hệ thống theo ngữ cảnh PRD, chia phạm vi file rõ ràng và giảm tối đa xung đột khi phát triển song song.

> Quy trình này giúp **giảm** xung đột, không thể đảm bảo tuyệt đối không có xung đột. Khi cần sửa file dùng chung, thành viên phải phối hợp với người phụ trách file trước khi thay đổi.

## 1. Ngữ cảnh và nguyên tắc nghiệp vụ

VietGuide cung cấp nội dung thuyết minh du lịch đa ngôn ngữ trên web, kết hợp danh sách địa điểm, bản đồ, GPS, audio tạo bằng AI và khu vực quản trị.

- **API:** ASP.NET Core trên .NET 10.
- **Kiến trúc:** API → BLL (nghiệp vụ) → DAL (EF Core/PostgreSQL, AI và Storage).
- **Frontend:** HTML, CSS, JavaScript và Leaflet.
- **Dữ liệu/Storage:** PostgreSQL và Supabase Storage. Audio phải được lưu riêng tư; Backend cấp signed URL ngắn hạn hoặc proxy audio, không phát URL public của bucket.
- **AI:** Gemini cho tạo nội dung và TTS; tên model lấy từ cấu hình triển khai, không ghi API key vào mã nguồn.
- **Triển khai:** Docker/Render theo cấu hình môi trường của dự án.

Các yêu cầu nghiệp vụ phải giữ đúng:

1. Chống đếm trùng lượt truy cập trong cửa sổ **30 phút**.
2. Chỉ tính lượt nghe hợp lệ khi audio thực sự phát liên tục tối thiểu **15 giây**.
3. Mỗi cặp `(LocationId, LanguageCode)` có tối đa **một bản thuyết minh hiện hành**.
4. Bucket audio luôn **Private**; không đưa `ServiceRoleKey` hoặc thông tin bí mật xuống trình duyệt.
5. Tính năng lịch sử nghe cá nhân nằm ngoài phạm vi; không xây dựng hoặc lưu lịch sử nghe vào `localStorage`.
6. Khách vẫn có thể chọn địa điểm/ngôn ngữ thủ công nếu GPS bị từ chối hoặc không khả dụng.
7. Không xóa địa điểm/ngôn ngữ đang được bản thuyết minh tham chiếu; việc dọn audio phải kiểm tra không còn bản ghi nào dùng object đó.

## 2. Chuẩn bị môi trường

### 2.1. Cấu hình bí mật

Không commit connection string, mật khẩu, JWT signing key, Gemini API key hay Supabase service-role key. Khởi tạo .NET User Secrets một lần cho dự án API:

```powershell
dotnet user-secrets init --project .\src\ThuyetMinhDaNgonNgu.API\ThuyetMinhDaNgonNgu.API.csproj
```

Sau đó đặt giá trị secret (thực hiện tại thư mục gốc repository):

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<postgres-connection-string>" --project .\src\ThuyetMinhDaNgonNgu.API\ThuyetMinhDaNgonNgu.API.csproj
dotnet user-secrets set "Gemini:ApiKey" "<gemini-api-key>" --project .\src\ThuyetMinhDaNgonNgu.API\ThuyetMinhDaNgonNgu.API.csproj
dotnet user-secrets set "Supabase:Url" "https://<project>.supabase.co" --project .\src\ThuyetMinhDaNgonNgu.API\ThuyetMinhDaNgonNgu.API.csproj
dotnet user-secrets set "Supabase:ServiceRoleKey" "<service-role-key>" --project .\src\ThuyetMinhDaNgonNgu.API\ThuyetMinhDaNgonNgu.API.csproj
dotnet user-secrets set "Admin:Username" "<local-admin-username>" --project .\src\ThuyetMinhDaNgonNgu.API\ThuyetMinhDaNgonNgu.API.csproj
dotnet user-secrets set "Admin:Password" "<local-admin-password>" --project .\src\ThuyetMinhDaNgonNgu.API\ThuyetMinhDaNgonNgu.API.csproj
dotnet user-secrets set "Jwt:SigningKey" "<secret-at-least-32-bytes>" --project .\src\ThuyetMinhDaNgonNgu.API\ThuyetMinhDaNgonNgu.API.csproj
```

Không dùng secrets thật trong ví dụ, ảnh chụp màn hình, issue, log hoặc pull request.

### 2.2. Restore, build và chạy

```powershell
dotnet restore .\ThuyetMinhDaNgonNgu.sln
dotnet build .\ThuyetMinhDaNgonNgu.sln
dotnet run --launch-profile https --project .\src\ThuyetMinhDaNgonNgu.API\ThuyetMinhDaNgonNgu.API.csproj
```

- Giao diện khách: `https://localhost:7138/index.html`
- Giao diện quản trị: `https://localhost:7138/admin.html`

API hiện migrate database khi khởi động. Chỉ chạy với database local/dev mà nhóm được phép sử dụng; không tự ý chạy migration hoặc thử nghiệm xóa dữ liệu trên database dùng chung/production.

## 3. Quy ước GitHub dùng chung

### 3.1. Nhánh và quy tắc cơ bản

- Bảo vệ nhánh `master`: không làm việc trực tiếp, không force-push và không tự ý push commit lên nhánh này.
- Mỗi thành viên tạo feature branch riêng từ base branch mới nhất:
  - TV1: `feature/client-experience`
  - TV2: `feature/analytics-events`
  - TV3: `feature/admin-crud`
- Một nhánh chỉ phục vụ một phạm vi công việc. Không dùng chung một feature branch giữa nhiều thành viên.
- Commit nhỏ, có nội dung rõ ràng; không commit `bin/`, `obj/`, file IDE tạm, file xuất dữ liệu hay secrets.

### 3.2. Clone repository lần đầu

Thực hiện một lần trên máy mỗi thành viên:

```powershell
git clone <github-repository-url>
Set-Location .\ThuyetMinhDaNgonNgu
git switch test
git pull --ff-only origin test
```

Nếu base branch của nhóm đã đổi, thay `test` bằng tên nhánh được thống nhất. Nếu đã clone repository, không clone lần nữa; vào thư mục repository và kiểm tra `git status`.

### 3.3. Bắt đầu một nhánh công việc

Trước mỗi task, cập nhật base branch rồi tạo/đổi sang feature branch của mình:

```powershell
git status
git switch test
git pull --ff-only origin test
git switch -c feature/client-experience
```

Thay tên nhánh theo thành viên. Nếu feature branch đã tồn tại trên máy:

```powershell
git switch feature/client-experience
git fetch origin
git rebase origin/test
```

Chỉ rebase khi thay đổi local đã được commit hoặc đã được cất an toàn. Không dùng `reset --hard` để xử lý thay đổi chưa rõ nguồn gốc.

### 3.4. Commit và push lên GitHub

Trước khi commit, xem chính xác file nào sẽ được đưa lên:

```powershell
git status
git diff
git diff --check
git add <cac-file-thuoc-task>
git diff --cached
git commit -m "feat(client): add browser language selection"
git push -u origin feature/client-experience
```

Quy ước commit gợi ý: `feat`, `fix`, `test`, `docs`, `refactor`, tiếp theo là phạm vi, ví dụ `feat(analytics): record qualified playback`. Không dùng `git add .` nếu chưa kiểm tra worktree.

### 3.5. Tạo Pull Request

1. Push feature branch của mình lên GitHub.
2. Tạo Pull Request (**PR**) từ feature branch vào base branch đã thống nhất.
3. Mô tả chức năng/PRD ID, file hoặc migration bị ảnh hưởng, cách kiểm thử, biến cấu hình mới và các quyết định API/schema.
4. Yêu cầu thành viên khác review; xử lý review trên chính feature branch.
5. Chỉ merge khi build/test đạt, không còn review bắt buộc và PR không chứa thay đổi ngoài phạm vi.
6. Ưu tiên **Squash and merge** để lịch sử nhánh tích hợp dễ đọc, trừ khi nhóm thống nhất cách khác.
7. Sau khi PR được merge, đồng bộ nhánh local:

```powershell
git switch test
git pull --ff-only origin test
```

### 3.6. Đồng bộ nhánh và giải quyết conflict

Đồng bộ feature branch với thay đổi mới trên base branch trước khi mở PR hoặc khi PR báo conflict:

```powershell
git fetch origin
git switch feature/client-experience
git rebase origin/test
```

Nếu xảy ra conflict:

1. Dừng lại, đọc `git status` và xác định file bị conflict.
2. Với file do thành viên khác sở hữu, liên hệ người phụ trách trước khi chọn nội dung hoặc sửa tiếp.
3. Mở từng file, kết hợp thay đổi có chủ đích và xóa các marker `<<<<<<<`, `=======`, `>>>>>>>`.
4. Chạy build/test liên quan, rồi tiếp tục:

```powershell
git add <file-da-giai-quyet>
git rebase --continue
git push --force-with-lease origin feature/client-experience
```

Chỉ dùng `--force-with-lease` để cập nhật **feature branch của chính mình sau rebase**; không force-push base branch hoặc branch của người khác. Nếu không chắc cách xử lý, dừng rebase và nhờ người sở hữu file hỗ trợ; không chọn đại một phía.

## 4. Phân công thành viên và phạm vi file

Quyền sở hữu dưới đây là quy tắc điều phối để hạn chế sửa cùng file. Thành viên khác chỉ thay đổi file thuộc người khác khi đã trao đổi, thống nhất cách tích hợp và có review của người sở hữu.

### Thành viên 1 — Trải nghiệm khách `CL01–CL07`

**Nhánh:** `feature/client-experience`

**Phụ trách chính:**

- `src/ThuyetMinhDaNgonNgu.API/wwwroot/index.html`
- `src/ThuyetMinhDaNgonNgu.API/wwwroot/js/app.js`
- `src/ThuyetMinhDaNgonNgu.API/wwwroot/css/style.css`
- Client-facing flow trong `src/ThuyetMinhDaNgonNgu.API/Controllers/NarrationsController.cs`
- DTO request/response riêng cho API công khai của khách, sau khi thống nhất hợp đồng với TV2 khi có liên quan đến analytics.

**Luồng công việc:**

1. Xóa hoàn toàn mục/lưu trữ lịch sử nghe cá nhân khỏi HTML và JavaScript.
2. Tải danh sách ngôn ngữ, tự chọn theo `navigator.language` nếu được hỗ trợ; nếu không, dùng `vi` nếu có, rồi cho phép người dùng tự chọn.
3. Đảm bảo đổi địa điểm/ngôn ngữ không tiếp tục phát audio cũ hoặc hiển thị nhầm kết quả.
4. Dùng GPS khi khách chủ động yêu cầu; tính khoảng cách Haversine và thông báo địa điểm trong ngưỡng 50 m. Nếu GPS lỗi/từ chối, giữ luồng chọn thủ công.
5. Gọi API visits theo hợp đồng của TV2 khi mở trang; không tự tạo schema hoặc controller analytics.
6. Chỉ gửi sự kiện playback sau khi audio thực sự phát liên tục đủ 15 giây; reset bộ đếm khi audio pause, seek, kết thúc hoặc đổi bản thuyết minh. Hợp đồng request phải được thống nhất với TV2.
7. Kiểm thử giao diện, các trạng thái lỗi mạng/GPS/audio và xác nhận không còn key/lịch sử nghe cá nhân.

**Không tự ý sửa:** `Program.cs`, `AppDbContext.cs`, migrations, entities analytics, controller/service analytics, `admin.html`, `admin.js`.

### Thành viên 2 — Schema, sự kiện và thống kê `AD01–AD05`

**Nhánh:** `feature/analytics-events`

**Phụ trách chính:**

- Entity và persistence analytics mới trong `src/ThuyetMinhDaNgonNgu.DAL/Entities/`, `Persistence/AppDbContext.cs`, `Persistence/Migrations/` và repository analytics mới.
- Bổ sung các trường audit/active vào `LocationEntity` và `LanguageEntity` theo PRD, cùng migration tương ứng.
- Interface/model/service analytics mới trong `src/ThuyetMinhDaNgonNgu.BLL/`.
- Controller và DTO mới cho visit, playback và báo cáo analytics trong `src/ThuyetMinhDaNgonNgu.API/`.
- `src/ThuyetMinhDaNgonNgu.API/Extensions/AnalyticsExtensions.cs`
- Trang analytics độc lập để tránh sửa cùng giao diện CRUD của TV3:
  - `src/ThuyetMinhDaNgonNgu.API/wwwroot/analytics.html` (nếu cần)
  - `src/ThuyetMinhDaNgonNgu.API/wwwroot/js/admin-analytics.js`
  - `src/ThuyetMinhDaNgonNgu.API/wwwroot/css/admin-analytics.css` (nếu cần)

**Luồng công việc:**

1. Trước khi code, chốt với TV1/TV3 hợp đồng event, tên trường và thay đổi entity/migration cần cho CRUD.
2. Ưu tiên PR nền tảng schema/migration riêng. Thông báo nhóm và chờ PR này được merge trước khi các nhánh khác phụ thuộc vào schema mới.
3. Tạo API visit với cửa sổ chống đếm trùng 30 phút; xác định rõ khóa nhận diện/ưu tiên visitor token và cách xử lý IP theo PRD.
4. Tạo API playback, xác thực dữ liệu và điều kiện thời lượng tối thiểu 15 giây theo hợp đồng đã thống nhất.
5. Tạo các báo cáo tổng lượt truy cập/khách ước tính và lượt nghe tổng hợp theo ngày, địa điểm, ngôn ngữ, cặp địa điểm–ngôn ngữ. Bảo vệ endpoint báo cáo bằng quyền Admin.
6. Đăng ký repository/service/controller liên quan trong `AnalyticsExtensions.cs`, không sửa `Program.cs`.
7. Làm giao diện analytics trong file riêng; nhờ TV3 thêm liên kết từ trang quản trị sau khi thống nhất để tránh cùng sửa `admin.html`.
8. Viết/ chạy kiểm thử cho dedup 30 phút, ngưỡng 15 giây, báo cáo theo khoảng thời gian và phân quyền Admin.

**Không tự ý sửa:** `Program.cs`, `admin.html`, `admin.js`, `admin.css`, các controller/CRUD service của TV3. `AppDbContext.cs`, entities và migrations do TV2 sở hữu; TV3 gửi yêu cầu schema cho TV2 thay vì tự tạo migration.

### Thành viên 3 — Quản trị CRUD, narration và storage `AD06–AD14`

**Nhánh:** `feature/admin-crud`

**Phụ trách chính:**

- `src/ThuyetMinhDaNgonNgu.API/Controllers/LocationsController.cs`
- `src/ThuyetMinhDaNgonNgu.API/Controllers/LanguagesController.cs`
- `src/ThuyetMinhDaNgonNgu.API/Controllers/AdminNarrationsController.cs`
- DTO quản trị mới trong `src/ThuyetMinhDaNgonNgu.API/DTOs/`
- `src/ThuyetMinhDaNgonNgu.BLL/Interfaces/` và `Services/` liên quan CRUD địa điểm/ngôn ngữ/thuyết minh
- `src/ThuyetMinhDaNgonNgu.DAL/Repositories/` liên quan CRUD
- `src/ThuyetMinhDaNgonNgu.DAL/Storage/SupabaseAudioStorage.cs` và hợp đồng storage cần thiết
- `src/ThuyetMinhDaNgonNgu.API/Extensions/ManagementExtensions.cs`
- Giao diện quản trị hiện có:
  - `src/ThuyetMinhDaNgonNgu.API/wwwroot/admin.html`
  - `src/ThuyetMinhDaNgonNgu.API/wwwroot/js/admin.js`
  - `src/ThuyetMinhDaNgonNgu.API/wwwroot/css/admin.css`

**Luồng công việc:**

1. Trao đổi với TV2 về các trường `IsActive`, thời gian tạo/cập nhật và ràng buộc dữ liệu cần migration; không tự sửa `AppDbContext` hoặc tạo migration song song.
2. Hoàn thiện endpoint Admin tạo/sửa/xóa địa điểm và ngôn ngữ; giữ API đọc cần thiết cho khách. Không xóa dữ liệu còn được narration tham chiếu.
3. Hoàn thiện đọc/tạo nháp/công bố/xóa narration. Duy trì một bản hiện hành cho mỗi cặp địa điểm–ngôn ngữ.
4. Chuyển audio sang bucket Private và signed URL ngắn hạn hoặc proxy backend. Khi thay/xóa narration, chỉ xóa object audio sau khi xác nhận không còn bản ghi nào tham chiếu.
5. Cập nhật UI hiện tại trong `admin.html`, `admin.js`, `admin.css`; không nhúng phần dashboard analytics của TV2 vào các file này nếu chưa phối hợp.
6. Kiểm thử quyền Admin, validate dữ liệu, xung đột/tồn tại bản ghi, xử lý lỗi Storage và trường hợp file audio còn được tham chiếu.

**Không tự ý sửa:** `Program.cs`, `AppDbContext.cs`, migrations, entities analytics, `AnalyticsExtensions.cs`, controller/service analytics hoặc các file trang khách thuộc TV1.

## 5. File dùng chung và quy tắc tích hợp

| File/phạm vi | Chủ sở hữu | Quy tắc |
|---|---|---|
| `Program.cs` | Người tích hợp/nhóm cùng quản lý | Không thêm DI trực tiếp cho từng tính năng. Dùng `AnalyticsExtensions.cs` và `ManagementExtensions.cs`; chỉ người tích hợp xử lý thay đổi khởi động dùng chung. |
| `AppDbContext.cs`, migrations, entity schema | TV2 | Chỉ một thành viên tạo migration tại một thời điểm. TV3/TV1 gửi đề xuất schema cho TV2; migration phải được review và merge trước khi nhánh phụ thuộc dùng. |
| `LocationEntity.cs`, `LanguageEntity.cs` | TV2 quản lý schema; TV3 yêu cầu thay đổi | Thảo luận trước khi sửa. Tránh hai nhánh cùng thay đổi entity/model snapshot. |
| `admin.html`, `admin.js`, `admin.css` | TV3 | TV2 xây dashboard analytics bằng file riêng. TV3 tích hợp liên kết/nút vào màn hình quản trị trong commit riêng. |
| Hợp đồng API event | TV2 định nghĩa; TV1 tích hợp client | Chốt URL, request, response và ngưỡng nghiệp vụ trước khi code hai phía. |
| `Extensions/AnalyticsExtensions.cs` | TV2 | Chỉ đăng ký dịch vụ analytics/event. |
| `Extensions/ManagementExtensions.cs` | TV3 | Chỉ đăng ký dịch vụ quản trị/CRUD. |

Không chia đôi một file cho hai nhánh chạy song song. Nếu thay đổi buộc phải chạm file do thành viên khác sở hữu:

1. Trao đổi trước và thống nhất ai thực hiện phần thay đổi.
2. Người sở hữu file tạo commit/PR thay đổi đó hoặc review patch của người yêu cầu.
3. Các nhánh khác cập nhật từ base branch sau khi PR được merge.

## 6. Thứ tự làm việc và tích hợp đề xuất

1. **Kickoff:** cả nhóm thống nhất base branch, phân công, API event, trường dữ liệu, quy ước response và acceptance criteria.
2. **Nền tảng dữ liệu:** TV2 cập nhật entity/schema và mở PR migration nền tảng. Nhóm review/build; merge PR trước khi các nhánh phụ thuộc bắt đầu tích hợp database.
3. **Phát triển song song:** TV1 triển khai client; TV3 triển khai CRUD/admin/storage; TV2 triển khai event/analytics dựa trên schema đã được merge. Mỗi người làm trên feature branch riêng.
4. **PR theo phần độc lập:** mở PR nhỏ theo chức năng. PR có migration hoặc thay đổi hợp đồng API phải được review trước PR phía client sử dụng hợp đồng đó.
5. **Tích hợp UI analytics:** TV2 giữ dashboard ở file riêng; sau khi PR analytics được merge, TV3 thêm liên kết từ trang Admin nếu cần.
6. **Kiểm thử tích hợp:** cập nhật cả nhóm lên base branch mới nhất, build/test, chạy thử client → API → database/storage và Admin → API.
7. **Nghiệm thu:** chỉ đánh dấu hoàn thành các yêu cầu đã được kiểm thử thực tế; không coi tài liệu hoặc stub/TODO là tính năng đã hoàn thành.

## 7. API mục tiêu cần thống nhất

Bảng dưới đây là hợp đồng mục tiêu theo ngữ cảnh PRD; một số endpoint có thể **chưa được triển khai** trong code hiện tại. Trước khi đổi URL hoặc DTO, TV1 và TV2 phải thống nhất request/response qua issue hoặc PR.

| Method | Endpoint | Quyền | Mục đích |
|---|---|---|---|
| `GET` | `/api/locations`, `/api/locations/{id}` | Public | Danh sách/chi tiết địa điểm |
| `GET` | `/api/languages` | Public | Danh sách ngôn ngữ hỗ trợ |
| `POST` | `/api/narrations` | Public | Lấy narration hiện hành cho địa điểm/ngôn ngữ |
| `POST` | `/api/visits` | Public | Ghi nhận visit; chống đếm trùng 30 phút |
| `POST` | `/api/playback` | Public | Ghi nhận playback đủ điều kiện 15 giây |
| `POST` | `/api/admin/auth/login` | Public | Đăng nhập Admin, nhận JWT |
| `POST`, `PUT`, `DELETE` | `/api/locations...` | Admin | CRUD địa điểm |
| `POST`, `PUT`, `DELETE` | `/api/languages...` | Admin | CRUD ngôn ngữ |
| `GET`, `POST`, `PUT`, `DELETE` | `/api/admin/narrations...` | Admin | Xem, tạo nháp, công bố, xóa narration |
| `GET` | `/api/admin/analytics/...` | Admin | Báo cáo visits và playbacks |

Mọi route Admin phải xác thực/ủy quyền phía server; ẩn nút trên giao diện không thay thế cho kiểm tra quyền API.

## 8. Checklist trước khi tạo/cập nhật PR

- [ ] Nhánh bắt đầu từ base branch mới nhất; không commit trực tiếp lên base branch.
- [ ] `git status`, `git diff` và staged diff chỉ gồm file thuộc task; không có secrets.
- [ ] Chạy `git diff --check`.
- [ ] Build solution: `dotnet build .\ThuyetMinhDaNgonNgu.sln`.
- [ ] Chạy test liên quan; nếu chưa có test tự động, mô tả rõ các bước kiểm thử thủ công trong PR.
- [ ] Controller chỉ điều phối HTTP; nghiệp vụ nằm trong BLL; DAL không gọi trực tiếp AI/TTS.
- [ ] Không có lịch sử nghe cá nhân trên trang khách hoặc trong local storage.
- [ ] Audio không được truy cập bằng public bucket/URL; không có service-role key ở frontend.
- [ ] Visit dedup 30 phút và playback tối thiểu 15 giây được kiểm thử.
- [ ] Ràng buộc tham chiếu khi xóa địa điểm/ngôn ngữ/narration và dọn Storage đã được kiểm thử.
- [ ] Migration chỉ được tạo bởi người phụ trách schema, có tên rõ ràng và kiểm tra model snapshot.
- [ ] PR mô tả mã chức năng, thay đổi API/schema, cấu hình mới và kết quả kiểm thử.
