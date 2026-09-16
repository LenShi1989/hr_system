# 工作目錄與習慣 (working-directory)

- 根目錄：`D:\Gitlab\hr_system`
- 佈局：`backend/`（API）、`frontend/`（SPA）、`docs/system-design.md`（設計依據）、`memory/`（本記憶系統）、`skills/memory-system/`（技能本體）
- 建置（均在各自目錄執行，勿用 cd）：
  - 後端：`dotnet build`；`dotnet run --launch-profile http`
  - 前端：`npm install`（首次）；`npm run build`（交案驗證）；`npm run dev`
- 啟動注意：
  - build 前先停止執行中的 `HrSystem.Api`，否則 bin 檔被鎖 → MSB3027/MSB3021
  - 背景啟動後端：`Start-Process dotnet -ArgumentList "run","--launch-profile","http" -WorkingDirectory "<backend>"`
  - 等就緒：輪詢 `POST /api/v1/auth/login`（HTTP 200）再測其他 API
  - 每個 bash tool call 是全新 PowerShell session，變數不跨 call；要連續測試請寫在同一支指令
- 驗證習慣：改完「後端」先 build；改完「前端」先 `npm run build`；功能改動都用 seed 帳號跑 API 冒煙測試
- 種子帳號：`admin`/`hr`/`manager`/`employee`@hr.local（密碼在 gitignored `appsettings.Development.json`）
- API 除錯：http://localhost:5106/openapi/v1.json