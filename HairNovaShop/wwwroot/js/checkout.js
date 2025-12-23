console.log("JS đã load thành công!");
// 1. ẨN / HIỆN KHỐI "GIAO HÀNG ĐẾN ĐỊA CHỈ KHÁC?"
const shipOtherCheckbox = document.getElementById("shipOther");
const shipOtherBlock = document.getElementById("shipping-other-block");

if (shipOtherCheckbox && shipOtherBlock) {
  shipOtherCheckbox.addEventListener("change", () => {
    // nếu được tick thì bỏ class hidden, không tick thì thêm lại
    shipOtherBlock.classList.toggle("hidden", !shipOtherCheckbox.checked);
  });
}

// 2. VOUCHER
const voucherInput = document.getElementById("voucher");
const voucherMsg = document.getElementById("voucherMsg");
const totalSpan = document.querySelector(".order-total");
const applyBtn = document.querySelector(".btn-apply");
let discountAmount = 0;
let originalTotal = 0;

if (voucherInput && voucherMsg && totalSpan && applyBtn) {
  // Lưu tổng tiền ban đầu
  originalTotal = parseFloat(totalSpan.textContent.replace(/[^\d]/g, '')) || 0;
  
  applyBtn.addEventListener("click", () => {
    const code = voucherInput.value.trim().toUpperCase();

    if (!code) {
      voucherMsg.textContent = "Vui lòng nhập mã voucher.";
      voucherMsg.style.color = "#c53030";
      return;
    }

    if (code === "HAIR10") {
      discountAmount = originalTotal * 0.1; // Giảm 10%
      const newTotal = originalTotal - discountAmount;
      voucherMsg.textContent = "Áp dụng mã HAIR10 thành công: giảm 10%.";
      voucherMsg.style.color = "green";
      totalSpan.textContent = newTotal.toLocaleString('vi-VN') + " đ";
      
      // Cập nhật hidden input để gửi lên server
      const discountInput = document.getElementById("discountAmount");
      if (discountInput) {
        discountInput.value = discountAmount;
      }
    } else {
      discountAmount = 0;
      voucherMsg.textContent = "Mã không hợp lệ hoặc đã hết hạn.";
      voucherMsg.style.color = "#c53030";
      totalSpan.textContent = originalTotal.toLocaleString('vi-VN') + " đ";
      
      const discountInput = document.getElementById("discountAmount");
      if (discountInput) {
        discountInput.value = "0";
      }
    }
  });
}

// 3. SUBMIT FORM
const orderForm = document.getElementById("checkoutForm");
if (orderForm) {
  orderForm.addEventListener("submit", (e) => {
    e.preventDefault();
    
    // Validation
    const fullname = document.getElementById("fullname")?.value.trim();
    const phone = document.getElementById("phone")?.value.trim();
    const email = document.getElementById("email")?.value.trim();
    const province = document.getElementById("province")?.value;
    const ward = document.getElementById("ward")?.value;
    const address = document.getElementById("address")?.value.trim();
    
    if (!fullname || !phone || !email || !province || !ward || !address) {
      alert("Vui lòng điền đầy đủ thông tin bắt buộc (có dấu *).");
      return;
    }
    
    // Validate email format
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailRegex.test(email)) {
      alert("Email không hợp lệ.");
      return;
    }
    
    // Validate phone
    const phoneRegex = /^[0-9]{10,11}$/;
    if (!phoneRegex.test(phone)) {
      alert("Số điện thoại không hợp lệ.");
      return;
    }
    
    // Nếu có giao hàng đến địa chỉ khác, kiểm tra thông tin
    if (shipOtherCheckbox && shipOtherCheckbox.checked) {
      const shipName = document.getElementById("shipName")?.value.trim();
      const shipProvince = document.getElementById("shipProvince")?.value;
      const shipWard = document.getElementById("shipWard")?.value;
      const shipAddress = document.getElementById("shipAddress")?.value.trim();
      
      if (!shipName || !shipProvince || !shipWard || !shipAddress) {
        alert("Vui lòng điền đầy đủ thông tin địa chỉ giao hàng khác.");
        return;
      }
    }
    
    // Submit form
    orderForm.submit();
  });
}

/* =========================================
      SCROLL RESTORATION
      ========================================= */
if (history.scrollRestoration) {
    history.scrollRestoration = 'manual';
} else {
    window.onbeforeunload = function () {
        window.scrollTo(0, 0);
    };
}
