"use strict";

// ======================================================
// 1. CAU HINH VA KHAI BAO CAC THANH PHAN HTML
// ======================================================

const API_BASE = "";
const HISTORY_KEY = "vietguide_history_v1";
const MAX_HISTORY = 50;

const locationSelect = document.getElementById("locationSelect");
const languageSelect = document.getElementById("languageSelect");
const locationPreview = document.getElementById("locationPreview");
const narrationForm = document.getElementById("narrationForm");
const submitBtn = document.getElementById("submitBtn");
const errorMessage = document.getElementById("errorMessage");
const resultSection = document.getElementById("resultSection");

const resultMeta = document.getElementById("resultMeta");
const resultTitle = document.getElementById("resultTitle");
const resultText = document.getElementById("resultText");
const resultStatus = document.getElementById("resultStatus");
const audioPlayer = document.getElementById("audioPlayer");

const exploreMap = document.getElementById("exploreMap");
const mapMessage = document.getElementById("mapMessage");

const historyList = document.getElementById("historyList");
const clearHistoryButton = document.getElementById("clearHistory");

let locations = [];
let languages = [];
let vietGuideMap = null;
let vietGuideMarkers = [];
let featuredMarkers = [];
const featuredPlaces = [];

// ======================================================
// 2. HAM HO TRO
// ======================================================

function getFirstValue(object, keys, fallback = "") {
  if (!object) {
    return fallback;
  }

  for (const key of keys) {
    if (
      object[key] !== undefined &&
      object[key] !== null &&
      object[key] !== ""
    ) {
      return object[key];
    }
  }

  return fallback;
}

function getLocationId(location) {
  return getFirstValue(location, [
    "id",
    "locationId",
    "maDiaDiem",
    "MaDiaDiem",
  ]);
}

function getLocationName(location) {
  return getFirstValue(
    location,
    ["name", "locationName", "tenDiaDiem", "TenDiaDiem", "title"],
    "Địa điểm chưa có tên",
  );
}

function getLanguageCode(language) {
  return getFirstValue(language, [
    "code",
    "languageCode",
    "maNgonNgu",
    "MaNgonNgu",
  ]);
}

function getLanguageName(language) {
  return getFirstValue(
    language,
    ["name", "languageName", "tenNgonNgu", "TenNgonNgu"],
    "Ngôn ngữ",
  );
}

function getArrayFromResponse(response) {
  if (Array.isArray(response)) {
    return response;
  }

  if (response && Array.isArray(response.data)) {
    return response.data;
  }

  if (response && Array.isArray(response.items)) {
    return response.items;
  }

  if (response && Array.isArray(response.locations)) {
    return response.locations;
  }

  if (response && Array.isArray(response.languages)) {
    return response.languages;
  }

  return [];
}

async function getJson(url, options = {}) {
  const response = await fetch(`${API_BASE}${url}`, {
    ...options,
    headers: {
      "Content-Type": "application/json",
      ...(options.headers || {}),
    },
  });

  const contentType = response.headers.get("content-type") || "";
  let data;

  if (contentType.includes("json")) {
    data = await response.json();
  } else {
    data = await response.text();
  }

  if (!response.ok) {
    let message = "Yêu cầu không thành công.";

    if (typeof data === "string" && data.trim()) {
      message = data;
    } else if (data && typeof data === "object") {
      message = data.message || data.detail || data.error || message;
    }

    const error = new Error(message);
    error.status = response.status;
    throw error;
  }

  return data;
}

function showError(message) {
  if (!errorMessage) {
    console.error(message);
    return;
  }

  errorMessage.textContent = message;
  errorMessage.hidden = false;
  errorMessage.style.display = "";
}

function hideError() {
  if (!errorMessage) {
    return;
  }

  errorMessage.textContent = "";
  errorMessage.hidden = true;
  errorMessage.style.display = "none";
}

