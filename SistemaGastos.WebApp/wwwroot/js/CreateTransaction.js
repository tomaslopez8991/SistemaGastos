(function () {
    // 1. FUNCIÓN DE ARRANQUE (BOOTSTRAP)
    function initCreatePage() {
        const $container = $('#create-page-container');
        // Si no estamos en la página de crear, abortamos silenciosamente
        if ($container.length === 0) return;

        // Si el contenedor está vacío, inyectamos la primera tarjeta automáticamente
        if ($('#unifiedTransactionsContainer .tx-block').length === 0) {
            agregarBloqueTransaccion();
        }
    }

    // 2. DISPARADORES (Cubrimos todos los escenarios de carga)
    $(document).ready(initCreatePage); // Carga normal (F5)
    $(document).on('turbo:load', initCreatePage); // Navegación tipo SPA (Turbo/Turbolinks)

    // ==========================================
    // 3. FUNCIONES PRINCIPALES
    // ==========================================
    function agregarBloqueTransaccion() {
        const template = document.getElementById('tplTransactionBlock').content.cloneNode(true);
        const $block = $(template).find('.tx-block');
        const today = new Date().toISOString().split('T')[0];

        $block.find('.tx-date').val(today);
        $('#unifiedTransactionsContainer').append($block);

        // Le agregamos su primera fila de cuenta (Split)
        agregarFilaSplit($block.find('.splits-container'));

        // Toggle % atribuido al seleccionar persona
        $block.find('.tx-person').on('change', function () {
            const $wrap = $(this).closest('.row').find('.tx-pct-wrapper');
            if ($(this).val()) {
                $wrap.show();
            } else {
                $wrap.hide();
                $wrap.find('.tx-person-pct').val(100);
            }
        });
        actualizarMetricasUnificadas();
    }

    function agregarFilaSplit($container) {
        const template = document.getElementById('tplSplitRow').content.cloneNode(true);
        $container.append($(template).find('.split-row'));

        if ($container.find('.split-row').length > 1) {
            $container.find('.btn-remove-split').show();
        }
    }

    function actualizarMetricasUnificadas() {
        let totalLote = 0;
        $('.tx-block').each(function (index) {
            $(this).find('.tx-number').text(`#${index + 1}`);
        });
        $('.split-amount').each(function () {
            totalLote += parseFloat($(this).val()) || 0;
        });

        $('#lblTotalTxCount').text($('.tx-block').length);
        $('#lblTotalBatchAmount').text('$ ' + totalLote.toLocaleString('es-AR', { minimumFractionDigits: 2, maximumFractionDigits: 2 }));
    }

    // ==========================================
    // 4. EVENTOS (DELEGADOS PARA QUE NO SE PIERDAN NUNCA)
    // ==========================================

    // Botón: Agregar otro movimiento al lote
    $(document).off('click', '#btnAddNewTxBlock').on('click', '#btnAddNewTxBlock', function () {
        agregarBloqueTransaccion();
    });

    // Botón: Eliminar movimiento
    $(document).off('click', '.btn-remove-tx').on('click', '.btn-remove-tx', function () {
        if ($('.tx-block').length > 1) {
            $(this).closest('.tx-block').remove();
            actualizarMetricasUnificadas();
        } else {
            Swal.fire('Atención', 'Debe haber al menos un movimiento en la pantalla.', 'warning');
        }
    });

    // Botón: Dividir pago (Agregar cuenta)
    $(document).off('click', '.btn-add-split').on('click', '.btn-add-split', function () {
        agregarFilaSplit($(this).closest('.splits-wrapper').find('.splits-container'));
    });

    // Botón: Eliminar cuenta
    $(document).off('click', '.btn-remove-split').on('click', '.btn-remove-split', function () {
        const $container = $(this).closest('.splits-container');
        $(this).closest('.split-row').remove();

        if ($container.find('.split-row').length === 1) {
            $container.find('.btn-remove-split').hide();
        }
        actualizarMetricasUnificadas();
    });

    // Input: Recalcular totales al escribir
    $(document).off('input', '.split-amount').on('input', '.split-amount', function () {
        actualizarMetricasUnificadas();
    });

    // Botón: GUARDAR TODO EL LOTE
    $(document).off('submit', '#unifiedTransactionForm').on('submit', '#unifiedTransactionForm', async function (e) {
        e.preventDefault();

        const $container = $('#create-page-container');
        const urlCreate = $container.data('url-create');
        const urlIndex = $container.data('url-index');

        const dataPayload = [];
        let isFormValid = true;

        $('.tx-block').each(function () {
            const $block = $(this);
            const dateVal = $block.find('.tx-date').val();
            const catId = parseInt($block.find('.tx-category').val());
            const descVal = $block.find('.tx-desc').val() || '';
            const splits = [];

            $block.find('.split-row').each(function () {
                const accId = parseInt($(this).find('.split-account').val());
                const amt = parseFloat($(this).find('.split-amount').val()) || 0;
                if (accId && amt > 0) splits.push({ AccountID: accId, Amount: amt });
            });

            if (!dateVal || !catId || splits.length === 0) {
                $block.removeClass('border-primary border-opacity-50').addClass('border-danger border-2');
                isFormValid = false;
            } else {
                $block.removeClass('border-danger border-2').addClass('border-primary border-opacity-50');

                // Mapeamos el DTO clonando la primera cuenta a la raíz para evadir al validador genérico
                const personId = parseInt($block.find('.tx-person').val()) || null;
                const personPct = personId ? (parseFloat($block.find('.tx-person-pct').val()) || 100) : null;
                dataPayload.push({
                    Date: dateVal,
                    CategoryID: catId,
                    Description: descVal,
                    FixedExpenseID: null,
                    PersonID: personId,
                    PersonPercentage: personPct,
                    AccountID: splits[0].AccountID,
                    Amount: splits[0].Amount,
                    Splits: splits
                });
            }
        });

        if (!isFormValid) {
            Swal.fire('Atención', 'Revisa los movimientos marcados en rojo. Faltan categorías, cuentas o montos.', 'warning');
            return;
        }

        const $btn = $('#btnSaveUnified');
        const textOriginal = $btn.html();
        $btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-2"></span>Guardando...');

        try {
            const response = await $.ajax({
                url: urlCreate,
                type: 'POST',
                contentType: 'application/json',
                data: JSON.stringify(dataPayload),
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() }
            });

            if (response.success || response.succeeded) {
                Swal.fire({ icon: 'success', title: '¡Guardado con éxito!', timer: 1500, showConfirmButton: false }).then(() => {
                    if (typeof Turbo !== 'undefined') {
                        Turbo.visit(urlIndex);
                    } else {
                        window.location.href = urlIndex;
                    }
                });
            } else {
                Swal.fire('Error', response.message || 'Error al guardar', 'error');
            }
        } catch (error) {
            console.error(error);
            Swal.fire('Error', 'Problema de comunicación con el servidor', 'error');
        } finally {
            $btn.prop('disabled', false).html(textOriginal);
        }
    });
})();