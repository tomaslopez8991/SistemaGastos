(function () {
    'use strict';

    const container   = document.getElementById('persons-container');
    const URL_LIST    = container.dataset.urlList;
    const URL_SAVE    = container.dataset.urlSave;
    const URL_DELETE  = container.dataset.urlDelete;
    const URL_ACCOUNTS = container.dataset.urlAccounts;

    const modal       = new bootstrap.Modal(document.getElementById('modalPersona'));
    const modalTitle  = document.getElementById('modalPersonaTitle');
    const inputID     = document.getElementById('personID');
    const inputName   = document.getElementById('personName');
    const antiForgery = document.querySelector('input[name="__RequestVerificationToken"]');

    // ── Cargar cuentas ────────────────────────────────────────────
    async function loadAccounts() {
        const grid = document.getElementById('accounts-grid');
        grid.innerHTML = `<div class="col-12 text-center py-4 text-muted">
            <div class="spinner-border spinner-border-sm text-primary" role="status"></div>
            <span class="ms-2 small">Calculando saldos...</span></div>`;
        try {
            const res = await fetch(URL_ACCOUNTS);
            const json = await res.json();
            const accounts = json.data || [];

            if (accounts.length === 0) {
                grid.innerHTML = `
                    <div class="col-12 text-center py-5 text-muted">
                        <i class="fa-solid fa-people-group fa-3x mb-3 opacity-25"></i>
                        <p class="mb-1">No hay personas con gastos atribuidos aún.</p>
                        <p class="small">Creá personas en el tab "Personas" y asignales transacciones o gastos.</p>
                    </div>`;
                return;
            }

            grid.innerHTML = accounts.map(a => buildAccountCard(a)).join('');

            grid.querySelectorAll('.btn-expand-account').forEach(btn => {
                btn.addEventListener('click', () => {
                    const collapseEl = document.getElementById(btn.dataset.target);
                    const bsCollapse = bootstrap.Collapse.getOrCreateInstance(collapseEl);
                    bsCollapse.toggle();
                    const icon = btn.querySelector('i');
                    collapseEl.addEventListener('shown.bs.collapse',  () => icon.className = 'fa-solid fa-chevron-up',  { once: true });
                    collapseEl.addEventListener('hidden.bs.collapse', () => icon.className = 'fa-solid fa-chevron-down', { once: true });
                });
            });
        } catch (e) {
            console.error(e);
            grid.innerHTML = `<div class="col-12 text-danger small p-3">Error al cargar cuentas.</div>`;
        }
    }

    function buildAccountCard(account) {
        const collapseID = `collapse-person-${account.personID}`;
        const isPositive = account.totalOwed > 0;
        const colorClass = isPositive ? 'text-danger' : 'text-success';
        const icon = isPositive ? 'fa-arrow-trend-up' : 'fa-check-circle';

        const itemsHtml = account.items.length === 0
            ? `<p class="text-muted small mb-0">Sin movimientos registrados.</p>`
            : `<div class="table-responsive">
                <table class="table table-sm table-hover mb-0" style="table-layout:fixed; width:100%;">
                  <colgroup>
                    <col style="width:80px">
                    <col style="width:auto">
                    <col style="width:110px">
                    <col style="width:70px">
                    <col style="width:90px">
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
                <div class="d-flex align-items-center gap-2">
                  <div class="rounded-circle bg-primary bg-opacity-10 d-flex align-items-center justify-content-center flex-shrink-0"
                       style="width:40px;height:40px;">
                    <i class="fa-solid fa-user text-primary"></i>
                  </div>
                  <div style="min-width:0;">
                    <div class="fw-semibold text-truncate">${escHtml(account.personName)}</div>
                    <div class="small text-muted">${account.items.length} movimiento(s)</div>
                  </div>
                </div>
                <button class="btn btn-sm btn-light flex-shrink-0 ms-2 btn-expand-account"
                        data-target="${collapseID}" title="Ver detalle">
                  <i class="fa-solid fa-chevron-down"></i>
                </button>
              </div>
              <div class="d-flex justify-content-between align-items-center p-3 rounded bg-body-tertiary">
                <span class="small text-muted">Saldo adeudado</span>
                <span class="fw-bold fs-5 ${colorClass}">
                  <i class="fa-solid ${icon} me-1 small"></i>${account.totalOwedFmt}
                </span>
              </div>
            </div>
            <div class="collapse" id="${collapseID}">
              <div class="card-body pt-2 border-top">
                ${itemsHtml}
              </div>
            </div>
          </div>
        </div>`;
    }

    function typeBadge(type) {
        switch (type) {
            case 'Transaction':  return 'bg-success bg-opacity-10 text-success';
            case 'CreditCard':   return 'bg-warning bg-opacity-10 text-warning';
            case 'FixedExpense': return 'bg-info bg-opacity-10 text-info';
            default:             return 'bg-secondary bg-opacity-10 text-secondary';
        }
    }

    function escHtml(str) {
        return String(str ?? '')
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;');
    }

    // ── Cargar lista de personas ──────────────────────────────────
    async function loadPersons() {
        const grid = document.getElementById('persons-grid');
        try {
            const res = await fetch(URL_LIST);
            const json = await res.json();
            const persons = json.data || [];

            if (persons.length === 0) {
                grid.innerHTML = `
                    <div class="text-center py-5 text-muted">
                        <i class="fa-solid fa-user-slash fa-3x mb-3 opacity-25"></i>
                        <p>No hay personas creadas aún.</p>
                        <p class="small">Usá el botón "Nueva Persona" para agregar.</p>
                    </div>`;
                return;
            }

            grid.innerHTML = `
            <table class="table table-hover align-middle">
              <thead>
                <tr>
                  <th>Nombre</th>
                  <th class="text-end">Acciones</th>
                </tr>
              </thead>
              <tbody>
                ${persons.map(p => `
                  <tr>
                    <td>
                      <i class="fa-solid fa-user text-primary me-2"></i>
                      <span class="fw-semibold">${escHtml(p.name)}</span>
                    </td>
                    <td class="text-end">
                      <button class="btn btn-sm btn-outline-secondary me-1 btn-edit-person"
                              data-id="${p.id}" data-name="${escHtml(p.name)}" title="Editar">
                        <i class="fas fa-edit"></i>
                      </button>
                      <button class="btn btn-sm btn-outline-danger btn-delete-person"
                              data-id="${p.id}" data-name="${escHtml(p.name)}" title="Eliminar">
                        <i class="fas fa-trash"></i>
                      </button>
                    </td>
                  </tr>`).join('')}
              </tbody>
            </table>`;

            grid.querySelectorAll('.btn-edit-person').forEach(btn =>
                btn.addEventListener('click', () => openModal(btn.dataset.id, btn.dataset.name)));

            grid.querySelectorAll('.btn-delete-person').forEach(btn =>
                btn.addEventListener('click', () => deletePerson(btn.dataset.id, btn.dataset.name)));

        } catch (e) {
            console.error(e);
            grid.innerHTML = `<div class="text-danger small">Error al cargar personas.</div>`;
        }
    }

    // ── Modal ────────────────────────────────────────────────────
    function openModal(id = 0, name = '') {
        inputID.value = id;
        inputName.value = name;
        inputName.classList.remove('is-invalid');
        const isEdit = parseInt(id) > 0;
        modalTitle.innerHTML = isEdit
            ? '<i class="fa-solid fa-user-pen me-2"></i>Editar Persona'
            : '<i class="fa-solid fa-user-plus me-2"></i>Nueva Persona';
        modal.show();
        setTimeout(() => inputName.focus(), 300);
    }

    document.getElementById('btnNuevaPersona').addEventListener('click', () => openModal());

    document.getElementById('btnGuardarPersona').addEventListener('click', async () => {
        const name = inputName.value.trim();
        if (!name) {
            inputName.classList.add('is-invalid');
            inputName.focus();
            return;
        }
        inputName.classList.remove('is-invalid');

        const btn = document.getElementById('btnGuardarPersona');
        btn.disabled = true;
        btn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span>Guardando...';

        try {
            const dto = { id: parseInt(inputID.value) || 0, name };
            const res = await fetch(URL_SAVE, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': antiForgery?.value ?? ''
                },
                body: JSON.stringify(dto)
            });

            if (!res.ok) throw new Error(`HTTP ${res.status}`);

            const json = await res.json();

            if (json.success) {                          // ← propiedad correcta del wrapper
                modal.hide();
                await Swal.fire({
                    icon: 'success',
                    title: json.message,
                    timer: 1500,
                    showConfirmButton: false
                });
                // Recargar ambos tabs dinámicamente
                await Promise.all([loadAccounts(), loadPersons()]);
            } else {
                Swal.fire({ icon: 'error', title: 'Error', text: json.message || 'No se pudo guardar.' });
            }
        } catch (e) {
            console.error(e);
            Swal.fire({ icon: 'error', title: 'Error', text: 'Error de conexión al guardar.' });
        } finally {
            btn.disabled = false;
            btn.innerHTML = '<i class="fas fa-save me-1"></i>Guardar';
        }
    });

    // ── Eliminar ─────────────────────────────────────────────────
    async function deletePerson(id, name) {
        const confirm = await Swal.fire({
            icon: 'warning',
            title: `¿Eliminar "${name}"?`,
            text: 'Se desvinculará de todos sus movimientos. Esta acción no se puede deshacer.',
            showCancelButton: true,
            confirmButtonText: 'Sí, eliminar',
            cancelButtonText: 'Cancelar',
            confirmButtonColor: '#dc3545'
        });
        if (!confirm.isConfirmed) return;

        try {
            const res = await fetch(`${URL_DELETE}/${id}`, {
                method: 'DELETE',
                headers: { 'RequestVerificationToken': antiForgery?.value ?? '' }
            });

            if (!res.ok) throw new Error(`HTTP ${res.status}`);

            const json = await res.json();
            if (json.success) {                          // ← propiedad correcta
                await Swal.fire({ icon: 'success', title: json.message, timer: 1500, showConfirmButton: false });
                await Promise.all([loadPersons(), loadAccounts()]);
            } else {
                Swal.fire({ icon: 'error', title: 'Error', text: json.message });
            }
        } catch (e) {
            console.error(e);
            Swal.fire({ icon: 'error', title: 'Error', text: 'Error de conexión al eliminar.' });
        }
    }

    // ── Tab switch ───────────────────────────────────────────────
    document.getElementById('personas-tab').addEventListener('shown.bs.tab', loadPersons);
    document.getElementById('cuentas-tab').addEventListener('shown.bs.tab', loadAccounts);

    // ── Init ─────────────────────────────────────────────────────
    loadAccounts();
})();
