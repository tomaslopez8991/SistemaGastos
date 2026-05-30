(function () {
    const $wrapper = $('#notif-wrapper');
    if ($wrapper.length === 0) return;

    const apiUrl = $wrapper.data('url') || '/Notifications/GetAll';

    const TYPE_CONFIG = {
        task:        { label: 'Tareas',         icon: 'fa-list-check'       },
        fixedexpense:{ label: 'Gastos Fijos',    icon: 'fa-receipt'          },
        budget:      { label: 'Presupuestos',    icon: 'fa-chart-pie'        },
        balance:     { label: 'Cuentas',         icon: 'fa-building-columns' }
    };

    const SEVERITY_COLOR = {
        danger:  { text: 'text-danger',  bg: 'rgba(220,53,69,0.1)',   dot: '#dc3545' },
        warning: { text: 'text-warning', bg: 'rgba(255,193,7,0.1)',   dot: '#ffc107' },
        info:    { text: 'text-info',    bg: 'rgba(13,202,240,0.1)',  dot: '#0dcaf0' }
    };

    function buildItem(n) {
        const sc = SEVERITY_COLOR[n.severity] || SEVERITY_COLOR.info;
        const tc = TYPE_CONFIG[n.type]        || { icon: 'fa-bell' };
        return `
            <a href="${n.actionUrl}"
               class="dropdown-item d-flex align-items-center gap-2 py-2 px-3 border-bottom"
               style="background:transparent;">
                <div class="flex-shrink-0 d-flex align-items-center justify-content-center rounded-circle"
                     style="width:34px;height:34px;background:${sc.bg};">
                    <i class="fa-solid ${tc.icon} ${sc.text}" style="font-size:0.85rem;"></i>
                </div>
                <div class="flex-grow-1 overflow-hidden">
                    <p class="mb-0 small fw-semibold text-truncate">${n.title}</p>
                    <span class="text-muted text-truncate d-block" style="font-size:0.72rem;">${n.subtitle}</span>
                </div>
                <span class="flex-shrink-0 rounded-circle"
                      style="width:8px;height:8px;background:${sc.dot};"></span>
            </a>`;
    }

    function buildEmptyState() {
        return `
            <div class="text-center py-4 px-3 text-muted">
                <i class="fa-regular fa-bell-slash fa-2x d-block mb-2 opacity-40"></i>
                <small>Todo en orden, sin alertas activas</small>
            </div>`;
    }

    function renderNotifications(data) {
        const $list  = $('#notif-list');
        const $badge = $('#notif-badge');

        if (!data.notifications || data.notifications.length === 0) {
            $badge.hide();
            $list.html(buildEmptyState());
            return;
        }

        $badge.text(data.count).show();

        // Agrupar por tipo manteniendo el orden de severidad
        const groups = {};
        data.notifications.forEach(n => {
            if (!groups[n.type]) groups[n.type] = [];
            groups[n.type].push(n);
        });

        let html = '';
        for (const [type, items] of Object.entries(groups)) {
            const cfg = TYPE_CONFIG[type] || { label: type, icon: 'fa-bell' };
            html += `<div class="notif-section-header">${cfg.label} (${items.length})</div>`;
            html += items.map(buildItem).join('');
        }

        $list.html(html);
    }

    function loadNotifications() {
        $.get(apiUrl, renderNotifications)
         .fail(function () {
             $('#notif-list').html(
                 '<div class="text-center py-3 text-muted small">No disponible</div>'
             );
         });
    }

    // Recargar al abrir el dropdown
    $wrapper.on('show.bs.dropdown', loadNotifications);

    // Carga inicial solo para el badge
    $(document).ready(function () {
        $.get(apiUrl, function (data) {
            if (data.count > 0) $('#notif-badge').text(data.count).show();

            // Modal de bienvenida post-login
            if (window._showLoginNotifications && data.count > 0) {
                showLoginModal(data);
            }
        });
    });

    // ── Modal de briefing post-login ──────────────────────────────────────
    function showLoginModal(data) {
        const notifs = data.notifications || [];
        const urlAll = (apiUrl || '/Notifications/GetAll').replace('GetAll', 'Index');

        const counts = { danger: 0, warning: 0, info: 0 };
        notifs.forEach(n => { if (counts[n.severity] !== undefined) counts[n.severity]++; });

        const SEV_META = {
            danger:  { label: 'Crítica',   dotColor: '#dc3545' },
            warning: { label: 'Atención',  dotColor: '#ffc107' },
            info:    { label: 'Info',      dotColor: '#0dcaf0' }
        };

        const TYPE_ICON = {
            task:         'fa-list-check',
            fixedexpense: 'fa-receipt',
            budget:       'fa-chart-pie',
            balance:      'fa-building-columns'
        };

        // Resumen de severidad
        let summaryHtml = '<div class="d-flex justify-content-center gap-3 mb-3">';
        Object.entries(counts).forEach(([sev, count]) => {
            if (count === 0) return;
            const m = SEV_META[sev];
            summaryHtml += `
                <div class="text-center px-3 py-2 rounded-3" style="background:${m.dotColor}18; min-width:80px;">
                    <div class="fw-bold fs-5" style="color:${m.dotColor};">${count}</div>
                    <div class="small text-muted">${m.label}${count > 1 ? 's' : ''}</div>
                </div>`;
        });
        summaryHtml += '</div>';

        // Top items (máx 4)
        const topItems = notifs.slice(0, 4);
        let itemsHtml = '<div class="text-start" style="max-height:200px;overflow-y:auto;">';
        topItems.forEach(n => {
            const dot = SEV_META[n.severity]?.dotColor || '#6c757d';
            const icon = TYPE_ICON[n.type] || 'fa-bell';
            itemsHtml += `
                <div class="d-flex align-items-center gap-2 py-2 border-bottom" style="border-color:rgba(255,255,255,0.08)!important;">
                    <div class="rounded-circle d-flex align-items-center justify-content-center flex-shrink-0"
                         style="width:32px;height:32px;background:${dot}20;">
                        <i class="fa-solid ${icon}" style="color:${dot};font-size:0.8rem;"></i>
                    </div>
                    <div class="flex-grow-1 overflow-hidden">
                        <p class="mb-0 small fw-semibold text-truncate">${n.title}</p>
                        <span class="text-muted" style="font-size:0.72rem;">${n.subtitle}</span>
                    </div>
                    <span class="flex-shrink-0 rounded-circle" style="width:7px;height:7px;background:${dot};"></span>
                </div>`;
        });
        if (notifs.length > 4) {
            itemsHtml += `<p class="text-center text-muted small mt-2 mb-0">y ${notifs.length - 4} más...</p>`;
        }
        itemsHtml += '</div>';

        Swal.fire({
            title: `<i class="fa-regular fa-bell me-2 text-warning"></i>${data.count} alerta${data.count > 1 ? 's' : ''} activa${data.count > 1 ? 's' : ''}`,
            html: summaryHtml + itemsHtml,
            width: '460px',
            showCancelButton: true,
            confirmButtonText: '<i class="fa-solid fa-arrow-right me-1"></i>Ver todas',
            cancelButtonText: 'Revisar después',
            confirmButtonColor: '#0d6efd',
            reverseButtons: true,
            focusCancel: true
        }).then(result => {
            if (result.isConfirmed) window.location.href = urlAll;
        });
    }
})();
