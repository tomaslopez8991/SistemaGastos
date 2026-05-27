(function () {
    const $container = $('#category-container');
    if ($container.length === 0) return;

    const urls = {
        list: $container.data('url-list'),
        form: $container.data('url-form'),
        save: $container.data('url-save'),
        delete: $container.data('url-delete')
    };

    let allCategories = []; // Almacén local para filtrar sin recargar

    // Config Toast
    window.Toast = window.Toast || Swal.mixin({
        toast: true,
        position: 'top-end',
        showConfirmButton: false,
        timer: 3000,
        timerProgressBar: true
    });

    // 1. CARGAR DATOS
    function loadCategories() {
        $.get(urls.list, function (res) {
            allCategories = res.data; // Guardamos
            renderGrid(allCategories); // Renderizamos todo por defecto
        });
    }

    // 2. RENDERIZAR (Recibe un array de datos)
    function renderGrid(data) {
        const $grid = $('#categories-grid');
        $grid.empty();

        if (data.length === 0) {
            $grid.html('<div class="col-12 text-center text-muted py-5">No se encontraron categorías.</div>');
            return;
        }

        data.forEach(cat => {
            // Estilos dinámicos basados en el color elegido por el usuario
            // Usamos el color como borde izquierdo y como fondo sutil del ícono
            const html = `
                <div class="col-md-4 col-lg-3">
                    <div class="card bg-body-tertiary border-0 shadow-sm h-100 position-relative overflow-hidden category-card" 
                         style="border-left: 5px solid ${cat.color} !important;">
                        
                        <div class="card-body d-flex align-items-center gap-3">
                            <div class="rounded-circle d-flex align-items-center justify-content-center flex-shrink-0" 
                                 style="width: 50px; height: 50px; background-color: ${cat.color}20; color: ${cat.color}; font-size: 1.2rem;">
                                <i class="${cat.icon}"></i>
                            </div>
                            
                            <div class="flex-grow-1 overflow-hidden">
                                <h6 class="fw-bold mb-1 text-truncate">${cat.name}</h6>
                                <span class="badge bg-dark bg-opacity-25 border border-secondary border-opacity-25" style="font-weight: normal;">
                                    ${cat.type}
                                </span>
                                ${cat.description ? `<small class="d-block text-muted text-truncate mt-1">${cat.description}</small>` : ''}
                            </div>

                            <div class="d-flex flex-column gap-2 opacity-50 hover-actions">
                                <button class="btn btn-sm btn-link text-primary p-0" onclick="abrirModalCategoria(${cat.id})"><i class="fa-solid fa-pen"></i></button>
                                <button class="btn btn-sm btn-link text-danger p-0" onclick="deleteCategory(${cat.id})"><i class="fa-solid fa-trash"></i></button>
                            </div>
                        </div>
                    </div>
                </div>
            `;
            $grid.append(html);
        });
    }

    // 3. FILTRO (Client-Side)
    window.filtrarCategorias = function (type) {
        // Actualizar estado visual de los botones
        $('#pills-tab .nav-link').removeClass('active');
        event.target.classList.add('active');

        if (type === 'all') {
            renderGrid(allCategories);
        } else {
            const filtered = allCategories.filter(c => c.type === type);
            renderGrid(filtered);
        }
    };

    // 4. ABM ACCIONES

    // Crear / Editar
    window.abrirModalCategoria = function (id = null) {
        let url = urls.form;
        if (id) url += '?id=' + id;
        const isEdit = id !== null;

        $.get(url, function (html) {
            Swal.fire({
                title: isEdit ? 'Editar Categoría' : 'Nueva Categoría',
                html: html, width: '500px', showCancelButton: true, confirmButtonText: 'Guardar',
                background: '#212529', color: '#fff',
                preConfirm: () => {
                    const form = document.getElementById('form-category');
                    const formData = new FormData(form);
                    const data = Object.fromEntries(formData.entries());
                    data.ID = parseInt(data.ID) || 0;

                    if (!data.Name) { Swal.showValidationMessage('El nombre es obligatorio'); return false; }
                    return data;
                }
            }).then(res => {
                if (res.isConfirmed) {
                    $.ajax({
                        url: urls.save, type: 'POST', contentType: 'application/json',
                        data: JSON.stringify(res.value),
                        headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
                        success: (r) => {
                            if (r.success) {
                                loadCategories(); // Recargar lista
                                Toast.fire({ icon: 'success', title: 'Categoría guardada' });
                            } else Swal.fire('Error', r.message, 'error');
                        }
                    });
                }
            });
        });
    };

    // Eliminar
    window.deleteCategory = function (id) {
        Swal.fire({
            title: '¿Eliminar?', text: 'Esto podría afectar a transacciones históricas.',
            icon: 'warning', showCancelButton: true, confirmButtonColor: '#d33', confirmButtonText: 'Borrar',
            background: '#212529', color: '#fff'
        }).then(res => {
            if (res.isConfirmed) {
                $.post(urls.delete, { id: id }, function (r) {
                    if (r.success) {
                        loadCategories();
                        Toast.fire({ icon: 'success', title: 'Categoría eliminada' });
                    } else {
                        Swal.fire('Error', r.message, 'error');
                    }
                });
            }
        });
    };

    // Init
    loadCategories();
})();