function setMapMessage(message) {
  if (mapMessage) {
    mapMessage.textContent = message;
  }
}

function formatDate(dateValue) {
  if (!dateValue) {
    return "Không rõ thời gian";
  }

  const date = new Date(dateValue);

  if (Number.isNaN(date.getTime())) {
    return "Không rõ thời gian";
  }

  return date.toLocaleString("vi-VN");
}

// ======================================================
// 3. TAI DANH SACH DIA DIEM VA NGON NGU
// ======================================================

function fillLocationOptions() {
  if (!locationSelect) {
    return;
  }

  locationSelect.replaceChildren();

  const defaultOption = document.createElement("option");
  defaultOption.value = "";
  defaultOption.textContent = "-- Chọn địa điểm --";
  locationSelect.appendChild(defaultOption);

  locations.forEach((location) => {
    const id = getLocationId(location);

    if (id === "") {
      return;
    }

    const option = document.createElement("option");
    option.value = String(id);
    option.textContent = getLocationName(location);

    locationSelect.appendChild(option);
  });
}

function fillLanguageOptions() {
  if (!languageSelect) {
    return;
  }

  languageSelect.replaceChildren();

  const defaultOption = document.createElement("option");
  defaultOption.value = "";
  defaultOption.textContent = "-- Chọn ngôn ngữ --";
  languageSelect.appendChild(defaultOption);

  languages.forEach((language) => {
    const code = getLanguageCode(language);

    if (code === "") {
      return;
    }

    const option = document.createElement("option");
    option.value = String(code);
    option.textContent = getLanguageName(language);

    languageSelect.appendChild(option);
  });
}

async function loadLocations() {
  const response = await getJson("/api/locations");
  locations = getArrayFromResponse(response);

  fillLocationOptions();
  refreshVietGuideMap();

  return locations;
}

async function loadLanguages() {
  const response = await getJson("/api/languages");
  languages = getArrayFromResponse(response);

  fillLanguageOptions();

  return languages;
}

async function loadOptions() {
  const results = await Promise.allSettled([loadLocations(), loadLanguages()]);

  const locationResult = results[0];
  const languageResult = results[1];

  if (locationResult.status === "rejected") {
    console.error("Lỗi tải địa điểm:", locationResult.reason);

    if (locationSelect) {
      locationSelect.innerHTML =
        '<option value="">Không tải được địa điểm</option>';
    }

    setMapMessage(
      "Không tải được danh sách địa điểm. Hãy kiểm tra API /api/locations.",
    );
  }

  if (languageResult.status === "rejected") {
    console.error("Lỗi tải ngôn ngữ:", languageResult.reason);

    if (languageSelect) {
      languageSelect.innerHTML =
        '<option value="">Không tải được ngôn ngữ</option>';
    }
  }

  if (
    locationResult.status === "rejected" ||
    languageResult.status === "rejected"
  ) {
    showError(
      "Không tải được một số dữ liệu. Hãy kiểm tra máy chủ API và thử tải lại trang.",
    );
  } else {
    hideError();
  }

  // Luôn cập nhật bản đồ, kể cả khi API không tải được.
  refreshVietGuideMap();
}

// Khi người dùng đổi địa điểm
if (locationSelect) {
  locationSelect.addEventListener("change", () => {
    const selectedLocation = locations.find(
      (location) => String(getLocationId(location)) === locationSelect.value,
    );

    if (locationPreview) {
      if (selectedLocation) {
        locationPreview.textContent = getFirstValue(
          selectedLocation,
          ["description", "descriptionText", "moTa", "MoTa"],
          getLocationName(selectedLocation),
        );
      } else {
        locationPreview.textContent = "";
      }
    }
  });
}

// ======================================================
// 4. TAO BAI THUYET MINH
// ======================================================

