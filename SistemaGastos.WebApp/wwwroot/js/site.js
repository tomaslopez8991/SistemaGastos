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
            titulo: 'Dashboard Financiero',
            descripcion: 'Tu panel de control con un resumen completo de tu situación financiera.',
            features: ['Saldo total y métricas clave del mes', 'Deuda en tarjetas en tiempo real', 'Accesos rápidos a todos los módulos'],
            stepTitles: ['Panel de Control', 'Métricas Financieras', 'Saldo Total', 'Gastos del Mes', 'Deuda en Tarjetas', 'Accesos Rápidos', 'Atajos del Sistema', 'Proyección', 'Calculadora TC', 'Tareas Pendientes', 'Categorías']
        },
        'transacciones': {
            storageKey: 'transacciones_tutorial_completed',
            icono: 'fa-money-bill-transfer',
            titulo: 'Transacciones',
            descripcion: 'El corazón del sistema: registrá todos tus movimientos financieros diarios.',
            features: ['Crear ingresos y gastos fácilmente', 'Filtrar por fecha, cuenta y categoría', 'Ver el balance neto del mes en tiempo real'],
            stepTitles: ['Módulo de Transacciones', 'Panel de Control', 'Nuevo Movimiento', 'Filtros de Búsqueda', 'Filtro por Fecha', 'Filtro Hasta', 'Filtro por Cuenta', 'Filtro por Categoría', 'Aplicar Filtros', 'Limpiar Filtros', 'Lista de Movimientos']
        },
        'billetera': {
            storageKey: 'billetera_tutorial_completed',
            icono: 'fa-building-columns',
            titulo: 'Mis Cuentas',
            descripcion: 'Gestioná todas tus cuentas bancarias y efectivo en un solo lugar.',
            features: ['Registrar cuentas bancarias y efectivo', 'Ver saldo consolidado por moneda', 'Agregar y editar cuentas'],
            stepTitles: ['Mis Cuentas', 'Panel General', 'Agregar Cuenta', 'Saldo por Moneda', 'Detalle de Cuentas']
        },
        'tareas': {
            storageKey: 'tareas_tutorial_completed',
            icono: 'fa-list-check',
            titulo: 'Tareas Financieras',
            descripcion: 'Organizá tus pendientes financieros para no olvidar ningún pago.',
            features: ['Crear tareas con prioridad (Alta / Media / Baja)', 'Configurar recordatorios por fecha', 'Seguir el historial de tareas completadas'],
            stepTitles: ['Tareas Financieras', 'Panel de Tareas', 'Nueva Tarea', 'Filtrar por Prioridad', 'Vista Kanban', 'Pendientes', 'Gestionar Tarea', 'Completadas', 'Historial']
        },
        'creditcard': {
            storageKey: 'creditcard_tutorial_completed',
            icono: 'fa-credit-card',
            titulo: 'Tarjetas de Crédito',
            descripcion: 'Controlá tus consumos con tarjeta, cuotas y deuda total.',
            features: ['Registrar consumos en cuotas', 'Ver deuda total y distribución', 'Eliminar consumos en lote'],
            stepTitles: ['Tarjetas de Crédito', 'Panel de Control', 'Eliminar en Lote', 'Nuevo Consumo', 'Deuda Total', 'Distribución de Deuda', 'Lista de Consumos']
        },
        'cashflow': {
            storageKey: 'cashflow_tutorial_completed',
            icono: 'fa-timeline',
            titulo: 'Proyección / Cashflow',
            descripcion: 'Proyectá tu liquidez futura y planificá tus finanzas con anticipación.',
            features: ['Planificar ingresos y gastos futuros', 'Ver cashflow proyectado mes a mes', 'Gestionar gastos recurrentes'],
            stepTitles: ['Cashflow Pro']
        },
        'presupuestos': {
            storageKey: 'presupuestos_tutorial_completed',
            icono: 'fa-chart-pie',
            titulo: 'Presupuestos',
            descripcion: 'Definí límites de gasto por categoría y seguí tu cumplimiento en tiempo real.',
            features: ['Definir límites por categoría', 'Ver progreso visual de cada presupuesto', 'Recibir alertas inteligentes al acercarte al límite'],
            stepTitles: ['Presupuestos', 'Panel de Presupuestos', 'Navegar por Meses', 'Nuevo Presupuesto', 'Resumen del Mes', 'Estado Financiero', 'Cifras Clave', 'Progreso del Mes', 'Días Restantes', 'Tarjetas por Categoría', 'Sin Presupuesto Aún', 'Crear desde Cero', 'Copiar del Mes Anterior', 'Tarjeta de Categoría', 'Opciones de Categoría', 'Alerta Inteligente']
        },
        'estadisticas': {
            storageKey: 'estadisticas_tutorial_completed',
            icono: 'fa-chart-line',
            titulo: 'Estadísticas',
            descripcion: 'Analizá tus finanzas con gráficos, tendencias históricas y proyecciones.',
            features: ['Tendencia de ingresos vs gastos (6 meses)', 'Top de categorías de gasto e ingreso', 'Patrimonio neto y proyecciones futuras'],
            stepTitles: ['Dashboard Financiero', 'Panel de Análisis', 'Actualizar Datos', 'Situación Patrimonial', 'Patrimonio Neto', 'Saldo Total', 'Deuda en Tarjetas', 'Actividad del Mes', 'Ingresos del Mes', 'Gastos del Mes', 'Ahorro del Mes', 'Gasto Diario', 'Tendencia Histórica', 'Top Gastos', 'Top Ingresos', 'Distribución de Cuentas', 'Proyecciones', 'Últimas Transacciones']
        },
        'gastosfijos': {
            storageKey: 'gastosfijos_tutorial_completed',
            icono: 'fa-file-invoice-dollar',
            titulo: 'Gastos Fijos',
            descripcion: 'Controlá tus suscripciones y pagos recurrentes mensuales.',
            features: ['Registrar suscripciones y servicios', 'Ver próximos vencimientos', 'Marcar gastos como pagados cada mes'],
            stepTitles: ['Gastos Fijos', 'Panel de Gastos Fijos', 'Nuevo Gasto Fijo', 'Resumen Mensual', 'Total Mensual', 'Pagado Este Mes', 'Pendientes de Pago', 'Próximo Vencimiento', 'Filtros', 'Filtrar por Estado', 'Filtrar por Categoría', 'Aplicar Filtros', 'Lista de Gastos']
        },
        'categorias': {
            storageKey: 'categorias_tutorial_completed',
            icono: 'fa-tags',
            titulo: 'Categorías',
            descripcion: 'Organizá tus movimientos con etiquetas personalizadas.',
            features: ['Crear categorías de ingresos y gastos', 'Elegir ícono y color personalizados', 'Filtrar por tipo de categoría'],
            stepTitles: ['Categorías', 'Panel de Categorías', 'Nueva Categoría', 'Filtrar por Tipo', 'Ver Todas', 'Solo Gastos', 'Solo Ingresos', 'Lista de Categorías']
        },
        'perfil': {
            storageKey: 'perfil_tutorial_completed',
            icono: 'fa-user-gear',
            titulo: 'Mi Perfil',
            descripcion: 'Gestioná tu cuenta, datos personales y configuración de seguridad.',
            features: ['Actualizar tu correo de contacto', 'Cambiar tu contraseña', 'Ver tu rol y estado de cuenta'],
            stepTitles: ['Mi Perfil', 'Tu Identidad', 'Estado de Conexión', 'Tu Rol', 'Secciones del Perfil', 'Pestañas de Configuración', 'Pestaña Datos', 'Pestaña Seguridad', 'Datos Personales', 'Nombre de Usuario', 'Tu Rol', 'Email de Contacto', 'Guardar Email', 'Cambiar Contraseña', 'Nueva Contraseña', 'Confirmar Contraseña', 'Guardar Contraseña']
        }
    };

    // =========================================================
    // DETECTAR MÓDULO ACTUAL
    // =========================================================

    function detectarModuloActual() {
        const path = window.location.pathname.toLowerCase();

        if (path.includes('/transaction/index') ||
            (path.includes('/transaction') && !path.includes('tmp') && !path.includes('credit') && !path.includes('statistics'))) {
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
            showBullets: false,
            exitOnOverlayClick: false,
            disableInteraction: false,
            scrollToElement: true,
            scrollPadding: 80,
            overlayOpacity: 0.72,
            tooltipClass: 'customTooltip',
            highlightClass: 'customHighlight',
            showStepNumbers: false
        });

        // Aplicar títulos de cada paso dinámicamente (sin tocar el HTML)
        if (config.stepTitles && config.stepTitles.length > 0) {
            const elements = Array.from(document.querySelectorAll('[data-intro]'))
                .sort((a, b) => parseInt(a.dataset.step || 0) - parseInt(b.dataset.step || 0));
            elements.forEach((el, idx) => {
                if (config.stepTitles[idx] && !el.hasAttribute('data-title')) {
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
