document.addEventListener("DOMContentLoaded", function () {
    setTimeout(() => {
        document.querySelectorAll('.hidden-up, .hidden-left').forEach(el => el.classList.add('show'));
    }, 100);

    loadDashboardData();
    renderOrders('all');
    const allButton = document.querySelector('.btn-filter');
    if (allButton) allButton.classList.add('active');

    // Add enter key handler for tracking search
    const trackingInput = document.getElementById('tracking-order-code');
    if (trackingInput) {
        trackingInput.addEventListener('keypress', function(e) {
            if (e.key === 'Enter') {
                searchTracking();
            }
        });
    }
});

function openTab(tabName, element) {
    // 1. Ẩn tất cả nội dung tab
    var i, tabContent;
    tabContent = document.getElementsByClassName("tab-pane");
    for (i = 0; i < tabContent.length; i++) {
        tabContent[i].classList.remove("active");
    }

    // 2. Xóa trạng thái active của menu
    var tabLinks = document.getElementsByClassName("menu-item");
    for (i = 0; i < tabLinks.length; i++) {
        tabLinks[i].classList.remove("active");
    }

    // 3. Hiển thị tab được chọn
    document.getElementById(tabName).classList.add("active");

    // 4. Active menu tương ứng
    if (element) {
        element.classList.add("active");
    } else {
        if (tabName === 'tracking') tabLinks[1].classList.add("active");
        if (tabName === 'orders') tabLinks[2].classList.add("active");
        if (tabName === 'address') tabLinks[3].classList.add("active");
        if (tabName === 'profile') tabLinks[4].classList.add("active");
    }
}

function handleLogout() {
    if (confirm("Bạn có chắc chắn muốn đăng xuất không?")) {
        window.location.href = '/Account/Logout';
    }
}

function closeAddressModal() {
    const modal = document.getElementById('address-modal');
    if (modal) {
        modal.classList.remove('show');
        const form = document.getElementById('address-form');
        if (form) {
            form.reset();
            document.getElementById('set-default-address').checked = false;
        }
    }
}

function openEditAddressModal(addressId) {
    // Find address data from the clicked card
    const addressCard = event.target.closest('.address-card');
    if (!addressCard) {
        openAddAddressModal();
        return;
    }

    const addressData = addressCard.querySelector('.address-data');
    if (addressData) {
        document.getElementById('recipient-name').value = addressData.dataset.name || '';
        document.getElementById('recipient-phone').value = addressData.dataset.phone || '';
        document.getElementById('street-address').value = addressData.dataset.address || '';
        document.getElementById('province').value = addressData.dataset.province || '';
        document.getElementById('district').value = addressData.dataset.ward || '';
    }

    document.getElementById('modal-title').textContent = "Cập nhật địa chỉ";
    document.getElementById('address-modal').classList.add('show');
}

function openAddAddressModal() {
    document.getElementById('modal-title').textContent = "Thêm địa chỉ mới";
    const form = document.getElementById('address-form');
    if (form) {
        form.reset();
        document.getElementById('set-default-address').checked = false;
    }
    document.getElementById('address-modal').classList.add('show');
}

function saveAddress() {
    const recipientName = document.getElementById('recipient-name')?.value.trim();
    const recipientPhone = document.getElementById('recipient-phone')?.value.trim();
    const streetAddress = document.getElementById('street-address')?.value.trim();
    const province = document.getElementById('province')?.value;
    const district = document.getElementById('district')?.value.trim();

    if (!recipientName || !recipientPhone || !streetAddress || !province) {
        showPasswordToast('error', 'Lỗi!', 'Vui lòng điền đầy đủ thông tin bắt buộc');
        return;
    }

    // For now, just show message since address is saved when placing order
    showPasswordToast('info', 'Thông báo', 'Địa chỉ sẽ được lưu khi bạn đặt hàng. Vui lòng sử dụng địa chỉ này khi thanh toán.');
    closeAddressModal();
}

function handleDeleteAddress(addressId, button) {
    if (confirm('Bạn có chắc chắn muốn xóa địa chỉ này không?')) {
        // For now, just show message since addresses come from orders
        showPasswordToast('info', 'Thông báo', 'Địa chỉ này được lấy từ đơn hàng của bạn. Không thể xóa trực tiếp.');
    }
}

// ==========================================
// LOAD ORDERS FROM SERVER
// ==========================================
let ordersData = [];

