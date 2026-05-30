(function () {
    'use strict';

    const $container = $('#statistics-container');
    if ($container.length === 0) return;

    const urls = { stats: $container.data('url-stats') };

    loadStatistics();

    function loadStatistics() {
        $.get(urls.stats, function (response) {
            if (!response.success || !response.data) {
                showError();
                return;
            }
            renderStatistics(response.data);
        }).fail(showError);
    }

    function showError() {
        const msg = '<p class="text-muted text-center py-3">Error al cargar datos</p>';
        $('#topExpenses, #topIncomes, #accountsDistribution').html(msg);
        $('#recentTransactions tbody').html('<tr><td colspan="3" class="text-center text-muted py-3">Error al cargar datos</td></tr>');
    }

    // ── Helpers ──────────────────────────────────────────────

    function getThemeColors() {
        const isDark = document.documentElement.getAttribute('data-bs-theme') === 'dark';
        return {
            text: isDark ? '#adb5bd' : '#495057',
            grid: isDark ? 'rgba(255,255,255,0.07)' : 'rgba(0,0,0,0.07)'
        };
    }

    function formatCurrencyShort(val) {
        const abs = Math.abs(val);
        if (abs >= 1000000) return (val < 0 ? '-' : '') + '$' + (abs / 1000000).toFixed(1) + 'M';
        if (abs >= 1000) return (val < 0 ? '-' : '') + '$' + (abs / 1000).toFixed(0) + 'K';
        return (val < 0 ? '-' : '') + '$' + abs.toFixed(0);
    }

    function capitalizeFirst(str) {
        if (!str) return str;
        return str.charAt(0).toUpperCase() + str.slice(1);
    }

    function getExistingChart(id) {
        return Chart.getChart(id) || null;
    }

    // ── Render principal ─────────────────────────────────────

    function renderStatistics(data) {
        // Timestamp de actualización
        const now = new Date();
        $('#lastUpdated').text('Actualizado: ' + now.toLocaleTimeString('es-AR'));

        renderKPIs(data);
        renderMesActual(data);
        renderTrendChart(data.monthlyTrend);
        renderTopExpenses(data.topExpenseCategories);
        renderTopIncomes(data.topIncomeCategories);
        renderAccounts(data.accounts);
        renderProjections(data);
        renderRecentTransactions(data.recentTransactions);
    }

    // ── KPIs ─────────────────────────────────────────────────

    function renderKPIs(data) {
        // Patrimonio Neto — rojo si negativo
        $('#netWorth')
            .text(data.netWorthFmt)
            .removeClass('text-primary text-danger')
            .addClass(data.netWorth >= 0 ? 'text-primary' : 'text-danger');

        $('#totalBalance').text(data.totalBalanceFmt);
        $('#accountCount').text(data.totalAccounts + ' cuentas');
        $('#totalDebt').text(data.totalCreditCardDebtFmt);
        $('#installmentCount').text(data.activeInstallments + ' cuotas activas');
    }

    // ── Mes actual ───────────────────────────────────────────

    function renderMesActual(data) {
        $('#currentIncome').text(data.currentMonthIncomeFmt);
        $('#currentExpense').text(data.currentMonthExpenseFmt);
        $('#currentSavings').text(data.currentMonthSavingsFmt);
        $('#lastMonthIncome').text(data.lastMonthIncomeFmt);
        $('#lastMonthExpense').text(data.lastMonthExpenseFmt);

        renderVariationBadge('#incomeVariation', data.incomeVariation, false);
        renderVariationBadge('#expenseVariation', data.expenseVariation, true);

        // Savings rate progress bar (0-100%)
        const rate = Math.max(0, Math.min(100, data.savingsRate));
        $('#savingsRateText').text(data.savingsRate.toFixed(1) + '%');
        $('#savingsRateBar').css('width', rate + '%').attr('aria-valuenow', rate);

        $('#avgDailyExpense').text(data.averageDailyExpenseFmt);
        $('#daysLeft').text(data.daysUntilMonthEnd);
    }

    function renderVariationBadge(selector, value, invertColor) {
        // invertColor=true: para gastos, subir es malo (rojo)
        let cssClass;
        if (value === 0) {
            cssClass = 'bg-secondary';
        } else if (invertColor) {
            cssClass = value > 0 ? 'bg-danger' : 'bg-success';
        } else {
            cssClass = value > 0 ? 'bg-success' : 'bg-danger';
        }
        const label = (value > 0 ? '+' : '') + value.toFixed(1) + '%';
        $(selector).removeClass('bg-success bg-danger bg-secondary').addClass(cssClass).text(label);
    }

    // ── Gráfico de tendencia 6 meses ─────────────────────────

    function renderTrendChart(trend) {
        const ctx = document.getElementById('trendChart');
        if (!ctx || !trend || trend.length === 0) return;

        const existing = getExistingChart('trendChart');
        if (existing) existing.destroy();

        const colors = getThemeColors();

        new Chart(ctx, {
            type: 'bar',
            data: {
                labels: trend.map(m => capitalizeFirst(m.month)),
                datasets: [
                    {
                        label: 'Ingresos',
                        data: trend.map(m => Number(m.income)),
                        backgroundColor: 'rgba(25, 135, 84, 0.75)',
                        borderColor: 'rgba(25, 135, 84, 1)',
                        borderWidth: 1,
                        borderRadius: 4,
                        borderSkipped: false
                    },
                    {
                        label: 'Gastos',
                        data: trend.map(m => Number(m.expense)),
                        backgroundColor: 'rgba(220, 53, 69, 0.75)',
                        borderColor: 'rgba(220, 53, 69, 1)',
                        borderWidth: 1,
                        borderRadius: 4,
                        borderSkipped: false
                    }
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { display: false },
                    tooltip: {
                        callbacks: {
                            label: ctx => '  ' + ctx.dataset.label + ': ' + formatCurrencyShort(ctx.raw)
                        }
                    }
                },
                scales: {
                    x: {
                        ticks: { color: colors.text },
                        grid: { color: colors.grid }
                    },
                    y: {
                        ticks: {
                            color: colors.text,
                            callback: val => formatCurrencyShort(val)
                        },
                        grid: { color: colors.grid }
                    }
                }
            }
        });
    }

    // ── Top Gastos (lista + donut) ────────────────────────────

    const PIE_COLORS = ['#e74c3c', '#e67e22', '#f39c12', '#9b59b6', '#3498db'];

    function renderTopExpenses(categories) {
        if (!categories || categories.length === 0) {
            $('#topExpenses').html('<p class="text-muted text-center py-3 small">No hay gastos registrados este mes</p>');
            return;
        }

        let html = '';
        categories.forEach((cat, i) => {
            const color = PIE_COLORS[i % PIE_COLORS.length];
            html += `
                <div class="mb-3">
                    <div class="d-flex justify-content-between align-items-center mb-1">
                        <span class="fw-semibold small text-truncate" style="max-width:130px" title="${cat.categoryName}">${cat.categoryName}</span>
                        <span class="fw-bold small text-danger ms-1">${cat.amountFmt}</span>
                    </div>
                    <div class="progress mb-1" style="height:5px;">
                        <div class="progress-bar" role="progressbar"
                             style="width:${cat.percentage}%;background-color:${color}"></div>
                    </div>
                    <small class="text-muted">${cat.transactionCount} tx &middot; ${cat.percentage.toFixed(1)}%</small>
                </div>`;
        });
        $('#topExpenses').html(html);

        // Donut chart
        const ctx = document.getElementById('expensePieChart');
        if (!ctx) return;
        const existing = getExistingChart('expensePieChart');
        if (existing) existing.destroy();

        new Chart(ctx, {
            type: 'doughnut',
            data: {
                labels: categories.map(c => c.categoryName),
                datasets: [{
                    data: categories.map(c => Number(c.percentage)),
                    backgroundColor: PIE_COLORS,
                    borderWidth: 0,
                    hoverOffset: 6
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: true,
                cutout: '68%',
                plugins: {
                    legend: { display: false },
                    tooltip: {
                        callbacks: {
                            label: ctx => '  ' + ctx.label + ': ' + ctx.parsed.toFixed(1) + '%'
                        }
                    }
                }
            }
        });
    }

    // ── Top Ingresos ──────────────────────────────────────────

    const INCOME_COLORS = ['#27ae60', '#2ecc71', '#1abc9c', '#16a085', '#2980b9'];

    function renderTopIncomes(categories) {
        if (!categories || categories.length === 0) {
            $('#topIncomes').html('<p class="text-muted text-center py-3 small">No hay ingresos registrados este mes</p>');
            return;
        }

        let html = '';
        categories.forEach((cat, i) => {
            const color = INCOME_COLORS[i % INCOME_COLORS.length];
            html += `
                <div class="mb-3">
                    <div class="d-flex justify-content-between align-items-center mb-1">
                        <span class="fw-semibold small text-truncate" style="max-width:180px" title="${cat.categoryName}">${cat.categoryName}</span>
                        <span class="fw-bold small text-success ms-1">${cat.amountFmt}</span>
                    </div>
                    <div class="progress mb-1" style="height:5px;">
                        <div class="progress-bar" role="progressbar"
                             style="width:${cat.percentage}%;background-color:${color}"></div>
                    </div>
                    <small class="text-muted">${cat.transactionCount} tx &middot; ${cat.percentage.toFixed(1)}%</small>
                </div>`;
        });
        $('#topIncomes').html(html);
    }

    // ── Distribución de Cuentas ──────────────────────────────

    function renderAccounts(accounts) {
        if (!accounts || accounts.length === 0) {
            $('#accountsDistribution').html('<p class="text-muted text-center py-3 small">No hay cuentas registradas</p>');
            return;
        }

        let html = '';
        accounts.forEach(acc => {
            const colorClass = acc.balance >= 0 ? 'success' : 'danger';
            html += `
                <div class="mb-3">
                    <div class="d-flex justify-content-between align-items-center mb-1">
                        <div class="d-flex align-items-center gap-2 overflow-hidden">
                            <span class="fw-semibold small text-truncate">${acc.accountName}</span>
                            <span class="badge bg-secondary flex-shrink-0" style="font-size:.6rem">${acc.accountType}</span>
                        </div>
                        <span class="text-${colorClass} fw-bold small ms-2 flex-shrink-0">${acc.balanceFmt}</span>
                    </div>
                    <div class="progress" style="height:5px;">
                        <div class="progress-bar bg-${colorClass}" role="progressbar"
                             style="width:${Math.abs(acc.percentage)}%"></div>
                    </div>
                </div>`;
        });
        $('#accountsDistribution').html(html);
    }

    // ── Proyecciones ─────────────────────────────────────────

    function renderProjections(data) {
        $('#estimatedBalance')
            .text(data.estimatedMonthEndBalanceFmt)
            .toggleClass('text-danger', data.estimatedMonthEndBalance < 0);

        $('#nextMonthInstallments').text(data.nextMonthInstallmentsFmt);

        $('#projectedBalance')
            .text(data.projectedBalanceNextMonthFmt)
            .removeClass('text-primary text-danger')
            .addClass(data.projectedBalanceNextMonth >= 0 ? 'text-primary' : 'text-danger');
    }

    // ── Últimas Transacciones ────────────────────────────────

    function renderRecentTransactions(transactions) {
        if (!transactions || transactions.length === 0) {
            $('#recentTransactions tbody').html(
                '<tr><td colspan="3" class="text-center text-muted py-3 small">No hay transacciones registradas</td></tr>'
            );
            return;
        }

        let html = '';
        transactions.forEach(t => {
            const colorClass = t.isIncome ? 'success' : 'danger';
            const icon = t.isIncome ? 'arrow-up' : 'arrow-down';
            html += `
                <tr>
                    <td style="width:44px;vertical-align:middle;">
                        <div class="rounded-circle bg-${colorClass} bg-opacity-10 d-flex align-items-center justify-content-center"
                             style="width:36px;height:36px;">
                            <i class="fas fa-${icon} text-${colorClass} small"></i>
                        </div>
                    </td>
                    <td style="vertical-align:middle;">
                        <div class="fw-semibold small">${t.description || 'Sin descripción'}</div>
                        <small class="text-muted">${t.categoryName} &middot; ${t.dateFormatted}</small>
                    </td>
                    <td class="text-end" style="vertical-align:middle;">
                        <div class="fw-bold small text-${colorClass}">${t.isIncome ? '+' : '-'}${t.amountFormatted}</div>
                        <small class="text-muted">${t.accountName}</small>
                    </td>
                </tr>`;
        });
        $('#recentTransactions tbody').html(html);
    }

})();
