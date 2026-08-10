(function () {
    const $container = $('#planning-container');
    if ($container.length === 0) return;

    const urls = {
        list:       $container.data('url-list'),
        form:       $container.data('url-form'),
        create:     $container.data('url-create'),
        delete:     $container.data('url-delete'),
        balances:        $container.data('url-balances'),
        earliestPending: $container.data('url-earliest-pending'),
        confirmDay:      $container.data('url-confirm-day'),
        fixedList:  $container.data('url-fixed-list'),
        incomeList: $container.data('url-income-list')
    };

    const $carousel = $('#monthCarousel');
    const $prevBtn = $('#carouselPrev');
    const $nextBtn = $('#carouselNext');

    let currentYear = new Date().getFullYear();
    let currentMonth = new Date().getMonth() + 1;
    let monthsData = [];
    let activeMonthKey = null;

    // ── Inicialización ────────────────────────────────────────
    init();

    function init() {
        loadBalances();
        setupCarouselNavigation();
        window.reloadCashflowBalances = loadBalances;
    }

    // ── Carga de balances y renderizado del carrusel ──────────
    function loadBalances() {
        $.get(urls.balances, { year: currentYear }, function (response) {
            if (!response.success || !response.data) {
                $carousel.html('<div class="alert alert-danger w-100 small py-2">Error cargando datos.</div>');
                return;
            }

            monthsData = response.data;

            if (monthsData.length === 0) {
                $carousel.html('<div class="text-muted w-100 text-center py-2 small">No hay proyecciones disponibles.</div>');
                return;
            }

            const mesActual = monthsData.find(m => m.month === currentMonth);
            if (mesActual && !activeMonthKey) {
                activeMonthKey = mesActual.key;
            } else if (!activeMonthKey) {
                activeMonthKey = monthsData[0].key;
            }

            renderMonthCarousel();
            actualizarInfoBar();

            // Restringir navegación: fin = último mes con proyecciones, inicio = primer mes con TmpTransactions pasadas pendientes
            if (window.cashflowCalendarSetValidRange && monthsData.length > 0) {
                const lastKey  = monthsData[monthsData.length - 1].key;
                const [ly, lm] = lastKey.split('-').map(Number);
                const endYear  = lm === 12 ? ly + 1 : ly;
                const endMonth = lm === 12 ? 1 : lm + 1;
                const endStr   = `${endYear}-${String(endMonth).padStart(2, '0')}-01`;

                $.get(urls.earliestPending, function (res) {
                    let startStr = null;
                    if (res.success && res.data) {
                        const [ey, em] = res.data.split('-').map(Number);
                        startStr = `${ey}-${String(em).padStart(2, '0')}-01`;
                    }
                    window.cashflowCalendarSetValidRange(startStr, endStr);
                }).fail(function () {
                    window.cashflowCalendarSetValidRange(null, endStr);
                });
            }

            const [year, month] = activeMonthKey.split('-');
            notifyTabModules(year, month);

        }).fail(function (xhr) {
            console.error('Error GetBalances:', xhr);
            Swal.fire('Error', 'No se pudieron cargar los balances mensuales', 'error');
        });
    }

    // ── Renderizar carrusel slim de meses ─────────────────────
    function renderMonthCarousel() {
        $carousel.empty();

        monthsData.forEach(balance => {
            const isActive = balance.key === activeMonthKey ? 'active' : '';
            const amountClass = balance.balance >= 0 ? 'positive' : 'negative';

            const html = `
                <button type="button" class="month-card-slim ${isActive}" data-key="${balance.key}" data-month="${balance.month}"
                        aria-pressed="${balance.key === activeMonthKey}" aria-label="${balance.label || balance.monthName}: saldo ${balance.balanceFmt}">
                    <div class="mcs-label">${balance.label || balance.monthName}</div>
                    <div class="mcs-amount ${amountClass}">${balance.balanceFmt}</div>
                </button>
            `;
            $carousel.append(html);
        });

        $('.month-card-slim').off('click').on('click', function () {
            selectMonth($(this).data('key'));
        });

        setTimeout(scrollToActiveMonth, 100);
        setTimeout(updateCarouselButtons, 150);
    }

    // ── Seleccionar mes ───────────────────────────────────────
    function selectMonth(key) {
        activeMonthKey = key;

        $('.month-card-slim').removeClass('active').attr('aria-pressed', 'false');
        $(`.month-card-slim[data-key="${key}"]`).addClass('active').attr('aria-pressed', 'true');

        actualizarInfoBar();
        updateCarouselButtons();

        const [year, month] = key.split('-');
        notifyTabModules(year, month);

        // Sincronizar el calendario si está disponible
        if (window.cashflowCalendarGotoMonth) {
            window.cashflowCalendarGotoMonth(parseInt(year), parseInt(month));
        }
    }

    // Actualiza la vista del carrusel desde el calendario (sin re-disparar el calendario)
    window.selectPlanningMonth = function (key) {
        if (activeMonthKey === key) return;
        activeMonthKey = key;

        $('.month-card-slim').removeClass('active').attr('aria-pressed', 'false');
        $(`.month-card-slim[data-key="${key}"]`).addClass('active').attr('aria-pressed', 'true');

        actualizarInfoBar();
        scrollToActiveMonthSilent();

        const [year, month] = key.split('-');
        notifyTabModules(year, month);
    };

    function notifyTabModules(year, month) {
        if (typeof window.cargarPresupuestos === 'function')   window.cargarPresupuestos(year, month);
        if (typeof window.cargarGastosFijos === 'function')    window.cargarGastosFijos(year, month);
        if (typeof window.cargarIngresosFijos === 'function')  window.cargarIngresosFijos(year, month);
        if (typeof window.cargarCuentas === 'function')        window.cargarCuentas(year, month);
    }

    // ── Barra info compacta ───────────────────────────────────
    function actualizarInfoBar() {
        const data = monthsData.find(m => m.key === activeMonthKey);
        if (!data) return;

        const fmt = new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS' });

        $('#infobar-income').text(data.incomeFmt || fmt.format(data.income));
        $('#infobar-expense').text(data.expenseFmt || fmt.format(data.expense));

        const $bal = $('#infobar-balance');
        $bal.text(data.balanceFmt || fmt.format(data.balance))
            .removeClass('text-success text-danger')
            .addClass(data.balance >= 0 ? 'text-success' : 'text-danger');

        $('#month-info-bar').fadeIn(150);
    }

    // ── Scroll del carrusel ───────────────────────────────────
    function scrollToActiveMonth() {
        const $activeCard = $('.month-card-slim.active');
        if ($activeCard.length === 0) return;

        const cardOffset = $activeCard.position().left;
        const carouselWidth = $carousel.width();
        const cardWidth = $activeCard.outerWidth();
        const scrollLeft = $carousel.scrollLeft() + cardOffset - (carouselWidth / 2) + (cardWidth / 2);

        $carousel.animate({ scrollLeft: scrollLeft }, 300, updateCarouselButtons);
    }

    function scrollToActiveMonthSilent() {
        const $activeCard = $('.month-card-slim.active');
        if ($activeCard.length === 0) return;

        const cardOffset = $activeCard.position().left;
        const carouselWidth = $carousel.width();
        const cardWidth = $activeCard.outerWidth();
        const scrollLeft = $carousel.scrollLeft() + cardOffset - (carouselWidth / 2) + (cardWidth / 2);

        $carousel.scrollLeft(scrollLeft);
    }

    // ── Navegación del carrusel ───────────────────────────────
    function setupCarouselNavigation() {
        $prevBtn.off('click').on('click', function () {
            $carousel.animate({ scrollLeft: $carousel.scrollLeft() - 200 }, 300, updateCarouselButtons);
        });

        $nextBtn.off('click').on('click', function () {
            $carousel.animate({ scrollLeft: $carousel.scrollLeft() + 200 }, 300, updateCarouselButtons);
        });

        $carousel.off('scroll').on('scroll', updateCarouselButtons);
    }

    function updateCarouselButtons() {
        if ($carousel.length === 0) return;
        const scrollLeft = $carousel.scrollLeft();
        const maxScroll = $carousel[0].scrollWidth - $carousel[0].clientWidth;

        scrollLeft <= 10 ? $prevBtn.fadeOut(200) : $prevBtn.fadeIn(200);
        scrollLeft >= maxScroll - 10 ? $nextBtn.fadeOut(200) : $nextBtn.fadeIn(200);
    }

    // ── Botón "Nuevo" ─────────────────────────────────────────
    $(document).off('click', '#btnCrearPlanificado').on('click', '#btnCrearPlanificado', function () {
        abrirModal(null, null);
    });

    // ── API pública para CashflowCalendar ─────────────────────
    window.abrirModalTmpTransaction = function (id, fechaISO) {
        abrirModal(id, fechaISO);
    };

    window.eliminarTmpTransaction = function (ids) {
        eliminarTransacciones(ids);
    };

    window.confirmarTmpTransaction = function (id, day) {
        confirmarTransaccion(id, day);
    };

    // ── Confirmar transacción ─────────────────────────────────
    function confirmarTransaccion(id, day) {
        Swal.fire({
            title: '¿Confirmar movimiento?',
            text: 'Se registrará como transacción real y se actualizará el saldo de la cuenta.',
            icon: 'question',
            showCancelButton: true,
            confirmButtonText: 'Sí, confirmar',
            cancelButtonText: 'Cancelar'
        }).then((res) => {
            if (!res.isConfirmed) return;

            $.ajax({
                url: urls.confirmDay,
                type: 'POST',
                contentType: 'application/json',
                data: JSON.stringify({ ID: id, Day: day }),
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
                success: (response) => {
                    if (response.success) {
                        Swal.fire('Éxito', response.message || 'Confirmado correctamente', 'success');
                        loadBalances();
                        if (window.reloadCashflowCalendar) window.reloadCashflowCalendar();
                    } else {
                        Swal.fire('Error', response.message || 'No se pudo confirmar', 'warning');
                    }
                },
                error: (xhr) => {
                    const r = xhr.responseJSON;
                    Swal.fire('Error', r?.errors?.join(', ') || r?.message || 'Error al confirmar', 'error');
                }
            });
        });
    }

    // ── Eliminar transacciones ────────────────────────────────
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
            if (!res.isConfirmed) return;

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
                        if (window.reloadCashflowCalendar) window.reloadCashflowCalendar();
                    } else {
                        Swal.fire('Error', response.message || 'No se pudo eliminar', 'error');
                    }
                },
                error: (xhr) => {
                    const r = xhr.responseJSON;
                    Swal.fire('Error', r?.errors?.join(', ') || r?.message || 'Error al eliminar', 'error');
                }
            });
        });
    }

    // ── Abrir modal de creación/edición ───────────────────────
    function abrirModal(id, fechaISO) {
        let url = urls.form;
        if (id) url += '?id=' + id;

        let distributionPanel = null;

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

                    // Pre-rellenar fecha si viene del panel de día
                    if (fechaISO && !id) {
                        $(popup).find('#Transaction_DateTransaction').val(fechaISO);
                    }

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

                    if ($check.is(':checked')) { $panel.show(); $bloque.hide(); }

                    $check.on('change', function () {
                        if (this.checked) {
                            $panel.slideDown(); $bloque.slideUp();
                        } else {
                            $panel.slideUp(); $bloque.slideDown();
                            $selMeses.find('option').prop('selected', false);
                            $countLbl.text('0 meses seleccionados');
                        }
                    });

                    $selMeses.on('change', function () {
                        const n = $(this).find('option:selected').length;
                        $countLbl.text(n === 0 ? 'Ningún mes seleccionado'
                            : n === 1 ? '1 mes seleccionado'
                                : `${n} meses seleccionados`);
                    });

                    $btnAll.on('click', function () {
                        $selMeses.find('option').prop('selected', true);
                        $selMeses.trigger('change');
                    });

                    $btnClear.on('click', function () {
                        $selMeses.find('option').prop('selected', false);
                        $selMeses.trigger('change');
                    });

                    // ── Panel de distribución diaria ────────────────────
                    const $checkDist = $(popup).find('#checkDistribuir');
                    const initialEndDay = parseInt($checkDist.data('initial-end-day')) || null;
                    const initialExcludedDays = ($checkDist.data('initial-excluded') ?? '')
                        .toString().split(',').map(s => parseInt(s)).filter(n => !isNaN(n));

                    function getStartDay() {
                        const val = $(popup).find('#Transaction_DateTransaction').val();
                        if (!val) return null;
                        const day = parseInt(val.split('-')[2]);
                        return isNaN(day) ? null : day;
                    }

                    function getMonthYear() {
                        const val = $(popup).find('#Transaction_DateTransaction').val();
                        if (!val) return {};
                        const parts = val.split('-');
                        return { year: parseInt(parts[0]), month: parseInt(parts[1]) };
                    }

                    distributionPanel = DistributionPanel.init($(popup), {
                        checkboxSelector: '#checkDistribuir',
                        panelSelector: '#panel-distribucion',
                        gridSelector: '#distribucion-dias-grid',
                        presetSelector: '.tmp-day-preset',
                        holidayStatusSelector: '#distribucion-feriados-status',
                        holidaysUrl: '/TmpTransaction/GetHolidays',
                        getStartDay,
                        getMonthYear,
                        initialEndDay,
                        initialExcludedDays
                    });

                    $(popup).find('#Transaction_DateTransaction').on('change', () => distributionPanel.refresh());
                },

                preConfirm: () => {
                    const form = $('#form-planning');
                    const esRecurrente = form.find('#checkRecurrente').is(':checked');

                    const mesesSeleccionados = esRecurrente
                        ? Array.from(form.find('#selectMeses')[0]?.selectedOptions || []).map(o => o.value)
                        : [];

                    const dist = distributionPanel
                        ? distributionPanel.getValues()
                        : { distributionEndDay: null, excludedDays: null };

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
                        MesesSeleccionados: mesesSeleccionados,
                        DistributionEndDay: dist.distributionEndDay,
                        ExcludedDays: dist.excludedDays
                    };
                }
            }).then((res) => {
                if (res.isConfirmed) guardar(res.value);
            });
        });
    }

    // ── Guardar transacción ───────────────────────────────────
    function guardar(data) {
        if (!data.Description || data.Description.length === 0) {
            Swal.fire('Error', 'La descripción es obligatoria', 'error'); return;
        }
        if (!data.CategoryID || data.CategoryID <= 0) {
            Swal.fire('Error', 'Debe seleccionar una categoría', 'error'); return;
        }
        if (data.EsRecurrente) {
            if (!data.MesesSeleccionados || data.MesesSeleccionados.length === 0) {
                Swal.fire('Error', 'Debe seleccionar al menos un mes futuro', 'error'); return;
            }
            data.DateTransaction = null;
        } else {
            if (!data.DateTransaction) {
                Swal.fire('Error', 'Debe indicar el mes de impacto', 'error'); return;
            }
            data.MesesSeleccionados = [];
        }
        if (data.Amount <= 0) {
            Swal.fire('Error', 'El monto debe ser mayor a 0', 'error'); return;
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
                    if (window.reloadCashflowCalendar) window.reloadCashflowCalendar();
                } else {
                    Swal.fire('Error', response.errors?.join(', ') || response.message || 'No se pudo guardar', 'error');
                }
            },
            error: (xhr) => {
                const r = xhr.responseJSON;
                Swal.fire('Error', r?.errors?.join(', ') || r?.message || 'Error de comunicación', 'error');
            }
        });
    }
})();