async function loadOrdersFromServer() {
    try {
        const response = await fetch('/Account/GetOrders');
        const result = await response.json();
        if (result.success && result.orders) {
            ordersData = result.orders.map(order => ({
                id: order.orderCode,
                orderId: order.id,
                date: order.createdAt,
                status: mapStatusToFilter(order.status),
                statusText: order.statusName,
                total: formatPrice(order.total),
                productName: order.firstProductName || 'Đơn hàng',
                productDesc: order.itemsCount ? `${order.itemsCount} sản phẩm` : '',
                img: order.firstProductImage || '/images/placeholder.png',
                payment: order.paymentMethod,
                logs: getStatusLog(order.status, order.createdAt)
            }));
            return true;
        }
    } catch (error) {
        console.error('Error loading orders:', error);
    }
    return false;
}

function mapStatusToFilter(status) {
    if (status === 'pending' || status === 'confirmed') return 'processing';
    if (status === 'completed') return 'success';
    if (status === 'cancelled') return 'cancelled';
    return 'processing';
}

function formatPrice(price) {
    return new Intl.NumberFormat('vi-VN').format(price) + ' đ';
}

function getStatusLog(status, date) {
    const statusMessages = {
        'pending': `Đơn hàng của bạn đang được <strong>Chờ xác nhận</strong> và sẽ được xử lý sớm nhất.`,
        'confirmed': `Đơn hàng đã được <strong>Xác nhận</strong> và đang được chuẩn bị.`,
        'shipping': `Đơn hàng đang được <strong>Vận chuyển</strong> đến địa chỉ của bạn.`,
        'completed': `Đơn hàng đã được <strong>Giao thành công</strong>. Cảm ơn bạn đã mua sắm!`,
        'cancelled': `Đơn hàng đã bị <strong>Hủy</strong>.`
    };
    return statusMessages[status] || 'Đơn hàng đang được xử lý.';
}

// ==========================================
// HÀM TẠO HTML THẺ ĐƠN HÀNG (DÙNG CHUNG)
// ==========================================
function createOrderCardHTML(order) {
    let statusClass = 'status-pending';
    let iconHTML = '<i class="fas fa-cube"></i>';
    let iconBg = '';

    if (order.status === 'success') {
        statusClass = 'status-success';
        iconHTML = '<i class="fas fa-check"></i>';
        iconBg = 'background-color: #e0f2f1; color: #0f5132;';
    } else if (order.status === 'cancelled') {
        statusClass = 'status-danger';
        iconHTML = '<i class="fas fa-times"></i>';
        iconBg = 'background-color: #ffebeb; color: #ff4444;';
    }

    return `
    <div class="order-card">
        <div class="order-info">
            <div class="order-icon-box" style="${iconBg}">
                ${iconHTML}
            </div>
            <div>
                <div style="font-weight: 700; font-size: 1rem; margin-bottom: 4px;">${order.productName}</div>
                <div style="font-size: 0.85rem; color: var(--text-gray);">${order.date} • ${order.total}</div>
            </div>
        </div>
        <div class="order-actions-wrapper">
            <span class="status-badge ${statusClass}">${order.statusText}</span>
            ${order.status === 'success' ? `
                <button class="btn btn-primary" onclick="openReviewProductsModal('${order.orderId || order.id}')" 
                    style="padding: 8px 20px; font-size: 0.85rem; border-radius: 50px; font-weight: 600; white-space: nowrap; margin-right: 10px;">
                    <i class="fas fa-star" style="margin-right: 5px;"></i>Đánh giá sản phẩm
                </button>
            ` : ''}
            <button class="btn btn-outline" onclick="openOrderDetail('${order.orderId || order.id}')" 
                style="padding: 8px 20px; font-size: 0.85rem; border-radius: 50px; font-weight: 600; white-space: nowrap;">
                Chi tiết
            </button>
        </div>
    </div>
    `;
}

// ==========================================
// HÀM TẠO NÚT HÀNH ĐỘNG TÙY TRẠNG THÁI
// ==========================================
function getActionButtonsHTML(status) {
    let buttons = '';

    // Chỉ cho phép hủy khi status là "pending"
    if (status === 'pending') {
        buttons += `<button type="button" class="btn btn-danger" onclick="handleCancelOrder()" style="margin-left: 10px;">Hủy đơn</button>`;
    }
    else if (status === 'completed') {
        buttons += `<button type="button" class="btn btn-primary" onclick="handleReorder()" style="margin-left: 10px;">Mua lại</button>`;
    }

    buttons += `<button type="button" class="btn btn-outline" onclick="closeOrderDetailModal()">Đóng</button>`;

    return buttons;
}