function displayNarration(item) {
  if (!item) {
    return;
  }

  const locationName = item.locationName || item.name || "Địa điểm";

  const languageName = item.languageName || item.languageCode || "Ngôn ngữ";

  if (resultTitle) {
    resultTitle.textContent = `Thuyết minh: ${locationName}`;
  }

  if (resultMeta) {
    resultMeta.textContent = `Địa điểm: ${locationName} | Ngôn ngữ: ${languageName}`;
  }

  if (resultText) {
    resultText.textContent = item.text || "Chưa có nội dung thuyết minh.";
  }

  if (resultStatus) {
    resultStatus.textContent = item.status ? `Trạng thái: ${item.status}` : "";
  }

  if (audioPlayer) {
    if (item.audioUrl) {
      audioPlayer.src = item.audioUrl;
      audioPlayer.hidden = false;
      audioPlayer.style.display = "";

      if (typeof audioPlayer.load === "function") {
        audioPlayer.load();
      }
    } else {
      audioPlayer.pause();
      audioPlayer.removeAttribute("src");
      audioPlayer.load();
      audioPlayer.hidden = true;
      audioPlayer.style.display = "none";
    }
  }

  if (resultSection) {
    resultSection.hidden = false;
    resultSection.style.display = "";
  }
}

if (narrationForm) {
  narrationForm.addEventListener("submit", async (event) => {
    event.preventDefault();
    hideError();

    const locationId = locationSelect ? locationSelect.value : "";

    const languageCode = languageSelect ? languageSelect.value : "";

    if (!locationId) {
      showError("Vui lòng chọn địa điểm.");
      return;
    }

    if (!languageCode) {
      showError("Vui lòng chọn ngôn ngữ.");
      return;
    }

    const selectedLocation = locations.find(
      (location) => String(getLocationId(location)) === String(locationId),
    );

    const selectedLanguage = languages.find(
      (language) => String(getLanguageCode(language)) === String(languageCode),
    );

    const originalButtonText = submitBtn ? submitBtn.textContent : "";

    if (submitBtn) {
      submitBtn.disabled = true;
      submitBtn.textContent = "Đang tải bài thuyết minh...";
    }

    try {
      const response = await getJson(
        `/api/narrations/${encodeURIComponent(locationId)}/${encodeURIComponent(languageCode)}`,
      );

      // Một số API trả dữ liệu trực tiếp, một số trả trong thuộc tính data.
      const data =
        response && response.data && typeof response.data === "object"
          ? response.data
          : response;

      const narration = {
        locationId: getFirstValue(data, ["locationId"], locationId),
        locationName: getFirstValue(
          data,
          ["locationName", "name"],
          selectedLocation ? getLocationName(selectedLocation) : "Địa điểm",
        ),
        languageCode: getFirstValue(data, ["languageCode"], languageCode),
        languageName: getFirstValue(
          data,
          ["languageName"],
          selectedLanguage ? getLanguageName(selectedLanguage) : languageCode,
        ),
        text: getFirstValue(data, ["text", "content", "narrationText"], ""),
        status: getFirstValue(data, ["status"], ""),
        audioUrl: getFirstValue(data, ["audioUrl", "audioURL", "audio"], ""),
      };

      if (!narration.text) {
        throw new Error(
          "Bản thuyết minh hiện hành không có nội dung.",
        );
      }

      narration.createdAt = new Date().toISOString();

      displayNarration(narration);
      saveNarrationHistory(narration);

      if (resultSection) {
        resultSection.scrollIntoView({
          behavior: "smooth",
          block: "start",
        });
      }
    } catch (error) {
      if (resultSection) {
        resultSection.hidden = true;
      }

      if (error.status === 404) {
        showError("Chưa có bài thuyết minh cho địa điểm và ngôn ngữ đã chọn.");
      } else {
        console.error("Lỗi tải bài thuyết minh:", error);

        showError(
          error.message ||
            "Không thể tải bài thuyết minh. Hãy kiểm tra kết nối máy chủ.",
        );
      }
    } finally {
      if (submitBtn) {
        submitBtn.disabled = false;
        submitBtn.textContent = originalButtonText || "Xem thuyết minh";
      }
    }
  });
}

