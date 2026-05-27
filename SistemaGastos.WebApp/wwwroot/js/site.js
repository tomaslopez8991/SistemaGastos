// =========================================================
// 0. VARIABLES GLOBALES (EVITAR REDECLARACIÓN CON TURBO)
// =========================================================

window.sidebarResizeTimer = window.sidebarResizeTimer || null;

// =========================================================
// 1. GESTIÓN DE EVENTOS (BLINDADO CONTRA DUPLICADOS)
// =========================================================

// Toggle Sidebar
$(document).off('click.sidebar', '#menu-toggle').on('click.sidebar', '#menu-toggle', function (e) {
    e.preventDefault();
    e.stopPropagation();

    const $wrapper = $('#wrapper');
    $wrapper.toggleClass('toggled');

    // Guardar preferencia
    const isToggled = $wrapper.hasClass('toggled');
    localStorage.setItem('sidebar-toggled', isToggled);

    // Overlay móvil
    if (window.innerWidth <= 992) {
        if ($wrapper.hasClass('toggled')) {
            $('#overlay-mobile').hide();
        } else {
            $('#overlay-mobile').show();
        }
    }
});

// Cerrar al hacer clic en overlay (Móvil)
$(document).off('click.sidebarOverlay', '#overlay-mobile').on('click.sidebarOverlay', '#overlay-mobile', function () {
    $('#wrapper').addClass('toggled');
    $(this).hide();
});

// Cerrar sidebar al hacer clic en el contenido (solo móvil)
$(document).off('click.contentClose', '#page-content-wrapper').on('click.contentClose', '#page-content-wrapper', function (e) {
    if (window.innerWidth <= 992) {
        const $wrapper = $('#wrapper');
        if (!$wrapper.hasClass('toggled')) {
            $wrapper.addClass('toggled');
            $('#overlay-mobile').hide();
        }
    }
});

// =========================================================
// 2. CICLO DE VIDA DE TURBO
// =========================================================

// A. AL INICIAR NAVEGACIÓN -> Spinner
document.addEventListener("turbo:visit", function () {
    $('#spinner').fadeIn(100);
});

// B. AL CARGAR LA PÁGINA (Restaura el estado visual)
$(document).on('turbo:load', function () {

    // 1. Ocultar Spinner
    $('#spinner').fadeOut(200);

    // 2. Restaurar Estado Sidebar
    const $wrapper = $('#wrapper');
    const isToggled = localStorage.getItem('sidebar-toggled') === 'true';
    const isDesktop = window.innerWidth > 992;

    if (isDesktop) {
        if (isToggled) {
            $wrapper.addClass('toggled');
        } else {
            $wrapper.removeClass('toggled');
        }
        $('#overlay-mobile').hide();
    } else {
        $wrapper.addClass('toggled');
        $('#overlay-mobile').hide();
    }

    // 3. Marcar Link Activo en Sidebar
    marcarLinkActivo();

    // 4. Reinicializar Bootstrap Dropdowns
    $('.dropdown-toggle').each(function () {
        const existingInstance = bootstrap.Dropdown.getInstance(this);
        if (existingInstance) {
            existingInstance.dispose();
        }
        new bootstrap.Dropdown(this);
    });

    // Reinicializar Tooltips
    $('[data-bs-toggle="tooltip"]').each(function () {
        const existingInstance = bootstrap.Tooltip.getInstance(this);
        if (existingInstance) {
            existingInstance.dispose();
        }
        new bootstrap.Tooltip(this);
    });

    // 5. Plugins jQuery Globales
    if ($.fn.mask) {
        $('.money').mask("#.##0,00", { reverse: true });
    }

    // 6. Crear overlay si no existe
    if ($('#overlay-mobile').length === 0) {
        $('body').append('<div id="overlay-mobile"></div>');
    }
});

