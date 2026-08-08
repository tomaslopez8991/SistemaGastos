(function () {
    const $container = $('#planning-container');
    if ($container.length === 0) return;

    const urlDailyBalances  = $container.data('url-daily-balances');
    const urlSetDayOverride = $container.data('url-set-day-override');
    const urlSetTcScenario  = $container.data('url-set-tc-scenario');
    const urlFixedPay              = $container.data('url-fixed-pay');
    const urlIncomeReceive         = $container.data('url-income-receive');
    const urlConfirmDay            = $container.data('url-confirm-day');
    const urlPersonRegisterPayment = $container.data('url-person-register-payment');
    const urlPayTc                 = $container.data('url-pay-tc');
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
        InteresEstimado:'Cuentas y obligaciones',
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

        const $balLabel = $('#cf-panel-balance-label');
        if (day.balanceFmt) {
            const balClass = day.balance >= 0 ? 'text-success' : 'text-danger';
            $balLabel.removeClass('text-success text-danger').addClass(balClass)
                .text('Saldo del día: ' + day.balanceFmt).show();
        } else {
            $balLabel.hide();
        }

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
            const typeLabel = item.isAutomaticPersonCollection
                ? sourceLabels.Personas
                : (sourceLabels[item.sourceType] || item.sourceType);
            const scenarioMode = item.tcProjectionMode || 'Total';
            const scenarioControls = item.tcAccountId ? `
                <div class="btn-group btn-group-sm mt-2 cf-tc-scenario" role="group" aria-label="Escenario de pago">
                    <button type="button" class="btn btn-outline-secondary cf-tc-scenario-btn ${scenarioMode === 'Minimo' ? 'active' : ''}"
                            data-tc-id="${item.tcAccountId}" data-mode="1">Mínimo</button>
                    <button type="button" class="btn btn-outline-secondary cf-tc-scenario-btn ${scenarioMode === 'Personalizado' ? 'active' : ''}"
                            data-tc-id="${item.tcAccountId}" data-mode="2" data-current="${item.tcCustomAmount || ''}">Personalizado</button>
                    <button type="button" class="btn btn-outline-secondary cf-tc-scenario-btn ${scenarioMode === 'Total' ? 'active' : ''}"
                            data-tc-id="${item.tcAccountId}" data-mode="0">Total</button>
                </div>` : '';

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
            } else if (item.sourceType === 'GastoFijo' && item.sourceId && item.tcAccountId) {
                // GastoFijo TC: tres botones de pago
                const minBtn = item.tcMinimumAmount > 0
                    ? `<button class="btn btn-xs btn-outline-warning cf-panel-tc-pay"
                               data-tc-id="${item.tcAccountId}" data-amount="${item.tcMinimumAmount}" data-label="Pagar mínimo">
                           <i class="fas fa-minus-circle me-1"></i>Mínimo
                       </button>`
                    : '';
                actions = `
                ${scenarioControls}
                <div class="cf-panel-item-actions d-flex gap-1 mt-2 flex-wrap">
                    <button class="btn btn-xs btn-outline-danger cf-panel-tc-pay"
                            data-tc-id="${item.tcAccountId}" data-fixed-expense-id="${item.sourceId}"
                            data-amount="${item.tcTotalAmount || item.amount}" data-label="Pagar total">
                        <i class="fas fa-credit-card me-1"></i>Total
                    </button>
                    ${minBtn}
                    <button class="btn btn-xs btn-outline-secondary cf-panel-tc-pay-custom"
                            data-tc-id="${item.tcAccountId}" data-fixed-expense-id="${item.sourceId}"
                            data-suggested="${item.amount}">
                        <i class="fas fa-pen me-1"></i>Personalizado
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
                            data-id="${item.sourceId}" data-amount="${amt}"
                            data-person-collection="${item.isAutomaticPersonCollection ? 'true' : 'false'}"
                            title="Registrar cobro">
                        <i class="fas fa-check me-1"></i>Cobrar
                    </button>
                </div>`;
            } else if (item.sourceType === 'TarjetaCredito' && item.tcAccountId) {
                const isCompletionPayment = item.description.startsWith('Completar TC');
                if (isCompletionPayment) {
                    actions = `
                    <div class="cf-panel-item-actions d-flex gap-1 mt-2 flex-wrap">
                        <button class="btn btn-xs btn-outline-success cf-panel-tc-pay"
                                data-tc-id="${item.tcAccountId}"
                                data-amount="${item.amount}"
                                data-label="Pagar cuota sugerida">
                            <i class="fas fa-check me-1"></i>Pagar
                        </button>
                        <button class="btn btn-xs btn-outline-secondary cf-panel-tc-pay-custom"
                                data-tc-id="${item.tcAccountId}"
                                data-suggested="${item.amount}">
                            <i class="fas fa-pen me-1"></i>Pagar otro monto
                        </button>
                    </div>`;
                } else {
                const minBtn = item.tcMinimumAmount > 0
                    ? `<button class="btn btn-xs btn-outline-warning cf-panel-tc-pay"
                               data-tc-id="${item.tcAccountId}"
                               data-amount="${item.tcMinimumAmount}"
                               data-label="Pago mínimo">
                           <i class="fas fa-minus-circle me-1"></i>Pagar mínimo
                       </button>`
                    : '';
                actions = `
                ${scenarioControls}
                <div class="cf-panel-item-actions d-flex gap-1 mt-2 flex-wrap">
                    ${minBtn}
                    <button class="btn btn-xs btn-outline-danger cf-panel-tc-pay-custom"
                            data-tc-id="${item.tcAccountId}"
                            data-suggested="${item.amount}">
                        <i class="fas fa-credit-card me-1"></i>Pagar otro monto
                    </button>
                </div>`;
                }
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
    $(document).on('click', '.cf-panel-receive-fi', async function () {
        const id = parseInt($(this).data('id'));
        const rawAmt = $(this).data('amount');
        const amount = rawAmt !== '' && rawAmt != null ? parseFloat(rawAmt) : null;
        const isPersonCollection = String($(this).data('person-collection')).toLowerCase() === 'true';
        const receiptDay = currentPanelDate?.day || null;
        closePanel();

        let accountSelector = '';
        if (isPersonCollection) {
            try {
                const res = await fetch(urlPersonPaymentAccounts, { credentials: 'same-origin' });
                if (!res.ok) throw new Error(`HTTP ${res.status}`);
                const json = await res.json();
                const accounts = json.data || [];
                if (!accounts.length) {
                    Swal.fire('Sin cuentas', 'No hay cuentas disponibles para acreditar el cobro.', 'warning');
                    return;
                }
                accountSelector = `<div class="text-start mt-3">
                    <label class="form-label small fw-semibold">Acreditar en cuenta</label>
                    <select id="cf-fi-receipt-account" class="form-select">
                        ${accounts.map(a => `<option value="${a.id}">${a.name} (${a.currency})</option>`).join('')}
                    </select>
                </div>`;
            } catch {
                Swal.fire('Error', 'No se pudieron cargar las cuentas disponibles para acreditar el cobro.', 'error');
                return;
            }
        }

        Swal.fire({
            title: '¿Registrar cobro?',
            html: isPersonCollection
                ? `Se registrará el ingreso en la cuenta seleccionada.${accountSelector}`
                : 'Se registrará el ingreso en la cuenta asociada.',
            icon: 'question',
            showCancelButton: true,
            confirmButtonText: 'Sí, cobrar',
            cancelButtonText: 'Cancelar',
            confirmButtonColor: '#198754',
            preConfirm: () => isPersonCollection
                ? parseInt(document.getElementById('cf-fi-receipt-account').value) || null
                : null
        }).then(result => {
            if (!result.isConfirmed) return;
            $.ajax({
                url: urlIncomeReceive,
                type: 'POST',
                contentType: 'application/json',
                data: JSON.stringify({ id, amount, accountID: result.value, day: receiptDay }),
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
                success: response => {
                    if (response.success) {
                        Swal.fire({ toast: true, position: 'top-end', icon: 'success', title: response.message || 'Cobro registrado', showConfirmButton: false, timer: 2500 });
                        if (window.reloadCashflowCalendar) window.reloadCashflowCalendar();
                        if (window.reloadCashflowBalances) window.reloadCashflowBalances();
                    } else {
                        Swal.fire('Error', response.message || 'No se pudo registrar el cobro', 'error');
                    }
                },
                error: xhr => {
                    const message = xhr.responseJSON?.message || 'No se pudo registrar el cobro.';
                    Swal.fire('Error', message, 'error');
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
            const res  = await fetch(urlPersonPaymentAccounts, { credentials: 'same-origin' });
            if (!res.ok) throw new Error(`HTTP ${res.status}`);
            const json = await res.json();
            accounts   = json.data || [];
        } catch {
            Swal.fire('Error', 'No se pudieron cargar las cuentas disponibles para acreditar el cobro.', 'error');
            return;
        }

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
            },
            error: xhr => {
                const message = xhr.responseJSON?.message || 'No se pudo registrar el cobro.';
                Swal.fire('Error', message, 'error');
            }
        });
    });

    // Pagar mínimo TC (monto pre-fijado)
    $(document).on('click', '.cf-tc-scenario-btn', async function () {
        const tcId = parseInt($(this).data('tc-id'));
        const mode = parseInt($(this).data('mode'));
        const date = new Date((currentPanelDate?.date || new Date().toISOString().slice(0, 10)) + 'T00:00:00');
        let customAmount = null;

        if (mode === 2) {
            const result = await Swal.fire({
                title: 'Monto personalizado',
                input: 'number',
                inputValue: $(this).data('current') || '',
                inputAttributes: { min: '0.01', step: '0.01' },
                showCancelButton: true,
                confirmButtonText: 'Aplicar escenario',
                cancelButtonText: 'Cancelar',
                inputValidator: value => (!value || parseFloat(value) <= 0) ? 'Ingresá un monto mayor a cero.' : null
            });
            if (!result.isConfirmed) return;
            customAmount = parseFloat(result.value);
        }

        $.ajax({
            url: urlSetTcScenario,
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({ accountID: tcId, year: date.getFullYear(), month: date.getMonth() + 1, mode, customAmount }),
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            success: response => {
                if (!response.success) {
                    Swal.fire('Error', response.message || 'No se pudo actualizar el escenario.', 'error');
                    return;
                }
                closePanel();
                if (window.reloadCashflowCalendar) window.reloadCashflowCalendar();
                if (window.reloadCashflowBalances) window.reloadCashflowBalances();
            },
            error: xhr => Swal.fire('Error', xhr.responseJSON?.message || 'No se pudo actualizar el escenario.', 'error')
        });
    });

    $(document).on('click', '.cf-panel-tc-pay', async function () {
        const tcId  = parseInt($(this).data('tc-id'));
        const amount = parseFloat($(this).data('amount'));
        const label  = $(this).data('label') || 'Pago';
        const fixedExpenseId = parseInt($(this).data('fixed-expense-id')) || null;
        const paymentDate = currentPanelDate?.date || new Date().toISOString().slice(0, 10);
        closePanel();
        await openTcPayModal(tcId, amount, label, false, fixedExpenseId, paymentDate);
    });

    // Pagar otro monto TC (monto editable)
    $(document).on('click', '.cf-panel-tc-pay-custom', async function () {
        const tcId     = parseInt($(this).data('tc-id'));
        const suggested = parseFloat($(this).data('suggested')) || 0;
        const fixedExpenseId = parseInt($(this).data('fixed-expense-id')) || null;
        const paymentDate = currentPanelDate?.date || new Date().toISOString().slice(0, 10);
        closePanel();
        await openTcPayModal(tcId, suggested, 'Pagar otro monto', true, fixedExpenseId, paymentDate);
    });

    async function openTcPayModal(tcId, amount, label, editable = false, fixedExpenseId = null, paymentDate = null) {
        let accounts = [];
        try {
            const res  = await fetch(urlPersonPaymentAccounts);
            const json = await res.json();
            accounts   = json.data || [];
        } catch {}

        if (!accounts.length) {
            Swal.fire('Sin cuentas', 'No hay cuentas disponibles para debitar el pago.', 'warning');
            return;
        }

        const fmt = new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS' });
        const accountOptions = accounts.map(a => `<option value="${a.id}">${a.name} (${a.currency})</option>`).join('');
        const defaultPaymentDate = paymentDate || new Date().toISOString().slice(0, 10);
        const maxPaymentDate = new Date().toISOString().slice(0, 10);
        const amountInput = editable
            ? `<input id="cf-tc-amount" type="number" step="0.01" min="0.01"
                      class="form-control text-end fw-bold" value="${amount.toFixed(2)}" />`
            : `<div class="form-control-plaintext fw-bold text-danger">${fmt.format(amount)}</div>
               <input type="hidden" id="cf-tc-amount" value="${amount.toFixed(2)}" />`;

        const { value: formValues, isConfirmed } = await Swal.fire({
            title: label,
            html: `<div class="text-start">
                <div class="mb-3">
                    <label class="form-label small fw-semibold">Monto a pagar</label>
                    ${amountInput}
                </div>
                <div class="mb-2">
                    <label class="form-label small fw-semibold">Débitar de cuenta</label>
                    <select id="cf-tc-account" class="form-select">${accountOptions}</select>
                </div>
                <div class="mt-3">
                    <label class="form-label small fw-semibold">Fecha del pago</label>
                    <input id="cf-tc-payment-date" type="date" class="form-control"
                           value="${defaultPaymentDate}" max="${maxPaymentDate}" />
                </div>
            </div>`,
            showCancelButton: true,
            confirmButtonText: '<i class="fas fa-check me-1"></i>Confirmar pago',
            confirmButtonColor: '#dc3545',
            cancelButtonText: 'Cancelar',
            preConfirm: () => {
                const paymentDate = document.getElementById('cf-tc-payment-date').value;
                if (!paymentDate) {
                    Swal.showValidationMessage('Seleccioná la fecha real del pago.');
                    return false;
                }
                return {
                    amount: parseFloat(document.getElementById('cf-tc-amount').value) || 0,
                    accountId: parseInt(document.getElementById('cf-tc-account').value) || 0,
                    paymentDate
                };
            }
        });

        if (!isConfirmed || !formValues || formValues.amount <= 0) return;

        $.ajax({
            url: urlPayTc,
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({
                tcAccountId: tcId,
                sourceAccountId: formValues.accountId,
                amount: formValues.amount,
                paymentDate: formValues.paymentDate,
                fixedExpenseId
            }),
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            success: response => {
                if (response.success) {
                    Swal.fire({ toast: true, position: 'top-end', icon: 'success', title: response.message || 'Pago registrado', showConfirmButton: false, timer: 2500 });
                    if (window.reloadCashflowCalendar) window.reloadCashflowCalendar();
                    if (window.reloadCashflowBalances) window.reloadCashflowBalances();
                    if (window.cargarCuentas) window.cargarCuentas();
                } else {
                    Swal.fire('Error', response.message || 'No se pudo registrar el pago', 'error');
                }
            },
            error: xhr => {
                const message = xhr.responseJSON?.message || 'No se pudo registrar el pago en el servidor';
                Swal.fire('Error', message, 'error');
            }
        });
    }

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
        // Mes anterior sin ítems pendientes → volver al mes actual
        if (data.isPastMonth && !data.hasPendingItems) {
            calendar.today();
            Swal.fire({
                toast: true, position: 'top-end', icon: 'info',
                title: 'No hay movimientos pendientes en ' + data.monthLabel,
                showConfirmButton: false, timer: 3000
            });
            return;
        }

        balancesByDate = {};
        const events = [];

        // Banner informativo para meses pasados con ítems
        $('#cf-past-month-banner').remove();
        if (data.isPastMonth) {
            const banner = `<div id="cf-past-month-banner" class="alert alert-warning alert-dismissible d-flex align-items-center gap-2 mb-2 py-2 px-3" role="alert">
                <i class="fas fa-exclamation-triangle"></i>
                <span>Ítems pendientes de <strong>${data.monthLabel}</strong>. Solo se muestran los movimientos sin acción.</span>
                <button type="button" class="btn-close ms-auto" data-bs-dismiss="alert"></button>
            </div>`;
            $('#financialCalendar').before(banner);
        }

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

        if (!data.isPastMonth) {
            setTimeout(function () { injectDayBalances(data.days); }, 60);
        }
    }

    function eventClassFor(item) {
        if (item.isAutomaticPersonCollection)      return 'cf-evt-personas';
        if (item.sourceType === 'Transaccion')    return item.isIncome ? 'cf-evt-hist-income' : 'cf-evt-hist-expense';
        if (item.sourceType === 'TarjetaCredito') return 'cf-evt-tc';
        if (item.sourceType === 'Personas')       return 'cf-evt-personas';
        if (item.sourceType === 'Transaccion')    return item.isIncome ? 'cf-evt-hist-income' : 'cf-evt-hist-expense';
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
