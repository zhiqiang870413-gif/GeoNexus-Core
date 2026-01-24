<template>
  <div class="dashboard">
    <header class="top-bar">
      <div class="logo">🛰️ 全市設施動態監控系統</div>
      <div class="auth-info">
        <span class="user-badge">管理員模式</span>
      </div>
    </header>

    <div class="admin-bar">
      <button v-if="!isLoggedIn" @click="showLoginModal = true" class="btn-login">管理員登入</button>
      <div v-else class="admin-status">
        <span>👨‍💻 已登入</span>
        <button @click="logout" class="btn-logout">登出</button>
      </div>
    </div>

    <div v-if="showLoginModal" class="modal-overlay" @click.self="showLoginModal = false">
      <div class="modal-card">
        <h2>GeoNexus 管理系統</h2>
        <div class="input-group">
          <input v-model="loginForm.Username" placeholder="管理員帳號" />
          <input v-model="loginForm.Password" type="password" placeholder="密碼" @keyup.enter="handleLogin" />
        </div>
        <button @click="handleLogin" class="btn-submit">確認登入</button>
      </div>
    </div>

    <div class="main-content">
      <aside class="sidebar">
        <div class="search-box">
          <input v-model="searchText" placeholder="搜尋設施名稱..." />
          <div class="stats">
            區域內顯示: <strong>{{ filteredVendors.length }}</strong> 筆設施
          </div>
        </div>

        <div class="list-container">
          <template v-if="filteredVendors.length > 0">
            <div 
              v-for="v in filteredVendors" 
              :key="v.id" 
              class="vendor-card"
              :class="{ active: selectedId === v.id }"
              @click="focusOnVendor(v)"
            >
              <div class="card-header">
                <span :class="['status-dot', v.status]"></span>
                <h3>{{ v.name }}</h3>
                <button @click.stop="deleteVendor(v.id)" class="btn-delete" title="刪除設施">
                  🗑️
                </button>
              </div>

              <div class="card-body">
                <p class="type-tag">{{ v.type }}</p>
                <p class="coords">📍 {{ v.lat.toFixed(4) }}, {{ v.lng.toFixed(4) }}</p>
              </div>

              <div class="card-actions">
                <button @click.stop="updateStatus(v.id, 'success')" class="btn-status s">正常</button>
                <button @click.stop="updateStatus(v.id, 'warning')" class="btn-status w">警告</button>
                <button @click.stop="updateStatus(v.id, 'error')" class="btn-status e">危險</button>
              </div>
            </div>
          </template>

          <div v-else class="no-data-hint">
            <div class="hint-icon">🔍</div>
            <h4>此範圍內無設施</h4>
            <p>請嘗試移動地圖，或在地圖上點擊右鍵新增標註。</p>
          </div>
        </div>
      </aside>

      <section class="map-container">
        <div id="map"></div>
        <div class="map-overlay">
          <p>💡 提示：拖動地圖會自動更新左側列表</p>
        </div>
      </section>
    </div>
  </div>
</template>

<script setup>
// 自動判定：如果在生產環境(Render)就用雲端網址，否則用本地網址
const isProd = import.meta.env.PROD;
const API_BASE_URL = isProd 
  ? "https://你的後端專案名稱.onrender.com/api" 
  : "http://localhost:10000/api";

import { ref, computed, onMounted, reactive} from 'vue';
import axios from 'axios';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';

// 引入叢集插件的 JS
import 'leaflet.markercluster';
// 引入叢集插件的 CSS
import 'leaflet.markercluster/dist/MarkerCluster.css';
import 'leaflet.markercluster/dist/MarkerCluster.Default.css';

// --- 響應式變數 ---
const searchText = ref("");
const vendors = ref([]);
const searchQuery = ref('');
const selectedId = ref(null);
const apiStatus = ref('連線中...');
const statusClass = ref('text-warning');
let map = null;
let markerGroup = null; // 用來統一管理地圖標記
let clusterGroup;



