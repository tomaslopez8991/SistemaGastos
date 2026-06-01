(function () {
    const $container = $('#planning-container');
    if ($container.length === 0) return;

    const urls = {
        list: $container.data('url-fixed-list'),
        form: $container.data('url-fixed-form'),
        save: $container.data('url-fixed-save'),
        delete: $container.data('url-fixed-delete'),
        pay: $container.data('url-fixed-pay'),
        toggle: $container.data('url-fixed-toggle')
    };

    let expensesData = [];
    let activeYear = new Date().getFullYear();
    let activeMonth = new Date().getMonth() + 1;

    init();

    function init() {
        loadExpenses(activeYear, activeMonth);
        $('#btnNewExpense').on('click', (e) => { e.preventDefault(); openModal(); });
        $('#filterStatusFixed').on('change', applyFilters);
    }

    window.cargarGastosFijos = function (year, month) {
        activeYear = parseInt(year);
        activeMonth = parseInt(month);
        loadExpenses(activeYear, activeMonth);
    };

    // =========================================================
    // HELPER: calcula días hasta el próximo vencimiento
    // usando expense.paymentDay (día del mes en que se cobra)
    // =========================================================
    function calcDaysUntilDue(expense) {
        // Si el backend ya lo manda y no es undefined/null, usarlo directamente
        if (expense.daysUntilDue !== undefined && expense.daysUntilDue !== null) {
            return expense.daysUntilDue;
        }

        const paymentDay = expense.paymentDay;
        if (!paymentDay) return null;

        const today = new Date();
        today.setHours(0, 0, 0, 0);

        // Fecha de vencimiento en el mes actual
        let due = new Date(today.getFullYear(), today.getMonth(), paymentDay);

        // Si ese día ya pasó este mes, saltar al mes siguiente
        if (due < today) {
            due = new Date(today.getFullYear(), today.getMonth() + 1, paymentDay);
        }

        const diffMs = due.getTime() - today.getTime();
        const diffDays = Math.round(diffMs / (1000 * 60 * 60 * 24));
        return diffDays;
    }

    // =========================================================
    // BADGE de estado
    // =========================================================
    function getStatusBadge(expense) {
        if (!expense.active) {
            return '<span class="badge text-bg-secondary"><i class="fas fa-pause me-1"></i>En pausa</span>';
        }
        if (expense.alreadyPaidThisMonth) {
            return '<span class="badge text-bg-success"><i class="fas fa-check-circle me-1"></i>Al día</span>';
        }

        const days = calcDaysUntilDue(expense);

        if (days === null) return '<span class="badge text-bg-secondary">—</span>';
        if (days < 0) return '<span class="badge text-bg-danger"><i class="fas fa-circle-exclamation me-1"></i>Vencido</span>';
        if (days === 0) return '<span class="badge text-bg-danger"><i class="fas fa-circle-exclamation me-1"></i>Vence hoy</span>';
        if (days <= 3) return `<span class="badge text-bg-warning"><i class="fas fa-clock me-1"></i>${days}d restantes</span>`;
        return `<span class="badge text-bg-light text-dark border">${days} días</span>`;
    }

    // =========================================================
    // RENDER DE CARDS
    // =========================================================
    function loadExpenses(year, month) {
        $.get(urls.list, { year: year, month: month }, function (response) {
            if (!response.success || !response.data) { showError(); return; }
            expensesData = response.data;
            updateTabBadge();
            applyFilters();
        }).fail(showError);
    }

    function showError() {
        $('#expenses-grid').html(`
            <div class="col-12">
                <div class="alert alert-danger">
                    <i class="fas fa-exclamation-triangle me-2"></i>
                    Error al cargar los compromisos. <a href="#" onclick="location.reload()">Reintentar</a>
                </div>
            </div>
        `);
    }

    function updateTabBadge() {
        const pendingCount = expensesData.filter(e => e.active && !e.alreadyPaidThisMonth).length;
        const $badge = $('#badge-pending-fixed');
        $badge.text(pendingCount);
        $badge.toggleClass('text-bg-warning', pendingCount > 0)
            .toggleClass('text-bg-secondary', pendingCount === 0);
    }

    function renderExpenses(expenses) {
        if (expenses.length === 0) {
            $('#expenses-grid').html(`
                <div class="col-12">
                    <div class="alert alert-info border-0 shadow-sm">
                        <i class="fas fa-check-circle me-2"></i>
                        No hay gastos fijos con este filtro.
                    </div>
                </div>
            `);
            return;
        }

        const culture = new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS' });
        let html = '';

        expenses.forEach(expense => {
            const isPaid = !!expense.alreadyPaidThisMonth;
            const paidMonthName = expense.paidMonthName || '';
            const isActive = !!expense.active;
            const days = calcDaysUntilDue(expense);

            // Clases de estado para la card
            let cardStateClass = '';
            if (isPaid) {
                cardStateClass = 'fe-card-paid';
            } else if (!isActive) {
                cardStateClass = 'fe-card-paused';
            } else if (days !== null && days < 0) {
                cardStateClass = 'fe-card-overdue';
            } else if (days !== null && days <= 3) {
                cardStateClass = 'fe-card-urgent';
            }

            // Icono/logo con color según estado
            let logoBg = 'bg-primary-subtle';
            let logoIcon = 'text-primary';
            if (isPaid) { logoBg = 'bg-success-subtle'; logoIcon = 'text-success'; }
            else if (!isActive) { logoBg = 'bg-secondary-subtle'; logoIcon = 'text-secondary'; }
            else if (days !== null && days < 0) { logoBg = 'bg-danger-subtle'; logoIcon = 'text-danger'; }
            else if (days !== null && days <= 3) { logoBg = 'bg-warning-subtle'; logoIcon = 'text-warning-emphasis'; }

            const logoHtml = expense.logoUrl
                ? `<img src="${expense.logoUrl}" alt="${expense.name}" class="rounded" style="width:40px;height:40px;object-fit:cover;">`
                : `<div class="rounded ${logoBg} d-flex align-items-center justify-content-center" style="width:40px;height:40px;">
                       <i class="fas fa-receipt ${logoIcon}"></i>
                   </div>`;

            // Texto de estado debajo de cuenta/día
            let daysHtml = '';
            if (!isActive) {
                daysHtml = `<div class="d-flex align-items-center gap-2 text-secondary">
                                <i class="fas fa-pause-circle" style="width:16px;"></i>
                                <span>Gasto en pausa</span>
                            </div>`;
            } else if (isPaid) {
                // vacío: la info de pago va en el footer strip
            } else if (days !== null) {
                let daysClass = 'text-body-secondary';
                let daysIcon = 'fa-calendar-day';
                let daysText = `Vence en ${days} día${days !== 1 ? 's' : ''}`;

                if (days < 0) { daysClass = 'text-danger fw-semibold'; daysIcon = 'fa-circle-exclamation'; daysText = `Vencido hace ${Math.abs(days)} día${Math.abs(days) !== 1 ? 's' : ''}`; }
                else if (days === 0) { daysClass = 'text-danger fw-semibold'; daysIcon = 'fa-circle-exclamation'; daysText = 'Vence hoy'; }
                else if (days <= 3) { daysClass = 'text-warning-emphasis fw-semibold'; daysIcon = 'fa-clock'; }

                daysHtml = `<div class="d-flex align-items-center gap-2 ${daysClass}">
                                <i class="fas ${daysIcon}" style="width:16px;"></i>
                                <span>${daysText}</span>
                            </div>`;
            }

            // Botones de acción
            let actionsHtml = '';
            if (isPaid) {
                // Footer strip reemplaza los botones
            } else if (isActive) {
                actionsHtml = `
                    <button class="btn btn-success btn-sm flex-fill fw-bold" onclick="payExpense(${expense.id})">
                        <i class="fas fa-check me-1"></i>Pagar
                    </button>
                    <button class="btn btn-outline-primary btn-sm" onclick="editExpense(${expense.id})" title="Editar">
                        <i class="fas fa-pen"></i>
                    </button>
                    <button class="btn btn-outline-warning btn-sm" onclick="toggleActive(${expense.id}, true)" title="Pausar">
                        <i class="fas fa-pause"></i>
                    </button>
                    <button class="btn btn-outline-danger btn-sm" onclick="deleteExpense(${expense.id})" title="Eliminar">
                        <i class="fas fa-trash"></i>
                    </button>
                `;
            } else {
                // Pausado: Reanudar como acción principal
                actionsHtml = `
                    <button class="btn btn-primary btn-sm flex-fill fw-bold" onclick="toggleActive(${expense.id}, false)" title="Reanudar gasto">
                        <i class="fas fa-play me-1"></i>Reanudar
                    </button>
                    <button class="btn btn-outline-secondary btn-sm" onclick="editExpense(${expense.id})" title="Editar">
                        <i class="fas fa-pen"></i>
                    </button>
                    <button class="btn btn-outline-danger btn-sm" onclick="deleteExpense(${expense.id})" title="Eliminar">
                        <i class="fas fa-trash"></i>
                    </button>
                `;
            }

            // Footer para gastos pagados (reemplaza los botones)
            const paidFooter = isPaid
                ? `<div class="fe-paid-footer mt-3">
                       <i class="fas fa-circle-check"></i>
                       <span>Pagado en ${paidMonthName || 'este mes'}</span>
                   </div>`
                : '';

            html += `
                <div class="col-md-6 col-lg-4">
                    <div class="card border-0 shadow-sm h-100 expense-card ${cardStateClass}">
                        <div class="card-body d-flex flex-column">

                            <div class="d-flex justify-content-between align-items-start mb-3">
                                <div class="d-flex align-items-center gap-3">
                                    ${logoHtml}
                                    <div>
                                        <h6 class="mb-0 fw-bold text-body-emphasis">${expense.name}</h6>
                                        <small class="text-body-secondary">${expense.categoryName}</small>
                                    </div>
                                </div>
                                ${getStatusBadge(expense)}
                            </div>

                            <div class="mb-3">
                                <div class="text-body-secondary small mb-1">Monto mensual</div>
                                <h4 class="fw-bold mb-0 ${isPaid ? 'text-success' : !isActive ? 'text-secondary' : 'text-primary-emphasis'}">
                                    ${culture.format(expense.amount)}
                                </h4>
                            </div>

                            <div class="mb-3 small text-body-secondary">
                                <div class="d-flex align-items-center gap-2 mb-2">
                                    <i class="fas fa-calendar-day" style="width:16px;"></i>
                                    <span>Día ${expense.paymentDay}</span>
                                </div>
                                <div class="d-flex align-items-center gap-2 mb-2">
                                    <i class="fas fa-wallet" style="width:16px;"></i>
                                    <span>${expense.accountName}</span>
                                </div>
                                ${daysHtml}
                            </div>

                            ${isPaid
                                ? paidFooter
                                : `<div class="d-flex gap-2 mt-auto">${actionsHtml}</div>`
                            }
                        </div>
                    </div>
                </div>
            `;
        });

        $('#expenses-grid').html(html);
    }

    function applyFilters() {
        const status = $('#filterStatusFixed').val();
        let filtered = expensesData;
        if (status === 'active') {
            filtered = filtered.filter(e => e.active && !e.alreadyPaidThisMonth);
        }
        renderExpenses(filtered);
    }

    function notifyDashboardChanged() {
        loadExpenses(activeYear, activeMonth);
        if (window.reloadCashflowBalances) window.reloadCashflowBalances();
    }

    // =========================================================
    // FUNCIONES GLOBALES (llamadas desde onclick en el HTML)
    // =========================================================
    window.editExpense = function (id) { openModal(id); };

    window.payExpense = function (id) {
        const expense = expensesData.find(e => e.id === id);
        if (!expense) return;

        Swal.fire({
            title: '¿Confirmar pago?',
            html: `<div class="text-start">
                    <p><strong>Gasto:</strong> ${expense.name}</p>
                    <p><strong>Monto:</strong> ${new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS' }).format(expense.amount)}</p>
                    <p><strong>Cuenta:</strong> ${expense.accountName}</p>
                   </div>`,
            icon: 'question',
            showCancelButton: true,
            confirmButtonText: 'Sí, pagar',
            cancelButtonText: 'Cancelar',
            confirmButtonColor: '#28a745'
        }).then((result) => {
            if (result.isConfirmed) {
                $.ajax({
                    url: urls.pay,
                    type: 'POST',
                    contentType: 'application/json',
                    data: JSON.stringify(id),
                    headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
                    success: (response) => {
                        if (response.success) {
                            Swal.fire({ title: 'Pagado', icon: 'success', timer: 1500, showConfirmButton: false });
                            notifyDashboardChanged();
                        } else {
                            Swal.fire('Error', response.message, 'error');
                        }
                    }
                });
            }
        });
    };

    window.toggleActive = function (id, currentActive) {
        $.ajax({
            url: urls.toggle,
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(id),
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            success: (response) => {
                if (response.success) notifyDashboardChanged();
            }
        });
    };

    window.deleteExpense = function (id) {
        Swal.fire({
            title: '¿Eliminar gasto?',
            icon: 'warning',
            showCancelButton: true,
            confirmButtonText: 'Sí, eliminar',
            confirmButtonColor: '#dc3545'
        }).then((result) => {
            if (result.isConfirmed) {
                Swal.showLoading();
                $.ajax({
                    url: `${urls.delete}/${id}`,
                    type: 'DELETE',
                    headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
                    success: (response) => {
                        if (response.success || response.succeeded) {
                            Swal.fire({ icon: 'success', title: 'Eliminado', showConfirmButton: false, timer: 1000 });
                            notifyDashboardChanged();
                        } else {
                            Swal.fire('Error', response.message || 'Error al eliminar', 'error');
                        }
                    },
                    error: (xhr) => {
                        console.error('Error:', xhr);
                        Swal.fire('Error', 'No se pudo comunicar con el servidor', 'error');
                    }
                });
            }
        });
    };

    function openModal(id = null) {
        const url = id ? `${urls.form}?id=${id}` : urls.form;
        $.get(url, function (html) {
            Swal.fire({
                title: id ? 'Editar suscripción' : 'Nueva suscripción',
                html: html,
                width: '600px',
                showCancelButton: true,
                confirmButtonText: 'Guardar',
                cancelButtonText: 'Cancelar',
                preConfirm: () => {
                    const form = $('#expenseForm');
                    return {
                        ID: parseInt(form.find('#ID').val()) || 0,
                        Name: form.find('#Name').val()?.trim(),
                        Amount: parseFloat(form.find('#Amount').val()) || 0,
                        PaymentDay: parseInt(form.find('#PaymentDay').val()) || 1,
                        CategoryID: parseInt(form.find('#CategoryID').val()) || 0,
                        AccountID: parseInt(form.find('#AccountID').val()) || 0,
                        LogoUrl: form.find('#LogoUrl').val()?.trim() || null,
                        PersonID: parseInt(form.find('#PersonID').val()) || null,
                        Active: true
                    };
                }
            }).then((result) => {
                if (result.isConfirmed) saveExpense(result.value);
            });
        });
    }

    function saveExpense(data) {
        if (!data.Name || data.Amount <= 0) {
            Swal.fire('Error', 'Datos incompletos', 'error');
            return;
        }
        $.ajax({
            url: urls.save,
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(data),
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            success: (response) => {
                if (response.success) {
                    Swal.fire({ title: 'Guardado', icon: 'success', timer: 1500, showConfirmButton: false });
                    notifyDashboardChanged();
                } else {
                    Swal.fire('Error', response.message, 'error');
                }
            }
        });
    }

})();