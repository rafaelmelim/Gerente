document.addEventListener('DOMContentLoaded', function() {
    var form = document.getElementById('criarContaForm');
    var submitBtn = document.getElementById('submitBtn');
    var nomeInput = document.getElementById('Nome');
    var emailInput = document.getElementById('Email');
    
    // Validação de e-mail em tempo real
    emailInput.addEventListener('input', function() {
        var email = this.value.trim();
        
        if (email.length > 0) {
            if (isValidEmail(email)) {
                this.classList.remove('is-invalid');
                this.classList.add('is-valid');
            } else {
                this.classList.add('is-invalid');
                this.classList.remove('is-valid');
            }
        } else {
            this.classList.remove('is-invalid');
            this.classList.remove('is-valid');
        }
    });
    
    // Validação de nome em tempo real
    nomeInput.addEventListener('input', function() {
        var nome = this.value.trim();
        
        if (nome.length > 0) {
            if (nome.length >= 2) {
                this.classList.remove('is-invalid');
                this.classList.add('is-valid');
            } else {
                this.classList.add('is-invalid');
                this.classList.remove('is-valid');
            }
        } else {
            this.classList.remove('is-invalid');
            this.classList.remove('is-valid');
        }
    });
    
    // Submit do formulário
    form.addEventListener('submit', function(e) {
        var nome = nomeInput.value.trim();
        var email = emailInput.value.trim();
        
        var isValid = true;
        
        // Validações
        if (!nome || nome.length < 2) {
            nomeInput.classList.add('is-invalid');
            isValid = false;
        } else {
            nomeInput.classList.remove('is-invalid');
        }
        
        if (!email || !isValidEmail(email)) {
            emailInput.classList.add('is-invalid');
            isValid = false;
        } else {
            emailInput.classList.remove('is-invalid');
        }
        
        if (!isValid) {
            e.preventDefault();
            showMessage('error', 'Por favor, corrija os erros no formulário.', 5000);
            return;
        }
        
        // Mostrar loading
        var btnText = submitBtn.querySelector('.btn-text');
        var btnLoading = submitBtn.querySelector('.btn-loading');
        
        btnText.classList.add('d-none');
        btnLoading.classList.remove('d-none');
        submitBtn.disabled = true;
    });
});

// Função para mostrar mensagens
function showMessage(type, message, duration) {
    // Remover mensagens existentes
    var existingMessages = document.querySelectorAll('.message-container');
    existingMessages.forEach(function(msg) { msg.remove(); });

    // Criar nova mensagem
    var messageDiv = document.createElement('div');
    messageDiv.className = 'message-container message-' + type;
    messageDiv.innerHTML =
        '<div class="message-content">' +
            '<div class="message-icon">' +
                '<i class="bi ' + getMessageIcon(type) + '"></i>' +
            '</div>' +
            '<div class="message-text">' + message + '</div>' +
            '<button class="message-close" onclick="this.parentElement.parentElement.remove()">' +
                '<i class="bi bi-x"></i>' +
            '</button>' +
        '</div>';

    // Adicionar ao body
    document.body.appendChild(messageDiv);

    // Mostrar com animação
    setTimeout(function() {
        messageDiv.classList.add('message-show');
    }, 100);

    // Remover automaticamente
    if (duration > 0) {
        setTimeout(function() {
            messageDiv.classList.remove('message-show');
            setTimeout(function() {
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

function isValidEmail(email) {
    var atIndex = email.indexOf('@');
    var dotIndex = email.lastIndexOf('.');
    return atIndex > 0 && dotIndex > atIndex + 1 && dotIndex < email.length - 1;
} 