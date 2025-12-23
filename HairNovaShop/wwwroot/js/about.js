document.addEventListener("DOMContentLoaded", function () {
    // Use shared animation utility instead of creating new observer
    if (window.observeAnimations) {
        window.observeAnimations('.hidden-up, .hidden-left, .hidden-right');
    }
    
    // Stats numbers are already handled by site.js shared utility
    // No need to duplicate the logic here
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
