#!/bin/bash

# =====================================================
# SISTEMA GERENTE - INSTALADOR AUTOMATIZADO
# =====================================================
# Este script instala o sistema completo do zero
# Inclui: PostgreSQL, banco de dados, aplicação
# Versão: 1.4.0
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

# Variáveis de configuração
DB_NAME="Projetos"
DB_USER="admin"
DB_PASSWORD="admin123"
DB_HOST="localhost"
DB_PORT="5432"
APP_NAME="Gerente"
APP_VERSION="1.4.0"

# =====================================================
# 1. VERIFICAÇÕES INICIAIS
# =====================================================

print_header "VERIFICAÇÕES INICIAIS"

# Verificar se é root (para instalação do PostgreSQL)
if [[ $EUID -eq 0 ]]; then
   print_warning "Este script está sendo executado como root"
fi

# Verificar sistema operacional
if [[ "$OSTYPE" == "linux-gnu"* ]]; then
    print_message "Sistema Linux detectado"
elif [[ "$OSTYPE" == "darwin"* ]]; then
    print_message "Sistema macOS detectado"
else
    print_error "Sistema operacional não suportado: $OSTYPE"
    exit 1
fi

# Verificar se PostgreSQL está instalado
if command -v psql &> /dev/null; then
    print_message "PostgreSQL já está instalado"
    PSQL_VERSION=$(psql --version | cut -d' ' -f3)
    print_message "Versão do PostgreSQL: $PSQL_VERSION"
else
    print_warning "PostgreSQL não encontrado. Será instalado automaticamente."
fi

# Verificar se .NET está instalado
if command -v dotnet &> /dev/null; then
    print_message ".NET já está instalado"
    DOTNET_VERSION=$(dotnet --version)
    print_message "Versão do .NET: $DOTNET_VERSION"
else
    print_error ".NET não encontrado. Instale o .NET 8.0 primeiro."
    exit 1
fi

# =====================================================
# 2. INSTALAÇÃO DO POSTGRESQL (se necessário)
# =====================================================

print_header "INSTALAÇÃO DO POSTGRESQL"

if ! command -v psql &> /dev/null; then
    print_message "Instalando PostgreSQL..."
    
    if [[ "$OSTYPE" == "linux-gnu"* ]]; then
        # Ubuntu/Debian
        if command -v apt-get &> /dev/null; then
            sudo apt-get update
            sudo apt-get install -y postgresql postgresql-contrib
            sudo systemctl enable postgresql
            sudo systemctl start postgresql
        # CentOS/RHEL
        elif command -v yum &> /dev/null; then
            sudo yum install -y postgresql-server postgresql-contrib
            sudo postgresql-setup initdb
            sudo systemctl enable postgresql
            sudo systemctl start postgresql
        else
            print_error "Gerenciador de pacotes não suportado"
            exit 1
        fi
    elif [[ "$OSTYPE" == "darwin"* ]]; then
        # macOS
        if command -v brew &> /dev/null; then
            brew install postgresql
            brew services start postgresql
        else
            print_error "Homebrew não encontrado. Instale o Homebrew primeiro."
            exit 1
        fi
    fi
    
    print_message "PostgreSQL instalado com sucesso!"
else
    print_message "PostgreSQL já está instalado"
fi

# =====================================================
# 3. CONFIGURAÇÃO DO BANCO DE DADOS
# =====================================================

print_header "CONFIGURAÇÃO DO BANCO DE DADOS"

# Criar usuário do banco (se não existir)
print_message "Criando usuário do banco de dados..."
sudo -u postgres psql -c "CREATE USER $DB_USER WITH PASSWORD '$DB_PASSWORD';" 2>/dev/null || print_warning "Usuário $DB_USER já existe"

# Criar banco de dados (se não existir)
print_message "Criando banco de dados..."
sudo -u postgres psql -c "CREATE DATABASE $DB_NAME OWNER $DB_USER;" 2>/dev/null || print_warning "Banco $DB_NAME já existe"

# Dar permissões ao usuário
print_message "Configurando permissões..."
sudo -u postgres psql -c "GRANT ALL PRIVILEGES ON DATABASE $DB_NAME TO $DB_USER;"
sudo -u postgres psql -c "ALTER USER $DB_USER CREATEDB;"

# =====================================================
# 4. EXECUÇÃO DO SCRIPT DE BANCO
# =====================================================

print_header "CRIAÇÃO DAS ESTRUTURAS DO BANCO"

# Verificar se o arquivo database_completo.sql existe
if [ ! -f "database_completo.sql" ]; then
    print_error "Arquivo database_completo.sql não encontrado!"
    exit 1
fi

print_message "Executando script de criação do banco..."
PGPASSWORD=$DB_PASSWORD psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -f database_completo.sql

if [ $? -eq 0 ]; then
    print_message "Banco de dados criado com sucesso!"
else
    print_error "Erro ao criar banco de dados!"
    exit 1
fi

# =====================================================
# 5. VERIFICAÇÃO DO BANCO
# =====================================================

print_header "VERIFICAÇÃO DO BANCO DE DADOS"

print_message "Verificando estruturas criadas..."
PGPASSWORD=$DB_PASSWORD psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -c "
SELECT 
    schemaname,
    tablename,
    tableowner
