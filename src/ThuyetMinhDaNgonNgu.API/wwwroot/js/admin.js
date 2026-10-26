"use strict";

const $ = (id) => document.getElementById(id);
const TOKEN_KEY = "vietguide_admin_token";
const EXPIRY_KEY = "vietguide_admin_token_expiry";
let locations = [];
let languages = [];
let savedNarrations = [];

function token() {
    const expiresAt = Number(sessionStorage.getItem(EXPIRY_KEY) || 0);
    if (!sessionStorage.getItem(TOKEN_KEY) || expiresAt <= Date.now()) {
        clearToken();
        return "";
    }

    return sessionStorage.getItem(TOKEN_KEY);
}

function clearToken() {
    sessionStorage.removeItem(TOKEN_KEY);
    sessionStorage.removeItem(EXPIRY_KEY);
}

function showLogin(message = "") {
    $("loginPanel").hidden = false;
    $("adminContent").hidden = true;
    $("logoutBtn").hidden = true;
    if (message) {
        $("loginMessage").textContent = message;
        $("loginMessage").style.color = "#b43e32";
    }
}

function showAdmin() {
    $("loginPanel").hidden = true;
    $("adminContent").hidden = false;
    $("logoutBtn").hidden = false;
}

async function api(url, options = {}) {
    const headers = new Headers(options.headers || {});
    if (options.body && !headers.has("Content-Type")) {
        headers.set("Content-Type", "application/json");
    }

    if (url.startsWith("/api/admin/") || url === "/api/languages") {
        const accessToken = token();
        if (accessToken) {
            headers.set("Authorization", `Bearer ${accessToken}`);
        }
    }

    const response = await fetch(url, { ...options, headers });
    const contentType = response.headers.get("content-type") || "";
    const data = contentType.includes("application/json")
        ? await response.json()
        : await response.text();

    if (response.status === 401 || response.status === 403) {
        clearToken();
        showLogin(response.status === 401
            ? "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại."
            : "Tài khoản không có quyền thực hiện thao tác này.");
    }

    if (!response.ok) {
        const message = typeof data === "string"
            ? data
            : data?.message || data?.detail || data?.title ||
                `Yêu cầu thất bại (${response.status}).`;
        throw new Error(message);
    }

    return data;
}

function msg(id, text, error = false) {
    $(id).textContent = text;
    $(id).style.color = error ? "#b43e32" : "#42735c";
}

function escapeHtml(value) {
    return String(value ?? "").replace(/[&<>"']/g, (character) => ({
        "&": "&amp;",
        "<": "&lt;",
        ">": "&gt;",
        '"': "&quot;",
        "'": "&#39;"
    })[character]);
}

function refreshLanguageOptions() {
    const select = $("language");
    select.replaceChildren(new Option("-- Chọn ngôn ngữ --", ""));
    languages.forEach((item) => {
        select.add(new Option(`${item.name} (${item.code})`, item.code));
    });
}

function renderLanguages() {
    $("languageList").innerHTML = languages.length
        ? languages.map((item) =>
            `<div class="language-item"><span>${escapeHtml(item.name)}</span>` +
            `<code>${escapeHtml(item.code)}</code></div>`).join("")
        : "Chưa có ngôn ngữ.";
}

async function loadOptions() {
    [locations, languages] = await Promise.all([
        api("/api/locations"),
        api("/api/languages")
    ]);

    const locationSelect = $("location");
    locationSelect.replaceChildren(new Option("-- Chọn địa điểm --", ""));
    locations.forEach((item) => {
        locationSelect.add(new Option(
            `${item.name}${item.city ? ` — ${item.city}` : ""}`,
            item.id
        ));
    });
    refreshLanguageOptions();
    renderLanguages();
}

function renderSaved() {
    const container = $("savedList");
    container.replaceChildren();

    if (!savedNarrations.length) {
        container.textContent = "Chưa có bản hiện hành.";
        return;
    }

    const heading = document.createElement("div");
    heading.className = "subhead";
    heading.textContent = "Các bản thuyết minh hiện hành trên máy chủ";
    container.appendChild(heading);

    savedNarrations.forEach((item) => {
        const row = document.createElement("div");
        row.className = "saved-item";
        const label = document.createElement("span");
        label.textContent = `${item.locationName} · ${item.languageName}`;
        const date = document.createElement("small");
        date.textContent = item.updatedAtUtc
            ? new Date(item.updatedAtUtc).toLocaleString("vi-VN")
            : "";
        label.append(document.createElement("br"), date);

        const button = document.createElement("button");
        button.type = "button";
        button.textContent = "Mở bản";
        button.addEventListener("click", () => {
            $("location").value = item.locationId;
            $("language").value = item.languageCode;
            $("narrationText").value = item.text;
            msg("narrationMessage", "Đã tải bản hiện hành.");
        });
        row.append(label, button);
        container.appendChild(row);
    });
}

