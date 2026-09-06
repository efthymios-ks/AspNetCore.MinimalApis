(() => {
    const darkModeClass = 'dark-mode';

    new MutationObserver((_, observer) => {
        if (document.documentElement.classList.contains(darkModeClass)) {
            document.documentElement.classList.remove(darkModeClass);
            observer.disconnect();
        }
    }).observe(document.documentElement, { attributes: true, attributeFilter: ['class'] });
})();