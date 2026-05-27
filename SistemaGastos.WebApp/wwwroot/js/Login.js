$(document).on('turbo:load', function () {

    // --- LÓGICA DE LOGIN ---
    // Verificamos si estamos en la pantalla de login buscando un elemento único
    var toggleBtn = document.getElementById('togglePassword');

    if (toggleBtn) {
        // Evento Ver Contraseña (Usamos .off().on() por seguridad si fuera jQuery, pero en vainilla basta con cuidar el scope)
        toggleBtn.addEventListener('click', function () {
            let password = document.getElementById('Password');
            let type = password.getAttribute('type') === 'password' ? 'text' : 'password';
            password.setAttribute('type', type);
            this.innerHTML = type === 'password' ? '<i class="fa-solid fa-eye"></i>' : '<i class="fa-solid fa-eye-slash"></i>';
        });

        // Cargar estado "Remember Me"
        loadRememberMeState();

        // Guardar estado al cambiar
        document.getElementById('rememberMe').addEventListener('change', function () {
            saveRememberMeState(this.checked);
        });

        // Interceptar submit
        var loginForm = document.querySelector('form');
        if (loginForm) {
            loginForm.addEventListener('submit', function () {
                let rememberMeCheckbox = document.getElementById('rememberMe');
                if (rememberMeCheckbox.checked) {
                    let username = document.getElementById('Username').value;
                    localStorage.setItem('rememberedUsername', username);
                } else {
                    localStorage.removeItem('rememberedUsername');
                }
            });
        }
    }

    // --- LÓGICA DE REGISTRO (Confirmar Password) ---
    var toggleConfirm = document.getElementById('toggleConfirmPassword');
    if (toggleConfirm) {
        toggleConfirm.addEventListener('click', function () {
            let confirmInput = document.getElementById('confirmPasswordInput');
            let type = confirmInput.getAttribute('type') === 'password' ? 'text' : 'password';
            confirmInput.setAttribute('type', type);
            this.innerHTML = type === 'password' ? '<i class="fa-solid fa-eye"></i>' : '<i class="fa-solid fa-eye-slash"></i>';
        });
    }

    // --- LÓGICA UPDATE PASSWORD ---
    // Si existe el formulario de update
    var updateForm = document.getElementById("updatePasswordForm");
    if (updateForm) {
        // Asignar evento al botón de submit manual
        // Nota: Asegúrate que tu botón llame a submitFormUpdatePass() o asigna el evento aquí
    }
});

// --- FUNCIONES HELPER (Fuera del load) ---

function saveRememberMeState(checked) {
    localStorage.setItem('rememberMe', checked);
}

function loadRememberMeState() {
    let rememberMe = localStorage.getItem('rememberMe');
    if (rememberMe !== null) {
        let chk = document.getElementById('rememberMe');
        if (chk) chk.checked = rememberMe === 'true';
    }

    let rememberedUsername = localStorage.getItem('rememberedUsername');
    if (rememberedUsername !== null) {
        let userInput = document.getElementById('Username');
        if (userInput) userInput.value = rememberedUsername;
    }
}

// Se puede dejar global para llamadas desde onclick="..." en el HTML
window.submitFormUpdatePass = function () {
    let password = document.getElementById("updatePass").value;
    let confirmPassword = document.getElementById("updatePassConfirm").value;
    let passwordError = document.getElementById("updatePassError");
    let confirmPasswordError = document.getElementById("updatePassConfirmError");

    if (password !== confirmPassword) {
        passwordError.textContent = "Las contraseñas no coinciden";
        confirmPasswordError.textContent = "Las contraseñas no coinciden";
    } else {
        passwordError.textContent = "";
        confirmPasswordError.textContent = "";
        document.getElementById("updatePasswordForm").submit();
    }
};

document.addEventListener('DOMContentLoaded', function () {
    var dropdownElementList = [].slice.call(document.querySelectorAll('.dropdown-toggle'));
    var dropdownList = dropdownElementList.map(function (dropdownToggleEl) {
        return new bootstrap.Dropdown(dropdownToggleEl);
    });
});

