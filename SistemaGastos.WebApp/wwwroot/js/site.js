// =========================================================
// 0. VARIABLES GLOBALES (EVITAR REDECLARACIÓN CON TURBO)
// =========================================================

window.sidebarResizeTimer = window.sidebarResizeTimer || null;

function closeMobileSidebar() {
    if (window.innerWidth > 992) return;

    $('#wrapper').addClass('toggled');
    $('#overlay-mobile').hide();
    $('body').removeClass('sidebar-open');
    $('#menu-toggle').attr('aria-expanded', 'false').trigger('focus');
}

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
            $('body').removeClass('sidebar-open');
            $(this).attr('aria-expanded', 'false');
        } else {
            $('#overlay-mobile').show();
            $('body').addClass('sidebar-open');
            $(this).attr('aria-expanded', 'true');
            $('#sidebar-close').trigger('focus');
        }
    }
});

$(document).off('click.sidebarClose', '#sidebar-close').on('click.sidebarClose', '#sidebar-close', function () {
    closeMobileSidebar();
});

// Cerrar al hacer clic en overlay (Móvil)
$(document).off('click.sidebarOverlay', '#overlay-mobile').on('click.sidebarOverlay', '#overlay-mobile', function () {
    closeMobileSidebar();
});

// Cerrar sidebar al hacer clic en el contenido (solo móvil)
$(document).off('click.contentClose', '#page-content-wrapper').on('click.contentClose', '#page-content-wrapper', function (e) {
    if (window.innerWidth <= 992) {
        const $wrapper = $('#wrapper');
        if (!$wrapper.hasClass('toggled')) {
            closeMobileSidebar();
        }
    }
});

$(document).off('keydown.sidebar').on('keydown.sidebar', function (e) {
    if (e.key === 'Escape' && window.innerWidth <= 992 && !$('#wrapper').hasClass('toggled')) {
        closeMobileSidebar();
    }
});