// ======================================================
// 5. SAO CHEP NOI DUNG THUYET MINH
// ======================================================

async function copyNarrationText() {
  const text = resultText ? resultText.textContent.trim() : "";

  if (!text) {
    showError("Chưa có nội dung để sao chép.");
    return;
  }

  try {
    if (navigator.clipboard && window.isSecureContext) {
      await navigator.clipboard.writeText(text);
    } else {
      const textArea = document.createElement("textarea");
      textArea.value = text;
      textArea.style.position = "fixed";
      textArea.style.left = "-9999px";

      document.body.appendChild(textArea);
      textArea.select();

      const copied = document.execCommand("copy");
      textArea.remove();

      if (!copied) {
        throw new Error("Không thể sao chép nội dung.");
      }
    }

    hideError();
    alert("Đã sao chép nội dung thuyết minh.");
  } catch (error) {
    console.error("Lỗi sao chép:", error);
    showError("Không thể sao chép. Bạn hãy thử chọn và sao chép thủ công.");
  }
}

const copyBtn = document.getElementById("copyBtn");

if (copyBtn) {
  copyBtn.addEventListener("click", copyNarrationText);
}
// ======================================================
// 6. LUU VA HIEN THI LICH SU THUYET MINH
// ======================================================

function getNarrationHistory() {
  try {
    const saved = localStorage.getItem(HISTORY_KEY);

    if (!saved) {
      return [];
    }

    const history = JSON.parse(saved);

    if (!Array.isArray(history)) {
      return [];
    }

    return history;
  } catch (error) {
    console.error("Không đọc được lịch sử:", error);
    return [];
  }
}

function writeNarrationHistory(history) {
  try {
    localStorage.setItem(
      HISTORY_KEY,
      JSON.stringify(history.slice(0, MAX_HISTORY)),
    );

    return true;
  } catch (error) {
    console.error("Không lưu được lịch sử:", error);
    showError(
      "Không lưu được lịch sử trên trình duyệt. Hãy kiểm tra dung lượng hoặc cài đặt lưu trữ.",
    );
    return false;
  }
}

function saveNarrationHistory(item) {
  const history = getNarrationHistory();

  const savedItem = {
    ...item,
    createdAt: item.createdAt || new Date().toISOString(),
  };

  history.unshift(savedItem);

  if (writeNarrationHistory(history)) {
    renderNarrationHistory();
  }
}

function createHistoryButton(text, className, onClick) {
  const button = document.createElement("button");

  button.type = "button";
  button.textContent = text;
  button.className = className;
  button.addEventListener("click", onClick);

  return button;
}

function renderNarrationHistory() {
  if (!historyList) {
    return;
  }

  const history = getNarrationHistory();

  historyList.replaceChildren();

  if (history.length === 0) {
    const emptyMessage = document.createElement("p");
    emptyMessage.textContent = "Bạn chưa có lịch sử thuyết minh.";
    historyList.appendChild(emptyMessage);
    return;
  }

  history.forEach((item, index) => {
    const card = document.createElement("div");
    card.className = "history-card";

    const title = document.createElement("h3");
    title.textContent = item.locationName || "Địa điểm";
    card.appendChild(title);

    const meta = document.createElement("p");
    meta.className = "history-meta";
    meta.textContent =
      `${item.languageName || item.languageCode || "Ngôn ngữ"} • ` +
      formatDate(item.createdAt);
    card.appendChild(meta);

    const preview = document.createElement("p");
    preview.className = "history-preview";

    const content = item.text || "";
    preview.textContent =
      content.length > 180 ? content.substring(0, 180) + "..." : content;

    card.appendChild(preview);

    const actions = document.createElement("div");
    actions.className = "history-actions";

    const viewButton = createHistoryButton("Xem lại", "history-view-btn", () =>
      openSavedNarration(item, false),
    );

    actions.appendChild(viewButton);

    if (item.audioUrl) {
      const playButton = createHistoryButton(
        "Nghe lại",
        "history-play-btn",
        () => openSavedNarration(item, true),
      );

      actions.appendChild(playButton);
    }

    const deleteButton = createHistoryButton("Xóa", "history-delete-btn", () =>
      deleteNarrationHistory(index),
    );

    actions.appendChild(deleteButton);
    card.appendChild(actions);
    historyList.appendChild(card);
  });
}

