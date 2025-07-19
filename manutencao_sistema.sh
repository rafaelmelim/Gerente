#!/bin/bash

# =====================================================
# SISTEMA GERENTE - SCRIPT DE MANUTENÇÃO E BACKUP
# =====================================================
# Versão: 1.0.0
# Descrição: Script para backup, manutenção e monitoramento do Sistema Gerente
# =====================================================

# Cores para output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configurações
APP_DIR="/var/www/gerente"
BACKUP_DIR="/var/backups/gerente"
DB_NAME="Projetos"
DB_USER="admin"
DB_PASSWORD="admin123"
DATE=$(date +%Y%m%d_%H%M%S)

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

# Função para criar diretório de backup
create_backup_directory() {
    if [ ! -d "$BACKUP_DIR" ]; then
        print_message "Criando diretório de backup: $BACKUP_DIR"
        sudo mkdir -p $BACKUP_DIR
        sudo chown $USER:$USER $BACKUP_DIR
    fi
}

# Função para backup do banco de dados
backup_database() {
    print_header "BACKUP DO BANCO DE DADOS"
    
    create_backup_directory
    
    BACKUP_FILE="$BACKUP_DIR/gerente_db_$DATE.sql"
    
    print_message "Iniciando backup do banco de dados..."
    print_message "Arquivo: $BACKUP_FILE"
    
    PGPASSWORD=$DB_PASSWORD pg_dump -U $DB_USER -h localhost $DB_NAME > $BACKUP_FILE
    
    if [ $? -eq 0 ]; then
        BACKUP_SIZE=$(du -h $BACKUP_FILE | cut -f1)
        print_message "Backup concluído com sucesso! Tamanho: $BACKUP_SIZE"
        
        # Manter apenas os últimos 10 backups
        cd $BACKUP_DIR
        ls -t gerente_db_*.sql | tail -n +11 | xargs -r rm
        print_message "Backups antigos removidos (mantidos últimos 10)"
    else
        print_error "Falha no backup do banco de dados"
        exit 1
    fi
}

# Função para backup dos arquivos da aplicação
backup_application() {
    print_header "BACKUP DOS ARQUIVOS DA APLICAÇÃO"
    
    create_backup_directory
    
    BACKUP_FILE="$BACKUP_DIR/gerente_app_$DATE.tar.gz"
    
    print_message "Iniciando backup dos arquivos da aplicação..."
    print_message "Arquivo: $BACKUP_FILE"
    
    cd $APP_DIR
    tar -czf $BACKUP_FILE --exclude='bin' --exclude='obj' --exclude='.git' .
    
    if [ $? -eq 0 ]; then
        BACKUP_SIZE=$(du -h $BACKUP_FILE | cut -f1)
        print_message "Backup da aplicação concluído! Tamanho: $BACKUP_SIZE"
        
        # Manter apenas os últimos 5 backups da aplicação
        cd $BACKUP_DIR
        ls -t gerente_app_*.tar.gz | tail -n +6 | xargs -r rm
        print_message "Backups antigos da aplicação removidos (mantidos últimos 5)"
    else
        print_error "Falha no backup da aplicação"
        exit 1
    fi
}

# Função para backup completo
backup_complete() {
    print_header "BACKUP COMPLETO DO SISTEMA"
    
    backup_database
    backup_application
    
    print_message "Backup completo concluído com sucesso!"
}

# Função para restaurar banco de dados
restore_database() {
    print_header "RESTAURAÇÃO DO BANCO DE DADOS"
    
    if [ -z "$1" ]; then
        print_error "Especifique o arquivo de backup"
        echo "Uso: $0 restore-db <arquivo_backup>"
        exit 1
    fi
    
    BACKUP_FILE="$1"
    
    if [ ! -f "$BACKUP_FILE" ]; then
        print_error "Arquivo de backup não encontrado: $BACKUP_FILE"
        exit 1
    fi
    
    print_warning "ATENÇÃO: Esta operação irá substituir o banco de dados atual!"
    read -p "Deseja continuar? (y/n): " -n 1 -r
    echo
    
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        print_message "Restauração cancelada"
        exit 0
    fi
    
    print_message "Iniciando restauração do banco de dados..."
    
    # Parar aplicação
    sudo systemctl stop gerente.service
    
    # Restaurar banco
    PGPASSWORD=$DB_PASSWORD psql -U $DB_USER -h localhost -d $DB_NAME -c "DROP SCHEMA public CASCADE; CREATE SCHEMA public;"
    PGPASSWORD=$DB_PASSWORD psql -U $DB_USER -h localhost -d $DB_NAME < $BACKUP_FILE
    
    if [ $? -eq 0 ]; then
        print_message "Restauração concluída com sucesso!"
        
        # Reiniciar aplicação
        sudo systemctl start gerente.service
        print_message "Aplicação reiniciada"
    else
        print_error "Falha na restauração do banco de dados"
        sudo systemctl start gerente.service
        exit 1
    fi
}