// ==========================================
// XỬ LÝ MODAL CHI TIẾT ĐƠN HÀNG
// ==========================================
async function openOrderDetail(orderId) {
    try {
        const response = await fetch(`/Account/GetOrderDetails?id=${orderId}`);
        const result = await response.json();
        
        if (!result.success) {
            showPasswordToast('error', 'Lỗi!', result.message || 'Không thể tải chi tiết đơn hàng');
            return;
        }

        const order = result.order;
        const modalBody = document.getElementById('detail-modal-body');
        const modalFooter = document.getElementById('order-detail-modal').querySelector('.modal-footer');
        if (!modalBody || !modalFooter) return;

        // Store order code and status for cancel function
        window.currentOrderCode = order.orderCode;
        window.currentOrderStatus = order.status;

        // Tạo nội dung thanh hành động
        modalFooter.innerHTML = getActionButtonsHTML(order.status);

        // Tạo nội dung modal
        let itemsHTML = '';
        if (order.items && order.items.length > 0) {
            itemsHTML = order.items.map(item => `
                <div style="display:flex; gap:15px; padding: 10px; border-bottom: 1px solid #eee;">
                    <img src="${item.productImage}" style="width:70px; height:70px; object-fit:contain; border:1px solid #eee; border-radius:8px;" onerror="this.src='/images/placeholder.png'">
                    <div style="flex-grow: 1;">
                        <p style="font-weight: 500;">${item.productName}</p>
                        <p style="font-size:0.85rem; color:#666;">${item.capacity ? item.capacity + ' • ' : ''}SL: ${item.quantity}</p>
                    </div>
                    <div style="text-align: right; font-weight: 700; color:var(--bs-primary);">
                        ${formatPrice(item.total)}
                    </div>
                </div>
            `).join('');
        }

        modalBody.innerHTML = `
            <div style="background:#f9f9f9; padding:15px; border-radius:10px; margin-bottom:15px;">
                <p><strong>Mã đơn:</strong> ${order.orderCode}</p>
                <p><strong>Ngày đặt:</strong> ${order.createdAt}</p>
                <p><strong>Trạng thái:</strong> <span style="font-weight:700; color:var(--bs-primary)">${order.statusName}</span></p>
                <p><strong>Tổng tiền:</strong> <span style="font-weight:700; color:#dc3545;">${formatPrice(order.total)}</span></p>
                <p><strong>Thanh toán:</strong> ${order.paymentMethod}</p>
                <p><strong>Địa chỉ giao hàng:</strong> ${order.shippingAddress}</p>
            </div>
            
            <div style="margin-bottom:15px;">
                <p style="font-weight:bold; margin-bottom:10px; font-size: 1.05rem;">Sản phẩm đã mua:</p>
                ${itemsHTML || '<p style="color:#999;">Không có sản phẩm</p>'}
            </div>

            <div style="padding-top:15px;">
                 <p style="font-weight:bold; margin-bottom:5px;">Lịch sử/Ghi chú hệ thống:</p>
                 <p style="font-size:0.9rem; color:#555;">${getStatusLog(order.status, order.createdAt)}</p>
            </div>
        `;

        document.getElementById('order-detail-modal').classList.add('show');
    } catch (error) {
        console.error('Error loading order details:', error);
        showPasswordToast('error', 'Lỗi!', 'Có lỗi xảy ra khi tải chi tiết đơn hàng');
    }
}

function closeOrderDetailModal() {
    document.getElementById('order-detail-modal').classList.remove('show');
}

async function handleCancelOrder() {
    // Kiểm tra trạng thái đơn hàng
    const orderStatus = window.currentOrderStatus;
    
    if (orderStatus !== 'pending') {
        showPasswordToast('error', 'Lỗi!', 'Chỉ có thể hủy đơn hàng ở trạng thái "Chờ xác nhận"');
        return;
    }

    if (!confirm('Bạn có chắc chắn muốn hủy đơn hàng này không?')) {
        return;
    }

    const orderCode = window.currentOrderCode;
    if (!orderCode) {
        showPasswordToast('error', 'Lỗi!', 'Không thể lấy mã đơn hàng');
        return;
    }
    
    try {
        const response = await fetch(`/Account/CancelOrder?orderCode=${encodeURIComponent(orderCode)}`, {
            method: 'POST'
        });

        const result = await response.json();
        
        if (result.success) {
            showPasswordToast('success', 'Thành công!', result.message || 'Đã hủy đơn hàng thành công');
            closeOrderDetailModal();
            // Reload orders
            await loadOrdersFromServer();
            renderOrders('all');
            loadDashboardData();
        } else {
            showPasswordToast('error', 'Lỗi!', result.message || 'Không thể hủy đơn hàng');
        }
    } catch (error) {
        console.error('Error cancelling order:', error);
        showPasswordToast('error', 'Lỗi!', 'Có lỗi xảy ra khi hủy đơn hàng');
    }
}

