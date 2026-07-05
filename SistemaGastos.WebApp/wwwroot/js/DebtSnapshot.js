(function () {
    const $container = $('#planning-container');
    if ($container.length === 0) return;

    const urls = {
        debtSnapshot: $container.data('url-debt-snapshot'),
        debtAdvice: $container.data('url-debt-advice')
    };

    const fmtARS = new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS' });
    const fmtUSD = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' });

    let loaded = false;

    $('#deudas-tab').on('shown.bs.tab', function () {
        if (!loaded) {
            loaded = true;
            loadDebtSnapshot();
        }
    });

    $('#btnGenerateDebtAdvice').on('click', () => loadDebtAdvice());
    $('#btnAskDebtAdvice').on('click', askQuestion);
    $('#debtAdviceQuestion').on('keydown', function (e) {
        if (e.key === 'Enter') { e.preventDefault(); askQuestion(); }
    });

    function askQuestion() {
        const question = $('#debtAdviceQuestion').val().trim();
        if (!question) return;
        loadDebtAdvice(question);
    }

    function loadDebtAdvice(question) {
        const $genBtn = $('#btnGenerateDebtAdvice');
        const $askBtn = $('#btnAskDebtAdvice');
        const $content = $('#debt-advice-content');

        $genBtn.prop('disabled', true);
        $askBtn.prop('disabled', true);
        $content.html('<div class="text-center py-3"><div class="spinner-border spinner-border-sm text-primary" role="status"></div><span class="ms-2 small">Consultando a la IA...</span></div>');

        $.get(urls.debtAdvice, question ? { question } : {}, function (response) {
            if (!response.success || !response.data) {
                $content.html(`<div class="alert alert-warning mb-0">${response.message || 'No se pudo generar la recomendación.'}</div>`);
                return;
            }
            const adviceHtml = response.data.advice
                .split('\n')
                .filter(line => line.trim().length > 0)
                .map(line => `<p class="mb-2">${$('<div>').text(line).html()}</p>`)
                .join('');
            $content.html(`<div class="text-body">${adviceHtml}</div>`);
            if (question) $('#debtAdviceQuestion').val('');
        }).fail(function () {
            $content.html('<div class="alert alert-danger mb-0">Error al generar la recomendación.</div>');
        }).always(function () {
            $genBtn.prop('disabled', false);
            $askBtn.prop('disabled', false);
        });
    }

    function fmt(amount, currency) {
        return currency === 'USD' ? fmtUSD.format(amount) : fmtARS.format(amount);
    }

    function loadDebtSnapshot() {
        $.get(urls.debtSnapshot, function (response) {
            if (!response.success || !response.data) {
                $('#debt-snapshot-summary').html('<div class="col-12"><div class="alert alert-danger">Error cargando el snapshot de deudas.</div></div>');
                return;
            }
            render(response.data);
        }).fail(function () {
            $('#debt-snapshot-summary').html('<div class="col-12"><div class="alert alert-danger">No se pudo calcular el snapshot de deudas.</div></div>');
        });
    }

    function render(data) {
        renderSummary(data);
        renderCardDebts(data.cardDebts);
    }

    function renderSummary(data) {
        const surplusClass = data.projectedSurplusNextMonth >= 0 ? 'text-success' : 'text-danger';
        const surplusIcon = data.projectedSurplusNextMonth >= 0 ? 'fa-arrow-trend-up' : 'fa-arrow-trend-down';

        const html = `
            <div class="col-12 col-md-4">
                <div class="card fin-card card-glass-modern border-0 h-100">
                    <div class="gradient-overlay ${data.projectedSurplusNextMonth >= 0 ? 'gradient-success' : 'gradient-danger'}"></div>
                    <div class="card-body p-4 position-relative">
                        <div class="metric-badge ${data.projectedSurplusNextMonth >= 0 ? 'bg-success' : 'bg-danger'} bg-opacity-10 mb-3">
                            <i class="fa-solid ${surplusIcon} ${surplusClass} fa-lg"></i>
                        </div>
                        <p class="text-muted small mb-2 text-uppercase fw-semibold">Excedente próximo mes</p>
                        <h2 class="fw-bold mb-0 fin-num ${surplusClass}" style="font-size: 1.75rem;">
                            ${fmtARS.format(data.projectedSurplusNextMonth)}
                        </h2>
                    </div>
                </div>
            </div>
            <div class="col-12 col-md-4">
                <div class="card fin-card card-glass-modern border-0 h-100">
                    <div class="gradient-overlay gradient-primary"></div>
                    <div class="card-body p-4 position-relative">
                        <div class="metric-badge bg-primary bg-opacity-10 mb-3">
                            <i class="fa-solid fa-arrow-trend-up text-primary fa-lg"></i>
                        </div>
                        <p class="text-muted small mb-2 text-uppercase fw-semibold">Ingreso proyectado próximo mes</p>
                        <h2 class="fw-bold mb-0 fin-num text-primary" style="font-size: 1.75rem;">
                            ${fmtARS.format(data.projectedIncomeNextMonth)}
                        </h2>
                    </div>
                </div>
            </div>
            <div class="col-12 col-md-4">
                <div class="card fin-card card-glass-modern border-0 h-100">
                    <div class="gradient-overlay gradient-warning"></div>
                    <div class="card-body p-4 position-relative">
                        <div class="metric-badge bg-warning bg-opacity-10 mb-3">
                            <i class="fa-solid fa-percent text-warning fa-lg"></i>
                        </div>
                        <p class="text-muted small mb-2 text-uppercase fw-semibold">Interés por descubierto acumulado</p>
                        <h2 class="fw-bold mb-0 fin-num text-warning" style="font-size: 1.75rem;">
                            ${fmtARS.format(data.overdraftInterestAccrued)}
                        </h2>
                    </div>
                </div>
            </div>
            <div class="col-12">
                ${renderPendingFixedExpenses(data.pendingFixedExpenses)}
            </div>
        `;

        $('#debt-snapshot-summary').html(html);
    }

    function renderPendingFixedExpenses(items) {
        if (!items || items.length === 0) {
            return '<p class="text-muted small mb-0"><i class="fa-solid fa-circle-check text-success me-1"></i>No hay gastos fijos pendientes este mes.</p>';
        }

        const rows = items.map(fe => `
            <li class="list-group-item d-flex justify-content-between align-items-center">
                <span><i class="fa-solid fa-repeat text-warning me-2"></i>${fe.name} <span class="text-muted small">(día ${fe.paymentDay})</span></span>
                <span class="fw-semibold">${fmt(fe.amount, fe.currency)}</span>
            </li>
        `).join('');

        return `
            <p class="text-muted small mb-2 text-uppercase fw-semibold">Gastos fijos pendientes este mes</p>
            <ul class="list-group list-group-flush">${rows}</ul>
        `;
    }

    function renderCardDebts(cards) {
        if (!cards || cards.length === 0) {
            $('#debt-snapshot-cards').html('<div class="col-12 text-muted small">No hay deuda activa en tarjetas.</div>');
            return;
        }

        const html = cards.map(c => {
            const days = Math.round((new Date(c.nextDueDate) - new Date()) / 86400000);
            const dueBadge = days <= 3
                ? `<span class="badge text-bg-danger"><i class="fas fa-circle-exclamation me-1"></i>Vence en ${days}d</span>`
                : `<span class="badge text-bg-light text-dark border">Vence en ${days}d</span>`;

            return `
                <div class="col-12 col-md-6 col-lg-4">
                    <div class="card fin-card card-glass-modern border-0 h-100">
                        <div class="gradient-overlay gradient-danger"></div>
                        <div class="card-body p-4 position-relative">
                            <div class="d-flex justify-content-between align-items-start mb-3">
                                <div class="metric-badge bg-danger bg-opacity-10">
                                    <i class="fa-regular fa-credit-card text-danger fa-lg"></i>
                                </div>
                                ${dueBadge}
                            </div>
                            <p class="text-muted small mb-1 text-uppercase fw-semibold">${c.accountName}</p>
                            <h3 class="fw-bold mb-2 fin-num text-danger" style="font-size: 1.5rem;">
                                ${fmt(c.currentBalance, c.currency)}
                            </h3>
                            <p class="text-muted small mb-0">
                                <i class="fa-solid fa-layer-group me-1"></i>
                                ${c.installmentsRemainingCount} compra(s) en cuotas · restan ${fmt(c.remainingInstallmentsTotal, c.currency)}
                            </p>
                        </div>
                    </div>
                </div>
            `;
        }).join('');

        $('#debt-snapshot-cards').html(html);
    }
})();
