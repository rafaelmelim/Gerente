#!/bin/bash

# =====================================================
# SISTEMA GERENTE - INSTALADOR AUTOMATIZADO UBUNTU
# =====================================================
# Versão: 1.0.0
# Descrição: Script de instalação completa do Sistema Gerente no Ubuntu
# =====================================================

# Cores para output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Função para imprimir mensagens coloridas
print_message() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

print_header() {
    echo -e "${BLUE}=====================================================${NC}"
    echo -e "${BLUE}$1${NC}"
    echo -e "${BLUE}=====================================================${NC}"
}

# Função para verificar se o comando existe
command_exists() {
    command -v "$1" >/dev/null 2>&1
}

# Função para verificar se é root
check_root() {
    if [[ $EUID -eq 0 ]]; then
        print_error "Este script não deve ser executado como root"
        exit 1
    fi
}

# Função para verificar sistema operacional
check_os() {
    if [[ ! -f /etc/os-release ]]; then
        print_error "Sistema operacional não suportado"
        exit 1
    fi
    
    source /etc/os-release
    if [[ "$ID" != "ubuntu" ]]; then
        print_error "Este script é específico para Ubuntu"
        exit 1
    fi
    
    print_message "Sistema operacional detectado: $PRETTY_NAME"
}

# Função para atualizar sistema
update_system() {
    print_header "ATUALIZANDO SISTEMA"
    
    print_message "Atualizando lista de pacotes..."
    sudo apt update
    
    print_message "Atualizando pacotes do sistema..."
    sudo apt upgrade -y
    
    print_message "Instalando pacotes essenciais..."
    sudo apt install -y curl wget git unzip software-properties-common apt-transport-https ca-certificates gnupg lsb-release
}

# Função para instalar .NET 8.0
install_dotnet() {
    print_header "INSTALANDO .NET 8.0 SDK"
    
    if command_exists dotnet; then
        DOTNET_VERSION=$(dotnet --version)
        print_message ".NET já está instalado: $DOTNET_VERSION"
        
        if [[ "$DOTNET_VERSION" == 8.* ]]; then
            print_message ".NET 8.0 já está instalado"
            return 0
        fi
    fi
    
    print_message "Adicionando repositório Microsoft..."
    wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
    sudo dpkg -i packages-microsoft-prod.deb
    rm packages-microsoft-prod.deb
    
    print_message "Atualizando lista de pacotes..."
    sudo apt update
    
    print_message "Instalando .NET 8.0 SDK..."
    sudo apt install -y dotnet-sdk-8.0
    
    if command_exists dotnet; then
        print_message ".NET 8.0 SDK instalado com sucesso: $(dotnet --version)"
    else
        print_error "Falha na instalação do .NET 8.0 SDK"
        exit 1
    fi
}

# Função para instalar PostgreSQL
install_postgresql() {
    print_header "INSTALANDO POSTGRESQL"
    
    if command_exists psql; then
        print_message "PostgreSQL já está instalado"
        return 0
    fi
    
    print_message "Instalando PostgreSQL..."
    sudo apt install -y postgresql postgresql-contrib
    
    print_message "Iniciando serviço PostgreSQL..."
    sudo systemctl start postgresql
    sudo systemctl enable postgresql
    
    if sudo systemctl is-active --quiet postgresql; then
        print_message "PostgreSQL iniciado com sucesso"
    else
        print_error "Falha ao iniciar PostgreSQL"
        exit 1
    fi
}

# Função para configurar PostgreSQL
configure_postgresql() {
    print_header "CONFIGURANDO POSTGRESQL"
    
    # Definir variáveis de banco
    DB_NAME="Projetos"
    DB_USER="admin"
    DB_PASSWORD="admin123"
    
    print_message "Criando usuário e banco de dados..."
    
    # Criar usuário e banco
    sudo -u postgres psql -c "CREATE USER $DB_USER WITH PASSWORD '$DB_PASSWORD';" 2>/dev/null || print_warning "Usuário $DB_USER já existe"
    sudo -u postgres psql -c "CREATE DATABASE \"$DB_NAME\" OWNER $DB_USER;" 2>/dev/null || print_warning "Banco $DB_NAME já existe"
    sudo -u postgres psql -c "GRANT ALL PRIVILEGES ON DATABASE \"$DB_NAME\" TO $DB_USER;" 2>/dev/null || print_warning "Privilégios já concedidos"
    
    print_message "Configurando acesso local..."
    
    # Configurar pg_hba.conf para acesso local
    sudo sed -i 's/local   all             postgres                                peer/local   all             postgres                                md5/' /etc/postgresql/*/main/pg_hba.conf
    sudo sed -i 's/local   all             all                                     peer/local   all             all                                     md5/' /etc/postgresql/*/main/pg_hba.conf
    
    # Reiniciar PostgreSQL
    sudo systemctl restart postgresql
    
    print_message "PostgreSQL configurado com sucesso"
}

