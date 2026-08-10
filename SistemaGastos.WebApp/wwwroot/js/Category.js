(function () {
    'use strict';

    const container = document.getElementById('category-container');
    if (!container) return;

    const urls = {
        list: container.dataset.urlList,
        form: container.dataset.urlForm,
        save: container.dataset.urlSave,
        delete: container.dataset.urlDelete
    };
    const grid = document.getElementById('categories-grid');
    const searchInput = document.getElementById('category-search');
    const feedback = document.getElementById('category-feedback');
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? '';
    const iconPattern = /^fa-(solid|regular|brands)(\s+fa-[a-z0-9-]+)+$/;
    const colorPattern = /^#[0-9a-f]{6}$/i;

    let categories = [];
    let activeFilter = 'all';

    const Toast = Swal.mixin({
        toast: true,
        position: 'top-end',
        showConfirmButton: false,
        timer: 3500,
        timerProgressBar: true
    });

    function setLoading() {
        grid.setAttribute('aria-busy', 'true');
        grid.replaceChildren(...Array.from({ length: 6 }, () => {
            const skeleton = document.createElement('div');
            skeleton.className = 'category-skeleton';
            skeleton.setAttribute('aria-hidden', 'true');
            return skeleton;
        }));
    }

    function stateView(icon, title, message, action) {
        const state = document.createElement('div');
        state.className = 'category-state';

        const stateIcon = document.createElement('i');
        stateIcon.className = icon;
        stateIcon.setAttribute('aria-hidden', 'true');
        const heading = document.createElement('strong');
        heading.textContent = title;
        const text = document.createElement('span');
        text.textContent = message;
        state.append(stateIcon, heading, text);

        if (action) {
            const button = document.createElement('button');
            button.type = 'button';
            button.className = 'btn btn-outline-primary mt-1';
            button.textContent = action.label;
            button.addEventListener('click', action.handler);
            state.append(button);
        }
        return state;
    }

    function updateCounts() {
        const expenses = categories.filter(category => category.type === 'Gasto').length;
        const incomes = categories.filter(category => category.type === 'Ingreso').length;
        document.getElementById('category-total').textContent = categories.length;
        document.getElementById('count-all').textContent = categories.length;
        document.getElementById('count-expense').textContent = expenses;
        document.getElementById('count-income').textContent = incomes;
    }

    function createAction(icon, label, className, handler) {
        const button = document.createElement('button');
        button.type = 'button';
        button.className = `category-action ${className ?? ''}`.trim();
        button.title = label;
        button.setAttribute('aria-label', label);
        const buttonIcon = document.createElement('i');
        buttonIcon.className = icon;
        buttonIcon.setAttribute('aria-hidden', 'true');
        button.append(buttonIcon);
        button.addEventListener('click', handler);
        return button;
    }

    function createCard(category) {
        const safeColor = colorPattern.test(category.color) ? category.color : '#0D6EFD';
        const safeIcon = iconPattern.test(category.icon) ? category.icon : 'fa-solid fa-tag';
        const card = document.createElement('article');
        card.className = 'category-card-new';
        card.style.setProperty('--category-color', safeColor);

        const content = document.createElement('div');
        content.className = 'category-card-content';
        const iconWrap = document.createElement('span');
        iconWrap.className = 'category-card-icon';
        iconWrap.setAttribute('aria-hidden', 'true');
        const icon = document.createElement('i');
        icon.className = safeIcon;
        iconWrap.append(icon);

        const details = document.createElement('div');
        details.className = 'overflow-hidden';
        const title = document.createElement('h2');
        title.className = 'category-card-title';
        title.textContent = category.name;
        title.title = category.name;
        const type = document.createElement('span');
        type.className = 'category-type-badge';
        const typeIcon = document.createElement('i');
        typeIcon.className = category.type === 'Ingreso'
            ? 'fa-solid fa-arrow-trend-up'
            : 'fa-solid fa-arrow-trend-down';
        typeIcon.setAttribute('aria-hidden', 'true');
        type.append(typeIcon, document.createTextNode(category.type));
        details.append(title, type);

        const actions = document.createElement('div');
        actions.className = 'category-actions';
        actions.append(
            createAction('fa-solid fa-pen', `Editar ${category.name}`, '', () => openCategory(category.id)),
            createAction('fa-solid fa-trash', `Eliminar ${category.name}`, 'delete', () => deleteCategory(category))
        );
        content.append(iconWrap, details, actions);
        card.append(content);

        if (category.description) {
            const description = document.createElement('p');
            description.className = 'category-card-description';
            description.textContent = category.description;
            description.title = category.description;
            card.append(description);
        }
        return card;
    }

    function render() {
        const query = searchInput.value.trim().toLocaleLowerCase('es');
        const filtered = categories.filter(category => {
            const matchesType = activeFilter === 'all' || category.type === activeFilter;
            const searchable = `${category.name} ${category.description ?? ''}`.toLocaleLowerCase('es');
            return matchesType && searchable.includes(query);
        });

        grid.setAttribute('aria-busy', 'false');
        if (!filtered.length) {
            const hasFilters = activeFilter !== 'all' || query.length > 0;
            grid.replaceChildren(stateView(
                hasFilters ? 'fa-solid fa-filter-circle-xmark' : 'fa-solid fa-tags',
                hasFilters ? 'No hay coincidencias' : 'Todavía no hay categorías',
                hasFilters ? 'Probá con otra búsqueda o cambiá el filtro.' : 'Creá una categoría para organizar tus movimientos.',
                hasFilters
                    ? { label: 'Limpiar filtros', handler: clearFilters }
                    : { label: 'Crear categoría', handler: () => openCategory() }
            ));
        } else {
            grid.replaceChildren(...filtered.map(createCard));
        }
        feedback.textContent = `${filtered.length} categorías visibles.`;
    }

    function clearFilters() {
        searchInput.value = '';
        setFilter('all');
    }

    function setFilter(filter) {
        activeFilter = filter;
        document.querySelectorAll('[data-category-filter]').forEach(button => {
            const isActive = button.dataset.categoryFilter === filter;
            button.classList.toggle('active', isActive);
            button.setAttribute('aria-pressed', String(isActive));
        });
        render();
    }

    async function loadCategories() {
        setLoading();
        try {
            const response = await fetch(urls.list, { headers: { Accept: 'application/json' } });
            if (!response.ok) throw new Error('No se pudieron cargar las categorías.');
            const result = await response.json();
            categories = Array.isArray(result.data) ? result.data : [];
            updateCounts();
            render();
        } catch (error) {
            grid.setAttribute('aria-busy', 'false');
            grid.replaceChildren(stateView(
                'fa-solid fa-triangle-exclamation',
                'No pudimos cargar las categorías',
                'Revisá tu conexión e intentá nuevamente.',
                { label: 'Reintentar', handler: loadCategories }
            ));
        }
    }

    function readError(xhr, fallback) {
        return xhr?.responseJSON?.message || fallback;
    }

    function bindFormPreview() {
        const name = document.getElementById('Name');
        const description = document.getElementById('Description');
        const icon = document.getElementById('icon-input');
        const color = document.getElementById('Color');
        const previewIcon = document.getElementById('icon-preview');
        const previewIconWrap = document.getElementById('category-preview-icon-wrap');
        const previewName = document.getElementById('category-name-preview');
        const previewType = document.getElementById('category-type-preview');
        const descriptionCount = document.getElementById('description-count');
        const colorValue = document.getElementById('color-value');

        const refresh = () => {
            const selectedType = document.querySelector('input[name="Type"]:checked')?.value ?? 'Gasto';
            const safeIcon = iconPattern.test(icon.value.trim()) ? icon.value.trim() : 'fa-solid fa-tag';
            const safeColor = colorPattern.test(color.value) ? color.value : '#0D6EFD';
            previewName.textContent = name.value.trim() || 'Vista previa';
            previewType.textContent = selectedType;
            previewIcon.className = safeIcon;
            previewIconWrap.style.setProperty('--category-color', safeColor);
            colorValue.textContent = safeColor.toUpperCase();
            descriptionCount.textContent = description.value.length;
        };

        [name, description, icon, color].forEach(input => input.addEventListener('input', refresh));
        document.querySelectorAll('input[name="Type"]').forEach(input => input.addEventListener('change', refresh));
        refresh();
        setTimeout(() => name.focus(), 100);
    }

    async function openCategory(id = null) {
        const isEdit = Number.isInteger(id);
        try {
            const response = await fetch(`${urls.form}${isEdit ? `?id=${id}` : ''}`);
            if (!response.ok) throw new Error();
            const html = await response.text();

            const result = await Swal.fire({
                title: isEdit ? 'Editar categoría' : 'Nueva categoría',
                html,
                width: 560,
                showCancelButton: true,
                confirmButtonText: isEdit ? 'Guardar cambios' : 'Crear categoría',
                cancelButtonText: 'Cancelar',
                focusConfirm: false,
                showLoaderOnConfirm: true,
                didOpen: bindFormPreview,
                preConfirm: async () => {
                    const form = document.getElementById('form-category');
                    const name = form.elements.Name.value.trim();
                    const type = form.querySelector('input[name="Type"]:checked')?.value;
                    const icon = form.elements.Icon.value.trim();
                    const color = form.elements.Color.value;

                    form.elements.Name.classList.toggle('is-invalid', !name || name.length > 60);
                    form.elements.Icon.classList.toggle('is-invalid', !iconPattern.test(icon));
                    if (!name || name.length > 60 || !type || !iconPattern.test(icon) || !colorPattern.test(color)) {
                        Swal.showValidationMessage('Revisá los campos marcados antes de guardar.');
                        return false;
                    }

                    const payload = {
                        ID: Number.parseInt(form.elements.ID.value, 10) || 0,
                        Name: name,
                        Type: type,
                        Description: form.elements.Description.value.trim() || null,
                        Icon: icon,
                        Color: color
                    };

                    try {
                        return await $.ajax({
                            url: urls.save,
                            method: 'POST',
                            contentType: 'application/json',
                            data: JSON.stringify(payload),
                            headers: { RequestVerificationToken: token }
                        });
                    } catch (xhr) {
                        Swal.showValidationMessage(readError(xhr, 'No pudimos guardar la categoría. Intentá nuevamente.'));
                        return false;
                    }
                },
                allowOutsideClick: () => !Swal.isLoading()
            });

            if (result.isConfirmed) {
                await loadCategories();
                Toast.fire({ icon: 'success', title: isEdit ? 'Categoría actualizada' : 'Categoría creada' });
            }
        } catch {
            Swal.fire('No pudimos abrir el formulario', 'Intentá nuevamente.', 'error');
        }
    }

    async function deleteCategory(category) {
        const confirmation = await Swal.fire({
            title: `Eliminar ${category.name}`,
            text: 'Esta acción no se puede deshacer. Si la categoría está en uso, conservaremos su historial.',
            icon: 'warning',
            showCancelButton: true,
            confirmButtonText: 'Eliminar',
            cancelButtonText: 'Cancelar',
            confirmButtonColor: '#dc3545',
            focusCancel: true,
            showLoaderOnConfirm: true,
            preConfirm: async () => {
                try {
                    return await $.ajax({
                        url: urls.delete,
                        method: 'POST',
                        data: { id: category.id },
                        headers: { RequestVerificationToken: token }
                    });
                } catch (xhr) {
                    Swal.showValidationMessage(readError(xhr, 'No pudimos eliminar la categoría. Intentá nuevamente.'));
                    return false;
                }
            },
            allowOutsideClick: () => !Swal.isLoading()
        });

        if (confirmation.isConfirmed) {
            await loadCategories();
            Toast.fire({ icon: 'success', title: 'Categoría eliminada' });
        }
    }

    document.getElementById('new-category-button').addEventListener('click', () => openCategory());
    document.querySelectorAll('[data-category-filter]').forEach(button => {
        button.addEventListener('click', () => setFilter(button.dataset.categoryFilter));
    });
    searchInput.addEventListener('input', render);

    loadCategories();
})();
