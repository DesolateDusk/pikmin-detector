# 後端

提供圖鑑截圖辨識、純點查詢與點位匯入的 .NET API，供拾光追蹤網站使用。

從儲存庫根目錄執行：

```powershell
dotnet run --project backend/PikminDetector.Api --launch-profile https
dotnet test backend/PikminDetector.Api.Tests/PikminDetector.Api.Tests.csproj
```

本機設定範本為 [appsettings.example.json](PikminDetector.Api/appsettings.example.json)，資料庫初始化檔案為 [Schema.sql](migrations/Schema.sql)。部署入口為 [render.yaml](render.yaml)；自動驗證見 [backend workflow](../.github/workflows/backend.yml)。

[專案介紹](../README.md) · [前端](../frontend/README.md)
