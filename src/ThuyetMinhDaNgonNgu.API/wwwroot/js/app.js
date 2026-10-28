"use strict";

// ======================================================
// 1. CAU HINH VA KHAI BAO CAC THANH PHAN HTML
// ======================================================

const API_BASE = "";

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
const findNearbyButton = document.getElementById("findNearbyButton");
const placesList = document.getElementById("placesList");
const placesPlaybackStatus = document.getElementById("placesPlaybackStatus");
const placesLanguageSelect = document.getElementById("placesLanguageSelect");

let locations = [];
let languages = [];
let currentUserCoordinates = null;
let activePlacesLanguage = "vi";
let placesLanguageManuallySelected = false;
let placesLoaded = false;
let placesLoadFailed = false;
let activeSiteLanguage = "vi";
let vietGuideMap = null;
let vietGuideMarkers = [];
let featuredMarkers = [];
let userLocationMarker = null;
let hasCenteredOnUserLocation = false;
let narrationRequestVersion = 0;
const NEARBY_RADIUS_METERS = 50;
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

function getLocationCity(location) {
  return getFirstValue(location, ["city", "City", "thanhPho", "ThanhPho"]);
}

function getLocalizedLocationValue(location, fieldNames, language, fallback) {
  const translations = getFirstValue(
    location,
    ["translations", "Translations"],
    null,
  );
  const languageTranslation =
    translations && typeof translations === "object"
      ? translations[language] ||
        translations[language.split("-")[0]] ||
        null
      : null;

  if (languageTranslation) {
    const translatedValue = getFirstValue(languageTranslation, fieldNames);
    if (translatedValue) return translatedValue;
  }

  return getFirstValue(location, fieldNames, fallback);
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

function formatDistance(distance) {
  if (distance < 1000) {
    return `${Math.round(distance)} m`;
  }

  return `${(distance / 1000).toFixed(1)} km`;
}

function renderPlacesList() {
  if (!placesList) return;

  placesList.replaceChildren();

  if (placesLoadFailed) {
    const status = document.createElement("p");
    status.className = "places-empty";
    status.textContent =
      (siteTranslations[activeSiteLanguage] || siteTranslations.vi)
        .placesLoadError;
    placesList.appendChild(status);
    return;
  }

  if (!placesLoaded) {
    const status = document.createElement("p");
    status.className = "places-empty";
    status.textContent =
      (siteTranslations[activeSiteLanguage] || siteTranslations.vi)
        .placesLoading;
    placesList.appendChild(status);
    return;
  }

  const coordinates = currentUserCoordinates;
  const sortedLocations = locations
    .map((location, index) => {
      const locationCoordinates = getLocationCoordinates(location);
      return {
        location,
        index,
        distance:
          coordinates && locationCoordinates
            ? calculateDistanceMeters(
                coordinates[0],
                coordinates[1],
                locationCoordinates[0],
                locationCoordinates[1],
              )
            : null,
      };
    })
    .sort((a, b) => {
      if (a.distance === null && b.distance === null) return a.index - b.index;
      if (a.distance === null) return 1;
      if (b.distance === null) return -1;
      return a.distance - b.distance;
    });

  if (sortedLocations.length === 0) {
    const status = document.createElement("p");
    status.className = "places-empty";
    status.textContent =
      (siteTranslations[activeSiteLanguage] || siteTranslations.vi)
        .placesEmpty;
    placesList.appendChild(status);
    return;
  }

  const translations =
    siteTranslations[activeSiteLanguage] || siteTranslations.vi;

  sortedLocations.forEach(({ location, distance }) => {
    const card = document.createElement("button");
    card.className = "place-card";
    card.type = "button";
    card.dataset.locationId = String(getLocationId(location));
    card.setAttribute(
      "aria-label",
      `${getLocationName(location)}. ${translations.placeSelect}`,
    );

    const city = getLocationCity(location);
    if (city) {
      const cityLabel = document.createElement("span");
      cityLabel.className = "place-card-city";
      cityLabel.textContent = city;
      card.appendChild(cityLabel);
    }

    const title = document.createElement("h3");
    title.className = "place-card-name";
    title.textContent = getLocalizedLocationValue(
      location,
      ["name", "Name", "locationName", "title"],
      activePlacesLanguage,
      getLocationName(location),
    );
    card.appendChild(title);

    const description = getLocalizedLocationValue(
      location,
      ["description", "Description", "descriptionText", "moTa", "MoTa"],
      activePlacesLanguage,
      "",
    );
    if (description) {
      const descriptionText = document.createElement("p");
      descriptionText.className = "place-card-description";
      descriptionText.textContent = description;
      card.appendChild(descriptionText);
    }

    const distanceLabel = document.createElement("span");
    distanceLabel.className = "place-card-distance";
    distanceLabel.textContent =
      distance === null
        ? translations.distanceUnavailable
        : formatDistance(distance);
    card.appendChild(distanceLabel);

    card.addEventListener("click", () => {
      selectVietGuideLocation(location, {
        scrollToForm: false,
        announce: false,
      });
      void autoPlayNearbyNarration(location, {
        force: true,
        fromPlacesList: true,
      });
    });
    placesList.appendChild(card);
  });
}

function setPlacesPlaybackStatus(message, isError = false) {
  if (!placesPlaybackStatus) return;

  placesPlaybackStatus.textContent = message;
  placesPlaybackStatus.hidden = !message;
  placesPlaybackStatus.classList.toggle("is-error", isError);
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

function choosePreferredLanguage() {
  if (!languageSelect || languages.length === 0) return "";

  const supportedLanguages = languages
    .map((language) => ({
      code: String(getLanguageCode(language) || ""),
      name: getLanguageName(language),
    }))
    .filter((language) => language.code);

  const browserLanguages = [
    ...(navigator.languages || []),
    navigator.language || "",
  ]
    .map((language) => language.toLowerCase())
    .filter(Boolean);

  // Ưu tiên ngôn ngữ trình duyệt khớp chính xác,
  // sau đó thử khớp mã ngôn ngữ cơ bản.
  let preferred = null;

  for (const browserLanguage of browserLanguages) {
    preferred = supportedLanguages.find(
      (language) => language.code.toLowerCase() === browserLanguage
    );

    if (preferred) break;

    const baseLanguage = browserLanguage.split("-")[0];

    preferred = supportedLanguages.find(
      (language) => language.code.toLowerCase() === baseLanguage
    );

    if (preferred) break;
  }

  // Nếu không hỗ trợ ngôn ngữ trình duyệt thì ưu tiên tiếng Việt.
  if (!preferred) {
    preferred = supportedLanguages.find(
      (language) => language.code.toLowerCase() === "vi"
    );
  }

  // Nếu không có tiếng Việt thì để người dùng tự chọn.
  languageSelect.value = preferred ? preferred.code : "";
  return preferred ? preferred.code : "";
}

function fillPlacesLanguageOptions(preferredCode) {
  if (!placesLanguageSelect) return;

  const previousValue = placesLanguageSelect.value;
  placesLanguageSelect.replaceChildren();

  languages.forEach((language) => {
    const code = getLanguageCode(language);
    if (!code) return;

    const option = document.createElement("option");
    option.value = String(code);
    option.textContent = getLanguageName(language);
    placesLanguageSelect.appendChild(option);
  });

  if (placesLanguageSelect.options.length === 0) return;

  const requestedValue = placesLanguageManuallySelected
    ? previousValue
    : preferredCode;
  const matchingOption = Array.from(placesLanguageSelect.options).find(
    (option) =>
      option.value.toLowerCase() === String(requestedValue || "").toLowerCase(),
  );
  const vietnameseOption = Array.from(placesLanguageSelect.options).find(
    (option) => option.value.toLowerCase() === "vi",
  );

  placesLanguageSelect.value = (
    matchingOption || vietnameseOption || placesLanguageSelect.options[0]
  ).value;
  activePlacesLanguage = placesLanguageSelect.value;
  renderPlacesList();
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
  placesLoaded = true;
  placesLoadFailed = false;

  console.table(
    locations.map((location) => ({
      name: getLocationName(location),
      coordinates: JSON.stringify(getLocationCoordinates(location)),
    }))
  );
  fillLocationOptions();
  renderPlacesList();
  refreshVietGuideMap();

  return locations;
}

async function loadLanguages() {
  const response = await getJson("/api/languages");
  languages = getArrayFromResponse(response);

  fillLanguageOptions();
  const preferredLanguage = choosePreferredLanguage();
  fillPlacesLanguageOptions(preferredLanguage);

  return languages;
}

async function loadOptions() {
  const results = await Promise.allSettled([loadLocations(), loadLanguages()]);

  const locationResult = results[0];
  const languageResult = results[1];

  if (locationResult.status === "rejected") {
    placesLoaded = true;
    placesLoadFailed = true;
    renderPlacesList();
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
async function initApp() {
  initVietGuideMap();
  await loadOptions();

  // Tự động xin quyền xác định vị trí khi mở trang.
  // Tự động theo dõi GPS và phát thuyết minh khi đến gần.
  startAutomaticNearbyNarration();
}

document.addEventListener("DOMContentLoaded", initApp);
// Khi người dùng đổi địa điểm
function clearCurrentNarration() {
  narrationRequestVersion += 1;
  if (submitBtn) {
    submitBtn.disabled = false;
    submitBtn.removeAttribute("aria-busy");
    setButtonLabel(
      "#submitBtn",
      (siteTranslations[activeSiteLanguage] || siteTranslations.vi).submit,
    );
  }
  if (audioPlayer) {
    audioPlayer.pause();
    audioPlayer.removeAttribute("src");
    audioPlayer.load();
    audioPlayer.hidden = true;
    audioPlayer.style.display = "none";
  }
  if (resultSection) {
    resultSection.hidden = true;
    resultSection.style.display = "none";
  }
}

if (locationSelect) {
  locationSelect.addEventListener("change", () => {
    clearCurrentNarration();
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

if (languageSelect) {
  languageSelect.addEventListener("change", () => {
    // Dừng audio cũ và tải ngay narration theo ngôn ngữ vừa chọn.
    clearCurrentNarration();
    autoNarrationKey = "";
    setPlacesPlaybackStatus("");

    // Nếu GPS đang xác định khách ở trong vùng 50 m, không cần bấm
    // "Tìm địa điểm gần tôi"; tự tải và thử phát audio ngôn ngữ mới.
    if (currentNearbyLocation) {
      autoPlayNearbyNarration(currentNearbyLocation);
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

    const requestVersion = ++narrationRequestVersion;
    const requestedLocationId = String(locationId);
    const requestedLanguageCode = String(languageCode);
    if (submitBtn) {
      submitBtn.disabled = true;
      submitBtn.setAttribute("aria-busy", "true");
      setButtonLabel(
        "#submitBtn",
        (siteTranslations[activeSiteLanguage] || siteTranslations.vi).loading,
      );
    }

    try {
      const response = await getJson(
        `/api/narrations/${encodeURIComponent(locationId)}/${encodeURIComponent(languageCode)}`,
      );

      // Nếu người dùng đã đổi lựa chọn trong lúc chờ API, bỏ qua response cũ.
      if (
        requestVersion !== narrationRequestVersion ||
        locationSelect?.value !== requestedLocationId ||
        languageSelect?.value !== requestedLanguageCode
      ) return;

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

      displayNarration(narration);

      if (resultSection) {
        resultSection.scrollIntoView({
          behavior: "smooth",
          block: "start",
        });
      }

      if (narration.audioUrl && audioPlayer) {
        try {
          await audioPlayer.play();
        } catch (error) {
          if (requestVersion !== narrationRequestVersion) return;
          console.warn("Trình duyệt chặn tự phát âm thanh:", error);
          showError(
            "Bài thuyết minh đã tải. Trình duyệt chặn tự phát, hãy nhấn nút phát trong trình nghe.",
          );
        }
      }
    } catch (error) {
      if (requestVersion !== narrationRequestVersion) return;
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
      if (submitBtn && requestVersion === narrationRequestVersion) {
        submitBtn.disabled = false;
        submitBtn.removeAttribute("aria-busy");
        setButtonLabel(
          "#submitBtn",
          (siteTranslations[activeSiteLanguage] || siteTranslations.vi).submit,
        );
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
// 7. KHOI TAO BAN DO LEAFLET
// ======================================================

function calculateDistanceMeters(lat1, lon1, lat2, lon2) {
  const earthRadiusMeters = 6371000;
  const toRadians = (degrees) => degrees * Math.PI / 180;
  const deltaLatitude = toRadians(lat2 - lat1);
  const deltaLongitude = toRadians(lon2 - lon1);
  const a = Math.sin(deltaLatitude / 2) ** 2 +
    Math.cos(toRadians(lat1)) * Math.cos(toRadians(lat2)) *
    Math.sin(deltaLongitude / 2) ** 2;
  const boundedA = Math.min(1, Math.max(0, a));
  return 2 * earthRadiusMeters * Math.asin(Math.sqrt(boundedA));
}

// Cập nhật dấu vị trí GPS tự động, không phụ thuộc nút "Tìm vị trí gần tôi".
function updateUserLocationOnMap(latitude, longitude, centerMap = false) {
  currentUserCoordinates = [latitude, longitude];
  renderPlacesList();

  if (!vietGuideMap || typeof L === "undefined") return;

  if (!userLocationMarker) {
    const icon = L.divIcon({
      className: "",
      html: '<span class="gps-user-marker"></span>',
      iconSize: [18, 18],
      iconAnchor: [9, 9],
    });

    userLocationMarker = L.marker([latitude, longitude], {
      icon,
      zIndexOffset: 1000,
    })
      .addTo(vietGuideMap)
      .bindPopup("Vị trí hiện tại của bạn");
  } else {
    userLocationMarker.setLatLng([latitude, longitude]);
  }

  // Chỉ tự căn bản đồ ở lần định vị đầu tiên để không cản người dùng kéo bản đồ.
  if (centerMap && !hasCenteredOnUserLocation) {
    vietGuideMap.setView(
      [latitude, longitude],
      Math.max(vietGuideMap.getZoom(), 15)
    );
    hasCenteredOnUserLocation = true;
  }
}

function getCurrentPosition() {
  return new Promise((resolve, reject) => {
    if (!navigator.geolocation) {
      reject(new Error("Trình duyệt này không hỗ trợ GPS. Bạn vẫn có thể chọn địa điểm thủ công."));
      return;
    }
    navigator.geolocation.getCurrentPosition(resolve, (error) => {
      let message = "Không lấy được vị trí hiện tại. Bạn vẫn có thể chọn địa điểm thủ công.";
      if (error.code === error.PERMISSION_DENIED) {
        message = "Bạn chưa cấp quyền vị trí. Hãy cho phép GPS trong trình duyệt hoặc chọn địa điểm thủ công.";
      } else if (error.code === error.TIMEOUT) {
        message = "GPS phản hồi quá lâu. Hãy thử lại hoặc chọn địa điểm thủ công.";
      }
      reject(new Error(message));
    }, { enableHighAccuracy: true, timeout: 12000, maximumAge: 10000 });
  });
}

async function findNearbyLocation() {
  const oldText = findNearbyButton?.textContent.trim() || "";

  if (findNearbyButton) {
    findNearbyButton.disabled = true;
    findNearbyButton.textContent = "Đang xác định vị trí...";
  }

  try {
    const position = await getCurrentPosition();

    const userLat = position.coords.latitude;
    const userLon = position.coords.longitude;
    const accuracy = position.coords.accuracy;
    currentUserCoordinates = [userLat, userLon];
    renderPlacesList();

    console.log("=== KIỂM TRA GPS ===");
    console.log("Vĩ độ:", userLat);
    console.log("Kinh độ:", userLon);
    console.log("Độ chính xác ước tính:", accuracy, "m");
    console.log(
      "Tọa độ Google Maps:",
      `https://www.google.com/maps?q=${userLat},${userLon}`
    );

    // Nút tìm vị trí dùng chung hàm cập nhật với GPS tự động.
    updateUserLocationOnMap(userLat, userLon, true);
    if (userLocationMarker) userLocationMarker.openPopup();

    console.table(
      locations.map((location) => ({
        name: getLocationName(location),
        latitude: getLocationCoordinates(location)?.[0] ?? "Không có",
        longitude: getLocationCoordinates(location)?.[1] ?? "Không có",
      }))
    );

    // Tính khoảng cách từ vị trí người dùng đến các địa điểm.
    const nearby = locations
      .map((location) => {
        const coordinates = getLocationCoordinates(location);

        if (!coordinates) {
          return null;
        }

        return {
          location,
          distance: calculateDistanceMeters(
            userLat,
            userLon,
            coordinates[0],
            coordinates[1]
          ),
          coordinates,
        };
      })
      .filter(Boolean)
      .sort((a, b) => a.distance - b.distance);

    if (!nearby.length) {
      setMapMessage(
        "Đã xác định vị trí của bạn, nhưng chưa có địa điểm nào được khai báo tọa độ. Bạn có thể chọn thủ công trong danh sách."
      );
      return;
    }

    const nearest = nearby[0];

    const distanceText =
      nearest.distance < 1000
        ? `${Math.round(nearest.distance)} m`
        : `${(nearest.distance / 1000).toFixed(2)} km`;

    if (nearest.distance <= NEARBY_RADIUS_METERS) {
      selectVietGuideLocation(nearest.location);

      setMapMessage(
        `Bạn đang cách ${getLocationName(nearest.location)} khoảng ${distanceText}, trong phạm vi ${NEARBY_RADIUS_METERS} m.`
      );
    } else {
      setMapMessage(
        `Địa điểm gần nhất là ${getLocationName(nearest.location)}, cách bạn khoảng ${distanceText}; ngoài phạm vi ${NEARBY_RADIUS_METERS} m. Bạn có thể chọn địa điểm gần nhất trên bản đồ hoặc trong danh sách.`
      );

      if (vietGuideMap) {
        vietGuideMap.setView(
          nearest.coordinates,
          Math.max(vietGuideMap.getZoom(), 15)
        );
      }
    }
  } catch (error) {
    setMapMessage(
      error.message ||
      "Không thể xác định vị trí. Bạn vẫn có thể chọn địa điểm thủ công."
    );
  } finally {
    if (findNearbyButton) {
      findNearbyButton.disabled = false;
      findNearbyButton.textContent =
        oldText || "⌖ Tìm địa điểm gần tôi";
    }
  }
}

let autoNarrationKey = "";
let gpsWatchId = null;
let currentNearbyLocation = null;

async function autoPlayNearbyNarration(
  location,
  { force = false, fromPlacesList = false } = {},
) {
  const locationId = String(getLocationId(location));
  const languageCode = String(languageSelect?.value || "");
  const translations =
    siteTranslations[activeSiteLanguage] || siteTranslations.vi;

  if (!locationId || !languageCode) {
    if (fromPlacesList) {
      setPlacesPlaybackStatus(translations.placesNarrationSelectLanguage, true);
    }
    setMapMessage("Hãy chọn ngôn ngữ thuyết minh để tiếp tục.");
    return;
  }

  const key = `${locationId}_${languageCode}`;

  // Không phát lại liên tục khi GPS cập nhật nhiều lần.
  if (!force && autoNarrationKey === key) {
    return;
  }

  autoNarrationKey = key;
  if (fromPlacesList) {
    setPlacesPlaybackStatus(
      `${translations.placesNarrationLoading} ${getLocationName(location)}...`,
    );
  }

  if (locationSelect) {
    locationSelect.value = locationId;
    locationSelect.dispatchEvent(
      new Event("change", { bubbles: true })
    );
  }

  // Mã phiên giúp bỏ qua phản hồi cũ nếu khách đổi ngôn ngữ/địa điểm
  // trong lúc API đang tải narration.
  const requestVersion = ++narrationRequestVersion;

  try {
    const response = await getJson(
      `/api/narrations/${encodeURIComponent(locationId)}/${encodeURIComponent(languageCode)}`
    );

    if (
      requestVersion !== narrationRequestVersion ||
      String(languageSelect?.value || "") !== languageCode ||
      String(locationSelect?.value || "") !== locationId
    ) {
      return;
    }

    const data =
      response?.data && typeof response.data === "object"
        ? response.data
        : response;

    const selectedLanguage = languages.find(
      (language) =>
        String(getLanguageCode(language)) === languageCode
    );

    const narration = {
      locationId,
      locationName: getFirstValue(
        data,
        ["locationName", "name"],
        getLocationName(location)
      ),
      languageCode,
      languageName: getFirstValue(
        data,
        ["languageName"],
        selectedLanguage
          ? getLanguageName(selectedLanguage)
          : languageCode
      ),
      text: getFirstValue(
        data,
        ["text", "content", "narrationText"],
        ""
      ),
      status: getFirstValue(data, ["status"], ""),
      audioUrl: getFirstValue(
        data,
        ["audioUrl", "audioURL", "audio"],
        ""
      ),
    };

    if (!narration.text) {
      throw new Error("Địa điểm này chưa có nội dung thuyết minh.");
    }

    displayNarration(narration);

    if (audioPlayer && narration.audioUrl) {
      audioPlayer.controls = true;
      if (fromPlacesList && resultSection) {
        resultSection.scrollIntoView({
          behavior: "smooth",
          block: "start",
        });
      }

      try {
        await audioPlayer.play();
        if (requestVersion !== narrationRequestVersion) return;
        if (fromPlacesList) {
          setPlacesPlaybackStatus(
            `${translations.placesNarrationPlaying} ${getLocationName(location)}.`,
          );
        }
        setMapMessage(
          `Bạn đã đến gần ${getLocationName(location)}. Đang tự động phát thuyết minh.`
        );
      } catch (error) {
        if (requestVersion !== narrationRequestVersion) return;
        console.warn("Trình duyệt chặn tự phát âm thanh:", error);
        if (fromPlacesList) {
          setPlacesPlaybackStatus(
            translations.placesNarrationAutoplayBlocked,
            true,
          );
        }

        setMapMessage(
          `Đã tìm thấy ${getLocationName(location)}. Hãy nhấn nút phát trên trình phát âm thanh để bắt đầu nghe.`
        );
      }
    } else {
      if (fromPlacesList) {
        setPlacesPlaybackStatus(translations.placesNarrationNoAudio, true);
      }
      setMapMessage(
        `Đã tìm thấy ${getLocationName(location)}, nhưng địa điểm này chưa có đường dẫn âm thanh.`
      );
    }
  } catch (error) {
    // Không hiển thị lỗi từ một yêu cầu cũ sau khi khách đã đổi ngôn ngữ.
    if (requestVersion !== narrationRequestVersion) {
      return;
    }

    console.error("Lỗi tự động phát thuyết minh:", error);

    // Cho phép thử lại nếu tải bài thuyết minh thất bại.
    autoNarrationKey = "";
    if (fromPlacesList) {
      setPlacesPlaybackStatus(
        error.status === 404
          ? translations.placesNarrationNotFound
          : error.message || translations.placesNarrationError,
        true,
      );
    }

    setMapMessage(
      error.message || "Không thể tải bài thuyết minh."
    );
  }
}

function startAutomaticNearbyNarration() {
  if (!("geolocation" in navigator)) {
    setMapMessage("Trình duyệt không hỗ trợ GPS.");
    return;
  }

  if (gpsWatchId !== null) {
    navigator.geolocation.clearWatch(gpsWatchId);
  }

  gpsWatchId = navigator.geolocation.watchPosition(
    (position) => {
      const userLat = position.coords.latitude;
      const userLon = position.coords.longitude;

      // Hiện vị trí GPS ngay khi trình duyệt lấy được tọa độ, không cần bấm nút.
      updateUserLocationOnMap(userLat, userLon, true);

      const nearby = locations
        .map((location) => {
          const coordinates = getLocationCoordinates(location);

          if (!coordinates) return null;

          return {
            location,
            distance: calculateDistanceMeters(
              userLat,
              userLon,
              coordinates[0],
              coordinates[1]
            ),
          };
        })
        .filter(Boolean)
        .sort((a, b) => a.distance - b.distance);

      const nearest = nearby[0];

      if (!nearest || nearest.distance > NEARBY_RADIUS_METERS) {
        currentNearbyLocation = null;
        // Reset only after moving clearly outside the 50 m zone. A small
        // 25 m buffer prevents GPS jitter around the boundary from replaying audio.
        if (!nearest || nearest.distance > NEARBY_RADIUS_METERS + 25) {
          autoNarrationKey = "";
        }

        setMapMessage(
          `Đang tìm địa điểm trong phạm vi ${NEARBY_RADIUS_METERS} m...`
        );
        return;
      }

      currentNearbyLocation = nearest.location;
      autoPlayNearbyNarration(nearest.location);
    },
    (error) => {
      console.error("Không lấy được GPS:", error);

      setMapMessage(
        "Không thể xác định vị trí. Hãy cấp quyền GPS cho trình duyệt."
      );
    },
    {
      enableHighAccuracy: true,
      timeout: 15000,
      maximumAge: 0,
    }
  );
}

// Đăng ký sự kiện một lần, bên ngoài hàm findNearbyLocation().
if (findNearbyButton) {
  findNearbyButton.addEventListener("click", findNearbyLocation);
}

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
    ["latitude", "Latitude", "lat", "viDo", "ViDo"],
    getFirstValue(
      coordinates,
      ["latitude", "Latitude", "lat", "viDo", "ViDo"],
      null
    )
  );

  const longitudeValue = getFirstValue(
    location,
    ["longitude", "Longitude", "lng", "lon", "kinhDo", "KinhDo"],
    getFirstValue(
      coordinates,
      ["longitude", "Longitude", "lng", "lon", "kinhDo", "KinhDo"],
      null
    )
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

function selectVietGuideLocation(
  location,
  { scrollToForm = true, announce = true } = {},
) {
  if (!location || !locationSelect) {
    return;
  }

  const id = getLocationId(location);

  locationSelect.value = String(id);

  // Thông báo cho các listener hiện có khi chọn địa điểm từ bản đồ.
  locationSelect.dispatchEvent(new Event("change", { bubbles: true }));

  const createSection = scrollToForm
    ? document.getElementById("create")
    : null;
  if (createSection) {
    createSection.scrollIntoView({
      behavior: "smooth",
      block: "start",
    });
  }

  if (announce) {
    setMapMessage(
      `Đã chọn địa điểm: ${getLocationName(location)}. Hãy chọn ngôn ngữ và tạo bài thuyết minh.`,
    );
  }
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



// Ghi nhận lượt nghe chỉ khi audio phát liên tục đủ 15 giây.
(() => {
  if (!audioPlayer) return;

  let listenedMs = 0;
  let lastTick = null;
  let lastAudioTime = null;
  let recorded = false;
  let recording = false;
  let activeNarrationKey = "";

  function resetListenSession() {
    listenedMs = 0;
    lastTick = null;
    lastAudioTime = null;
    recorded = false;
    recording = false;
  }

  function resetContinuousTimer() {
    // Yêu cầu là 15 giây liên tục, nên pause/seek sẽ đặt lại thời gian.
    listenedMs = 0;
    lastTick = null;
    lastAudioTime = null;
  }

  audioPlayer.addEventListener("play", () => {
    lastTick = performance.now();
    lastAudioTime = audioPlayer.currentTime;
  });
  audioPlayer.addEventListener("pause", resetContinuousTimer);
  audioPlayer.addEventListener("seeking", resetContinuousTimer);
  audioPlayer.addEventListener("seeked", () => {
    if (!audioPlayer.paused) {
      lastTick = performance.now();
      lastAudioTime = audioPlayer.currentTime;
    }
  });
  audioPlayer.addEventListener("emptied", resetListenSession);
  audioPlayer.addEventListener("ended", resetListenSession);

  setInterval(async () => {
    if (audioPlayer.paused || audioPlayer.ended || audioPlayer.seeking || !audioPlayer.currentSrc || recorded || recording) {
      lastTick = null;
      lastAudioTime = null;
      return;
    }

    const now = performance.now();
    const currentAudioTime = audioPlayer.currentTime;
    if (lastTick !== null && lastAudioTime !== null) {
      const elapsed = now - lastTick;
      const audioProgress = currentAudioTime - lastAudioTime;
      if (audioProgress > 0 && audioProgress < 1.5 && elapsed > 0 && elapsed < 1500) {
        listenedMs += elapsed;
      } else if (audioProgress <= 0 || audioProgress >= 1.5 || elapsed >= 1500) {
        listenedMs = 0;
      }
    }
    lastTick = now;
    lastAudioTime = currentAudioTime;

    if (listenedMs >= 15000) {
      recorded = true;
      recording = true;
      try {
        const selectedLocation = locations.find((location) => String(getLocationId(location)) === String(locationSelect?.value || ""));
        const selectedLanguage = languageSelect?.value || null;

        // visitorId do visitor-counter.js tạo và lưu trong localStorage.
        const visitorId = localStorage.getItem("vietguide_visitor_id");

        const parsedLocationId = selectedLocation
          ? Number(getLocationId(selectedLocation))
          : NaN;

        const response = await fetch("/api/audio-listens", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            listenId: crypto.randomUUID(),
            visitorId: visitorId || null,
            locationId: Number.isInteger(parsedLocationId) ? parsedLocationId : null,
            locationName: selectedLocation ? getLocationName(selectedLocation) : (locationSelect?.selectedOptions?.[0]?.textContent?.trim() || null),
            languageCode: selectedLanguage,
          }),
        });
        if (!response.ok) throw new Error(`Không lưu được lượt nghe (HTTP ${response.status}).`);
        console.log("Đã ghi nhận lượt nghe đủ 15 giây liên tục.");
      } catch (error) {
        // Không tự tạo nhiều lượt nghe trùng trong cùng một phiên phát.
        console.error("Lỗi ghi nhận lượt nghe:", error);
      } finally {
        recording = false;
      }
    }




  },
    250);
})();
/* ===== DOI NGON NGU GIAO DIEN VIETGUIDE ===== */

const siteTranslations = {
  vi: {
    navDiscover: "Khám phá",
    navHow: "Cách hoạt động",
    navAbout: "Giới thiệu",
    navMap: "Bản đồ",
    navCta: "Xem thuyết minh",
    eyebrow: "HÀNH TRÌNH CỦA RIÊNG BẠN",
    heroTitle: 'Mỗi điểm đến<br />đều có <em>một câu chuyện.</em>',
    heroDescription:
      "Khám phá những câu chuyện văn hóa và lịch sử Việt Nam qua lời thuyết minh bằng ngôn ngữ bạn yêu thích.",
    heroLink: "Bắt đầu khám phá",
    heroNote: "DI SẢN · VĂN HÓA · CON NGƯỜI",
    introKicker: "KHÁM PHÁ THEO CÁCH CỦA BẠN",
    introTitle: 'Đi xa hơn.<br /><em>Hiểu sâu hơn.</em>',
    feature1: "Địa điểm nổi bật",
    feature2: "Đa ngôn ngữ",
    feature3: "Nghe và cảm nhận",
    mapKicker: "KHÁM PHÁ VIỆT NAM",
    placesKicker: "ĐIỂM ĐẾN DÀNH CHO BẠN",
    placesTitle: 'Khám phá <em>các địa điểm.</em>',
    placesLanguageLabel: "Ngôn ngữ",
    placesLoading: "Đang tải danh sách địa điểm...",
    placesDescription: "Các địa điểm được sắp xếp từ gần đến xa dựa trên vị trí hiện tại của bạn.",
    placesEmpty: "Chưa có địa điểm nào để hiển thị.",
    placesLoadError: "Không tải được danh sách địa điểm. Vui lòng thử tải lại trang.",
    placesNarrationSelectLanguage: "Vui lòng chọn ngôn ngữ thuyết minh trước.",
    placesNarrationLoading: "Đang tải thuyết minh cho",
    placesNarrationPlaying: "Đang phát thuyết minh cho",
    placesNarrationAutoplayBlocked: "Trình duyệt chưa cho phép tự phát. Hãy nhấn nút phát trong trình nghe bên dưới.",
    placesNarrationNoAudio: "Bài thuyết minh này chưa có audio.",
    placesNarrationNotFound: "Chưa có bài thuyết minh cho địa điểm và ngôn ngữ này.",
    placesNarrationError: "Không thể tải bài thuyết minh. Vui lòng thử lại.",
    distanceUnavailable: "Chưa xác định khoảng cách",
    placeSelect: "Chọn địa điểm",
    mapTitle: 'Bạn muốn khám phá <em>ở đâu?</em>',
    mapDescription:
      "Chọn một địa điểm trên bản đồ để bắt đầu hành trình thuyết minh đa ngôn ngữ.",
    gpsButton: "Tìm địa điểm gần tôi",
    builderKicker: "TẠO HÀNH TRÌNH CỦA BẠN",
    builderTitle: 'Câu chuyện tiếp theo<br />bắt đầu <em>từ đây.</em>',
    formTitle: "Nghe thuyết minh",
    formSubtitle: "Chọn địa điểm và ngôn ngữ để nghe thuyết minh.",
    locationLabel: "Bạn muốn khám phá đâu?",
    languageLabel: "Ngôn ngữ thuyết minh",
    submit: "Nghe thuyết minh",
    loading: "Đang tải thuyết minh...",
    resultKicker: "CÂU CHUYỆN DÀNH CHO BẠN",
    resultTitle: 'Điều thú vị <em>đang chờ bạn.</em>',
    copy: "Sao chép nội dung ↗",
    footer: "Khám phá Việt Nam, theo cách của bạn."
  },

  en: {
    navDiscover: "Discover",
    navHow: "How it works",
    navAbout: "About",
    navMap: "Map",
    navCta: "Listen to narration",
    eyebrow: "YOUR OWN JOURNEY",
    heroTitle: 'Every destination<br />has <em>a story.</em>',
    heroDescription:
      "Discover Vietnam's culture and history through narration in your preferred language.",
    heroLink: "Start exploring",
    heroNote: "HERITAGE · CULTURE · PEOPLE",
    introKicker: "EXPLORE YOUR WAY",
    introTitle: 'Go further.<br /><em>Understand deeper.</em>',
    feature1: "Featured destinations",
    feature2: "Multilingual",
    feature3: "Listen and discover",
    mapKicker: "EXPLORE VIETNAM",
    placesKicker: "DESTINATIONS FOR YOU",
    placesTitle: 'Explore <em>the destinations.</em>',
    placesLanguageLabel: "Language",
    placesLoading: "Loading destinations...",
    placesDescription: "Destinations are sorted from nearest to farthest based on your current location.",
    placesEmpty: "There are no destinations to display yet.",
    placesLoadError: "Could not load destinations. Please reload the page.",
    placesNarrationSelectLanguage: "Please choose a narration language first.",
    placesNarrationLoading: "Loading narration for",
    placesNarrationPlaying: "Playing narration for",
    placesNarrationAutoplayBlocked: "Your browser blocked autoplay. Press play in the audio player below.",
    placesNarrationNoAudio: "This narration does not have an audio file yet.",
    placesNarrationNotFound: "No narration is available for this destination and language.",
    placesNarrationError: "Could not load the narration. Please try again.",
    distanceUnavailable: "Distance unavailable",
    placeSelect: "Select destination",
    mapTitle: 'Where would you like <em>to explore?</em>',
    mapDescription:
      "Choose a place on the map to begin your multilingual audio tour.",
    gpsButton: "Find places near me",
    builderKicker: "CREATE YOUR JOURNEY",
    builderTitle: 'Your next story<br />starts <em>here.</em>',
    formTitle: "Listen to narration",
    formSubtitle: "Choose a destination and language.",
    locationLabel: "Where would you like to explore?",
    languageLabel: "Narration language",
    submit: "Listen to narration",
    loading: "Loading narration...",
    resultKicker: "YOUR STORY",
    resultTitle: 'Something wonderful <em>awaits you.</em>',
    copy: "Copy narration ↗",
    footer: "Discover Vietnam, your way."
  },

  ja: {
    navDiscover: "探訪",
    navHow: "使い方",
    navAbout: "紹介",
    navMap: "地図",
    navCta: "音声ガイドを聞く",
    eyebrow: "あなただけの旅",
    heroTitle: "すべての場所に<br /><em>物語があります。</em>",
    heroDescription: "お好みの言語で、ベトナムの文化と歴史を音声ガイドで楽しみましょう。",
    heroLink: "探検を始める",
    heroNote: "遺産 · 文化 · 人々",
    introKicker: "自分らしい探検",
    introTitle: "もっと遠くへ。<br /><em>もっと深く。</em>",
    feature1: "おすすめの場所",
    feature2: "多言語対応",
    feature3: "聞いて感じる",
    mapKicker: "ベトナムを探検",
    placesKicker: "おすすめの目的地",
    placesTitle: "目的地を <em>探検しましょう。</em>",
    placesLanguageLabel: "言語",
    placesLoading: "目的地を読み込んでいます...",
    placesDescription: "現在地から近い順に目的地を表示します。",
    placesEmpty: "表示できる目的地はありません。",
    placesLoadError: "目的地を読み込めませんでした。ページを再読み込みしてください。",
    placesNarrationSelectLanguage: "先に音声ガイドの言語を選択してください。",
    placesNarrationLoading: "音声ガイドを読み込んでいます:",
    placesNarrationPlaying: "音声ガイドを再生中:",
    placesNarrationAutoplayBlocked: "自動再生がブロックされました。下のプレーヤーで再生を押してください。",
    placesNarrationNoAudio: "この音声ガイドには音声ファイルがありません。",
    placesNarrationNotFound: "この目的地と言語の音声ガイドはありません。",
    placesNarrationError: "音声ガイドを読み込めませんでした。もう一度お試しください。",
    distanceUnavailable: "距離を確認できません",
    placeSelect: "目的地を選択",
    mapTitle: "どこを <em>探検しますか？</em>",
    mapDescription: "地図から場所を選び、多言語音声ガイドを始めましょう。",
    gpsButton: "近くの場所を探す",
    builderKicker: "旅を作る",
    builderTitle: "次の物語は<br /><em>ここから。</em>",
    formTitle: "音声ガイド",
    formSubtitle: "場所と言語を選択してください。",
    locationLabel: "どこを探検しますか？",
    languageLabel: "音声ガイドの言語",
    submit: "音声ガイドを聞く",
    loading: "音声ガイドを読み込み中...",
    resultKicker: "あなたの物語",
    resultTitle: "素敵な発見が <em>待っています。</em>",
    copy: "内容をコピー ↗",
    footer: "あなたらしい方法でベトナムを探検。"
  },

  ko: {
    navDiscover: "탐험",
    navHow: "이용 방법",
    navAbout: "소개",
    navMap: "지도",
    navCta: "오디오 가이드 듣기",
    eyebrow: "나만의 여행",
    heroTitle: "모든 여행지에는<br /><em>이야기가 있습니다.</em>",
    heroDescription: "원하는 언어로 베트남의 문화와 역사를 오디오 가이드로 만나보세요.",
    heroLink: "탐험 시작하기",
    heroNote: "유산 · 문화 · 사람",
    introKicker: "나만의 방식으로 탐험",
    introTitle: "더 멀리.<br /><em>더 깊이.</em>",
    feature1: "추천 여행지",
    feature2: "다국어 지원",
    feature3: "듣고 느끼기",
    mapKicker: "베트남 탐험",
    placesKicker: "추천 여행지",
    placesTitle: "여행지를 <em>탐험해 보세요.</em>",
    placesLanguageLabel: "언어",
    placesLoading: "여행지를 불러오는 중...",
    placesDescription: "현재 위치에서 가까운 순서로 여행지를 표시합니다.",
    placesEmpty: "표시할 여행지가 없습니다.",
    placesLoadError: "여행지를 불러오지 못했습니다. 페이지를 새로고침해 주세요.",
    placesNarrationSelectLanguage: "먼저 오디오 가이드 언어를 선택해 주세요.",
    placesNarrationLoading: "오디오 가이드 불러오는 중:",
    placesNarrationPlaying: "오디오 가이드 재생 중:",
    placesNarrationAutoplayBlocked: "브라우저에서 자동 재생을 차단했습니다. 아래 플레이어에서 재생을 눌러 주세요.",
    placesNarrationNoAudio: "이 오디오 가이드에는 아직 오디오 파일이 없습니다.",
    placesNarrationNotFound: "이 여행지와 언어에 대한 오디오 가이드가 없습니다.",
    placesNarrationError: "오디오 가이드를 불러오지 못했습니다. 다시 시도해 주세요.",
    distanceUnavailable: "거리 확인 불가",
    placeSelect: "여행지 선택",
    mapTitle: "어디를 <em>탐험하고 싶으신가요?</em>",
    mapDescription: "지도에서 장소를 선택하고 다국어 오디오 가이드를 시작하세요.",
    gpsButton: "내 주변 장소 찾기",
    builderKicker: "나만의 여행 만들기",
    builderTitle: "다음 이야기는<br /><em>여기서 시작됩니다.</em>",
    formTitle: "오디오 가이드 듣기",
    formSubtitle: "장소와 언어를 선택하세요.",
    locationLabel: "어디를 탐험하고 싶으신가요?",
    languageLabel: "가이드 언어",
    submit: "오디오 가이드 듣기",
    loading: "오디오 가이드 불러오는 중...",
    resultKicker: "당신을 위한 이야기",
    resultTitle: "새로운 발견이 <em>기다립니다.</em>",
    copy: "내용 복사 ↗",
    footer: "나만의 방식으로 베트남을 탐험하세요."
  },

  fr: {
    navDiscover: "Découvrir",
    navHow: "Comment ça marche",
    navAbout: "À propos",
    navMap: "Carte",
    navCta: "Écouter le guide",
    eyebrow: "VOTRE PROPRE VOYAGE",
    heroTitle: "Chaque destination<br />a <em>une histoire.</em>",
    heroDescription: "Découvrez la culture et l'histoire du Vietnam grâce à un guide audio dans votre langue préférée.",
    heroLink: "Commencer l'exploration",
    heroNote: "PATRIMOINE · CULTURE · POPULATION",
    introKicker: "EXPLOREZ À VOTRE FAÇON",
    introTitle: "Allez plus loin.<br /><em>Comprenez mieux.</em>",
    feature1: "Lieux incontournables",
    feature2: "Multilingue",
    feature3: "Écouter et découvrir",
    mapKicker: "EXPLORER LE VIETNAM",
    placesKicker: "DESTINATIONS POUR VOUS",
    placesTitle: "Explorez <em>les destinations.</em>",
    placesLanguageLabel: "Langue",
    placesLoading: "Chargement des destinations...",
    placesDescription: "Les lieux sont classés du plus proche au plus éloigné selon votre position actuelle.",
    placesEmpty: "Aucune destination à afficher pour le moment.",
    placesLoadError: "Impossible de charger les destinations. Rechargez la page.",
    placesNarrationSelectLanguage: "Veuillez d'abord choisir la langue du guide.",
    placesNarrationLoading: "Chargement du guide pour",
    placesNarrationPlaying: "Lecture du guide pour",
    placesNarrationAutoplayBlocked: "La lecture automatique est bloquée. Appuyez sur Lecture dans le lecteur ci-dessous.",
    placesNarrationNoAudio: "Ce guide audio ne contient pas encore de fichier audio.",
    placesNarrationNotFound: "Aucun guide n'est disponible pour cette destination et cette langue.",
    placesNarrationError: "Impossible de charger le guide. Veuillez réessayer.",
    distanceUnavailable: "Distance indisponible",
    placeSelect: "Choisir cette destination",
    mapTitle: "Où souhaitez-vous <em>explorer ?</em>",
    mapDescription: "Choisissez un lieu sur la carte pour commencer votre visite audio multilingue.",
    gpsButton: "Trouver des lieux près de moi",
    builderKicker: "CRÉEZ VOTRE VOYAGE",
    builderTitle: "Votre prochaine histoire<br />commence <em>ici.</em>",
    formTitle: "Guide audio",
    formSubtitle: "Choisissez un lieu et une langue.",
    locationLabel: "Où souhaitez-vous explorer ?",
    languageLabel: "Langue du guide",
    submit: "Écouter le guide",
    loading: "Chargement du guide...",
    resultKicker: "VOTRE HISTOIRE",
    resultTitle: "De belles découvertes <em>vous attendent.</em>",
    copy: "Copier le contenu ↗",
    footer: "Découvrez le Vietnam à votre façon."
  }
};
function setButtonLabel(selector, value) {
  const element = document.querySelector(selector);
  if (!element || value === undefined) return;

  const textNode = Array.from(element.childNodes).find(
    (node) =>
      node.nodeType === Node.TEXT_NODE && node.textContent.trim()
  );

  if (textNode) {
    textNode.textContent = value + " ";
  } else {
    element.insertBefore(
      document.createTextNode(value + " "),
      element.firstChild
    );
  }
}
function setSiteLanguage(language) {
  const translations = siteTranslations[language] || siteTranslations.vi;
  activeSiteLanguage = siteTranslations[language] ? language : "vi";
  activePlacesLanguage = language;

  document.documentElement.lang = language;
  if (placesLanguageSelect) {
    const optionExists = Array.from(placesLanguageSelect.options).some(
      (option) => option.value.toLowerCase() === language.toLowerCase(),
    );
    if (optionExists) placesLanguageSelect.value = language;
  }

  const label = document.getElementById("siteLanguageLabel");
  if (label) {
    const labels = {
      vi: "Tiếng Việt",
      en: "English",
      ja: "日本語",
      ko: "한국어",
      fr: "Français"
    };
    label.textContent = labels[language] || labels.vi;
  }

  const setText = (selector, value) => {
    const element = document.querySelector(selector);
    if (element && value !== undefined) element.textContent = value;
  };

  const setHTML = (selector, value) => {
    const element = document.querySelector(selector);
    if (element && value !== undefined) element.innerHTML = value;
  };

  const navLinks = document.querySelectorAll(".topbar nav a");
  const navKeys = ["navDiscover", "navHow", "navAbout", "navMap"];
  navLinks.forEach((link, index) => {
    if (translations[navKeys[index]]) {
      link.textContent = translations[navKeys[index]];
    }
  });

  setButtonLabel(".nav-cta", translations.navCta);
  setText(".eyebrow", translations.eyebrow);
  setHTML(".hero-content h1", translations.heroTitle);
  setText(".hero-content > p", translations.heroDescription);
  setButtonLabel(".hero-link", translations.heroLink);
  setText(".hero-note", translations.heroNote);

  setText(".intro .section-kicker", translations.introKicker);
  setHTML(".intro h2", translations.introTitle);

  const featureTitles = document.querySelectorAll(".feature h3");
  [translations.feature1, translations.feature2, translations.feature3]
    .forEach((text, index) => {
      if (featureTitles[index]) featureTitles[index].textContent = text;
    });

  setText("#mapExplore .section-label", translations.mapKicker);
  setText("#placesKicker", translations.placesKicker);
  setHTML("#placesTitle", translations.placesTitle);
  setText("#placesLanguageLabel", translations.placesLanguageLabel);
  const placesLanguageLabel = document.getElementById("placesLanguageSelect");
  if (placesLanguageLabel) {
    placesLanguageLabel.setAttribute("aria-label", translations.placesLanguageLabel);
  }
  setText("#placesDescription", translations.placesDescription);
  setHTML("#mapExplore h2", translations.mapTitle);
  setText("#mapExplore .section-description", translations.mapDescription);
  setButtonLabel("#findNearbyButton", translations.gpsButton);

  setText(".builder-copy .section-kicker", translations.builderKicker);
  setHTML(".builder-copy h2", translations.builderTitle);
  setText(".form-card h3", translations.formTitle);
  setText(".form-subtitle", translations.formSubtitle);

  const labels = document.querySelectorAll("#narrationForm label");
  if (labels[0]) labels[0].textContent = translations.locationLabel;
  if (labels[1]) labels[1].textContent = translations.languageLabel;

  setButtonLabel("#submitBtn", translations.submit);
  setText(".result-section .section-kicker", translations.resultKicker);
  setHTML(".result-head h2", translations.resultTitle);
  setText("#copyBtn", translations.copy);
  setText("footer > span:nth-child(2)", translations.footer);

  document.dispatchEvent(
    new CustomEvent("siteLanguageChange", { detail: { language } })
  );
  renderPlacesList();
}

function getPreferredSiteLanguage() {
  const browserLanguages = [
    ...(navigator.languages || []),
    navigator.language || "",
  ];

  for (const browserLanguage of browserLanguages) {
    const normalized = browserLanguage.toLowerCase();
    if (siteTranslations[normalized]) return normalized;

    const baseLanguage = normalized.split("-")[0];
    if (siteTranslations[baseLanguage]) return baseLanguage;
  }

  return "vi";
}

function initSiteLanguageMenu() {
  const button = document.getElementById("siteLanguageButton");
  const menu = document.getElementById("siteLanguageMenu");

  if (!button || !menu) return;

  button.addEventListener("click", (event) => {
    event.stopPropagation();
    menu.hidden = !menu.hidden;
    button.setAttribute("aria-expanded", String(!menu.hidden));
  });

  menu.querySelectorAll("[data-site-language]").forEach((item) => {
    item.addEventListener("click", () => {
      setSiteLanguage(item.dataset.siteLanguage);
      menu.hidden = true;
      button.setAttribute("aria-expanded", "false");
    });
  });

  if (placesLanguageSelect) {
    placesLanguageSelect.addEventListener("change", () => {
      const language = placesLanguageSelect.value;
      activePlacesLanguage = language;
      placesLanguageManuallySelected = true;
      if (siteTranslations[language]) {
        setSiteLanguage(language);
      } else {
        renderPlacesList();
      }

      const narrationOption = Array.from(languageSelect?.options || []).find(
        (option) => option.value.toLowerCase() === language.toLowerCase(),
      );
      if (languageSelect && narrationOption) {
        languageSelect.value = narrationOption.value;
        languageSelect.dispatchEvent(new Event("change", { bubbles: true }));
      }
    });
  }

  document.addEventListener("click", (event) => {
    if (!event.target.closest(".site-language")) {
      menu.hidden = true;
      button.setAttribute("aria-expanded", "false");
    }
  });

  setSiteLanguage(getPreferredSiteLanguage());
}

if (document.readyState === "loading") {
  document.addEventListener("DOMContentLoaded", initSiteLanguageMenu);
} else {
  initSiteLanguageMenu();
}