function handleReorder() {
    showPasswordToast('info', 'Thông báo', 'Chức năng mua lại sẽ được triển khai sau. Vui lòng thêm sản phẩm vào giỏ hàng từ trang sản phẩm.');
}

// Open modal to review products in completed order
async function openReviewProductsModal(orderId) {
    try {
        const response = await fetch(`/Account/GetOrderDetails?id=${orderId}`);
        const result = await response.json();
        
        if (!result.success) {
            showPasswordToast('error', 'Lỗi!', result.message || 'Không thể tải chi tiết đơn hàng');
            return;
        }

        const order = result.order;
        
        // Check if order is completed
        if (order.status !== 'completed') {
            showPasswordToast('error', 'Lỗi!', 'Chỉ có thể đánh giá sản phẩm khi đơn hàng đã giao thành công');
            return;
        }

        // Build modal content with products list
        let productsHTML = '';
        if (order.items && order.items.length > 0) {
            productsHTML = order.items.map(item => `
                <div style="display: flex; gap: 15px; padding: 15px; border: 1px solid #eee; border-radius: 10px; margin-bottom: 15px; align-items: center;">
                    <img src="${item.productImage}" style="width: 80px; height: 80px; object-fit: contain; border: 1px solid #eee; border-radius: 8px;" onerror="this.src='/images/placeholder.png'">
                    <div style="flex: 1;">
                        <h4 style="font-size: 1rem; font-weight: 600; margin-bottom: 5px;">${item.productName}</h4>
                        <p style="font-size: 0.85rem; color: #666; margin-bottom: 0;">${item.capacity ? item.capacity + ' • ' : ''}Số lượng: ${item.quantity}</p>
                    </div>
                    <a href="/Product/Detail/${item.productId}" class="btn btn-primary" style="padding: 8px 20px; font-size: 0.85rem; border-radius: 50px; white-space: nowrap; text-decoration: none;">
                        <i class="fas fa-star" style="margin-right: 5px;"></i>Đánh giá
                    </a>
                </div>
            `).join('');
        } else {
            productsHTML = '<p style="color: #999; text-align: center; padding: 20px;">Không có sản phẩm nào trong đơn hàng</p>';
        }

        // Create or update review products modal
        let modal = document.getElementById('review-products-modal');
        if (!modal) {
            modal = document.createElement('div');
            modal.id = 'review-products-modal';
            modal.className = 'modal-overlay';
            modal.innerHTML = `
                <div class="modal-content" style="max-width: 700px;">
                    <div class="modal-header">
                        <h4 class="modal-title">Đánh giá sản phẩm</h4>
                        <button type="button" class="close-btn" onclick="closeReviewProductsModal()">&times;</button>
                    </div>
                    <div class="modal-body" id="review-products-modal-body" style="max-height: 500px; overflow-y: auto;">
                    </div>
                    <div class="modal-footer" style="text-align: right; padding-top: 20px;">
                        <button type="button" class="btn btn-outline" onclick="closeReviewProductsModal()">Đóng</button>
                    </div>
                </div>
            `;
            document.body.appendChild(modal);
        }

        const modalBody = document.getElementById('review-products-modal-body');
        if (modalBody) {
            modalBody.innerHTML = `
                <div style="margin-bottom: 20px; padding: 15px; background: #f9fafb; border-radius: 10px;">
                    <p style="margin-bottom: 5px;"><strong>Mã đơn hàng:</strong> ${order.orderCode}</p>
                    <p style="margin-bottom: 0;"><strong>Ngày đặt:</strong> ${order.createdAt}</p>
                </div>
                <h5 style="font-weight: 600; margin-bottom: 15px;">Chọn sản phẩm để đánh giá:</h5>
                ${productsHTML}
            `;
        }

        modal.classList.add('show');
    } catch (error) {
        console.error('Error opening review products modal:', error);
        showPasswordToast('error', 'Lỗi!', 'Có lỗi xảy ra khi mở danh sách sản phẩm');
    }
}

