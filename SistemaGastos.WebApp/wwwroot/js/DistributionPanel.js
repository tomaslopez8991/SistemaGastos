// Widget compartido: panel de "repartir en varios días" para
// Movimientos planificados, Gastos fijos e Ingresos fijos.
window.DistributionPanel = (function () {

    function init($popup, options) {
        const {
            checkboxSelector,
            panelSelector,
            gridSelector,
            presetSelector = null,
            holidayStatusSelector = null,
            holidaysUrl = null,
            getStartDay,
            getMonthYear,
            initialEndDay    = null,
            initialExcludedDays = []
        } = options;

        const $check = $popup.find(checkboxSelector);
        const $panel = $popup.find(panelSelector);
        const $grid  = $popup.find(gridSelector);
        const $presets = presetSelector ? $popup.find(presetSelector) : $();
        const $holidayStatus = holidayStatusSelector ? $popup.find(holidayStatusSelector) : $();

        let selectedDays = new Set();
        let holidayMap = new Map();

        function escapeAttribute(value) {
            return String(value || '')
                .replaceAll('&', '&amp;')
                .replaceAll('"', '&quot;')
                .replaceAll('<', '&lt;')
                .replaceAll('>', '&gt;');
        }

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
                const holiday = holidayMap.get(d);
                const classes    = ['day-chip'];
                if (isDisabled)            classes.push('disabled');
                if (!isDisabled && !isSelected) classes.push('excluded');
                if (holiday) classes.push('holiday');

                $(`<button type="button" class="${classes.join(' ')}" data-day="${d}" title="${escapeAttribute(holiday)}">${d}</button>`)
                    .appendTo($grid);
            }
        }

        async function loadHolidays() {
            holidayMap = new Map();
            if (!holidaysUrl || typeof getMonthYear !== 'function') return;
            const { year, month } = getMonthYear();
            if (!year || !month) return;

            $holidayStatus.text('Consultando feriados nacionales y turísticos...');
            try {
                const response = await fetch(`${holidaysUrl}?year=${year}`);
                const holidays = response.ok ? await response.json() : [];
                holidays
                    .filter(h => {
                        const date = new Date(`${h.date}T00:00:00`);
                        return date.getFullYear() === year && date.getMonth() + 1 === month;
                    })
                    .forEach(h => holidayMap.set(parseInt(h.date.slice(8, 10)), h.name));
                $holidayStatus.text(holidayMap.size
                    ? `${holidayMap.size} feriado${holidayMap.size === 1 ? '' : 's'} excluido${holidayMap.size === 1 ? '' : 's'}.`
                    : 'Sin feriados nacionales o turísticos en este mes.');
            } catch {
                $holidayStatus.text('No se pudieron consultar feriados; podés ajustar los días manualmente.');
            }
        }

        async function applyPreset(mode) {
            const startDay = getStartDay() || 1;
            const daysInMonth = getDaysInMonth();
            const { year, month } = typeof getMonthYear === 'function' ? getMonthYear() : {};
            if (mode === 'business') await loadHolidays();
            else {
                holidayMap = new Map();
                $holidayStatus.text(mode === 'weekdays' ? 'Sábados y domingos excluidos.' : 'Se incluyen todos los días.');
            }

            selectedDays = new Set();
            for (let day = startDay; day <= daysInMonth; day++) {
                const date = year && month ? new Date(year, month - 1, day) : null;
                const weekend = date && (date.getDay() === 0 || date.getDay() === 6);
                if ((mode === 'weekdays' || mode === 'business') && weekend) continue;
                if (mode === 'business' && holidayMap.has(day)) continue;
                selectedDays.add(day);
            }
            $presets.removeClass('active').filter(`[data-mode="${mode}"]`).addClass('active');
            renderGrid();
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
            $presets.removeClass('active');
            $holidayStatus.text('Selección personalizada.');
        });

        $presets.off('click.distpanel').on('click.distpanel', function () {
            applyPreset($(this).data('mode'));
        });

        $check.off('change.distpanel').on('change.distpanel', function () {
            if ($check.is(':checked') && selectedDays.size === 0) {
                initSelectedDays();
                $presets.removeClass('active').filter('[data-mode="all"]').addClass('active');
            }
            applyToggleState(true);
        });

        if (initialEndDay) {
            $check.prop('checked', true);
            initSelectedDays();
            $presets.removeClass('active');
            $holidayStatus.text('Selección guardada.');
        }
        applyToggleState(false);

        return {
            refresh: function () {
                if (!$check.is(':checked')) return;
                const activeMode = $presets.filter('.active').data('mode');
                if (activeMode) applyPreset(activeMode);
                else renderGrid();
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
