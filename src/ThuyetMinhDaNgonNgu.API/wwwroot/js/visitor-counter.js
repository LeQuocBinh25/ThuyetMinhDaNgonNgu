// Ghi nhận lượt truy cập website.
(async function () {
  const key = "vietguide_last_visit_time";
  const interval = 10 * 60 * 1000; // 10 phút

  const now = Date.now();
  const lastVisit = Number(localStorage.getItem(key) || 0);

  if (now - lastVisit < interval) {
    return;
  }

  localStorage.setItem(key, now.toString());

  try {
    const response = await fetch("/api/visitor", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({
        visitId: crypto.randomUUID(),
      }),
    });

    if (!response.ok) {
      localStorage.setItem(key, lastVisit.toString());
      console.error("Không thể ghi nhận lượt truy cập:", response.status);
    }
  } catch (error) {
    localStorage.setItem(key, lastVisit.toString());
    console.error("Lỗi kết nối API:", error);
  }
})();

// Theo dõi trạng thái người truy cập.
(function () {
  const visitorKey = "vietguide_visitor_id";
  let visitorId = localStorage.getItem(visitorKey);

  if (!visitorId) {
    visitorId = crypto.randomUUID();
    localStorage.setItem(visitorKey, visitorId);
  }

  let leaving = false;

  async function sendHeartbeat() {
    if (leaving || document.visibilityState !== "visible") {
      return;
    }

    try {
      const response = await fetch("/api/visitor-activity/heartbeat", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({ visitorId }),
        keepalive: true,
      });

      if (!response.ok) {
        console.error("Heartbeat thất bại:", response.status);
      }
    } catch (error) {
      console.error("Không gửi được heartbeat:", error);
    }
  }

  function sendLeaveSignal() {
    if (leaving) {
      return;
    }

    leaving = true;

    fetch("/api/visitor-activity/leave", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({ visitorId }),
      keepalive: true,
    }).catch((error) => {
      console.error("Không gửi được tín hiệu rời trang:", error);
    });
  }

  // Gửi ngay khi mở trang, sau đó gửi mỗi 20 giây.
  sendHeartbeat();
  const heartbeatTimer = setInterval(sendHeartbeat, 20000);

  // Nếu người dùng quay lại tab, gửi heartbeat ngay.
  document.addEventListener("visibilitychange", () => {
    if (document.visibilityState === "visible") {
      leaving = false;
      sendHeartbeat();
    }
  });

  // Gửi tín hiệu khi đóng trang hoặc chuyển sang trang khác.
  window.addEventListener("pagehide", () => {
    clearInterval(heartbeatTimer);
    sendLeaveSignal();
  });
})();