function openSavedNarration(item, shouldPlay = false) {
  if (!item) {
    return;
  }

  displayNarration(item);
  hideError();

  if (resultSection) {
    resultSection.scrollIntoView({
      behavior: "smooth",
      block: "start",
    });
  }

  if (shouldPlay && audioPlayer && item.audioUrl) {
    audioPlayer.play().catch((error) => {
      console.error("Không phát được âm thanh:", error);
      showError(
        "Không thể phát âm thanh. Hãy kiểm tra đường dẫn audio hoặc thử lại.",
      );
    });
  }
}

function deleteNarrationHistory(index) {
  const history = getNarrationHistory();

  if (index < 0 || index >= history.length) {
    return;
  }

  const confirmed = confirm(
    "Bạn có chắc muốn xóa bài thuyết minh này khỏi lịch sử?",
  );

  if (!confirmed) {
    return;
  }

  history.splice(index, 1);

  if (writeNarrationHistory(history)) {
    renderNarrationHistory();
  }
}

if (clearHistoryButton) {
  clearHistoryButton.addEventListener("click", () => {
    const history = getNarrationHistory();

    if (history.length === 0) {
      alert("Lịch sử hiện đang trống.");
      return;
    }

    const confirmed = confirm(
      "Bạn có chắc muốn xóa toàn bộ lịch sử thuyết minh?",
    );

    if (!confirmed) {
      return;
    }

    try {
      localStorage.removeItem(HISTORY_KEY);
      renderNarrationHistory();
    } catch (error) {
      console.error("Lỗi xóa lịch sử:", error);
      showError("Không thể xóa lịch sử.");
    }
  });
}

// ======================================================
// 7. KHOI TAO BAN DO LEAFLET
// ======================================================

function initVietGuideMap() {
  if (!exploreMap) {
    console.error("Không tìm thấy phần tử #exploreMap trong HTML.");
    return;
  }

  if (typeof L === "undefined") {
    console.error("Leaflet chưa được tải.");
    return;
  }

  if (vietGuideMap) return;

  vietGuideMap = L.map(exploreMap, {
    center: [16.0, 107.5],
    zoom: 5,
    minZoom: 4,
    maxZoom: 18,
    maxBounds: [
      [7.5, 101.5],
      [24.0, 112.0],
    ],
    maxBoundsViscosity: 0.8,
  });

  L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
    attribution:
      '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
    maxZoom: 19,
  }).addTo(vietGuideMap);

  addFeaturedPlaces();

  requestAnimationFrame(() => {
    vietGuideMap.invalidateSize();
  });
}

function getLocationCoordinates(location) {
  const coordinates = location.coordinates || location.coordinate || {};

  const latitudeValue = getFirstValue(
    location,
    ["latitude", "lat", "viDo", "ViDo"],
    getFirstValue(coordinates, ["latitude", "lat", "viDo"], null),
  );

  const longitudeValue = getFirstValue(
    location,
    ["longitude", "lng", "lon", "kinhDo", "KinhDo"],
    getFirstValue(coordinates, ["longitude", "lng", "lon", "kinhDo"], null),
  );

  if (latitudeValue !== null && longitudeValue !== null) {
    const latitude = Number(latitudeValue);
    const longitude = Number(longitudeValue);

    if (
      Number.isFinite(latitude) &&
      Number.isFinite(longitude) &&
      latitude >= -90 &&
      latitude <= 90 &&
      longitude >= -180 &&
      longitude <= 180
    ) {
      return [latitude, longitude];
    }
  }

  return null;
}

