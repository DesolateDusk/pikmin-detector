# 前端

拾光追蹤的 Angular 網站，提供收集總覽、缺項編輯與附近純點查詢。紀錄保存在目前瀏覽器。

本機開發直接使用 `src/environments/environment.ts`。第一次設定時，若檔案尚未存在，從 `environment.example.ts` 複製一份並填入本機 API 網址；既有設定可直接沿用。

從 frontend 目錄執行：

```powershell
npm ci
npm start
```

本機啟動不讀取環境變數，也不執行設定腳本。`scripts/configure-production.mjs` 只在正式建置時將 `API_BASE_URL` 寫入 `environment.production.ts`，由 Angular 選用；不驗證變數是否存在或網址格式，也不覆寫既有本機設定。public 存放網站圖示等靜態資產。

`npm test` 執行 tracker 業務測試。正式部署的 API 網址由 `API_BASE_URL` 提供：

```powershell
$env:API_BASE_URL = 'https://你的後端服務.onrender.com'
npm run build:pages
```

## 部署到 GitHub Pages

[CI 入口](../.github/workflows/ci.yml) 在每次推送 main 時執行前後端測試，全部通過才呼叫 [Pages workflow](../.github/workflows/deploy-pages.yml) 建置及部署。PR 只測試與建置；main 上也可手動執行 CI。GitHub Pages 接收 `dist/pikmin-detector-pages/browser` 的靜態產物，不使用 Docker image。

部署前完成以下 repository 設定：

1. **Settings → Pages → Build and deployment → Source** 選擇 **GitHub Actions**，讓 workflow 發布網站。
2. **Settings → Secrets and variables → Actions → Variables** 新增 `API_BASE_URL`，值為 Render API 的正式 HTTPS 網址，例如 `https://pikmin-detector-api.onrender.com`。請填入實際網址；若 API 不在網站根目錄，可包含 API base path。既有同名 secret 請移轉為 variable，workflow 已改為讀取 variable。
3. 確認 `github-pages` environment 允許 main 部署；若希望推送後直接部署，不設定 required reviewers。

API URL 是公開的建置設定，會寫入網站 JavaScript，無需由 Doppler 帶入。更改 variable 後需重新執行 main 的 CI 才會更新網站；不可在前端放入資料庫密碼、Doppler token 或 Render API key。

設定腳本只套用提供的值；未設定時會寫入空字串，建置仍可完成，API 請求會使用前端網站的相對路徑。因此部署前仍需在 GitHub 設定正確的後端網址。

網站路徑固定為 `https://desolatedusk.github.io/pikmin-detector/`，建置使用 `/pikmin-detector/` base href；後端 CORS 則設定 `https://desolatedusk.github.io`。網站目前只有根路由，可直接重新整理。若未來增加子路由，需要另外處理 Pages 的 SPA 路由回退。

首次建立 Render 與 Doppler 的順序見[後端部署說明](../backend/README.md)。Render 的後端自動部署會等待同一 commit 的全部 GitHub checks，所以 Pages 的建置與部署設定也必須完成。

未來前端若移到 Render，可以在同一 repository 另建 **Static Site**，指定 `frontend` 為 Root Directory、`frontend/**` 為 build filter，使用 `npm ci && npm run build -- --base-href /` 建置，發布 `dist/pikmin-detector/browser`。API URL 改由該 Static Site 的建置環境提供；Render 網站使用根路徑，需明確指定 `/` base href，取代 Pages 的 `/pikmin-detector/`。後端 Doppler 的 CORS origin 也需改為新網站 origin。屆時將前端部署入口移交 Render，仍可共用 repository 與測試關卡，不必切分專案；目前保留 GitHub Pages 部署。

官方參考：[使用 GitHub Actions 發布 Pages](https://docs.github.com/en/pages/getting-started-with-github-pages/using-custom-workflows-with-github-pages)、[Render monorepo 支援](https://render.com/docs/monorepo-support)、[Render Static Sites](https://render.com/docs/static-sites)。

[專案介紹](../README.md) · [後端](../backend/README.md)
