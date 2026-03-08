window.themeInterop = {
    toggleDarkMode: function (isDark) {
        document.documentElement.classList.toggle('dark', isDark);
        localStorage.setItem('winsmtprelay-theme', isDark ? 'dark' : 'light');
    },
    setColorTheme: function (theme) {
        if (theme) {
            document.documentElement.setAttribute('data-theme', theme);
        } else {
            document.documentElement.removeAttribute('data-theme');
        }
        localStorage.setItem('winsmtprelay-color-theme', theme || '');
        document.documentElement.dispatchEvent(new CustomEvent('bb-theme-changed'));
    },
    isDarkMode: function () {
        return document.documentElement.classList.contains('dark');
    },
    getColorTheme: function () {
        return localStorage.getItem('winsmtprelay-color-theme') || '';
    }
};
