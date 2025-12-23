document.addEventListener('DOMContentLoaded', function () {
    const shopScope = document.querySelector('.shop');
    if (!shopScope) return;

    const priceRange = shopScope.querySelector("#priceRange");
    const priceValue = shopScope.querySelector("#priceValue");
    const sortTabs = shopScope.querySelectorAll(".sort-tab");
    const brandCheckboxes = shopScope.querySelectorAll(".brand-checkbox");
    const catButtons = shopScope.querySelectorAll(".cat-btn");

    // Format price
    function formatPrice(v) {
        return new Intl.NumberFormat('vi-VN').format(v) + " đ";
    }

    // Update price display
    if (priceRange && priceValue) {
        priceRange.addEventListener("input", () => {
            priceValue.textContent = formatPrice(Number(priceRange.value));
        });
    }

    // Helper function to preserve search parameter when redirecting
    function getCurrentSearchParam() {
        const url = new URL(window.location.href);
        return url.searchParams.get('search') || '';
    }

    // Category buttons - redirect with category parameter
    catButtons.forEach(btn => {
        btn.addEventListener("click", function() {
            // Remove active class from all buttons
            catButtons.forEach(b => b.classList.remove("active"));
            // Add active class to clicked button
            this.classList.add("active");
            
            const category = this.dataset.category;
            const url = new URL(window.location.href);
            if (category === "all") {
                url.searchParams.delete('category');
            } else {
                url.searchParams.set('category', category);
            }
            url.searchParams.delete('page'); // Reset to page 1 when filtering
            // Preserve search parameter
            const searchTerm = getCurrentSearchParam();
            if (searchTerm) {
                url.searchParams.set('search', searchTerm);
            }
            window.location.href = url.toString();
        });
    });

    // Sort tabs - redirect with sort parameter
    sortTabs.forEach(tab => {
        tab.addEventListener("click", function() {
            const sort = this.dataset.sort;
            const url = new URL(window.location.href);
            url.searchParams.set('sort', sort);
            url.searchParams.delete('page'); // Reset to page 1 when sorting
            // Preserve search parameter
            const searchTerm = getCurrentSearchParam();
            if (searchTerm) {
                url.searchParams.set('search', searchTerm);
            }
            window.location.href = url.toString();
        });
    });

    // Brand checkboxes - redirect with brand parameter
    brandCheckboxes.forEach(cb => {
        cb.addEventListener("change", function() {
            const url = new URL(window.location.href);
            if (this.checked) {
                url.searchParams.set('brand', this.value);
            } else {
                url.searchParams.delete('brand');
            }
            url.searchParams.delete('page'); // Reset to page 1 when filtering
            // Preserve search parameter
            const searchTerm = getCurrentSearchParam();
            if (searchTerm) {
                url.searchParams.set('search', searchTerm);
            }
            window.location.href = url.toString();
        });
    });

    // Price range - redirect with maxPrice parameter
    if (priceRange) {
        let priceTimeout = null;
        priceRange.addEventListener("input", function() {
            clearTimeout(priceTimeout);
            priceTimeout = setTimeout(() => {
                const url = new URL(window.location.href);
                url.searchParams.set('maxPrice', this.value);
                url.searchParams.delete('page'); // Reset to page 1 when filtering
                // Preserve search parameter
                const searchTerm = getCurrentSearchParam();
                if (searchTerm) {
                    url.searchParams.set('search', searchTerm);
                }
                window.location.href = url.toString();
            }, 500); // Debounce 500ms
        });
    }

    // Add to cart
    window.addToCart = function(productId) {
        // Kiểm tra xem sản phẩm có variants không
        fetch(`/Cart/GetProductVariants?id=${productId}`)
            .then(response => response.json())
            .then(data => {
                if (!data.success) {
                    showToast(data.message || "Có lỗi xảy ra!");
                    return;
                }

                // Nếu có variants, hiển thị modal chọn dung tích
                if (data.hasVariants && data.variants && data.variants.length > 0) {
                    showCapacityModal(productId, data.variants, data.basePrice);
                } else {
                    // Không có variants, thêm trực tiếp
                    addToCartDirect(productId, 1, null);
                }
            })
            .catch(error => {
                console.error('Error:', error);
                showToast("Có lỗi xảy ra khi kiểm tra sản phẩm!");
            });
    };

    // Hàm hiển thị modal chọn dung tích
    function showCapacityModal(productId, variants, basePrice) {
        // Tạo modal HTML
        let modalHTML = `
            <div id="capacityModal" style="position: fixed; top: 0; left: 0; width: 100%; height: 100%; background: rgba(0,0,0,0.5); z-index: 10000; display: flex; align-items: center; justify-content: center;">
                <div style="background: white; border-radius: 12px; padding: 24px; max-width: 400px; width: 90%; max-height: 90vh; overflow-y: auto;">
                    <h3 style="margin: 0 0 16px 0; font-size: 1.2rem; font-weight: 600;">Chọn dung tích</h3>
                    <div id="capacityOptions" style="margin-bottom: 16px;">
        `;

        variants.forEach(variant => {
            const capacity = variant.capacity || '';
            const price = variant.price || basePrice;
            const stock = variant.stock || 0;
            const priceText = price ? parseFloat(price).toLocaleString('vi-VN') + ' đ' : parseFloat(basePrice).toLocaleString('vi-VN') + ' đ';
            
            modalHTML += `
                <button class="capacity-option-btn" 
                        data-capacity="${capacity}" 
                        data-price="${price || basePrice}"
                        data-stock="${stock}"
                        style="width: 100%; padding: 12px; margin-bottom: 8px; border: 2px solid #e5e7eb; border-radius: 8px; background: white; cursor: pointer; text-align: left; transition: all 0.2s;">
                    <div style="font-weight: 600; color: #111827;">${capacity}</div>
                    <div style="font-size: 0.9rem; color: #6b7280; margin-top: 4px;">
                        ${priceText} ${stock > 0 ? `• Còn ${stock}` : '• Hết hàng'}
                    </div>
                </button>
            `;
        });

        modalHTML += `
                    </div>
                    <div style="display: flex; gap: 8px;">
                        <button id="cancelCapacityBtn" style="flex: 1; padding: 10px; border: 1px solid #e5e7eb; border-radius: 8px; background: white; cursor: pointer; font-weight: 600;">Hủy</button>
                    </div>
                </div>
            </div>
        `;

        // Thêm modal vào DOM
        document.body.insertAdjacentHTML('beforeend', modalHTML);

        const modal = document.getElementById('capacityModal');
        const cancelBtn = document.getElementById('cancelCapacityBtn');
        const optionBtns = document.querySelectorAll('.capacity-option-btn');

        // Xử lý chọn dung tích
        optionBtns.forEach(btn => {
            btn.addEventListener('click', function() {
                const capacity = this.dataset.capacity;
                const stock = parseInt(this.dataset.stock) || 0;
                
                if (stock <= 0) {
                    showToast("Dung tích này đã hết hàng!");
                    return;
                }

                // Đóng modal
                modal.remove();
                
                // Thêm vào giỏ hàng với dung tích đã chọn
                addToCartDirect(productId, 1, capacity);
            });

            // Hover effect
            btn.addEventListener('mouseenter', function() {
                this.style.borderColor = '#0061E1';
                this.style.background = '#f4f8ff';
            });
            btn.addEventListener('mouseleave', function() {
                this.style.borderColor = '#e5e7eb';
                this.style.background = 'white';
            });
        });

        // Xử lý hủy
        cancelBtn.addEventListener('click', () => {
            modal.remove();
        });

        // Đóng khi click bên ngoài
        modal.addEventListener('click', (e) => {
            if (e.target === modal) {
                modal.remove();
            }
        });
    }

    // Hàm thêm vào giỏ hàng trực tiếp
    function addToCartDirect(productId, quantity, capacity, redirectToCart = false) {
        const formData = new FormData();
        formData.append('quantity', quantity);
        if (capacity) {
            formData.append('capacity', capacity);
        }

        fetch(`/Cart/Add/${productId}`, {
            method: 'POST',
            body: formData
        })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                if (redirectToCart) {
                    // Chuyển đến trang giỏ hàng
                    window.location.href = '/Cart';
                } else {
                    showToast(data.message || "Đã thêm sản phẩm vào giỏ hàng!");
                    
                    // Cập nhật cart badge
                    const cartBadge = document.querySelector('.cart-badge');
                    if (cartBadge) {
                        cartBadge.textContent = data.cartCount || 0;
                    } else {
                        // Tạo badge nếu chưa có
                        const cartIcon = document.querySelector('a[href*="Cart"]');
                        if (cartIcon) {
                            const badge = document.createElement('span');
                            badge.className = 'badge-count cart-badge';
                            badge.textContent = data.cartCount || 0;
                            cartIcon.appendChild(badge);
                        }
                    }
                }
            } else {
                if (data.requiresCapacity) {
                    // Nếu cần chọn dung tích, hiển thị lại modal
                    fetch(`/Cart/GetProductVariants?id=${productId}`)
                        .then(response => response.json())
                        .then(variantData => {
                            if (variantData.success && variantData.hasVariants) {
                                showCapacityModal(productId, variantData.variants, variantData.basePrice);
                            }
                        });
                } else {
                    showToast(data.message || "Có lỗi xảy ra!");
                }
            }
        })
        .catch(error => {
            console.error('Error:', error);
            showToast("Có lỗi xảy ra khi thêm sản phẩm!");
        });
    }

    // Buy now
    window.buyNow = function(productId) {
        // Kiểm tra xem sản phẩm có variants không
        fetch(`/Cart/GetProductVariants?id=${productId}`)
            .then(response => response.json())
            .then(data => {
                if (!data.success) {
                    showToast(data.message || "Có lỗi xảy ra!");
                    return;
                }

                // Nếu có variants, hiển thị modal chọn dung tích
                if (data.hasVariants && data.variants && data.variants.length > 0) {
                    showCapacityModalForBuyNow(productId, data.variants, data.basePrice);
                } else {
                    // Không có variants, thêm trực tiếp và chuyển đến giỏ hàng
                    addToCartDirect(productId, 1, null, true);
                }
            })
            .catch(error => {
                console.error('Error:', error);
                showToast("Có lỗi xảy ra khi kiểm tra sản phẩm!");
            });
    };

    // Hàm hiển thị modal chọn dung tích cho Buy Now
    function showCapacityModalForBuyNow(productId, variants, basePrice) {
        // Tạo modal HTML tương tự như addToCart
        let modalHTML = `
            <div id="capacityModalBuyNow" style="position: fixed; top: 0; left: 0; width: 100%; height: 100%; background: rgba(0,0,0,0.5); z-index: 10000; display: flex; align-items: center; justify-content: center;">
                <div style="background: white; border-radius: 12px; padding: 24px; max-width: 400px; width: 90%; max-height: 90vh; overflow-y: auto;">
                    <h3 style="margin: 0 0 16px 0; font-size: 1.2rem; font-weight: 600;">Chọn dung tích</h3>
                    <div id="capacityOptionsBuyNow" style="margin-bottom: 16px;">
        `;

        variants.forEach(variant => {
            const capacity = variant.capacity || '';
            const price = variant.price || basePrice;
            const stock = variant.stock || 0;
            const priceText = price ? parseFloat(price).toLocaleString('vi-VN') + ' đ' : parseFloat(basePrice).toLocaleString('vi-VN') + ' đ';
            
            modalHTML += `
                <button class="capacity-option-btn-buynow" 
                        data-capacity="${capacity}" 
                        data-price="${price || basePrice}"
                        data-stock="${stock}"
                        style="width: 100%; padding: 12px; margin-bottom: 8px; border: 2px solid #e5e7eb; border-radius: 8px; background: white; cursor: pointer; text-align: left; transition: all 0.2s;">
                    <div style="font-weight: 600; color: #111827;">${capacity}</div>
                    <div style="font-size: 0.9rem; color: #6b7280; margin-top: 4px;">
                        ${priceText} ${stock > 0 ? `• Còn ${stock}` : '• Hết hàng'}
                    </div>
                </button>
            `;
        });

        modalHTML += `
                    </div>
                    <div style="display: flex; gap: 8px;">
                        <button id="cancelCapacityBtnBuyNow" style="flex: 1; padding: 10px; border: 1px solid #e5e7eb; border-radius: 8px; background: white; cursor: pointer; font-weight: 600;">Hủy</button>
                    </div>
                </div>
            </div>
        `;

        document.body.insertAdjacentHTML('beforeend', modalHTML);

        const modal = document.getElementById('capacityModalBuyNow');
        const cancelBtn = document.getElementById('cancelCapacityBtnBuyNow');
        const optionBtns = document.querySelectorAll('.capacity-option-btn-buynow');

        optionBtns.forEach(btn => {
            btn.addEventListener('click', function() {
                const capacity = this.dataset.capacity;
                const stock = parseInt(this.dataset.stock) || 0;
                
                if (stock <= 0) {
                    showToast("Dung tích này đã hết hàng!");
                    return;
                }

                modal.remove();
                addToCartDirect(productId, 1, capacity, true);
            });

            btn.addEventListener('mouseenter', function() {
                this.style.borderColor = '#0061E1';
                this.style.background = '#f4f8ff';
            });
            btn.addEventListener('mouseleave', function() {
                this.style.borderColor = '#e5e7eb';
                this.style.background = 'white';
            });
        });

        cancelBtn.addEventListener('click', () => {
            modal.remove();
        });

        modal.addEventListener('click', (e) => {
            if (e.target === modal) {
                modal.remove();
            }
        });
    }

    // Toast message
    const toastEl = document.getElementById("toast");
    const toastMsgEl = document.getElementById("toastMessage");
    let toastTimeout = null;

    window.showToast = function(message) {
        if (!toastEl || !toastMsgEl) return;
        toastMsgEl.textContent = message;
        toastEl.classList.add("show");
        if (toastTimeout) clearTimeout(toastTimeout);
        toastTimeout = setTimeout(() => {
            toastEl.classList.remove("show");
        }, 2000);
    };
});