// C. LIMPIEZA ANTES DE SALIR
document.addEventListener("turbo:before-cache", function () {
    $('.modal.show').modal('hide');
    $('.dropdown-menu.show').removeClass('show');
    if (typeof Swal !== 'undefined') Swal.close();
    $('#overlay-mobile').hide();
    $('#spinner').hide();
    $('.nav-item').removeClass('active');

    // Limpiar tooltips y dropdowns
    $('[data-bs-toggle="tooltip"]').each(function () {
        const tooltip = bootstrap.Tooltip.getInstance(this);
        if (tooltip) tooltip.dispose();
    });

    $('.dropdown-toggle').each(function () {
        const dropdown = bootstrap.Dropdown.getInstance(this);
        if (dropdown) dropdown.dispose();
    });

    const swalContainer = document.querySelector('.swal2-container');
    if (swalContainer) {
        swalContainer.remove();
    }
});

// =========================================================
// 3. FUNCIONES AUXILIARES SIDEBAR
// =========================================================

function marcarLinkActivo() {
    const currentPath = window.location.pathname.toLowerCase();
    $('.nav-item').removeClass('active');

    $('.nav-item').each(function () {
        const $link = $(this);
        const linkPath = $link.attr('href')?.toLowerCase();

        if (!linkPath) return;

        if (currentPath === linkPath) {
            $link.addClass('active');
            return false;
        }

        if (linkPath !== '/' && currentPath.startsWith(linkPath)) {
            $link.addClass('active');
        }
    });
}

function handleSidebarResize() {
    const $wrapper = $('#wrapper');
    const isDesktop = window.innerWidth > 992;

    if (isDesktop) {
        const isToggled = localStorage.getItem('sidebar-toggled') === 'true';
        if (isToggled) {
            $wrapper.addClass('toggled');
        } else {
            $wrapper.removeClass('toggled');
        }
        $('#overlay-mobile').hide();
    } else {
        $wrapper.addClass('toggled');
        $('#overlay-mobile').hide();
    }
}

// Escuchar cambios de tamaño (usando variable global)
$(window).off('resize.sidebar').on('resize.sidebar', function () {
    clearTimeout(window.sidebarResizeTimer);
    window.sidebarResizeTimer = setTimeout(function () {
        handleSidebarResize();
    }, 250);
});

// =========================================================
// 4. HELPERS GLOBALES
// =========================================================

function formatNumber(amount) {
    return new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS' }).format(amount);
}

async function fetchApi(url, method = 'GET', body = null) {
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

    const headers = {
        'Content-Type': 'application/json'
    };

    if (token) {
        headers['RequestVerificationToken'] = token;
    }

    const options = {
        method: method,
        headers: headers
    };

    if (body) options.body = JSON.stringify(body);

    const response = await fetch(url, options);
    const json = await response.json();

    if (!response.ok) {
        throw new Error(json.message || "Error desconocido");
    }

    if (json.success !== undefined && !json.success) {
        throw new Error(json.message);
    }

    return json.data !== undefined ? json.data : json;
}

// ================================
// SISTEMA DE TUTORIALES INTERACTIVOS
// ================================

