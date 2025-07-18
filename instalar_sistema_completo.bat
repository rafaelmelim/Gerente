@echo off
setlocal enabledelayedexpansion

REM =====================================================
REM SISTEMA GERENTE - INSTALADOR AUTOMATIZADO (WINDOWS)
REM =====================================================
REM Este script instala o sistema completo do zero
REM Inclui: PostgreSQL, banco de dados, aplicação
REM Versão: 1.4.0
REM =====================================================

REM Configurações
set DB_NAME=Projetos
set DB_USER=admin
set DB_PASSWORD=admin123
set DB_HOST=localhost
set DB_PORT=5432
set APP_NAME=Gerente
set APP_VERSION=1.4.0

echo =====================================================
echo SISTEMA GERENTE - INSTALADOR AUTOMATIZADO
echo =====================================================
echo.

REM =====================================================
REM 1. VERIFICAÇÕES INICIAIS
REM =====================================================

echo [INFO] Verificando requisitos...

REM Verificar se .NET está instalado
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo [ERROR] .NET não encontrado. Instale o .NET 8.0 primeiro.
    pause
    exit /b 1
)

for /f "tokens=*" %%i in ('dotnet --version') do set DOTNET_VERSION=%%i
echo [INFO] .NET encontrado: %DOTNET_VERSION%

REM Verificar se PostgreSQL está instalado
psql --version >nul 2>&1
if %errorlevel% neq 0 (
    echo [WARNING] PostgreSQL não encontrado.
    echo [INFO] Você precisará instalar o PostgreSQL manualmente.
    echo [INFO] Baixe em: https://www.postgresql.org/download/windows/
    echo.
    set /p CONTINUE="Deseja continuar mesmo sem PostgreSQL? (s/n): "
    if /i not "!CONTINUE!"=="s" (
        echo [INFO] Instalação cancelada.
        pause
        exit /b 1
    )
) else (
    for /f "tokens=3" %%i in ('psql --version') do set PSQL_VERSION=%%i
    echo [INFO] PostgreSQL encontrado: %PSQL_VERSION%
)

echo.

REM =====================================================
REM 2. VERIFICAÇÃO DE ARQUIVOS
REM =====================================================

echo [INFO] Verificando arquivos necessários...

if not exist "database_completo.sql" (
    echo [ERROR] Arquivo database_completo.sql não encontrado!
    pause
    exit /b 1
)

if not exist "appsettings.json" (
    echo [ERROR] Arquivo appsettings.json não encontrado!
    pause
    exit /b 1
)

echo [INFO] Todos os arquivos necessários encontrados.
echo.

REM =====================================================
REM 3. CONFIGURAÇÃO DO BANCO DE DADOS
REM =====================================================

echo =====================================================
echo CONFIGURAÇÃO DO BANCO DE DADOS
echo =====================================================

REM Tentar conectar ao PostgreSQL
echo [INFO] Testando conexão com PostgreSQL...
psql -h %DB_HOST% -p %DB_PORT% -U %DB_USER% -d postgres -c "SELECT 1;" >nul 2>&1
if %errorlevel% neq 0 (
    echo [WARNING] Não foi possível conectar ao PostgreSQL.
    echo [INFO] Verifique se o PostgreSQL está rodando e as credenciais estão corretas.
    echo.
    set /p DB_USER="Usuário do banco (padrão: admin): "
    if "!DB_USER!"=="" set DB_USER=admin
    
    set /p DB_PASSWORD="Senha do banco (padrão: admin123): "
    if "!DB_PASSWORD!"=="" set DB_PASSWORD=admin123
    
    set /p DB_HOST="Host do banco (padrão: localhost): "
    if "!DB_HOST!"=="" set DB_HOST=localhost
    
    set /p DB_PORT="Porta do banco (padrão: 5432): "
    if "!DB_PORT!"=="" set DB_PORT=5432
)

REM Criar banco de dados
echo [INFO] Criando banco de dados...
psql -h %DB_HOST% -p %DB_PORT% -U %DB_USER% -d postgres -c "CREATE DATABASE %DB_NAME%;" >nul 2>&1
if %errorlevel% equ 0 (
    echo [INFO] Banco de dados criado com sucesso!
) else (
    echo [WARNING] Banco de dados já existe ou erro na criação.
)

echo.

REM =====================================================
REM 4. EXECUÇÃO DO SCRIPT DE BANCO
REM =====================================================

echo =====================================================
echo CRIAÇÃO DAS ESTRUTURAS DO BANCO
echo =====================================================

echo [INFO] Executando script de criação do banco...
set PGPASSWORD=%DB_PASSWORD%
psql -h %DB_HOST% -p %DB_PORT% -U %DB_USER% -d %DB_NAME% -f database_completo.sql

if %errorlevel% equ 0 (
    echo [INFO] Banco de dados criado com sucesso!
) else (
    echo [ERROR] Erro ao criar banco de dados!
    pause
    exit /b 1
)

echo.

REM =====================================================
REM 5. VERIFICAÇÃO DO BANCO
REM =====================================================

echo =====================================================
echo VERIFICAÇÃO DO BANCO DE DADOS
echo =====================================================

