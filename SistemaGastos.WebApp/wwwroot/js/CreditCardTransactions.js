(function () {
    const $container = $('#credit-card-container');
    if ($container.length === 0) return;

    if (typeof gridjs === 'undefined') {
        console.warn("Grid.js not loaded, retrying...");
        setTimeout(() => location.reload(), 100);
        return;
    }

    const config = {
        urls: {
            list: $container.data('url-list'),
            create: $container.data('url-create'),
            edit: $container.data('url-edit'),
            delete: $container.data('url-delete'),
            form: $container.data('url-form'),
            totals: $container.data('url-totals'),
            formMultiple: $container.data('url-multiple-form')
        },
        selectors: {
            tableContainer: '#cc-rows-container',
            rowTemplate: '#row-template-cc',
            totalCount: '#lbl-total-count',
            totalAmount: '#lbl-total-amount',
            formTransaction: '#form-transaction'
        }
    };

    // =========================================================
    // HELPERS DE FORMATO
    // =========================================================
    const fmtARS = new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS' });
    const fmtUSD = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' });

    function formatAmount(amount, currency) {
        return currency === 'USD' ? fmtUSD.format(amount) : fmtARS.format(amount);
    }

    // =========================================================
    // 1. GRID.JS
    // =========================================================
    const wrapper = document.getElementById('card-grid-wrapper');
    if (wrapper) wrapper.innerHTML = '';

    window.cardGrid = new gridjs.Grid({
        columns: [
            {
                // Checkbox de selección — pasa el id como valor
                id: 'check',
                name: gridjs.html('<input type="checkbox" class="check-all-cards form-check-input" style="cursor:pointer;">'),
                width: '42px',
                sort: false,
                formatter: (cell) => gridjs.html(
                    `<input type="checkbox" class="form-check-input row-checkbox" value="${cell}" style="cursor:pointer;">`
                )
            },
            {
                id: 'fecha',
                name: 'Fecha',
                width: '90px',
                formatter: (cell) => gridjs.html(
                    `<span class="tx-date">${cell}</span>`
                )
            },
            {
                // Descripción + nombre de tarjeta apilados
                id: 'descripcion',
                name: 'Descripción',
                formatter: (cell, row) => {
                    // row.cells[9].data = accountName  (ver mapeo en then:)
                    const accountName = row.cells[9]?.data || '';
                    return gridjs.html(`
                        <div style="line-height:1.3;">
                            <div style="font-size:13px;">${cell}</div>
                            ${accountName
                            ? `<div class="cc-account-name"><i class="fa-regular fa-credit-card" style="font-size:10px;margin-right:3px;"></i>${accountName}</div>`
                            : ''}
                        </div>
                    `);
                }
            },
            {
                id: 'categoria',
                name: 'Categoría',
                width: '140px',
                formatter: (cell) => gridjs.html(
                    `<span class="tx-badge" style="background:var(--bs-secondary-bg);color:var(--bs-secondary-color);border:1px solid var(--bs-border-color);">${cell || '—'}</span>`
                )
            },
            {
                id: 'cuotas',
                name: 'Cuotas',
                width: '80px',
                formatter: (cell) => {
                    if (!cell) return '—';
                    const [actual, total] = cell.split('/');
                    return gridjs.html(
                        `<span class="cc-installment"><span class="cc-inst-current">${actual}</span>/${total}</span>`
                    );
                }
            },
            {
                id: 'monto',
                name: 'Monto',
                width: '120px',
                formatter: (cell, row) => {
                    // row.cells[8].data = currency
                    const currency = row.cells[8]?.data || 'ARS';
                    const cls = currency === 'USD' ? 'cc-amount-usd' : 'cc-amount-ars';
                    return gridjs.html(`<span class="${cls}">${cell}</span>`);
                }
            },
            {
                id: 'tipo',
                name: 'Tipo',
                width: '100px',
                formatter: (cell) => {
                    return cell
                        ? gridjs.html('<span class="tx-badge cc-badge-fijo"><i class="fa-solid fa-lock" style="font-size:10px;margin-right:3px;"></i>Fijo</span>')
                        : gridjs.html('<span class="tx-badge cc-badge-var"><i class="fa-solid fa-chart-line" style="font-size:10px;margin-right:3px;"></i>Variable</span>');
                }
            },
            // Columnas ocultas de soporte (usadas por formatters via row.cells)
            { id: 'id', name: 'id', hidden: true },
            { id: 'currency', name: 'currency', hidden: true },
            { id: 'accountName', name: 'tarjeta', hidden: true },
            {
                id: 'acciones',
                name: '',
                sort: false,
                width: '80px',
                formatter: (cell, row) => {
                    const id = row.cells[7].data; // columna id oculta
                    return gridjs.html(`
                        <div class="d-flex justify-content-center gap-1">
                            <button class="tx-btn-action edit btn-editar" data-id="${id}" title="Editar">
                                <i class="fa-solid fa-pen"></i>
                            </button>
                            <button class="tx-btn-action del btn-eliminar-single" data-id="${id}" title="Eliminar">
                                <i class="fa-solid fa-trash"></i>
                            </button>
                        </div>
                    `);
                }
            }
        ],
        pagination: {
            limit: 15,
            server: {
                url: (prev, page, limit) =>
                    `${prev}${prev.includes('?') ? '&' : '?'}limit=${limit}&offset=${page * limit}`
            }
        },
        search: {
            server: {
                url: (prev, keyword) =>
                    `${prev}${prev.includes('?') ? '&' : '?'}keyword=${encodeURIComponent(keyword)}`
            },
            enabled: true,
            placeholder: 'Buscar...'
        },
        server: {
            url: config.urls.list,
            total: data => data.total,
            then: data => {
                // Actualizar contador de consumos en stat card
                $('#statCount').text(data.total ?? data.results?.length ?? '—');

                return (data.results || []).map(t => {
                    // ⚠️ Verificar que el DTO del backend incluya accountName.
                    // Si no viene, agregar la propiedad en el query/DTO del backend
                    // incluyendo: .Include(t => t.Account) y mapeando t.Account.Name → accountName
                    const accountName = t.accountName || t.account?.name || '';
                    const cuotas = (t.actualInstallment > 0)
                        ? `${String(t.actualInstallment).padStart(2, '0')}/${String(t.installments).padStart(2, '0')}`
                        : null;

                    return [
                        t.id,                           // [0] check (id)
                        t.transactionDate
                            ? new Date(t.transactionDate).toLocaleDateString('es-AR')
                            : '',                       // [1] fecha
                        t.description || '',            // [2] descripcion
                        t.categoryName || '—',          // [3] categoria
                        cuotas,                         // [4] cuotas
                        formatAmount(t.amount, t.currency), // [5] monto
                        t.fixed ?? false,               // [6] tipo
                        t.id,                           // [7] id oculto (acciones)
                        t.currency || 'ARS',            // [8] currency oculta (monto formatter)
                        accountName                     // [9] accountName oculta (descripcion formatter)
                    ];
                });
            }
        },
        className: {
            table: 'table align-middle mb-0',
            th: '',
            td: ''
        },
        language: {
            'search': { 'placeholder': 'Buscar...' },
            'pagination': {
                'previous': 'Anterior',
                'next': 'Siguiente',
                'showing': 'Mostrando',
                'results': () => 'movimientos',
                'to': 'a',
                'of': 'de'
            },
            'loading': 'Cargando...',
            'noRecordsFound': 'No se encontraron movimientos',
            'error': 'Ocurrió un error al cargar los datos'
        }
    }).render(wrapper);

    // =========================================================
    // 2. TOTALES Y STATS DEL HEADER
    // =========================================================
    async function fetchDolarBolsa() {
        try {
            const r = await fetch('/api/dolar/bolsa');
            return (await r.json()).dolarBolsa || 0;
        } catch { return 0; }
    }

    async function actualizarTotalesHeader() {
        try {
            const [resBackend, dolarBolsa] = await Promise.all([
                $.get(config.urls.totals),
                fetchDolarBolsa()
            ]);

            const totalArs = resBackend.totalArs || 0;
            const totalUsd = resBackend.totalUsd || 0;
            const totalGlobal = totalArs + (totalUsd * dolarBolsa);
            const totalFijo = (resBackend.fijoArs || 0) + ((resBackend.fijoUsd || 0) * dolarBolsa);
            const totalVar = (resBackend.varArs || 0) + ((resBackend.varUsd || 0) * dolarBolsa);

            // Stat cards
            $('#statTotalArs').text(fmtARS.format(totalArs));
            $('#statTotalUsd').text(fmtUSD.format(totalUsd));
            $('#statTotalGlobal').text(fmtARS.format(totalGlobal));

            // Badge dólar
            if (dolarBolsa > 0) {
                $('#dolarBadge').text(`Dólar bolsa: ${fmtARS.format(dolarBolsa)}`);
            }

            // Leyenda fijo / variable
            $('#hero-total-fijo').text(fmtARS.format(totalFijo));
            $('#hero-total-var').text(fmtARS.format(totalVar));

            // Barra de progreso
            const totalSuma = totalFijo + totalVar;
            if (totalSuma > 0) {
                const pctFijo = Math.round((totalFijo / totalSuma) * 100);
                const pctVar = 100 - pctFijo;
                $('#bar-fijo').css('width', pctFijo + '%');
                $('#bar-variable').css('width', pctVar + '%');
                $('#pct-fijo').text(`(${pctFijo}%)`);
                $('#pct-var').text(`(${pctVar}%)`);
            } else {
                $('#bar-fijo, #bar-variable').css('width', '0%');
            }

        } catch (e) { console.error('Error actualizando totales:', e); }
    }

    function recargarTablaYTotales() {
        window.cardGrid.forceRender();
        actualizarTotalesHeader();
        $('#deleteSelectedBtn').prop('disabled', true);
    }

    // =========================================================
    // 3. CREDIT CARD MANAGER
    // =========================================================
    const CreditCardManager = (function () {

        function _saveData(data, url, method) {
            Swal.fire({ title: 'Guardando...', didOpen: () => Swal.showLoading() });
            $.ajax({
                url, type: method, contentType: 'application/json',
                data: JSON.stringify(data),
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
                success: (res) => {
                    if (res.success) {
                        Swal.fire({ icon: 'success', title: 'Guardado', timer: 1500, showConfirmButton: false });
                        recargarTablaYTotales();
                    } else {
                        Swal.fire('Error', res.message || res.error, 'error');
                    }
                },
                error: (xhr) => {
                    console.error('Save Error:', xhr);
                    Swal.fire('Error', 'Error de servidor: ' + xhr.status, 'error');
                }
            });
        }

        function openMultipleModal() {
            Swal.showLoading();
            $.get(config.urls.formMultiple, function (html) {
                Swal.fire({
                    title: '<i class="fa-solid fa-layer-group me-2"></i> Carga masiva',
                    html, width: '1100px',
                    showCancelButton: true,
                    confirmButtonText: 'Guardar todo',
                    cancelButtonText: 'Cancelar',
                    didOpen: () => {
                        _bindTableEvents();
                        _addRow();
                    },
                    preConfirm: () => _getTableData()
                }).then((res) => {
                    if (res.isConfirmed) _saveData(res.value, config.urls.create, 'POST');
                });
            }).fail(() => Swal.fire('Error', 'No se pudo cargar el formulario.', 'error'));
        }

        function _bindTableEvents() {
            $(document).off('click', '#btn-add-cc-row').on('click', '#btn-add-cc-row', () => _addRow());
            $(document).off('click', '.btn-remove-row').on('click', '.btn-remove-row', function () { _removeRow(this); });
            $(document).off('input', '.input-amount').on('input', '.input-amount', _calculateTotals);
            $(document).off('change', '.input-fixed').on('change', '.input-fixed', function () {
                const $row = $(this).closest('tr');
                if ($(this).is(':checked')) {
                    $row.find('.installments-group').css('visibility', 'hidden');
                    $row.find('.input-actual, .input-total').val('');
                } else {
                    $row.find('.installments-group').css('visibility', 'visible');
                }
            });
        }

        function _addRow() {
            const template = document.querySelector(config.selectors.rowTemplate);
            if (!template) { console.error('Template not found:', config.selectors.rowTemplate); return; }

            const clone = template.content.cloneNode(true);

            // Inyecta las opciones de tarjeta desde data-account-options del contenedor.
            // Usamos dataset en lugar de depender de scripts dentro del partial
            // (SweetAlert2 inyecta HTML con innerHTML y no ejecuta <script> por seguridad).
            const container = document.getElementById('cc-multiple-container');
            const accountOptions = container ? container.dataset.accountOptions : null;
            if (accountOptions) {
                const sel = clone.querySelector('select.input-account');
                if (sel) sel.innerHTML = accountOptions;
            }

            $(config.selectors.tableContainer).append(clone);
            _calculateTotals();
        }

        function _removeRow(btn) {
            const $row = $(btn).closest('tr');
            if ($(config.selectors.tableContainer).find('tr').length > 1) {
                $row.fadeOut(300, function () { $(this).remove(); _calculateTotals(); });
            } else {
                $row.find('input:not([type="checkbox"])').val('');
                $row.find('.input-actual, .input-total').val('');
                $row.find('.input-fixed').prop('checked', false).trigger('change');
                _calculateTotals();
            }
        }

        function _calculateTotals() {
            let total = 0, count = 0;
            $(config.selectors.tableContainer).find('.input-amount').each(function () {
                const val = parseFloat($(this).val()) || 0;
                if (val > 0) { total += val; count++; }
            });
            $(config.selectors.totalCount).text(count);
            $(config.selectors.totalAmount).text('$ ' + total.toLocaleString('es-AR', { minimumFractionDigits: 2 }));
        }

        function _getTableData() {
            const transacciones = [];
            let errorMsg = null;

            $(config.selectors.tableContainer).find('tr').each(function () {
                const $row = $(this);
                const amount = parseFloat($row.find('.input-amount').val()) || 0;
                const desc = $row.find('.input-desc').val()?.trim();

                if (amount > 0 || desc) {
                    const accountId = parseInt($row.find('.input-account').val());
                    const categoryId = parseInt($row.find('.input-category').val());

                    if (!accountId || !categoryId || amount <= 0) {
                        errorMsg = 'Faltan datos (Tarjeta, Categoría o Monto) en una de las filas.';
                        return false;
                    }

                    transacciones.push({
                        AccountID: accountId,
                        TransactionDate: $row.find('.input-date').val(),
                        CategoryID: categoryId,
                        Description: desc,
                        Amount: amount,
                        ActualInstallment: parseInt($row.find('.input-actual').val()) || null,
                        Installments: parseInt($row.find('.input-total').val()) || null,
                        Fixed: $row.find('.input-fixed').is(':checked')
                    });
                }
            });

            if (errorMsg) { Swal.showValidationMessage(errorMsg); return false; }
            if (transacciones.length === 0) { Swal.showValidationMessage('La tabla está vacía o los montos son $0.'); return false; }
            return transacciones;
        }

        function openSingleModal(id = null) {
            let url = config.urls.form;
            if (id) url += '?id=' + id;
            const isEditMode = id !== null;

            $.get(url, function (html) {
                Swal.fire({
                    title: isEditMode ? 'Editar transacción' : 'Nueva transacción',
                    html, width: '600px',
                    showCancelButton: true,
                    confirmButtonText: 'Guardar',
                    cancelButtonText: 'Cancelar',
                    didOpen: () => {
                        _bindSingleCalculator();
                        const scriptEl = $(html).filter('script');
                        if (scriptEl.length > 0) $.globalEval(scriptEl.text());
                    },
                    preConfirm: () => _getSingleFormData(isEditMode)
                }).then((res) => {
                    if (res.isConfirmed) {
                        const dataToSend = isEditMode ? res.value : [res.value];
                        const targetUrl = isEditMode ? config.urls.edit : config.urls.create;
                        const method = isEditMode ? 'PUT' : 'POST';
                        _saveData(dataToSend, targetUrl, method);
                    }
                });
            });
        }

        function _bindSingleCalculator() {
            const $form = $(config.selectors.formTransaction);
            const $inputMonto = $form.find('input[name="Amount"]');
            const $inputCuotas = $form.find('input[name="Installments"]');

            if ($inputMonto.length && $inputCuotas.length) {
                if ($('#info-calculo').length === 0) {
                    $inputMonto.parent().append('<small id="info-calculo" class="text-muted d-block mt-1 text-end"></small>');
                }
                const calcular = () => {
                    const total = parseFloat($inputMonto.val()) || 0;
                    const cuotas = parseInt($inputCuotas.val()) || 1;
                    if (cuotas > 1 && total > 0) {
                        $('#info-calculo').text(`Total: ${fmtARS.format(total)} ÷ ${cuotas} = ${fmtARS.format(total / cuotas)} / mes`);
                    } else {
                        $('#info-calculo').text('');
                    }
                };
                $inputMonto.off('input').on('input', calcular);
                $inputCuotas.off('input').on('input', calcular);
                calcular();
            }
        }

        function _getSingleFormData(isEditMode) {
            const form = document.querySelector(config.selectors.formTransaction);
            const data = Object.fromEntries(new FormData(form).entries());

            data.ID = parseInt(data.ID) || 0;
            data.Fixed = $(form).find('input[name="Fixed"]').is(':checked');
            data.CategoryID = parseInt(data.CategoryID) || 0;
            data.ActualInstallment = parseInt(data.ActualInstallment) || null;
            data.Installments = parseInt(data.Installments) || null;
            data.PersonID = parseInt(data.PersonID) || null;
            const monto = parseFloat(data.Amount) || 0;
            data.Amount = monto;

            if (!isEditMode && data.Installments > 1 && monto > 0) {
                data.Amount = monto / data.Installments;
            }

            if (!data.Description || data.Amount <= 0 || !data.CategoryID) {
                Swal.showValidationMessage('Complete descripción, monto y categoría.');
                return false;
            }
            return data;
        }

        function deleteItems(ids) {
            Swal.fire({
                title: '¿Eliminar?',
                text: ids.length > 1 ? `Se eliminarán ${ids.length} consumos.` : 'Se eliminará el consumo.',
                icon: 'warning',
                showCancelButton: true,
                confirmButtonColor: '#d33',
                confirmButtonText: 'Sí, eliminar',
                cancelButtonText: 'Cancelar'
            }).then((result) => {
                if (result.isConfirmed) {
                    $.ajax({
                        url: config.urls.delete, type: 'DELETE', contentType: 'application/json',
                        data: JSON.stringify(ids),
                        headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
                        success: (res) => {
                            if (res.success) {
                                Swal.fire({ icon: 'success', title: 'Eliminado', timer: 1500, showConfirmButton: false });
                                recargarTablaYTotales();
                            } else {
                                Swal.fire('Error', res.message, 'error');
                            }
                        }
                    });
                }
            });
        }

        return {
            abrirModalIndividual: openSingleModal,
            abrirModalMasivo: openMultipleModal,
            eliminar: deleteItems
        };
    })();

    // =========================================================
    // 4. EVENT LISTENERS
    // =========================================================
    $(document).off('click', '#btnCrear').on('click', '#btnCrear', () => CreditCardManager.abrirModalMasivo());

    $(document).off('click', '.btn-editar').on('click', '.btn-editar', function () {
        CreditCardManager.abrirModalIndividual($(this).data('id'));
    });

    $(document).off('click', '.btn-eliminar-single').on('click', '.btn-eliminar-single', function () {
        CreditCardManager.eliminar([$(this).data('id')]);
    });

    // Selección masiva
    $(document).on('change', '.check-all-cards', function () {
        $('#card-grid-wrapper .row-checkbox').prop('checked', $(this).is(':checked')).trigger('change');
    });

    $(document).on('change', '.row-checkbox', function () {
        const count = $('#card-grid-wrapper .row-checkbox:checked').length;
        $('#deleteSelectedBtn').prop('disabled', count === 0);
    });

    $(document).off('click', '#deleteSelectedBtn').on('click', '#deleteSelectedBtn', function () {
        const ids = [];
        $('#card-grid-wrapper .row-checkbox:checked').each(function () {
            ids.push(parseInt($(this).val()));
        });
        if (ids.length > 0) CreditCardManager.eliminar(ids);
    });

    // Carga inicial
    actualizarTotalesHeader();

})();