$(document).off('click.sidebarLink', '#sidebar-wrapper .nav-item').on('click.sidebarLink', '#sidebar-wrapper .nav-item', function () {
    if (window.innerWidth <= 992) closeMobileSidebar();
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
        $('body').removeClass('sidebar-open');
        $('#menu-toggle').attr('aria-expanded', 'false');
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
    $('#btnAyudaGlobal').removeClass('tutorial-fab--visible tutorial-fab--new tutorial-pulse');

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
        'dashboard': {
            storageKey: 'dashboard_tutorial_completed',
            icono: 'fa-house',
            titulo: 'Dashboard',
            descripcion: 'Tu resumen financiero en tiempo real.',
            features: ['Saldo, ingresos y balance del mes', 'Vencimientos próximos y módulos más usados'],
            stepTitles: ['Panel principal', 'Métricas del mes', 'Gastos fijos pendientes', 'Módulos más usados', 'Accesos rápidos']
        },
        'transacciones': {
            storageKey: 'transacciones_tutorial_completed',
            icono: 'fa-money-bill-transfer',
            titulo: 'Transacciones',
            descripcion: 'Registrá cada ingreso y gasto.',
            features: ['Crear movimientos en segundos', 'Filtrar por fecha, cuenta o categoría'],
            stepTitles: ['Módulo de transacciones', 'Crear transacción', 'Filtros', 'Lista de movimientos']
        },
        'transacciones-crear': {
            storageKey: 'transacciones_crear_tutorial_completed',
            icono: 'fa-layer-group',
            titulo: 'Registrar Movimiento',
            descripcion: 'Cargá uno o varios movimientos a la vez.',
            features: ['Carga masiva con varios bloques', 'Cada bloque puede dividirse entre cuentas'],
            stepTitles: ['Registrar movimientos', 'Carga masiva', 'Guardar']
        },
        'billetera': {
            storageKey: 'billetera_tutorial_completed',
            icono: 'fa-building-columns',
            titulo: 'Mis Cuentas',
            descripcion: 'Tus cuentas bancarias y efectivo.',
            features: ['Registrar y gestionar cuentas', 'Ver saldo consolidado por moneda'],
            stepTitles: ['Mis Cuentas', 'Agregar cuenta', 'Saldos por moneda', 'Lista de cuentas']
        },
        'tareas': {
            storageKey: 'tareas_tutorial_completed',
            icono: 'fa-list-check',
            titulo: 'Tareas',
            descripcion: 'Tus pagos y compromisos pendientes.',
            features: ['Crear tareas con prioridad y recordatorio', 'Ver pendientes y completadas'],
            stepTitles: ['Módulo de tareas', 'Crear tarea', 'Filtrar por prioridad', 'Pendientes', 'Completadas']
        },
        'creditcard': {
            storageKey: 'creditcard_tutorial_completed',
            icono: 'fa-credit-card',
            titulo: 'Tarjetas de crédito',
            descripcion: 'Controlá consumos y cuotas.',
            features: ['Registrar consumos en cuotas', 'Ver deuda total y distribución'],
            stepTitles: ['Tarjetas de crédito', 'Nuevo consumo', 'Deuda total', 'Composición de deuda', 'Lista de consumos']
        },
        'cashflow': {
            storageKey: 'cashflow_tutorial_completed',
            icono: 'fa-timeline',
            titulo: 'Proyección',
            descripcion: 'Tu cashflow proyectado mes a mes.',
            features: ['Planificar ingresos y gastos futuros', 'Ver liquidez de un vistazo por mes'],
            stepTitles: ['Módulo de proyección', 'Crear elemento', 'Selector de mes', 'Barra del mes', 'Pestañas de contenido']
        },
        'presupuestos': {
            storageKey: 'presupuestos_tutorial_completed',
            icono: 'fa-chart-pie',
            titulo: 'Presupuestos',
            descripcion: 'Límites de gasto por categoría.',
            features: ['Definir un tope por categoría', 'Seguir el avance en tiempo real'],
            stepTitles: ['Módulo de presupuestos', 'Navegar meses', 'Crear presupuesto', 'Panel resumen', 'Tarjetas por categoría', 'Alerta inteligente']
        },
        'estadisticas': {
            storageKey: 'estadisticas_tutorial_completed',
            icono: 'fa-chart-line',
            titulo: 'Estadísticas',
            descripcion: 'Análisis histórico de tus finanzas.',
            features: ['Tendencia ingresos vs gastos (6 meses)', 'Patrimonio, top gastos y proyecciones'],
            stepTitles: ['Módulo de estadísticas', 'Patrimonio neto', 'Actividad del mes', 'Tendencia histórica', 'Top categorías']
        },
        'gastosfijos': {
            storageKey: 'gastosfijos_tutorial_completed',
            icono: 'fa-file-invoice-dollar',
            titulo: 'Gastos Fijos',
            descripcion: 'Suscripciones y pagos recurrentes.',
            features: ['Registrar servicios y suscripciones', 'Ver vencimientos y estado de pago'],
            stepTitles: ['Módulo de gastos fijos', 'Agregar gasto fijo', 'Resumen mensual', 'Filtros de búsqueda', 'Lista de gastos']
        },
        'categorias': {
            storageKey: 'categorias_tutorial_completed',
            icono: 'fa-tags',
            titulo: 'Categorías',
            descripcion: 'Las etiquetas para clasificar tus movimientos.',
            features: ['Crear categorías de ingresos y gastos', 'Elegir ícono y color personalizados'],
            stepTitles: ['Módulo de categorías', 'Nueva categoría', 'Filtrar por tipo', 'Lista de categorías']
        },
        'perfil': {
            storageKey: 'perfil_tutorial_completed',
            icono: 'fa-user-gear',
            titulo: 'Mi Perfil',
            descripcion: 'Tu cuenta y configuración de seguridad.',
            features: ['Actualizar tu email de contacto', 'Cambiar tu contraseña'],
            stepTitles: ['Mi Perfil', 'Tu identidad', 'Actualizar datos', 'Cambiar contraseña']
        }
    };

    // =========================================================
    // DETECTAR MÓDULO ACTUAL
    // =========================================================

    function detectarModuloActual() {
        const path = window.location.pathname.toLowerCase();

        if (path.includes('/transaction/create')) return 'transacciones-crear';
        if (path.includes('/transaction/index') ||
            (path.includes('/transaction') && !path.includes('tmp') && !path.includes('credit') && !path.includes('statistics') && !path.includes('/create'))) {
            return 'transacciones';
        }
        if (path.includes('/account')) return 'billetera';
        if (path.includes('/todo')) return 'tareas';
        if (path.includes('/creditcard')) return 'creditcard';
        if (path.includes('/tmptransaction')) return 'cashflow';
        if (path.includes('/budget')) return 'presupuestos';
        if (path.includes('/transaction/statistics') || path.includes('/statistics')) return 'estadisticas';
        if (path.includes('/fixedexpense')) return 'gastosfijos';
        if (path.includes('/category')) return 'categorias';
        if (path.includes('/login/profile') || path.includes('/profile')) return 'perfil';
        if (path === '/' || path === '/home' || path.includes('/home/index')) return 'dashboard';

        return null;
    }

    // =========================================================
    // INICIAR TUTORIAL
    // =========================================================

    function iniciarTutorial(moduloKey) {
        const config = tutorialesConfig[moduloKey];
        if (!config) return;

        const intro = introJs();

        intro.setOptions({
            nextLabel: 'Siguiente <i class="fa-solid fa-arrow-right ms-1" style="font-size:0.75rem"></i>',
            prevLabel: '<i class="fa-solid fa-arrow-left me-1" style="font-size:0.75rem"></i> Anterior',
            skipLabel: '✕',
            doneLabel: '<i class="fa-solid fa-check me-1"></i> ¡Listo!',
            showProgress: true,
            showBullets: true,
            exitOnOverlayClick: false,
            disableInteraction: false,
            scrollToElement: true,
            scrollPadding: 80,
            overlayOpacity: 0.68,
            tooltipClass: 'customTooltip',
            highlightClass: 'customHighlight',
            showStepNumbers: true
        });

        // Aplicar títulos de cada paso dinámicamente (sin tocar el HTML)
        if (config.stepTitles && config.stepTitles.length > 0) {
            const elements = Array.from(document.querySelectorAll('[data-intro]'))
                .sort((a, b) => parseInt(a.dataset.step || 0) - parseInt(b.dataset.step || 0));
            elements.forEach((el, idx) => {
                el.removeAttribute('data-title');
                if (config.stepTitles[idx]) {
                    el.setAttribute('data-title', config.stepTitles[idx]);
                }
            });
        }

        intro.oncomplete(function () {
            localStorage.setItem(config.storageKey, 'true');
            $('#btnAyudaGlobal').removeClass('tutorial-fab--new');
            Swal.fire({
                icon: 'success',
                title: '¡Tutorial completado!',
                html: `<p class="mb-0 text-muted" style="font-size:0.9rem">Ya conocés <strong>${config.titulo}</strong>. Podés repetirlo cuando quieras desde el botón <span class="badge text-bg-primary px-2 py-1"><i class="fa-solid fa-graduation-cap me-1"></i>Tutorial</span> abajo a la derecha.</p>`,
                confirmButtonText: 'Comenzar a usar',
                confirmButtonColor: '#10b981',
                timer: 5000,
                timerProgressBar: true
            });
        });

        intro.onexit(function () {
            localStorage.setItem(config.storageKey, 'true');
            $('#btnAyudaGlobal').removeClass('tutorial-fab--new');
        });

        intro.start();
    }

    // =========================================================
    // VERIFICAR PRIMERA VISITA
    // =========================================================

    function verificarPrimeraVisita(moduloKey) {
        const config = tutorialesConfig[moduloKey];
        if (!config) return;

        if (localStorage.getItem(config.storageKey)) return;

        setTimeout(() => {
            const tieneIntros = document.querySelectorAll('[data-intro]').length > 0;
            if (!tieneIntros) {
                localStorage.setItem(config.storageKey, 'true');
                return;
            }

            const featuresList = config.features
                .map(f => `<li class="d-flex align-items-start gap-2 mb-1"><i class="fa-solid fa-circle-check text-success mt-1" style="font-size:0.8rem;flex-shrink:0"></i><span>${f}</span></li>`)
                .join('');

            Swal.fire({
                customClass: {
                    popup: 'tutorial-welcome-popup',
                    confirmButton: 'btn btn-primary px-4',
                    cancelButton: 'btn btn-link text-muted text-decoration-none'
                },
                buttonsStyling: false,
                html: `
                    <div class="text-center mb-3">
                        <div style="width:64px;height:64px;background:linear-gradient(135deg,rgb(var(--bs-primary-rgb)),#7c3aed);border-radius:50%;display:flex;align-items:center;justify-content:center;margin:0 auto 12px;box-shadow:0 8px 24px rgba(var(--bs-primary-rgb),0.4)">
                            <i class="fa-solid ${config.icono} fa-xl text-white"></i>
                        </div>
                        <h5 class="fw-bold mb-1">${config.titulo}</h5>
                        <p class="text-muted mb-0" style="font-size:0.875rem">${config.descripcion}</p>
                    </div>
                    <ul class="list-unstyled text-start small mb-0" style="border-top:1px solid var(--bs-border-color);padding-top:12px;margin-top:4px;">
                        ${featuresList}
                    </ul>
                `,
                showCancelButton: true,
                confirmButtonText: '<i class="fa-solid fa-graduation-cap me-2"></i>Iniciar tutorial',
                cancelButtonText: 'Ahora no'
            }).then(result => {
                if (result.isConfirmed) {
                    iniciarTutorial(moduloKey);
                } else {
                    localStorage.setItem(config.storageKey, 'true');
                    $('#btnAyudaGlobal').removeClass('tutorial-fab--new');
                }
            });
        }, 1200);
    }

    // =========================================================
    // CONFIGURAR BOTÓN FLOTANTE
    // =========================================================

    function configurarBotonAyuda() {
        const moduloActual = detectarModuloActual();
        const $btn = $('#btnAyudaGlobal');

        $btn.removeClass('tutorial-fab--visible tutorial-fab--new tutorial-pulse');

        if (!moduloActual || !tutorialesConfig[moduloActual]) return;

        const tieneIntros = document.querySelectorAll('[data-intro]').length > 0;
        if (!tieneIntros) return;

        const config = tutorialesConfig[moduloActual];
        const tutorialCompletado = localStorage.getItem(config.storageKey);

        if (!tutorialCompletado) {
            $btn.addClass('tutorial-fab--visible tutorial-fab--new tutorial-pulse');
        } else {
            setTimeout(() => {
                $btn.addClass('tutorial-fab--visible tutorial-pulse');
                setTimeout(() => $btn.removeClass('tutorial-pulse'), 7000);
            }, 1500);
        }

        $btn.off('click.tutorial').on('click.tutorial', function () {
            iniciarTutorial(moduloActual);
        });
    }

    // =========================================================
    // INICIALIZAR
    // =========================================================

    function inicializarTutoriales() {
        configurarBotonAyuda();
        const moduloActual = detectarModuloActual();
        if (moduloActual) verificarPrimeraVisita(moduloActual);
    }

    // Reiniciar todos los tutoriales (dev/testing)
    window.resetearTutoriales = function () {
        Object.values(tutorialesConfig).forEach(c => localStorage.removeItem(c.storageKey));
        Swal.fire('Tutoriales reiniciados', 'Recargá la página para verlos de nuevo.', 'info');
    };

    window.mostrarTutorial = function () {
        const moduloActual = detectarModuloActual();
        if (moduloActual) iniciarTutorial(moduloActual);
        else Swal.fire('Sin tutorial', 'No hay tutorial disponible para esta página.', 'info');
    };

    $(document).on('turbo:load', function () {
        inicializarTutoriales();

        const tabHash = window.location.hash;
        if (tabHash) {
            const trigger = document.querySelector(`[data-bs-target="${tabHash}"]`);
            if (trigger) new bootstrap.Tab(trigger).show();
        }
    });

})();
