# 後端

提供圖鑑截圖辨識、純點查詢與點位匯入的 .NET API，供拾光追蹤網站使用。

從儲存庫根目錄執行：

```powershell
dotnet run --project backend/PikminDetector.Api --launch-profile https
dotnet test backend/PikminDetector.Api.Tests/PikminDetector.Api.Tests.csproj
```

截圖辨識需要英文與繁體中文的 Tesseract 語言模型。Docker 與 CI 會安裝 `tesseract-ocr-eng`、`tesseract-ocr-chi-tra`，模型不存放於原始碼庫。本機執行辨識或圖片測試前，請安裝對應模型，並將 `TESSDATA_PREFIX` 設為包含 `eng.traineddata` 與 `chi_tra.traineddata` 的目錄，例如 Windows 的 `C:\Program Files\Tesseract-OCR\tessdata`。測試圖片位於 `PikminDetector.Api.Tests/TestData`。

本機設定範本為 [appsettings.example.json](PikminDetector.Api/appsettings.example.json)，資料庫初始化檔案為 [Schema.sql](migrations/Schema.sql)。

## 部署到 Render

[CI 入口](../.github/workflows/ci.yml) 在每次推送 main 時執行前後端測試；[backend workflow](../.github/workflows/backend.yml) 負責 Linux OCR 與 API 測試，測試全部通過才建置及部署前端 Pages。Render 使用 GitHub repository 的 Dockerfile 建置後端 image，Auto-Deploy 設為 **After CI Checks Pass**，等同一 commit 的 GitHub checks 全部通過後才開始建置、部署及健康檢查。此關卡也包含 Pages 建置與部署，Pages 設定未完成或部署失敗會阻擋後端自動部署。

PR 會執行前後端測試與前端正式建置，不部署。main 上也可手動執行 CI；後端部署結果在 Render 的 Deploys 頁面查看，Actions 通過只代表 CI 成功。Blueprint 的 build filter 只在後端或後端 CI 設定變更時觸發後端部署；前端單獨變更仍會執行共同測試與 Pages 部署。

Docker 使用多階段建置，最終 image 只包含 API publish 產物、ASP.NET runtime 與 Linux OCR 執行依賴。測試專案、圖片、SDK、原始碼、本機設定、debug symbols 及 Windows OCR DLL 不進入最終 image。Dockerfile 在建置階段檢查執行產物與 OCR libraries、模型是否齊全，不啟動 API，也不注入資料庫或 CORS 假設定；正式啟動與 `/health` 檢查由 Render 執行。`/health` 是程序存活檢查，資料庫是否可用仍須透過實際 API 驗收。

在 Render 選擇 **New → Web Service**，連接 GitHub repository `DesolateDusk/pikmin-detector`，設定：

| 欄位 | 值 | 用途 |
|---|---|---|
| Branch | `main` | 正式部署分支。 |
| Language | `Docker` | 由 Render 建置 image。 |
| Root Directory | 留空 | 下列 Docker 路徑從 repository 根目錄解析。 |
| Dockerfile Path | `backend/PikminDetector.Api/Dockerfile` | API 的多階段 Dockerfile。 |
| Docker Build Context Directory | `backend` | 排除前端與其他 repository 內容。 |
| Health Check Path | `/health` | 確認 API 程序已啟動。 |
| Auto-Deploy | `After CI Checks Pass` | CI 成功後才自動建置及部署。 |
| Build Filter：Included Paths | `backend/**`、`.github/workflows/ci.yml`、`.github/workflows/backend.yml` | 後端與其 CI 變更才觸發後端部署。 |

也可使用 [render.yaml](render.yaml) 建立 Blueprint，檔案路徑指定 `backend/render.yaml`，其中已設定 `runtime: docker`、`branch: main` 與 `autoDeployTrigger: checksPass`。image 由 Render 建置與保存，GitHub Actions 不需要 GHCR、registry PAT、`RENDER_API_KEY` 或 `RENDER_SERVICE_ID`。

後端環境設定使用 **Doppler → Integrations → Render**，選擇正式 config 與上述 service，同步以下值：

| 名稱 | 值與用途 |
|---|---|
| `CONNECTIONSTRINGS__PIKMIN` | 正式 PostgreSQL 連線字串，包含 Host、Database、Username、Password 與資料庫供應商要求的 TLS 設定。 |
| `ALLOWEDORIGINS__0` | `https://desolatedusk.github.io`；CORS origin 不含 `/pikmin-detector/` 路徑或尾端斜線。若使用自訂網域，改成該網域的 HTTPS origin。 |

`ASPNETCORE_ENVIRONMENT=Production`、OCR 模型位置與預設 HTTP port 已由 image 提供；Render 的 `PORT` 由應用程式讀取，不需 Doppler 重複設定。Doppler integration 仍需在 Doppler 中設定 Render API key，供它同步 Render environment variables；這把 key 不提供給 GitHub Actions。image 與 CI 不需要 Doppler CLI 或 `DOPPLER_TOKEN`。

首次初始化順序：

1. 準備 PostgreSQL／PostGIS，執行 `migrations/Schema.sql` 建立資料表與圖鑑系列、款式初始資料；純點需要透過既有匯入功能另外載入。確認資料庫連線與初始資料可用，再設定 Doppler 正式 config。
2. 將部署設定推送到 GitHub，從 repository 建立 Render Web Service 或 Blueprint。建立服務不需預先提供 image。服務建立後即可取得 Render 指派的 HTTPS 網址；尚未同步 Doppler 時，初次啟動可能失敗，這不影響服務與網址的建立。
3. 把該服務連接到 Doppler 正式 config，確認資料庫與 CORS 設定已同步。將 Render HTTPS 網址填入 GitHub repository variable `API_BASE_URL`，並依前端 README 啟用 GitHub Pages。
4. 在 main 手動執行 **Test and deploy application**，或重跑先前缺少 API URL 而失敗的 CI。確認同一 commit 的 checks 全部通過，再於 Render 執行一次 **Manual Deploy → Deploy latest commit**，完成初次正式部署。
5. 驗證前端上傳辨識及純點查詢。此後 main 的後端變更會在 CI 成功後由 Render 自動建置部署；前端變更由 Actions 部署 Pages。

若 Render 已有 service，確認它使用 GitHub repository 的 Docker runtime，部署分支與 Auto-Deploy 符合上表，再完成 Doppler 同步與前端設定。設定參數改變後，依需要重跑 Pages CI 或手動部署後端。

官方設定參考：[Render Docker 部署](https://render.com/docs/docker)、[CI 通過後部署](https://render.com/docs/deploys#integrating-with-ci)、[Doppler Render integration](https://docs.doppler.com/docs/render)。

[專案介紹](../README.md) · [前端](../frontend/README.md)
