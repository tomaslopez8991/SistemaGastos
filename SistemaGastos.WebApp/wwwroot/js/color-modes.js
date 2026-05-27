(() => {
    'use strict'

    const getStoredTheme = () => localStorage.getItem('theme')
    const setStoredTheme = theme => localStorage.setItem('theme', theme)

    const getPreferredTheme = () => {
        const storedTheme = getStoredTheme()
        if (storedTheme) {
            return storedTheme
        }
        return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light'
    }

    const setTheme = theme => {
        if (theme === 'auto' && window.matchMedia('(prefers-color-scheme: dark)').matches) {
            document.documentElement.setAttribute('data-bs-theme', 'dark')
        } else {
            document.documentElement.setAttribute('data-bs-theme', theme)
        }
    }

    setTheme(getPreferredTheme())

    const showActiveTheme = (theme, focus = false) => {
        const themeSwitcher = document.querySelector('#bd-theme')

        if (!themeSwitcher) {
            return
        }

        const btnToActive = document.querySelector(`[data-bs-theme-value="${theme}"]`)

        // Iconos map
        const icons = {
            'light': 'fa-sun',
            'dark': 'fa-moon',
            'auto': 'fa-circle-half-stroke'
        }

        // Actualizar ícono del botón principal
        const iconClass = icons[theme] || 'fa-circle-half-stroke';
        const iconElement = themeSwitcher.querySelector('i');
        if (iconElement) {
            // Remover clases viejas y poner la nueva
            iconElement.className = `fa-solid ${iconClass} theme-icon-active`;
        }

        // Manejar estado activo en el dropdown
        document.querySelectorAll('[data-bs-theme-value]').forEach(element => {
            element.classList.remove('active')
            element.setAttribute('aria-pressed', 'false')
        })

        if (btnToActive) {
            btnToActive.classList.add('active')
            btnToActive.setAttribute('aria-pressed', 'true')
        }

        if (focus) {
            themeSwitcher.focus()
        }
    }

    window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', () => {
        const storedTheme = getStoredTheme()
        if (storedTheme !== 'light' && storedTheme !== 'dark') {
            setTheme(getPreferredTheme())
        }
    })

    const initTheme = () => {
        const theme = getPreferredTheme();
        setTheme(theme);
        showActiveTheme(theme);

        // Listeners para los botones del dropdown
        document.querySelectorAll('[data-bs-theme-value]').forEach(toggle => {
            // Remover listeners previos para evitar duplicados con Turbo
            const newToggle = toggle.cloneNode(true);
            toggle.parentNode.replaceChild(newToggle, toggle);

            newToggle.addEventListener('click', () => {
                const theme = newToggle.getAttribute('data-bs-theme-value')
                setStoredTheme(theme)
                setTheme(theme)
                showActiveTheme(theme, true)
            })
        })
    };

    window.addEventListener('DOMContentLoaded', initTheme);
    document.addEventListener('turbo:load', initTheme);
})()
