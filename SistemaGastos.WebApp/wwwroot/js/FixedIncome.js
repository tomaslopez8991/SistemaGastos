(function () {
    const $container = $('#planning-container');
    if ($container.length === 0) return;

    const urls = {
        list:    $container.data('url-income-list'),
        form:    $container.data('url-income-form'),
        save:    $container.data('url-income-save'),
        receive: $container.data('url-income-receive'),
        delete:  $container.data('url-income-delete'),
        toggle:  $container.data('url-income-toggle')
    };

    let incomesData = [];
    let activeYear  = new Date().getFullYear();
    let activeMonth = new Date().getMonth() + 1;

    init();

    function init() {
        loadIncomes(activeYear, activeMonth);
        $('#btnNewIncome').on('click', e => { e.preventDefault(); openModal(); });
    }

    window.cargarIngresosFijos = function (year, month) {
        activeYear  = parseInt(year);
        activeMonth = parseInt(month);
        loadIncomes(activeYear, activeMonth);
    };

    // ── Días desde hoy hasta el cobro en el MES SELECCIONADO ───
    function calcDaysUntilReceipt(income, targetYear, targetMonth) {
        const day = income.receiptDay;
        if (!day) return null;
        const today = new Date(); today.setHours(0,0,0,0);
        const due = new Date(targetYear, targetMonth - 1, day);
        return Math.round((due - today) / 86400000);
    }

    // ── Badge de estado ─────────────────────────────────────────
    function getStatusBadge(income) {
        const selectedMonthStart = new Date(activeYear, activeMonth - 1, 1);
        const startDate = income.startDate ? new Date(income.startDate) : null;
        const notStartedYet = startDate !== null && startDate > selectedMonthStart;

        if (!income.active)
            return '<span class="badge text-bg-secondary"><i class="fas fa-pause me-1"></i>En pausa</span>';
        if (notStartedYet) {
            const from = startDate.toLocaleDateString('es-AR', { month: 'short', year: '2-digit' });
            return `<span class="badge text-bg-secondary"><i class="fas fa-clock me-1"></i>Desde ${from}</span>`;
        }
        if (income.alreadyReceivedThisMonth)
            return '<span class="badge text-bg-success"><i class="fas fa-check-circle me-1"></i>Cobrado</span>';
        const days = calcDaysUntilReceipt(income, activeYear, activeMonth);
        if (days === null) return '<span class="badge text-bg-secondary">—</span>';
        if (days === 0)  return '<span class="badge text-bg-success"><i class="fas fa-circle-check me-1"></i>Cobrar hoy</span>';
        if (days <= 3)   return `<span class="badge text-bg-warning"><i class="fas fa-clock me-1"></i>${days}d para cobrar</span>`;
        return `<span class="badge text-bg-light text-dark border">${days} días</span>`;
    }

    // ── Cargar y renderizar ─────────────────────────────────────
    function loadIncomes(year, month) {
        $.get(urls.list, { year, month }, function (response) {
            if (!response.success || !response.data) { showError(); return; }
            incomesData = response.data;
            updateTabBadge();
            renderIncomes(incomesData);
        }).fail(showError);
    }

    function showError() {
        $('#incomes-grid').html(`
            <div class="col-12">
                <div class="alert alert-danger">
                    <i class="fas fa-exclamation-triangle me-2"></i>
                    Error al cargar ingresos fijos. <a href="#" onclick="location.reload()">Reintentar</a>
                </div>
            </div>`);
    }

    function updateTabBadge() {
        const pending = incomesData.filter(e => e.active && !e.alreadyReceivedThisMonth).length;
        const $badge = $('#badge-pending-income');
        $badge.text(pending);
        $badge.toggleClass('tmp-badge-success', pending > 0)
              .toggleClass('text-bg-secondary', pending === 0);
    }

    function renderIncomes(incomes) {
        if (incomes.length === 0) {
            $('#incomes-grid').html(`
                <div class="col-12">
                    <div class="alert alert-info border-0 shadow-sm">
                        <i class="fas fa-info-circle me-2"></i>
                        No hay ingresos fijos registrados. Usá <strong>+ Nuevo → Ingreso fijo</strong> para agregar uno.
                    </div>
                </div>`);
            return;
        }

        const fmt = new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS' });
        let html = '';

        incomes.forEach(income => {
            const isReceived = !!income.alreadyReceivedThisMonth;
            const isActive   = !!income.active;

            const selectedMonthStart = new Date(activeYear, activeMonth - 1, 1);
            const startDate = income.startDate ? new Date(income.startDate) : null;
            const notStartedYet = startDate !== null && startDate > selectedMonthStart;
            const effectivelyActive = isActive && !notStartedYet;

            const days = effectivelyActive ? calcDaysUntilReceipt(income, activeYear, activeMonth) : null;

            let cardClass = '';
            if (isReceived)            cardClass = 'fi-card-received';
            else if (!effectivelyActive) cardClass = 'fe-card-paused';
            else if (days === 0)       cardClass = 'fi-card-today';
            else if (days !== null && days <= 3) cardClass = 'fe-card-urgent';

            const logoHtml = income.logoUrl
                ? `<img src="${income.logoUrl}" alt="${income.name}" class="rounded" style="width:40px;height:40px;object-fit:cover;">`
                : `<div class="rounded bg-success-subtle d-flex align-items-center justify-content-center" style="width:40px;height:40px;">
                       <i class="fas fa-coins text-success"></i>
                   </div>`;

            // StartDate badge
            const startDateHtml = income.startDate
                ? `<div class="d-flex align-items-center gap-2 mb-2">
                       <i class="fas fa-calendar-plus" style="width:16px;"></i>
                       <span>Desde ${new Date(income.startDate).toLocaleDateString('es-AR', { month: 'short', year: '2-digit' })}</span>
                   </div>`
                : '';

            let daysHtml = '';
            if (!effectivelyActive) {
                const pauseLabel = notStartedYet
                    ? `Activo desde ${startDate.toLocaleDateString('es-AR', { month: 'short', year: '2-digit' })}`
                    : 'Ingreso en pausa';
                daysHtml = `<div class="d-flex align-items-center gap-2 text-secondary"><i class="fas fa-pause-circle" style="width:16px;"></i><span>${pauseLabel}</span></div>`;
            } else if (!isReceived && days !== null) {
                let cls = 'text-body-secondary', icon = 'fa-calendar-day', text = days < 0
                    ? `Venció hace ${Math.abs(days)} día${Math.abs(days) !== 1 ? 's' : ''}`
                    : `Cobra en ${days} día${days !== 1 ? 's' : ''}`;
                if (days < 0) { cls = 'text-danger fw-semibold'; icon = 'fa-circle-exclamation'; }
                else if (days === 0) { cls = 'text-success fw-semibold'; icon = 'fa-circle-check'; text = 'Cobrar hoy'; }
                else if (days <= 3) { cls = 'text-warning-emphasis fw-semibold'; icon = 'fa-clock'; }
                daysHtml = `<div class="d-flex align-items-center gap-2 ${cls}"><i class="fas ${icon}" style="width:16px;"></i><span>${text}</span></div>`;
            }

            let actionsHtml = '';
            if (isReceived) {
                actionsHtml = `<div class="fi-received-footer mt-3">
                    <i class="fas fa-circle-check"></i>
                    <span>Cobrado en ${income.receivedMonthName || 'este mes'}</span>
                </div>`;
            } else if (effectivelyActive) {
                actionsHtml = `
                    <div class="d-flex gap-2 mt-auto">
                        <button class="btn btn-success btn-sm flex-fill fw-bold" onclick="receiveIncome(${income.id})">
                            <i class="fas fa-check me-1"></i>Cobrar
                        </button>
                        <button class="btn btn-outline-primary btn-sm" onclick="editIncome(${income.id})" title="Editar">
                            <i class="fas fa-pen"></i>
                        </button>
                        <button class="btn btn-outline-warning btn-sm" onclick="toggleIncome(${income.id}, true, ${activeYear}, ${activeMonth})" title="Pausar">
                            <i class="fas fa-pause"></i>
                        </button>
                        <button class="btn btn-outline-danger btn-sm" onclick="deleteIncome(${income.id})" title="Eliminar">
                            <i class="fas fa-trash"></i>
                        </button>
                    </div>`;
            } else if (notStartedYet) {
                actionsHtml = `
                    <div class="d-flex gap-2 mt-auto">
                        <button class="btn btn-outline-secondary btn-sm flex-fill" onclick="editIncome(${income.id})">
                            <i class="fas fa-pen me-1"></i>Editar
                        </button>
                        <button class="btn btn-outline-danger btn-sm" onclick="deleteIncome(${income.id})"><i class="fas fa-trash"></i></button>
                    </div>`;
            } else {
                actionsHtml = `
                    <div class="d-flex gap-2 mt-auto">
                        <button class="btn btn-primary btn-sm flex-fill fw-bold" onclick="toggleIncome(${income.id}, false, ${activeYear}, ${activeMonth})">
                            <i class="fas fa-play me-1"></i>Reanudar desde este mes
                        </button>
                        <button class="btn btn-outline-secondary btn-sm" onclick="editIncome(${income.id})"><i class="fas fa-pen"></i></button>
                        <button class="btn btn-outline-danger btn-sm" onclick="deleteIncome(${income.id})"><i class="fas fa-trash"></i></button>
                    </div>`;
            }

            html += `
                <div class="col-md-6 col-lg-4">
                    <div class="card border-0 shadow-sm h-100 expense-card ${cardClass}">
                        <div class="card-body d-flex flex-column">
                            <div class="d-flex justify-content-between align-items-start mb-3">
                                <div class="d-flex align-items-center gap-3">
                                    ${logoHtml}
                                    <div>
                                        <h6 class="mb-0 fw-bold text-body-emphasis">${income.name}</h6>
                                        <small class="text-body-secondary">${income.categoryName}</small>
                                    </div>
                                </div>
                                ${getStatusBadge(income)}
                            </div>

                            <div class="mb-3">
                                <div class="text-body-secondary small mb-1">Ingreso mensual</div>
                                <h4 class="fw-bold mb-0 ${isReceived ? 'text-success' : !isActive ? 'text-secondary' : 'text-success-emphasis'}">
                                    ${income.amountFormatted || fmt.format(income.amount)}
                                </h4>
                                ${income.currency === 'USD'
                                    ? `<small class="badge bg-info-subtle text-info-emphasis">USD</small>`
                                    : ''}
                            </div>

                            <div class="mb-3 small text-body-secondary">
                                <div class="d-flex align-items-center gap-2 mb-2">
                                    <i class="fas fa-calendar-day" style="width:16px;"></i>
                                    <span>Día ${income.receiptDay}</span>
                                </div>
                                <div class="d-flex align-items-center gap-2 mb-2">
                                    <i class="fas fa-wallet" style="width:16px;"></i>
                                    <span>${income.accountName}</span>
                                </div>
                                ${startDateHtml}
                                ${daysHtml}
                            </div>

                            ${actionsHtml}
                        </div>
                    </div>
                </div>`;
        });

        $('#incomes-grid').html(html);
    }

    function notifyChanged() {
        loadIncomes(activeYear, activeMonth);
        if (window.reloadCashflowBalances) window.reloadCashflowBalances();
    }

    // ── Funciones globales (onclick en HTML) ────────────────────
    window.editIncome = id => openModal(id);

    window.receiveIncome = function (id) {
        const income = incomesData.find(e => e.id === id);
        if (!income) return;
        const fmt = new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS' });
        Swal.fire({
            title: '¿Confirmar cobro?',
            html: `<div class="text-start">
                    <p><strong>Ingreso:</strong> ${income.name}</p>
                    <p><strong>Monto:</strong> ${income.amountFormatted || fmt.format(income.amount)}</p>
                    <p><strong>Cuenta:</strong> ${income.accountName}</p>
                   </div>`,
            icon: 'question',
            showCancelButton: true,
            confirmButtonText: 'Sí, cobrar',
            cancelButtonText: 'Cancelar',
            confirmButtonColor: '#198754'
        }).then(result => {
            if (!result.isConfirmed) return;
            $.ajax({
                url: urls.receive,
                type: 'POST',
                contentType: 'application/json',
                data: JSON.stringify(id),
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
                success: response => {
                    if (response.success) {
                        Swal.fire({ title: 'Cobrado', icon: 'success', timer: 1500, showConfirmButton: false });
                        notifyChanged();
                    } else {
                        Swal.fire('Error', response.message, 'error');
                    }
                }
            });
        });
    };

    // currentActive = true → pausando; false → reanudando desde el mes seleccionado
    window.toggleIncome = function (id, currentActive, year, month) {
        const body = currentActive
            ? { ID: id }
            : { ID: id, Year: year, Month: month };
        $.ajax({
            url: urls.toggle,
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(body),
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            success: response => { if (response.success) notifyChanged(); }
        });
    };

    window.deleteIncome = function (id) {
        Swal.fire({
            title: '¿Eliminar ingreso?',
            icon: 'warning',
            showCancelButton: true,
            confirmButtonText: 'Sí, eliminar',
            confirmButtonColor: '#dc3545'
        }).then(result => {
            if (!result.isConfirmed) return;
            $.ajax({
                url: `${urls.delete}/${id}`,
                type: 'DELETE',
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
                success: response => {
                    if (response.success || response.succeeded) {
                        Swal.fire({ icon: 'success', title: 'Eliminado', showConfirmButton: false, timer: 1000 });
                        notifyChanged();
                    } else {
                        Swal.fire('Error', response.message || 'Error al eliminar', 'error');
                    }
                },
                error: () => Swal.fire('Error', 'No se pudo comunicar con el servidor', 'error')
            });
        });
    };

    function openModal(id = null) {
        const url = id ? `${urls.form}/${id}` : `${urls.form}/`;
        $.get(url, function (html) {
            Swal.fire({
                title: id ? 'Editar ingreso fijo' : 'Nuevo ingreso fijo',
                html,
                width: '600px',
                showCancelButton: true,
                confirmButtonText: 'Guardar',
                cancelButtonText: 'Cancelar',
                confirmButtonColor: '#198754',
                preConfirm: () => {
                    const form = $('#incomeForm');
                    return {
                        ID:          parseInt(form.find('#FI_ID').val()) || 0,
                        Name:        form.find('#FI_Name').val()?.trim(),
                        Amount:      parseFloat(form.find('#FI_Amount').val()) || 0,
                        Currency:    form.find('#FI_Currency').val() || 'ARS',
                        ReceiptDay:  parseInt(form.find('#FI_ReceiptDay').val()) || 1,
                        CategoryID:  parseInt(form.find('#FI_CategoryID').val()) || 0,
                        AccountID:   parseInt(form.find('#FI_AccountID').val()) || 0,
                        LogoUrl:     form.find('#FI_LogoUrl').val()?.trim() || null,
                        StartDate:   form.find('#FI_StartDate').val() || null,
                        Active:      true
                    };
                }
            }).then(result => {
                if (result.isConfirmed) saveIncome(result.value);
            });
        });
    }

    function saveIncome(data) {
        if (!data.Name || data.Amount <= 0 || data.CategoryID <= 0 || data.AccountID <= 0) {
            Swal.fire('Error', 'Completá nombre, monto, cuenta y categoría', 'error');
            return;
        }
        $.ajax({
            url: urls.save,
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(data),
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            success: response => {
                if (response.success) {
                    Swal.fire({ title: 'Guardado', icon: 'success', timer: 1500, showConfirmButton: false });
                    notifyChanged();
                } else {
                    Swal.fire('Error', response.message, 'error');
                }
            }
        });
    }

})();
