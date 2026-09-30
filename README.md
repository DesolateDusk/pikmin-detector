# pikmin-detector

Angular 前端與 .NET 10 API 位於同一儲存庫。Supabase PostgreSQL／PostGIS 儲存裝飾系列、款式與 pure 點位；API 使用 Dapper 執行參數化查詢，使用 Serilog 記錄執行資訊。

## 啟動

```powershell
dotnet run --project backend/PikminDetector.Api
dotnet test backend/PikminDetector.Api.Tests/PikminDetector.Api.Tests.csproj
```

在新資料庫執行 `supabase/migrations/202609270001_pure_spots.sql`，並以後端環境變數 `ConnectionStrings__Pikmin` 提供 PostgreSQL 連線字串。PostGIS 位於 `gis` schema；migration 會在尚未啟用時安裝到該 schema。連線字串不保存在儲存庫。

## 資料與 API

- `GET /api/spots?country=TW&city=臺北市&area=信義區&limit=50`：依國家、縣市、地區查點位；`city`、`area` 可省略，`area` 須伴隨 `city`。
- `GET /api/spots/nearby?latitude=25.03&longitude=121.56&radiusMeters=3000&decorTypeKey=cafe`：依位置與距離查點位，可加系列 key。距離上限 20 公里，每次最多 100 筆。
- `POST /api/spot-imports`：JSON body 傳入 `country`，可加 `city`／`area` 縮小匯入範圍。臺灣依行政區 JSON 檔名寫入 `area`；海外讀國家 JSON，以來源逐筆 `city` 寫入或篩選。只匯入 `status=pure` 且恰有一個裝飾類型的紀錄。匯入在請求中執行，回應包含新增、更新、移除數及失敗範圍。
- `POST /api/recognitions`：`multipart/form-data` 的 `image` 欄位，接受最多 10 MB 的 PNG／JPEG 圖鑑截圖。使用本機 OCR 讀取卡片標題，對照 `decor_type.name` 的 `zh-TW`／`en` 名稱，並根據 `costume_type.available_types` 與圖鑑格位回傳 `series[].missing[]`，每個缺項有 `decorTypeKey`、`costumeTypeKey`、`pikminType`。沒有完整顯示的卡片會略過。

`202609270001_pure_spots.sql` 一次建立資料表、索引與中英文初始目錄。`available_types` 的陣列順序就是截圖的格位順序；`NULL` 代表尚未確認，辨識器不會推測缺項。辨識第一版只對已提供樣本的圖鑑版型驗證，新的版型需增加樣本再核對。
