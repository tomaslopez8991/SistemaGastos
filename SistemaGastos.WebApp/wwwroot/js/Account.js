(function () {
    const $container = $('#accounts-container');
    if ($container.length === 0) return;

    const urls = {
        list: $container.data('url-list'),
        totals: $container.data('url-totals'),
        create: $container.data('url-create'),
        edit: $container.data('url-edit'),
        delete: $container.data('url-delete'),
        transfer: $container.data('url-transfer')
    };

    let allAccounts = []; // Almacén en memoria

    function loadData() {
        $.get(urls.totals, function (res) {
            if (!res.success || !res.data) {
                console.error("Error al cargar totales:", res.message);
                return;
            }

            const $wrapper = $('#totals-wrapper');
            $wrapper.empty();

            res.data.forEach(t => {
                let colorClass = '';
                let balanceState = '';

                if (t.total > 0) {
                    colorClass = 'text-success';
                    balanceState = 'positive';
                } else if (t.total < 0) {
                    colorClass = 'text-danger';
                    balanceState = 'negative';
                } else {
                    colorClass = 'text-muted';
                    balanceState = 'neutral';
                }

                let currencyIcon = '';
                if (t.currency === 'ARS') currencyIcon = 'fa-peso-sign';
                else if (t.currency === 'USD') currencyIcon = 'fa-dollar-sign';
                else if (t.currency === 'USDT') currencyIcon = 'fa-coins';
                else currencyIcon = 'fa-money-bill-wave';

                const html = `
            <div class="col-md-4 col-6">
                <div class="total-card total-card-interactive ${balanceState}" 
                     data-currency="${t.currency}"
                     onclick="filtrarCuentas('${t.currency}', this)">
                    
                    <div class="total-card-icon">
                        <i class="fa-solid ${currencyIcon}"></i>
                    </div>
                    
                    <small class="text-muted text-uppercase fw-bold">${t.currency}</small>
                    
                    <h3 class="mb-0 fw-bold ${colorClass}">
                        ${formatMoney(t.total, t.currency)}
                    </h3>
                    
                    <div class="card-hint">
                        <i class="fa-solid fa-filter"></i>
                        <span>Click para filtrar</span>
                    </div>
                </div>
            </div>
        `;

                $wrapper.append(html);
            });
        });

        $.get(urls.list, function (res) {
            if (!res.success || !res.data) {
                console.error("Error al cargar cuentas:", res.message);
                return;
            }

            allAccounts = res.data;
            filtrarCuentas('ARS', null);
        });
    }

    window.filtrarCuentas = function (currency, btn) {
        $('.btn-filter').removeClass('active');

        if (btn) {
            $(btn).addClass('active');
        } else {
            $('.btn-filter').each(function () {
                if ($(this).text().trim() === currency) $(this).addClass('active');
            });
        }

        $('.total-card').removeClass('active-filter');
        $('.total-card').each(function () {
            if ($(this).find('small').text() === currency) $(this).addClass('active-filter');
        });

        if (currency === 'all' || currency === 'Todas') {
            renderGrid(allAccounts);
        } else {
            const filtered = allAccounts.filter(a => a.currency === currency);
            renderGrid(filtered);
        }
    };

    function renderGrid(data) {
        const $grid = $('#accounts-grid');
        $grid.empty();

        if (!data || data.length === 0) {
            $grid.html('<div class="col-12 text-center text-muted py-5">No tienes cuentas en esta moneda.</div>');
            return;
        }

        data.forEach(acc => {
            let icon = 'fa-building-columns';
            let styleClass = 'acc-bank';
            let colorClass = '';

            // Importante: El acc.type ahora seguramente te venga como número o string dependiendo de cómo lo mande tu C#. 
            // Si en tu C# el DTO de listado lo devuelve como string ("Efectivo", "TarjetaCredito", etc.), ajustalo aquí.
            if (acc.type === 'crypto' || acc.type === 5 || acc.type === 'Crypto') { icon = 'fa-bitcoin'; styleClass = 'acc-crypto'; }
            if (acc.type === 'mp' || acc.type === 3 || acc.type === 'BilleteraVirtual') { icon = 'fa-handshake'; styleClass = 'acc-mp'; }
            if (acc.type === 'cash' || acc.type === 1 || acc.type === 'Efectivo') { icon = 'fa-money-bill-wave'; styleClass = 'acc-cash'; }

            if (acc.currency === 'USD' && acc.type !== 'crypto') {
                styleClass = 'acc-bank';
            }

            if (acc.balance > 0) colorClass = 'text-success'
            else if (acc.balance < 0) colorClass = 'text-danger'

            const cardHtml = `
            <div class="col-md-4 col-lg-3">
                <div class="account-card ${styleClass}">
                    <div class="card-blob"></div>
                    
                    <div class="acc-content">
                        <div class="d-flex justify-content-between align-items-start">
                            <div class="acc-icon">
                                <i class="fa-solid ${icon}"></i>
                            </div>
                            <span class="badge bg-dark bg-opacity-50 border border-secondary">${acc.currency}</span>
                        </div>
                        
                        <h5 class="fw-bold mb-0 text-truncate" title="${acc.name}">${acc.name}</h5>
                        
                        <div class="acc-balance mt-2 ${colorClass}">
                            ${formatMoney(acc.balance, acc.currency)}
                        </div>
                    </div>

                    <div class="acc-actions">
                        <button class="btn btn-sm btn-primary flex-grow-1 btn-editar" data-id="${acc.id}">
                            <i class="fa-solid fa-pen"></i>
                        </button>
                        <button class="btn btn-sm btn-warning flex-grow-1 btn-transfer" data-id="${acc.id}" title="Transferir">
                            <i class="fa-solid fa-arrow-right-arrow-left"></i>
                        </button>
                        <button class="btn btn-sm btn-danger flex-grow-1 btn-eliminar" data-id="${acc.id}">
                            <i class="fa-solid fa-trash"></i>
                        </button>
                    </div>
                </div>
            </div>
        `;
            $grid.append(cardHtml);
        });
    }

    function formatMoney(amount, currency) {
        let safeCurrency = currency;
        let locale = 'es-AR';

        if (currency === 'USD' || currency === 'USDT') {
            locale = 'en-US';
            safeCurrency = 'USD';
        }

        try {
            return new Intl.NumberFormat(locale, {
                style: 'currency',
                currency: safeCurrency
            }).format(amount);
        } catch (e) {
            return '$ ' + amount.toFixed(2);
        }
    }

    $(document).off('click', '.btn-eliminar').on('click', '.btn-eliminar', function () {
        const id = $(this).data('id');
        Swal.fire({ title: '¿Eliminar Cuenta?', text: 'Se borrarán todos los gastos asociados.', icon: 'warning', showCancelButton: true, confirmButtonColor: '#d33', confirmButtonText: 'Sí, eliminar' })
            .then((res) => {
                if (res.isConfirmed) {
                    $.ajax({
                        url: urls.delete + '?id=' + encodeURIComponent(id),
                        type: 'DELETE',
                        headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
                        success: (res) => {
                            if (res.success) {
                                Swal.fire({ icon: 'success', title: 'Eliminado', timer: 1500, showConfirmButton: false });
                                loadData();
                            } else {
                                Swal.fire('Error', res.message, 'error');
                            }
                        },
                        error: () => Swal.fire('Error', 'Error de conexión', 'error')
                    });
                }
            });
    });

    // ==========================================
    // ABM Modal (Crear / Editar)
    // ==========================================
    // El <select> de Tipo usa los nombres del enum AccountType como value (para que
    // asp-for marque la opción seleccionada al editar). Acá lo mapeamos al ID numérico
    // que espera el backend (AccountType: Efectivo=1, Banco=2, BilleteraVirtual=3, TarjetaCredito=4, Crypto=5).
    const accountTypeMap = { Efectivo: 1, Banco: 2, BilleteraVirtual: 3, TarjetaCredito: 4, Crypto: 5 };

    function getTypeId(rawValue) {
        if (rawValue in accountTypeMap) return accountTypeMap[rawValue];
        return parseInt(rawValue) || 0;
    }

    window.abrirModalCuenta = function (id = null) {
        let urlForm = '/Account/GetAccountForm';
        if (id) urlForm += '?id=' + id;
        const isEdit = id !== null;

        $.get(urlForm, function (html) {
            Swal.fire({
                title: isEdit ? 'Editar Cuenta' : 'Nueva Cuenta',
                html: html,
                width: '600px',
                showCancelButton: true,
                confirmButtonText: '<i class="fas fa-save me-1"></i> Guardar',
                cancelButtonText: 'Cancelar',
                focusConfirm: false,
                didOpen: () => {
                    const scriptEl = $(html).filter('script');
                    if (scriptEl.length > 0) $.globalEval(scriptEl.text());

                    const $ddlType = $('#Type').length ? $('#Type') : $('#ddlAccountType');

                    function toggleCardFields() {
                        // Cambiamos a buscar el ID NUMÉRICO (4 es TarjetaCredito)
                        const tipoId = getTypeId($ddlType.val());
                        if (tipoId === 4) {
                            $('#credit-card-fields').slideDown();
                        } else {
                            $('#credit-card-fields').slideUp();
                        }
                    }

                    $ddlType.off('change').on('change', toggleCardFields);
                    toggleCardFields();
                },
                preConfirm: () => {
                    // PARSEAMOS A NÚMERO
                    const typeId = getTypeId($('#Type').val() || $('#ddlAccountType').val()) || 1;
                    const closingInput = $('#ClosingDay').val();
                    const dueInput = $('#DueDay').val();
                    const dueMonthOffsetInput = $('#DueMonthOffset').val();

                    var data = {
                        ID: parseInt($('#ID').val()) || 0,
                        Name: $('#Name').val(),
                        Type: typeId, // Ahora es un int
                        Currency: $('#Currency').val(),
                        Balance: parseFloat($('#Balance').val()) || 0,
                        ClosingDay: closingInput ? parseInt(closingInput) : null,
                        DueDay: dueInput ? parseInt(dueInput) : null,
                        DueMonthOffset: dueMonthOffsetInput !== undefined && dueMonthOffsetInput !== '' ? parseInt(dueMonthOffsetInput) : null
                    };

                    if (!data.Name || !data.Currency || !data.Type) {
                        Swal.showValidationMessage('Nombre, Tipo y Moneda son obligatorios');
                        return false;
                    }

                    // VALIDACIÓN POR ID NUMÉRICO
                    if (data.Type === 4) {
                        if (!data.ClosingDay || !data.DueDay) {
                            Swal.showValidationMessage('Para tarjetas de crédito, debes indicar día de Cierre y Vencimiento.');
                            return false;
                        }
                        if (data.ClosingDay < 1 || data.ClosingDay > 31 || data.DueDay < 1 || data.DueDay > 31) {
                            Swal.showValidationMessage('Los días deben ser números entre 1 y 31.');
                            return false;
                        }
                    } else {
                        data.ClosingDay = null;
                        data.DueDay = null;
                        data.DueMonthOffset = null;
                    }

                    return { payload: data, isEdit: isEdit };
                }
            }).then((result) => {
                if (result.isConfirmed) {
                    guardarCuenta(result.value.payload, result.value.isEdit);
                }
            });
        }).fail(() => Swal.fire('Error', 'No se pudo cargar el formulario', 'error'));
    };

    async function guardarCuenta(data, isEdit) {
        let urlSave = isEdit ? (typeof urls !== 'undefined' && urls.edit ? urls.edit : '/Account/Edit')
            : (typeof urls !== 'undefined' && urls.create ? urls.create : '/Account/Create');
        let method = isEdit ? 'PUT' : 'POST';

        try {
            const response = await $.ajax({
                url: urlSave,
                type: method,
                contentType: 'application/json',
                data: JSON.stringify(data),
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() }
            });

            if (response.success || response.ID || response.succeeded) {
                Swal.fire({
                    icon: 'success',
                    title: isEdit ? 'Actualizado' : 'Creado',
                    text: response.message || 'Cuenta guardada correctamente',
                    timer: 1500,
                    showConfirmButton: false
                });

                if (typeof loadData === 'function') {
                    loadData();
                } else {
                    setTimeout(() => window.location.reload(), 1500);
                }
            } else {
                Swal.fire('Error', response.message || 'Error al guardar la cuenta', 'error');
            }
        } catch (error) {
            console.error('❌ Error guardando:', error);
            Swal.fire({ icon: 'error', title: 'Error del servidor', text: error.responseJSON?.message || 'Revisa la consola.' });
        }
    }

    $(document).off('click', '.btn-editar').on('click', '.btn-editar', function () {
        const id = $(this).data('id');
        abrirModalCuenta(id);
    });

    // ==========================================
    // --- LÓGICA DE TRANSFERENCIA ---
    // ==========================================
    window.abrirModalTransferencia = function (originId) {
        let url = '/Account/GetTransferForm?originId=' + originId;

        $.get(url, function (html) {
            Swal.fire({
                title: 'Transferir saldo',
                html: html,
                width: '500px',
                showCancelButton: true,
                confirmButtonText: 'Transferir',
                confirmButtonColor: '#ffc107',
                cancelButtonText: 'Cancelar',
                didOpen: () => {
                    $('#TransferAmount').focus();

                    $('#chkUseMaxBalance').off('change').on('change', function () {
                        const isChecked = $(this).is(':checked');

                        if (isChecked) {
                            const saldoStr = $('#AvailableBalanceValue').val();
                            if (saldoStr) {
                                const saldoLimpio = saldoStr.replace(',', '.');
                                const saldoFloat = parseFloat(saldoLimpio);

                                if (saldoFloat <= 0) {
                                    $(this).prop('checked', false);
                                    $('#TransferAmount').val('');
                                    $('#transfer-warning').slideDown();
                                } else {
                                    $('#TransferAmount').val(saldoLimpio);
                                    $('#transfer-warning').slideUp();
                                }
                            }
                        } else {
                            $('#TransferAmount').val('');
                            $('#transfer-warning').slideUp();
                        }
                    });

                    $('#TransferAmount').on('input', function () {
                        $('#transfer-warning').slideUp();

                        const saldoStr = $('#AvailableBalanceValue').val();
                        const saldoLimpio = saldoStr ? saldoStr.replace(',', '.') : '0';
                        if ($(this).val() !== saldoLimpio) {
                            $('#chkUseMaxBalance').prop('checked', false);
                        }
                    });
                },
                preConfirm: () => {
                    const form = document.getElementById('form-transfer');
                    const formData = new FormData(form);
                    const data = Object.fromEntries(formData.entries());

                    data.OriginAccountId = parseInt(data.OriginAccountId);
                    data.DestinationAccountId = parseInt(data.DestinationAccountId);
                    data.Amount = parseFloat(data.Amount);

                    if (!data.DestinationAccountId || !data.Amount || data.Amount <= 0) {
                        Swal.showValidationMessage('Selecciona destino y un monto válido');
                        return false;
                    }
                    return data;
                }
            }).then((result) => {
                if (result.isConfirmed) {
                    enviarTransferencia(result.value);
                }
            });
        }).fail(() => Swal.fire('Atención', 'No se pudo cargar el formulario. Verifica que tengas otras cuentas en la misma moneda.', 'warning'));
    };

    function enviarTransferencia(data) {
        $.ajax({
            url: urls.transfer,
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(data),
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            success: (res) => {
                if (res.success) {
                    Swal.fire({ icon: 'success', title: '¡Transferencia Exitosa!', timer: 1500, showConfirmButton: false });
                    loadData();
                } else {
                    Swal.fire('Error', res.message, 'error');
                }
            },
            error: () => Swal.fire('Error', 'Error de conexión', 'error')
        });
    }

    $(document).off('click', '.btn-transfer').on('click', '.btn-transfer', function () {
        const id = $(this).data('id');
        abrirModalTransferencia(id);
    });

    // Inicializar
    loadData();
})();