function closeReviewProductsModal() {
    const modal = document.getElementById('review-products-modal');
    if (modal) {
        modal.classList.remove('show');
    }
}

// ==========================================
// HÀM RENDER
// ==========================================
async function loadDashboardData() {
    const container = document.getElementById('dashboard-recent-orders');
    if (!container) return;
    
    await loadOrdersFromServer();
    const recentOrders = ordersData.slice(0, 2);
    container.innerHTML = recentOrders.map(order => createOrderCardHTML(order)).join('');
}

function renderOrders(filterStatus = 'all') {
    const container = document.getElementById('order-list-container');
    if (!container) return;

    const filteredData = filterStatus === 'all'
        ? ordersData
        : ordersData.filter(order => order.status === filterStatus);

    if (filteredData.length === 0) {
        container.innerHTML = '<div class="text-center" style="padding:30px; color:#999;">Không có đơn hàng nào.</div>';
    } else {
        container.innerHTML = filteredData.map(order => createOrderCardHTML(order)).join('');
    }
}

function filterOrders(status, btn) {
    document.querySelectorAll('.btn-filter').forEach(b => b.classList.remove('active'));
    if (btn) btn.classList.add('active');
    renderOrders(status);
}

// ==========================================
// TRACKING ORDER
// ==========================================
async function searchTracking() {
    const input = document.getElementById('tracking-order-code');
    const orderCode = input?.value.trim();
    
    if (!orderCode) {
        showPasswordToast('error', 'Lỗi!', 'Vui lòng nhập mã đơn hàng');
        return;
    }

    // Show loading state
    const searchButton = document.querySelector('.tracking-search button');
    const originalButtonText = searchButton?.innerHTML;
    if (searchButton) {
        searchButton.disabled = true;
        searchButton.innerHTML = '<i class="fas fa-spinner fa-spin me-2"></i>Đang tìm kiếm...';
    }

    try {
        const response = await fetch(`/Account/GetOrderByCode?orderCode=${encodeURIComponent(orderCode)}`);
        const result = await response.json();
        
        if (!result.success) {
            showPasswordToast('error', 'Lỗi!', result.message || 'Không tìm thấy đơn hàng');
            // Hide tracking result
            const trackingResult = document.getElementById('tracking-result');
            if (trackingResult) trackingResult.style.display = 'none';
            return;
        }

        const order = result.order;
        displayTrackingResult(order);
        
        // Show tracking result section
        const trackingResult = document.getElementById('tracking-result');
        if (trackingResult) {
            trackingResult.style.display = 'block';
            // Scroll to result
            trackingResult.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
        }
    } catch (error) {
        console.error('Error searching order:', error);
        showPasswordToast('error', 'Lỗi!', 'Có lỗi xảy ra khi tìm kiếm đơn hàng');
    } finally {
        // Restore button state
        if (searchButton) {
            searchButton.disabled = false;
            searchButton.innerHTML = originalButtonText || '<i class="fas fa-search" style="margin-right: 8px;"></i>Tra cứu';
        }
    }
}