// --- 計算屬性：搜尋過濾 ---
const filteredVendors = computed(() => {
  if (!searchText.value) {
    return vendors.value;
  }
  return vendors.value.filter(v => 
    v.name.toLowerCase().includes(searchText.value.toLowerCase()) ||
    v.type.toLowerCase().includes(searchText.value.toLowerCase())
  );
});

const errorCount = computed(() => {
  return vendors.value.filter(v => v.status === 'error' || v.status === 'warning').length;
});

// --- 地圖邏輯 ---
const initMap = () => {
  // 初始化地圖，預設中心點在台北
  map = L.map('map', {
    zoomControl: false // 隱藏預設縮放鈕，稍後自訂位置
  }).setView([25.0478, 121.5170], 13);

  // --- 新增：地圖點擊事件 ---
  map.on('click', async (e) => {
    const { lat, lng } = e.latlng;
    const name = prompt("發現新設施！請輸入名稱：");
    
    if (name) {
      await addNewPoint(name, lat, lng);
    }
  });

  map.on('moveend', () => {
    const bounds = map.getBounds();
    fetchData({
      minLat: bounds.getSouth(),
      maxLat: bounds.getNorth(),
      minLng: bounds.getWest(),
      maxLng: bounds.getEast()
    });
  });

  // 加入地圖圖層 (OpenStreetMap)
  L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
    attribution: '© OpenStreetMap contributors'
  }).addTo(map);

  // 加入自訂縮放按鈕到右下角
  L.control.zoom({ position: 'bottomright' }).addTo(map);

  // 建立一個 LayerGroup，方便後續清除與重新繪製 Marker
  //markerGroup = L.layerGroup().addTo(map);
  clusterGroup = L.markerClusterGroup().addTo(map);

  // 修正容器渲染問題
  setTimeout(() => { map.invalidateSize(); }, 400);
};

// --- 2. 登入狀態管理 ---
const isLoggedIn = ref(!!localStorage.getItem('userToken'));
const showLoginModal = ref(false);
const loginForm = reactive({ Username: '', Password: '' });

// --- 3. 升級後的 fetchData ---
const fetchData = async (boundsParams = null) => {
  try {
    let params = {};

    // 判斷邊界邏輯 (保留你原本的邏輯)
    if (boundsParams) {
      params = boundsParams;
    } else if (map.value) {
      const bounds = map.value.getBounds();
      params = {
        minLat: bounds.getSouth(),
        maxLat: bounds.getNorth(),
        minLng: bounds.getWest(),
        maxLng: bounds.getEast()
      };
    }

    // 發送請求 (攔截器會自動幫你帶 Token)
    const response = await axios.get(`${API_BASE_URL}/vendors`, { params });

    vendors.value = response.data;
    
    // 更新地圖標記
    if (typeof updateMarkers === 'function') {
      updateMarkers();
    }

    console.log("回傳數量:", vendors.value.length);
  } catch (error) {
    // 如果報 401，代表沒登入或 Token 過期
    if (error.response && error.response.status === 401) {
      console.warn("未登入，無法取得受保護的資料");
      vendors.value = []; // 清空資料，避免顯示舊的
    } else {
      console.error("無法取得資料：", error);
    }
  }
};

// --- 4. 登入與登出邏輯 ---
const handleLogin = async () => {
  try {
    console.log("正在登入，傳送資料：", loginForm); // 檢查這裡
    const res = await axios.post(`${API_BASE_URL}/Auth/login`, loginForm);
    localStorage.setItem('userToken', res.data.token);
    isLoggedIn.value = true;
    showLoginModal.value = false;
    alert('登入成功！');
    fetchData(); // 登入後立即重新整理地圖資料
  } catch (err) {
    alert('帳號或密碼錯誤');
  }
};

const logout = () => {
  localStorage.removeItem('userToken');
  isLoggedIn.value = false;
  vendors.value = []; // 登出後清空設施
  alert('已登出');
  // 如果你有 updateMarkers，也要執行一次清空標記
  if (typeof updateMarkers === 'function') updateMarkers();
};


