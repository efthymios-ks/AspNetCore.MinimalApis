(function () {
    'use strict';

    function init() {
        const observer = new MutationObserver(() => {
            const topbar = document.querySelector('.topbar-wrapper');
            if (!topbar || document.querySelector('.views-index-link')) return;

            const link = document.createElement('a');
            link.href = '/views-index';
            link.textContent = 'Views Index';
            link.className = 'views-index-link';
            link.style.cssText = [
                'color: #fff',
                'font-size: 0.9rem',
                'font-family: sans-serif',
                'text-decoration: none',
                'padding: 0.4rem 0.9rem',
                'border: 1px solid rgba(255,255,255,0.6)',
                'border-radius: 4px',
                'margin-left: 1.5rem',
                'white-space: nowrap',
            ].join(';');

            topbar.appendChild(link);
            observer.disconnect();
        });

        observer.observe(document.body, { childList: true, subtree: true });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
