// Optimized Animation Utility - Shared across all pages
// This reduces timeout issues by using a single IntersectionObserver and optimizing animations

(function() {
    'use strict';
    
    // Single shared IntersectionObserver for all pages
    let sharedObserver = null;
    
    // Initialize shared observer lazily
    function getSharedObserver() {
        if (!sharedObserver) {
            const observerOptions = {
                root: null,
                rootMargin: '0px',
                threshold: 0.1
            };
            
            sharedObserver = new IntersectionObserver((entries) => {
                // Process entries using requestAnimationFrame for smooth animation
                entries.forEach(entry => {
                    if (entry.isIntersecting) {
                        requestAnimationFrame(() => {
                            entry.target.classList.add('show');
                            // Remove will-change after animation to free resources
                            setTimeout(() => {
                                if (entry.target.style) {
                                    entry.target.style.willChange = 'auto';
                                }
                            }, 800);
                        });
                        sharedObserver.unobserve(entry.target);
                    }
                });
            }, observerOptions);
        }
        return sharedObserver;
    }
    
    // Optimized function to observe elements in batches
    window.observeAnimations = function(selector, scope = document) {
        const elements = scope.querySelectorAll(selector);
        const observer = getSharedObserver();
        
        // Process in batches to avoid blocking main thread
        const processBatch = (startIndex) => {
            const batchSize = 10;
            const endIndex = Math.min(startIndex + batchSize, elements.length);
            
            for (let i = startIndex; i < endIndex; i++) {
                const el = elements[i];
                // Add will-change for GPU acceleration before observing
                if (!el.classList.contains('show')) {
                    if (el.style) {
                        el.style.willChange = 'transform, opacity';
                    }
                    observer.observe(el);
                }
            }
            
            if (endIndex < elements.length) {
                requestAnimationFrame(() => processBatch(endIndex));
            }
        };
        
        if (elements.length > 0) {
            processBatch(0);
        }
    };
    
    // Optimized counter animation using requestAnimationFrame
    window.animateCounter = function(element, target, duration = 2000) {
        let start = 0;
        const startTime = performance.now();
        
        const animate = (currentTime) => {
            const elapsed = currentTime - startTime;
            const progress = Math.min(elapsed / duration, 1);
            
            // Easing function for smooth animation
            const easeOutQuart = 1 - Math.pow(1 - progress, 4);
            const current = Math.floor(start + (target - start) * easeOutQuart);
            
            element.textContent = current;
            
            if (progress < 1) {
                requestAnimationFrame(animate);
            } else {
                element.textContent = target;
            }
        };
        
        requestAnimationFrame(animate);
    };
    
    // Initialize on DOMContentLoaded
    function initAnimations() {
        // Observe common animation classes globally
        const hiddenSelectors = '.hidden-up, .hidden-left, .hidden-right';
        window.observeAnimations(hiddenSelectors);
        
        // Handle stat numbers separately with optimized counter
        const statObserver = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    const statEl = entry.target;
                    const target = parseInt(statEl.getAttribute('data-target')) || 0;
                    window.animateCounter(statEl, target, 2000);
                    statObserver.unobserve(statEl);
                }
            });
        }, { threshold: 0.1 });
        
        document.querySelectorAll('.stat-number').forEach(stat => {
            statObserver.observe(stat);
        });
    }
    
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initAnimations);
    } else {
        initAnimations();
    }
})();