const addNewPoint = async (name, lat, lng) => {
  const newVendor = { 
    name: name, 
    lat: lat, 
    lng: lng, 
    status: 'success', 
    type: '手動新增' 
  };

  try {
    // 發送請求到後端
    //await axios.post('http://localhost:5240/vendors', newVendor);
    await axios.post(`${API_BASE_URL}/vendors`, newVendor, {
          headers: {
          'Authorization': `Bearer ${localStorage.getItem('userToken')}` // 務必檢查是否有 Bearer 字樣和空格
                    }
    });

    // 關鍵：新增完後，主動獲取目前地圖邊界來更新畫面
    const bounds = map.getBounds();
    const params = {
      minLat: bounds.getSouth(),
      maxLat: bounds.getNorth(),
      minLng: bounds.getWest(),
      maxLng: bounds.getEast()
    };
    
    // 重新抓取資料，這會觸發 updateMarkers()
    await fetchData(params); 
    
    alert("設施新增成功！");
  } catch (error) {
    console.error("新增失敗：", error);
    alert("新增失敗，請檢查後端伺服器是否正常運作");
  }
};

// --- 修改狀態 (PATCH) ---
const updateStatus = async (id, newStatus) => {
  try {
    //await axios.patch(`http://localhost:5240/vendors/${id}`, { status: newStatus });
    await axios.patch(`${API_BASE_URL}/vendors/${id}`, { status: newStatus }, {
          headers: {
          'Authorization': `Bearer ${localStorage.getItem('userToken')}` // 務必檢查是否有 Bearer 字樣和空格
                    }
    });

    await fetchData(); // 重新抓取資料並更新地圖顏色
  } catch (error) {
    alert("修改失敗");
  }
};

// --- 刪除設施 (DELETE) ---
const deleteVendor = async (id) => {
  if (!confirm("確定要移除這個設施嗎？")) return;
  
  try {
    //await axios.delete(`http://localhost:5240/vendors/${id}`);
    await axios.delete(`${API_BASE_URL}/vendors/${id}`, {
          headers: {
          'Authorization': `Bearer ${localStorage.getItem('userToken')}` // 務必檢查是否有 Bearer 字樣和空格
                    }
    });

    await fetchData(); // 重新抓取資料，該點會從清單與地圖消失
  } catch (error) {
    alert("刪除失敗");
  }
};

const updateMarkers = () => {
  // 檢查 clusterGroup 是否真的存在且已加到地圖
  if (!clusterGroup) {
    console.error("❌ 錯誤：clusterGroup 尚未初始化！");
    return;
  }

  // 1. 清除舊的標記
  clusterGroup.clearLayers();

  // 2. 檢查 vendors 是否有資料
  console.log("📊 當前準備繪製的設施數量：", vendors.value.length);

  if (vendors.value.length === 0) {
    console.warn("⚠️ 警告：目前 vendors 陣列是空的，所以地圖沒東西。");
    return;
  }

  // 3. 建立並加入標記
  vendors.value.forEach(v => {
    // 建立一個標準標記
    const marker = L.marker([v.lat, v.lng]).bindPopup(`<b>${v.name}</b>`);
    
    // 將標記加入群組
    clusterGroup.addLayer(marker);
  });

  console.log("✅ 標記已成功加入叢集群組！");
};

const focusOnVendor = (v) => {
  selectedId.value = v.id;
  map.flyTo([v.lat, v.lng], 16, { animate: true, duration: 1 });
  
  // 延遲開啟資訊視窗，讓動畫更順暢
  setTimeout(() => {
    L.popup()
      .setLatLng([v.lat, v.lng])
      .setContent(`<strong>${v.name}</strong><br>定位成功`)
      .openOn(map);
  }, 1100);
};


// --- 生命週期 ---
onMounted(() => {
  initMap();
  fetchData();
});
</script>

<style scoped>
/* 全域佈局 */
.dashboard {
  display: flex;
  flex-direction: column;
  height: 100vh;
  width: 100vw;
  font-family: 'PingFang TC', 'Microsoft JhengHei', sans-serif;
  background: #f4f7f6;
}

/* 頂部 Header */
.header {
  height: 60px;
  background: #2c3e50;
  color: white;
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 0 20px;
  box-shadow: 0 2px 5px rgba(0,0,0,0.2);
}

