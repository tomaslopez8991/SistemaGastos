(function () {
    const $container = $('#planning-container');
    if ($container.length === 0) return;

    const urlDailyBalances = $container.data('url-daily-balances');
    const fmtArs = new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS' });
    const fmtCompact = new Intl.NumberFormat('es-AR', {
        style: 'currency', currency: 'ARS', notation: 'compact', maximumFractionDigits: 1
    });

    const sourceLabels = {
        Planificado: 'Movimiento planificado',
        GastoFijo: 'Gasto fijo',
        IngresoFijo: 'Ingreso fijo',
        TarjetaCredito: 'Tarjeta de crédito',
        Personas: 'A cobrar (persona)'
    };

    let calendar = null;
    let balancesByDate = {};

    $('#calendarModal').on('shown.bs.modal', function () {
        if (!calendar) {
            initCalendar();
        } else {
            calendar.updateSize();
        }
    });

    function initCalendar() {
        const el = document.getElementById('financialCalendar');
        const today = new Date();
        const minDate = new Date(today.getFullYear(), today.getMonth(), 1);

        calendar = new FullCalendar.Calendar(el, {
            initialView: 'dayGridMonth',
            locale: 'es',
            firstDay: 1,
            height: 'auto',
            validRange: { start: minDate },
            headerToolbar: { left: 'prev,next today', center: 'title', right: '' },
            dayMaxEvents: 3,
            eventDisplay: 'block',
            datesSet: function (info) {
                const mid = new Date(info.view.currentStart.getFullYear(), info.view.currentStart.getMonth(), 15);
                loadMonth(mid.getFullYear(), mid.getMonth() + 1);
            },
            eventClick: function (info) {
                const day = balancesByDate[info.event.startStr];
                if (day) showDayDetail(day);
            },
            dateClick: function (info) {
                const day = balancesByDate[info.dateStr];
                if (day) showDayDetail(day);
            }
        });

        calendar.render();
    }

    function loadMonth(year, month) {
        $.get(urlDailyBalances, { year, month }, function (response) {
            if (!response.success || !response.data) return;
            renderMonth(response.data);
        }).fail(function () {
            Swal.fire('Error', 'No se pudo cargar el calendario de saldos', 'error');
        });
    }

    function renderMonth(data) {
        balancesByDate = {};

        const events = [];
        data.days.forEach(function (day) {
            balancesByDate[day.date] = day;
            day.items.forEach(function (item) {
                events.push({
                    title: `${item.isIncome ? '+' : '-'}${item.amountFmt} ${item.description}`,
                    start: day.date,
                    allDay: true,
                    classNames: [eventClassFor(item)]
                });
            });
        });

        calendar.removeAllEvents();
        calendar.addEventSource(events);

        setTimeout(function () { injectDayBalances(data.days); }, 60);
    }

    function eventClassFor(item) {
        if (item.sourceType === 'TarjetaCredito') return 'cf-evt-tc';
        if (item.sourceType === 'Personas') return 'cf-evt-personas';
        return item.isIncome ? 'cf-evt-income' : 'cf-evt-expense';
    }

    function injectDayBalances(days) {
        days.forEach(function (day) {
            const $cell = $(`.fc-daygrid-day[data-date="${day.date}"]`);
            if ($cell.length === 0) return;

            $cell.find('.cf-day-balance').remove();

            const cls = day.balance >= 0 ? 'text-success' : 'text-danger';
            $cell.find('.fc-daygrid-day-top').append(
                `<span class="cf-day-balance ${cls}">${fmtCompact.format(day.balance)}</span>`
            );
        });
    }

    function showDayDetail(day) {
        const dateObj = new Date(day.date + 'T00:00:00');
        const dateLabel = dateObj.toLocaleDateString('es-AR', { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' });
        const balanceClass = day.balance >= 0 ? 'text-success' : 'text-danger';

        let itemsHtml;
        if (day.items.length === 0) {
            itemsHtml = '<p class="text-body-secondary small mb-0">Sin movimientos para este día.</p>';
        } else {
            itemsHtml = day.items.map(function (item) {
                const cls = item.isIncome ? 'text-success' : 'text-danger';
                const sign = item.isIncome ? '+' : '-';
                return `<div class="d-flex justify-content-between align-items-start gap-2 border-bottom py-2">
                    <div>
                        <div class="text-body-emphasis">${item.description}</div>
                        <small class="text-body-secondary">${sourceLabels[item.sourceType] || item.sourceType}</small>
                    </div>
                    <span class="${cls} fw-semibold text-nowrap">${sign} ${item.amountFmt}</span>
                </div>`;
            }).join('');
        }

        Swal.fire({
            title: dateLabel.charAt(0).toUpperCase() + dateLabel.slice(1),
            html: `<div class="text-start">
                ${itemsHtml}
                <div class="d-flex justify-content-between mt-3 pt-2 border-top fw-bold">
                    <span>Saldo del día</span>
                    <span class="${balanceClass}">${day.balanceFmt}</span>
                </div>
            </div>`,
            confirmButtonText: 'Cerrar'
        });
    }
})();