function displayTrackingResult(order) {
    // Update order code display
    const codeDisplay = document.getElementById('tracking-order-code-display');
    if (codeDisplay) codeDisplay.textContent = '#' + order.orderCode;

    // Update order date
    const orderDateEl = document.getElementById('tracking-order-date');
    if (orderDateEl && order.createdAtDate) {
        orderDateEl.textContent = order.createdAtDate;
    }

    // Update expected delivery date
    const expectedDateEl = document.getElementById('tracking-expected-date');
    if (expectedDateEl && order.expectedDeliveryDate) {
        expectedDateEl.textContent = order.expectedDeliveryDate;
    }

    // Calculate progress percentage
    let progress = 0;
    if (order.status === 'completed') progress = 100;
    else if (order.status === 'shipping') progress = 75;
    else if (order.status === 'confirmed') progress = 50;
    else if (order.status === 'pending') progress = 25;
    else if (order.status === 'cancelled') progress = 0;

    const statusName = order.statusName;
    const statusClass = order.status === 'completed' ? 'status-success' : 
                       order.status === 'cancelled' ? 'status-danger' : 'status-pending';

    // Update status badge
    const statusBadge = document.getElementById('tracking-status-badge');
    if (statusBadge) {
        statusBadge.textContent = statusName;
        statusBadge.className = `status-badge ${statusClass}`;
    }

    // Update timeline steps with dates
    const timelineDates = order.timelineDates || {};
    const steps = [
        { id: 'step-1', date: timelineDates.orderDate || '--/--' },
        { id: 'step-2', date: timelineDates.confirmedDate || '--/--' },
        { id: 'step-3', date: timelineDates.shippingDate || '--/--' },
        { id: 'step-4', date: timelineDates.completedDate || '--/--' }
    ];

    steps.forEach((stepInfo, index) => {
        const step = document.getElementById(stepInfo.id);
        if (step) {
            step.classList.remove('active');
            
            // Update date
            const dateEl = step.querySelector('.step-date');
            if (dateEl) {
                dateEl.textContent = stepInfo.date;
            }

            // Activate steps based on status
            if (order.status === 'cancelled') {
                // Don't activate any steps for cancelled orders
            } else if (order.status === 'pending' && index === 0) {
                step.classList.add('active');
            } else if (order.status === 'confirmed' && index <= 1) {
                step.classList.add('active');
            } else if (order.status === 'shipping' && index <= 2) {
                step.classList.add('active');
            } else if (order.status === 'completed' && index <= 3) {
                step.classList.add('active');
            }
        }
    });

    // Update progress bar
    const progressBar = document.getElementById('tracking-progress');
    if (progressBar) {
        progressBar.style.width = progress + '%';
        if (order.status === 'cancelled') {
            progressBar.style.background = '#ef4444';
        } else {
            progressBar.style.background = 'var(--bs-primary)';
        }
    }

    // Update logs with detailed information
    const logsContainer = document.getElementById('tracking-logs');
    if (logsContainer) {
        let logsHTML = '<h4 style="margin-bottom: 15px;">Chi tiết hành trình</h4>';

        // Add order creation log
        if (timelineDates.orderDate) {
            logsHTML += `
                <div class="log-item">
                    <div class="log-icon"><i class="fas fa-shopping-cart"></i></div>
                    <div class="log-content">
                        <h4>Đơn hàng đã được đặt</h4>
                        <p>Đơn hàng #${order.orderCode} đã được tạo thành công</p>
                    </div>
                    <div class="log-time">${timelineDates.orderDate}</div>
                </div>
            `;
        }

        // Add confirmation log
        if (order.status !== 'pending' && timelineDates.confirmedDate) {
            logsHTML += `
                <div class="log-item">
                    <div class="log-icon"><i class="fas fa-check-double"></i></div>
                    <div class="log-content">
                        <h4>Đơn hàng đã được xác nhận</h4>
                        <p>Đơn hàng đã được xác nhận và đang được chuẩn bị</p>
                    </div>
                    <div class="log-time">${timelineDates.confirmedDate}</div>
                </div>
            `;
        }

        // Add shipping log
        if ((order.status === 'shipping' || order.status === 'completed') && timelineDates.shippingDate) {
            logsHTML += `
                <div class="log-item">
                    <div class="log-icon"><i class="fas fa-shipping-fast"></i></div>
                    <div class="log-content">
                        <h4>Đơn hàng đang được vận chuyển</h4>
                        <p>Đơn hàng đã rời kho và đang trên đường đến địa chỉ của bạn</p>
                    </div>
                    <div class="log-time">${timelineDates.shippingDate}</div>
                </div>
            `;
        }

        // Add completion log
        if (order.status === 'completed' && timelineDates.completedDate) {
            logsHTML += `
                <div class="log-item">
                    <div class="log-icon"><i class="fas fa-check-circle"></i></div>
                    <div class="log-content">
                        <h4>Giao hàng thành công</h4>
                        <p>Đơn hàng đã được giao thành công. Cảm ơn bạn đã mua sắm!</p>
                    </div>
                    <div class="log-time">${timelineDates.completedDate}</div>
                </div>
            `;
        }

        // Add cancelled log
        if (order.status === 'cancelled') {
            logsHTML += `
                <div class="log-item">
                    <div class="log-icon"><i class="fas fa-times-circle"></i></div>
                    <div class="log-content">
                        <h4>Đơn hàng đã bị hủy</h4>
                        <p>Đơn hàng đã bị hủy theo yêu cầu hoặc do lý do khác</p>
                    </div>
                    <div class="log-time">${order.createdAt}</div>
                </div>
            `;
        }

        // Add order details section
        if (order.items && order.items.length > 0) {
            logsHTML += `
                <hr style="border: 0; border-top: 1px dashed #eee; margin: 20px 0;">
                <h4 style="margin-bottom: 15px;">Thông tin đơn hàng</h4>
                <div style="background: #f9fafb; padding: 15px; border-radius: 10px; margin-bottom: 15px;">
                    <p style="margin-bottom: 8px;"><strong>Khách hàng:</strong> ${order.customerName}</p>
                    <p style="margin-bottom: 8px;"><strong>Điện thoại:</strong> ${order.customerPhone}</p>
                    <p style="margin-bottom: 8px;"><strong>Địa chỉ:</strong> ${order.shippingAddress}${order.shippingProvince ? ', ' + order.shippingProvince : ''}${order.shippingWard ? ', ' + order.shippingWard : ''}</p>
                    <p style="margin-bottom: 8px;"><strong>Phương thức thanh toán:</strong> ${order.paymentMethod}</p>
                    <p style="margin-bottom: 0;"><strong>Tổng tiền:</strong> <span style="color: var(--bs-primary); font-weight: 700; font-size: 1.1rem;">${formatPrice(order.total)}</span></p>
                </div>
                <h4 style="margin-bottom: 15px;">Sản phẩm trong đơn hàng</h4>
            `;

            order.items.forEach(item => {
                logsHTML += `
                    <div class="log-item" style="margin-bottom: 15px;">
                        <img src="${item.productImage}" style="width: 60px; height: 60px; object-fit: contain; border: 1px solid #eee; border-radius: 8px; margin-right: 15px;" onerror="this.src='/images/placeholder.png'">
                        <div class="log-content" style="flex: 1;">
                            <h4 style="margin-bottom: 5px;">${item.productName}</h4>
                            <p style="font-size: 0.85rem; color: #666;">${item.capacity ? item.capacity + ' • ' : ''}Số lượng: ${item.quantity}</p>
                        </div>
                        <div style="text-align: right; font-weight: 700; color: var(--bs-primary);">
                            ${formatPrice(item.subtotal)}
                        </div>
                    </div>
                `;
            });
        }

        logsContainer.innerHTML = logsHTML;
    }
}

