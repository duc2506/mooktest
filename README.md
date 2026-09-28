# MookTest

Ứng dụng quản lý đề và làm bài: ASP.NET Core / EF Core / SQL Server, Angular 20.

## Chức năng và quyền

- **Trainer**: quản lý tất cả đề, thêm/sửa/xóa câu hỏi và đáp án, xem bài nộp theo đề.
- **Trainee**: xem đề, bắt đầu/tiếp tục lượt làm bài, chọn một/nhiều đáp án, trả lời văn bản, nộp và xem lịch sử của chính mình.
- Đăng ký công khai luôn tạo Trainee. Tạo Trainer bằng lệnh quản trị bên dưới.
- API kiểm tra chữ ký JWT, issuer, audience, thời hạn và vai trò. JWT có hạn 60 phút; mật khẩu dùng PasswordHasher của ASP.NET Core. Frontend dùng sessionStorage và xử lý hết phiên/401.
- Đề đã có lượt làm bài hoặc bài nộp được khóa sửa/xóa để bảo toàn nội dung. Muốn thay đổi, tạo đề mới.
- Đề lựa chọn phải có ít nhất hai đáp án; một lựa chọn/đúng-sai có đúng một đáp án đúng; đúng-sai có đúng hai đáp án. Kiểm tra khi bắt đầu bài.
- Thời gian làm bài do backend quản lý. Tải lại tiếp tục lượt còn hạn; câu trả lời nháp lưu trong tab. Hết hạn không nhận nộp. Có thể bắt đầu lượt mới.
- Hiện lưu và xem câu trả lời, **chưa có chấm điểm tự động**.

## Chạy tại máy

Cần .NET SDK 10, SQL Server và Node.js phù hợp Angular 20. Chạy lệnh từ thư mục gốc D:\cshap1.

1. Kiểm tra ConnectionStrings:DefaultConnection trong backend/MookTest/appsettings.json. Có thể ghi đè bằng biến môi trường ConnectionStrings__DefaultConnection.
2. Khôi phục package và áp dụng migration:

```powershell
dotnet restore backend/MookTest/MookTest.csproj
dotnet run --project backend/MookTest --launch-profile http -- --migrate
```

Migration mới đổi cột Tilte thành Title bằng rename, cho phép AnswerId null, thêm Users và QuizAttempts, thêm liên kết người nộp/lượt làm bài. Dữ liệu cũ được giữ; bài nộp cũ chưa gắn tài khoản chỉ Trainer xem được. Lệnh migrate phải được chạy trước khi dùng phiên bản mới; chạy backend thông thường không tự cập nhật database.

3. Tạo Trainer (script hỏi mật khẩu, không ghi mật khẩu vào file):

```powershell
.\scripts\create-trainer.ps1 -Email trainer@example.com -DisplayName 'Giảng viên'
```

Mật khẩu dài 10–128 ký tự. Không có tài khoản/mật khẩu mặc định. Học viên đăng ký trên giao diện.

4. Chạy backend:

```powershell
dotnet run --project backend/MookTest --launch-profile http
```

API: http://localhost:5243/api. Scalar ở http://localhost:5243/scalar/v1 trong Development. Khi thử API được bảo vệ, gửi Authorization: Bearer <accessToken> nhận từ POST /api/auth/login.

5. Mở terminal khác:

```powershell
cd frontend/MookTestFrontend
npm ci
npm start
```

Mở http://localhost:4200. proxy.conf.json chuyển /api tới backend cổng 5243. Giao diện chạy dạng SPA; build bằng npm run build, kết quả trong dist/MookTestFrontend/browser. Khi triển khai cần reverse proxy /api tới backend và fallback route giao diện về index.html.

## Cấu hình JWT

Development tự sinh khóa ký ngẫu nhiên trong bộ nhớ nếu chưa có Jwt:Key; khởi động lại backend sẽ yêu cầu đăng nhập lại. Có thể đặt Jwt__Key để giữ phiên qua lần khởi động. Production bắt buộc cấu hình khóa bí mật ngẫu nhiên dài ít nhất 32 byte; không đưa khóa vào Git. Issuer mặc định MookTest, audience MookTestFrontend. Cấu hình qua Jwt__Issuer, Jwt__Audience, Jwt__Key.

Frontend không cho tự chọn vai trò. Route guard phục vụ điều hướng; quyền truy cập dữ liệu luôn được kiểm tra tại API. Đăng xuất xóa token ở tab hiện tại; phiên bản này chưa có refresh token hoặc danh sách thu hồi token. Production chạy HTTPS và cấu hình Cors:Origins theo miền frontend nếu truy cập khác origin.

## Kiểm thử

Frontend:

```powershell
cd frontend/MookTestFrontend
npm run build
npm test -- --watch=false --browsers=ChromeHeadless
```

Các test kiểm tra guard, bearer interceptor, hết phiên, lỗi API, chỉnh sửa câu hỏi và payload nộp nhiều đáp án/tự luận.

API checks không cần test framework bổ sung, chạy qua HTTP với backend thật. **Dùng database test riêng** vì các kiểm thử tạo tài khoản, đề và bài nộp. Trong terminal backend, đặt ConnectionStrings__DefaultConnection trỏ database test, Jwt__Key là khóa test; chạy migrate, tạo Trainer rồi khởi động backend. Trong terminal kiểm thử, đặt cùng Jwt__Key và thông tin Trainer:

```powershell
$env:TestApi__Url = 'http://localhost:5243'
$env:Trainer__Email = 'trainer@example.com'
$testPassword = Read-Host 'Test trainer password' -AsSecureString
$env:Trainer__Password = [pscredential]::new('trainer', $testPassword).GetNetworkCredential().Password
try {
    dotnet run --project tests/MookTest.ApiChecks
} finally {
    Remove-Item Env:Trainer__Password
    $testPassword.Dispose()
}
```

Kiểm tra 401/403, không tự cấp quyền Trainer, JWT hết hạn/sai issuer/sai audience, CRUD, che đáp án đúng, tiếp tục lượt làm bài, khóa đề đã dùng, kiểm tra câu trả lời, lưu tự luận/nhiều lựa chọn, chống nộp lại và tách lịch sử giữa các học viên. Dữ liệu kiểm thử được giữ lại trong database test để kiểm tra.

## Tài liệu tham khảo

- [JWT bearer authentication — Microsoft](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-jwt-bearer-authentication?view=aspnetcore-10.0)
- [Angular interceptors](https://angular.dev/guide/http/interceptors)
- [Angular route guards](https://angular.dev/guide/routing/route-guards)

