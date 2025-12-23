// ---------------------- HÀM FORMAT TIỀN ----------------------
function formatVND(value) {
  return value.toLocaleString("vi-VN") + " đ";
}

// ---------------------- TÍNH TIỀN GIỎ HÀNG -------------------
function updateCartTotals() {
  const items = document.querySelectorAll(".cart-item");
  let subtotal = 0;

  items.forEach((item) => {
    const priceEl = item.querySelector(".price");
    const qtyInput = item.querySelector(".qty-input");
    const subtotalEl = item.querySelector(".subtotal");

    if (!priceEl || !qtyInput || !subtotalEl) return;

    const price = Number(priceEl.dataset.price || 0);
    const qty = Math.max(1, Number(qtyInput.value) || 1);

    const itemSubtotal = price * qty;
    subtotal += itemSubtotal;
    subtotalEl.textContent = formatVND(itemSubtotal);
  });

  // Lưu subtotal vào dataset để dùng cho voucher
  const billSubtotalEl = document.getElementById("billSubtotal");
  if (billSubtotalEl) {
    billSubtotalEl.dataset.raw = subtotal;
    billSubtotalEl.textContent = formatVND(subtotal);
  }

  applyCurrentVoucher();
}

// ---------------------- VOUCHER ------------------------------
const vouchers = {
  HAIR10: { type: "percent", value: 10 },
  HAIR20: { type: "percent", value: 20 },
  HAIR15K: { type: "amount", value: 15000 },
  HAIR50K: { type: "amount", value: 50000 },
};

let currentVoucherCode = null;

function applyCurrentVoucher() {
  const subtotalEl = document.getElementById("billSubtotal");
  const discountEl = document.getElementById("billDiscount");
  const totalEl = document.getElementById("billTotal");

  if (!subtotalEl || !discountEl || !totalEl) return;

  const rawSubtotal = Number(subtotalEl.dataset.raw || 0);
  let discount = 0;

  if (currentVoucherCode && vouchers[currentVoucherCode]) {
    const v = vouchers[currentVoucherCode];
    if (v.type === "percent") {
      discount = Math.round((rawSubtotal * v.value) / 100);
    } else if (v.type === "amount") {
      discount = v.value;
    }

    // không cho âm
    if (discount > rawSubtotal) discount = rawSubtotal;
  }

  const total = rawSubtotal - discount;

  discountEl.textContent = formatVND(discount);
  discountEl.dataset.raw = discount;
  totalEl.textContent = formatVND(total);
}