function selectVietGuideLocation(location) {
  if (!location || !locationSelect) {
    return;
  }

  const id = getLocationId(location);

  locationSelect.value = String(id);

  // Thông báo cho các listener hiện có khi chọn địa điểm từ bản đồ.
  locationSelect.dispatchEvent(new Event("change", { bubbles: true }));

  const createSection = document.getElementById("create");

  if (createSection) {
    createSection.scrollIntoView({
      behavior: "smooth",
      block: "start",
    });
  }

  setMapMessage(
    `Đã chọn địa điểm: ${getLocationName(location)}. Hãy chọn ngôn ngữ và tạo bài thuyết minh.`,
  );
}

function addFeaturedPlaces() {
  if (!vietGuideMap || typeof L === "undefined") return;
  if (featuredMarkers.length > 0) return;

  featuredPlaces.forEach((place) => {
    const marker = L.marker(place.position).addTo(vietGuideMap);

    const popup = document.createElement("div");

    const title = document.createElement("strong");
    title.textContent = place.name;
    popup.appendChild(title);

    const description = document.createElement("p");
    description.textContent = place.description;
    popup.appendChild(description);

    const button = document.createElement("button");
    button.type = "button";
    button.textContent = "Chọn địa điểm";
    button.style.marginTop = "6px";

    button.addEventListener("click", () => {
      const normalize = (value) =>
        String(value || "")
          .normalize("NFD")
          .replace(/[\u0300-\u036f]/g, "")
          .toLowerCase()
          .replace(/[^a-z0-9]+/g, " ")
          .trim();

      const aliases = {
        "bao tang ho chi minh ben nha rong": [
          "bao tang ho chi minh",
          "ben nha rong",
          "nha rong",
        ],
        "cong vien sang tao": ["cong vien sang tao"],
        "landmark 81": ["landmark 81"],
        "bitexco financial tower": ["bitexco"],
        "pho di bo bui vien": ["bui vien"],
        "pho di bo nguyen hue": ["nguyen hue"],
        "bao tang chung tich chien tranh": ["chung tich chien tranh"],
        "bao tang phu nu nam bo": ["bao tang phu nu nam bo"],
        "cho ben thanh": ["ben thanh"],
      };

      const placeKey = normalize(place.name);
      const keywords = aliases[placeKey] || [placeKey];

      const matchedLocation = locations.find((location) => {
        const name = normalize(getLocationName(location));

        return keywords.some((keyword) => name.includes(normalize(keyword)));
      });

      if (matchedLocation) {
        selectVietGuideLocation(matchedLocation);
      } else {
        setMapMessage(
          `Bạn đã chọn ${place.name}. Địa điểm này chưa có trong danh sách API, nên chưa thể tạo bài thuyết minh cho đến khi được thêm vào dữ liệu.`,
        );
      }

      marker.closePopup();
    });

    popup.appendChild(button);
    marker.bindPopup(popup);

    featuredMarkers.push(marker);
  });
}

