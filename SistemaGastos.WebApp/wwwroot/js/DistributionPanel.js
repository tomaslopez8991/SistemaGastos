// Widget compartido: panel de "repartir en varios días" para
// Movimientos planificados, Gastos fijos e Ingresos fijos.
window.DistributionPanel = (function () {

    function init($popup, options) {
        const {
            checkboxSelector,
            panelSelector,
            gridSelector,
            getStartDay,
            getMonthYear,
            initialEndDay    = null,
            initialExcludedDays = []
        } = options;

        const $check = $popup.find(checkboxSelector);
        const $panel = $popup.find(panelSelector);
        const $grid  = $popup.find(gridSelector);

        let selectedDays = new Set();

        function getDaysInMonth() {
            if (typeof getMonthYear === 'function') {
                const { year, month } = getMonthYear();
                if (year && month) return new Date(year, month, 0).getDate();
            }
            return 31;
        }

        function initSelectedDays() {
            const startDay    = getStartDay() || 1;
            const daysInMonth = getDaysInMonth();
            selectedDays      = new Set();

            if (initialEndDay) {
                const endDay     = Math.min(initialEndDay, daysInMonth);
                const excludeSet = new Set(initialExcludedDays);
                for (let d = startDay; d <= endDay; d++) {
                    if (!excludeSet.has(d)) selectedDays.add(d);
                }
            } else {
                for (let d = startDay; d <= daysInMonth; d++) {
                    selectedDays.add(d);
                }
            }
        }

        function renderGrid() {
            const startDay    = getStartDay() || 1;
            const daysInMonth = getDaysInMonth();
            $grid.empty();

            for (let d = 1; d <= daysInMonth; d++) {
                const isDisabled = d < startDay;
                const isSelected = selectedDays.has(d);
                const classes    = ['day-chip'];
                if (isDisabled)            classes.push('disabled');
                if (!isDisabled && !isSelected) classes.push('excluded');

                $(`<button type="button" class="${classes.join(' ')}" data-day="${d}">${d}</button>`)
                    .appendTo($grid);
            }
        }

        function applyToggleState(animate) {
            if ($check.is(':checked')) {
                animate ? $panel.slideDown() : $panel.show();
                renderGrid();
            } else {
                animate ? $panel.slideUp() : $panel.hide();
            }
        }

        $grid.off('click.distpanel').on('click.distpanel', '.day-chip:not(.disabled)', function () {
            const day = parseInt($(this).data('day'));
            if (selectedDays.has(day)) {
                selectedDays.delete(day);
                $(this).addClass('excluded');
            } else {
                selectedDays.add(day);
                $(this).removeClass('excluded');
            }
        });

        $check.off('change.distpanel').on('change.distpanel', function () {
            if ($check.is(':checked') && selectedDays.size === 0) {
                initSelectedDays();
            }
            applyToggleState(true);
        });

        if (initialEndDay) {
            $check.prop('checked', true);
            initSelectedDays();
        }
        applyToggleState(false);

        return {
            refresh: function () {
                if ($check.is(':checked')) renderGrid();
            },
            getValues: function () {
                if (!$check.is(':checked') || selectedDays.size === 0) {
                    return { distributionEndDay: null, excludedDays: null };
                }

                const startDay    = getStartDay() || 1;
                const validDays   = [...selectedDays].filter(d => d >= startDay);
                if (validDays.length === 0) {
                    return { distributionEndDay: null, excludedDays: null };
                }

                const endDay  = Math.max(...validDays);
                if (endDay <= startDay) {
                    return { distributionEndDay: null, excludedDays: null };
                }

                const excluded = [];
                for (let d = startDay; d <= endDay; d++) {
                    if (!selectedDays.has(d)) excluded.push(d);
                }

                return {
                    distributionEndDay: endDay,
                    excludedDays: excluded.length > 0 ? excluded.join(',') : null
                };
            }
        };
    }

    return { init };
})();
