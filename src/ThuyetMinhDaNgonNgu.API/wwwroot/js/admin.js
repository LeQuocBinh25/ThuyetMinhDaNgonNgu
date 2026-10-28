"use strict";

const $ = (id) => document.getElementById(id);

let locations = [];
let languages = [];
let savedNarrations = [];

function showAdmin() {
    $("adminContent").hidden = false;
}

async function api(url, options = {}) {
    const headers = new Headers(options.headers || {});

    if (options.body && !headers.has("Content-Type")) {
        headers.set("Content-Type", "application/json");
    }

    // Không cần gửi JWT vì hệ thống không yêu cầu đăng nhập.
    const response = await fetch(url, {
        ...options,
        headers
    });

    const contentType = response.headers.get("content-type") || "";
    const data = contentType.includes("json")
        ? await response.json()
        : await response.text();

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

function refreshLanguageOptions() {
    const select = $("language");
    select.replaceChildren(new Option("-- Chọn ngôn ngữ --", ""));
    languages.forEach((item) => {
        select.add(new Option(`${item.name} (${item.code})`, item.code));
    });
}

function renderLanguages() {
    const container = $("languageList");
    container.replaceChildren();

    if (!languages.length) {
        container.textContent = "Chưa có ngôn ngữ.";
        return;
    }

    languages.forEach((item) => {
        const row = document.createElement("div");
        row.className = "management-item";

        const details = document.createElement("div");
        const name = document.createElement("strong");
        name.textContent = item.name;
        const code = document.createElement("small");
        code.textContent = item.code;
        details.append(name, code);

        const actions = document.createElement("div");
        actions.className = "management-actions";
        const editButton = document.createElement("button");
        editButton.type = "button";
        editButton.className = "outline";
        editButton.dataset.action = "edit-language";
        editButton.dataset.code = item.code;
        editButton.textContent = "Sửa";
        const deleteButton = document.createElement("button");
        deleteButton.type = "button";
        deleteButton.dataset.action = "delete-language";
        deleteButton.dataset.code = item.code;
        deleteButton.textContent = "Xóa";
        actions.append(editButton, deleteButton);
        row.append(details, actions);
        container.appendChild(row);
    });
}

function renderLocations() {
    const container = $("locationList");
    container.replaceChildren();

    if (!locations.length) {
        container.textContent = "Chưa có địa điểm.";
        return;
    }

    locations.forEach((item) => {
        const row = document.createElement("div");
        row.className = "management-item";

        const details = document.createElement("div");
        const name = document.createElement("strong");
        name.textContent = item.name;
        const city = document.createElement("small");
        city.textContent = item.city || "";
        details.append(name, city);

        const actions = document.createElement("div");
        actions.className = "management-actions";
        const editButton = document.createElement("button");
        editButton.type = "button";
        editButton.className = "outline";
        editButton.dataset.action = "edit-location";
        editButton.dataset.id = item.id;
        editButton.textContent = "Sửa";
        const deleteButton = document.createElement("button");
        deleteButton.type = "button";
        deleteButton.dataset.action = "delete-location";
        deleteButton.dataset.id = item.id;
        deleteButton.textContent = "Xóa";
        actions.append(editButton, deleteButton);
        row.append(details, actions);
        container.appendChild(row);
    });
}

function confirmAction(message, title = "Xác nhận thao tác") {
    return new Promise((resolve) => {
        const dialog = $("confirmDialog");
        $("confirmTitle").textContent = title;
        $("confirmMessage").textContent = message;
        dialog.addEventListener("close", () => {
            resolve(dialog.returnValue === "confirm");
        }, { once: true });
        dialog.showModal();
    });
}

function resetLocationForm() {
    $("locationForm").reset();
    $("locationId").value = "";
    $("saveLocationBtn").textContent = "Thêm địa điểm";
    $("cancelLocationEditBtn").hidden = true;
}

async function loadOptions() {
    const [locationResult, languageResult] = await Promise.allSettled([
        api("/api/locations"),
        api("/api/languages")
    ]);

    if (locationResult.status === "fulfilled") {
        locations = locationResult.value;
    } else {
        locations = [];
    }

    if (languageResult.status === "fulfilled") {
        languages = languageResult.value;
    } else {
        languages = [];
    }

    const locationSelect = $("location");
    locationSelect.replaceChildren(new Option("-- Chọn địa điểm --", ""));
    locations.forEach((item) => {
        locationSelect.add(new Option(
            `${item.name}${item.city ? ` — ${item.city}` : ""}`,
            item.id
        ));
    });
    renderLocations();
    refreshLanguageOptions();
    renderLanguages();

    if (locationResult.status === "rejected") {
        const errorMessage = locationResult.reason.message || "Lỗi không xác định.";
        $("locationList").textContent = `Không tải được địa điểm: ${errorMessage}`;
        locationSelect.replaceChildren(new Option("Không tải được địa điểm", ""));
        msg("locationMessage", `Không tải được địa điểm: ${errorMessage}`, true);
    } else {
        msg("locationMessage", "");
    }
    if (languageResult.status === "rejected") {
        const errorMessage = languageResult.reason.message || "Lỗi không xác định.";
        $("languageList").textContent = `Không tải được ngôn ngữ: ${errorMessage}`;
        $("language").replaceChildren(new Option("Không tải được ngôn ngữ", ""));
        msg("languageMessage", `Không tải được danh sách ngôn ngữ: ${errorMessage}`, true);
    } else {
        msg("languageMessage", "");
    }
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
        const deleteButton = document.createElement("button");
        deleteButton.type = "button";
        deleteButton.className = "outline";
        deleteButton.dataset.action = "delete-narration";
        deleteButton.dataset.locationId = item.locationId;
        deleteButton.dataset.languageCode = item.languageCode;
        deleteButton.textContent = "Xóa";
        row.append(label, button, deleteButton);
        container.appendChild(row);
    });
}

