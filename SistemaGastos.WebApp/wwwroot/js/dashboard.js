const USAGE_KEY = 'sg_module_usage';

const MODULE_META = {
    'Proyección':     { icon: 'fa-calculator',         color: 'text-info',      href: '/TmpTransaction' },
    'Tarjetas':       { icon: 'fa-credit-card',         color: 'text-primary',   href: '/CreditCardTransaction' },
    'Transacciones':  { icon: 'fa-money-bill-transfer', color: 'text-success',   href: '/Transaction' },
    'Tareas':         { icon: 'fa-list-check',          color: 'text-warning',   href: '/Todo' },
    'Estadísticas':   { icon: 'fa-chart-line',          color: 'text-info',      href: '/Transaction/Statistics' },
    'Cuentas':        { icon: 'fa-people-group',        color: 'text-secondary', href: '/Person' },
    'Mis Cuentas':    { icon: 'fa-building-columns',    color: 'text-primary',   href: '/Account' },
    'Intereses':      { icon: 'fa-percent',             color: 'text-info',      href: '/AccountInterest' },
    'Categorías':     { icon: 'fa-tags',                color: 'text-secondary', href: '/Category' },
};

function dashboardGetUsage() {
    try { return JSON.parse(localStorage.getItem(USAGE_KEY) || '{}'); }
    catch { return {}; }
}

function dashboardSaveUsage(data) {
    localStorage.setItem(USAGE_KEY, JSON.stringify(data));
}

function dashboardResetUsage() {
    localStorage.removeItem(USAGE_KEY);
    renderMostUsed();
}

function dashboardTrackClick(moduleName) {
    const data = dashboardGetUsage();
    data[moduleName] = (data[moduleName] || 0) + 1;
    dashboardSaveUsage(data);
}

function renderMostUsed() {
    const container = document.getElementById('most-used-modules');
    if (!container) return;

    const data = dashboardGetUsage();
    const sorted = Object.entries(data)
        .sort((a, b) => b[1] - a[1])
        .slice(0, 4);

    if (sorted.length === 0) {
        container.innerHTML = `
            <div class="col-12">
                <div class="card card-glass-modern border-0">
                    <div class="card-body p-4 text-center text-muted">
                        <i class="fa-solid fa-chart-bar fa-2x mb-2 d-block opacity-25"></i>
                        <small>Todavía no hay datos. Navegá por los módulos y los más usados aparecerán aquí.</small>
                    </div>
                </div>
            </div>`;
        return;
    }

    const maxCount = sorted[0][1];

    container.innerHTML = sorted.map(([name, count], i) => {
        const meta = MODULE_META[name] || { icon: 'fa-star', color: 'text-muted', href: '#' };
        const pct = Math.round((count / maxCount) * 100);
        const rankColors = ['text-warning', 'text-secondary', 'text-danger', 'text-muted'];
        const rankIcons = ['fa-trophy', 'fa-medal', 'fa-award', 'fa-star'];

        return `
        <div class="col-6 col-md-3">
            <a href="${meta.href}" class="card card-glass-modern border-0 h-100 text-decoration-none module-link" data-module="${name}">
                <div class="card-body p-3">
                    <div class="d-flex align-items-center justify-content-between mb-2">
                        <i class="fa-solid ${meta.icon} fa-lg ${meta.color}"></i>
                        <i class="fa-solid ${rankIcons[i]} ${rankColors[i]}" style="font-size:.85rem" title="Puesto ${i+1}"></i>
                    </div>
                    <p class="fw-bold mb-1 small">${name}</p>
                    <div class="progress mb-1" style="height:4px;background:rgba(255,255,255,.08)">
                        <div class="progress-bar ${meta.color.replace('text-','bg-')}" style="width:${pct}%"></div>
                    </div>
                    <small class="text-muted">${count} acceso${count !== 1 ? 's' : ''}</small>
                </div>
            </a>
        </div>`;
    }).join('');

    // Re-attach click tracking on newly rendered cards
    attachModuleLinks();
}

function attachModuleLinks() {
    document.querySelectorAll('.module-link').forEach(el => {
        el.addEventListener('click', function () {
            const name = this.dataset.module;
            if (name) dashboardTrackClick(name);
        }, { once: true });
    });
}

// Also track clicks on the sidebar nav items
function attachSidebarTracking() {
    document.querySelectorAll('#sidebar-wrapper .nav-item').forEach(el => {
        const text = el.querySelector('.nav-text')?.textContent?.trim();
        if (text && MODULE_META[text]) {
            el.addEventListener('click', () => dashboardTrackClick(text));
        }
    });
}

document.addEventListener('DOMContentLoaded', () => {
    renderMostUsed();
    attachModuleLinks();
    attachSidebarTracking();
});
