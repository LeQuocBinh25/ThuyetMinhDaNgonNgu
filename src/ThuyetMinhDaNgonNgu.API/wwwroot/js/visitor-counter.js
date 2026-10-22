(async function () {
  const key = "vietguide_visit_id";

  if (sessionStorage.getItem(key)) {
    return;
  }

  const visitId = crypto.randomUUID();
  sessionStorage.setItem(key, visitId);

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
      sessionStorage.removeItem(key);
      console.error("Không thể ghi nhận lượt truy cập.");
    }
  } catch (error) {
    sessionStorage.removeItem(key);
    console.error("Lỗi kết nối API:", error);
  }
})();
