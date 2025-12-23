document.addEventListener('DOMContentLoaded', function () {
    // 1. Khai báo các phần tử
    const searchBtn = document.getElementById('searchBtn');       // Nút kính lúp
    const searchOverlay = document.getElementById('searchOverlay'); // Khung tìm kiếm
    const closeSearchBtn = document.getElementById('closeSearchBtn'); // Nút đóng
    const searchInput = document.getElementById('searchInput');   // Ô nhập liệu

    // 2. Kiểm tra an toàn (Debug)
    if (!searchBtn || !searchOverlay || !closeSearchBtn) {
        console.error("Lỗi: Không tìm thấy ID của các phần tử tìm kiếm trong HTML.");
        return;
    }

    // 3. Mở thanh tìm kiếm
    searchBtn.addEventListener('click', function (e) {
        e.preventDefault();
        searchOverlay.classList.add('active');
        // Tự động focus vào ô nhập sau 0.2s để animation chạy xong
        setTimeout(() => searchInput.focus(), 200);
    });

    // 5. Đóng thanh tìm kiếm
    closeSearchBtn.addEventListener('click', function () {
        searchOverlay.classList.remove('active');
    });

    // 5. Đóng khi nhấn phím ESC
    document.addEventListener('keydown', function (e) {
        if (e.key === "Escape" && searchOverlay.classList.contains('active')) {
            searchOverlay.classList.remove('active');
            const suggestionsContainer = document.getElementById('searchSuggestions');
            if (suggestionsContainer) {
                suggestionsContainer.classList.remove('show');
            }
        }
    });

    // 7. Xử lý submit form tìm kiếm và suggestions
    const searchSubmitBtn = document.querySelector('.btn-search-submit');
    const suggestionsContainer = document.getElementById('searchSuggestions');
    let searchTimeout = null;
    let currentRequest = null;

    if (searchInput && searchSubmitBtn) {
        // Xử lý input để hiển thị suggestions với debounce
        searchInput.addEventListener('input', function (e) {
            const query = e.target.value.trim();
            
            // Clear previous timeout
            if (searchTimeout) {
                clearTimeout(searchTimeout);
            }

            // Hide suggestions if empty
            if (!query) {
                hideSuggestions();
                return;
            }

            // Debounce: wait 300ms before making request
            searchTimeout = setTimeout(() => {
                loadSearchSuggestions(query);
            }, 300);
        });

        // Handle arrow keys and Enter in suggestions
        searchInput.addEventListener('keydown', function (e) {
            if (!suggestionsContainer) return;
            
            if (e.key === 'ArrowDown' || e.key === 'ArrowUp') {
                e.preventDefault();
                const items = suggestionsContainer.querySelectorAll('.suggestion-item');
                const currentActive = suggestionsContainer.querySelector('.suggestion-item.active');
                
                if (items.length === 0) return;

                let nextIndex = 0;
                if (currentActive) {
                    const currentIndex = Array.from(items).indexOf(currentActive);
                    nextIndex = e.key === 'ArrowDown' 
                        ? (currentIndex + 1) % items.length
                        : (currentIndex - 1 + items.length) % items.length;
                }

                items.forEach(item => item.classList.remove('active'));
                items[nextIndex].classList.add('active');
                items[nextIndex].scrollIntoView({ block: 'nearest' });
            } else if (e.key === 'Enter') {
                const activeItem = suggestionsContainer.querySelector('.suggestion-item.active');
                if (activeItem && suggestionsContainer.classList.contains('show')) {
                    e.preventDefault();
                    activeItem.click();
                } else {
                    e.preventDefault();
                    performSearch();
                }
            } else if (e.key === 'Escape') {
                hideSuggestions();
            }
        });

        // Hide suggestions when clicking outside
        document.addEventListener('click', function (e) {
            if (!searchInput.closest('.search-input-wrapper').contains(e.target)) {
                hideSuggestions();
            }
        });

        // Submit khi nhấn Enter trong ô input (nếu không có suggestions active)
        searchInput.addEventListener('keypress', function (e) {
            if (e.key === 'Enter') {
                const activeItem = suggestionsContainer ? suggestionsContainer.querySelector('.suggestion-item.active') : null;
                if (!activeItem || !suggestionsContainer || !suggestionsContainer.classList.contains('show')) {
                    e.preventDefault();
                    performSearch();
                }
            }
        });

        // Submit khi click nút "Tìm kiếm"
        searchSubmitBtn.addEventListener('click', function (e) {
            e.preventDefault();
            performSearch();
        });

        function performSearch() {
            const searchTerm = searchInput.value.trim();
            hideSuggestions();
            if (searchTerm) {
                // Chuyển đến trang Shop với parameter search
                const url = new URL('/Shop', window.location.origin);
                url.searchParams.set('search', searchTerm);
                window.location.href = url.toString();
            } else {
                // Nếu không có từ khóa, chỉ chuyển đến Shop
                window.location.href = '/Shop';
            }
        }

        async function loadSearchSuggestions(query) {
            // Cancel previous request if exists
            if (currentRequest) {
                currentRequest.abort();
            }

            // Show loading state
            showSuggestionsLoading();

            try {
                // Make request
                const url = `/Shop/SearchSuggestions?query=${encodeURIComponent(query)}&limit=8`;
                currentRequest = new AbortController();
                
                const response = await fetch(url, {
                    signal: currentRequest.signal
                });

                if (!response.ok) {
                    throw new Error('Network response was not ok');
                }

                const suggestions = await response.json();
                displaySuggestions(suggestions);
            } catch (error) {
                if (error.name !== 'AbortError') {
                    console.error('Error loading search suggestions:', error);
                    hideSuggestions();
                }
            } finally {
                currentRequest = null;
            }
        }

        function showSuggestionsLoading() {
            if (!suggestionsContainer) return;
            suggestionsContainer.innerHTML = '<div class="suggestion-item-loading">Đang tìm kiếm...</div>';
            suggestionsContainer.classList.add('show');
            suggestionsContainer.classList.remove('empty');
        }

        function displaySuggestions(suggestions) {
            if (!suggestionsContainer) return;

            if (!suggestions || suggestions.length === 0) {
                suggestionsContainer.innerHTML = '<div class="suggestion-item-empty">Không tìm thấy sản phẩm nào</div>';
                suggestionsContainer.classList.add('show');
                suggestionsContainer.classList.remove('empty');
                return;
            }

            let html = '';
            suggestions.forEach(item => {
                const imageUrl = item.image || '/images/placeholder.png';
                const priceFormatted = new Intl.NumberFormat('vi-VN').format(item.price);
                
                html += `
                    <a href="/Product/Detail/${item.id}" class="suggestion-item" data-product-id="${item.id}">
                        <img src="${imageUrl}" alt="${escapeHtml(item.name)}" class="suggestion-item-image" onerror="this.src='/images/placeholder.png'">
                        <div class="suggestion-item-content">
                            <div class="suggestion-item-name">${escapeHtml(item.name)}</div>
                            ${item.brand ? `<div class="suggestion-item-brand">${escapeHtml(item.brand)}</div>` : ''}
                            <div class="suggestion-item-price">${priceFormatted} đ</div>
                        </div>
                    </a>
                `;
            });

            suggestionsContainer.innerHTML = html;
            suggestionsContainer.classList.add('show');
            suggestionsContainer.classList.remove('empty');

            // Add click handlers
            suggestionsContainer.querySelectorAll('.suggestion-item').forEach(item => {
                item.addEventListener('click', function(e) {
                    e.preventDefault();
                    const url = this.getAttribute('href');
                    window.location.href = url;
                });
            });
        }

        function hideSuggestions() {
            if (!suggestionsContainer) return;
            suggestionsContainer.classList.remove('show');
        }

        function escapeHtml(text) {
            const div = document.createElement('div');
            div.textContent = text;
            return div.innerHTML;
        }

        // Hide suggestions when overlay closes
        closeSearchBtn.addEventListener('click', function() {
            hideSuggestions();
        });
    }

    // 6. User dropdown menu (Additional for ASP.NET Core)
    const userBtn = document.getElementById('userBtn');
    const userDropdown = document.querySelector('.user-dropdown');
    const userDropdownMenu = document.getElementById('userDropdownMenu');

    if (userBtn && userDropdown) {
        // Toggle dropdown khi click vào icon user
        userBtn.addEventListener('click', function (e) {
            e.preventDefault();
            e.stopPropagation();
            userDropdown.classList.toggle('active');
        });

        // Đóng dropdown khi click ra ngoài
        document.addEventListener('click', function (e) {
            if (userDropdown && !userDropdown.contains(e.target)) {
                userDropdown.classList.remove('active');
            }
        });

        // Đóng dropdown khi nhấn phím ESC
        document.addEventListener('keydown', function (e) {
            if (e.key === "Escape" && userDropdown && userDropdown.classList.contains('active')) {
                userDropdown.classList.remove('active');
            }
        });

        // Đóng dropdown khi click vào link trong menu
        if (userDropdownMenu) {
            userDropdownMenu.addEventListener('click', function (e) {
                // Chỉ đóng nếu click vào link (không phải divider)
                if (e.target.tagName === 'A') {
                    setTimeout(() => {
                        userDropdown.classList.remove('active');
                    }, 100);
                }
            });
        }
    }
});
