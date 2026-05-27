(function () {
    const $container = $('#todo-container');
    if ($container.length === 0) return;

    const urls = {
        list: $container.data('url-list'),
        form: $container.data('url-form'),
        save: $container.data('url-save'),
        toggle: $container.data('url-toggle'),
        delete: $container.data('url-delete')
    };

    // --- 1. CONFIGURACIÓN DE NOTIFICACIÓN (TOAST) ---
    // Definimos el estilo una sola vez para reusarlo
    window.Toast = window.Toast || Swal.mixin({
        toast: true,
        position: 'top-end',
        showConfirmButton: false,
        timer: 3000,
        timerProgressBar: true
    });

    function loadTasks() {
        $.get(urls.list, function (response) {
            const $pending = $('#pending-list');
            const $completed = $('#completed-list');

            $pending.empty();
            $completed.empty();

            let pendingCount = 0;
            let completedCount = 0;

            response.data.forEach(t => {
                const isOverdue = t.isOverdue;
                const dateFmt = moment(t.dueDate).format('DD/MM/YYYY');
                const overdueClass = isOverdue ? 'overdue' : '';
                const completedClass = t.isCompleted ? 'completed' : '';
                const dateColor = isOverdue ? 'text-danger fw-bold' : 'text-muted';
                const dateIcon = isOverdue ? '<i class="fa-solid fa-circle-exclamation me-1"></i>' : '<i class="fa-regular fa-calendar me-1"></i>';

                const html = `
                    <div class="task-card ${overdueClass} ${completedClass}" id="task-${t.id}">
                        
                        <div class="btn-check-task" onclick="toggleTask(${t.id})" title="${t.isCompleted ? 'Marcar pendiente' : 'Completar'}">
                            <i class="fa-solid fa-check small"></i>
                        </div>

                        <div class="flex-grow-1 cursor-pointer" onclick="abrirModalTarea(${t.id})">
                            <div class="d-flex justify-content-between align-items-start">
                                <h6 class="fw-bold mb-1 task-title">${t.title}</h6>
                                
                                <button class="btn btn-link text-danger p-0 ms-2" onclick="event.stopPropagation(); deleteTask(${t.id})">
                                    <i class="fa-solid fa-times"></i>
                                </button>
                            </div>
                            <p class="small mb-2" style="line-height: 1.4;">${t.description || 'Sin detalles'}</p>
                            <div class="small ${dateColor}">
                                ${dateIcon} ${dateFmt} ${isOverdue ? '(Vencida)' : ''}
                            </div>
                        </div>
                    </div>
                `;

                if (t.isCompleted) {
                    $completed.append(html);
                    completedCount++;
                } else {
                    $pending.append(html);
                    pendingCount++;
                }
            });

            $('#count-pending').text(pendingCount);
            $('#count-completed').text(completedCount);
        });
    }

    // --- ACCIONES ---

    // 1. TOGGLE (Completar)
    window.toggleTask = function (id) {
        const $card = $(`#task-${id}`);
        const isCompletedNow = $card.hasClass('completed');

        // Animación visual inmediata
        $card.animate({ opacity: 0.5 }, 200);

        $.ajax({
            url: urls.toggle,
            type: 'POST',
            data: { id: id },
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            success: function (res) {
                if (res.success) {
                    loadTasks();
                    // TOAST: Mensaje dinámico
                    Toast.fire({
                        icon: 'success',
                        title: isCompletedNow ? 'Tarea reactivada' : '¡Tarea completada!'
                    });
                } else {
                    $card.css('opacity', 1);
                    Swal.fire('Error', 'No se pudo actualizar', 'error');
                }
            },
            error: function () {
                $card.css('opacity', 1);
                Swal.fire('Error', 'Error de conexión', 'error');
            }
        });
    };

    // 2. ELIMINAR
    window.deleteTask = function (id) {
        Swal.fire({
            title: '¿Borrar tarea?',
            text: 'No podrás deshacer esta acción.',
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#d33',
            confirmButtonText: 'Sí, borrar',
            cancelButtonText: 'Cancelar'
        }).then(res => {
            if (res.isConfirmed) {
                $.ajax({
                    url: urls.delete,
                    type: 'POST',
                    data: { id: id },
                    headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
                    success: function (r) {
                        if (r.success) {
                            loadTasks();
                            // TOAST: Confirmación de borrado
                            Toast.fire({ icon: 'success', title: 'Tarea eliminada' });
                        } else {
                            Swal.fire('Error', 'No se pudo eliminar', 'error');
                        }
                    }
                });
            }
        });
    };

    // 3. CREAR / EDITAR
    window.abrirModalTarea = function (id = null) {
        let url = urls.form;
        if (id) url += '?id=' + id;
        const isEdit = id !== null;

        $.get(url, function (html) {
            Swal.fire({
                title: isEdit ? 'Editar Tarea' : 'Nueva Tarea',
                html: html,
                width: '500px',
                showCancelButton: true,
                confirmButtonText: isEdit ? 'Guardar Cambios' : 'Crear Tarea',
                cancelButtonText: 'Cancelar',
                preConfirm: () => {
                    const form = document.getElementById('form-todo');
                    const formData = new FormData(form);
                    const data = Object.fromEntries(formData.entries());

                    data.TaskId = parseInt(data.TaskId) || 0;

                    if (!data.Title || !data.DueDate) {
                        Swal.showValidationMessage('El título y la fecha son obligatorios');
                        return false;
                    }
                    return data;
                }
            }).then(res => {
                if (res.isConfirmed) {
                    $.ajax({
                        url: urls.save,
                        type: 'POST',
                        contentType: 'application/json',
                        data: JSON.stringify(res.value),
                        headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
                        success: (r) => {
                            if (r.success) {
                                loadTasks();
                                // TOAST: Mensaje diferente según acción
                                Toast.fire({
                                    icon: 'success',
                                    title: isEdit ? 'Tarea actualizada' : 'Tarea creada con éxito'
                                });
                            }
                            else Swal.fire('Error', r.message, 'error');
                        }
                    });
                }
            });
        });
    };

    // Iniciar carga
    loadTasks();
})();