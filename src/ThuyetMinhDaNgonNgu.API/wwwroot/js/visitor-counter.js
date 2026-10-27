(async function () {
  const key = "vietguide_last_visit_time";
  const interval = 10 * 60 * 1000; // 10 phút

  const now = Date.now();
  const lastVisit = Number(localStorage.getItem(key) || 0);

  // Chưa đủ 10 phút thì không tính lượt mới
  if (now - lastVisit < interval) {
    return;
  }

  // Ghi thời điểm trước khi gửi yêu cầu để tránh gửi trùng
  localStorage.setItem(key, now.toString());

  const visitId = crypto.randomUUID();

  try {
    const response = await fetch("/api/visitor", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({
        visitId: visitId,
      }),
    });

    if (!response.ok) {
      // Cho phép thử lại nếu API báo lỗi
      localStorage.setItem(key, lastVisit.toString());
      console.error("Không thể ghi nhận lượt truy cập:", response.status);
    }
  } catch (error) {
    localStorage.setItem(key, lastVisit.toString());
    console.error("Lỗi kết nối API:", error);
  }
})();
// Theo dõi trạng thái người truy cập
const visitorKey = "vietguide_visitor_id";
let visitorId = localStorage.getItem(visitorKey);

if (!visitorId) {
  visitorId = crypto.randomUUID();
  localStorage.setItem(visitorKey, visitorId);
}

async function sendHeartbeat() {
  try {
    await fetch("/api/visitor-activity/heartbeat", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({
        visitorId: visitorId,
      }),
    });
  } catch (error) {
    console.error("Không gửi được heartbeat:", error);
  }
}

sendHeartbeat();
setInterval(sendHeartbeat, 20000);

document.addEventListener("visibilitychange", () => {
  if (document.visibilityState === "visible") {
    sendHeartbeat();
  }
});

function sendLeaveSignal() {
  const data = JSON.stringify({
    visitorId: visitorId,
  });

  const blob = new Blob([data], {
    type: "application/json",
  });

  navigator.sendBeacon("/api/visitor-activity/leave", blob);
}

window.addEventListener("pagehide", sendLeaveSignal);