# Função para verificar status do sistema
check_system_status() {
    print_header "STATUS DO SISTEMA"
    
    echo -e "${GREEN}=== Serviços ===${NC}"
    
    # Verificar serviço da aplicação
    if sudo systemctl is-active --quiet gerente.service; then
        echo -e "  Aplicação: ${GREEN}✓ Ativo${NC}"
    else
        echo -e "  Aplicação: ${RED}✗ Inativo${NC}"
    fi
    
    # Verificar PostgreSQL
    if sudo systemctl is-active --quiet postgresql; then
        echo -e "  PostgreSQL: ${GREEN}✓ Ativo${NC}"
    else
        echo -e "  PostgreSQL: ${RED}✗ Inativo${NC}"
    fi
    
    # Verificar Nginx (se instalado)
    if command -v nginx >/dev/null 2>&1; then
        if sudo systemctl is-active --quiet nginx; then
            echo -e "  Nginx: ${GREEN}✓ Ativo${NC}"
        else
            echo -e "  Nginx: ${RED}✗ Inativo${NC}"
        fi
    fi
    
    echo -e "\n${GREEN}=== Banco de Dados ===${NC}"
    
    # Verificar conexão com banco
    if PGPASSWORD=$DB_PASSWORD psql -U $DB_USER -h localhost -d $DB_NAME -c "SELECT 1;" >/dev/null 2>&1; then
        echo -e "  Conexão: ${GREEN}✓ OK${NC}"
        
        # Contar registros nas tabelas principais
        USUARIOS=$(PGPASSWORD=$DB_PASSWORD psql -U $DB_USER -h localhost -d $DB_NAME -t -c "SELECT COUNT(*) FROM usuarios;" | xargs)
        PERFIS=$(PGPASSWORD=$DB_PASSWORD psql -U $DB_USER -h localhost -d $DB_NAME -t -c "SELECT COUNT(*) FROM perfis_acesso;" | xargs)
        BACKLOG=$(PGPASSWORD=$DB_PASSWORD psql -U $DB_USER -h localhost -d $DB_NAME -t -c "SELECT COUNT(*) FROM backlog_arquitetura;" | xargs)
        
        echo -e "  Usuários: ${BLUE}$USUARIOS${NC}"
        echo -e "  Perfis: ${BLUE}$PERFIS${NC}"
        echo -e "  Backlog: ${BLUE}$BACKLOG${NC}"
    else
        echo -e "  Conexão: ${RED}✗ Falha${NC}"
    fi
    
    echo -e "\n${GREEN}=== Recursos do Sistema ===${NC}"
    
    # Uso de disco
    DISK_USAGE=$(df -h $APP_DIR | tail -1 | awk '{print $5}')
    echo -e "  Uso de disco: ${BLUE}$DISK_USAGE${NC}"
    
    # Uso de memória
    MEMORY_USAGE=$(free -h | grep Mem | awk '{print $3"/"$2}')
    echo -e "  Uso de memória: ${BLUE}$MEMORY_USAGE${NC}"
    
    # Uso de CPU
    CPU_USAGE=$(top -bn1 | grep "Cpu(s)" | awk '{print $2}' | cut -d'%' -f1)
    echo -e "  Uso de CPU: ${BLUE}${CPU_USAGE}%${NC}"
}

# Função para limpeza de logs
cleanup_logs() {
    print_header "LIMPEZA DE LOGS"
    
    print_message "Limpando logs antigos..."
    
    # Limpar logs do systemd (manter últimos 7 dias)
    sudo journalctl --vacuum-time=7d
    
    # Limpar logs do PostgreSQL (se existirem)
    if [ -d "/var/log/postgresql" ]; then
        sudo find /var/log/postgresql -name "*.log" -mtime +7 -delete
    fi
    
    # Limpar logs do Nginx (se existirem)
    if [ -d "/var/log/nginx" ]; then
        sudo find /var/log/nginx -name "*.log" -mtime +7 -delete
    fi
    
    print_message "Limpeza de logs concluída"
}