// ==========================================
// UPDATE PROFILE
// ==========================================
async function updateProfile() {
    const fullName = document.getElementById('profile-fullname')?.value;
    const phone = document.getElementById('profile-phone')?.value;

    if (!fullName || !phone) {
        alert('Vui lòng điền đầy đủ thông tin');
        return;
    }

    try {
        const formData = new FormData();
        formData.append('fullName', fullName);
        formData.append('phone', phone);

        const response = await fetch('/Account/UpdateProfile', {
            method: 'POST',
            body: formData,
            headers: {
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
            }
        });

        const result = await response.json();
        if (result.success) {
            showPasswordToast('success', 'Thành công!', result.message || 'Cập nhật thông tin thành công!');
            setTimeout(() => {
                location.reload();
            }, 1500);
        } else {
            showPasswordToast('error', 'Lỗi!', result.message || 'Có lỗi xảy ra');
        }
    } catch (error) {
        console.error('Error updating profile:', error);
        alert('Có lỗi xảy ra khi cập nhật thông tin');
    }
}

// ==========================================
// CHANGE PASSWORD
// ==========================================
async function changePassword() {
    const currentPasswordInput = document.getElementById('current-password');
    const newPasswordInput = document.getElementById('new-password');
    const confirmPasswordInput = document.getElementById('confirm-password');

    const currentPassword = currentPasswordInput?.value.trim();
    const newPassword = newPasswordInput?.value.trim();
    const confirmPassword = confirmPasswordInput?.value.trim();

    // Clear previous error states
    [currentPasswordInput, newPasswordInput, confirmPasswordInput].forEach(input => {
        if (input) {
            input.style.borderColor = '';
            input.style.boxShadow = '';
        }
    });

    // Validation
    if (!currentPassword) {
        showPasswordError(currentPasswordInput, 'Vui lòng nhập mật khẩu hiện tại');
        return;
    }

    if (!newPassword) {
        showPasswordError(newPasswordInput, 'Vui lòng nhập mật khẩu mới');
        return;
    }

    if (newPassword.length < 6) {
        showPasswordError(newPasswordInput, 'Mật khẩu mới phải có ít nhất 6 ký tự');
        return;
    }

    if (!confirmPassword) {
        showPasswordError(confirmPasswordInput, 'Vui lòng xác nhận mật khẩu mới');
        return;
    }

    if (newPassword !== confirmPassword) {
        showPasswordError(confirmPasswordInput, 'Mật khẩu mới và xác nhận mật khẩu không khớp');
        return;
    }

    // Show loading state
    const submitButton = document.querySelector('#password-form button[onclick="changePassword()"]');
    const originalButtonText = submitButton?.textContent;
    if (submitButton) {
        submitButton.disabled = true;
        submitButton.textContent = 'Đang xử lý...';
    }

    try {
        const formData = new FormData();
        formData.append('currentPassword', currentPassword);
        formData.append('newPassword', newPassword);
        formData.append('confirmPassword', confirmPassword);

        const response = await fetch('/Account/ChangePassword', {
            method: 'POST',
            body: formData
        });

        const result = await response.json();
        
        if (result.success) {
            // Success - show toast notification and clear form
            showPasswordSuccess(result.message || 'Đổi mật khẩu thành công!');
            currentPasswordInput.value = '';
            newPasswordInput.value = '';
            confirmPasswordInput.value = '';
        } else {
            // Error - show error message
            showPasswordError(null, result.message || 'Có lỗi xảy ra khi đổi mật khẩu');
            // Also show error toast
            showPasswordToast('error', 'Lỗi!', result.message || 'Có lỗi xảy ra khi đổi mật khẩu');
        }
    } catch (error) {
        console.error('Error changing password:', error);
        showPasswordError(null, 'Có lỗi xảy ra khi đổi mật khẩu. Vui lòng thử lại sau.');
    } finally {
        // Restore button state
        if (submitButton) {
            submitButton.disabled = false;
            submitButton.textContent = originalButtonText || 'Đổi mật khẩu';
        }
    }
}