async function refreshSaved() {
    savedNarrations = await api("/api/admin/narrations");
    renderSaved();
}

async function loadAdminData() {
    showAdmin();
    try {
        await Promise.all([loadOptions(), refreshSaved()]);
    } catch (error) {
        msg("narrationMessage", `Không tải được dữ liệu quản trị: ${error.message}`, true);
    }
}

$("loginForm").addEventListener("submit", async (event) => {
    event.preventDefault();
    msg("loginMessage", "Đang xác thực...");
    try {
        const result = await api("/api/admin/auth/login", {
            method: "POST",
            body: JSON.stringify({
                username: $("username").value,
                password: $("password").value
            })
        });
        sessionStorage.setItem(TOKEN_KEY, result.accessToken);
        sessionStorage.setItem(EXPIRY_KEY, String(new Date(result.expiresAtUtc).getTime()));
        $("password").value = "";
        await loadAdminData();
    } catch (error) {
        msg("loginMessage", error.message, true);
    }
});

$("logoutBtn").addEventListener("click", () => {
    clearToken();
    showLogin("Bạn đã đăng xuất.");
});

$("languageForm").addEventListener("submit", async (event) => {
    event.preventDefault();
    try {
        const item = await api("/api/languages", {
            method: "POST",
            body: JSON.stringify({
                code: $("code").value.trim(),
                name: $("name").value.trim()
            })
        });
        languages.push(item);
        renderLanguages();
        refreshLanguageOptions();
        $("languageForm").reset();
        msg("languageMessage", "Đã thêm ngôn ngữ thành công.");
    } catch (error) {
        msg("languageMessage", error.message, true);
    }
});

$("narrationForm").addEventListener("submit", async (event) => {
    event.preventDefault();
    if (!$("location").value || !$("language").value) {
        msg("narrationMessage", "Hãy chọn địa điểm và ngôn ngữ.", true);
        return;
    }

    msg("narrationMessage", "Đang tạo bản nháp bằng AI...");
    try {
        const result = await api("/api/admin/narrations/draft", {
            method: "POST",
            body: JSON.stringify({
                locationId: Number($("location").value),
                languageCode: $("language").value
            })
        });
        $("narrationText").value = result.text;
        msg("narrationMessage", "Đã tạo bản nháp. Chỉnh sửa nội dung rồi công bố.");
    } catch (error) {
        msg("narrationMessage", error.message, true);
    }
});

$("saveBtn").addEventListener("click", async () => {
    const locationId = $("location").value;
    const languageCode = $("language").value;
    if (!locationId || !languageCode) {
        msg("narrationMessage", "Hãy chọn địa điểm và ngôn ngữ trước khi công bố.", true);
        return;
    }

    msg("narrationMessage", "Đang tạo audio cho nội dung đã chỉnh sửa và công bố...");
    try {
        await api(`/api/admin/narrations/${locationId}/${encodeURIComponent(languageCode)}`, {
            method: "PUT",
            body: JSON.stringify({ text: $("narrationText").value })
        });
        await refreshSaved();
        msg("narrationMessage", "Đã tạo audio và công bố bản hiện hành.");
    } catch (error) {
        msg("narrationMessage", error.message, true);
    }
});

$("loadBtn").addEventListener("click", async () => {
    const locationId = $("location").value;
    const languageCode = $("language").value;
    if (!locationId || !languageCode) {
        msg("narrationMessage", "Hãy chọn địa điểm và ngôn ngữ trước khi tải.", true);
        return;
    }

    try {
        const result = await api(
            `/api/admin/narrations/${locationId}/${encodeURIComponent(languageCode)}`
        );
        $("narrationText").value = result.text;
        msg("narrationMessage", "Đã tải bản hiện hành.");
    } catch (error) {
        msg("narrationMessage", error.message, true);
    }
});

if (token()) {
    loadAdminData();
} else {
    showLogin();
}