(function () {
    'use strict';

    const tutorialesConfig = {
        // Gestión diaria
        'transacciones': {
            storageKey: 'transacciones_tutorial_completed',
            titulo: '¡Bienvenido a Transacciones!',
            descripcion: '¿Quieres ver cómo registrar tus ingresos y gastos diarios?'
        },
        'billetera': {
            storageKey: 'billetera_tutorial_completed',
            titulo: '¡Bienvenido a Mi Billetera!',
            descripcion: '¿Quieres aprender a gestionar tus cuentas y ver tus saldos?'
        },
        'tareas': {
            storageKey: 'tareas_tutorial_completed',
            titulo: '¡Bienvenido a Tareas!',
            descripcion: '¿Quieres ver cómo organizar tus pendientes financieros?'
        },

        // Análisis
        'creditcard': {
            storageKey: 'creditcard_tutorial_completed',
            titulo: '¡Bienvenido a Tarjetas de Crédito!',
            descripcion: '¿Quieres aprender a gestionar tus consumos y cuotas?'
        },
        'cashflow': {
            storageKey: 'cashflow_tutorial_completed',
            titulo: '¡Bienvenido a Cashflow!',
            descripcion: '¿Quieres ver cómo proyectar tu liquidez futura mes a mes?'
        },
        'presupuestos': {
            storageKey: 'presupuestos_tutorial_completed',
            titulo: '¡Bienvenido a Presupuestos!',
            descripcion: '¿Quieres aprender a controlar tus límites de gasto?'
        },
        'estadisticas': {
            storageKey: 'estadisticas_tutorial_completed',
            titulo: '¡Bienvenido a Estadísticas!',
            descripcion: '¿Quieres ver cómo analizar tus patrones de gasto?'
        },
        'gastosfijos': {
            storageKey: 'gastosfijos_tutorial_completed',
            titulo: '¡Bienvenido a Gastos Fijos!',
            descripcion: '¿Quieres controlar tus suscripciones y servicios recurrentes?'
        },

        // Administración
        'categorias': {
            storageKey: 'categorias_tutorial_completed',
            titulo: '¡Bienvenido a Categorías!',
            descripcion: '¿Quieres aprender a organizar tus movimientos con etiquetas?'
        },
        'perfil': {
            storageKey: 'perfil_tutorial_completed',
            titulo: '¡Bienvenido a tu Perfil!',
            descripcion: '¿Quieres ver cómo personalizar tu cuenta?'
        },

        // Dashboard
        'dashboard': {
            storageKey: 'dashboard_tutorial_completed',
            titulo: '¡Bienvenido a tu Dashboard!',
            descripcion: '¿Quieres un recorrido por las funciones principales del sistema?'
        }
    };

    // =========================================================
    // DETECTAR MÓDULO ACTUAL (MEJORADO Y COMPLETO)
    // =========================================================

    function detectarModuloActual() {
        const path = window.location.pathname.toLowerCase();

        // ============================================
        // GESTIÓN DIARIA
        // ============================================

        if (path.includes('/transaction/index') ||
            (path.includes('/transaction') && !path.includes('tmp') && !path.includes('credit'))) {
            return 'transacciones';
        }

        if (path.includes('/account')) {
            return 'billetera';
        }

        if (path.includes('/todo')) {
            return 'tareas';
        }

        // ============================================
        // ANÁLISIS
        // ============================================

        if (path.includes('/creditcard')) {
            return 'creditcard';
        }

        if (path.includes('/tmptransaction')) {
            return 'cashflow';
        }

        if (path.includes('/budget')) {
            return 'presupuestos';
        }

        if (path.includes('/transaction/statistics') || path.includes('/statistics')) {
            return 'estadisticas';
        }

        if (path.includes('/fixedexpense')) {
            return 'gastosfijos';
        }

        // ============================================
        // ADMINISTRACIÓN
        // ============================================

        if (path.includes('/category')) {
            return 'categorias';
        }

        if (path.includes('/login/profile') || path.includes('/profile')) {
            return 'perfil';
        }

        if (path === '/' || path === '/home' || path.includes('/home/index')) {
            return 'dashboard';
        }

        return null;
    }

    // ✅ INICIAR TUTORIAL
    function iniciarTutorial(moduloKey) {
        const config = tutorialesConfig[moduloKey];
        if (!config) {
            console.warn('No hay tutorial configurado para este módulo');
            return;
        }

        const intro = introJs();

        intro.setOptions({
            nextLabel: 'Siguiente →',
            prevLabel: '← Anterior',
            skipLabel: 'Saltar',
            doneLabel: '¡Entendido!',
            showProgress: true,
            showBullets: false,
            exitOnOverlayClick: false,
            disableInteraction: false,
            scrollToElement: true,
            overlayOpacity: 0.7,
            tooltipClass: 'customTooltip',
            highlightClass: 'customHighlight'
        });

        // Callback al completar
        intro.oncomplete(function () {
            localStorage.setItem(config.storageKey, 'true');
            Swal.fire({
                icon: 'success',
                title: '¡Tutorial completado!',
                text: 'Ahora puedes usar este módulo con confianza. Usa el botón de ayuda (?) si necesitas verlo de nuevo.',
                confirmButtonText: 'Comenzar',
                timer: 3000,
                timerProgressBar: true
            });
        });

        // Callback al salir
        intro.onexit(function () {
            localStorage.setItem(config.storageKey, 'true');
        });

        intro.start();
    }

    // ✅ VERIFICAR PRIMERA VISITA AL MÓDULO
    function verificarPrimeraVisita(moduloKey) {
        const config = tutorialesConfig[moduloKey];
        if (!config) return;

        const tutorialCompletado = localStorage.getItem(config.storageKey);

        if (!tutorialCompletado) {
            // Esperar a que se carguen los datos del módulo
            setTimeout(() => {
                // Verificar si el módulo tiene pasos de tutorial definidos
                const tieneIntros = document.querySelectorAll('[data-intro]').length > 0;

                if (!tieneIntros) {
                    console.warn('No hay elementos con data-intro en este módulo');
                    localStorage.setItem(config.storageKey, 'true');
                    return;
                }

                Swal.fire({
                    icon: 'info',
                    title: config.titulo,
                    text: config.descripcion,
                    showCancelButton: true,
                    confirmButtonText: 'Sí, mostrar tutorial',
                    cancelButtonText: 'No, saltar',
                    customClass: {
                        confirmButton: 'btn btn-primary',
                        cancelButton: 'btn btn-outline-secondary'
                    }
                }).then((result) => {
                    if (result.isConfirmed) {
                        iniciarTutorial(moduloKey);
                    } else {
                        localStorage.setItem(config.storageKey, 'true');
                    }
                });
            }, 1500);
        }
    }

    // ✅ BOTÓN DE AYUDA FLOTANTE
    function configurarBotonAyuda() {
        const moduloActual = detectarModuloActual();
        const $btnAyuda = $('#btnAyudaGlobal');

        if (!moduloActual || !tutorialesConfig[moduloActual]) {
            // Si no hay tutorial para este módulo, ocultar el botón
            $btnAyuda.hide();
            return;
        }

        // Verificar si hay elementos con data-intro
        const tieneIntros = document.querySelectorAll('[data-intro]').length > 0;

        if (!tieneIntros) {
            $btnAyuda.hide();
            return;
        }

        // Mostrar botón y configurar click
        $btnAyuda.show();

        $btnAyuda.off('click').on('click', function () {
            iniciarTutorial(moduloActual);
        });

        // Animación de pulso para llamar la atención (solo primeras 5 segundos)
        setTimeout(() => {
            $btnAyuda.addClass('pulse');
            setTimeout(() => {
                $btnAyuda.removeClass('pulse');
            }, 5000);
        }, 2000);
    }

    // ✅ INICIALIZAR SISTEMA DE TUTORIALES
    function inicializarTutoriales() {
        const moduloActual = detectarModuloActual();

        if (moduloActual) {
            configurarBotonAyuda();
            verificarPrimeraVisita(moduloActual);
        } else {
            $('#btnAyudaGlobal').hide();
        }
    }

    // ✅ REINICIAR TODOS LOS TUTORIALES (Para desarrollo/testing)
    window.resetearTutoriales = function () {
        Object.values(tutorialesConfig).forEach(config => {
            localStorage.removeItem(config.storageKey);
        });
        Swal.fire('Tutoriales reiniciados', 'Recarga la página para verlos de nuevo', 'info');
    };

    // ✅ EXPORTAR FUNCIÓN PARA USO MANUAL
    window.mostrarTutorial = function () {
        const moduloActual = detectarModuloActual();
        if (moduloActual) {
            iniciarTutorial(moduloActual);
        } else {
            Swal.fire('Aviso', 'No hay tutorial disponible para esta página', 'info');
        }
    };

    $(document).on('turbo:load', function () {
        inicializarTutoriales();
    });

})();
