(function () {
    const $container = $('#profile-container');
    if ($container.length === 0) return;

    const urls = {
        updatePass: $container.data('url-update-pass'),
        updateInfo: $container.data('url-update-info')
    };

    window.Toast = window.Toast || Swal.mixin({
        toast: true,
        position: 'top-end',
        showConfirmButton: false,
        timer: 3000,
        timerProgressBar: true
    });

    window.updateInfo = function () {
        const data = {
            ID: parseInt($('#userId').val()),
            Email: $('#inputEmail').val()
        };

        if (!data.Email) { Swal.fire('Atención', 'El email es obligatorio', 'warning'); return; }

        $.ajax({
            url: urls.updateInfo, type: 'POST', contentType: 'application/json',
            data: JSON.stringify(data),
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            success: (res) => {
                if (res.success) Toast.fire({ icon: 'success', title: 'Perfil actualizado' });
                else Swal.fire('Error', res.message, 'error');
            }
        });
    };

    window.updatePassword = function () {
        const newPass = $('#newPass').val();
        const confirmPass = $('#confirmPass').val();
        const userId = parseInt($('#userId').val());

        if (!newPass || !confirmPass) { Swal.fire('Atención', 'Completa los campos', 'warning'); return; }
        if (newPass !== confirmPass) { Swal.fire('Error', 'Las contraseñas no coinciden', 'error'); return; }

        Swal.fire({
            title: '¿Cambiar contraseña?', text: 'Deberás reingresar al sistema.', icon: 'warning',
            showCancelButton: true, confirmButtonText: 'Sí, cambiar', confirmButtonColor: '#d33'
        }).then((result) => {
            if (result.isConfirmed) {
                $.ajax({
                    url: urls.updatePass, type: 'POST', contentType: 'application/json',
                    data: JSON.stringify({ UserID: userId, NewPassword: newPass }),
                    headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
                    success: (res) => {
                        if (res.success) {
                            $('#newPass').val(''); $('#confirmPass').val('');
                            Swal.fire('¡Listo!', 'Contraseña actualizada', 'success');
                        } else Swal.fire('Error', res.message, 'error');
                    }
                });
            }
        });
    };

    window.togglePass = function (inputId, btn) {
        const input = document.getElementById(inputId);
        const icon = btn.querySelector('i');
        if (input.type === "password") {
            input.type = "text";
            icon.className = 'fa-solid fa-eye-slash';
        } else {
            input.type = "password";
            icon.className = 'fa-solid fa-eye';
        }
    };
})();