// ---------------------- SỰ KIỆN TRÊN DOM ---------------------
document.addEventListener("DOMContentLoaded", () => {
  // 1. Tăng/giảm số lượng
  document.querySelectorAll(".cart-item").forEach((item) => {
    const minusBtn = item.querySelector(".qty-btn.minus");
    const plusBtn = item.querySelector(".qty-btn.plus");
    const qtyInput = item.querySelector(".qty-input");
    const productId = item.dataset.id;
    const capacity = item.dataset.capacity || null;

    if (minusBtn && qtyInput) {
      minusBtn.addEventListener("click", () => {
        let val = Number(qtyInput.value) || 1;
        if (val > 1) {
          val--;
          qtyInput.value = val;
          updateQuantity(productId, val, capacity);
        }
      });
    }
    if (plusBtn && qtyInput) {
      plusBtn.addEventListener("click", () => {
        let val = Number(qtyInput.value) || 1;
        val++;
        qtyInput.value = val;
        updateQuantity(productId, val, capacity);
      });
    }
    if (qtyInput) {
      qtyInput.addEventListener("change", () => {
        let val = Number(qtyInput.value) || 1;
        if (val < 1) val = 1;
        qtyInput.value = val;
        updateQuantity(productId, val, capacity);
      });
    }

    // Xóa sản phẩm
    const removeLink = item.querySelector(".cart-remove");
    if (removeLink) {
      removeLink.addEventListener("click", (e) => {
        e.preventDefault();
        removeFromCart(productId, capacity);
      });
    }
  });

  // 2. Toggle danh sách voucher bên trái
  const toggleVoucherBtn = document.getElementById("toggleVoucherList");
  const voucherList = document.getElementById("voucherList");
  if (toggleVoucherBtn && voucherList) {
    toggleVoucherBtn.addEventListener("click", () => {
      voucherList.classList.toggle("hidden");
    });
  }

  // 3. Copy mã voucher khi bấm nút COPY
  document.querySelectorAll(".voucher-item .btn-copy").forEach((btn) => {
    btn.addEventListener("click", () => {
      const parent = btn.closest(".voucher-item");
      if (!parent) return;
      const code = parent.dataset.code;
      const input = document.getElementById("voucherInput");
      const msg = document.getElementById("voucherMessage");
      if (input) {
        input.value = code;
        input.focus();
        input.select();
        if (navigator.clipboard && navigator.clipboard.writeText) {
          navigator.clipboard.writeText(code).catch(() => {});
        }
      }
      if (msg) {
        msg.textContent = "Đã copy mã " + code + ". Vui lòng bấm Áp dụng để sử dụng.";
        msg.style.color = "#16a34a";
      }
    });
  });

  // 4. Nút "Xem các mã giảm giá hiện có" bên phải – chỉ mở list bên trái
  const showAllRightBtn = document.getElementById("showAllVoucherRight");
  if (showAllRightBtn && voucherList) {
    showAllRightBtn.addEventListener("click", () => {
      voucherList.classList.remove("hidden");
      window.scrollTo({ top: voucherList.offsetTop - 80, behavior: "smooth" });
    });
  }

  // 5. Áp dụng voucher
  const applyBtn = document.getElementById("applyVoucherBtn");
  const inputVoucher = document.getElementById("voucherInput");
  const msgEl = document.getElementById("voucherMessage");

  if (applyBtn && inputVoucher && msgEl) {
    applyBtn.addEventListener("click", () => {
      const code = inputVoucher.value.trim().toUpperCase();
      if (!code) {
        msgEl.textContent = "Vui lòng nhập mã voucher.";
        msgEl.style.color = "#dc2626";
        currentVoucherCode = null;
        applyCurrentVoucher();
        return;
      }

      if (!vouchers[code]) {
        msgEl.textContent = "Mã không hợp lệ hoặc đã hết hạn.";
        msgEl.style.color = "#dc2626";
        currentVoucherCode = null;
        applyCurrentVoucher();
        return;
      }

      currentVoucherCode = code;
      msgEl.textContent = "Áp dụng mã " + code + " thành công.";
      msgEl.style.color = "#16a34a";
      applyCurrentVoucher();
    });
  }

  // 6. Tính tiền lần đầu khi load trang
  updateCartTotals();
});

  // 7. TAB "BẠN CÓ CẦN THÊM / DEAL SỐC"
  const tabs = document.querySelectorAll(".recommend-tab");
  const panels = document.querySelectorAll(".recommend-panel");

  tabs.forEach((tab) => {
    tab.addEventListener("click", () => {
      const targetId = tab.dataset.target;

      tabs.forEach((t) => t.classList.remove("active"));
      panels.forEach((p) => p.classList.remove("active"));

      tab.classList.add("active");
      const panel = document.getElementById(targetId);
      if (panel) panel.classList.add("active");
    });
  });

  // 8. SLIDER CHO TAB "BẠN CÓ CẦN THÊM?"
  const track = document.getElementById("moreTrack");
  const prevBtn = document.querySelector(".product-slider .prev");
  const nextBtn = document.querySelector(".product-slider .next");

  if (track && prevBtn && nextBtn) {
    const scrollAmount = 200; // px mỗi lần bấm

    prevBtn.addEventListener("click", () => {
      track.scrollBy({ left: -scrollAmount, behavior: "smooth" });
    });
    nextBtn.addEventListener("click", () => {
      track.scrollBy({ left: scrollAmount, behavior: "smooth" });
    });
  };

// ---------------------- AJAX FUNCTIONS ----------------------
function updateQuantity(productId, quantity, capacity) {
  const formData = new FormData();
  formData.append('quantity', quantity);
  if (capacity && capacity !== '') {
    formData.append('capacity', capacity);
  }

  fetch(`/Cart/Update/${productId}`, {
    method: 'POST',
    body: formData
  })
  .then(response => response.json())
  .then(data => {
    if (data.success) {
      updateCartTotals();
      // Update cart badge if exists
      const cartBadge = document.querySelector('.cart-badge');
      if (cartBadge) {
        if (data.cartCount > 0) {
          cartBadge.textContent = data.cartCount;
        } else {
          cartBadge.remove();
        }
      }
    }
  })
  .catch(error => {
    console.error('Error updating quantity:', error);
    alert('Có lỗi xảy ra khi cập nhật số lượng!');
  });
}

function removeFromCart(productId, capacity) {
  if (!confirm('Bạn có chắc muốn xóa sản phẩm này khỏi giỏ hàng?')) {
    return;
  }

  const formData = new FormData();
  if (capacity && capacity !== '') {
    formData.append('capacity', capacity);
  }

  fetch(`/Cart/Remove/${productId}`, {
    method: 'POST',
    body: formData
  })
  .then(response => response.json())
  .then(data => {
    if (data.success) {
      // Remove item from DOM
      const capacityAttr = capacity || '';
      const item = document.querySelector(`.cart-item[data-id="${productId}"][data-capacity="${capacityAttr}"]`);
      if (item) {
        item.remove();
        updateCartTotals();
      }
      // Reload page if cart is empty
      if (data.cartCount === 0) {
        location.reload();
      }
      // Update cart badge
      const cartBadge = document.querySelector('.cart-badge');
      if (cartBadge) {
        if (data.cartCount > 0) {
          cartBadge.textContent = data.cartCount;
        } else {
          cartBadge.remove();
        }
      }
    }
  })
  .catch(error => {
    console.error('Error removing item:', error);
    alert('Có lỗi xảy ra khi xóa sản phẩm!');
  });
}