# Função para atualizar sistema
update_system() {
    print_header "ATUALIZAÇÃO DO SISTEMA"
    
    print_message "Atualizando lista de pacotes..."
    sudo apt update
    
    print_message "Atualizando pacotes do sistema..."
    sudo apt upgrade -y
    
    print_message "Limpando pacotes desnecessários..."
    sudo apt autoremove -y
    sudo apt autoclean
    
    print_message "Atualização do sistema concluída"
}

# Função para reiniciar serviços
restart_services() {
    print_header "REINICIANDO SERVIÇOS"
    
    print_message "Reiniciando PostgreSQL..."
    sudo systemctl restart postgresql
    
    print_message "Reiniciando aplicação..."
    sudo systemctl restart gerente.service
    
    if command -v nginx >/dev/null 2>&1; then
        print_message "Reiniciando Nginx..."
        sudo systemctl restart nginx
    fi
    
    print_message "Serviços reiniciados com sucesso"
}

# Função para mostrar logs
show_logs() {
    print_header "LOGS DO SISTEMA"
    
    echo -e "${GREEN}=== Logs da Aplicação ===${NC}"
    sudo journalctl -u gerente.service -n 20 --no-pager
    
    echo -e "\n${GREEN}=== Logs do PostgreSQL ===${NC}"
    sudo journalctl -u postgresql.service -n 10 --no-pager
    
    if command -v nginx >/dev/null 2>&1; then
        echo -e "\n${GREEN}=== Logs do Nginx ===${NC}"
        sudo journalctl -u nginx.service -n 10 --no-pager
    fi
}

# Função para listar backups
list_backups() {
    print_header "BACKUPS DISPONÍVEIS"
    
    if [ ! -d "$BACKUP_DIR" ]; then
        print_warning "Diretório de backup não existe"
        return
    fi
    
    echo -e "${GREEN}=== Backups do Banco de Dados ===${NC}"
    if ls $BACKUP_DIR/gerente_db_*.sql >/dev/null 2>&1; then
        ls -lh $BACKUP_DIR/gerente_db_*.sql
    else
        echo "  Nenhum backup do banco encontrado"
    fi
    
    echo -e "\n${GREEN}=== Backups da Aplicação ===${NC}"
    if ls $BACKUP_DIR/gerente_app_*.tar.gz >/dev/null 2>&1; then
        ls -lh $BACKUP_DIR/gerente_app_*.tar.gz
    else
        echo "  Nenhum backup da aplicação encontrado"
    fi
}

# Função para mostrar ajuda
show_help() {
    print_header "AJUDA - SCRIPT DE MANUTENÇÃO"
    
    echo "Uso: $0 [comando]"
    echo
    echo "Comandos disponíveis:"
    echo "  backup-db          - Backup apenas do banco de dados"
    echo "  backup-app         - Backup apenas dos arquivos da aplicação"
    echo "  backup-complete    - Backup completo do sistema"
    echo "  restore-db <file>  - Restaurar banco de dados"
    echo "  status             - Verificar status do sistema"
    echo "  logs               - Mostrar logs do sistema"
    echo "  cleanup            - Limpar logs antigos"
    echo "  update             - Atualizar sistema"
    echo "  restart            - Reiniciar serviços"
    echo "  list-backups       - Listar backups disponíveis"
    echo "  help               - Mostrar esta ajuda"
    echo
    echo "Exemplos:"
    echo "  $0 backup-complete"
    echo "  $0 restore-db /var/backups/gerente/gerente_db_20250101_120000.sql"
    echo "  $0 status"
}

# Função principal
main() {
    case "$1" in
        "backup-db")
            backup_database
            ;;
        "backup-app")
            backup_application
            ;;
        "backup-complete")
            backup_complete
            ;;
        "restore-db")
            restore_database "$2"
            ;;
        "status")
            check_system_status
            ;;
        "logs")
            show_logs
            ;;
        "cleanup")
            cleanup_logs
            ;;
        "update")
            update_system
            ;;
        "restart")
            restart_services
            ;;
        "list-backups")
            list_backups
            ;;
        "help"|"--help"|"-h"|"")
            show_help
            ;;
        *)
            print_error "Comando inválido: $1"
            echo
            show_help
            exit 1
            ;;
    esac
}

# Executar função principal
main "$@" 