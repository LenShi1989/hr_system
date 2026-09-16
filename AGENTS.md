# AGENTS.md

人事管理系統 (HR management system). Vue 3 SPA + ASP.NET Web API (EF Core) + PostgreSQL.

- **System design (source of truth)**: `docs/system-design.md` — modules, DB schema, API, frontend structure, milestones.
- **Repo layout**: `backend/` = ASP.NET Web API (.NET 10, Npgsql/EF Core 10), `frontend/` = Vue 3 + TS + Pinia + Vue Router + Axios. 唯一前端相依 UI 套件為 **chart.js**（儀表板圖表，僅 `dashboard/Index.vue` 使用；`import Chart from 'chart.js/auto'`）。其餘 UI 自製，未釘 UI library —— 要再加請先確認。

## 開發指令 (已驗證)

- 後端 build：`dotnet build`（在 `backend/`）
- 後端執行：`dotnet run --launch-profile http` → http://localhost:5106
- Migration：`dotnet ef migrations add <名稱>` / `dotnet ef database update`（global tool `dotnet-ef` 10.x；startup 時會自動 Migrate + seed）
- 前端 dev：`npm install`（首次）→ `npm run dev` → http://127.0.0.1:5173（`/api` proxy 到 5106，見 `frontend/vite.config.ts`）
  - 注意：**新增/修改路由後務必重啟 vite**——路由檔的 HMR 不會重掛 router 實例，執行中的 app 一直用舊路由表。特徵是側欄出現新選單、點下去卻被 catch-all 導回儀表板。
- 前端驗證/typecheck：`npm run build`（`vue-tsc -b && vite build`，先跑這個再交）
- API 除錯：http://localhost:5106/openapi/v1.json（OpenAPI JSON，未掛 Swagger UI）

## 資料庫

- Host: `localhost:5432`，user: `postgres`，database: `hr_system`（已建立）。
- 密碼、JWT secret、種子帳號密碼全部只在 `backend/appsettings.Development.json`，該檔已被 `backend/.gitignore` 排除，**嚴禁** commit 或貼到前端/文件。
- 種子帳號：`admin` / `hr` / `manager` / `employee` 加上 `@hr.local`，密碼見 gitignored 檔案。
- JWT/RBAC：`PermissionCatalog.RoleDefaults`（`backend/Models/PermissionCatalog.cs`）是角色→權限的唯一來源；改權限要在 `DbSeeder` 增刪角色時同步。

## 慣例

- API 統一 `/api/v1` 前綴，回應 `{ "data": ..., "error": { "code", "message" } }`；前端 `src/services/http.ts` 攔截器把 `data` 拆出、401 自動跳登入（登入請求除外）。
- EF Core Migration 放 `backend/Migrations/`，隨程式碼 commit（不含連線字串與密碼）。
- 以後端權限為準：前端路由 `meta.permission` + 守衛只是 UI 層，API 端也要加 `[Authorize]` / permission 檢查。
- 時間欄位用 `DateTimeOffset`（Postgres `timestamptz`），金額用 `numeric(18,2)`（decimal）。
- 登入/認證邏輯在 `TokenService`（JWT）與 `PasswordHasher`（PBKDF2）——新帳號流程沿用這兩個 service。