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

    window.Toast = window.Toast || Swal.mixin({
        toast: true,
        position: 'top-end',
        showConfirmButton: false,
        timer: 3000,
        timerProgressBar: true
    });

    const PRIORITY = {
        1: { label: 'Alta',  color: '#dc3545', icon: 'fa-arrow-up',   outline: 'danger'  },
        2: { label: 'Media', color: '#ffc107', icon: 'fa-minus',      outline: 'warning' },
        3: { label: 'Baja',  color: '#198754', icon: 'fa-arrow-down', outline: 'success' }
    };

    let allTasks = [];
    let currentFilter = 'all';

    // --- CARGA DE TAREAS ---
    function loadTasks() {
        $.get(urls.list, function (response) {
            allTasks = response.data;
            renderTasks();
        });
    }

    function renderTasks() {
        const $pending   = $('#pending-list');
        const $completed = $('#completed-list');

        $pending.empty();
        $completed.empty();

        let pendingCount   = 0;
        let completedCount = 0;

        allTasks.forEach(t => {
            // Aplicar filtro sólo a pendientes
            if (!t.isCompleted && currentFilter !== 'all' && String(t.priority) !== currentFilter) return;

            const html = buildTaskCard(t);

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

        // Estados vacíos
        if ($pending.children().length === 0) {
            const msg = currentFilter !== 'all'
                ? 'Sin tareas de esta prioridad'
                : '¡Todo al día! Sin pendientes.';
            $pending.html(`<div class="empty-state">
                <i class="fa-solid fa-circle-check fa-3x d-block mb-2"></i>
                <p class="small mb-0">${msg}</p>
            </div>`);
        }

        if ($completed.children().length === 0) {
            $completed.html(`<div class="empty-state">
                <i class="fa-solid fa-list-check fa-3x d-block mb-2"></i>
                <p class="small mb-0">Aún no completaste ninguna tarea</p>
            </div>`);
        }
    }

    function buildTaskCard(t) {
        const prio      = PRIORITY[t.priority] || PRIORITY[2];
        const today     = moment().startOf('day');
        const dueDate   = moment(t.dueDate);
        const diffDays  = dueDate.diff(today, 'days');

        let dateLabel, dateColorClass, dateIconClass;

        if (t.isOverdue) {
            dateLabel      = `Venció el ${dueDate.format('DD/MM/YYYY')}`;
            dateColorClass = 'text-danger fw-semibold';
            dateIconClass  = 'fa-circle-exclamation';
        } else if (diffDays === 0) {
            dateLabel      = 'Vence hoy';
            dateColorClass = 'text-warning fw-semibold';
            dateIconClass  = 'fa-triangle-exclamation';
        } else if (diffDays === 1) {
            dateLabel      = 'Vence mañana';
            dateColorClass = 'text-warning';
            dateIconClass  = 'fa-clock';
        } else {
            dateLabel      = dueDate.format('DD/MM/YYYY');
            dateColorClass = 'text-muted';
            dateIconClass  = 'fa-calendar';
        }

        const overdueClass   = t.isOverdue    ? 'overdue'   : '';
        const completedClass = t.isCompleted  ? 'completed' : '';

        const reminderHtml = (t.reminderDate && !t.isCompleted)
            ? `<i class="fa-regular fa-bell ms-1 text-info" title="Recordatorio activo"></i>`
            : '';

        const desc = t.description
            ? `<p class="small mb-2 task-description text-muted" style="line-height:1.4;">${t.description}</p>`
            : '';

        const checkTitle = t.isCompleted ? 'Marcar como pendiente' : 'Marcar como completada';

        return `
            <div class="task-card ${overdueClass} ${completedClass}" id="task-${t.id}" data-priority="${t.priority}">

                <div class="task-priority-bar" style="background-color:${prio.color};"></div>

                <div class="btn-check-task ms-1" onclick="toggleTask(${t.id})" title="${checkTitle}">
                    <i class="fa-solid fa-check small"></i>
                </div>

                <div class="flex-grow-1 min-w-0" style="cursor:pointer;" onclick="abrirModalTarea(${t.id})">
                    <div class="d-flex justify-content-between align-items-start gap-2">
                        <div class="flex-grow-1 min-w-0">
                            <h6 class="fw-bold mb-1 task-title d-flex align-items-center gap-1 flex-wrap">
                                <span class="text-truncate">${t.title}</span>
                                ${reminderHtml}
                            </h6>
                            <span class="priority-badge border"
                                  style="background-color:${prio.color}18; color:${prio.color}; border-color:${prio.color}50 !important;">
                                <i class="fa-solid ${prio.icon} me-1"></i>${prio.label}
                            </span>
                        </div>
                        <button class="btn btn-link text-danger p-0 flex-shrink-0"
                                onclick="event.stopPropagation(); deleteTask(${t.id})"
                                title="Eliminar tarea">
                            <i class="fa-solid fa-times"></i>
                        </button>
                    </div>

                    ${desc}

                    <div class="small ${dateColorClass} mt-1">
                        <i class="fa-solid ${dateIconClass} me-1"></i>${dateLabel}
                    </div>
                </div>
            </div>
        `;
    }

    // --- FILTROS ---
    $(document).on('click', '[data-filter]', function () {
        currentFilter = String($(this).data('filter'));
        $('[data-filter]').removeClass('active');
        $(this).addClass('active');
        renderTasks();
    });

    // --- TOGGLE (Completar/Reactivar) ---
    window.toggleTask = function (id) {
        const $card = $(`#task-${id}`);
        const isCompletedNow = $card.hasClass('completed');

        $card.animate({ opacity: 0.4 }, 150);

        $.ajax({
            url: urls.toggle,
            type: 'POST',
            data: { id },
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            success: function (res) {
                if (res.success) {
                    loadTasks();
                    Toast.fire({
                        icon: 'success',
                        title: isCompletedNow ? 'Tarea reactivada' : '¡Tarea completada!'
                    });
                } else {
                    $card.css('opacity', 1);
                    Swal.fire('Error', 'No se pudo actualizar la tarea', 'error');
                }
            },
            error: function () {
                $card.css('opacity', 1);
                Swal.fire('Error', 'Error de conexión', 'error');
            }
        });
    };

    // --- ELIMINAR ---
    window.deleteTask = function (id) {
        Swal.fire({
            title: '¿Borrar tarea?',
            text: 'Esta acción no se puede deshacer.',
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#d33',
            confirmButtonText: 'Sí, borrar',
            cancelButtonText: 'Cancelar'
        }).then(res => {
            if (!res.isConfirmed) return;

            $.ajax({
                url: urls.delete,
                type: 'POST',
                data: { id },
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
                success: function (r) {
                    if (r.success) {
                        $(`#task-${id}`).fadeOut(200, function () {
                            allTasks = allTasks.filter(t => t.id !== id);
                            renderTasks();
                        });
                        Toast.fire({ icon: 'success', title: 'Tarea eliminada' });
                    } else {
                        Swal.fire('Error', 'No se pudo eliminar la tarea', 'error');
                    }
                }
            });
        });
    };

    // --- CREAR / EDITAR ---
    window.abrirModalTarea = function (id = null) {
        let url = urls.form;
        if (id) url += '?id=' + id;
        const isEdit = id !== null;

        $.get(url, function (html) {
            Swal.fire({
                title: isEdit ? '<i class="fa-solid fa-pen-to-square me-2"></i>Editar Tarea' : '<i class="fa-solid fa-plus me-2"></i>Nueva Tarea',
                html: html,
                width: '520px',
                showCancelButton: true,
                confirmButtonText: isEdit ? 'Guardar cambios' : 'Crear tarea',
                cancelButtonText: 'Cancelar',
                confirmButtonColor: '#198754',
                preConfirm: () => {
                    const form = document.getElementById('form-todo');
                    const formData = new FormData(form);
                    const data = Object.fromEntries(formData.entries());

                    data.TaskId      = parseInt(data.TaskId) || 0;
                    data.Priority    = parseInt(data.Priority) || 2;
                    data.ReminderDate = data.ReminderDate || null;

                    if (!data.Title || !data.Title.trim()) {
                        Swal.showValidationMessage('El título es obligatorio');
                        return false;
                    }
                    if (!data.DueDate) {
                        Swal.showValidationMessage('La fecha de vencimiento es obligatoria');
                        return false;
                    }
                    if (data.ReminderDate && data.DueDate && data.ReminderDate > data.DueDate) {
                        Swal.showValidationMessage('La fecha de recordatorio no puede ser posterior al vencimiento');
                        return false;
                    }
                    return data;
                }
            }).then(res => {
                if (!res.isConfirmed) return;

                $.ajax({
                    url: urls.save,
                    type: 'POST',
                    contentType: 'application/json',
                    data: JSON.stringify(res.value),
                    headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
                    success: (r) => {
                        if (r.success) {
                            loadTasks();
                            Toast.fire({
                                icon: 'success',
                                title: isEdit ? 'Tarea actualizada' : '¡Tarea creada!'
                            });
                        } else {
                            Swal.fire('Error', r.message || 'No se pudo guardar la tarea', 'error');
                        }
                    },
                    error: function () {
                        Swal.fire('Error', 'Error de conexión al guardar', 'error');
                    }
                });
            });
        });
    };

    // Carga inicial
    loadTasks();
})();
