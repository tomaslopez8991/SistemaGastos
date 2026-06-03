/**
 * PersonAccounts.js — Cuentas por persona integradas en la pantalla de Proyección.
 * Maneja el tab "Cuentas" y la card "A cobrar" del dashboard mensual.
 */
(function () {
    'use strict';

    const container = document.getElementById('planning-container');
    if (!container) return;

    const URL_ACCOUNTS  = container.dataset.urlPersonAccounts;
    const URL_LIST      = container.dataset.urlPersonList;
    const URL_SAVE      = container.dataset.urlPersonSave;
    const URL_DELETE    = container.dataset.urlPersonDelete;
    const antiForgery   = () => document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? '';

    // ── Estado ───────────────────────────────────────────────────
    let accountsLoaded = false;

    // ── Helpers ─────────────────────────────────────────────────
    function escHtml(str) {
        return String(str ?? '')
            .replace(/&/g, '&amp;').replace(/</g, '&lt;')
            .replace(/>/g, '&gt;').replace(/"/g, '&quot;');
    }

    function typeBadge(type) {
        switch (type) {
            case 'Transaction':  return 'bg-success bg-opacity-10 text-success';
            case 'CreditCard':   return 'bg-warning bg-opacity-10 text-warning';
            case 'FixedExpense': return 'bg-info bg-opacity-10 text-info';
            default:             return 'bg-secondary bg-opacity-10 text-secondary';
        }
    }

    // ── Cargar cuentas ───────────────────────────────────────────
    async function loadAccounts() {
        const grid = document.getElementById('person-accounts-grid');
        if (!grid) return;

        grid.innerHTML = `<div class="col-12 text-center py-4 text-muted">
            <div class="spinner-border spinner-border-sm text-success" role="status"></div>
            <span class="ms-2 small">Calculando cuentas...</span></div>`;

        try {
            const res = await fetch(URL_ACCOUNTS);
            const json = await res.json();
            const accounts = json.data || [];

            updateDashboardCard(accounts);

            if (accounts.length === 0) {
                grid.innerHTML = `
                    <div class="col-12 text-center py-5 text-muted">
                        <i class="fa-solid fa-people-group fa-3x mb-3 opacity-25"></i>
                        <p class="mb-1 fw-semibold">Sin cuentas activas</p>
                        <p class="small">Creá personas en <a href="${container.dataset.urlPersonList ? '#' : '/Person'}" class="text-success">Administrar personas</a> y asignales gastos.</p>
                    </div>`;
                return;
            }

            grid.innerHTML = accounts.map(a => buildCard(a)).join('');

            grid.querySelectorAll('.btn-expand-pa').forEach(btn => {
                btn.addEventListener('click', () => {
                    const el = document.getElementById(btn.dataset.target);
                    const bsc = bootstrap.Collapse.getOrCreateInstance(el);
                    bsc.toggle();
                    const icon = btn.querySelector('i');
                    el.addEventListener('shown.bs.collapse',  () => icon.className = 'fa-solid fa-chevron-up',  { once: true });
                    el.addEventListener('hidden.bs.collapse', () => icon.className = 'fa-solid fa-chevron-down', { once: true });
                });
            });

            accountsLoaded = true;
        } catch (e) {
            console.error(e);
            grid.innerHTML = `<div class="col-12 text-danger small p-3">Error al cargar cuentas de personas.</div>`;
        }
    }

    // ── Card de resumen "A cobrar" en el dashboard mensual ───────
    function updateDashboardCard(accounts) {
        const card = document.getElementById('dash-persons-card');
        const val  = document.getElementById('dash-persons-receivable');
        if (!card || !val) return;

        const total = accounts.reduce((s, a) => s + a.totalOwed, 0);
        if (total > 0) {
            val.textContent = new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS' }).format(total);
            card.style.display = '';
        } else {
            card.style.display = 'none';
        }
    }

    // ── Construir card de persona ────────────────────────────────
    function buildCard(account) {
        const id = `pa-collapse-${account.personID}`;
        const isPos = account.totalOwed > 0;
        const color = isPos ? 'text-danger' : 'text-success';
        const icon  = isPos ? 'fa-arrow-trend-up' : 'fa-check-circle';

        const itemsHtml = account.items.length === 0
            ? `<p class="text-muted small mb-0">Sin movimientos registrados.</p>`
            : `<div class="table-responsive">
                <table class="table table-sm table-hover mb-0" style="table-layout:fixed;width:100%;">
                  <colgroup>
                    <col style="width:80px"><col style="width:auto">
                    <col style="width:110px"><col style="width:55px"><col style="width:95px">
                  </colgroup>
                  <thead>
                    <tr>
                      <th class="text-muted fw-normal small">Fecha</th>
                      <th class="text-muted fw-normal small">Descripción</th>
                      <th class="text-muted fw-normal small">Tipo</th>
                      <th class="text-muted fw-normal small text-end">%</th>
                      <th class="text-muted fw-normal small text-end">Monto</th>
                    </tr>
                  </thead>
                  <tbody>
                    ${account.items.map(i => `
                      <tr>
                        <td class="small text-muted text-nowrap">${i.dateFmt}</td>
                        <td class="small" style="overflow:hidden;text-overflow:ellipsis;white-space:nowrap;"
                            title="${escHtml(i.description)}">${escHtml(i.description)}</td>
                        <td style="overflow:hidden;text-overflow:ellipsis;white-space:nowrap;">
                          <span class="badge ${typeBadge(i.type)}" style="font-size:10px;">${i.typeLabel}</span>
                        </td>
                        <td class="small text-end text-muted text-nowrap">${i.percentage}%</td>
                        <td class="small text-end fw-semibold text-nowrap">${i.amountFmt}</td>
                      </tr>`).join('')}
                  </tbody>
                </table>
              </div>`;

        return `
        <div class="col-md-6 col-xl-4">
          <div class="card shadow-sm border-0 h-100">
            <div class="card-body pb-2">
              <div class="d-flex justify-content-between align-items-start mb-3">
                <div class="d-flex align-items-center gap-2" style="min-width:0;">
                  <div class="rounded-circle bg-success bg-opacity-10 d-flex align-items-center justify-content-center flex-shrink-0"
                       style="width:40px;height:40px;">
                    <i class="fa-solid fa-user text-success"></i>
                  </div>
                  <div style="min-width:0;">
                    <div class="fw-semibold text-truncate">${escHtml(account.personName)}</div>
                    <div class="small text-muted">${account.items.length} movimiento(s)</div>
                  </div>
                </div>
                <button class="btn btn-sm btn-light flex-shrink-0 ms-2 btn-expand-pa"
                        data-target="${id}" title="Ver detalle">
                  <i class="fa-solid fa-chevron-down"></i>
                </button>
              </div>
              <div class="d-flex justify-content-between align-items-center p-3 rounded bg-body-tertiary">
                <span class="small text-muted">Saldo adeudado</span>
                <span class="fw-bold fs-5 ${color}">
                  <i class="fa-solid ${icon} me-1 small"></i>${account.totalOwedFmt}
                </span>
              </div>
            </div>
            <div class="collapse" id="${id}">
              <div class="card-body pt-2 border-top">
                ${itemsHtml}
              </div>
            </div>
          </div>
        </div>`;
    }

    // ── TmpTransaction.js publica el mes seleccionado vía evento.
    //    Cuando el tab Cuentas se abre por primera vez, cargamos las cuentas.
    // ── Tab switch ───────────────────────────────────────────────
    const cuentasTab = document.getElementById('cuentas-tab');
    if (cuentasTab) {
        cuentasTab.addEventListener('shown.bs.tab', () => {
            if (!accountsLoaded) loadAccounts();
        });
    }

    // ── La card del dashboard se actualiza cuando se cargan los balances.
    //    TmpTransaction.js dispara 'balances:loaded' con el mes activo.
    //    Si ese evento no existe, cargamos las cuentas al inicio.
    // ─────────────────────────────────────────────────────────────
    // Cargar inmediatamente para que la card A cobrar esté disponible
    // desde el primer momento (independiente del tab Cuentas)
    loadAccounts();
})();