function refreshVietGuideMap() {
  if (!vietGuideMap) {
    initVietGuideMap();
  }

  if (!vietGuideMap || typeof L === "undefined") {
    return;
  }

  // Chỉ xóa ghim từ API, không xóa 9 ghim cố định.
  vietGuideMarkers.forEach((marker) => {
    vietGuideMap.removeLayer(marker);
  });

  vietGuideMarkers = [];

  locations.forEach((location) => {
    const coordinates = getLocationCoordinates(location);

    if (!coordinates) return;

    const marker = L.marker(coordinates).addTo(vietGuideMap);

    const popup = document.createElement("div");

    const title = document.createElement("strong");
    title.textContent = getLocationName(location);
    popup.appendChild(title);

    const button = document.createElement("button");
    button.type = "button";
    button.textContent = "Khám phá địa điểm này";
    button.style.display = "block";
    button.style.marginTop = "8px";

    button.addEventListener("click", () => {
      selectVietGuideLocation(location);
      marker.closePopup();
    });

    popup.appendChild(button);
    marker.bindPopup(popup);

    vietGuideMarkers.push(marker);
  });

  requestAnimationFrame(() => {
    if (vietGuideMap) {
      vietGuideMap.invalidateSize();
    }
  });

  const withoutCoordinates = locations.length - vietGuideMarkers.length;
  setMapMessage(
    `Bản đồ hiển thị ${vietGuideMarkers.length} địa điểm có tọa độ trong dữ liệu hệ thống.` +
      (withoutCoordinates > 0
        ? ` ${withoutCoordinates} địa điểm chưa có tọa độ nên chỉ có trong danh sách.`
        : ""),
  );
}

// ======================================================
// 8. KHOI CHAY UNG DUNG
// ======================================================

// Khởi tạo bản đồ độc lập với API.
initVietGuideMap();

// Hiển thị lịch sử đã lưu ngay khi mở trang.
renderNarrationHistory();

// Tải dữ liệu địa điểm và ngôn ngữ.
loadOptions();

// kiểm tra lượt nghe

/* Đếm lượt nghe khi audio thực sự phát đủ 15 giây */
(() => {
  if (!audioPlayer) return;

  let listenedMs = 0;
  let lastTick = null;
  let lastAudioTime = null;
  let recorded = false;

  function resetListenSession() {
    listenedMs = 0;
    lastTick = null;
    lastAudioTime = null;
    recorded = false;
  }

  audioPlayer.addEventListener("play", () => {
    lastTick = performance.now();
    lastAudioTime = audioPlayer.currentTime;
  });

  audioPlayer.addEventListener("pause", () => {
    lastTick = null;
    lastAudioTime = null;
  });

  audioPlayer.addEventListener("seeking", () => {
    lastTick = null;
    lastAudioTime = null;
  });

  audioPlayer.addEventListener("seeked", () => {
    lastTick = performance.now();
    lastAudioTime = audioPlayer.currentTime;
  });

  audioPlayer.addEventListener("emptied", resetListenSession);
  audioPlayer.addEventListener("ended", resetListenSession);

  setInterval(async () => {
    if (
      audioPlayer.paused ||
      audioPlayer.ended ||
      audioPlayer.seeking ||
      !audioPlayer.currentSrc ||
      recorded
    ) {
      lastTick = null;
      lastAudioTime = null;
      return;
    }

    const now = performance.now();
    const currentAudioTime = audioPlayer.currentTime;

    if (lastTick !== null && lastAudioTime !== null) {
      const elapsed = now - lastTick;
      const audioProgress = currentAudioTime - lastAudioTime;

      // Chỉ tính thời gian khi audio đang thực sự tiến triển.
      if (
        audioProgress > 0 &&
        audioProgress < 1.5 &&
        elapsed > 0 &&
        elapsed < 1500
      ) {
        listenedMs += elapsed;
      }
    }

    lastTick = now;
    lastAudioTime = currentAudioTime;

    if (listenedMs >= 15000) {
      recorded = true;

      try {
        const response = await fetch("/api/audio-listens", {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
          },
          body: JSON.stringify({
            listenId: crypto.randomUUID(),
            locationName:
              locationSelect?.selectedOptions?.[0]?.textContent?.trim() || null,
            languageCode: languageSelect?.value || null,
          }),
        });

        if (!response.ok) {
          throw new Error("Không lưu được lượt nghe.");
        }

        console.log("Đã ghi nhận lượt nghe đủ 15 giây.");
      } catch (error) {
        // Cho phép thử gửi lại nếu API bị lỗi.
        recorded = false;
        console.error("Lỗi ghi nhận lượt nghe:", error);
      }
    }
  }, 250);
})();
