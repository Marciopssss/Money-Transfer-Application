// File: wwwroot/js/auth.js

function showTab(tab) {
    document.getElementById('tab-login').classList.toggle('active', tab === 'login');
    document.getElementById('tab-signup').classList.toggle('active', tab === 'signup');
    document.getElementById('panel-login').classList.toggle('hidden', tab !== 'login');
    document.getElementById('panel-signup').classList.toggle('hidden', tab !== 'signup');
}

function togglePass(inputId, eyeEl) {
    const input = document.getElementById(inputId);
    if (input.type === 'password') {
        input.type = 'text';
        eyeEl.style.color = '#4F8EF7';
    } else {
        input.type = 'password';
        eyeEl.style.color = '#3A4060';
    }
}

// If the server returns with a register error, auto-open signup tab
document.addEventListener('DOMContentLoaded', function () {
    const isRegisterError = document.querySelector('#panel-signup .ft-field-error');
    if (isRegisterError) showTab('signup');
});