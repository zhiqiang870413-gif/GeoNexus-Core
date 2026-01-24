GeoNexus Core - Web GIS 設施管理系統
GeoNexus Core 是一款基於 Web GIS 技術的設施管理解決方案。採用 前後端分離 架構，後端基於 ASP.NET Core 9.0 並嚴格遵循 Service Layer (服務層) 設計模式與 JWT 安全驗證，前端則結合 Vue.js 與 Leaflet 提供直觀的地理空間操作介面。

🚀 技術堆疊 (Tech Stack)
後端 (Backend)
框架: .NET 9.0 (ASP.NET Core Web API)

資料庫: PostgreSQL (生產環境) / SQLite (開發環境)

ORM: Entity Framework Core

安全性: JWT (JSON Web Tokens) 身份驗證

架構: 多層次架構 (Controller -> Service -> DBContext)

前端 (Frontend)
框架: Vue.js 3

地圖引擎: Leaflet.js

通訊: Axios (與後端 API 交互)

🏗️ 系統架構亮點

1. 職責分離 (Separation of Concerns)
   專案從傳統的 Controller 操作資料庫轉向 Service Layer 模式：

Controllers: 僅負責 HTTP 請求調度與狀態碼回應。

Services: 封裝業務邏輯，如 GIS 座標範圍計算、點位過濾與權限校驗。

Interfaces: 定義服務契約，實現依賴反轉 (IoC)，大幅提升程式碼的可測試性。

2. 環境適應性資料庫 (Environment-Aware DB)
   系統能自動偵測環境變數：

開發環境: 使用 SQLite 輕量化開發。

雲端環境: 自動解析 DATABASE\_URL 並連接 PostgreSQL。

3. JWT 安全機制
   實現自定義 AuthService 進行身份驗證。

所有設施變動 API (POST/PATCH/DELETE) 均受 \[Authorize] 保護，確保資料安全性。

🛠️ 安裝與運行 (Getting Started)
後端設定
進入後端目錄： cd backend

安裝套件： dotnet restore

啟動專案： dotnet run

預設 API 位址： https://localhost:7154 (請依據你的設定修改)

前端設定
進入前端目錄： cd frontend

安裝依賴： npm install

啟動開發伺服器： npm run dev

