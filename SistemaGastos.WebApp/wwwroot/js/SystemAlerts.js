(function () {
    const $container = $('#system-alerts-container');
    if ($container.length === 0) return;

    const urlLogs = $container.data('url-logs');

    function severityClass(ms) {
        if (ms >= 2000) return 'sa-badge-critical';
        if (ms >= 1000) return 'sa-badge-warning';
        return 'sa-badge-slow';
    }

    function severityLabel(ms) {
        if (ms >= 2000) return 'Crítico';
        if (ms >= 1000) return 'Alto';
        return 'Lento';
    }

    // ── Grid ──────────────────────────────────────────────────
    const wrapper = document.getElementById('alerts-grid-wrapper');
    if (wrapper) wrapper.innerHTML = '';

    const grid = new gridjs.Grid({
        columns: [
            {
                id: 'createdAt',
                name: 'Fecha',
                width: '160px',
                formatter: (cell) => gridjs.html(
                    `<span class="tx-date">${new Date(cell).toLocaleString('es-AR')}</span>`
                )
            },
            {
                id: 'handlerName',
                name: 'Handler',
                formatter: (cell) => gridjs.html(`<span class="sa-handler-name">${cell}</span>`)
            },
            {
                id: 'elapsedMs',
                name: 'Tiempo',
                width: '130px',
                formatter: (cell) => gridjs.html(`
                    <div class="d-flex align-items-center gap-2">
                        <span class="sa-ms-value">${cell}ms</span>
                        <span class="tx-badge ${severityClass(cell)}">${severityLabel(cell)}</span>
                    </div>
                `)
            },
            {
                id: 'requestData',
                name: 'Request payload',
                formatter: (cell) => {
                    if (!cell) return '—';
                    const escaped = cell.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
                    const preview = escaped.length > 80 ? escaped.substring(0, 80) + '…' : escaped;
                    return gridjs.html(
                        `<code class="sa-payload" title="${escaped}">${preview}</code>`
                    );
                }
            }
        ],
        pagination: { limit: 25, server: { url: (prev, page, limit) => `${prev}${prev.includes('?') ? '&' : '?'}page=${page + 1}&pageSize=${limit}` } },
        server: {
            url: urlLogs,
            total: data => data.total,
            then: data => {
                // Actualizar stats con la primera carga
                if (data.total !== undefined) {
                    $('#saTotalCount').text(data.total);
                }
                const items = data.results || [];
                if (items.length) {
                    const max = Math.max(...items.map(x => x.elapsedMs));
                    const avg = Math.round(items.reduce((s, x) => s + x.elapsedMs, 0) / items.length);
                    $('#saMaxMs').text(max + 'ms');
                    $('#saAvgMs').text(avg + 'ms');
                }
                return items.map(x => [x.createdAt, x.handlerName, x.elapsedMs, x.requestData]);
            }
        },
        language: {
            pagination: { previous: 'Anterior', next: 'Siguiente', showing: 'Mostrando', results: () => 'alertas', to: 'a', of: 'de' },
            loading: 'Cargando...',
            noRecordsFound: 'No hay handlers lentos registrados 🎉',
            error: 'Error al cargar los datos'
        },
        className: { table: 'table align-middle mb-0' }
    }).render(wrapper);

    // ── Limpiar logs (solo UI, recarga vacío) ─────────────────
    $('#btnClearLogs').on('click', function () {
        Swal.fire({
            title: '¿Limpiar todos los logs?',
            text: 'Esta acción no se puede deshacer.',
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#d33',
            confirmButtonText: 'Sí, limpiar',
            cancelButtonText: 'Cancelar'
        }).then(result => {
            if (result.isConfirmed) {
                $.ajax({
                    url: '/SystemAlerts/ClearLogs',
                    type: 'DELETE',
                    headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
                    success: () => {
                        grid.forceRender();
                        $('#saTotalCount').text('0');
                        $('#saMaxMs').text('—');
                        $('#saAvgMs').text('—');
                        Swal.fire({ icon: 'success', title: 'Logs eliminados', timer: 1500, showConfirmButton: false });
                    }
                });
            }
        });
    });
})();
