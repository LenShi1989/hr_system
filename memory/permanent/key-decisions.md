# 關鍵決策與教訓 (key-decisions)

## 決策
- **權限以「後端為準」**：JWT 帶個別 permission claims；`PermissionCatalog.RoleDefaults` 是角色→權限唯一來源；前端路由 `meta.permission` 只是 UI 層。改權限要同步 `DbSeeder` 增刪角色。
- **審核規則**：不可自審；HR/admin 可審任何人；manager 僅直屬部屬（`Employee.ManagerId = 審核者 employee.id`）；`?all=true&status=pending` 一次撈待審清單。
- **薪資計算**（M3）：`PayrollCalculator` 依 `period=YYYYMM` 一次生成所有有薪資結構的在職員工；加班費=核准時數×（底薪/240）×倍率(1.5)；事假扣款=未支薪假天數×（底薪/30）；勞健保/稅為可設定比率（`payroll_settings`）；重跑同月份不覆蓋（跳過已存在）。
- **狀態機**：薪資單 `draft → confirmed → paid`；只有 draft 可改 bonus；confirm/pay 各自獨立端點。
- **時間寫入坑**：Npgsql 10 寫 `timestamptz` 只接受 offset=0；做法是在 `HrDbContext.ConfigureConventions` 用 `DateTimeOffsetUtcConverter` 全欄位轉 UTC（M2 踩到，已在 M2 修復）。
- **前端頁面模式**：`services/<module>.ts`（interface + axios funcs）→ router（permission）→ AdminLayout 依權限動態產選單群 → `views/<module>/` 頁面用 `.card/.toolbar/.table/.tag/.toast` CSS 類（`style.css` 全站共用）；`errorMessage()` 統一做錯誤 toast。
- **回應結構**：`{ data, error:{code,message} }`；前端 `http.ts` 拆出 data、401 跳登入（登入除外）。
- **開發機密可公開（2026-09-17 決定）**：`backend/appsettings.Development.json`（DB 連線、JWT secret、種子密碼）為**開發參數，允許進公開 repo**（該檔已 tracked、`backend/.gitignore` 已移除）；**正式部署務必改用環境變數/Secret Manager 且不得沿用這些值**。若日後要恢復隱藏，需 `git rm --cached` + 加回忽略規則。
- **角色權限編輯持久化（2026-09-17）**：`DbSeeder` 僅在角色「首次建立」時套 `RoleDefaults`，之後 admin 於 UI 的編輯永久保留、不再被啟動流程覆寫；刪除角色採硬刪，但仍綁使用者時回 400。

## 教訓
- backend build 前務必停 HrSystem.Api 進程，否則 exe 被鎖（MSB3027/MSB3021）。
- `datetime-local` 前端 → 後端 ISO：`new Date(value).toISOString()`（預設本地時區）。
- 前端 build 會因未使用變數/函式失敗（TS6133）——測試後移除死碼。
- PowerShell 呼叫 `Start-Process` 啟動 npm 要用 `npm.cmd`。