FROM pg_tables 
WHERE schemaname = 'public' 
ORDER BY tablename;
"

print_message "Verificando dados iniciais..."
PGPASSWORD=$DB_PASSWORD psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -c "
SELECT 
    'Usuários' as tipo,
    COUNT(*) as total
FROM usuarios
UNION ALL
SELECT 
    'Perfis' as tipo,
    COUNT(*) as total
FROM perfis_acesso
UNION ALL
SELECT 
    'Configurações Email' as tipo,
    COUNT(*) as total
FROM configuracao_email
UNION ALL
SELECT 
    'Parâmetros Sistema' as tipo,
    COUNT(*) as total
FROM parametros_sistema;
"

# =====================================================
# 6. CONFIGURAÇÃO DA APLICAÇÃO
# =====================================================

print_header "CONFIGURAÇÃO DA APLICAÇÃO"

# Verificar se o arquivo appsettings.json existe
if [ ! -f "appsettings.json" ]; then
    print_error "Arquivo appsettings.json não encontrado!"
    exit 1
fi

# Atualizar connection string no appsettings.json
print_message "Atualizando configuração de conexão..."
sed -i "s/Server=localhost/Server=$DB_HOST/g" appsettings.json
sed -i "s/Port=5432/Port=$DB_PORT/g" appsettings.json
sed -i "s/Database=Projetos/Database=$DB_NAME/g" appsettings.json
sed -i "s/Username=admin/Username=$DB_USER/g" appsettings.json
sed -i "s/Password=admin123/Password=$DB_PASSWORD/g" appsettings.json

print_message "Configuração de conexão atualizada!"

# =====================================================
# 7. COMPILAÇÃO E TESTE
# =====================================================

print_header "COMPILAÇÃO E TESTE"

# Restaurar dependências
print_message "Restaurando dependências..."
dotnet restore

if [ $? -ne 0 ]; then
    print_error "Erro ao restaurar dependências!"
    exit 1
fi

# Compilar aplicação
print_message "Compilando aplicação..."
dotnet build

if [ $? -ne 0 ]; then
    print_error "Erro na compilação!"
    exit 1
fi

print_message "Aplicação compilada com sucesso!"

# =====================================================
# 8. TESTE DE CONEXÃO
# =====================================================

print_header "TESTE DE CONEXÃO"

print_message "Testando conexão com o banco..."
PGPASSWORD=$DB_PASSWORD psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -c "SELECT version();" > /dev/null 2>&1

if [ $? -eq 0 ]; then
    print_message "✅ Conexão com banco de dados OK!"
else
    print_error "❌ Erro na conexão com banco de dados!"
    exit 1
fi

# =====================================================
# 9. INFORMAÇÕES FINAIS
# =====================================================

print_header "INSTALAÇÃO CONCLUÍDA"

print_message "🎉 Sistema instalado com sucesso!"
echo ""
print_message "📋 Informações de acesso:"
echo "   🌐 URL: http://localhost:5000"
echo "   📧 Email: admin@dorowcamp.com"
echo "   🔑 Senha: admin"
echo ""
print_message "📊 Informações do banco:"
echo "   🗄️  Banco: $DB_NAME"
echo "   👤 Usuário: $DB_USER"
echo "   🔌 Host: $DB_HOST:$DB_PORT"
echo ""
print_message "🚀 Para iniciar a aplicação:"
echo "   dotnet run"
echo ""
print_message "📁 Arquivos criados:"
echo "   ✅ database_completo.sql - Script completo do banco"
echo "   ✅ migrar_dados_existentes.sql - Script de migração"
echo "   ✅ instalar_sistema_completo.sh - Este instalador"
echo ""

# =====================================================
# 10. OPÇÕES ADICIONAIS
# =====================================================

print_header "OPÇÕES ADICIONAIS"

echo "1. Executar migração de dados existentes"
echo "2. Configurar como serviço do sistema"
echo "3. Configurar Nginx como proxy reverso"
echo "4. Configurar SSL/HTTPS"
echo "5. Sair"
echo ""
read -p "Escolha uma opção (1-5): " choice

case $choice in
    1)
        print_message "Executando migração de dados..."
        if [ -f "migrar_dados_existentes.sql" ]; then
            PGPASSWORD=$DB_PASSWORD psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -f migrar_dados_existentes.sql
            print_message "Migração concluída!"
        else
            print_error "Arquivo de migração não encontrado!"
        fi
        ;;
    2)
        print_message "Configurando como serviço..."
        # Aqui você pode adicionar a configuração do systemd
        print_warning "Configuração de serviço não implementada ainda"
        ;;
    3)
        print_message "Configurando Nginx..."
        # Aqui você pode adicionar a configuração do Nginx
        print_warning "Configuração do Nginx não implementada ainda"
        ;;
    4)
        print_message "Configurando SSL..."
        # Aqui você pode adicionar a configuração do SSL
        print_warning "Configuração SSL não implementada ainda"
        ;;
    5)
        print_message "Saindo..."
        ;;
    *)
        print_error "Opção inválida!"
        ;;
esac

print_header "FIM DA INSTALAÇÃO"

print_message "✅ Sistema Gerente instalado com sucesso!"
print_message "📖 Consulte o README.md para mais informações"
print_message "🆘 Para suporte, verifique os logs da aplicação"

exit 0 