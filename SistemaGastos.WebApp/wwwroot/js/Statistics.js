(function () {
    const $container = $('#statistics-container');
    if ($container.length === 0) return;

    const urls = {
        stats: $container.data('url-stats')
    };

    loadStatistics();

    function loadStatistics() {
        $.get(urls.stats, function (response) {
            if (!response.success || !response.data) {
                console.error("Error cargando estadísticas");
                showError();
                return;
            }

            const data = response.data;
            renderStatistics(data);
        }).fail(function (xhr) {
            console.error("Error:", xhr);
            showError();
        });
    }

    function showError() {
        $('#topExpenses, #accountsDistribution').html('<p class="text-muted text-center py-3">Error al cargar datos</p>');
        $('#recentTransactions tbody').html('<tr><td colspan="2" class="text-center text-muted py-3">Error al cargar datos</td></tr>');
    }

    function renderStatistics(data) {
        // Patrimonio y saldos
        $('#netWorth').text(data.netWorthFmt);
        $('#totalBalance').text(data.totalBalanceFmt);
        $('#accountCount').text(`${data.totalAccounts} cuentas`);
        $('#totalDebt').text(data.totalCreditCardDebtFmt);
        $('#installmentCount').text(`${data.activeInstallments} cuotas activas`);

        // Mes actual
        $('#currentIncome').text(data.currentMonthIncomeFmt);
        $('#currentExpense').text(data.currentMonthExpenseFmt);
        $('#currentSavings').text(data.currentMonthSavingsFmt);
        $('#savingsRate').text(data.savingsRate.toFixed(1));

        // Variaciones
        const incomeVar = data.incomeVariation;
        $('#incomeVariation')
            .removeClass('bg-success bg-danger bg-secondary')
            .addClass(incomeVar > 0 ? 'bg-success' : incomeVar < 0 ? 'bg-danger' : 'bg-secondary')
            .text(`${incomeVar > 0 ? '+' : ''}${incomeVar.toFixed(1)}%`);

        const expenseVar = data.expenseVariation;
        $('#expenseVariation')
            .removeClass('bg-success bg-danger bg-secondary')
            .addClass(expenseVar < 0 ? 'bg-success' : expenseVar > 0 ? 'bg-danger' : 'bg-secondary')
            .text(`${expenseVar > 0 ? '+' : ''}${expenseVar.toFixed(1)}%`);

        // Proyecciones
        $('#avgDailyExpense').text(data.averageDailyExpenseFmt);
        $('#daysLeft').text(data.daysUntilMonthEnd);
        $('#estimatedBalance').text(data.estimatedMonthEndBalanceFmt);
        $('#nextMonthInstallments').text(data.nextMonthInstallmentsFmt);
        $('#projectedBalance').text(data.projectedBalanceNextMonthFmt);

        // Top gastos
        if (data.topExpenseCategories && data.topExpenseCategories.length > 0) {
            let html = '';
            data.topExpenseCategories.forEach(cat => {
                html += `
                    <div class="mb-3">
                        <div class="d-flex justify-content-between align-items-center mb-2">
                            <span class="fw-bold">${cat.categoryName}</span>
                            <span class="text-danger fw-bold">${cat.amountFmt}</span>
                        </div>
                        <div class="progress mb-1" style="height: 8px;">
                            <div class="progress-bar bg-danger" role="progressbar" style="width: ${cat.percentage}%" aria-valuenow="${cat.percentage}" aria-valuemin="0" aria-valuemax="100"></div>
                        </div>
                        <small class="text-muted">${cat.transactionCount} transacciones (${cat.percentage.toFixed(1)}%)</small>
                    </div>
                `;
            });
            $('#topExpenses').html(html);
        } else {
            $('#topExpenses').html('<p class="text-muted text-center py-3">No hay gastos registrados este mes</p>');
        }

        // Distribución de cuentas
        if (data.accounts && data.accounts.length > 0) {
            let html = '';
            data.accounts.forEach(acc => {
                const colorClass = acc.balance >= 0 ? 'success' : 'danger';
                html += `
                    <div class="mb-3">
                        <div class="d-flex justify-content-between align-items-center mb-2">
                            <div>
                                <span class="fw-bold">${acc.accountName}</span>
                                <span class="badge bg-secondary ms-2">${acc.accountType}</span>
                            </div>
                            <span class="text-${colorClass} fw-bold">${acc.balanceFmt}</span>
                        </div>
                        <div class="progress" style="height: 8px;">
                            <div class="progress-bar bg-${colorClass}" role="progressbar" style="width: ${Math.abs(acc.percentage)}%" aria-valuenow="${Math.abs(acc.percentage)}" aria-valuemin="0" aria-valuemax="100"></div>
                        </div>
                    </div>
                `;
            });
            $('#accountsDistribution').html(html);
        } else {
            $('#accountsDistribution').html('<p class="text-muted text-center py-3">No hay cuentas registradas</p>');
        }

        // Últimas transacciones
        if (data.recentTransactions && data.recentTransactions.length > 0) {
            let html = '';
            data.recentTransactions.forEach(t => {
                const colorClass = t.isIncome ? 'success' : 'danger';
                const icon = t.isIncome ? 'arrow-up' : 'arrow-down';
                html += `
                    <tr>
                        <td>
                            <div class="d-flex align-items-center gap-3">
                                <div class="rounded-circle bg-${colorClass} bg-opacity-10 p-2" style="width: 40px; height: 40px; display: flex; align-items: center; justify-content: center;">
                                    <i class="fas fa-${icon} text-${colorClass}"></i>
                                </div>
                                <div>
                                    <div class="fw-bold">${t.description || 'Sin descripción'}</div>
                                    <small class="text-muted">${t.categoryName} • ${t.dateFormatted}</small>
                                </div>
                            </div>
                        </td>
                        <td class="text-end">
                            <div class="fw-bold text-${colorClass}">${t.isIncome ? '+' : '-'} ${t.amountFormatted}</div>
                            <small class="text-muted">${t.accountName}</small>
                        </td>
                    </tr>
                `;
            });
            $('#recentTransactions tbody').html(html);
        } else {
            $('#recentTransactions tbody').html('<tr><td colspan="2" class="text-center text-muted py-3">No hay transacciones registradas</td></tr>');
        }
    }
})();
