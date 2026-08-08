const AI = {
    token: document.querySelector('input[name="__RequestVerificationToken"]')?.value,

    headers() {
        return { 'Content-Type': 'application/json', 'RequestVerificationToken': this.token };
    },

    reload() { window.location.reload(); }
};

function selectInterestAccount(accountId) {
    const id = String(accountId);
    document.querySelectorAll('[data-interest-account]').forEach(item => {
        item.classList.toggle('active', item.dataset.interestAccount === id);
    });
    document.querySelectorAll('[data-interest-panel]').forEach(panel => {
        panel.classList.toggle('d-none', panel.dataset.interestPanel !== id);
    });

    const mobileSelect = document.getElementById('interestAccountSelect');
    if (mobileSelect) mobileSelect.value = id;
}

function syncTaxInputs() {
    const vat = document.getElementById('vatRate');
    const stamp = document.getElementById('stampTaxAnnualRate');
    if (vat) vat.disabled = !document.getElementById('applyVat').checked;
    if (stamp) stamp.disabled = !document.getElementById('applyStampTax').checked;
}

document.addEventListener('DOMContentLoaded', () => {
    const firstAccount = document.querySelector('[data-interest-account]')?.dataset.interestAccount
        ?? document.querySelector('[data-interest-panel]')?.dataset.interestPanel;
    if (firstAccount) selectInterestAccount(firstAccount);

    document.getElementById('applyVat')?.addEventListener('change', syncTaxInputs);
    document.getElementById('applyStampTax')?.addEventListener('change', syncTaxInputs);
    syncTaxInputs();
});

function editSetting(id, accountId, rate, enabled, applyVat, vatRate, applyStampTax, stampTaxAnnualRate) {
    document.getElementById('settingID').value = id;
    document.getElementById('accountID').value = accountId;
    document.getElementById('interestRate').value = rate;
    document.getElementById('settingEnabled').checked = enabled;
    document.getElementById('applyVat').checked = applyVat;
    document.getElementById('vatRate').value = (vatRate * 100).toFixed(2);
    document.getElementById('applyStampTax').checked = applyStampTax;
    document.getElementById('stampTaxAnnualRate').value = (stampTaxAnnualRate * 100).toFixed(2);
    document.getElementById('modalSettingTitle').textContent = 'Editar configuración';
    document.getElementById('accountSelectWrapper').style.display = 'none';
    syncTaxInputs();
    new bootstrap.Modal(document.getElementById('modalAddSetting')).show();
}

async function saveSetting() {
    const id = document.getElementById('settingID').value;
    const payload = {
        settingID: id ? parseInt(id, 10) : null,
        accountID: parseInt(document.getElementById('accountID').value, 10),
        interestRate: parseFloat(document.getElementById('interestRate').value),
        applyVat: document.getElementById('applyVat').checked,
        vatRate: (parseFloat(document.getElementById('vatRate').value) || 0) / 100,
        applyStampTax: document.getElementById('applyStampTax').checked,
        stampTaxAnnualRate: (parseFloat(document.getElementById('stampTaxAnnualRate').value) || 0) / 100,
        enabled: document.getElementById('settingEnabled').checked,
        userID: 0
    };

    const res = await fetch('/AccountInterest/SaveSetting', {
        method: 'POST',
        headers: AI.headers(),
        body: JSON.stringify(payload)
    });

    if (res.ok) {
        await Swal.fire({ icon: 'success', title: 'Guardado', timer: 1200, showConfirmButton: false });
        AI.reload();
    } else {
        const err = await res.json().catch(() => ({ message: 'Error al guardar' }));
        Swal.fire({ icon: 'error', title: 'Error', text: err.message });
    }
}

async function toggleSetting(id) {
    const res = await fetch(`/AccountInterest/Toggle?id=${id}`, {
        method: 'POST',
        headers: { 'RequestVerificationToken': AI.token }
    });
    if (res.ok) AI.reload();
}

async function deleteSetting(id, name) {
    const confirm = await Swal.fire({
        icon: 'warning',
        title: `¿Eliminar configuración de "${name}"?`,
        text: 'Se eliminarán también todos los logs diarios asociados.',
        showCancelButton: true,
        confirmButtonText: 'Eliminar',
        cancelButtonText: 'Cancelar',
        confirmButtonColor: '#dc3545'
    });

    if (!confirm.isConfirmed) return;

    const res = await fetch(`/AccountInterest/DeleteSetting?id=${id}`, {
        method: 'DELETE',
        headers: { 'RequestVerificationToken': AI.token }
    });

    if (res.ok) {
        await Swal.fire({ icon: 'success', title: 'Eliminado', timer: 1200, showConfirmButton: false });
        AI.reload();
    }
}

async function recalculate() {
    const confirm = await Swal.fire({
        icon: 'question',
        title: '¿Recalcular intereses?',
        text: 'Reconstruye todo el historial diario en base a las transacciones reales.',
        showCancelButton: true,
        confirmButtonText: 'Recalcular',
        cancelButtonText: 'Cancelar'
    });

    if (!confirm.isConfirmed) return;

    Swal.fire({ title: 'Recalculando...', allowOutsideClick: false, didOpen: () => Swal.showLoading() });

    const res = await fetch('/AccountInterest/Recalculate', {
        method: 'POST',
        headers: { 'RequestVerificationToken': AI.token }
    });

    if (res.ok) {
        await Swal.fire({ icon: 'success', title: 'Listo', timer: 1500, showConfirmButton: false });
        AI.reload();
    } else {
        Swal.fire({ icon: 'error', title: 'Error al recalcular' });
    }
}
