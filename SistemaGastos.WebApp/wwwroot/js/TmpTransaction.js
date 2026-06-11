(function () {
    const $container = $('#planning-container');
    if ($container.length === 0) return;

    const urls = {
        list:       $container.data('url-list'),
        form:       $container.data('url-form'),
        create:     $container.data('url-create'),
        delete:     $container.data('url-delete'),
        balances:   $container.data('url-balances'),
        confirm:    $container.data('url-confirm'),
        fixedList:  $container.data('url-fixed-list'),
        incomeList: $container.data('url-income-list')
    };

    const $carousel = $('#monthCarousel');
    const $prevBtn = $('#carouselPrev');
    const $nextBtn = $('#carouselNext');

    let currentYear = new Date().getFullYear();
    let currentMonth = new Date().getMonth() + 1;
    let currentGrid = null;
    let monthsData = [];
    let activeMonthKey = null;
    let activeDolarRate = 0; // cotización MEP del mes activo

    // INICIO
    init();

    function init() {
        loadBalances();
        setupCarouselNavigation();
        setupBreakdownCards();

        window.reloadCashflowBalances = loadBalances;
    }

    // ── Breakdown: abre modal Bootstrap con desglose ─────────────
    function setupBreakdownCards() {
        $(document).off('click', '.tmp-dash-card.clickable').on('click', '.tmp-dash-card.clickable', function () {
            if (!activeMonthKey) return;
            const [year, month] = activeMonthKey.split('-').map(Number);
            openBreakdownModal(year, month);
        });
    }

    function openBreakdownModal(year, month) {
        const monthLabel = new Date(year, month - 1, 1)
            .toLocaleDateString('es-AR', { month: 'long', year: 'numeric' });
        $('#breakdown-month-label').text(monthLabel);

        // Mostrar spinners mientras carga
        $('#breakdown-income-list, #breakdown-expense-list').html(
            '<div class="text-center py-3"><div class="spinner-border spinner-border-sm"></div></div>');
        $('#breakdown-income-total, #breakdown-expense-total').text('$ —');

        const modal = new bootstrap.Modal(document.getElementById('breakdownModal'));
        modal.show();

        loadBreakdown(year, month);
    }

    function loadBreakdown(year, month) {
        const fmt = new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS' });

        // 5 llamadas paralelas: movimientos, gastos fijos, ingresos fijos, personas, cuentas
        const callPlanned  = $.get(urls.list,       { year, month });
        const callFixed    = $.get(urls.fixedList,  { year, month });
        const callIncomes  = $.get(urls.incomeList, { year, month });
        const callPersons  = $.get($container.data('url-person-accounts'), { year, month });
        const callAccounts = $.get($container.data('url-person-payment-accounts'));

        // Cotización y datos de TC del mes activo
        const monthData    = monthsData.find(m => m.key === `${year}-${String(month).padStart(2, '0')}`);
        const dolarRate    = activeDolarRate || (monthData?.dolarRate ?? 0);
        const pendingTC    = monthData?.pendingInstallments ?? 0;
        const pendingTCFmt = monthData?.installmentsFmt ?? fmt.format(pendingTC);

        const toArs = (amount, currency) =>
            currency === 'USD' && dolarRate > 0 ? amount * dolarRate : amount;

        const fmtAmount = (amount, currency) => {
            if (currency === 'USD' && dolarRate > 0) {
                const arsEq = amount * dolarRate;
                return `USD ${amount.toLocaleString('es-AR', { minimumFractionDigits: 2 })} <span class="text-body-secondary small">(≈ ${fmt.format(arsEq)})</span>`;
            }
            return fmt.format(amount);
        };

        const makeRow = (label, sublabel, amount, currency, badgeText, badgeColor, colorClass) => {
            const amtHtml = fmtAmount(amount, currency);
            const badge = badgeText ? `<span class="breakdown-badge ${badgeColor}">${badgeText}</span>` : '';
            return `<div class="breakdown-row">
                <div class="breakdown-row-label">
                    <span class="text-body-emphasis">${label}</span>
                    ${badge}
                    ${sublabel ? `<span class="text-body-secondary" style="font-size:11px;">${sublabel}</span>` : ''}
                </div>
                <span class="breakdown-row-amount ${colorClass}">${amtHtml}</span>
            </div>`;
        };

        const emptySec = (msg) => `<p class="text-body-secondary small py-2 mb-0">${msg}</p>`;

        // Filtro para "efectivamente activo" en el mes seleccionado
        const selectedStart = new Date(year, month - 1, 1);
        const isEffectivelyActive = (item) => {
            if (!item.active) return false;
            if (item.startDate) {
                const sd = new Date(item.startDate);
                if (sd > selectedStart) return false;
            }
            return true;
        };

        $.when(callPlanned, callFixed, callIncomes, callPersons, callAccounts).done(function (r1, r2, r3, r4, r5) {
            // ── INGRESOS ──────────────────────────────────────────────
            const plannedIncome = (r1[0]?.data || []).filter(t => t.isIngreso);
            const fixedIncomes  = (r3[0]?.data || []).filter(f =>
                isEffectivelyActive(f) && !f.alreadyReceivedThisMonth);
            const personAccts   = (r4[0]?.data || []).filter(a => a.totalOwed > 0);
            // Cuentas con saldo positivo (liquidez disponible como punto de partida)
            const accounts      = (r5[0]?.data || []).filter(a => a.balance > 0);

            let incomeRows = '', incomeTotal = 0;

            if (accounts.length) {
                incomeRows += `<div class="breakdown-badge bg-primary bg-opacity-10 text-primary mb-2">Saldo en cuentas</div>`;
                accounts.forEach(a => {
                    const arsVal = toArs(a.balance, a.currency);
                    incomeTotal += arsVal;
                    incomeRows += makeRow(a.name, `Cuenta · ${a.currency}`, a.balance, a.currency, null, '', 'text-success');
                });
            }
            if (plannedIncome.length) {
                incomeRows += `<div class="breakdown-badge bg-success bg-opacity-10 text-success ${accounts.length ? 'mt-2 ' : ''}mb-2">Planificados</div>`;
                plannedIncome.forEach(t => {
                    incomeTotal += toArs(t.amount, t.currency);
                    incomeRows += makeRow(t.description, t.categoryName, t.amount, t.currency, null, '', 'text-success');
                });
            }
            if (fixedIncomes.length) {
                incomeRows += `<div class="breakdown-badge bg-success bg-opacity-10 text-success mt-2 mb-2">Ingresos fijos</div>`;
                fixedIncomes.forEach(f => {
                    incomeTotal += toArs(f.amount, f.currency);
                    incomeRows += makeRow(f.name, `Día ${f.receiptDay} · ${f.categoryName || ''}`, f.amount, f.currency, null, '', 'text-success');
                });
            }
            if (personAccts.length) {
                incomeRows += `<div class="breakdown-badge bg-warning bg-opacity-10 text-warning-emphasis mt-2 mb-2">A cobrar (personas)</div>`;
                personAccts.forEach(a => {
                    incomeTotal += a.totalOwed;
                    incomeRows += makeRow(a.personName, `${a.items.length} movimiento(s)`, a.totalOwed, 'ARS', null, '', 'text-success');
                });
            }

            if (!incomeRows) incomeRows = emptySec('Sin ingresos proyectados para este mes.');
            $('#breakdown-income-list').html(incomeRows);
            $('#breakdown-income-total').text(fmt.format(incomeTotal));

            // ── GASTOS ────────────────────────────────────────────────
            const plannedExpense = (r1[0]?.data || []).filter(t => !t.isIngreso);
            const fixedExpenses  = (r2[0]?.data || []).filter(f =>
                isEffectivelyActive(f) && !f.alreadyPaidThisMonth);

            let expenseRows = '', expenseTotal = 0;

            if (plannedExpense.length) {
                expenseRows += `<div class="breakdown-badge bg-danger bg-opacity-10 text-danger mb-2">Planificados</div>`;
                plannedExpense.forEach(t => {
                    const a = toArs(t.amount, t.currency);
                    expenseTotal += a;
                    expenseRows += makeRow(t.description, t.categoryName, t.amount, t.currency, null, '', 'text-danger');
                });
            }
            if (fixedExpenses.length) {
                expenseRows += `<div class="breakdown-badge bg-warning bg-opacity-10 text-warning mt-2 mb-2">Gastos fijos</div>`;
                fixedExpenses.forEach(f => {
                    const a = toArs(f.amount, f.currency);
                    expenseTotal += a;
                    expenseRows += makeRow(f.name, `Día ${f.paymentDay} · ${f.categoryName || ''}`, f.amount, f.currency, null, '', 'text-danger');
                });
            }
            if (pendingTC > 0) {
                expenseRows += `<div class="breakdown-badge bg-warning bg-opacity-10 text-warning mt-2 mb-2">Cuotas TC estimadas</div>`;
                expenseRows += makeRow('Tarjetas de crédito', 'Cuotas proyectadas para el mes', pendingTC, 'ARS', null, '', 'text-danger');
                expenseTotal += pendingTC;
            }

            if (!expenseRows) expenseRows = emptySec('Sin gastos proyectados para este mes.');
            $('#breakdown-expense-list').html(expenseRows);
            $('#breakdown-expense-total').text(fmt.format(expenseTotal));

        }).fail(() => {
            $('#breakdown-income-list, #breakdown-expense-list').html(
                `<div class="alert alert-danger small py-2 mb-0">Error al cargar el detalle.</div>`);
        });
    } // end loadBreakdown

    // CARGA DE BALANCES Y RENDERIZADO DEL CARRUSEL
    function loadBalances() {
        $.get(urls.balances, { year: currentYear }, function (response) {
            if (!response.success || !response.data) {
                $carousel.html('<div class="alert alert-danger w-100">Error cargando datos.</div>');
                return;
            }

            monthsData = response.data;

            if (monthsData.length === 0) {
                $carousel.html('<div class="text-muted w-100 text-center py-3">No hay proyecciones disponibles.</div>');
                return;
            }

            const mesActual = monthsData.find(m => m.month === currentMonth);
            if (mesActual && !activeMonthKey) {
                activeMonthKey = mesActual.key;
            } else if (!activeMonthKey) {
                activeMonthKey = monthsData[0].key;
            }

            renderMonthCarousel();
            actualizarDashboard();
            renderGrid();

            if (typeof window.cargarPresupuestos === 'function') {
                const [year, month] = activeMonthKey.split('-');
                window.cargarPresupuestos(year, month);
            }

            if (typeof window.cargarGastosFijos === 'function') {
                const [year, month] = activeMonthKey.split('-');
                window.cargarGastosFijos(year, month);
            }

            if (typeof window.cargarIngresosFijos === 'function') {
                const [year, month] = activeMonthKey.split('-');
                window.cargarIngresosFijos(year, month);
            }

            if (typeof window.cargarCuentas === 'function') {
                const [year, month] = activeMonthKey.split('-');
                window.cargarCuentas(year, month);
            }

        }).fail(function (xhr) {
            console.error("Error GetBalances:", xhr);
            Swal.fire('Error', 'No se pudieron cargar los balances mensuales', 'error');
        });
    }

    // RENDERIZAR CARRUSEL DE MESES
    function renderMonthCarousel() {
        $carousel.empty();

        monthsData.forEach(balance => {
            const isActive = balance.key === activeMonthKey ? 'active' : '';
            const amountClass = balance.balance >= 0 ? 'text-success' : 'text-danger';

            const html = `
                <div class="month-card ${isActive}" data-key="${balance.key}" data-month="${balance.month}">
                    <div class="month-label">${balance.label || balance.monthName}</div>
                    <h4 class="month-amount ${amountClass} mb-0">${balance.balanceFmt}</h4>
                </div>
            `;
            $carousel.append(html);
        });

        $('.month-card').off('click').on('click', function () {
            const key = $(this).data('key');
            selectMonth(key);
        });

        setTimeout(scrollToActiveMonth, 100);

        setTimeout(updateCarouselButtons, 150);
    }

    // SELECCIONAR MES
    function selectMonth(key) {
        activeMonthKey = key;

        $('.month-card').removeClass('active');
        $(`.month-card[data-key="${key}"]`).addClass('active');

        actualizarDashboard();
        renderGrid();

        updateCarouselButtons();

        if (typeof window.cargarPresupuestos === 'function') {
            const [year, month] = key.split('-');
            window.cargarPresupuestos(year, month);
        }

        if (typeof window.cargarGastosFijos === 'function') {
            const [year, month] = key.split('-');
            window.cargarGastosFijos(year, month);
        }

        if (typeof window.cargarIngresosFijos === 'function') {
            const [year, month] = key.split('-');
            window.cargarIngresosFijos(year, month);
        }

        if (typeof window.cargarCuentas === 'function') {
            const [year, month] = key.split('-');
            window.cargarCuentas(year, month);
        }
    }

    // SCROLL AUTOMÁTICO AL MES ACTIVO
    function scrollToActiveMonth() {
        const $activeCard = $('.month-card.active');
        if ($activeCard.length > 0) {
            const cardOffset = $activeCard.position().left;
            const carouselWidth = $carousel.width();
            const cardWidth = $activeCard.outerWidth();
            const scrollLeft = $carousel.scrollLeft() + cardOffset - (carouselWidth / 2) + (cardWidth / 2);

            $carousel.animate({ scrollLeft: scrollLeft }, 300, function () {
                updateCarouselButtons();
            });
        }
    }

    // CONFIGURAR NAVEGACIÓN DEL CARRUSEL
    function setupCarouselNavigation() {
        $prevBtn.off('click').on('click', function () {
            const scrollAmount = 200;
            $carousel.animate({
                scrollLeft: $carousel.scrollLeft() - scrollAmount
            }, 300, updateCarouselButtons);
        });

        $nextBtn.off('click').on('click', function () {
            const scrollAmount = 200;
            $carousel.animate({
                scrollLeft: $carousel.scrollLeft() + scrollAmount
            }, 300, updateCarouselButtons);
        });

        $carousel.off('scroll').on('scroll', function () {
            updateCarouselButtons();
        });
    }

    // ACTUALIZAR VISIBILIDAD DE FLECHAS
    function updateCarouselButtons() {
        if ($carousel.length === 0) return;

        const scrollLeft = $carousel.scrollLeft();
        const maxScroll = $carousel[0].scrollWidth - $carousel[0].clientWidth;

        if (scrollLeft <= 10) {
            $prevBtn.fadeOut(200);
        } else {
            $prevBtn.fadeIn(200);
        }

        if (scrollLeft >= maxScroll - 10) {
            $nextBtn.fadeOut(200);
        } else {
            $nextBtn.fadeIn(200);
        }
    }

    function actualizarDashboard() {
        const data = monthsData.find(m => m.key === activeMonthKey);
        if (!data) return;

        // Guardar el dolar rate del mes activo para usarlo en el modal de detalle
        if (data.dolarRate && data.dolarRate > 0) activeDolarRate = data.dolarRate;

        const culture = new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS' });

        $('#dash-balance').text(data.balanceFmt || culture.format(data.balance))
            .removeClass('text-success-emphasis text-danger-emphasis text-success text-danger')
            .addClass(data.balance >= 0 ? 'text-success-emphasis' : 'text-danger-emphasis');

        $('#dash-income').text(data.incomeFmt || culture.format(data.income));
        $('#dash-expense').text(data.expenseFmt || culture.format(data.expense));

        // (card "A cobrar personas" eliminada por requerimiento del usuario)

        $('#month-dashboard').fadeIn();

        if (typeof updateBalanceBreakdown === 'function') {
            updateBalanceBreakdown(data);
        }
    }

    // RENDERIZAR GRILLA
    function renderGrid() {
        if (!activeMonthKey) return;

        const [year, month] = activeMonthKey.split('-');
        const wrapper = document.getElementById("grid-wrapper");

        if (currentGrid) {
            currentGrid.updateConfig({
                server: {
                    url: `${urls.list}?year=${year}&month=${month}`,
                    then: response => {
                        const rows = mapGridData(response.data || response);
                        updateMovimientosBadge(rows.length);
                        return rows;
                    }
                }
            }).forceRender();
            return;
        }

        wrapper.innerHTML = '';
        currentGrid = new gridjs.Grid({
            columns: [
                {
                    id: 'check',
                    name: gridjs.html('<input type="checkbox" class="check-all form-check-input">'),
                    width: '66px',
                    sort: false,
                    formatter: (cell, row) => {
                        const isPaid = row.cells[5]?.data;

                        if (isPaid) return gridjs.html('<i class="fa-solid fa-check text-success ms-2"></i>');

                        return gridjs.html(`<input type="checkbox" class="form-check-input row-checkbox" value="${cell}">`);
                    }
                },
                { id: 'description', name: 'Descripción', width: '40%' },
                {
                    id: 'amount', name: 'Monto', width: '25%',
                    formatter: (cell) => gridjs.html(cell)
                },
                {
                    id: 'actions', name: 'Acciones', width: '20%', sort: false,
                    formatter: (cell, row) => {
                        const id = row.cells[4].data;
                        const isPaid = row.cells[5]?.data;

                        if (isPaid) {
                            return gridjs.html(`<span class="badge bg-success bg-opacity-10 text-success border border-success"><i class="fa-solid fa-check-double me-1"></i> Pagado</span>`);
                        }

                        return gridjs.html(`
                        <div class="d-flex justify-content-center gap-2">
                            <button class="btn btn-sm btn-success btn-confirmar" data-id="${id}" title="Confirmar">
                                <i class="fa-solid fa-check"></i>
                            </button>
                            <button class="btn btn-sm btn-primary btn-editar" data-id="${id}" title="Editar">
                                <i class="fa-solid fa-pen"></i>
                            </button>
                            <button class="btn btn-sm btn-danger btn-eliminar-individual" data-id="${id}" title="Eliminar">
                                <i class="fa-solid fa-trash"></i>
                            </button>
                        </div>
                    `);
                    }
                },
                { id: 'id', name: 'ID', hidden: true },
                { id: 'isPaid', name: 'IsPaid', hidden: true }
            ],
            rowAttributes: (row) => {
                if (row && row.cells[5]?.data === true) {
                    return {
                        'style': 'opacity: 0.55; background-color: rgba(var(--bs-success-rgb), 0.05); pointer-events: none;'
                    };
                }
                return {};
            },
            server: {
                url: `${urls.list}?year=${year}&month=${month}`,
                then: response => mapGridData(response.data || response)
            },
            className: { table: 'table table-hover table-striped align-middle mb-0' },
            pagination: false,
            language: {
                'search': { 'placeholder': '🔍 Buscar...' },
                'loading': 'Cargando...',
                'noRecordsFound': 'No hay transacciones para este mes'
            }
        }).render(wrapper);
    }

    function updateMovimientosBadge(count) {
        const $b = $('#badge-movimientos');
        if (count > 0) { $b.text(count).show(); } else { $b.hide(); }
    }

    // MAPEAR DATOS PARA LA GRILLA
    function mapGridData(responseData) {
        if (!Array.isArray(responseData)) return [];
        updateMovimientosBadge(responseData.length);

        return responseData.map(t => [
            t.id,
            t.description,
            `<span class="fw-bold ${t.isIngreso ? 'text-success' : 'text-danger'}">${t.amountFormatted}</span>`,
            null,
            t.id,
            t.isPaid
        ]);
    }

    // EVENTOS DE BOTONES
    $(document).off('click', '#btnCrearPlanificado').on('click', '#btnCrearPlanificado', function () {
        abrirModal();
    });

    $(document).off('click', '.btn-editar').on('click', '.btn-editar', function () {
        abrirModal($(this).data('id'));
    });

    $(document).off('click', '.btn-confirmar').on('click', '.btn-confirmar', function () {
        confirmarTransaccion($(this).data('id'));
    });

    $(document).off('click', '.btn-eliminar-individual').on('click', '.btn-eliminar-individual', function () {
        eliminarTransacciones([$(this).data('id')]);
    });

    $(document).off('click', '.btn-borrar-masivo').on('click', '.btn-borrar-masivo', function () {
        const ids = [];
        $('.row-checkbox:checked').each(function () { ids.push(parseInt($(this).val())); });
        if (ids.length > 0) eliminarTransacciones(ids);
    });

    $(document).on('change', '.row-checkbox, .check-all', function () {
        if ($(this).hasClass('check-all')) {
            $('.row-checkbox').prop('checked', $(this).is(':checked'));
        }
        $('.btn-borrar-masivo').prop('disabled', $('.row-checkbox:checked').length === 0);
    });

    // CONFIRMAR TRANSACCIÓN
    function confirmarTransaccion(id) {
        Swal.fire({
            title: '¿Confirmar transacción?',
            text: "Se actualizará el saldo de la cuenta.",
            icon: 'question',
            showCancelButton: true,
            confirmButtonText: 'Sí, confirmar',
            cancelButtonText: 'Cancelar'
        }).then((res) => {
            if (res.isConfirmed) {
                $.ajax({
                    url: urls.confirm,
                    type: 'POST',
                    contentType: 'application/json',
                    data: JSON.stringify(id),
                    headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
                    success: (response) => {
                        if (response.success) {
                            Swal.fire('Éxito', response.message || 'Confirmado correctamente', 'success');
                            loadBalances();
                        } else {
                            Swal.fire('Error', response.message || 'No se pudo confirmar', 'warning');
                        }
                    },
                    error: (xhr) => {
                        const response = xhr.responseJSON;
                        const errorMsg = response?.errors?.join(', ') || response?.message || 'Error al confirmar';
                        Swal.fire('Error', errorMsg, 'error');
                    }
                });
            }
        });
    }

    // ELIMINAR TRANSACCIONES
    function eliminarTransacciones(ids) {
        Swal.fire({
            title: '¿Eliminar transacciones?',
            text: `Se eliminarán ${ids.length} registro(s)`,
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#d33',
            confirmButtonText: 'Sí, eliminar',
            cancelButtonText: 'Cancelar'
        }).then((res) => {
            if (res.isConfirmed) {
                $.ajax({
                    url: urls.delete,
                    type: 'DELETE',
                    contentType: 'application/json',
                    data: JSON.stringify(ids),
                    headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
                    success: (response) => {
                        if (response.success) {
                            Swal.fire('Eliminado', response.message || 'Eliminado correctamente', 'success');
                            loadBalances();
                        } else {
                            Swal.fire('Error', response.message || 'No se pudo eliminar', 'error');
                        }
                    },
                    error: (xhr) => {
                        const response = xhr.responseJSON;
                        const errorMsg = response?.errors?.join(', ') || response?.message || 'Error al eliminar';
                        Swal.fire('Error', errorMsg, 'error');
                    }
                });
            }
        });
    }

    // ABRIR MODAL
    function abrirModal(id = null) {
        let url = urls.form;
        if (id) url += '?id=' + id;

        $.get(url, function (html) {
            Swal.fire({
                title: id ? 'Editar transacción' : 'Nueva transacción',
                html: html,
                width: '600px',
                showCancelButton: true,
                confirmButtonText: 'Guardar',
                cancelButtonText: 'Cancelar',
                didOpen: () => {
                    const popup = Swal.getPopup();
                    const $check = $(popup).find('#checkRecurrente');
                    const $panel = $(popup).find('#panel-meses');
                    const $bloque = $(popup).find('#bloque-fecha');
                    const $selMeses = $(popup).find('#selectMeses');
                    const $countLbl = $(popup).find('#monthsSelectedCount');
                    const $btnAll = $(popup).find('#btnSelectAllMonths');
                    const $btnClear = $(popup).find('#btnClearMonths');

                    // ── Conversión USD ──────────────────────────────────
                    const dolarRate = parseFloat($(popup).find('#Transaction_DolarMep').val()) || 0;
                    const fmtArs = new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS' });

                    function updateCurrencyUI() {
                        const currency = $(popup).find('#Transaction_CurrencySelect').val();
                        const amount = parseFloat($(popup).find('#Transaction_Amount').val()) || 0;
                        const $symbol = $(popup).find('#tmp-currency-symbol');
                        const $preview = $(popup).find('#tmp-usd-preview');
                        const $arsPreview = $(popup).find('#tmp-ars-preview');

                        if (currency === 'USD') {
                            $symbol.text('U$S');
                            $preview.removeClass('d-none');
                            $arsPreview.text(dolarRate > 0 ? fmtArs.format(amount * dolarRate) : '—');
                        } else {
                            $symbol.text('$');
                            $preview.addClass('d-none');
                        }
                    }

                    $(popup).find('#Transaction_CurrencySelect').on('change', updateCurrencyUI);
                    $(popup).find('#Transaction_Amount').on('input', updateCurrencyUI);
                    updateCurrencyUI();

                    // Estado inicial
                    if ($check.is(':checked')) {
                        $panel.show();
                        $bloque.hide();
                    }

                    // Toggle recurrencia
                    $check.on('change', function () {
                        if (this.checked) {
                            $panel.slideDown();
                            $bloque.slideUp();
                        } else {
                            $panel.slideUp();
                            $bloque.slideDown();
                            $selMeses.find('option').prop('selected', false);
                            $countLbl.text('0 meses seleccionados');
                        }
                    });

                    // Contador de meses seleccionados
                    $selMeses.on('change', function () {
                        const n = $(this).find('option:selected').length;
                        $countLbl.text(n === 0 ? 'Ningún mes seleccionado'
                            : n === 1 ? '1 mes seleccionado'
                                : `${n} meses seleccionados`);
                    });

                    // Botón "Todos"
                    $btnAll.on('click', function () {
                        $selMeses.find('option').prop('selected', true);
                        $selMeses.trigger('change');
                    });

                    // Botón "Ninguno"
                    $btnClear.on('click', function () {
                        $selMeses.find('option').prop('selected', false);
                        $selMeses.trigger('change');
                    });
                },

                preConfirm: () => {
                    const form = $('#form-planning');
                    const esRecurrente = form.find('#checkRecurrente').is(':checked');

                    // Lee el select multiple — compatible con MesesSeleccionados del backend
                    const mesesSeleccionados = esRecurrente
                        ? Array.from(form.find('#selectMeses')[0]?.selectedOptions || [])
                            .map(o => o.value)
                        : [];

                    return {
                        ID: parseInt(form.find('#Transaction_ID').val()) || 0,
                        Description: form.find('#Transaction_Description').val()?.trim(),
                        Amount: parseFloat(form.find('#Transaction_Amount').val()) || 0,
                        Currency: form.find('#Transaction_CurrencySelect').val() || 'ARS',
                        CategoryID: parseInt(form.find('#Transaction_CategoryID').val()) || 0,
                        AccountID: parseInt(form.find('#Transaction_AccountID').val()) || null,
                        DateTransaction: esRecurrente
                            ? null
                            : (form.find('#Transaction_DateTransaction').val() || null),
                        EsRecurrente: esRecurrente,
                        MesesSeleccionados: mesesSeleccionados
                    };
                }
            }).then((res) => {
                if (res.isConfirmed) guardar(res.value);
            });
        });
    }

    // GUARDAR TRANSACCIÓN
    function guardar(data) {
        if (!data.Description || data.Description.length === 0) {
            Swal.fire('Error', 'La descripción es obligatoria', 'error');
            return;
        }

        if (!data.CategoryID || data.CategoryID <= 0) {
            Swal.fire('Error', 'Debe seleccionar una categoría', 'error');
            return;
        }

        if (data.EsRecurrente) {
            if (!data.MesesSeleccionados || data.MesesSeleccionados.length === 0) {
                Swal.fire('Error', 'Debe seleccionar al menos un mes futuro', 'error');
                return;
            }
            data.DateTransaction = null;
        } else {
            if (!data.DateTransaction) {
                Swal.fire('Error', 'Debe indicar el mes de impacto', 'error');
                return;
            }
            data.MesesSeleccionados = [];
        }

        if (data.Amount <= 0) {
            Swal.fire('Error', 'El monto debe ser mayor a 0', 'error');
            return;
        }

        $.ajax({
            url: urls.create,
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(data),
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            success: (response) => {
                if (response.success) {
                    Swal.fire('Guardado', response.message || 'Transacción guardada correctamente', 'success');
                    loadBalances();
                } else {
                    const errorMsg = response.errors?.join(', ') || response.message || 'No se pudo guardar';
                    Swal.fire('Error', errorMsg, 'error');
                }
            },
            error: (xhr) => {
                const response = xhr.responseJSON;
                const errorMsg = response?.errors?.join(', ') || response?.message || 'Error de comunicación';
                Swal.fire('Error', errorMsg, 'error');
            }
        });
    }

    function updateBalanceBreakdown(data) {
        const breakdown = data.BalanceBreakdown || data.balanceBreakdown;
        if (!breakdown) return;

        const culture = new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS' });

        const initial = breakdown.InitialBalance ?? breakdown.initialBalance ?? 0;
        const income = breakdown.TotalIncome ?? breakdown.totalIncome ?? 0;
        const manualExp = breakdown.ManualExpenses ?? breakdown.manualExpenses ?? 0;
        const fixedExp = breakdown.FixedExpenses ?? breakdown.fixedExpenses ?? 0;
        const ccInstallments = breakdown.CreditCardInstallments ?? breakdown.creditCardInstallments ?? 0;
        const finalBalance = breakdown.FinalBalance ?? breakdown.finalBalance ?? 0;

        $('#breakdown-initial').text(culture.format(initial));
        $('#breakdown-income').text(culture.format(income));
        $('#breakdown-expenses').text(culture.format(manualExp));
        $('#breakdown-fixed').text(culture.format(fixedExp));
        $('#breakdown-installments').text(culture.format(ccInstallments));

        const finalClass = finalBalance >= 0 ? 'text-success-emphasis' : 'text-danger-emphasis';
        $('#breakdown-final').removeClass('text-success-emphasis text-danger-emphasis text-primary-emphasis')
            .addClass(finalClass)
            .text(culture.format(finalBalance));

        const fixedDetails = breakdown.FixedExpensesDetails || breakdown.fixedExpensesDetails || [];

        if (fixedDetails.length > 0) {
            let html = '';
            fixedDetails.forEach(expense => {
                const name = expense.Name || expense.name || 'Gasto Fijo';
                const category = expense.Category || expense.category || 'Varios';
                const amount = expense.Amount || expense.amount || 0;

                const dateStr = expense.NextPaymentDate || expense.nextPaymentDate;
                const dateObj = dateStr ? new Date(dateStr) : new Date();

                html += `
                <div class="list-group-item bg-transparent d-flex justify-content-between align-items-center border-secondary-subtle px-0 py-3">
                    <div>
                        <strong class="text-body-emphasis">${name}</strong>
                        <br>
                        <small class="text-body-secondary">
                            <i class="fa-solid fa-tag me-1"></i>${category}
                            <i class="fa-solid fa-calendar ms-2 me-1"></i>${dateObj.toLocaleDateString('es-AR')}
                        </small>
                    </div>
                    <span class="badge text-bg-warning rounded-pill fs-6">${culture.format(amount)}</span>
                </div>
                `;
            });
            $('#fixedExpensesItems').html(html);
            $('#fixedExpensesList').show();
        } else {
            $('#fixedExpensesList').hide();
        }
    }
})();