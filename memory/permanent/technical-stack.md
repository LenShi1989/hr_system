# 技術棧 (technical-stack)

- 前端：Vue 3 + TypeScript + Pinia + Vue Router + Axios（`frontend/`）
  - 無 UI 庫（未選型，加入前需確認）
  - `npm run build` = `vue-tsc -b && vite build`（typecheck + build，交案前必跑）
  - dev server: http://127.0.0.1:5173，`/api` proxy → http://localhost:5106（`frontend/vite.config.ts`）
- 後端：ASP.NET Web API（.NET 10）+ EF Core 10 + Npgsql（`backend/`）
  - `dotnet run --launch-profile http` → http://localhost:5106
  - OpenAPI JSON: http://localhost:5106/openapi/v1.json
  - 啟動時自動 `MigrateAsync()` + `DbSeeder`
- 資料庫：PostgreSQL `hr_system`（localhost:5432，user `postgres`）
  - 連線字串/密碼只放 `backend/appsettings.Development.json`（gitignored）
  - `dotnet ef migrations add <名>` / `dotnet ef database update`（global tool 10.x）
- 認證：JWT（TokenService）+ PBKDF2（PasswordHasher）
- 時間：`DateTimeOffset` ↔ Postgres `timestamptz`；Npgsql 只接受 offset=0 寫入，
  全專案用 `DateTimeOffsetUtcConverter`（Pre-convention）自動轉 UTC 後才入庫。
- 金額：`numeric(18,2)`（decimal），禁止 float。