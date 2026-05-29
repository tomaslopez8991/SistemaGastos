(function () {
    const $container = $('#planning-container');
    if ($container.length === 0) return;

    const urls = {
        list: $container.data('url-list'),
        form: $container.data('url-form'),
        create: $container.data('url-create'),
        delete: $container.data('url-delete'),
        balances: $container.data('url-balances'),
        confirm: $container.data('url-confirm')
    };

    const $carousel = $('#monthCarousel');
    const $prevBtn = $('#carouselPrev');
    const $nextBtn = $('#carouselNext');

    let currentYear = new Date().getFullYear();
    let currentMonth = new Date().getMonth() + 1;
    let currentGrid = null;
    let monthsData = [];
    let activeMonthKey = null;

    // INICIO
    init();

    function init() {
        loadBalances();
        setupCarouselNavigation();

        window.reloadCashflowBalances = loadBalances;
    }

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

        const culture = new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS' });

        $('#dash-balance').text(data.balanceFmt || culture.format(data.balance))
            .removeClass('text-success-emphasis text-danger-emphasis text-success text-danger')
            .addClass(data.balance >= 0 ? 'text-success-emphasis' : 'text-danger-emphasis');

        $('#dash-income').text(data.incomeFmt);
        $('#dash-expense').text(data.expenseFmt);

        const now = new Date();
        const next = new Date(now.getFullYear(), now.getMonth() + 1, 1);
        const nextKey = `${next.getFullYear()}-${String(next.getMonth() + 1).padStart(2, '0')}`;
        const nextData = monthsData.find(m => m.key === nextKey);

        if (nextData && (nextData.installmentsFmt || nextData.pendingInstallments)) {
            $('#dash-installments').text(nextData.installmentsFmt || culture.format(nextData.pendingInstallments));
        } else if (data.installmentsFmt) {
            $('#dash-installments').text(data.installmentsFmt);
        } else if (data.BalanceBreakdown && data.BalanceBreakdown.CreditCardInstallments !== undefined) {
            $('#dash-installments').text(culture.format(data.BalanceBreakdown.CreditCardInstallments));
        } else {
            $('#dash-installments').text('$ 0,00');
        }

        if (data.BalanceBreakdown && data.BalanceBreakdown.FixedExpenses !== undefined) {
            $('#dash-fixed-subtotal').text(culture.format(data.BalanceBreakdown.FixedExpenses));
        }

        const hasTotalPayment = (nextData && nextData.hasCreditCardPayment) || data.hasCreditCardPayment;
        if (hasTotalPayment) {
            $('#tc-payment-note').show();
        } else {
            $('#tc-payment-note').hide();
        }

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
                    then: response => mapGridData(response.data || response)
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

    // MAPEAR DATOS PARA LA GRILLA
    function mapGridData(responseData) {
        if (!Array.isArray(responseData)) return [];

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