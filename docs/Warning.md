<!-- ## Cảnh báo repo hiện tại

- `Dockerfile` đang build `src/Flex.Apigateway/Flex.Apigateway.csproj` và chạy `Flex.Apigateway.dll`, không khớp service hiện tại. Không dùng Dockerfile này để build Auth service nếu chưa sửa sang `src/Flex.Auth/Flex.Auth.csproj`.
- `Jenkinsfile` đang build/push image `flex-apigateway` và stage build/test đang ghi "Nothing to do". Không xem đây là pipeline production đúng cho Auth service.
- `src/Flex.Auth/Flex.Auth.http` vẫn gọi `/weatherforecast`, endpoint này không tồn tại trong code hiện tại.
- `docs/technical/auth.md` và một số file docs có nội dung thiết kế rộng hơn code hiện tại, bao gồm refresh token, logout, blacklist, multi-tenant, OAuth2, SSO. Phân biệt rõ phần "thiết kế/ý tưởng" với phần đã implement.
- `IdentitySeed` dùng `UserManager<User>` và `RoleManager<Role>`, nhưng service hiện tại chưa đăng ký ASP.NET Core Identity manager đầy đủ trong `AddInfrastructure()`. Kiểm tra trước khi gọi seed.
- `CLAUDE.md` và `src/CLAUDE.md` đang là file untracked theo `git status` tại thời điểm rà soát. -->
