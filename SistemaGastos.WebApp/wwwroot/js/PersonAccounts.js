/**
 * PersonAccounts.js — Cuentas por persona integradas en la pantalla de Proyección.
 * Maneja el tab "Cuentas" y la card "A cobrar" del dashboard mensual.
 */
(function () {
    'use strict';

    const container = document.getElementById('planning-container');
    if (!container) return;

    const URL_ACCOUNTS         = container.dataset.urlPersonAccounts;
    const URL_LIST             = container.dataset.urlPersonList;
    const URL_SAVE             = container.dataset.urlPersonSave;
    const URL_DELETE           = container.dataset.urlPersonDelete;
    const URL_PAYMENT_ACCOUNTS = container.dataset.urlPersonPaymentAccounts;
    const URL_REGISTER_PAYMENT = container.dataset.urlPersonRegisterPayment;
    const antiForgery          = () => document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? '';

    // ── Estado ───────────────────────────────────────────────────
    let accountsLoaded = false;
    let activeYear  = new Date().getFullYear();
    let activeMonth = new Date().getMonth() + 1;

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

    // ── Función pública: permite recargar desde TmpTransaction.js ─
    window.cargarCuentas = function(year, month) {
        activeYear  = parseInt(year)  || activeYear;
        activeMonth = parseInt(month) || activeMonth;
        accountsLoaded = false;
        loadAccounts();
    };

    // ── Cargar cuentas ───────────────────────────────────────────
    async function loadAccounts() {
        const grid = document.getElementById('person-accounts-grid');
        if (!grid) return;

        grid.innerHTML = `<div class="col-12 text-center py-4 text-muted">
            <div class="spinner-border spinner-border-sm text-success" role="status"></div>
            <span class="ms-2 small">Calculando cuentas...</span></div>`;

        try {
            const res = await fetch(`${URL_ACCOUNTS}?year=${activeYear}&month=${activeMonth}`);
            const json = await res.json();
            const accounts = json.data || [];

            updateDashboardCard(accounts);
            updateCuentasBadge(accounts);

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

            grid.querySelectorAll('.btn-collect-pa').forEach(btn => {
                btn.addEventListener('click', () => collectFromPerson(
                    parseInt(btn.dataset.personId),
                    btn.dataset.personName,
                    parseFloat(btn.dataset.totalOwed)
                ));
            });

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

    // ── Badge tab Cuentas ────────────────────────────────────────
    function updateCuentasBadge(accounts) {
        const total = accounts.length;
        const $b = document.getElementById('badge-cuentas');
        if ($b) {
            if (total > 0) { $b.textContent = total; $b.style.display = ''; }
            else $b.style.display = 'none';
        }
    }

    // ── Card "A cobrar" eliminada — función conservada como no-op ─
    function updateDashboardCard(_accounts) { /* card removida */ }

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
                <div>
                    <div class="small text-muted">Saldo adeudado</div>
                    <span class="fw-bold fs-5 ${color}">
                      <i class="fa-solid ${icon} me-1 small"></i>${account.totalOwedFmt}
                    </span>
                </div>
                ${isPos ? `<button class="btn btn-success btn-sm fw-bold btn-collect-pa"
                    data-person-id="${account.personID}"
                    data-person-name="${escHtml(account.personName)}"
                    data-total-owed="${account.totalOwed}">
                    <i class="fas fa-hand-holding-dollar me-1"></i>Cobrar
                  </button>` : ''}
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

    // ── Cobrar a persona ─────────────────────────────────────────
    async function collectFromPerson(personId, personName, totalOwed) {
        const fmt = new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS' });

        // Cargar cuentas disponibles
        let accounts = [];
        try {
            const res = await fetch(URL_PAYMENT_ACCOUNTS);
            const json = await res.json();
            accounts = json.data || [];
        } catch { /* silencioso */ }

        if (!accounts.length) {
            Swal.fire('Sin cuentas', 'No hay cuentas disponibles para acreditar el cobro.', 'warning');
            return;
        }

        const accountOptions = accounts.map(a =>
            `<option value="${a.id}">${a.name} (${a.currency})</option>`
        ).join('');

        const { value: formValues, isConfirmed } = await Swal.fire({
            title: `Cobrar a ${personName}`,
            html: `
                <div class="text-start">
                    <div class="mb-3">
                        <label class="form-label small fw-semibold">Monto a cobrar</label>
                        <div class="input-group">
                            <span class="input-group-text">$</span>
                            <input id="swal-collect-amount" type="number" step="0.01"
                                class="form-control text-end fw-bold"
                                value="${totalOwed.toFixed(2)}" min="0.01" />
                        </div>
                        <small class="text-muted">Total adeudado: <strong>${fmt.format(totalOwed)}</strong></small>
                    </div>
                    <div class="mb-2">
                        <label class="form-label small fw-semibold">Acreditar en cuenta</label>
                        <select id="swal-collect-account" class="form-select">
                            ${accountOptions}
                        </select>
                    </div>
                </div>`,
            showCancelButton: true,
            confirmButtonText: '<i class="fas fa-check me-1"></i>Confirmar cobro',
            confirmButtonColor: '#198754',
            cancelButtonText: 'Cancelar',
            preConfirm: () => ({
                amount:    parseFloat(document.getElementById('swal-collect-amount').value) || 0,
                accountId: parseInt(document.getElementById('swal-collect-account').value) || 0
            })
        });

        if (!isConfirmed || !formValues || formValues.amount <= 0) return;

        try {
            const res = await fetch(URL_REGISTER_PAYMENT, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': antiForgery()
                },
                body: JSON.stringify({
                    personID:  personId,
                    accountID: formValues.accountId,
                    amount:    formValues.amount
                })
            });
            const json = await res.json();
            if (json.success) {
                Swal.fire({ title: 'Cobro registrado', icon: 'success', timer: 1500, showConfirmButton: false });
                accountsLoaded = false;
                loadAccounts();
                if (window.reloadCashflowBalances) window.reloadCashflowBalances();
            } else {
                Swal.fire('Error', json.message || 'No se pudo registrar el cobro', 'error');
            }
        } catch (e) {
            Swal.fire('Error', 'Error de comunicación', 'error');
        }
    }

    // ── TmpTransaction.js publica el mes seleccionado vía evento.
    //    Cuando el tab Cuentas se abre por primera vez, cargamos las cuentas.
    // ── Tab switch ───────────────────────────────────────────────
    const cuentasTab = document.getElementById('cuentas-tab');
    if (cuentasTab) {
        // Recargar siempre al abrir el tab (el mes puede haber cambiado)
        cuentasTab.addEventListener('shown.bs.tab', () => loadAccounts());
    }

    // ── La card del dashboard se actualiza cuando se cargan los balances.
    //    TmpTransaction.js dispara 'balances:loaded' con el mes activo.
    //    Si ese evento no existe, cargamos las cuentas al inicio.
    // ─────────────────────────────────────────────────────────────
    // Carga inicial (para el badge del tab y el cálculo inicial)
    loadAccounts();
})();
