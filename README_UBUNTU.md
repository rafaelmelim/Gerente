# 🐧 SISTEMA GERENTE - INSTALAÇÃO UBUNTU

## 📋 Visão Geral
Este documento contém instruções completas para instalar o **Sistema Gerente** no Ubuntu, incluindo todas as dependências, configurações e scripts de manutenção.

---

## 🚀 INSTALAÇÃO RÁPIDA

### Opção 1: Instalação Automatizada (Recomendada)
```bash
# 1. Baixar o projeto
git clone <url-do-repositorio>
cd Gerente

# 2. Tornar o script executável
chmod +x instalar_sistema_ubuntu.sh

# 3. Executar instalação automatizada
./instalar_sistema_ubuntu.sh
```

### Opção 2: Instalação Manual
Siga as instruções detalhadas abaixo.

---

## 📋 PRÉ-REQUISITOS

### Sistema Operacional
- Ubuntu 20.04 LTS ou superior
- 2GB RAM mínimo (4GB recomendado)
- 10GB espaço em disco livre
- Acesso root/sudo

### Dependências
- .NET 8.0 SDK
- PostgreSQL 12+
- Nginx (opcional)
- Git

---

## 🛠️ INSTALAÇÃO MANUAL DETALHADA

### 1. Atualização do Sistema
```bash
# Atualizar lista de pacotes
sudo apt update

# Atualizar sistema
sudo apt upgrade -y

# Instalar pacotes essenciais
sudo apt install -y curl wget git unzip software-properties-common apt-transport-https ca-certificates gnupg lsb-release
```

### 2. Instalação do .NET 8.0 SDK
```bash
# Adicionar repositório Microsoft
wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

# Atualizar lista de pacotes
sudo apt update

# Instalar .NET 8.0 SDK
sudo apt install -y dotnet-sdk-8.0

# Verificar instalação
dotnet --version
```

### 3. Instalação do PostgreSQL
```bash
# Instalar PostgreSQL
sudo apt install -y postgresql postgresql-contrib

# Iniciar e habilitar serviço
sudo systemctl start postgresql
sudo systemctl enable postgresql

# Verificar status
sudo systemctl status postgresql
```

### 4. Configuração do PostgreSQL
```bash
# Acessar PostgreSQL como superusuário
sudo -u postgres psql

# Criar usuário e banco de dados
CREATE USER admin WITH PASSWORD 'admin123';
CREATE DATABASE "Projetos" OWNER admin;
GRANT ALL PRIVILEGES ON DATABASE "Projetos" TO admin;
\q

# Configurar acesso local
sudo sed -i 's/local   all             postgres                                peer/local   all             postgres                                md5/' /etc/postgresql/*/main/pg_hba.conf
sudo sed -i 's/local   all             all                                     peer/local   all             all                                     md5/' /etc/postgresql/*/main/pg_hba.conf

# Reiniciar PostgreSQL
sudo systemctl restart postgresql
```

### 5. Download e Configuração do Projeto
```bash
# Criar diretório para aplicação
sudo mkdir -p /var/www/gerente
sudo chown $USER:$USER /var/www/gerente

# Copiar arquivos do projeto
cp -r . /var/www/gerente/

# Remover arquivos desnecessários
rm -rf /var/www/gerente/.git
rm -rf /var/www/gerente/bin
rm -rf /var/www/gerente/obj

# Acessar diretório da aplicação
cd /var/www/gerente
```

### 6. Restauração de Dependências
```bash
# Restaurar pacotes NuGet
dotnet restore

# Compilar projeto
dotnet build
```

### 7. Configuração do Banco de Dados
```bash
# Executar script completo do banco
psql -U admin -d Projetos -h localhost -f database_completo.sql
```

### 8. Configuração da Aplicação
```bash
# Criar arquivo de configuração
cat > appsettings.json << EOF
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=Projetos;Username=admin;Password=admin123"
  }
}
EOF
```

### 9. Instalação do Nginx (Opcional)
```bash
# Instalar Nginx
sudo apt install -y nginx

# Criar configuração do site
sudo tee /etc/nginx/sites-available/gerente > /dev/null << EOF
server {
    listen 80;
    server_name _;

    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade \$http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host \$host;
        proxy_cache_bypass \$http_upgrade;
        proxy_set_header X-Real-IP \$remote_addr;
        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto \$scheme;
    }
}
EOF

# Ativar site
sudo ln -sf /etc/nginx/sites-available/gerente /etc/nginx/sites-enabled/
sudo rm -f /etc/nginx/sites-enabled/default

# Testar configuração
sudo nginx -t

# Reiniciar Nginx
sudo systemctl restart nginx
sudo systemctl enable nginx
```

### 10. Configuração do Serviço Systemd
```bash
# Criar arquivo de serviço
sudo tee /etc/systemd/system/gerente.service > /dev/null << EOF
[Unit]
Description=Sistema Gerente
After=network.target postgresql.service

[Service]
WorkingDirectory=/var/www/gerente
ExecStart=/usr/bin/dotnet /var/www/gerente/bin/Debug/net8.0/Gerente.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=gerente
User=$USER
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

[Install]
WantedBy=multi-user.target
EOF

# Recarregar systemd
sudo systemctl daemon-reload

# Habilitar e iniciar serviço
sudo systemctl enable gerente.service
sudo systemctl start gerente.service

# Verificar status
sudo systemctl status gerente.service
```

---

## 🔧 CONFIGURAÇÃO PÓS-INSTALAÇÃO