async function refreshSaved() {
    savedNarrations = await api("/api/admin/narrations");
    renderSaved();
}

async function loadAdminData() {
    showAdmin();
    const results = await Promise.allSettled([loadOptions(), refreshSaved()]);
    const savedResult = results[1];
    if (savedResult.status === "rejected") {
        msg("narrationMessage", `Không tải được danh sách bài thuyết minh: ${savedResult.reason.message}`, true);
    }
}

$("locationForm").addEventListener("submit", async (event) => {
    event.preventDefault();
    const id = $("locationId").value;
    const latitudeValue = $("locationLatitude").value.trim();
    const longitudeValue = $("locationLongitude").value.trim();
    const latitude = latitudeValue === "" ? null : Number(latitudeValue);
    const longitude = longitudeValue === "" ? null : Number(longitudeValue);

    if ((latitude === null) !== (longitude === null)) {
        msg("locationMessage", "Hãy nhập cả vĩ độ và kinh độ, hoặc để trống cả hai.", true);
        return;
    }

    const body = {
        name: $("locationName").value.trim(),
        city: $("locationCity").value.trim(),
        description: $("locationDescription").value.trim(),
        latitude,
        longitude
    };

    try {
        await api(id ? `/api/locations/${encodeURIComponent(id)}` : "/api/locations", {
            method: id ? "PUT" : "POST",
            body: JSON.stringify(body)
        });
        resetLocationForm();
        await loadOptions();
        msg("locationMessage", id ? "Đã cập nhật địa điểm." : "Đã thêm địa điểm.");
    } catch (error) {
        msg("locationMessage", error.message, true);
    }
});

$("cancelLocationEditBtn").addEventListener("click", resetLocationForm);

