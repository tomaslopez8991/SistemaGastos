(function () {
    const $container = $('#planning-container');
    if ($container.length === 0) return;

    const urls = {
        list: $container.data('url-fixed-list'),
        form: $container.data('url-fixed-form'),
        save: $container.data('url-fixed-save'),
        delete: $container.data('url-fixed-delete'),
        pay: $container.data('url-fixed-pay'),
        toggle: $container.data('url-fixed-toggle'),
        pause: $container.data('url-fixed-pause'),
        syncCc: $container.data('url-fixed-sync-cc'),
        payTc: $container.data('url-pay-tc'),
        personPaymentAccounts: $container.data('url-person-payment-accounts')
    };

    let expensesData = [];
    let activeYear = new Date().getFullYear();
    let activeMonth = new Date().getMonth() + 1;
    const syncedMonths = new Set(); // Evita llamar al sync más de una vez por mes/carga

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
    // HELPER: calcula días desde hoy hasta el vencimiento
    // del gasto en el MES SELECCIONADO (no siempre el actual)
    // =========================================================
    function calcDaysUntilDue(expense, targetYear, targetMonth) {
        const paymentDay = expense.paymentDay;
        if (!paymentDay) return null;

        const today = new Date();
        today.setHours(0, 0, 0, 0);

        // Fecha de vencimiento en el mes objetivo
        const due = new Date(targetYear, targetMonth - 1, paymentDay);

        return Math.round((due - today) / 86400000);
    }

    // =========================================================
    // BADGE de estado
    // =========================================================
    function getStatusBadge(expense) {
        const selectedMonthStart = new Date(activeYear, activeMonth - 1, 1);
        const startDate = expense.startDate ? new Date(expense.startDate) : null;
        const notStartedYet = startDate !== null && startDate > selectedMonthStart;

        if (!expense.active) {
            return '<span class="badge text-bg-secondary"><i class="fas fa-pause me-1"></i>En pausa</span>';
        }
        if (expense.isPausedThisMonth) {
            return '<span class="badge text-bg-warning text-dark"><i class="fas fa-calendar-minus me-1"></i>Pausado este mes</span>';
        }
        if (notStartedYet) {
            const from = startDate.toLocaleDateString('es-AR', { month: 'short', year: '2-digit' });
            return `<span class="badge text-bg-secondary"><i class="fas fa-clock me-1"></i>Desde ${from}</span>`;
        }
        if (expense.alreadyPaidThisMonth) {
            return '<span class="badge text-bg-success"><i class="fas fa-check-circle me-1"></i>Al día</span>';
        }

        const days = calcDaysUntilDue(expense, activeYear, activeMonth);

        if (days === null) return '<span class="badge text-bg-secondary">—</span>';
        if (days < 0) return `<span class="badge text-bg-danger"><i class="fas fa-circle-exclamation me-1"></i>Venció hace ${Math.abs(days)}d</span>`;
        if (days === 0) return '<span class="badge text-bg-danger"><i class="fas fa-circle-exclamation me-1"></i>Vence hoy</span>';
        if (days <= 3) return `<span class="badge text-bg-warning"><i class="fas fa-clock me-1"></i>${days}d restantes</span>`;
        return `<span class="badge text-bg-light text-dark border">${days} días</span>`;
    }

    // =========================================================
    // RENDER DE CARDS
    // =========================================================
    function loadExpenses(year, month) {
        const now = new Date();
        const isCurrentMonth = (year === now.getFullYear() && month === now.getMonth() + 1);

        const doLoad = () => {
            $.get(urls.list, { year: year, month: month }, function (response) {
                if (!response.success || !response.data) { showError(); return; }
                expensesData = response.data;
                updateTabBadge();
                applyFilters();
            }).fail(showError);
        };

        const monthKey = `${year}-${String(month).padStart(2, '0')}`;

        // Para el mes actual, sincronizar TC una sola vez por carga de página
        if (isCurrentMonth && urls.syncCc && !syncedMonths.has(monthKey)) {
            syncedMonths.add(monthKey);
            $.ajax({
                url: urls.syncCc,
                type: 'POST',
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
                complete: doLoad
            });
        } else {
            doLoad();
        }
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
        const pendingCount = expensesData.filter(e => e.active && !e.alreadyPaidThisMonth && !e.isPausedThisMonth).length;
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
            const isPausedThisMonth = !!expense.isPausedThisMonth;

            // Si el gasto tiene una fecha de inicio posterior al mes seleccionado,
            // se trata como "aún no activo" para ese mes
            const selectedMonthStart = new Date(activeYear, activeMonth - 1, 1);
            const startDate = expense.startDate ? new Date(expense.startDate) : null;
            const notStartedYet = startDate !== null && startDate > selectedMonthStart;
            const effectivelyActive = isActive && !notStartedYet && !isPausedThisMonth;

            const days = effectivelyActive ? calcDaysUntilDue(expense, activeYear, activeMonth) : null;

            // Clases de estado para la card
            let cardStateClass = '';
            if (isPaid) {
                cardStateClass = 'fe-card-paid';
            } else if (isPausedThisMonth) {
                cardStateClass = 'fe-card-month-paused';
            } else if (!effectivelyActive) {
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
            else if (isPausedThisMonth) { logoBg = 'bg-warning-subtle'; logoIcon = 'text-warning-emphasis'; }
            else if (!effectivelyActive) { logoBg = 'bg-secondary-subtle'; logoIcon = 'text-secondary'; }
            else if (days !== null && days < 0) { logoBg = 'bg-danger-subtle'; logoIcon = 'text-danger'; }
            else if (days !== null && days <= 3) { logoBg = 'bg-warning-subtle'; logoIcon = 'text-warning-emphasis'; }

            const defaultIcon = expense.isCreditCardPayment ? 'fa-credit-card' : 'fa-receipt';
            const logoHtml = expense.logoUrl
                ? `<img src="${expense.logoUrl}" alt="${expense.name}" class="rounded" style="width:40px;height:40px;object-fit:cover;">`
                : `<div class="rounded ${logoBg} d-flex align-items-center justify-content-center" style="width:40px;height:40px;">
                       <i class="fas ${defaultIcon} ${logoIcon}"></i>
                   </div>`;

            // Texto de estado debajo de cuenta/día
            let daysHtml = '';
            if (isPausedThisMonth) {
                daysHtml = `<div class="d-flex align-items-center gap-2 text-warning-emphasis">
                                <i class="fas fa-calendar-minus" style="width:16px;"></i>
                                <span>No aplica este mes</span>
                            </div>`;
            } else if (!effectivelyActive) {
                const pauseLabel = notStartedYet
                    ? `Activo desde ${startDate.toLocaleDateString('es-AR', { month: 'short', year: '2-digit' })}`
                    : 'Gasto en pausa';
                daysHtml = `<div class="d-flex align-items-center gap-2 text-secondary">
                                <i class="fas fa-pause-circle" style="width:16px;"></i>
                                <span>${pauseLabel}</span>
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
            } else if (expense.isSystemGenerated) {
                actionsHtml = `
                    <button class="btn btn-success btn-sm flex-fill fw-bold" onclick="payExpense(${expense.id})">
                        <i class="fas fa-check me-1"></i>Pagar
                    </button>
                    <span class="btn btn-sm btn-outline-secondary disabled" title="Calculado automáticamente por el sistema">
                        <i class="fas fa-lock"></i>
                    </span>
                `;
            } else if (isPausedThisMonth) {
                actionsHtml = `
                    <button class="btn btn-warning btn-sm flex-fill fw-bold" onclick="pauseExpense(${expense.id})">
                        <i class="fas fa-play me-1"></i>Reanudar este mes
                    </button>
                    <button class="btn btn-outline-secondary btn-sm" onclick="editExpense(${expense.id})" title="Editar">
                        <i class="fas fa-pen"></i>
                    </button>
                    <button class="btn btn-outline-danger btn-sm" onclick="deleteExpense(${expense.id})" title="Eliminar">
                        <i class="fas fa-trash"></i>
                    </button>
                `;
            } else if (effectivelyActive) {
                if (expense.isCreditCardPayment) {
                    const tcId    = expense.creditCardAccountID;
                    const tcTotal = expense.tcTotalAmount || expense.amount;
                    const minBtn  = expense.tcMinimumAmount > 0
                        ? `<button class="btn btn-warning btn-sm" onclick="payTcFromFe(${tcId}, ${expense.tcMinimumAmount}, 'Pagar mínimo', false)" title="Pagar mínimo">
                               <i class="fas fa-minus me-1"></i>Mínimo
                           </button>`
                        : '';
                    actionsHtml = `
                        <button class="btn btn-danger btn-sm flex-fill fw-bold" onclick="payTcFromFe(${tcId}, ${tcTotal}, 'Pagar total', false)">
                            <i class="fas fa-check me-1"></i>Total
                        </button>
                        ${minBtn}
                        <button class="btn btn-outline-secondary btn-sm" onclick="payTcFromFe(${tcId}, ${tcTotal}, 'Pago personalizado', true)" title="Personalizado">
                            <i class="fas fa-pencil-alt"></i>
                        </button>
                        <button class="btn btn-outline-primary btn-sm" onclick="editExpense(${expense.id})" title="Editar">
                            <i class="fas fa-pen"></i>
                        </button>
                        <button class="btn btn-outline-danger btn-sm" onclick="deleteExpense(${expense.id})" title="Eliminar">
                            <i class="fas fa-trash"></i>
                        </button>
                    `;
                } else {
                    actionsHtml = `
                        <button class="btn btn-success btn-sm flex-fill fw-bold" onclick="payExpense(${expense.id})">
                            <i class="fas fa-check me-1"></i>Pagar
                        </button>
                        <button class="btn btn-outline-primary btn-sm" onclick="editExpense(${expense.id})" title="Editar">
                            <i class="fas fa-pen"></i>
                        </button>
                        <button class="btn btn-outline-warning btn-sm" onclick="pauseExpense(${expense.id})" title="Pausar este mes">
                            <i class="fas fa-calendar-minus"></i>
                        </button>
                        <button class="btn btn-outline-danger btn-sm" onclick="deleteExpense(${expense.id})" title="Eliminar">
                            <i class="fas fa-trash"></i>
                        </button>
                    `;
                }
            } else if (notStartedYet) {
                // Aún no comenzó: solo editar y eliminar (no tiene sentido reanudar/pausar)
                actionsHtml = `
                    <button class="btn btn-outline-secondary btn-sm flex-fill" onclick="editExpense(${expense.id})" title="Editar">
                        <i class="fas fa-pen me-1"></i>Editar
                    </button>
                    <button class="btn btn-outline-danger btn-sm" onclick="deleteExpense(${expense.id})" title="Eliminar">
                        <i class="fas fa-trash"></i>
                    </button>
                `;
            } else {
                // Pausado manualmente: ofrecer reanudar desde este mes
                actionsHtml = `
                    <button class="btn btn-primary btn-sm flex-fill fw-bold" onclick="toggleActive(${expense.id}, false, ${activeYear}, ${activeMonth})" title="Reanudar desde ${activeMonth}/${activeYear}">
                        <i class="fas fa-play me-1"></i>Reanudar desde este mes
                    </button>
                    <button class="btn btn-outline-secondary btn-sm" onclick="editExpense(${expense.id})" title="Editar">
                        <i class="fas fa-pen"></i>
                    </button>
                    <button class="btn btn-outline-danger btn-sm" onclick="deleteExpense(${expense.id})" title="Eliminar">
                        <i class="fas fa-trash"></i>
                    </button>
                `;
            }

            // Monto a mostrar: si está pagado, usar el monto histórico de la Transaction
            const displayAmount = isPaid && expense.paidAmountFormatted
                ? expense.paidAmountFormatted
                : (expense.amountFormatted || culture.format(expense.amount));
            const amountLabel = isPaid
                ? 'Monto pagado'
                : (expense.isSystemGenerated ? 'Interés acumulado' : 'Monto mensual');

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
                        <div class="card-body d-flex flex-column" style="gap:.75rem;">

                            <div class="d-flex align-items-center gap-3">
                                ${logoHtml}
                                <div class="flex-fill" style="min-width:0;">
                                    <div class="fw-semibold text-truncate text-body-emphasis">${expense.name}</div>
                                    <div class="small text-muted text-truncate">${expense.categoryName}</div>
                                </div>
                                <div class="flex-shrink-0">${getStatusBadge(expense)}</div>
                            </div>

                            <div>
                                <div class="small text-muted mb-1">${amountLabel}</div>
                                <div class="fw-bold fs-5 ${isPaid ? 'text-success' : !isActive ? 'text-secondary' : 'text-primary-emphasis'}">${displayAmount}</div>
                            </div>

                            <div class="small text-muted d-flex flex-column gap-1">
                                <div class="d-flex align-items-center gap-2">
                                    <i class="fas fa-calendar-day" style="width:14px;flex-shrink:0;"></i>
                                    <span>Día ${expense.paymentDay}</span>
                                </div>
                                <div class="d-flex align-items-center gap-2">
                                    <i class="fas fa-wallet" style="width:14px;flex-shrink:0;"></i>
                                    <span class="text-truncate">${expense.accountName}</span>
                                </div>
                            </div>

                            <div class="small" style="min-height:1.4rem;">${daysHtml}</div>

                            <div class="mt-auto">
                                ${isPaid
                                    ? `<div class="fe-paid-footer"><i class="fas fa-circle-check"></i><span>Pagado en ${paidMonthName || 'este mes'}</span></div>`
                                    : `<div class="d-flex gap-2">${actionsHtml}</div>`
                                }
                            </div>

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

    // currentActive = true → pausando; false → reanudando
    // Al reanudar se envía el año/mes seleccionado para activar desde ese mes
    window.toggleActive = function (id, currentActive, year, month) {
        const body = currentActive
            ? { ID: id }                          // pausar: sin fecha
            : { ID: id, Year: year, Month: month }; // reanudar desde el mes seleccionado
        $.ajax({
            url: urls.toggle,
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(body),
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            success: (response) => {
                if (response.success) notifyDashboardChanged();
            }
        });
    };

    window.pauseExpense = function (id) {
        const expense = expensesData.find(e => e.id === id);
        if (!expense) return;

        const isPaused = !!expense.isPausedThisMonth;
        const monthName = new Date(activeYear, activeMonth - 1, 1)
            .toLocaleDateString('es-AR', { month: 'long', year: 'numeric' });

        const title = isPaused ? 'Reanudar este mes' : 'Pausar este mes';
        const text  = isPaused
            ? `<strong>${expense.name}</strong> volverá a aparecer en la proyección de <em>${monthName}</em>.`
            : `<strong>${expense.name}</strong> no aparecerá en la proyección de <em>${monthName}</em>. Los demás meses no se ven afectados.`;

        Swal.fire({
            title,
            html: text,
            icon: 'question',
            showCancelButton: true,
            confirmButtonText: isPaused ? 'Sí, reanudar' : 'Sí, pausar',
            cancelButtonText: 'Cancelar',
            confirmButtonColor: isPaused ? '#198754' : '#fd7e14'
        }).then((result) => {
            if (!result.isConfirmed) return;
            $.ajax({
                url: urls.pause,
                type: 'POST',
                contentType: 'application/json',
                data: JSON.stringify({ ID: id, Year: activeYear, Month: activeMonth }),
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
                success: (response) => {
                    if (response.success) notifyDashboardChanged();
                    else Swal.fire('Error', response.message, 'error');
                }
            });
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
        let distributionPanel = null;

        $.get(url, function (html) {
            Swal.fire({
                title: id ? 'Editar suscripción' : 'Nueva suscripción',
                html: html,
                width: '600px',
                showCancelButton: true,
                confirmButtonText: 'Guardar',
                cancelButtonText: 'Cancelar',
                didOpen: () => {
                    const popup = Swal.getPopup();
                    const $checkDist = $(popup).find('#checkDistribuirFE');
                    const initialEndDay = parseInt($checkDist.data('initial-end-day')) || null;
                    const initialExcludedDays = ($checkDist.data('initial-excluded') ?? '')
                        .toString()
                        .split(',')
                        .map(s => parseInt(s))
                        .filter(n => !isNaN(n));

                    function getStartDay() {
                        const day = parseInt($(popup).find('#PaymentDay').val());
                        return isNaN(day) ? null : day;
                    }

                    distributionPanel = DistributionPanel.init($(popup), {
                        checkboxSelector: '#checkDistribuirFE',
                        panelSelector: '#panel-distribucion-fe',
                        gridSelector: '#distribucion-dias-grid-fe',
                        getStartDay,
                        getMonthYear: () => ({ year: activeYear, month: activeMonth }),
                        initialEndDay,
                        initialExcludedDays
                    });

                    $(popup).find('#PaymentDay').on('input', () => distributionPanel.refresh());
                },
                preConfirm: () => {
                    const form = $('#expenseForm');
                    const dist = distributionPanel
                        ? distributionPanel.getValues()
                        : { distributionEndDay: null, excludedDays: null };

                    return {
                        ID:        parseInt(form.find('#ID').val()) || 0,
                        Name:      form.find('#Name').val()?.trim(),
                        Amount:    parseFloat(form.find('#Amount').val()) || 0,
                        Currency:  form.find('#Currency').val() || 'ARS',
                        PaymentDay:parseInt(form.find('#PaymentDay').val()) || 1,
                        StartDate: form.find('#StartDate').val() || null,
                        CategoryID:parseInt(form.find('#CategoryID').val()) || 0,
                        AccountID: parseInt(form.find('#AccountID').val()) || 0,
                        LogoUrl: form.find('#LogoUrl').val()?.trim() || null,
                        PersonID: parseInt(form.find('#PersonID').val()) || null,
                        PersonPercentage: (parseInt(form.find('#PersonID').val()) || null)
                            ? (parseFloat(form.find('#PersonPercentage').val()) || 100)
                            : null,
                        Active: true,
                        DistributionEndDay: dist.distributionEndDay,
                        ExcludedDays: dist.excludedDays
                    };
                }
            }).then((result) => {
                if (result.isConfirmed) saveExpense(result.value);
            });
        });
    }

    window.payTcFromFe = async function (tcId, amount, label, editable) {
        let accounts = [];
        try {
            const res  = await fetch(urls.personPaymentAccounts);
            const json = await res.json();
            accounts   = json.data || [];
        } catch {}

        if (!accounts.length) {
            Swal.fire('Sin cuentas', 'No hay cuentas disponibles para debitar el pago.', 'warning');
            return;
        }

        const fmt = new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS' });
        const accountOptions = accounts.map(a => `<option value="${a.id}">${a.name} (${a.currency})</option>`).join('');
        const amountInput = editable
            ? `<input id="fe-tc-amount" type="number" step="0.01" min="0.01"
                      class="form-control text-end fw-bold" value="${(+amount).toFixed(2)}" />`
            : `<div class="form-control-plaintext fw-bold text-danger">${fmt.format(amount)}</div>
               <input type="hidden" id="fe-tc-amount" value="${(+amount).toFixed(2)}" />`;

        const { value: formValues, isConfirmed } = await Swal.fire({
            title: label,
            html: `<div class="text-start">
                <div class="mb-3">
                    <label class="form-label small fw-semibold">Monto a pagar</label>
                    ${amountInput}
                </div>
                <div class="mb-2">
                    <label class="form-label small fw-semibold">Débitar de cuenta</label>
                    <select id="fe-tc-account" class="form-select">${accountOptions}</select>
                </div>
            </div>`,
            showCancelButton: true,
            confirmButtonText: '<i class="fas fa-check me-1"></i>Confirmar pago',
            confirmButtonColor: '#dc3545',
            cancelButtonText: 'Cancelar',
            preConfirm: () => ({
                amount:    parseFloat(document.getElementById('fe-tc-amount').value) || 0,
                accountId: parseInt(document.getElementById('fe-tc-account').value) || 0
            })
        });

        if (!isConfirmed || !formValues || formValues.amount <= 0) return;

        $.ajax({
            url: urls.payTc,
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({ tcAccountId: tcId, sourceAccountId: formValues.accountId, amount: formValues.amount }),
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            success: response => {
                if (response.success) {
                    Swal.fire({ toast: true, position: 'top-end', icon: 'success', title: response.message || 'Pago registrado', showConfirmButton: false, timer: 2500 });
                    notifyDashboardChanged();
                } else {
                    Swal.fire('Error', response.message || 'No se pudo registrar el pago', 'error');
                }
            },
            error: () => Swal.fire('Error', 'No se pudo conectar con el servidor', 'error')
        });
    };

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
