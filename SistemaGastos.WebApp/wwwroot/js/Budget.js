(function () {
    // 1. Apuntamos al contenedor principal unificado
    const $container = $('#planning-container');
    if ($container.length === 0) return;

    // 2. Leemos TODAS las URLs que configuramos en el HTML
    const urls = {
        list: $container.data('url-budget-list'),
        form: $container.data('url-budget-form'),
        save: $container.data('url-budget-save'),
        delete: $container.data('url-budget-delete'),
        copy: $container.data('url-budget-copy')
    };

    // 3. FUNCIÓN CLAVE: Leer la fecha directamente del carrusel activo
    function getActiveDate() {
        const activeKey = $('.month-card.active').data('key');
        if (!activeKey) {
            const now = new Date();
            return { month: now.getMonth() + 1, year: now.getFullYear() };
        }
        const [year, month] = activeKey.split('-');
        return { year: parseInt(year), month: parseInt(month) };
    }

    // --- FUNCIONES GLOBALES (Expuestas a window para los onclick) ---

    // CARGAR TARJETAS DE PRESUPUESTO
    window.cargarPresupuestos = function (year, month) {
        // Si no nos pasan año/mes, lo sacamos del carrusel
        if (!year || !month) {
            const date = getActiveDate();
            year = date.year;
            month = date.month;
        }

        $('#budgets-grid').html('<div class="col-12 text-center py-4"><div class="spinner-border text-info spinner-border-sm"></div><span class="text-body-secondary ms-2 small">Calculando límites...</span></div>');

        // Concatenamos los parámetros a la URL base
        const urlList = `${urls.list}?month=${month}&year=${year}`;

        $.get(urlList, function (html) {
            $('#budgets-grid').html(html);
        }).fail(function () {
            $('#budgets-grid').html('<div class="alert alert-danger">Error al cargar presupuestos.</div>');
        });
    };

    // ABRIR MODAL (NUEVO O EDITAR)
    window.abrirModalPresupuesto = function (id = null) {
        const date = getActiveDate();
        let url = urls.form;
        url += id ? `?id=${id}` : `?month=${date.month}&year=${date.year}`;

        $.get(url, function (html) {
            Swal.fire({
                title: id ? 'Editar Límite' : 'Nuevo Presupuesto',
                html: html,
                width: '450px',
                showCancelButton: true,
                confirmButtonText: 'Guardar',
                cancelButtonText: 'Cancelar',
                preConfirm: () => {
                    const form = $('#form-budget');
                    const catId = parseInt(form.find('#CategoryID').val()) || 0;
                    const limit = parseFloat(form.find('#AmountLimit').val()) || 0;

                    if (catId === 0 || limit <= 0) {
                        Swal.showValidationMessage('Selecciona categoría y un monto válido.');
                        return false;
                    }

                    return {
                        ID: parseInt(form.find('#ID').val()) || 0,
                        CategoryID: catId,
                        AmountLimit: limit,
                        PeriodMonth: parseInt(form.find('#PeriodMonth').val()),
                        PeriodYear: parseInt(form.find('#PeriodYear').val())
                    };
                }
            }).then((res) => {
                if (res.isConfirmed) guardarPresupuesto(res.value);
            });
        });
    };

    // GUARDAR PRESUPUESTO
    function guardarPresupuesto(data) {
        $.ajax({
            url: urls.save,
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(data),
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            success: (res) => {
                if (res.success) {
                    Swal.fire({ icon: 'success', title: 'Guardado', timer: 1000, showConfirmButton: false });
                    window.cargarPresupuestos(); // Refresca las tarjetas sin recargar la página entera
                } else {
                    Swal.fire('Atención', res.message, 'warning');
                }
            },
            error: () => Swal.fire('Error', 'Error de servidor', 'error')
        });
    }

    // ELIMINAR PRESUPUESTO
    window.eliminarPresupuesto = function (id) {
        Swal.fire({
            title: '¿Eliminar límite?',
            text: "Dejarás de controlar esta categoría por este mes.",
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#d33',
            confirmButtonText: 'Sí, borrar',
            cancelButtonText: 'Cancelar'
        }).then((result) => {
            if (result.isConfirmed) {
                Swal.showLoading();
                $.ajax({
                    url: urls.delete,
                    type: 'POST',
                    contentType: 'application/json',
                    data: JSON.stringify(id),
                    headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
                    success: (res) => {
                        if (res.success) {
                            Swal.fire({ icon: 'success', title: 'Eliminado', showConfirmButton: false, timer: 1000 });
                            window.cargarPresupuestos();
                        } else {
                            Swal.fire('Error', res.message || 'No se pudo eliminar.', 'error');
                        }
                    }
                });
            }
        });
    };

    // COPIAR MES ANTERIOR
    window.copiarMesAnterior = function () {
        const date = getActiveDate();

        Swal.fire({
            title: '¿Copiar presupuestos?',
            text: `Traeremos los límites del mes pasado a ${date.month}/${date.year}.`,
            icon: 'question',
            showCancelButton: true,
            confirmButtonText: 'Sí, copiar',
            cancelButtonText: 'Cancelar'
        }).then((result) => {
            if (result.isConfirmed) {
                Swal.showLoading();
                $.ajax({
                    url: urls.copy,
                    type: 'POST',
                    data: { targetMonth: date.month, targetYear: date.year },
                    headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
                    success: (res) => {
                        if (res.success) {
                            Swal.fire({ icon: 'success', title: '¡Copiado!', timer: 1000, showConfirmButton: false });
                            window.cargarPresupuestos();
                        } else {
                            Swal.fire('Atención', res.message, 'warning');
                        }
                    }
                });
            }
        });
    };

    // VER DETALLE
    window.verDetallePresupuesto = function (id) {
        $.get('/Budget/GetDetails?id=' + id, function (html) {
            Swal.fire({ html: html, width: '600px', showConfirmButton: true, confirmButtonText: 'Cerrar', customClass: { popup: 'text-start' } });
        });
    };

    // --- EVENTOS DE INICIALIZACIÓN ---
    $(document).off('click', '#btnNewBudget').on('click', '#btnNewBudget', function (e) {
        e.preventDefault();
        window.abrirModalPresupuesto();
    });

})();