echo [INFO] Verificando estruturas criadas...
psql -h %DB_HOST% -p %DB_PORT% -U %DB_USER% -d %DB_NAME% -c "SELECT tablename FROM pg_tables WHERE schemaname = 'public' ORDER BY tablename;"

echo.
echo [INFO] Verificando dados iniciais...
psql -h %DB_HOST% -p %DB_PORT% -U %DB_USER% -d %DB_NAME% -c "SELECT 'Usuários' as tipo, COUNT(*) as total FROM usuarios UNION ALL SELECT 'Perfis' as tipo, COUNT(*) as total FROM perfis_acesso UNION ALL SELECT 'Configurações Email' as tipo, COUNT(*) as total FROM configuracao_email UNION ALL SELECT 'Parâmetros Sistema' as tipo, COUNT(*) as total FROM parametros_sistema;"

echo.

REM =====================================================
REM 6. CONFIGURAÇÃO DA APLICAÇÃO
REM =====================================================

echo =====================================================
echo CONFIGURAÇÃO DA APLICAÇÃO
echo =====================================================

echo [INFO] Atualizando configuração de conexão...
powershell -Command "(Get-Content appsettings.json) -replace 'Server=localhost', 'Server=%DB_HOST%' -replace 'Port=5432', 'Port=%DB_PORT%' -replace 'Database=Projetos', 'Database=%DB_NAME%' -replace 'Username=admin', 'Username=%DB_USER%' -replace 'Password=admin123', 'Password=%DB_PASSWORD%' | Set-Content appsettings.json"

echo [INFO] Configuração de conexão atualizada!
echo.

REM =====================================================
REM 7. COMPILAÇÃO E TESTE
REM =====================================================

echo =====================================================
echo COMPILAÇÃO E TESTE
echo =====================================================

echo [INFO] Restaurando dependências...
dotnet restore
if %errorlevel% neq 0 (
    echo [ERROR] Erro ao restaurar dependências!
    pause
    exit /b 1
)

echo [INFO] Compilando aplicação...
dotnet build
if %errorlevel% neq 0 (
    echo [ERROR] Erro na compilação!
    pause
    exit /b 1
)

echo [INFO] Aplicação compilada com sucesso!
echo.

REM =====================================================
REM 8. TESTE DE CONEXÃO
REM =====================================================

echo =====================================================
echo TESTE DE CONEXÃO
echo =====================================================

echo [INFO] Testando conexão com o banco...
psql -h %DB_HOST% -p %DB_PORT% -U %DB_USER% -d %DB_NAME% -c "SELECT version();" >nul 2>&1
if %errorlevel% equ 0 (
    echo [INFO] Conexão com banco de dados OK!
) else (
    echo [ERROR] Erro na conexão com banco de dados!
    pause
    exit /b 1
)

echo.

REM =====================================================
REM 9. INFORMAÇÕES FINAIS
REM =====================================================

echo =====================================================
echo INSTALAÇÃO CONCLUÍDA
echo =====================================================

echo [INFO] Sistema instalado com sucesso!
echo.
echo [INFO] Informações de acesso:
echo    URL: http://localhost:5000
echo    Email: admin@dorowcamp.com
echo    Senha: admin
echo.
echo [INFO] Informações do banco:
echo    Banco: %DB_NAME%
echo    Usuário: %DB_USER%
echo    Host: %DB_HOST%:%DB_PORT%
echo.
echo [INFO] Para iniciar a aplicação:
echo    dotnet run
echo.
echo [INFO] Arquivos criados:
echo    database_completo.sql - Script completo do banco
echo    migrar_dados_existentes.sql - Script de migração
echo    instalar_sistema_completo.bat - Este instalador
echo.

REM =====================================================
REM 10. OPÇÕES ADICIONAIS
REM =====================================================

echo =====================================================
echo OPÇÕES ADICIONAIS
echo =====================================================

echo 1. Executar migração de dados existentes
echo 2. Iniciar aplicação agora
echo 3. Abrir pasta do projeto
echo 4. Sair
echo.
set /p choice="Escolha uma opção (1-4): "

if "%choice%"=="1" (
    echo [INFO] Executando migração de dados...
    if exist "migrar_dados_existentes.sql" (
        psql -h %DB_HOST% -p %DB_PORT% -U %DB_USER% -d %DB_NAME% -f migrar_dados_existentes.sql
        echo [INFO] Migração concluída!
    ) else (
        echo [ERROR] Arquivo de migração não encontrado!
    )
) else if "%choice%"=="2" (
    echo [INFO] Iniciando aplicação...
    dotnet run
) else if "%choice%"=="3" (
    echo [INFO] Abrindo pasta do projeto...
    explorer .
) else if "%choice%"=="4" (
    echo [INFO] Saindo...
) else (
    echo [ERROR] Opção inválida!
)

echo.
echo =====================================================
echo FIM DA INSTALAÇÃO
echo =====================================================

echo [INFO] Sistema Gerente instalado com sucesso!
echo [INFO] Consulte o README.md para mais informações
echo [INFO] Para suporte, verifique os logs da aplicação

pause 