function showPasswordError(input, message) {
    // Show error message
    let errorDiv = document.getElementById('password-error-message');
    if (!errorDiv) {
        errorDiv = document.createElement('div');
        errorDiv.id = 'password-error-message';
        errorDiv.style.cssText = 'color: #ef4444; padding: 10px; margin-bottom: 15px; background: #ffebeb; border-radius: 8px; border-left: 4px solid #ef4444;';
        const passwordForm = document.getElementById('password-form');
        if (passwordForm) {
            passwordForm.insertBefore(errorDiv, passwordForm.firstChild);
        }
    }
    errorDiv.textContent = message;
    errorDiv.style.display = 'block';

    // Highlight input field
    if (input) {
        input.style.borderColor = '#ef4444';
        input.style.boxShadow = '0 0 0 4px rgba(239, 68, 68, 0.1)';
        input.focus();
    }

    // Auto hide after 5 seconds
    setTimeout(() => {
        if (errorDiv) {
            errorDiv.style.display = 'none';
        }
        if (input) {
            input.style.borderColor = '';
            input.style.boxShadow = '';
        }
    }, 5000);
}

function showPasswordSuccess(message) {
    // Remove any existing error message
    const errorDiv = document.getElementById('password-error-message');
    if (errorDiv) {
        errorDiv.remove();
    }

    // Show toast notification
    showPasswordToast('success', 'Thành công!', message);
}

function showPasswordToast(type, title, message) {
    const toast = document.getElementById('passwordChangeToast');
    if (!toast) return;

    const toastTitle = toast.querySelector('.toast-title');
    const toastMessage = toast.querySelector('.toast-message');
    const toastIcon = toast.querySelector('.toast-icon i');

    // Update content
    toastTitle.textContent = title;
    toastMessage.textContent = message;

    // Update icon and class
    toast.classList.remove('success', 'error', 'hide');
    toast.classList.add(type);

    if (type === 'success') {
        toastIcon.className = 'fas fa-check-circle';
    } else {
        toastIcon.className = 'fas fa-exclamation-circle';
    }

    // Show toast
    toast.style.display = 'flex';
    setTimeout(() => {
        toast.style.animation = 'slideInRight 0.5s ease forwards';
    }, 10);

    // Auto hide after 5 seconds
    setTimeout(() => {
        closePasswordToast();
    }, 5000);
}

function closePasswordToast() {
    const toast = document.getElementById('passwordChangeToast');
    if (!toast) return;

    toast.classList.add('hide');
    setTimeout(() => {
        toast.style.display = 'none';
        toast.classList.remove('hide');
    }, 500);
}

if (history.scrollRestoration) {
    history.scrollRestoration = 'manual';
} else {
    window.onbeforeunload = function () {
        window.scrollTo(0, 0);
    }
}