$("locationList").addEventListener("click", async (event) => {
    const button = event.target.closest("button[data-action]");
    if (!button) {
        return;
    }

    const location = locations.find((item) => String(item.id) === button.dataset.id);
    if (!location) {
        return;
    }

    if (button.dataset.action === "edit-location") {
        $("locationId").value = location.id;
        $("locationName").value = location.name;
        $("locationCity").value = location.city;
        $("locationDescription").value = location.description || "";
        $("locationLatitude").value = location.latitude ?? "";
        $("locationLongitude").value = location.longitude ?? "";
        $("saveLocationBtn").textContent = "Lưu thay đổi";
        $("cancelLocationEditBtn").hidden = false;
        $("locationForm").scrollIntoView({ behavior: "smooth", block: "center" });
        return;
    }

    if (button.dataset.action === "delete-location" &&
        await confirmAction(
            `Xóa địa điểm "${location.name}" và tất cả bản thuyết minh liên quan? Audio không còn được bản ghi nào dùng sẽ bị dọn. Thao tác này không thể hoàn tác.`,
            "Xóa địa điểm và nội dung liên quan")) {
        try {
            await api(`/api/locations/${encodeURIComponent(location.id)}`, {
                method: "DELETE"
            });
        } catch (error) {
            msg("locationMessage", error.message, true);
            return;
        }

        if ($("locationId").value === String(location.id)) {
            resetLocationForm();
        }
        if (String($("location").value) === String(location.id)) {
            $("location").value = "";
            $("narrationText").value = "";
            msg("narrationMessage", "Địa điểm và các bản thuyết minh liên quan đã bị xóa.");
        }

        try {
            await Promise.all([loadOptions(), refreshSaved()]);
            msg("locationMessage", `Đã xóa địa điểm "${location.name}" và các bản thuyết minh liên quan.`);
        } catch (error) {
            msg("locationMessage", `Đã xóa địa điểm nhưng không tải lại được danh sách: ${error.message}`, true);
        }
    }
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
        await loadOptions();
        $("languageForm").reset();
        msg("languageMessage", "Đã thêm ngôn ngữ thành công.");
    } catch (error) {
        msg("languageMessage", error.message, true);
    }
});

$("languageList").addEventListener("click", async (event) => {
    const button = event.target.closest("button[data-action]");
    if (!button) {
        return;
    }

    const language = languages.find((item) => item.code === button.dataset.code);
    if (!language) {
        return;
    }

    if (button.dataset.action === "edit-language") {
        $("languageEditCode").value = language.code;
        $("languageEditName").value = language.name;
        $("languageEditForm").hidden = false;
        $("languageEditForm").scrollIntoView({ behavior: "smooth", block: "center" });
        return;
    }

    if (button.dataset.action === "delete-language" &&
        await confirmAction(`Xóa ngôn ngữ "${language.name}" (${language.code})? Không thể xóa nếu bài thuyết minh đang tham chiếu ngôn ngữ này.`)) {
        try {
            await api(`/api/languages/${encodeURIComponent(language.code)}`, {
                method: "DELETE"
            });
            await loadOptions();
            msg("languageMessage", `Đã xóa ngôn ngữ "${language.name}".`);
        } catch (error) {
            msg("languageMessage", error.message, true);
        }
    }
});

$("languageEditForm").addEventListener("submit", async (event) => {
    event.preventDefault();
    const code = $("languageEditCode").value;
    try {
        await api(`/api/languages/${encodeURIComponent(code)}`, {
            method: "PUT",
            body: JSON.stringify({ name: $("languageEditName").value.trim() })
        });
        $("languageEditForm").hidden = true;
        await loadOptions();
        msg("languageMessage", "Đã cập nhật ngôn ngữ.");
    } catch (error) {
        msg("languageMessage", error.message, true);
    }
});

$("cancelLanguageEditBtn").addEventListener("click", () => {
    $("languageEditForm").hidden = true;
    $("languageEditForm").reset();
});

$("savedList").addEventListener("click", async (event) => {
    const button = event.target.closest('button[data-action="delete-narration"]');
    if (!button) {
        return;
    }

    const { locationId, languageCode } = button.dataset;
    const narration = savedNarrations.find((item) =>
        String(item.locationId) === locationId && item.languageCode === languageCode);
    if (!narration || !await confirmAction(
        `Xóa bài thuyết minh "${narration.locationName} · ${narration.languageName}"? Audio chỉ được dọn nếu không còn bản ghi nào sử dụng.`,
        "Xóa bài thuyết minh")) {
        return;
    }

    try {
        await api(
            `/api/admin/narrations/${encodeURIComponent(locationId)}/${encodeURIComponent(languageCode)}`,
            { method: "DELETE" }
        );
        msg("narrationMessage", "Đã xóa bài thuyết minh và dọn audio không còn được sử dụng.");
    } catch (error) {
        msg("narrationMessage", error.message, true);
    } finally {
        try {
            await refreshSaved();
        } catch (error) {
            msg("narrationMessage", `Không thể tải lại danh sách bài thuyết minh: ${error.message}`, true);
        }
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

document.addEventListener("DOMContentLoaded", async () => {
    showAdmin();

    try {
        await loadAdminData();
    } catch (error) {
        console.error("Không thể tải dữ liệu quản trị:", error);
    }
});