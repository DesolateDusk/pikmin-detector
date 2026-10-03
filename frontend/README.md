# 前端

拾光追蹤的 Angular 網站，提供收集總覽、缺項編輯與附近純點查詢。紀錄保存在目前瀏覽器。

從 frontend 目錄執行：

```powershell
npm ci
npm run configure:local
npm start
```

scripts 用於本機設定初始化、正式建置設定產生及其測試，均由 package.json 使用。public 存放網站圖示等靜態資產。

測試使用 `npm test`；正式建置使用 `npm run build:pages`。部署入口見 [Pages workflow](../.github/workflows/deploy-pages.yml)。

[專案介紹](../README.md) · [後端](../backend/README.md)
