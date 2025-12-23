document.addEventListener('DOMContentLoaded', function() {
    const policyCards = document.querySelectorAll('.policy-card');

    policyCards.forEach(card => {
        const header = card.querySelector('.policy-header');
        const content = card.querySelector('.policy-content');

        header.addEventListener('click', () => {
            const isOpen = card.classList.contains('active');

            // 1. Đóng tất cả các thẻ khác đang mở (Accordion behavior)
            policyCards.forEach(otherCard => {
                if (otherCard !== card && otherCard.classList.contains('active')) {
                    otherCard.classList.remove('active');
                    otherCard.querySelector('.policy-content').style.maxHeight = null;
                }
            });

            // 2. Toggle thẻ hiện tại
            if (isOpen) {
                card.classList.remove('active');
                content.style.maxHeight = null;
            } else {
                card.classList.add('active');
                // scrollHeight là chiều cao thực tế của nội dung bên trong
                content.style.maxHeight = content.scrollHeight + "px";
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
