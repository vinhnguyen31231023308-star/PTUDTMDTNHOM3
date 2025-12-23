document.addEventListener('DOMContentLoaded', function () {
    const homeScope = document.querySelector('.home');

    if (!homeScope) return;

    console.log("Website HairNova (Home Scope) đã tải xong!");

    /* =========================================
       BUTTON EFFECTS (Scoped)
       ========================================= */
    const buttons = homeScope.querySelectorAll('.btn');
    buttons.forEach(btn => {
        btn.addEventListener('click', function (e) {
            if (this.getAttribute('href') === '#') {
                e.preventDefault();
            }
        });
    });

    /* =========================================
       SCROLL RESTORATION (Giữ nguyên logic)
       ========================================= */
    if (history.scrollRestoration) {
        history.scrollRestoration = 'manual';
    } else {
        window.onbeforeunload = function () {
            window.scrollTo(0, 0);
        }
    }

    /* =========================================
       INTERSECTION OBSERVER & ANIMATION 
       ========================================= */
    // Use shared animation utility instead of creating new observer
    // Stats numbers are already handled by site.js shared utility
    if (window.observeAnimations) {
        window.observeAnimations('.hidden-up, .hidden-left, .hidden-right', homeScope);
    }

    /* =========================================
       GLOBAL FUNCTIONS (Gắn vào window để gọi từ HTML onclick)
       ========================================= */

    // Slider Sản phẩm
    window.moveSlider = function (direction) {
        // Tìm ID bên trong .home để tránh nhầm với trang khác
        const track = homeScope.querySelector('#productTrack');
        if (!track) return;

        const cardWidth = 310;
        const scrollAmount = cardWidth * 2;
        if (direction === 1) {
            track.scrollBy({ left: scrollAmount, behavior: 'smooth' });
        } else {
            track.scrollBy({ left: -scrollAmount, behavior: 'smooth' });
        }
    };

    // Slider New Arrival
    window.moveArrivalSlider = function (direction) {
        const track = homeScope.querySelector('#arrivalTrack');
        if (!track) return;

        const cardWidth = 310;
        const scrollAmount = cardWidth;

        if (direction === 1) {
            track.scrollBy({ left: scrollAmount, behavior: 'smooth' });
        } else {
            track.scrollBy({ left: -scrollAmount, behavior: 'smooth' });
        }
    };

    // Copy Code Coupon
    window.copyCode = function (btn, code) {
        navigator.clipboard.writeText(code);
        const originalText = btn.innerText;
        btn.innerText = "ĐÃ CHÉP";
        btn.style.backgroundColor = "#ffb524";

        setTimeout(() => {
            btn.innerText = originalText;
            btn.style.backgroundColor = "";
        }, 2000);
    };

    /* =========================================
       PRODUCT TABS FILTERING
       ========================================= */
    const tabButtons = homeScope.querySelectorAll('.prod-tabs .tab-btn');
    const productTrack = homeScope.querySelector('#productTrack');
    
    if (tabButtons.length > 0 && productTrack) {
        tabButtons.forEach(btn => {
            btn.addEventListener('click', function() {
                // Remove active class from all tabs
                tabButtons.forEach(b => b.classList.remove('active'));
                // Add active class to clicked tab
                this.classList.add('active');
                
                // Get selected category
                const selectedCategory = this.getAttribute('data-category') || '';
                
                // Filter products
                const productCards = productTrack.querySelectorAll('.product-card');
                productCards.forEach(card => {
                    const cardCategory = card.getAttribute('data-category') || '';
                    if (selectedCategory === '' || cardCategory === selectedCategory) {
                        card.style.display = '';
                    } else {
                        card.style.display = 'none';
                    }
                });
                
                // Scroll to start
                productTrack.scrollTo({ left: 0, behavior: 'smooth' });
            });
        });
    }

    /* =========================================
       SWIPER SLIDER 
       ========================================= */
    // Chỉ khởi tạo Swiper nếu tìm thấy class trong .home
    if (homeScope.querySelector('.myTestimonialSwiper')) {

        // Logic clone slide (giữ nguyên logic cũ của bạn)
        var swiperWrapper = homeScope.querySelector('.myTestimonialSwiper .swiper-wrapper');
        if (swiperWrapper) {
            var slides = swiperWrapper.querySelectorAll('.swiper-slide');
            if (slides.length > 0 && slides.length < 6) {
                slides.forEach(function (slide) {
                    swiperWrapper.appendChild(slide.cloneNode(true));
                });
                slides.forEach(function (slide) {
                    swiperWrapper.appendChild(slide.cloneNode(true));
                });
            }
        }

        // Khởi tạo Swiper với selector cụ thể .home
        var swiper = new Swiper(".home .myTestimonialSwiper", {
            slidesPerView: 1,
            spaceBetween: 30,
            loop: true,
            autoplay: {
                delay: 3000,
                disableOnInteraction: false,
            },
            pagination: {
                el: ".home .swiper-pagination",
                clickable: true,
            },
            navigation: {
                nextEl: ".home .next-btn",
                prevEl: ".home .prev-btn",
            },
            breakpoints: {
                768: {
                    slidesPerView: 2,
                },
                1024: {
                    slidesPerView: 3,
                },
            },
        });
    }

    /* =========================================
       MARQUEE ANIMATION
       ========================================= */
    // Marquee cho pills
    const marqueeTrack = homeScope.querySelector('.marquee-track');
    if (marqueeTrack) {
        const pills = marqueeTrack.querySelectorAll('.pill-item');
        if (pills.length > 0) {
            pills.forEach(pill => {
                marqueeTrack.appendChild(pill.cloneNode(true));
            });
        }
    }

    // Marquee cho logos
    const logosTrack = homeScope.querySelector('.logos-slide-track');
    if (logosTrack) {
        const logos = logosTrack.querySelectorAll('.slide');
        if (logos.length > 0) {
            logos.forEach(logo => {
                logosTrack.appendChild(logo.cloneNode(true));
            });
        }
    }

    // Marquee cho values
    const valuesTrack = homeScope.querySelector('.values-track');
    if (valuesTrack) {
        const values = valuesTrack.querySelectorAll('.val-card');
        if (values.length > 0) {
            values.forEach(val => {
                valuesTrack.appendChild(val.cloneNode(true));
            });
        }
    }

    // Marquee cho running text
    const runningTrack = homeScope.querySelector('.running-track .running-content');
    if (runningTrack) {
        const items = runningTrack.querySelectorAll('.text-item');
        if (items.length > 0) {
            const clonedItems = Array.from(items).slice(0, 4); // Clone first 4 items
            clonedItems.forEach(item => {
                runningTrack.appendChild(item.cloneNode(true));
            });
        }
    }

    /* =========================================
       NEWSLETTER FORM
       ========================================= */
    const newsletterForm = homeScope.querySelector('#newsletterForm');
    if (newsletterForm) {
        newsletterForm.addEventListener('submit', function(e) {
            e.preventDefault();
            const emailInput = homeScope.querySelector('#emailInput');
            const messageEl = homeScope.querySelector('#formMessage');
            
            if (emailInput && messageEl) {
                const email = emailInput.value.trim();
                if (email) {
                    messageEl.textContent = 'Cảm ơn bạn đã đăng ký! Chúng tôi sẽ gửi ưu đãi đến email của bạn.';
                    messageEl.style.color = '#0061E1';
                    emailInput.value = '';
                    
                    setTimeout(() => {
                        messageEl.textContent = '';
                    }, 5000);
                }
            }
        });
    }

    /* =========================================
       GLOBAL FUNCTIONS FOR PRODUCT CARDS
       ========================================= */
    window.addToWishlist = function(productId) {
        // TODO: Implement wishlist
        console.log('Add to wishlist:', productId);
        alert('Chức năng yêu thích đang được phát triển!');
    };

    window.addToCart = function(productId, quantity = 1, capacity = null) {
        const formData = new FormData();
        formData.append('quantity', quantity);
        if (capacity && capacity !== '') {
            formData.append('capacity', capacity);
        }

        fetch(`/Cart/Add/${productId}`, {
            method: 'POST',
            body: formData
        })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                // Update cart badge
                const cartBadge = document.querySelector('.cart-badge');
                if (cartBadge) {
                    cartBadge.textContent = data.cartCount || 0;
                } else {
                    // Create badge if doesn't exist
                    const cartBtn = document.querySelector('a[title="Giỏ hàng"]') || document.querySelector('button[title="Giỏ hàng"]');
                    if (cartBtn && data.cartCount > 0) {
                        const badge = document.createElement('span');
                        badge.className = 'badge-count cart-badge';
                        badge.id = 'cartBadge';
                        badge.textContent = data.cartCount;
                        cartBtn.appendChild(badge);
                    }
                }
                // Show success message
                alert(data.message || 'Đã thêm sản phẩm vào giỏ hàng!');
            } else {
                alert(data.message || 'Có lỗi xảy ra!');
            }
        })
        .catch(error => {
            console.error('Error adding to cart:', error);
            alert('Có lỗi xảy ra khi thêm sản phẩm vào giỏ hàng!');
        });
    };
});
