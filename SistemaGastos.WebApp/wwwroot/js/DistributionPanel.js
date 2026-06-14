// Widget compartido: panel de "repartir en varios días" para
// Movimientos planificados, Gastos fijos e Ingresos fijos.
window.DistributionPanel = (function () {

    function init($popup, options) {
        const {
            checkboxSelector,
            panelSelector,
            endDayInputSelector,
            gridSelector,
            getStartDay,
            initialEndDay = null,
            initialExcludedDays = []
        } = options;

        const $check = $popup.find(checkboxSelector);
        const $panel = $popup.find(panelSelector);
        const $endDayInput = $popup.find(endDayInputSelector);
        const $grid = $popup.find(gridSelector);

        const excludedDays = new Set(initialExcludedDays);

        function getRange() {
            const startDay = getStartDay();
            const endDay = parseInt($endDayInput.val()) || null;
            return { startDay, endDay };
        }

        function renderGrid() {
            const { startDay, endDay } = getRange();
            $grid.empty();

            if (!startDay || !endDay || endDay <= startDay) {
                $grid.append('<p class="tmp-days-grid-hint mb-0">Indicá un día final mayor al día de inicio para repartir el monto.</p>');
                return;
            }

            const maxDay = Math.min(endDay, 31);

            for (let day = startDay; day <= maxDay; day++) {
                const isExcluded = excludedDays.has(day);
                $(`<button type="button" class="day-chip ${isExcluded ? 'excluded' : ''}" data-day="${day}">${day}</button>`)
                    .appendTo($grid);
            }
        }

        function applyToggleState(animate) {
            if ($check.is(':checked')) {
                if (!$endDayInput.val()) {
                    const startDay = getStartDay() || 1;
                    $endDayInput.val(Math.min(startDay + 1, 31));
                }
                animate ? $panel.slideDown() : $panel.show();
                renderGrid();
            } else {
                animate ? $panel.slideUp() : $panel.hide();
            }
        }

        $grid.off('click.distpanel').on('click.distpanel', '.day-chip', function () {
            const day = parseInt($(this).data('day'));
            if (excludedDays.has(day)) {
                excludedDays.delete(day);
                $(this).removeClass('excluded');
            } else {
                excludedDays.add(day);
                $(this).addClass('excluded');
            }
        });

        $check.off('change.distpanel').on('change.distpanel', function () {
            applyToggleState(true);
        });

        $endDayInput.off('input.distpanel change.distpanel').on('input.distpanel change.distpanel', renderGrid);

        // Estado inicial
        $endDayInput.attr('min', 1).attr('max', 31);
        if (initialEndDay) {
            $check.prop('checked', true);
            $endDayInput.val(initialEndDay);
        }
        applyToggleState(false);

        return {
            refresh: renderGrid,
            getValues: function () {
                if (!$check.is(':checked')) {
                    return { distributionEndDay: null, excludedDays: null };
                }

                const { startDay, endDay } = getRange();
                if (!endDay || endDay <= startDay) {
                    return { distributionEndDay: null, excludedDays: null };
                }

                const maxDay = Math.min(endDay, 31);
                const excluded = Array.from(excludedDays)
                    .filter(d => d >= startDay && d <= maxDay)
                    .sort((a, b) => a - b);

                return {
                    distributionEndDay: endDay,
                    excludedDays: excluded.length > 0 ? excluded.join(',') : null
                };
            }
        };
    }

    return { init };
})();
