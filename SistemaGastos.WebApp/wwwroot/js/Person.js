(function () {
    'use strict';

    const container = document.getElementById('persons-container');
    const URL_LIST    = container.dataset.urlList;
    const URL_SAVE    = container.dataset.urlSave;
    const URL_DELETE  = container.dataset.urlDelete;
    const URL_ACCOUNTS = container.dataset.urlAccounts;

    const modal        = new bootstrap.Modal(document.getElementById('modalPersona'));
    const modalTitle   = document.getElementById('modalPersonaTitle');
    const inputID      = document.getElementById('personID');
    const inputName    = document.getElementById('personName');
    const antiForgery  = document.querySelector('input[name="__RequestVerificationToken"]');

    // ── Cargar cuentas ────────────────────────────────────────────
    async function loadAccounts() {
        const grid = document.getElementById('accounts-grid');
        try {
            const res = await fetch(URL_ACCOUNTS);
            const json = await res.json();
            const accounts = json.data || [];

            if (accounts.length === 0) {
                grid.innerHTML = `
                    <div class="col-12 text-center py-5 text-muted">
                        <i class="fa-solid fa-people-group fa-3x mb-3 opacity-25"></i>
                        <p class="mb-1">No hay personas con gastos atribuidos.</p>
                        <p class="small">Creá personas y asignales transacciones o gastos.</p>
                    </div>`;
                return;
            }

            grid.innerHTML = accounts.map(a => buildAccountCard(a)).join('');

            grid.querySelectorAll('.btn-expand-account').forEach(btn => {
                btn.addEventListener('click', () => {
                    const collapseID = btn.dataset.target;
                    const el = document.getElementById(collapseID);
                    const bsCollapse = bootstrap.Collapse.getOrCreateInstance(el);
                    bsCollapse.toggle();
                    const icon = btn.querySelector('i');
                    el.addEventListener('shown.bs.collapse',  () => icon.className = 'fa-solid fa-chevron-up');
                    el.addEventListener('hidden.bs.collapse', () => icon.className = 'fa-solid fa-chevron-down');
                });
            });
        } catch (e) {
            grid.innerHTML = `<div class="col-12 text-danger">Error al cargar cuentas.</div>`;
        }
    }

    function buildAccountCard(account) {
        const collapseID = `collapse-person-${account.personID}`;
        const isPositive = account.totalOwed >= 0;
        const colorClass = isPositive ? 'text-danger' : 'text-success';
        const icon = isPositive ? 'fa-arrow-trend-up' : 'fa-check-circle';

        const itemsHtml = account.items.length === 0
            ? `<p class="text-muted small mb-0">Sin movimientos registrados.</p>`
            : `<table class="table table-sm table-hover mb-0">
                <thead>
                  <tr>
                    <th class="text-muted fw-normal small">Fecha</th>
                    <th class="text-muted fw-normal small">Descripción</th>
                    <th class="text-muted fw-normal small">Tipo</th>
                    <th class="text-muted fw-normal small text-end">Monto</th>
                  </tr>
                </thead>
                <tbody>
                  ${account.items.map(i => `
                    <tr>
                      <td class="small text-muted">${i.dateFmt}</td>
                      <td class="small">${i.description}</td>
                      <td><span class="badge ${typeBadge(i.type)} small">${i.typeLabel}</span></td>
                      <td class="small text-end fw-semibold">${i.amountFmt}</td>
                    </tr>`).join('')}
                </tbody>
              </table>`;

        return `
        <div class="col-md-6 col-lg-4">
          <div class="card shadow-sm border-0 h-100">
            <div class="card-body pb-2">
              <div class="d-flex justify-content-between align-items-start mb-3">
                <div class="d-flex align-items-center gap-2">
                  <div class="rounded-circle bg-primary bg-opacity-10 d-flex align-items-center justify-content-center"
                       style="width:40px;height:40px;">
                    <i class="fa-solid fa-user text-primary"></i>
                  </div>
                  <div>
                    <div class="fw-semibold">${account.personName}</div>
                    <div class="small text-muted">${account.items.length} movimiento(s)</div>
                  </div>
                </div>
                <button class="btn btn-sm btn-light btn-expand-account" data-target="${collapseID}" title="Ver detalle">
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
              <div class="card-body pt-0 border-top">
                ${itemsHtml}
              </div>
            </div>
          </div>
        </div>`;
    }

    function typeBadge(type) {
        switch (type) {
            case 'Transaction':   return 'bg-success bg-opacity-10 text-success';
            case 'CreditCard':    return 'bg-warning bg-opacity-10 text-warning';
            case 'FixedExpense':  return 'bg-info bg-opacity-10 text-info';
            default: return 'bg-secondary bg-opacity-10 text-secondary';
        }
    }

    // ── Cargar lista personas ─────────────────────────────────────
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
                      <span class="fw-semibold">${p.name}</span>
                    </td>
                    <td class="text-end">
                      <button class="btn btn-sm btn-outline-secondary me-1 btn-edit-person"
                              data-id="${p.id}" data-name="${p.name}" title="Editar">
                        <i class="fas fa-edit"></i>
                      </button>
                      <button class="btn btn-sm btn-outline-danger btn-delete-person"
                              data-id="${p.id}" data-name="${p.name}" title="Eliminar">
                        <i class="fas fa-trash"></i>
                      </button>
                    </td>
                  </tr>`).join('')}
              </tbody>
            </table>`;

            grid.querySelectorAll('.btn-edit-person').forEach(btn => {
                btn.addEventListener('click', () => openModal(btn.dataset.id, btn.dataset.name));
            });

            grid.querySelectorAll('.btn-delete-person').forEach(btn => {
                btn.addEventListener('click', () => deletePerson(btn.dataset.id, btn.dataset.name));
            });
        } catch (e) {
            grid.innerHTML = `<div class="text-danger">Error al cargar personas.</div>`;
        }
    }

    // ── Modal ────────────────────────────────────────────────────
    function openModal(id = 0, name = '') {
        inputID.value = id;
        inputName.value = name;
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
        if (!name) { inputName.classList.add('is-invalid'); return; }
        inputName.classList.remove('is-invalid');

        const dto = { id: parseInt(inputID.value), name };
        try {
            const res = await fetch(URL_SAVE, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': antiForgery.value
                },
                body: JSON.stringify(dto)
            });
            const json = await res.json();
            if (json.succeeded) {
                modal.hide();
                Swal.fire({ icon: 'success', title: json.message, timer: 1500, showConfirmButton: false });
                loadPersons();
                loadAccounts();
            } else {
                Swal.fire({ icon: 'error', title: 'Error', text: json.message });
            }
        } catch {
            Swal.fire({ icon: 'error', title: 'Error', text: 'Error de conexión' });
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
                headers: { 'RequestVerificationToken': antiForgery.value }
            });
            const json = await res.json();
            if (json.succeeded) {
                Swal.fire({ icon: 'success', title: json.message, timer: 1500, showConfirmButton: false });
                loadPersons();
                loadAccounts();
            } else {
                Swal.fire({ icon: 'error', title: 'Error', text: json.message });
            }
        } catch {
            Swal.fire({ icon: 'error', title: 'Error', text: 'Error de conexión' });
        }
    }

    // ── Tab switch ───────────────────────────────────────────────
    document.getElementById('personas-tab').addEventListener('shown.bs.tab', loadPersons);
    document.getElementById('cuentas-tab').addEventListener('shown.bs.tab', loadAccounts);

    // ── Init ─────────────────────────────────────────────────────
    loadAccounts();
})();
