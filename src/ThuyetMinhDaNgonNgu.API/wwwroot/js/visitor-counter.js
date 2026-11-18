//Tạo mã lượt truy cập và gửi đến API
(async function () {
  //Để tạo một hàm và thực thi ngay khi file JavaScript được tải sử dụng async để có thể dùng await khi gửi request đến API.
  const key = "vietguide_last_visit_time";
  const interval = 10 /*(số phút)*/ * 60 * 1000; // 10 phút
  const visitorKey = "vietguide_visitor_id";
  const initialVisitKey = "vietguide_initial_visit_recorded_for";
  let visitorId = localStorage.getItem(visitorKey); //GHI NHẬN LƯỢT TRUY CẬP

  if (!visitorId) {
    visitorId = crypto.randomUUID(); //tạo UUID ngẫu nhiên
    localStorage.setItem(visitorKey, visitorId); //lưu vào localStorage
  } //Kiểm tra và tạo visitorId

  const now = Date.now();
  const lastVisit = Number(localStorage.getItem(key) || 0);
  const needsInitialVisit = localStorage.getItem(initialVisitKey) !== visitorId;
  if (!needsInitialVisit && now - lastVisit < interval) {
    //Đã ghi nhận lần đầu và Chưa đủ 10 phút
    return;
  }

  localStorage.setItem(key, now.toString()); //Lưu thời điểm truy cập hiện tại vào localStorage

  try {
    const response = await fetch("/api/visitor", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({ visitorId }),
    });

    if (!response.ok) {
      localStorage.setItem(key, lastVisit.toString());
      console.error("Không thể ghi nhận lượt truy cập:", response.status);
      return;
    }

    localStorage.setItem(initialVisitKey, visitorId);
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

  let leaving = false; //Dùng để xác định người dùng đã bắt đầu rời trang hay chưa , false là chưa

  async function sendHeartbeat() {
    if (leaving || document.visibilityState !== "visible") {
      // ko gửi heartbeat khi người dùng đang rời trang hoặc tab hiện tại không còn hiển thị
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
    //gửi tín hiệu rời đi
    if (leaving) {
      return; // nếu gửi rồi thì không gửi tiếp nữa
    }

    leaving = true; // đã rời trang

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
