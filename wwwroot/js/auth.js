document.addEventListener('DOMContentLoaded', function () {
    const tabLogin = document.getElementById('tabLoginBtn');
    const tabSignup = document.getElementById('tabSignupBtn');
    const formLogin = document.getElementById('loginForm');
    const formSignup = document.getElementById('signupForm');

    if (tabLogin && tabSignup && formLogin && formSignup) {
        tabLogin.addEventListener('click', function () {
            tabLogin.classList.add('active');
            tabLogin.setAttribute('aria-selected', 'true');
            tabSignup.classList.remove('active');
            tabSignup.setAttribute('aria-selected', 'false');
            formLogin.style.display = '';
            formSignup.style.display = 'none';
        });

        tabSignup.addEventListener('click', function () {
            tabSignup.classList.add('active');
            tabSignup.setAttribute('aria-selected', 'true');
            tabLogin.classList.remove('active');
            tabLogin.setAttribute('aria-selected', 'false');
            formSignup.style.display = '';
            formLogin.style.display = 'none';
        });
    }

    // Toggle password visibility
    const pwToggles = document.querySelectorAll('.btn-toggle-pw');
    pwToggles.forEach(function (btn) {
        btn.addEventListener('click', function () {
            const input = btn.previousElementSibling;
            if (!input) return;
            if (input.type === 'password') {
                input.type = 'text';
            } else {
                input.type = 'password';
            }
        });
    });

    // Disable continue button on submit
    const forms = [formLogin, formSignup];
    forms.forEach(function (form) {
        if (!form) return;
        form.addEventListener('submit', function () {
            const submitBtn = form.querySelector('.submit-btn');
            if (submitBtn) {
                submitBtn.disabled = true;
                submitBtn.innerText = 'PLEASE WAIT…';
            }
        });
    });
});
