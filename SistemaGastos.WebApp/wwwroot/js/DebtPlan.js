'use strict';

(function () {
    const container = document.getElementById('debt-plan-container');
    if (!container) return;

    const urlData = container.dataset.urlData;
    const urlSave = '/TmpTransaction/SaveDebtPlanSettings';

    let planData = null;
    let chart = null;
    let recalcTimer = null;
    let saveTimer = null;
    let saveIndicatorTimer = null;
    let isScenariosMode = false;

    // Estado previo para animar transiciones de las cards de resumen
    let prevMinExtra = null;
    let prevReduction = null;
    let prevBalanceAnimated = false;
    let firstRender = true;

    const DEFAULT_SETTINGS = {
        goalType: 'maxNegative',
        goalValue: 1500000,
        extraMonthlyIncome: 0,
        scenariosMode: false,
        scenarioMin: 150000,
        scenarioNormal: 350000,
        scenarioMax: 600000,
        removedFixedExpenseIds: []
    };

    const fmt = new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS', maximumFractionDigits: 0 });

    function formatARS(val) { return fmt.format(val); }
    function formatShort(val) {
        const abs = Math.abs(val);
        if (abs >= 1_000_000) return (val / 1_000_000).toFixed(1) + 'M';
        if (abs >= 1_000) return Math.round(val / 1_000) + 'k';
        return val.toString();
    }

    // ── Formato de miles para inputs (1.500.000) ───────────────────────────────
    function formatThousands(value) {
        let digits = (value || '').toString().replace(/\D/g, '');
        digits = digits.replace(/^0+(?=\d)/, '');
        return digits.replace(/\B(?=(\d{3})+(?!\d))/g, '.');
    }

    function parseAmount(str) {
        return parseInt((str || '0').replace(/\./g, '')) || 0;
    }

    function setFormattedValue(id, num) {
        const el = document.getElementById(id);
        if (el) el.value = formatThousands(String(Math.max(0, Math.round(num || 0))));
    }

    function attachThousandsMask(input) {
        input.addEventListener('input', () => {
            const cursorFromEnd = input.value.length - (input.selectionStart ?? input.value.length);
            input.value = formatThousands(input.value);
            const pos = Math.max(0, input.value.length - cursorFromEnd);
            input.setSelectionRange(pos, pos);
        });
    }

    // ── Init ─────────────────────────────────────────────────────────────────
    async function init() {
        try {
            prevMinExtra = null;
            prevReduction = null;
            prevBalanceAnimated = false;
            firstRender = true;

            const res = await fetch(urlData);
            const json = await res.json();
            if (!json.success) throw new Error(json.message || 'Error al obtener datos');
            planData = json.data;

            // Destruir chart previo si Turbo restauró la página del caché
            if (chart) { chart.destroy(); chart = null; }

            renderExpensesList();
            applySettings(planData.settings || DEFAULT_SETTINGS);
            initChart();
            bindEvents();
            updateGoalHint();
            recalculate();

            document.getElementById('dp-loading').style.display = 'none';
            document.getElementById('dp-content').style.display = '';
        } catch (e) {
            document.getElementById('dp-loading').innerHTML =
                `<div class="alert alert-danger mx-auto text-start" style="max-width:500px;">
                    <i class="fas fa-exclamation-triangle me-2"></i><strong>Error al cargar las proyecciones</strong><br>
                    <small class="text-muted">${e.message}</small>
                </div>`;
        }
    }

    // ── Aplicar última configuración guardada (o valores por defecto) ─────────
    function applySettings(settings) {
        const s = Object.assign({}, DEFAULT_SETTINGS, settings || {});

        const radio = document.querySelector(`[name="goalType"][value="${s.goalType}"]`);
        if (radio) radio.checked = true;

        setFormattedValue('dp-goal-value', s.goalValue);

        isScenariosMode = !!s.scenariosMode;

        const slider = document.getElementById('dp-extra-slider');
        const sliderMax = parseInt(slider.max) || 1500000;
        const extra = Math.min(Math.max(0, Math.round(s.extraMonthlyIncome || 0)), sliderMax);
        slider.value = extra;
        document.getElementById('dp-extra-display').textContent = formatARS(extra);
        highlightActivePill(extra);

        setFormattedValue('dp-s1', s.scenarioMin);
        setFormattedValue('dp-s2', s.scenarioNormal);
        setFormattedValue('dp-s3', s.scenarioMax);

        document.getElementById('dp-single-mode').style.display = isScenariosMode ? 'none' : '';
        document.getElementById('dp-scenarios-mode').style.display = isScenariosMode ? '' : 'none';
        const toggleBtn = document.getElementById('dp-toggle-scenarios');
        toggleBtn.classList.toggle('btn-outline-secondary', !isScenariosMode);
        toggleBtn.classList.toggle('btn-primary', isScenariosMode);

        const removedIds = (s.removedFixedExpenseIds || []).map(String);
        document.querySelectorAll('.dp-expense-check').forEach(cb => {
            cb.checked = removedIds.includes(String(cb.value));
        });
    }

    // ── Recolectar configuración actual del formulario ─────────────────────────
    function gatherSettings() {
        return {
            goalType: document.querySelector('[name="goalType"]:checked')?.value || 'maxNegative',
            goalValue: parseAmount(document.getElementById('dp-goal-value').value),
            extraMonthlyIncome: parseInt(document.getElementById('dp-extra-slider').value) || 0,
            scenariosMode: isScenariosMode,
            scenarioMin: parseAmount(document.getElementById('dp-s1').value),
            scenarioNormal: parseAmount(document.getElementById('dp-s2').value),
            scenarioMax: parseAmount(document.getElementById('dp-s3').value),
            removedFixedExpenseIds: Array.from(document.querySelectorAll('.dp-expense-check:checked'))
                .map(cb => parseInt(cb.value))
        };
    }

    // ── Auto-guardado (debounced) ───────────────────────────────────────────────
    function scheduleSave() {
        showSaveIndicator('saving');
        clearTimeout(saveTimer);
        saveTimer = setTimeout(doSave, 700);
    }

    async function doSave() {
        try {
            const res = await fetch(urlSave, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(gatherSettings())
            });
            const json = await res.json();
            showSaveIndicator(json.success ? 'saved' : 'error');
        } catch (e) {
            showSaveIndicator('error');
        }
    }

    function showSaveIndicator(state) {
        const el = document.getElementById('dp-save-indicator');
        if (!el) return;
        const icon = el.querySelector('i');
        const text = document.getElementById('dp-save-text');

        clearTimeout(saveIndicatorTimer);
        el.classList.add('show');
        el.classList.remove('saved', 'error');

        if (state === 'saving') {
            icon.className = 'fa-solid fa-circle-notch fa-spin';
            text.textContent = 'Guardando...';
        } else if (state === 'saved') {
            icon.className = 'fa-solid fa-check';
            text.textContent = 'Guardado';
            el.classList.add('saved');
            saveIndicatorTimer = setTimeout(() => el.classList.remove('show'), 2000);
        } else {
            icon.className = 'fa-solid fa-triangle-exclamation';
            text.textContent = 'Error al guardar';
            el.classList.add('error');
            saveIndicatorTimer = setTimeout(() => el.classList.remove('show'), 3000);
        }
    }

    // ── Restablecer a valores por defecto ───────────────────────────────────────
    function handleReset() {
        const doReset = () => {
            applySettings(DEFAULT_SETTINGS);
            updateGoalHint();
            recalculate();
            scheduleSave();
        };

        if (window.Swal) {
            Swal.fire({
                title: '¿Restablecer simulación?',
                text: 'Se perderán los valores configurados y se volverá a los valores por defecto.',
                icon: 'warning',
                showCancelButton: true,
                confirmButtonText: 'Sí, restablecer',
                cancelButtonText: 'Cancelar',
                confirmButtonColor: '#dc3545',
                reverseButtons: true
            }).then(result => { if (result.isConfirmed) doReset(); });
        } else if (confirm('¿Restablecer la simulación a los valores por defecto?')) {
            doReset();
        }
    }

    // ── Render lista de gastos fijos ──────────────────────────────────────────
    function renderExpensesList() {
        const list = document.getElementById('dp-expenses-list');
        if (!planData.fixedExpenses || !planData.fixedExpenses.length) {
            list.innerHTML = '<p class="text-muted small text-center py-3 mb-0">No hay gastos fijos activos para simular.</p>';
            return;
        }
        list.innerHTML = planData.fixedExpenses.map(fe => {
            const logoHtml = fe.logoUrl
                ? `<img src="${fe.logoUrl}" alt="" class="rounded-circle flex-shrink-0"
                       style="width:26px;height:26px;object-fit:cover;"
                       onerror="this.style.display='none';this.nextElementSibling.style.display='flex';">
                   <div class="rounded-circle bg-body-secondary flex-shrink-0 align-items-center justify-content-center"
                        style="width:26px;height:26px;display:none;font-size:10px;">
                       <i class="fas fa-repeat text-secondary"></i></div>`
                : `<div class="rounded-circle bg-body-secondary flex-shrink-0 d-flex align-items-center justify-content-center"
                       style="width:26px;height:26px;font-size:10px;">
                       <i class="fas fa-repeat text-secondary"></i></div>`;
            return `
                <label class="dp-expense-item d-flex align-items-center gap-2 rounded px-2 py-2"
                       style="cursor:pointer;" data-id="${fe.id}">
                    <input type="checkbox" class="form-check-input dp-expense-check flex-shrink-0 m-0"
                           value="${fe.id}" data-amount="${fe.monthlyAmountARS}">
                    ${logoHtml}
                    <span class="flex-grow-1 small text-truncate" title="${fe.name}">${fe.name}</span>
                    <span class="text-danger small fw-semibold flex-shrink-0 ms-auto">${fe.monthlyAmountARSFmt}/mes</span>
                </label>`;
        }).join('');

        list.querySelectorAll('.dp-expense-check').forEach(cb =>
            cb.addEventListener('change', () => { scheduleRecalc(); scheduleSave(); }));
    }

    // ── Chart init ────────────────────────────────────────────────────────────
    function initChart() {
        const ctx = document.getElementById('dp-chart').getContext('2d');
        chart = new Chart(ctx, {
            type: 'line',
            data: { labels: planData.months.map(m => m.label), datasets: [] },
            options: {
                responsive: true,
                maintainAspectRatio: true,
                animation: { duration: 350, easing: 'easeInOutQuart' },
                interaction: { mode: 'index', intersect: false },
                onHover: (evt, elements) => {
                    if (elements.length) {
                        highlightMonth(elements[0].index, false);
                    } else {
                        clearHighlight(false);
                    }
                },
                plugins: {
                    legend: { display: false },
                    tooltip: {
                        callbacks: {
                            label: ctx => ` ${ctx.dataset.label}: ${formatARS(ctx.parsed.y)}`
                        }
                    }
                },
                scales: {
                    x: { grid: { display: false }, ticks: { font: { size: 11 } } },
                    y: {
                        ticks: {
                            font: { size: 11 },
                            callback: val => {
                                const abs = Math.abs(val);
                                if (abs >= 1_000_000) return (val / 1_000_000).toFixed(1) + 'M';
                                if (abs >= 1_000) return (val / 1_000).toFixed(0) + 'k';
                                return val;
                            }
                        }
                    }
                }
            }
        });
    }

    // ── Sincronización hover gráfico ↔ tabla ↔ barra de salud ──────────────────
    function highlightMonth(idx, syncChart) {
        document.querySelectorAll('#dp-table-body tr').forEach(tr => {
            tr.classList.toggle('dp-row-hover', parseInt(tr.dataset.monthIndex, 10) === idx);
        });
        document.querySelectorAll('.dp-progress-segment').forEach(seg => {
            seg.classList.toggle('dp-row-hover', parseInt(seg.dataset.monthIndex, 10) === idx);
        });
        if (syncChart !== false && chart) {
            const active = chart.data.datasets.map((_, dsIdx) => ({ datasetIndex: dsIdx, index: idx }));
            chart.setActiveElements(active);
            chart.tooltip.setActiveElements(active, { x: 0, y: 0 });
            chart.update();
        }
    }

    function clearHighlight(syncChart) {
        document.querySelectorAll('#dp-table-body tr.dp-row-hover').forEach(tr => tr.classList.remove('dp-row-hover'));
        document.querySelectorAll('.dp-progress-segment.dp-row-hover').forEach(seg => seg.classList.remove('dp-row-hover'));
        if (syncChart !== false && chart) {
            chart.setActiveElements([]);
            chart.tooltip.setActiveElements([], { x: 0, y: 0 });
            chart.update();
        }
    }

    // ── Bind events ───────────────────────────────────────────────────────────
    function bindEvents() {
        document.querySelectorAll('[name="goalType"]').forEach(r =>
            r.addEventListener('change', () => { updateGoalHint(); scheduleRecalc(); scheduleSave(); }));

        const goalInput = document.getElementById('dp-goal-value');
        attachThousandsMask(goalInput);
        goalInput.addEventListener('input', () => { scheduleRecalc(); scheduleSave(); });

        const slider = document.getElementById('dp-extra-slider');
        slider.addEventListener('input', () => {
            const val = parseInt(slider.value);
            document.getElementById('dp-extra-display').textContent = formatARS(val);
            highlightActivePill(val);
            scheduleRecalc();
            scheduleSave();
        });

        document.querySelectorAll('.dp-quick-pill').forEach(pill =>
            pill.addEventListener('click', () => {
                const val = parseInt(pill.dataset.value);
                slider.value = val;
                document.getElementById('dp-extra-display').textContent = formatARS(val);
                highlightActivePill(val);
                scheduleRecalc();
                scheduleSave();
            }));

        document.getElementById('dp-toggle-scenarios').addEventListener('click', () => {
            isScenariosMode = !isScenariosMode;
            document.getElementById('dp-single-mode').style.display = isScenariosMode ? 'none' : '';
            document.getElementById('dp-scenarios-mode').style.display = isScenariosMode ? '' : 'none';
            const btn = document.getElementById('dp-toggle-scenarios');
            btn.classList.toggle('btn-outline-secondary', !isScenariosMode);
            btn.classList.toggle('btn-primary', isScenariosMode);
            scheduleRecalc();
            scheduleSave();
        });

        document.querySelectorAll('.dp-scenario-input').forEach(input => {
            attachThousandsMask(input);
            input.addEventListener('input', () => { scheduleRecalc(); scheduleSave(); });
        });

        const resetBtn = document.getElementById('dp-reset-btn');
        if (resetBtn) resetBtn.addEventListener('click', handleReset);
    }

    function updateGoalHint() {
        const type = document.querySelector('[name="goalType"]:checked')?.value;
        const wrapper = document.getElementById('dp-goal-value-wrapper');
        const hint = document.getElementById('dp-goal-hint');
        if (type === 'reachZero') {
            wrapper.style.display = 'none';
        } else {
            wrapper.style.display = '';
            hint.textContent = type === 'maxNegative'
                ? 'Máximo saldo negativo permitido por mes'
                : 'Saldo positivo a alcanzar';
        }
    }

    function highlightActivePill(val) {
        document.querySelectorAll('.dp-quick-pill').forEach(p => {
            const active = parseInt(p.dataset.value) === val;
            p.className = 'badge rounded-pill dp-quick-pill ' +
                (active ? 'bg-success text-white border border-success' : 'bg-body-secondary border text-body-secondary');
        });
    }

    // ── Debounce ──────────────────────────────────────────────────────────────
    function scheduleRecalc() {
        clearTimeout(recalcTimer);
        recalcTimer = setTimeout(recalculate, 220);
    }

    // ── Recalcular ────────────────────────────────────────────────────────────
    function recalculate() {
        if (!planData) return;

        const goalType = document.querySelector('[name="goalType"]:checked')?.value || 'maxNegative';
        const rawVal = parseAmount(document.getElementById('dp-goal-value').value);
        const threshold = computeThreshold(goalType, rawVal);

        const removedIds = Array.from(document.querySelectorAll('.dp-expense-check:checked'))
            .map(cb => parseInt(cb.value));

        if (isScenariosMode) {
            doScenarios(threshold, removedIds);
        } else {
            const extra = parseInt(document.getElementById('dp-extra-slider').value) || 0;
            doSingle(threshold, removedIds, extra);
        }
    }

    // ── Modo simple ───────────────────────────────────────────────────────────
    function doSingle(threshold, removedIds, extraIncome) {
        const base = project(0, [], threshold);
        const sim = project(extraIncome, removedIds, threshold);

        const datasets = [
            makeDataset('Sin cambios', base.balances, '#6c757d', true)
        ];
        if (extraIncome > 0 || removedIds.length > 0)
            datasets.push(makeDataset('Con simulación', sim.balances, '#0d6efd', false));
        datasets.push(makeGoalLine(threshold));

        updateChart(datasets);
        updateSummaryCards(sim, extraIncome, removedIds, threshold);
        updateTable(sim.months, threshold, false);
        updateProgressBar(sim.months);
    }

    // ── Modo 3 escenarios ─────────────────────────────────────────────────────
    function doScenarios(threshold, removedIds) {
        const s1 = parseAmount(document.getElementById('dp-s1').value);
        const s2 = parseAmount(document.getElementById('dp-s2').value);
        const s3 = parseAmount(document.getElementById('dp-s3').value);

        const r0 = project(0, [], threshold);
        const r1 = project(s1, removedIds, threshold);
        const r2 = project(s2, removedIds, threshold);
        const r3 = project(s3, removedIds, threshold);

        const datasets = [
            makeDataset('Sin cambios', r0.balances, '#6c757d', true),
            makeDataset(`Mínimo +${formatShort(s1)}`, r1.balances, '#fd7e14', false),
            makeDataset(`Normal +${formatShort(s2)}`, r2.balances, '#0d6efd', false),
            makeDataset(`Máximo +${formatShort(s3)}`, r3.balances, '#198754', false),
            makeGoalLine(threshold)
        ];

        updateChart(datasets);
        // Cards y tabla usan el escenario "Normal"
        updateSummaryCards(r2, s2, removedIds, threshold);
        updateTable(r2.months, threshold, true);
        updateProgressBar(r2.months);
    }

    // ── Proyección ────────────────────────────────────────────────────────────
    function project(extraIncome, removedIds, threshold) {
        let cumRem = 0;
        const months = planData.months.map((m, i) => {
            const removedThisMonth = planData.fixedExpenses
                .filter(fe => removedIds.includes(fe.id) && fe.activeMonthIndices.includes(i))
                .reduce((s, fe) => s + fe.monthlyAmountARS, 0);
            cumRem += removedThisMonth;

            const cumExtra = extraIncome * (i + 1);
            const balance = m.balanceBase + cumRem + cumExtra;
            const isGoalMet = balance >= threshold;
            const goalDelta = balance - threshold;

            // warning si está a menos del 15% del umbral
            const margin = threshold !== 0 ? Math.abs(goalDelta / Math.abs(threshold)) : Math.abs(goalDelta);
            const status = isGoalMet ? 'safe' : margin < 0.15 ? 'warning' : 'danger';

            return { ...m, balance, cumExtra, cumRem, removedThisMonth, isGoalMet, goalDelta, status };
        });

        const balances = months.map(m => m.balance);
        const breakevenIdx = months.findIndex(m => m.isGoalMet);

        // Ingreso extra mínimo necesario para que TODOS los meses cumplan
        let minExtra = 0;
        let calcCumRem = 0;
        planData.months.forEach((m, i) => {
            const rem = planData.fixedExpenses
                .filter(fe => removedIds.includes(fe.id) && fe.activeMonthIndices.includes(i))
                .reduce((s, fe) => s + fe.monthlyAmountARS, 0);
            calcCumRem += rem;
            const needed = Math.max(0, threshold - m.balanceBase - calcCumRem) / (i + 1);
            if (needed > minExtra) minExtra = needed;
        });

        return { months, balances, breakevenIdx, minExtra };
    }

    // ── Chart helpers ─────────────────────────────────────────────────────────
    function makeDataset(label, data, color, dashed) {
        return {
            label,
            data,
            borderColor: color,
            backgroundColor: color + '15',
            borderWidth: dashed ? 2 : 2.5,
            borderDash: dashed ? [6, 4] : [],
            pointRadius: 3,
            pointHoverRadius: 7,
            tension: 0.35,
            fill: false
        };
    }

    function makeGoalLine(threshold) {
        return {
            label: 'Meta',
            data: planData.months.map(() => threshold),
            borderColor: '#dc3545',
            backgroundColor: 'transparent',
            borderWidth: 2,
            borderDash: [8, 4],
            pointRadius: 0,
            tension: 0,
            fill: false
        };
    }

    function updateChart(datasets) {
        chart.data.datasets = datasets;
        chart.update('active');

        const legend = document.getElementById('dp-chart-legend');
        legend.innerHTML = datasets.map(ds => `
            <div class="d-flex align-items-center gap-1" style="font-size:11px;">
                <div style="width:18px;height:3px;background:${ds.borderColor};border-radius:2px;
                     border-top:${ds.borderDash?.length ? '2px dashed ' + ds.borderColor : 'none'};
                     background:${ds.borderDash?.length ? 'none' : ds.borderColor};"></div>
                <span class="text-body-secondary">${ds.label}</span>
            </div>`).join('');
    }

    // ── Animación de valores ─────────────────────────────────────────────────
    function animateValue(el, from, to, formatFn, duration, finalText) {
        from = from || 0;
        to = to || 0;
        if (from === to) {
            el.textContent = finalText !== undefined ? finalText : formatFn(to);
            return;
        }
        const start = performance.now();
        function step(now) {
            const progress = Math.min((now - start) / duration, 1);
            const eased = 1 - Math.pow(1 - progress, 3);
            const current = from + (to - from) * eased;
            if (progress < 1) {
                el.textContent = formatFn(current);
                requestAnimationFrame(step);
            } else {
                el.textContent = finalText !== undefined ? finalText : formatFn(to);
                pulse(el);
            }
        }
        requestAnimationFrame(step);
    }

    function pulse(el) {
        el.classList.remove('dp-value-pulse');
        void el.offsetWidth; // forzar reflow para reiniciar la animación
        el.classList.add('dp-value-pulse');
    }

    // ── Summary cards ─────────────────────────────────────────────────────────
    function updateSummaryCards(proj, extraIncome, removedIds, threshold) {
        // Saldo actual (animado solo la primera vez que se muestra)
        const bal = planData.currentBalance;
        const balEl = document.getElementById('dp-card-balance');
        balEl.className = 'dp-summary-value ' + (bal >= 0 ? 'text-success' : 'text-danger');
        if (!prevBalanceAnimated) {
            prevBalanceAnimated = true;
            animateValue(balEl, 0, bal, formatARS, 700, planData.currentBalanceFmt);
        } else {
            balEl.textContent = planData.currentBalanceFmt;
        }

        // Breakeven
        const beEl = document.getElementById('dp-card-breakeven');
        const beSubEl = document.getElementById('dp-card-breakeven-sub');
        const prevBeText = beEl.textContent;
        if (proj.breakevenIdx >= 0) {
            const m = planData.months[proj.breakevenIdx];
            beEl.textContent = m.label;
            beEl.className = 'dp-summary-value text-success';
            beSubEl.textContent = `Mes ${proj.breakevenIdx + 1} de 12 del horizonte`;
        } else {
            beEl.textContent = 'No alcanzado';
            beEl.className = 'dp-summary-value text-danger';
            beSubEl.textContent = 'No se cumple en 12 meses';
        }
        if (!firstRender && prevBeText !== beEl.textContent) pulse(beEl);

        // Extra mínimo necesario
        const minExEl = document.getElementById('dp-card-min-extra');
        const roundedMinExtra = proj.minExtra > 0 ? Math.ceil(proj.minExtra / 1000) * 1000 : 0;
        if (proj.minExtra > 0) {
            const from = prevMinExtra ?? roundedMinExtra;
            animateValue(minExEl, from, roundedMinExtra, v => formatARS(v) + '/mes', 450, formatARS(roundedMinExtra) + '/mes');
            minExEl.className = 'dp-summary-value text-warning';
        } else {
            if (!firstRender && prevMinExtra !== 0) pulse(minExEl);
            minExEl.textContent = '¡Ya se cumple!';
            minExEl.className = 'dp-summary-value text-success';
        }
        prevMinExtra = roundedMinExtra;

        // Reducción seleccionada
        const reduction = planData.fixedExpenses
            .filter(fe => removedIds.includes(fe.id))
            .reduce((s, fe) => s + fe.monthlyAmountARS, 0);
        const redEl = document.getElementById('dp-card-reduction');
        const redSubEl = document.getElementById('dp-card-reduction-sub');
        if (reduction > 0) {
            const from = prevReduction ?? reduction;
            animateValue(redEl, from, reduction, v => '-' + formatARS(v) + '/mes', 450, '-' + formatARS(reduction) + '/mes');
            redEl.className = 'dp-summary-value text-info';
            redSubEl.textContent = `${removedIds.length} gasto${removedIds.length > 1 ? 's' : ''} seleccionado${removedIds.length > 1 ? 's' : ''}`;
        } else {
            if (!firstRender && prevReduction) pulse(redEl);
            redEl.textContent = '$ 0/mes';
            redEl.className = 'dp-summary-value text-secondary';
            redSubEl.textContent = 'Ningún gasto seleccionado';
        }
        prevReduction = reduction;

        // Badge de reducción en el panel
        const badge = document.getElementById('dp-reduction-badge');
        const badgeTotal = document.getElementById('dp-reduction-total');
        badge.style.display = reduction > 0 ? '' : 'none';
        badgeTotal.textContent = formatARS(reduction);

        firstRender = false;
    }

    // ── Barra de "Salud del plan" ────────────────────────────────────────────
    function updateProgressBar(months) {
        const segmentsEl = document.getElementById('dp-progress-segments');
        const labelEl = document.getElementById('dp-progress-label');
        if (!segmentsEl || !labelEl) return;

        const metCount = months.filter(m => m.status === 'safe').length;

        segmentsEl.innerHTML = months.map((m, i) => {
            const statusText = m.status === 'safe' ? 'Cumple la meta'
                : m.status === 'warning' ? 'Cerca de la meta'
                : 'En riesgo';
            return `<div class="dp-progress-segment ${m.status}" data-month-index="${i}"
                         title="${m.label}: ${statusText} (${formatARS(m.balance)})"></div>`;
        }).join('');

        labelEl.textContent = `${metCount} / ${months.length} meses cumplen`;

        segmentsEl.querySelectorAll('.dp-progress-segment').forEach(seg => {
            const idx = parseInt(seg.dataset.monthIndex, 10);
            seg.addEventListener('mouseenter', () => highlightMonth(idx, true));
            seg.addEventListener('mouseleave', () => clearHighlight(true));
        });
    }

    // ── Tabla mensual ─────────────────────────────────────────────────────────
    function updateTable(months, threshold, isScenariosNote) {
        const tbody = document.getElementById('dp-table-body');
        tbody.innerHTML = months.map((m, i) => {
            const rowCls = m.status === 'safe' ? 'dp-row-safe'
                         : m.status === 'warning' ? 'dp-row-warning'
                         : 'dp-row-danger';
            const badge = m.status === 'safe'
                ? '<span class="badge bg-success-subtle text-success border border-success-subtle">✓ Cumple</span>'
                : m.status === 'warning'
                ? '<span class="badge bg-warning-subtle text-warning border border-warning-subtle">⚠ Cerca</span>'
                : '<span class="badge bg-danger-subtle text-danger border border-danger-subtle">✗ Riesgo</span>';

            const extraHtml = m.cumExtra > 0
                ? `<span class="text-success fw-semibold">+${formatARS(m.cumExtra)}</span>`
                : '<span class="text-muted">—</span>';

            const deltaVal = m.goalDelta;
            const deltaCls = deltaVal >= 0 ? 'text-success' : 'text-danger';
            const deltaPrefix = deltaVal >= 0 ? '+' : '';

            return `
                <tr class="${rowCls}" data-month-index="${i}">
                    <td class="ps-4 fw-semibold">${m.label}</td>
                    <td class="text-end text-success">${formatARS(m.income)}</td>
                    <td class="text-end text-danger">-${formatARS(m.fixedExpensesTotal)}</td>
                    <td class="text-end">${extraHtml}</td>
                    <td class="text-end fw-bold ${m.balance >= 0 ? 'text-success' : 'text-danger'}">
                        ${formatARS(m.balance)}
                    </td>
                    <td class="text-end ${deltaCls}">${deltaPrefix}${formatARS(deltaVal)}</td>
                    <td class="text-center pe-4">${badge}</td>
                </tr>`;
        }).join('');

        tbody.querySelectorAll('tr').forEach(tr => {
            const idx = parseInt(tr.dataset.monthIndex, 10);
            tr.addEventListener('mouseenter', () => highlightMonth(idx, true));
            tr.addEventListener('mouseleave', () => clearHighlight(true));
        });
    }

    // ── Umbral por tipo de meta ───────────────────────────────────────────────
    function computeThreshold(goalType, value) {
        if (goalType === 'reachZero') return 0;
        if (goalType === 'maxNegative') return -Math.abs(value);
        return Math.abs(value);
    }

    // ── Arrancar ──────────────────────────────────────────────────────────────
    // El script está al final de la página (@section Scripts), por lo que el DOM
    // ya está listo cuando el IIFE se ejecuta. Se llama init() directamente.
    // El listener de turbo:load cubre las navegaciones SPA posteriores.
    let initialized = false;

    function tryInit() {
        if (initialized) return;
        if (!document.getElementById('debt-plan-container')) return;
        initialized = true;
        init();
    }

    // Llamar inmediatamente (DOMContentLoaded ya disparó)
    tryInit();

    // Para navegaciones Turbo posteriores (ej: volver a esta página sin reload)
    document.addEventListener('turbo:load', () => {
        initialized = false; // resetear para nueva navegación Turbo
        tryInit();
    });
})();
