const API_BASE = ""; // Frontend nằm trong wwwroot nên dùng cùng host với API.
const locationSelect = document.getElementById("locationSelect");
const languageSelect = document.getElementById("languageSelect");
const locationPreview = document.getElementById("locationPreview");
const form = document.getElementById("narrationForm");
const submitBtn = document.getElementById("submitBtn");
const errorMessage = document.getElementById("errorMessage");
const resultSection = document.getElementById("resultSection");
let locations = [];

async function getJson(url, options = {}) {
  const response = await fetch(`${API_BASE}${url}`, {
    headers: { "Content-Type": "application/json", ...(options.headers || {}) },
    ...options
  });
  const data = await response.json().catch(() => null);
  if (!response.ok) throw new Error(data?.message || `Có lỗi xảy ra (${response.status}).`);
  return data;
}

async function loadOptions() {
  try {
    const [locationData, languageData] = await Promise.all([
      getJson("/api/locations"),
      getJson("/api/languages")
    ]);
    locations = locationData;
    locationSelect.innerHTML = '<option value="">-- Chọn địa điểm --</option>';
    locations.forEach(item => {
      const option = document.createElement("option");
      option.value = item.id;
      option.textContent = `${item.name}${item.city ? ` — ${item.city}` : ""}`;
      locationSelect.appendChild(option);
    });
    languageSelect.innerHTML = '<option value="">-- Chọn ngôn ngữ --</option>';
    languageData.forEach(item => {
      const option = document.createElement("option");
      option.value = item.code;
      option.textContent = item.name;
      languageSelect.appendChild(option);
    });
    if (!locations.length) locationSelect.innerHTML = '<option value="">Chưa có địa điểm</option>';
    if (!languageData.length) languageSelect.innerHTML = '<option value="">Chưa có ngôn ngữ</option>';
  } catch (error) {
    locationSelect.innerHTML = '<option value="">Không tải được địa điểm</option>';
    languageSelect.innerHTML = '<option value="">Không tải được ngôn ngữ</option>';
    errorMessage.textContent = `Không kết nối được API. Hãy chạy ứng dụng ASP.NET Core rồi tải lại trang. Chi tiết: ${error.message}`;
  }
}

locationSelect.addEventListener("change", () => {
  const selected = locations.find(item => String(item.id) === locationSelect.value);
  locationPreview.textContent = selected?.description || "Chọn một địa điểm để xem thông tin giới thiệu.";
});

form.addEventListener("submit", async event => {
  event.preventDefault();
  errorMessage.textContent = "";
  if (!locationSelect.value || !languageSelect.value) {
    errorMessage.textContent = "Vui lòng chọn địa điểm và ngôn ngữ.";
    return;
  }
  submitBtn.disabled = true;
  submitBtn.innerHTML = 'Đang tạo nội dung... <span>◌</span>';
  try {
    const result = await getJson("/api/narrations", {
      method: "POST",
      body: JSON.stringify({
        locationId: Number(locationSelect.value),
        languageCode: languageSelect.value
      })
    });
    const selected = locations.find(item => item.id === Number(locationSelect.value));
    document.getElementById("resultMeta").textContent =
      `${result.locationName || selected?.name || "ĐỊA ĐIỂM"} · ${result.languageName || languageSelect.options[languageSelect.selectedIndex].text}`.toUpperCase();
    document.getElementById("resultTitle").textContent = result.locationName || selected?.name || "Nội dung thuyết minh";
    document.getElementById("resultText").textContent = result.text || "Chưa có nội dung thuyết minh.";
    document.getElementById("resultStatus").textContent = (result.status || "THUYẾT MINH").replaceAll("-", " ").toUpperCase();
    const audio = document.getElementById("audioPlayer");
    if (result.audioUrl) {
      audio.src = result.audioUrl;
      audio.hidden = false;
    } else {
      audio.removeAttribute("src");
      audio.hidden = true;
    }
    resultSection.hidden = false;
    resultSection.scrollIntoView({ behavior: "smooth", block: "start" });
  } catch (error) {
    errorMessage.textContent = error.message || "Không thể tạo nội dung thuyết minh. Vui lòng thử lại.";
  } finally {
    submitBtn.disabled = false;
    submitBtn.innerHTML = 'Tạo nội dung thuyết minh <span>↗</span>';
  }
});

document.getElementById("copyBtn").addEventListener("click", async () => {
  const text = document.getElementById("resultText").textContent;
  const button = document.getElementById("copyBtn");
  try {
    await navigator.clipboard.writeText(text);
    button.textContent = "Đã sao chép ✓";
  } catch {
    button.textContent = "Không thể sao chép";
  }
});

document.querySelector(".menu-toggle").addEventListener("click", () => {
  document.querySelector(".topbar nav").classList.toggle("open");
});
document.querySelectorAll(".topbar nav a").forEach(link => link.addEventListener("click", () => {
  document.querySelector(".topbar nav").classList.remove("open");
}));

loadOptions();