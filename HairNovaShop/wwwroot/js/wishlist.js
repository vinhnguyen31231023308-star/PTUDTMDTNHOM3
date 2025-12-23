// =========================================
// WISHLIST GLOBAL FUNCTIONALITY
// =========================================

// Toggle wishlist item - exposed to global scope
// Ensure function is defined to avoid timing issues
window.toggleWishlist = window.toggleWishlist || async function(productId, button) {
    try {
        const response = await fetch('/Wishlist/Toggle', {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
            body: `productId=${productId}`
        });
        
        const result = await response.json();
        
        if (result.requireLogin) {
            // Redirect to login
            if (confirm('Vui lòng đăng nhập để sử dụng chức năng yêu thích. Chuyển đến trang đăng nhập?')) {
                window.location.href = '/Account/Login';
            }
            return;
        }
        
        if (result.success) {
            // Update button state
            const icon = button.querySelector('i');
            if (result.added) {
                button.classList.add('active');
                if (icon) {
                    icon.classList.remove('fa-regular');
                    icon.classList.add('fa-solid');
                }
            } else {
                button.classList.remove('active');
                if (icon) {
                    icon.classList.remove('fa-solid');
                    icon.classList.add('fa-regular');
                }
            }
            
            // Update header badge
            if (window.updateWishlistBadge) {
                window.updateWishlistBadge(result.count);
            }
            
            // Show toast
            if (window.showWishlistToastGlobal) {
                window.showWishlistToastGlobal(result.added ? 'added' : 'removed', result.message);
            }
        } else {
            alert(result.message || 'Có lỗi xảy ra');
        }
    } catch (error) {
        console.error('Error toggling wishlist:', error);
        // Fallback: show error message if fetch fails
        if (button) {
            alert('Có lỗi xảy ra khi thêm vào yêu thích. Vui lòng thử lại sau.');
        }
    }
};

// Update wishlist badge in header
window.updateWishlistBadge = window.updateWishlistBadge || function(count) {
    const badges = document.querySelectorAll('.wishlist-badge');
    badges.forEach(badge => {
        badge.textContent = count;
        badge.style.display = count > 0 ? 'flex' : 'none';
    });
};

// Load user's wishlist and update UI
window.loadUserWishlist = window.loadUserWishlist || async function() {
    try {
        const response = await fetch('/Wishlist/GetUserWishlist');
        const result = await response.json();
        
        if (result.productIds && result.productIds.length > 0) {
            // Mark products that are in wishlist
            result.productIds.forEach(productId => {
                const buttons = document.querySelectorAll(`.wishlist-btn[data-product-id="${productId}"]`);
                buttons.forEach(btn => {
                    btn.classList.add('active');
                    const icon = btn.querySelector('i');
                    if (icon) {
                        icon.classList.remove('fa-regular');
                        icon.classList.add('fa-solid');
                    }
                });
            });
        }
    } catch (error) {
        console.error('Error loading wishlist:', error);
    }
};

// Get wishlist count
window.getWishlistCount = window.getWishlistCount || async function() {
    try {
        const response = await fetch('/Wishlist/Count');
        const result = await response.json();
        if (window.updateWishlistBadge) {
            window.updateWishlistBadge(result.count);
        }
    } catch (error) {
        console.error('Error getting wishlist count:', error);
    }
};

// Global toast for wishlist - expose to window
window.showWishlistToastGlobal = window.showWishlistToastGlobal || function(type, message) {
    // Check if toast container exists
    let toast = document.getElementById('globalWishlistToast');
    
    if (!toast) {
        // Create toast element
        toast = document.createElement('div');
        toast.id = 'globalWishlistToast';
        toast.className = 'wishlist-toast-global';
        toast.innerHTML = `
            <i class="fas fa-heart"></i>
            <span class="toast-message"></span>
        `;
        document.body.appendChild(toast);
        
        // Add styles if not exist
        if (!document.getElementById('wishlistToastStyles')) {
            const style = document.createElement('style');
            style.id = 'wishlistToastStyles';
            style.textContent = `
                .wishlist-toast-global {
                    position: fixed;
                    bottom: 30px;
                    right: 30px;
                    min-width: 280px;
                    background: #fff;
                    border-radius: 12px;
                    box-shadow: 0 15px 40px rgba(0, 0, 0, 0.2);
                    padding: 16px 20px;
                    display: flex;
                    align-items: center;
                    gap: 12px;
                    z-index: 10000;
                    transform: translateX(150%);
                    transition: transform 0.4s cubic-bezier(0.68, -0.55, 0.265, 1.55);
                }
                .wishlist-toast-global.show {
                    transform: translateX(0);
                }
                .wishlist-toast-global.added {
                    border-left: 4px solid #22c55e;
                }
                .wishlist-toast-global.added i {
                    color: #22c55e;
                }
                .wishlist-toast-global.removed {
                    border-left: 4px solid #ef4444;
                }
                .wishlist-toast-global.removed i {
                    color: #ef4444;
                }
                .wishlist-toast-global i {
                    font-size: 1.5rem;
                }
                .wishlist-toast-global .toast-message {
                    font-size: 0.95rem;
                    color: #374151;
                    font-weight: 500;
                }
            `;
            document.head.appendChild(style);
        }
    }
    
    // Update toast
    toast.classList.remove('added', 'removed', 'show');
    toast.classList.add(type);
    
    const icon = toast.querySelector('i');
    icon.className = type === 'added' ? 'fas fa-heart' : 'fas fa-heart-broken';
    
    const messageEl = toast.querySelector('.toast-message');
    messageEl.textContent = message;
    
    // Show toast
    setTimeout(() => toast.classList.add('show'), 10);
    
    // Hide after 3 seconds
    setTimeout(() => {
        toast.classList.remove('show');
    }, 3000);
};

// Initialize on page load
document.addEventListener('DOMContentLoaded', function() {
    // Load user wishlist to mark active items
    window.loadUserWishlist();
    
    // Get initial count
    window.getWishlistCount();
});