### 1. Verificar Instalação
```bash
# Verificar se a aplicação está rodando
sudo systemctl status gerente.service

# Verificar se o banco está acessível
psql -U admin -d Projetos -h localhost -c "SELECT COUNT(*) FROM usuarios;"

# Testar acesso web
curl http://localhost:5000
```

### 2. Configuração de Firewall (Opcional)
```bash
# Permitir acesso HTTP
sudo ufw allow 80/tcp

# Permitir acesso HTTPS (se configurado)
sudo ufw allow 443/tcp

# Habilitar firewall
sudo ufw enable
```

### 3. Configuração de SSL (Opcional)
```bash
# Instalar Certbot
sudo apt install -y certbot python3-certbot-nginx

# Obter certificado SSL
sudo certbot --nginx -d seu-dominio.com

# Configurar renovação automática
sudo crontab -e
# Adicionar linha: 0 12 * * * /usr/bin/certbot renew --quiet
```

---

## 📊 SCRIPT DE MANUTENÇÃO

### Instalação do Script
```bash
# Tornar executável
chmod +x manutencao_sistema.sh

# Mover para diretório do sistema
sudo mv manutencao_sistema.sh /usr/local/bin/gerente-manutencao
```

### Comandos Disponíveis
```bash
# Verificar status do sistema
gerente-manutencao status

# Backup completo
gerente-manutencao backup-complete

# Backup apenas do banco
gerente-manutencao backup-db

# Backup apenas da aplicação
gerente-manutencao backup-app

# Restaurar banco de dados
gerente-manutencao restore-db /caminho/para/backup.sql

# Mostrar logs
gerente-manutencao logs

# Limpar logs antigos
gerente-manutencao cleanup

# Atualizar sistema
gerente-manutencao update

# Reiniciar serviços
gerente-manutencao restart

# Listar backups
gerente-manutencao list-backups

# Mostrar ajuda
gerente-manutencao help
```

---

## 🔍 TROUBLESHOOTING

### Problemas Comuns

#### 1. Erro de Conexão com Banco
```bash
# Verificar se PostgreSQL está rodando
sudo systemctl status postgresql

# Verificar configurações de conexão
sudo nano /etc/postgresql/*/main/pg_hba.conf

# Testar conexão manual
psql -U admin -d Projetos -h localhost
```

#### 2. Erro de Permissões
```bash
# Verificar permissões do diretório
ls -la /var/www/gerente

# Corrigir permissões
sudo chown -R $USER:$USER /var/www/gerente
sudo chmod -R 755 /var/www/gerente
```

#### 3. Erro de Porta
```bash
# Verificar portas em uso
sudo netstat -tlnp | grep :5000

# Verificar se a aplicação está rodando
sudo systemctl status gerente.service

# Verificar logs
sudo journalctl -u gerente.service -f
```

#### 4. Erro de Compilação
```bash
# Verificar versão do .NET
dotnet --version

# Limpar cache
dotnet clean
dotnet restore
dotnet build
```

#### 5. Erro de Nginx
```bash
# Verificar configuração
sudo nginx -t

# Verificar logs
sudo tail -f /var/log/nginx/error.log

# Reiniciar Nginx
sudo systemctl restart nginx
```

---

## 📈 MONITORAMENTO

### Logs do Sistema
```bash
# Logs da aplicação
sudo journalctl -u gerente.service -f

# Logs do PostgreSQL
sudo journalctl -u postgresql.service -f

# Logs do Nginx
sudo tail -f /var/log/nginx/access.log
sudo tail -f /var/log/nginx/error.log
```

### Métricas de Performance
```bash
# Uso de CPU e memória
htop

# Uso de disco
df -h

# Processos da aplicação
ps aux | grep dotnet
```

### Backup Automático
```bash
# Criar cron job para backup diário
crontab -e

# Adicionar linha para backup diário às 2h da manhã
0 2 * * * /usr/local/bin/gerente-manutencao backup-complete
```

---

## 🔒 SEGURANÇA

### Configurações de Segurança
```bash
# Atualizar senhas padrão
psql -U admin -d Projetos -c "UPDATE usuarios SET senha = 'novo_hash_aqui' WHERE email = 'admin@gerente.com';"

# Configurar firewall
sudo ufw default deny incoming
sudo ufw default allow outgoing
sudo ufw allow ssh
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp
sudo ufw enable

# Configurar fail2ban
sudo apt install -y fail2ban
sudo systemctl enable fail2ban
sudo systemctl start fail2ban
```

### Backup de Segurança
```bash
# Backup automático com criptografia
# Adicionar ao crontab
0 3 * * * tar -czf /var/backups/gerente/gerente_encrypted_$(date +%Y%m%d).tar.gz --exclude='bin' --exclude='obj' /var/www/gerente && gpg -e /var/backups/gerente/gerente_encrypted_$(date +%Y%m%d).tar.gz
```

---

## 📞 SUPORTE

### Informações de Contato
- **Email**: suporte@gerente.com
- **Documentação**: Este documento
- **Logs**: `/var/log/gerente/`

### Comandos Úteis
```bash
# Status completo do sistema
gerente-manutencao status

# Backup antes de atualizações
gerente-manutencao backup-complete

# Verificar integridade do banco
psql -U admin -d Projetos -c "SELECT * FROM verificar_integridade_banco();"

# Estatísticas do sistema
psql -U admin -d Projetos -c "SELECT * FROM vw_estatisticas_sistema;"
```

---

## 📄 LICENÇA

Este sistema é desenvolvido para uso interno e comercial.
Todos os direitos reservados.

---

**Versão**: 1.0.0  
**Data**: 2025  
**Sistema Gerente** - Instalação Ubuntu 