.logo { display: flex; align-items: center; gap: 10px; }
.logo h1 { font-size: 1.2rem; margin: 0; }

/* 主內容區 */
.main-content {
  display: flex;
  flex: 1;
  overflow: hidden;
}

/* 側邊欄 */
.sidebar {
  width: 320px;
  background: white;
  display: flex;
  flex-direction: column;
  border-right: 1px solid #ddd;
}

.search-box { padding: 15px; }
.styled-input {
  width: 100%;
  padding: 10px;
  border: 1px solid #ddd;
  border-radius: 5px;
  outline: none;
}

.stats-panel {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 10px;
  padding: 0 15px 15px;
}

.stat-card {
  background: #f8f9fa;
  padding: 10px;
  border-radius: 5px;
  text-align: center;
}
.stat-card.danger .value { color: #e74c3c; font-weight: bold; }

.list-container {
  flex: 1;
  overflow-y: auto;
  padding: 10px;
}

.vendor-card {
  padding: 12px;
  border-radius: 8px;
  margin-bottom: 10px;
  border: 1px solid #eee;
  cursor: pointer;
  transition: all 0.2s;
}

.vendor-card:hover { background: #f0f7ff; transform: translateY(-2px); }
.vendor-card.active { border-color: #3498db; background: #ebf5ff; }

.card-header { display: flex; align-items: center; gap: 8px; margin-bottom: 5px; }
.card-header h3 { font-size: 1rem; margin: 0; }

.status-dot { width: 10px; height: 10px; border-radius: 50%; }
.success { background: #2ecc71; }
.warning { background: #f1c40f; }
.error { background: #e74c3c; }

.address { font-size: 0.8rem; color: #666; margin-bottom: 8px; }

.card-footer {
  display: flex;
  justify-content: space-between;
  font-size: 0.7rem;
}

.type-tag { background: #eee; padding: 2px 6px; border-radius: 4px; }

/* 地圖區域 */
.map-container {
  flex: 1;
  position: relative;
}

#map {
  height: 100%;
  width: 100%;
}

.map-overlay {
  position: absolute;
  top: 10px;
  right: 10px;
  background: rgba(255,255,255,0.9);
  padding: 8px 12px;
  border-radius: 20px;
  font-size: 0.8rem;
  z-index: 1000;
  box-shadow: 0 2px 5px rgba(0,0,0,0.1);
}

/* 狀態顏色 */
.text-success { color: #2ecc71; }
.text-warning { color: #f1c40f; }
.text-danger { color: #e74c3c; }

.card-actions {
  display: flex;
  gap: 5px;
  margin-top: 10px;
}
.btn-status {
  font-size: 0.7rem;
  padding: 2px 5px;
  border: none;
  border-radius: 3px;
  cursor: pointer;
}
.btn-status.s { background: #2ecc71; color: white; }
.btn-status.w { background: #f1c40f; }
.btn-status.e { background: #e74c3c; color: white; }

.btn-delete {
  margin-left: auto;
  background: none;
  border: none;
  cursor: pointer;
  filter: grayscale(1);
}
.btn-delete:hover { filter: grayscale(0); }

/* 控制列樣式 */
.admin-bar {
  position: absolute;
  top: 20px;
  right: 20px;
  z-index: 1000;
}

/* 登入視窗樣式 - 毛玻璃效果 */
.modal-overlay {
  position: fixed;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  background: rgba(0, 0, 0, 0.5);
  backdrop-filter: blur(8px); /* 關鍵：模糊背景 */
  display: flex;
  justify-content: center;
  align-items: center;
  z-index: 2000;
}

.modal-card {
  background: white;
  padding: 30px;
  border-radius: 15px;
  box-shadow: 0 10px 25px rgba(0,0,0,0.2);
  width: 320px;
  text-align: center;
}

.input-group input {
  width: 100%;
  margin: 10px 0;
  padding: 12px;
  border: 1px solid #ddd;
  border-radius: 8px;
}

.btn-submit {
  background: #42b883;
  color: white;
  width: 100%;
  padding: 12px;
  border: none;
  border-radius: 8px;
  cursor: pointer;
  font-weight: bold;
}

</style>