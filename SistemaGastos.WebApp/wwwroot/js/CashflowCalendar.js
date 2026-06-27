(function () {
    const $container = $('#planning-container');
    if ($container.length === 0) return;

    const urlDailyBalances  = $container.data('url-daily-balances');
    const urlSetDayOverride = $container.data('url-set-day-override');
    const urlFixedPay              = $container.data('url-fixed-pay');
    const urlIncomeReceive         = $container.data('url-income-receive');
    const urlConfirmDay            = $container.data('url-confirm-day');
    const urlPersonRegisterPayment = $container.data('url-person-register-payment');
    const urlPersonPaymentAccounts = $container.data('url-person-payment-accounts');
    const fmtCompact = new Intl.NumberFormat('es-AR', {
        style: 'currency', currency: 'ARS', notation: 'compact', maximumFractionDigits: 1
    });

    const sourceLabels = {
        Planificado:   'Movimiento planificado',
        GastoFijo:     'Gasto fijo',
        IngresoFijo:   'Ingreso fijo',
        TarjetaCredito:'Tarjeta de crédito',
        Personas:      'A cobrar (persona)',
        Transaccion:   'Transacción registrada'
    };

    let calendar = null;
    let balancesByDate = {};
    let navigatingFromSlider = false;
    let pendingValidRange = null;

    // ── Panel lateral ─────────────────────────────────────────
    const $panel   = $('#cf-day-panel');
    const $overlay = $('#cf-panel-overlay');
    let currentPanelDate = null;

    function openPanel(day) {
        currentPanelDate = day;

        const dateObj = new Date(day.date + 'T00:00:00');
        const dateLabel = dateObj.toLocaleDateString('es-AR', {
            weekday: 'long', day: 'numeric', month: 'long', year: 'numeric'
        });

        $('#cf-panel-date-label').text(
            dateLabel.charAt(0).toUpperCase() + dateLabel.slice(1)
        );

        const balClass = day.balance >= 0 ? 'text-success' : 'text-danger';
        $('#cf-panel-balance-label')
            .removeClass('text-success text-danger')
            .addClass(balClass)
            .text('Saldo del día: ' + day.balanceFmt);

        renderPanelItems(day);
        $panel.addClass('open');
        $overlay.addClass('open');
    }

    function closePanel() {
        $panel.removeClass('open');
        $overlay.removeClass('open');
        currentPanelDate = null;
    }

    function renderPanelItems(day) {
        const $body = $('#cf-panel-body');

        if (!day.items || day.items.length === 0) {
            $body.html('<p class="text-body-secondary small mb-0 px-1">Sin movimientos para este día.</p>');
            return;
        }

        const rows = day.items.map(function (item) {
            const cls  = item.isIncome ? 'text-success' : 'text-danger';
            const sign = item.isIncome ? '+' : '-';
            const typeLabel = sourceLabels[item.sourceType] || item.sourceType;

            let actions = '';
            if (item.sourceType === 'Planificado' && item.sourceId) {
                const overrideBtn = item.isDistributed
                    ? `<button class="btn btn-xs btn-outline-secondary cf-panel-override"
                               data-id="${item.sourceId}" data-day="${item.day}"
                               data-amount="${item.amount}" title="Editar monto de este día">
                           <i class="fas fa-dollar-sign"></i>
                       </button>`
                    : '';
                const confirmLabel = item.isIncome ? 'Cobrar' : 'Pagar';
                const confirmCls   = item.isIncome ? 'btn-outline-success' : 'btn-outline-warning';
                actions = `
                <div class="cf-panel-item-actions d-flex gap-1 mt-2">
                    <button class="btn btn-xs ${confirmCls} cf-panel-confirm"
                            data-id="${item.sourceId}" data-day="${item.day}"
                            title="${confirmLabel} este movimiento">
                        <i class="fas fa-check me-1"></i>${confirmLabel}
                    </button>
                    <button class="btn btn-xs btn-outline-primary cf-panel-edit"
                            data-id="${item.sourceId}" title="Editar">
                        <i class="fas fa-pen"></i>
                    </button>
                    ${overrideBtn}
                    <button class="btn btn-xs btn-outline-danger cf-panel-delete"
                            data-id="${item.sourceId}" title="Eliminar">
                        <i class="fas fa-trash"></i>
                    </button>
                </div>`;
            } else if (item.sourceType === 'GastoFijo' && item.sourceId) {
                actions = `
                <div class="cf-panel-item-actions d-flex gap-1 mt-2">
                    <button class="btn btn-xs btn-outline-danger cf-panel-pay-fe"
                            data-id="${item.sourceId}" title="Registrar pago">
                        <i class="fas fa-check me-1"></i>Pagar
                    </button>
                </div>`;
            } else if (item.sourceType === 'IngresoFijo' && item.sourceId) {
                const amt = item.isDistributed ? item.amount : '';
                actions = `
                <div class="cf-panel-item-actions d-flex gap-1 mt-2">
                    <button class="btn btn-xs btn-outline-success cf-panel-receive-fi"
                            data-id="${item.sourceId}" data-amount="${amt}" title="Registrar cobro">
                        <i class="fas fa-check me-1"></i>Cobrar
                    </button>
                </div>`;
            } else if (item.sourceType === 'Personas' && item.sourceId) {
                const personName = item.description.replace(/^Cobro:\s*/, '');
                actions = `
                <div class="cf-panel-item-actions d-flex gap-1 mt-2">
                    <button class="btn btn-xs btn-outline-success cf-panel-collect-person"
                            data-id="${item.sourceId}"
                            data-name="${personName}"
                            data-amount="${item.amount}"
                            title="Registrar cobro">
                        <i class="fas fa-hand-holding-dollar me-1"></i>Cobrar
                    </button>
                </div>`;
            }

            return `<div class="cf-panel-item">
                <div class="d-flex justify-content-between align-items-start gap-2">
                    <div class="min-w-0">
                        <div class="cf-panel-item-desc">${item.description}</div>
                        <small class="cf-panel-item-source">${typeLabel}</small>
                        ${actions}
                    </div>
                    <span class="${cls} fw-semibold text-nowrap">${sign} ${item.amountFmt}</span>
                </div>
            </div>`;
        }).join('');

        $body.html(rows);
    }

    // Cerrar panel
    $('#cf-panel-close-btn').on('click', closePanel);
    $overlay.on('click', closePanel);

    // Editar desde panel
    $(document).on('click', '.cf-panel-edit', function () {
        const id = $(this).data('id');
        closePanel();
        if (window.abrirModalTmpTransaction) window.abrirModalTmpTransaction(id, null);
    });

    // Eliminar desde panel
    $(document).on('click', '.cf-panel-delete', function () {
        const id = $(this).data('id');
        closePanel();
        if (window.eliminarTmpTransaction) window.eliminarTmpTransaction([id]);
    });

    // Editar monto del día (override distribución)
    $(document).on('click', '.cf-panel-override', function () {
        const txId    = parseInt($(this).data('id'));
        const day     = parseInt($(this).data('day'));
        const current = parseFloat($(this).data('amount')) || 0;
        const fmt     = new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS' });

        Swal.fire({
            title: `Monto del día ${day}`,
            html: `
                <div class="text-start">
                    <p class="text-muted small mb-2">Monto calculado: <strong>${fmt.format(current)}</strong></p>
                    <label class="form-label small fw-semibold">Nuevo monto</label>
                    <div class="input-group">
                        <span class="input-group-text">$</span>
                        <input id="swal-override-amount" type="number" step="0.01" min="0"
                               class="form-control text-end" value="${current.toFixed(2)}">
                    </div>
                    <div class="mt-2">
                        <a href="#" id="swal-clear-override" class="small text-muted">Restaurar automático</a>
                    </div>
                </div>`,
            showCancelButton: true,
            confirmButtonText: 'Guardar',
            cancelButtonText: 'Cancelar',
            didOpen: () => {
                document.getElementById('swal-clear-override').addEventListener('click', function (e) {
                    e.preventDefault();
                    document.getElementById('swal-override-amount').value = '';
                    Swal.clickConfirm();
                });
            },
            preConfirm: () => {
                const val = document.getElementById('swal-override-amount').value;
                return val === '' ? null : (parseFloat(val) || 0);
            }
        }).then(result => {
            if (!result.isConfirmed) return;
            const amount = result.value;  // null = clear override
            closePanel();
            $.ajax({
                url: urlSetDayOverride,
                type: 'POST',
                contentType: 'application/json',
                data: JSON.stringify({ TmpTransactionID: txId, Day: day, Amount: amount }),
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
                success: response => {
                    if (response.success) {
                        if (window.reloadCashflowCalendar) window.reloadCashflowCalendar();
                    } else {
                        Swal.fire('Error', response.message || 'No se pudo actualizar', 'error');
                    }
                }
            });
        });
    });

    // Confirmar movimiento planificado desde panel
    $(document).on('click', '.cf-panel-confirm', function () {
        const id  = parseInt($(this).data('id'));
        const day = parseInt($(this).data('day'));
        const label = $(this).text().trim();
        closePanel();
        Swal.fire({
            title: `¿${label} este movimiento?`,
            text: 'Se registrará como transacción real en la cuenta.',
            icon: 'question',
            showCancelButton: true,
            confirmButtonText: `Sí, ${label.toLowerCase()}`,
            cancelButtonText: 'Cancelar',
            confirmButtonColor: label === 'Cobrar' ? '#198754' : '#fd7e14'
        }).then(result => {
            if (!result.isConfirmed) return;
            $.ajax({
                url: urlConfirmDay,
                type: 'POST',
                contentType: 'application/json',
                data: JSON.stringify({ ID: id, Day: day }),
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
                success: response => {
                    if (response.success) {
                        Swal.fire({ toast: true, position: 'top-end', icon: 'success', title: response.message || 'Movimiento confirmado', showConfirmButton: false, timer: 2500 });
                        if (window.reloadCashflowCalendar) window.reloadCashflowCalendar();
                        if (window.reloadCashflowBalances) window.reloadCashflowBalances();
                    } else {
                        Swal.fire('Error', response.message || 'No se pudo confirmar el movimiento', 'error');
                    }
                }
            });
        });
    });

    // Pagar gasto fijo desde panel
    $(document).on('click', '.cf-panel-pay-fe', function () {
        const id = parseInt($(this).data('id'));
        closePanel();
        Swal.fire({
            title: '¿Registrar pago?',
            text: 'Se registrará el pago de este gasto fijo.',
            icon: 'question',
            showCancelButton: true,
            confirmButtonText: 'Sí, pagar',
            cancelButtonText: 'Cancelar',
            confirmButtonColor: '#dc3545'
        }).then(result => {
            if (!result.isConfirmed) return;
            $.ajax({
                url: urlFixedPay,
                type: 'POST',
                contentType: 'application/json',
                data: JSON.stringify(id),
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
                success: response => {
                    if (response.success) {
                        Swal.fire({ toast: true, position: 'top-end', icon: 'success', title: response.message || 'Pago registrado', showConfirmButton: false, timer: 2500 });
                        if (window.reloadCashflowCalendar) window.reloadCashflowCalendar();
                        if (window.reloadCashflowBalances) window.reloadCashflowBalances();
                    } else {
                        Swal.fire('Error', response.message || 'No se pudo registrar el pago', 'error');
                    }
                }
            });
        });
    });

    // Cobrar ingreso fijo desde panel
    $(document).on('click', '.cf-panel-receive-fi', function () {
        const id = parseInt($(this).data('id'));
        const rawAmt = $(this).data('amount');
        const amount = rawAmt !== '' && rawAmt != null ? parseFloat(rawAmt) : null;
        closePanel();
        Swal.fire({
            title: '¿Registrar cobro?',
            text: 'Se registrará el ingreso en la cuenta asociada.',
            icon: 'question',
            showCancelButton: true,
            confirmButtonText: 'Sí, cobrar',
            cancelButtonText: 'Cancelar',
            confirmButtonColor: '#198754'
        }).then(result => {
            if (!result.isConfirmed) return;
            $.ajax({
                url: urlIncomeReceive,
                type: 'POST',
                contentType: 'application/json',
                data: JSON.stringify({ id, amount }),
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
                success: response => {
                    if (response.success) {
                        Swal.fire({ toast: true, position: 'top-end', icon: 'success', title: response.message || 'Cobro registrado', showConfirmButton: false, timer: 2500 });
                        if (window.reloadCashflowCalendar) window.reloadCashflowCalendar();
                        if (window.reloadCashflowBalances) window.reloadCashflowBalances();
                    } else {
                        Swal.fire('Error', response.message || 'No se pudo registrar el cobro', 'error');
                    }
                }
            });
        });
    });

    // Cobrar persona desde panel
    $(document).on('click', '.cf-panel-collect-person', async function () {
        const personId   = parseInt($(this).data('id'));
        const personName = $(this).data('name');
        const netOwed    = parseFloat($(this).data('amount'));
        closePanel();

        let accounts = [];
        try {
            const res  = await fetch(urlPersonPaymentAccounts);
            const json = await res.json();
            accounts   = json.data || [];
        } catch {}

        if (!accounts.length) {
            Swal.fire('Sin cuentas', 'No hay cuentas disponibles para acreditar el cobro.', 'warning');
            return;
        }

        const fmt = new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS' });
        const accountOptions = accounts.map(a => `<option value="${a.id}">${a.name} (${a.currency})</option>`).join('');

        const { value: formValues, isConfirmed } = await Swal.fire({
            title: `Cobrar a ${personName}`,
            html: `<div class="text-start">
                <div class="mb-3">
                    <label class="form-label small fw-semibold">Monto a cobrar</label>
                    <div class="input-group">
                        <span class="input-group-text">$</span>
                        <input id="cf-collect-amount" type="number" step="0.01"
                               class="form-control text-end fw-bold"
                               value="${netOwed.toFixed(2)}" min="0.01" />
                    </div>
                    <small class="text-muted">Saldo neto: <strong>${fmt.format(netOwed)}</strong></small>
                </div>
                <div class="mb-2">
                    <label class="form-label small fw-semibold">Acreditar en cuenta</label>
                    <select id="cf-collect-account" class="form-select">${accountOptions}</select>
                </div>
            </div>`,
            showCancelButton: true,
            confirmButtonText: '<i class="fas fa-check me-1"></i>Confirmar cobro',
            confirmButtonColor: '#198754',
            cancelButtonText: 'Cancelar',
            preConfirm: () => ({
                amount:    parseFloat(document.getElementById('cf-collect-amount').value) || 0,
                accountId: parseInt(document.getElementById('cf-collect-account').value) || 0
            })
        });

        if (!isConfirmed || !formValues || formValues.amount <= 0) return;

        $.ajax({
            url: urlPersonRegisterPayment,
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({ personID: personId, accountID: formValues.accountId, amount: formValues.amount }),
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            success: response => {
                if (response.success) {
                    Swal.fire({ toast: true, position: 'top-end', icon: 'success', title: 'Cobro registrado', showConfirmButton: false, timer: 2500 });
                    if (window.reloadCashflowCalendar) window.reloadCashflowCalendar();
                    if (window.reloadCashflowBalances) window.reloadCashflowBalances();
                    if (window.cargarCuentas) window.cargarCuentas();
                } else {
                    Swal.fire('Error', response.message || 'No se pudo registrar el cobro', 'error');
                }
            }
        });
    });

    // Agregar en el día del panel
    $('#cf-panel-add-btn').on('click', function () {
        const date = currentPanelDate ? currentPanelDate.date : null;
        closePanel();
        if (window.abrirModalTmpTransaction) window.abrirModalTmpTransaction(null, date);
    });

    // ── Inicializar calendario ────────────────────────────────
    $(document).ready(function () {
        initCalendar();
    });

    // Reparar dimensiones cuando el tab de Movimientos vuelve a ser visible
    $(document).on('shown.bs.tab', '#planificados-tab', function () {
        if (calendar) calendar.updateSize();
    });

    // Permitir que TmpTransaction.js navegue el calendario al mes activo del slider
    window.cashflowCalendarGotoMonth = function (year, month) {
        if (!calendar) return;
        navigatingFromSlider = true;
        calendar.gotoDate(new Date(year, month - 1, 1));
    };

    // Restringir navegación del calendario al rango del slider
    window.cashflowCalendarSetValidRange = function (start, end) {
        if (calendar) {
            calendar.setOption('validRange', { start, end });
        } else {
            pendingValidRange = { start, end };
        }
    };

    // Recargar el mes visible tras guardar/eliminar/confirmar
    window.reloadCashflowCalendar = function () {
        if (!calendar) return;
        const view = calendar.view;
        const mid = new Date(view.currentStart.getFullYear(), view.currentStart.getMonth(), 15);
        loadMonth(mid.getFullYear(), mid.getMonth() + 1);
    };

    function initCalendar() {
        const el = document.getElementById('financialCalendar');
        if (!el) return;

        calendar = new FullCalendar.Calendar(el, {
            initialView: 'dayGridMonth',
            locale: 'es',
            firstDay: 1,
            height: 'auto',
            headerToolbar: { left: 'prev,next today', center: 'title', right: '' },
            dayMaxEvents: 8,
            eventDisplay: 'block',
            datesSet: function (info) {
                const mid = new Date(info.view.currentStart.getFullYear(), info.view.currentStart.getMonth(), 15);
                const year  = mid.getFullYear();
                const month = mid.getMonth() + 1;

                loadMonth(year, month);

                // Sincronizar slider (solo si no fue disparado por el slider)
                if (!navigatingFromSlider) {
                    const key = `${year}-${String(month).padStart(2, '0')}`;
                    if (window.selectPlanningMonth) window.selectPlanningMonth(key);
                }
                navigatingFromSlider = false;
            },
            eventClick: function (info) {
                const day = balancesByDate[info.event.startStr];
                if (day) openPanel(day);
            },
            dateClick: function (info) {
                const day = balancesByDate[info.dateStr];
                if (day && day.items.length > 0) {
                    openPanel(day);
                } else if (day) {
                    // Día sin items: abrir directo para agregar
                    if (window.abrirModalTmpTransaction) window.abrirModalTmpTransaction(null, info.dateStr);
                }
            }
        });

        calendar.render();

        if (pendingValidRange) {
            calendar.setOption('validRange', pendingValidRange);
            pendingValidRange = null;
        }
    }

    function loadMonth(year, month) {
        $.get(urlDailyBalances, { year, month }, function (response) {
            if (!response.success || !response.data) return;
            renderMonth(response.data);
        }).fail(function () {
            Swal.fire('Error', 'No se pudo cargar el calendario de saldos', 'error');
        });
    }

    function renderMonth(data) {
        balancesByDate = {};
        const events = [];

        data.days.forEach(function (day) {
            balancesByDate[day.date] = day;
            day.items.forEach(function (item) {
                events.push({
                    title: `${item.isIncome ? '+' : '-'}${item.amountFmt} ${item.description}`,
                    start: day.date,
                    allDay: true,
                    classNames: [eventClassFor(item)]
                });
            });
        });

        calendar.removeAllEvents();
        calendar.addEventSource(events);

        setTimeout(function () { injectDayBalances(data.days); }, 60);
    }

    function eventClassFor(item) {
        if (item.sourceType === 'Transaccion')    return item.isIncome ? 'cf-evt-hist-income' : 'cf-evt-hist-expense';
        if (item.sourceType === 'TarjetaCredito') return 'cf-evt-tc';
        if (item.sourceType === 'Personas')       return 'cf-evt-personas';
        return item.isIncome ? 'cf-evt-income' : 'cf-evt-expense';
    }

    function injectDayBalances(days) {
        days.forEach(function (day) {
            const $cell = $(`.fc-daygrid-day[data-date="${day.date}"]`);
            if ($cell.length === 0) return;

            $cell.find('.cf-day-balance').remove();

            const cls = day.balance >= 0 ? 'text-success' : 'text-danger';
            $cell.find('.fc-daygrid-day-top').append(
                `<span class="cf-day-balance ${cls}">${fmtCompact.format(day.balance)}</span>`
            );
        });
    }
})();