# Função para criar diretório da aplicação
create_app_directory() {
    print_header "CRIANDO DIRETÓRIO DA APLICAÇÃO"
    
    APP_DIR="/var/www/gerente"
    
    print_message "Criando diretório: $APP_DIR"
    sudo mkdir -p $APP_DIR
    sudo chown $USER:$USER $APP_DIR
    
    print_message "Diretório criado com sucesso"
}

# Função para copiar arquivos do projeto
copy_project_files() {
    print_header "COPIANDO ARQUIVOS DO PROJETO"
    
    APP_DIR="/var/www/gerente"
    CURRENT_DIR=$(pwd)
    
    print_message "Copiando arquivos do projeto..."
    
    # Copiar todos os arquivos do projeto
    cp -r $CURRENT_DIR/* $APP_DIR/
    
    # Remover arquivos desnecessários
    rm -rf $APP_DIR/.git
    rm -rf $APP_DIR/bin
    rm -rf $APP_DIR/obj
    
    print_message "Arquivos copiados com sucesso"
}

# Função para restaurar dependências
restore_dependencies() {
    print_header "RESTAURANDO DEPENDÊNCIAS"
    
    APP_DIR="/var/www/gerente"
    cd $APP_DIR
    
    print_message "Restaurando pacotes NuGet..."
    dotnet restore
    
    if [ $? -eq 0 ]; then
        print_message "Dependências restauradas com sucesso"
    else
        print_error "Falha ao restaurar dependências"
        exit 1
    fi
}

# Função para compilar projeto
build_project() {
    print_header "COMPILANDO PROJETO"
    
    APP_DIR="/var/www/gerente"
    cd $APP_DIR
    
    print_message "Compilando projeto..."
    dotnet build
    
    if [ $? -eq 0 ]; then
        print_message "Projeto compilado com sucesso"
    else
        print_error "Falha na compilação do projeto"
        exit 1
    fi
}

# Função para executar script do banco
setup_database() {
    print_header "CONFIGURANDO BANCO DE DADOS"
    
    APP_DIR="/var/www/gerente"
    DB_NAME="Projetos"
    DB_USER="admin"
    DB_PASSWORD="admin123"
    
    cd $APP_DIR
    
    if [ -f "database_completo.sql" ]; then
        print_message "Executando script do banco de dados..."
        PGPASSWORD=$DB_PASSWORD psql -h localhost -U $DB_USER -d $DB_NAME -f database_completo.sql
        
        if [ $? -eq 0 ]; then
            print_message "Banco de dados configurado com sucesso"
        else
            print_error "Falha na configuração do banco de dados"
            exit 1
        fi
    else
        print_error "Arquivo database_completo.sql não encontrado"
        exit 1
    fi
}

# Função para configurar arquivo de configuração
configure_appsettings() {
    print_header "CONFIGURANDO APLICAÇÃO"
    
    APP_DIR="/var/www/gerente"
    
    print_message "Configurando appsettings.json..."
    
    cat > $APP_DIR/appsettings.json << EOF
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
    
    print_message "Configuração da aplicação concluída"
}

# Função para instalar Nginx (opcional)
install_nginx() {
    print_header "INSTALANDO NGINX (OPCIONAL)"
    
    read -p "Deseja instalar o Nginx como proxy reverso? (y/n): " -n 1 -r
    echo
    
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        print_message "Instalando Nginx..."
        sudo apt install -y nginx
        
        print_message "Configurando Nginx..."
        
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
        
        if [ $? -eq 0 ]; then
            sudo systemctl restart nginx
            sudo systemctl enable nginx
            print_message "Nginx configurado e iniciado com sucesso"
        else
            print_error "Falha na configuração do Nginx"
        fi
    else
        print_message "Nginx não será instalado"
    fi
}

# Função para criar serviço systemd
create_systemd_service() {
    print_header "CRIANDO SERVIÇO SYSTEMD"
    
    APP_DIR="/var/www/gerente"
    
    print_message "Criando arquivo de serviço..."
    
    sudo tee /etc/systemd/system/gerente.service > /dev/null << EOF
[Unit]
Description=Sistema Gerente
After=network.target postgresql.service

[Service]
WorkingDirectory=$APP_DIR
ExecStart=/usr/bin/dotnet $APP_DIR/bin/Debug/net8.0/Gerente.dll
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
    
    print_message "Recarregando systemd..."
    sudo systemctl daemon-reload
    
    print_message "Habilitando serviço..."
    sudo systemctl enable gerente.service
    
    print_message "Serviço systemd criado com sucesso"
}

# Função para testar aplicação
test_application() {
    print_header "TESTANDO APLICAÇÃO"
    
    print_message "Iniciando aplicação..."
    sudo systemctl start gerente.service
    
    # Aguardar um pouco para a aplicação inicializar
    sleep 5
    
    if sudo systemctl is-active --quiet gerente.service; then
        print_message "Aplicação iniciada com sucesso"
        print_message "Acesse: http://localhost:5000"
        
        if command_exists curl; then
            print_message "Testando conexão..."
            if curl -s http://localhost:5000 > /dev/null; then
                print_message "Aplicação respondendo corretamente"
            else
                print_warning "Aplicação pode não estar respondendo ainda"
            fi
        fi
    else
        print_error "Falha ao iniciar aplicação"
        sudo systemctl status gerente.service
        exit 1
    fi
}

# Função para mostrar informações finais
show_final_info() {
    print_header "INSTALAÇÃO CONCLUÍDA"
    
    echo
    print_message "Sistema Gerente instalado com sucesso!"
    echo
    echo -e "${GREEN}Informações de Acesso:${NC}"
    echo -e "  URL: ${BLUE}http://localhost:5000${NC}"
    echo -e "  Email: ${BLUE}admin@gerente.com${NC}"
    echo -e "  Senha: ${BLUE}admin123${NC}"
    echo
    echo -e "${GREEN}Comandos Úteis:${NC}"
    echo -e "  Status: ${BLUE}sudo systemctl status gerente${NC}"
    echo -e "  Logs: ${BLUE}sudo journalctl -u gerente -f${NC}"
    echo -e "  Reiniciar: ${BLUE}sudo systemctl restart gerente${NC}"
    echo -e "  Parar: ${BLUE}sudo systemctl stop gerente${NC}"
    echo
    echo -e "${GREEN}Diretórios Importantes:${NC}"
    echo -e "  Aplicação: ${BLUE}/var/www/gerente${NC}"
    echo -e "  Logs: ${BLUE}/var/log/gerente${NC}"
    echo -e "  Configuração: ${BLUE}/var/www/gerente/appsettings.json${NC}"
    echo
    echo -e "${GREEN}Banco de Dados:${NC}"
    echo -e "  Nome: ${BLUE}Projetos${NC}"
    echo -e "  Usuário: ${BLUE}admin${NC}"
    echo -e "  Senha: ${BLUE}admin123${NC}"
    echo
    print_message "Instalação concluída com sucesso!"
}

# Função principal
main() {
    print_header "SISTEMA GERENTE - INSTALADOR UBUNTU"
    
    # Verificações iniciais
    check_root
    check_os
    
    # Confirmação do usuário
    echo
    print_warning "Este script irá instalar o Sistema Gerente no Ubuntu"
    print_warning "Serão instalados: .NET 8.0, PostgreSQL, Nginx (opcional)"
    echo
    read -p "Deseja continuar? (y/n): " -n 1 -r
    echo
    
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        print_message "Instalação cancelada"
        exit 0
    fi
    
    # Executar etapas de instalação
    update_system
    install_dotnet
    install_postgresql
    configure_postgresql
    create_app_directory
    copy_project_files
    restore_dependencies
    build_project
    setup_database
    configure_appsettings
    install_nginx
    create_systemd_service
    test_application
    show_final_info
}

# Executar função principal
main "$@" 