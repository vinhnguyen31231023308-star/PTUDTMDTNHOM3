// Tìm kiếm bài viết theo tiêu đề (dùng data-title)
document.addEventListener("DOMContentLoaded", function () {
  const input = document.getElementById("news-search-input");
  const cards = document.querySelectorAll(".news-card");

  if (!input) return;

  input.addEventListener("keyup", function () {
    const keyword = input.value.toLowerCase().trim();

    cards.forEach((card) => {
      const titleKey = card.dataset.title || "";
      if (titleKey.includes(keyword)) {
        card.style.display = "";
      } else {
        card.style.display = keyword === "" ? "" : "none";
      }
    });
  });
});
 /* =========================================
       SCROLL RESTORATION
       ========================================= */
    if (history.scrollRestoration) {
        history.scrollRestoration = 'manual';
    } else {
        window.onbeforeunload = function () {
            window.scrollTo(0, 0);
        }
    }
