(function () {
    const $container = $('#transaction-container');
    if ($container.length === 0) return;

    window.gridInstance = null;

    if (typeof gridjs === 'undefined') {
        console.warn("Grid.js aún no ha cargado. Reintentando en 100ms...");
        setTimeout(() => $(document).trigger('turbo:load'), 100);
        return;
    }

    const urls = {
        list: $container.data('url-list'),
        create: $container.data('url-create'),
        save: $container.data('url-save'),
        delete: $container.data('url-delete'),
        createForm: $container.data('url-create-form'),
        editForm: $container.data('url-edit-form'),
        dropdownData: $container.data('url-dropdown-data'),
        update: $container.data('url-update') || $container.data('url-save'),
        createBulk: $container.data('url-create-bulk') || $container.data('url-create'),
        invoicePreview: $container.data('url-invoice-preview'),
        emitInvoice: $container.data('url-emit-invoice')
    };

    let cachedAccounts = '<option value="">Seleccione</option>';
    let cachedCategories = '<option value="">Seleccione</option>';

    // ==========================================
    // HELPERS DE FORMATO
    // ==========================================
    function formatCurrency(amount) {
        return new Intl.NumberFormat('es-AR', {
            style: 'currency',
            currency: 'ARS',
            minimumFractionDigits: 2
        }).format(amount);
    }

    // ==========================================
    // STATS RÁPIDAS
    // ==========================================
    function updateStats(transactions) {
        let income = 0;
        let expense = 0;

        transactions.forEach(t => {
            if (t.isIncome) income += t.amount || 0;
            else expense += t.amount || 0;
        });

        const balance = income - expense;

        $('#statIncome').text(formatCurrency(income));
        $('#statExpense').text(formatCurrency(expense));

        const $balance = $('#statBalance');
        $balance.text(formatCurrency(balance));
        $balance.removeClass('tx-income tx-expense tx-balance');
        if (balance > 0) $balance.addClass('tx-income');
        else if (balance < 0) $balance.addClass('tx-expense');
        else $balance.addClass('tx-balance');
    }

    // ==========================================
    // MAPEADOR DE FILAS
    // ==========================================
    function mapTransactions(transactions) {
        updateStats(transactions);

        return transactions.map(t => [
            t.dateFormatted,
            `${t.accountName} (${t.accountCurrency})`,
            `<span class="tx-badge ${t.isIncome ? 'tx-badge-income' : 'tx-badge-expense'}">${t.categoryName}</span>`,
            t.description || '<em class="text-muted" style="font-size:12px;">Sin descripción</em>',
            `<span class="tx-amount-${t.isIncome ? 'income' : 'expense'}">${t.isIncome ? '+' : '-'} ${t.amountFormatted}</span>`,
            JSON.stringify({ id: t.id, isIncome: t.isIncome })
        ]);
    }

    // ==========================================
    // 1. CARGAR DROPDOWNS
    // ==========================================
    async function loadDropdowns() {
        try {
            const res = await $.get(urls.dropdownData);

            if (res.accounts && Array.isArray(res.accounts)) {
                const accountOptions = res.accounts
                    .map(a => `<option value="${a.id}">${a.name} (${a.currency})</option>`)
                    .join('');
                cachedAccounts += accountOptions;
                $('#filterAccount').html('<option value="">Todas</option>' + accountOptions);
            }

            if (res.categories && Array.isArray(res.categories)) {
                const categoryOptions = res.categories
                    .map(c => `<option value="${c.id}">${c.name}</option>`)
                    .join('');
                cachedCategories += categoryOptions;
                $('#filterCategory').html('<option value="">Todas</option>' + categoryOptions);
            }
        } catch (error) {
            console.error("Error cargando dropdowns:", error);
        }
    }

    loadDropdowns();

    // ==========================================
    // 2. INICIALIZAR FILTROS CON MES ACTUAL
    // ==========================================
    function initializeFilters() {
        const now = new Date();
        const firstDay = new Date(now.getFullYear(), now.getMonth(), 1);
        const lastDay = new Date(now.getFullYear(), now.getMonth() + 1, 0);

        $('#filterStart').val(firstDay.toISOString().split('T')[0]);
        $('#filterEnd').val(lastDay.toISOString().split('T')[0]);
    }

    initializeFilters();

    // ==========================================
    // 3. GRID.JS CONFIGURACIÓN
    // ==========================================
    function buildListUrl() {
        const start = $('#filterStart').val();
        const end = $('#filterEnd').val();
        const accountId = $('#filterAccount').val();
        const categoryId = $('#filterCategory').val();

        const params = new URLSearchParams();
        if (start) params.append('startDate', start);
        if (end) params.append('endDate', end);
        if (accountId) params.append('accountId', accountId);
        if (categoryId) params.append('categoryId', categoryId);

        return `${urls.list}?${params.toString()}`;
    }

    function buildServerConfig() {
        return {
            url: buildListUrl(),
            then: response => {
                if (!response.success) {
                    console.error("Error cargando grilla:", response.message);
                    updateStats([]);
                    return [];
                }
                return mapTransactions(response.data.transactions);
            }
        };
    }

    const wrapper = document.getElementById("grid-wrapper");
    if (wrapper) wrapper.innerHTML = '';

    window.gridInstance = new gridjs.Grid({
        columns: [
            {
                id: 'fecha', name: 'Fecha', width: '8%',
                formatter: (cell) => gridjs.html(`<span class="tx-date">${cell}</span>`)
            },
            { id: 'cuenta', name: 'Cuenta', width: '15%' },
            {
                id: 'categoria', name: 'Categoría', width: '13%',
                formatter: (cell) => gridjs.html(cell)
            },
            {
                id: 'descripcion', name: 'Descripción', width: '32%',
                formatter: (cell) => gridjs.html(cell)
            },
            {
                id: 'monto', name: 'Monto', width: '13%',
                formatter: (cell) => gridjs.html(cell)
            },
            {
                id: 'acciones',
                name: '',
                width: '10%',
                sort: false,
                formatter: (cell) => {
                    let data;
                    try { data = JSON.parse(cell); } catch { data = { id: cell, isIncome: false }; }
                    const id = data.id;
                    const invoiceBtn = data.isIncome
                        ? `<button class="tx-btn-action inv btn-invoice" data-id="${id}" title="Generar Factura C">
                               <i class="fa-solid fa-file-invoice"></i>
                           </button>`
                        : '';
                    return gridjs.html(`
                        <div class="d-flex justify-content-center gap-1">
                            <button class="tx-btn-action edit btn-edit" data-id="${id}" title="Editar">
                                <i class="fa-solid fa-pen"></i>
                            </button>
                            ${invoiceBtn}
                            <button class="tx-btn-action del btn-delete" data-id="${id}" title="Eliminar">
                                <i class="fa-solid fa-trash"></i>
                            </button>
                        </div>
                    `);
                }
            }
        ],
        server: buildServerConfig(),
        pagination: { enabled: true, limit: 15, summary: true },
        search: { enabled: true, placeholder: 'Buscar...' },
        sort: true,
        language: {
            'search': { 'placeholder': 'Buscar...' },
            'pagination': {
                'previous': 'Anterior',
                'next': 'Siguiente',
                'showing': 'Mostrando',
                'results': () => 'registros'
            },
            'loading': 'Cargando...',
            'noRecordsFound': 'No se encontraron registros'
        },
        className: {
            table: 'table align-middle mb-0',
            th: '',
            td: ''
        }
    }).render(wrapper);

    // ==========================================
    // 4. FILTROS
    // ==========================================
    $('#btnApplyFilters').click(function () {
        window.gridInstance.updateConfig({
            server: buildServerConfig()
        }).forceRender();
    });

    $('#btnClearFilters').click(function () {
        initializeFilters();
        $('#filterAccount').val('');
        $('#filterCategory').val('');
        $('#btnApplyFilters').click();
    });

    // ==========================================
    // 6. ELIMINAR INDIVIDUAL
    // ==========================================
    $(document).off('click', '.btn-delete').on('click', '.btn-delete', function () {
        const id = $(this).data('id');

        Swal.fire({
            title: '¿Eliminar transacción?',
            text: 'Se revertirá el saldo en la cuenta asociada.',
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#d33',
            confirmButtonText: 'Sí, eliminar',
            cancelButtonText: 'Cancelar'
        }).then((result) => {
            if (result.isConfirmed) {
                $.ajax({
                    url: `${urls.delete}/${id}`,
                    type: 'DELETE',
                    headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
                    success: (response) => {
                        if (response.success) {
                            Swal.fire({
                                icon: 'success',
                                title: 'Eliminado',
                                text: response.message,
                                timer: 2000,
                                showConfirmButton: false
                            });
                            window.gridInstance.forceRender();
                        } else {
                            Swal.fire('Error', response.message, 'error');
                        }
                    },
                    error: (xhr) => {
                        const response = xhr.responseJSON;
                        Swal.fire('Error', response?.message || 'Error al eliminar', 'error');
                    }
                });
            }
        });
    });

    // ==========================================
    // 7. EDITAR INDIVIDUAL
    // ==========================================
    $(document).off('click', '.btn-edit').on('click', '.btn-edit', function () {
        const id = $(this).data('id');
        abrirModalEditar(id);
    });

    function abrirModalEditar(id) {
        $.get(`${urls.editForm}?id=${id}`, function (html) {
            $('#modalEditContent').html(html);
            const modal = new bootstrap.Modal(document.getElementById('modalEditTransaction'));
            modal.show();
        }).fail(function (xhr) {
            console.error('Error cargando formulario:', xhr);
            Swal.fire('Error', 'No se pudo cargar el formulario de edición', 'error');
        });
    }

    // ==========================================
    // 8. LÓGICA DE MULTICUENTAS (EDICIÓN)
    // ==========================================
    $(document).off('click', '#btnEditAddSplitRow').on('click', '#btnEditAddSplitRow', function () {
        const $cont = $('#editSplitsContainer');
        const $firstRow = $cont.find('.edit-split-row').first();
        const $clone = $firstRow.clone();

        $clone.find('.edit-split-amount').val('');
        $clone.find('.edit-split-account').val('');
        $clone.find('.btn-remove-edit-split').show();

        $cont.append($clone);
        calcularTotalEdicion();
    });

    $(document).off('click', '.btn-remove-edit-split').on('click', '.btn-remove-edit-split', function () {
        $(this).closest('.edit-split-row').remove();
        calcularTotalEdicion();
    });

    $(document).off('input', '.edit-split-amount').on('input', '.edit-split-amount', function () {
        calcularTotalEdicion();
    });

    function calcularTotalEdicion() {
        let total = 0;
        $('.edit-split-amount').each(function () {
            const val = parseFloat($(this).val()) || 0;
            total += val;
        });
        $('#lblEditTotalAmount').text(total.toLocaleString('es-AR', { minimumFractionDigits: 2 }));
    }

    // ==========================================
    // 8b. FACTURA ARCA – ABRIR PREVIEW
    // ==========================================
    $(document).off('click', '.btn-invoice').on('click', '.btn-invoice', function () {
        const id = $(this).data('id');

        // Resetear contenido del modal al spinner
        $('#modalInvoiceContent').html(`
            <div class="text-center p-5">
                <div class="spinner-border text-success" role="status"></div>
                <div class="mt-2 text-muted small">Cargando vista previa...</div>
            </div>
        `);

        const modal = new bootstrap.Modal(document.getElementById('modalInvoice'));
        modal.show();

        $.get(`${urls.invoicePreview}?id=${id}`, function (html) {
            $('#modalInvoiceContent').html(html);
        }).fail(function () {
            $('#modalInvoiceContent').html(`
                <div class="modal-body text-center text-danger py-4">
                    <i class="fa-solid fa-circle-xmark fa-2x mb-2"></i>
                    <div>No se pudo cargar la vista previa de la factura.</div>
                </div>
                <div class="modal-footer border-0">
                    <button type="button" class="btn btn-secondary btn-sm" data-bs-dismiss="modal">Cerrar</button>
                </div>
            `);
        });
    });

    // ==========================================
    // 8c. FACTURA ARCA – EMITIR
    // ==========================================
    $(document).off('click', '#btnEmitirFactura').on('click', '#btnEmitirFactura', function () {
        const transactionId      = parseInt($('#invoiceTransactionId').val());
        const receptorNombre     = $('#receptorNombre').val().trim();
        const receptorCuit       = $('#receptorCuit').val().trim();
        const emisorRazonSocial  = $('#emisorRazonSocial').val().trim();
        const emisorCuit         = $('#emisorCuit').val().trim();
        const emisorDomicilio    = $('#emisorDomicilio').val().trim();
        const puntoVenta         = $('#puntoVenta').val().trim();

        if (!receptorNombre || !receptorCuit) {
            Swal.fire('Atención', 'Completa los datos del receptor antes de emitir.', 'warning');
            return;
        }
        if (!emisorRazonSocial || !emisorCuit) {
            Swal.fire('Atención', 'Completa la Razón Social y CUIT del emisor antes de emitir.', 'warning');
            return;
        }

        const $btn = $(this);
        $btn.prop('disabled', true).html('<i class="fa-solid fa-spinner fa-spin me-1"></i>Emitiendo...');

        $.ajax({
            url: urls.emitInvoice,
            type: 'POST',
            contentType: 'application/json',
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            data: JSON.stringify({
                transactionId,
                receptorNombre,
                receptorCuit,
                emisorRazonSocial,
                emisorCuit,
                emisorDomicilio,
                puntoVenta
            }),
            success: function (result) {
                if (result.success) {
                    $('#invoicePreviewBody').html(`
                        <div class="text-center py-3">
                            <div class="text-success mb-2">
                                <i class="fa-solid fa-circle-check" style="font-size:3rem;"></i>
                            </div>
                            <h5 class="fw-semibold text-success mb-3">¡Factura emitida!</h5>
                            <div class="invoice-result-success p-3 rounded">
                                <div class="mb-1">
                                    <span class="text-body-secondary" style="font-size:13px;">CAE:</span>
                                    <strong class="text-success ms-1">${result.cae}</strong>
                                </div>
                                <div>
                                    <span class="text-body-secondary" style="font-size:13px;">Vencimiento CAE:</span>
                                    <strong class="ms-1">${result.caeVencimiento}</strong>
                                </div>
                            </div>
                            <p class="text-body-secondary mt-3 mb-0" style="font-size:12px;">
                                <i class="fa-solid fa-circle-info me-1"></i>${result.message}
                            </p>
                        </div>
                    `);
                    $('#invoiceModalFooter').html(
                        '<button type="button" class="btn btn-secondary btn-sm" data-bs-dismiss="modal">Cerrar</button>'
                    );
                } else {
                    Swal.fire('Error', result.message || 'No se pudo emitir la factura.', 'error');
                    $btn.prop('disabled', false).html('<i class="fa-solid fa-paper-plane me-1"></i>Emitir Factura');
                }
            },
            error: function () {
                Swal.fire('Error', 'Error del servidor al emitir la factura.', 'error');
                $btn.prop('disabled', false).html('<i class="fa-solid fa-paper-plane me-1"></i>Emitir Factura');
            }
        });
    });

    // ==========================================
    // 9. SUBMIT DE EDICIÓN
    // ==========================================
    $(document).off('submit', '#editTransactionForm').on('submit', '#editTransactionForm', function (e) {
        e.preventDefault();

        const splits = [];
        $('.edit-split-row').each(function () {
            const accId = $(this).find('.edit-split-account').val();
            const amt = parseFloat($(this).find('.edit-split-amount').val()) || 0;
            if (accId && amt > 0) splits.push({ AccountID: parseInt(accId), Amount: amt });
        });

        if (splits.length === 0) {
            Swal.fire('Atención', 'Debes ingresar al menos un monto válido.', 'warning');
            return;
        }

        const commandData = {
            ID: parseInt($('#Edit_Transaction_ID').val()),
            Date: $('#Edit_Transaction_Date').val(),
            CategoryID: parseInt($('#Edit_Transaction_CategoryID').val()),
            Description: $('#Edit_Transaction_Description').val(),
            FixedExpenseID: parseInt($('#Edit_Transaction_FixedExpenseID').val()) || null,
            PersonID: parseInt($('#Edit_Transaction_PersonID').val()) || null,
            AccountID: splits[0].AccountID,
            Amount: splits[0].Amount,
            Splits: splits
        };

        const $btnUpdate = $('#btnUpdateTransaction');
        $btnUpdate.prop('disabled', true).html('<i class="fa-solid fa-spinner fa-spin me-1"></i> Actualizando...');

        $.ajax({
            url: urls.update,
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(commandData),
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            success: function (res) {
                if (res.success || res.succeeded) {
                    $('#modalEditTransaction').modal('hide');
                    Swal.fire({ icon: 'success', title: 'Actualizado', timer: 1000, showConfirmButton: false });
                    if (typeof window.gridInstance !== 'undefined') window.gridInstance.forceRender();
                    if (typeof actualizarDashboard === 'function') actualizarDashboard();
                } else {
                    Swal.fire('Error', res.message || 'Error al actualizar', 'error');
                }
            },
            error: function (xhr) {
                Swal.fire('Error', 'Error de servidor. Revisa la consola.', 'error');
                console.error("Error en Update:", xhr.responseText);
            },
            complete: function () {
                $btnUpdate.prop('disabled', false).html('<i class="fas fa-save me-1"></i> Actualizar');
            }
        });
    });
})();