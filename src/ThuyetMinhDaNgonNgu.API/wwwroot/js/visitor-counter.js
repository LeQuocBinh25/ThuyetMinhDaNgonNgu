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
