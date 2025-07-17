// Funções para o modal "Esqueci minha senha"
function esqueciSenha() {
    const modal = document.getElementById('forgotPasswordModal');
    modal.classList.add('show');
    document.getElementById('resetEmail').focus();
}

function fecharModal() {
    const modal = document.getElementById('forgotPasswordModal');
    modal.classList.remove('show');
    // Limpar formulário
    const form = document.getElementById('forgotPasswordForm');
    if (form) {
        form.reset();
    }
    const emailInput = document.getElementById('resetEmail');
    if (emailInput) {
        emailInput.classList.remove('is-invalid', 'is-valid');
    }
    // Remover loading se estiver ativo
    const sendResetBtn = document.getElementById('sendResetBtn');
    if (sendResetBtn) {
        sendResetBtn.classList.remove('loading');
        sendResetBtn.disabled = false;
    }
}

function criarConta() {
    showMessage('info', 'Funcionalidade de criação de conta será implementada em breve.', 5000);
}

// Função para mostrar mensagens
function showMessage(type, message, duration = 5000) {
    // Remover mensagens existentes
    const existingMessages = document.querySelectorAll('.message-container');
    existingMessages.forEach(msg => msg.remove());

    // Criar nova mensagem
    const messageDiv = document.createElement('div');
    messageDiv.className = `message-container message-${type}`;
    messageDiv.innerHTML = `
        <div class="message-content">
            <div class="message-icon">
                <i class="bi ${getMessageIcon(type)}"></i>
            </div>
            <div class="message-text">${message}</div>
            <button class="message-close" onclick="this.parentElement.parentElement.remove()">
                <i class="bi bi-x"></i>
            </button>
        </div>
    `;

    // Adicionar ao body
    document.body.appendChild(messageDiv);

    // Mostrar com animação
    setTimeout(() => {
        messageDiv.classList.add('message-show');
    }, 100);

    // Remover automaticamente
    if (duration > 0) {
        setTimeout(() => {
            messageDiv.classList.remove('message-show');
            messageDiv.classList.add('message-hide');
            setTimeout(() => {
                if (messageDiv.parentElement) {
                    messageDiv.remove();
                }
            }, 300);
        }, duration);
    }
}

function getMessageIcon(type) {
    switch (type) {
        case 'success': return 'bi-check-circle';
        case 'error': return 'bi-exclamation-circle';
        case 'warning': return 'bi-exclamation-triangle';
        case 'info': return 'bi-info-circle';
        default: return 'bi-info-circle';
    }
}

// Event listeners quando o DOM estiver carregado
document.addEventListener('DOMContentLoaded', function() {
    // Fechar modal ao clicar fora dele
    const modal = document.getElementById('forgotPasswordModal');
    if (modal) {
        modal.addEventListener('click', function(e) {
            if (e.target === modal) {
                fecharModal();
            }
        });
    }

    // Fechar modal com ESC
    document.addEventListener('keydown', function(e) {
        if (e.key === 'Escape') {
            fecharModal();
        }
    });

    // Formulário de redefinição de senha
    const forgotPasswordForm = document.getElementById('forgotPasswordForm');
    if (forgotPasswordForm) {
        forgotPasswordForm.addEventListener('submit', async function(e) {
            e.preventDefault();
            
            const emailInput = document.getElementById('resetEmail');
            const email = emailInput.value.trim();
            const sendResetBtn = document.getElementById('sendResetBtn');
            
            // Validação básica
            if (!email || !isValidEmail(email)) {
                emailInput.classList.add('is-invalid');
                emailInput.classList.remove('is-valid');
                showMessage('error', 'Por favor, informe um e-mail válido.', 5000);
                return;
            }
            
            emailInput.classList.remove('is-invalid');
            emailInput.classList.add('is-valid');
            
            // Mostrar loading
            sendResetBtn.classList.add('loading');
            sendResetBtn.disabled = true;
            
            try {
                const response = await fetch('/Login/ForgotPassword', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                    },
                    body: JSON.stringify({ email: email })
                });
                
                const result = await response.json();
                
                if (result.success) {
                    showMessage('success', result.message, 8000);
                    // Fechar modal após sucesso
                    setTimeout(() => {
                        fecharModal();
                    }, 2000);
                } else {
                    showMessage('error', result.message, 8000);
                }
            } catch (error) {
                showMessage('error', 'Erro ao processar solicitação. Tente novamente.', 5000);
            } finally {
                // Remover loading
                sendResetBtn.classList.remove('loading');
                sendResetBtn.disabled = false;
            }
        });
    }

    // Validação de e-mail em tempo real
    const resetEmailInput = document.getElementById('resetEmail');
    if (resetEmailInput) {
        resetEmailInput.addEventListener('input', function() {
            const email = this.value.trim();
            if (email.length > 0) {
                if (isValidEmail(email)) {
                    this.classList.remove('is-invalid');
                    this.classList.add('is-valid');
                } else {
                    this.classList.add('is-invalid');
                    this.classList.remove('is-valid');
                }
            } else {
                this.classList.remove('is-invalid', 'is-valid');
            }
        });
    }

    // Formulário de login
    const loginForm = document.getElementById('loginForm');
    if (loginForm) {
        loginForm.addEventListener('submit', function(e) {
            const usernameInput = document.getElementById('username');
            const senhaInput = document.getElementById('senha');
            const loginBtn = document.getElementById('loginBtn');
            
            // Validações básicas
            let isValid = true;
            
            if (!usernameInput.value.trim()) {
                usernameInput.classList.add('is-invalid');
                isValid = false;
            } else {
                usernameInput.classList.remove('is-invalid');
            }
            
            if (!senhaInput.value.trim()) {
                senhaInput.classList.add('is-invalid');
                isValid = false;
            } else {
                senhaInput.classList.remove('is-invalid');
            }
            
            if (!isValid) {
                e.preventDefault();
                showMessage('error', 'Por favor, preencha todos os campos obrigatórios.', 5000);
                return;
            }
            
            // Mostrar loading
            loginBtn.classList.add('loading');
            loginBtn.disabled = true;
        });
    }
});

// Função para validar e-mail
function isValidEmail(email) {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailRegex.test(email);
} 