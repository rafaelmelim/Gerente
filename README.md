# Sistema Gerente - Sistema de Gerenciamento de Projetos

## 📋 Descrição
Sistema web desenvolvido em ASP.NET Core MVC para gerenciamento de projetos, com funcionalidades de autenticação, redefinição de senha e configuração de email.

## 🚀 Funcionalidades Implementadas

### 1. Autenticação de Usuários
- Login com email e senha
- Validação de credenciais
- Hash seguro de senhas
- Redirecionamento baseado em autenticação

### 2. Sistema de Perfis de Acesso 🔐
- **Perfis Padrão**: Administrador e Usuário
- **Controle Granular**: Configurações, Usuários, Projetos, Relatórios
- **Acesso Total**: Perfil Administrador sem restrições
- **Acesso Limitado**: Perfil Usuário sem acesso a configurações
- **Associação de Perfis**: Cada usuário vinculado a um perfil
- **Menu Dinâmico**: Interface adaptada ao perfil do usuário

### 3. Gerenciamento de Usuários
- Cadastro de usuários com perfil de acesso
- Edição de informações e perfil
- Exclusão segura (proteção do admin)
- Listagem com informações do perfil

### 4. Redefinição de Senha
- Geração de tokens seguros
- Envio de email com link de redefinição
- Validação de tokens
- Interface para nova senha com validação
- Confirmação de senha em tempo real

### 5. Configuração de Email
- Interface para configuração de SMTP
- Validação de configurações
- Teste de conexão de email

### 6. Parâmetros do Sistema ⚙️
- **Cabeçalho do Sistema**: Personalização do título e cabeçalho em todo o sistema
- **Versão do Sistema**: Exibida no rodapé da tela inicial
- **Nome do Rodapé**: Substituição do rodapé padrão em todo o sistema
- **Aplicação Automática**: Parâmetros aplicados automaticamente em todas as telas
- **Controle de Acesso**: Acesso restrito apenas para usuários autorizados

### 7. Interface Responsiva 📱
- **Design Mobile-First**: Interface otimizada para dispositivos móveis
- **Sidebar Adaptativa**: Menu lateral responsivo com toggle para mobile
- **Formulários Responsivos**: Campos e botões adaptados para touch
- **Breakpoints Múltiplos**: Suporte para desktop, tablet e mobile
- **Acessibilidade**: Navegação por teclado e suporte a screen readers

## 🛠️ Tecnologias Utilizadas
- **Backend**: ASP.NET Core 8.0 MVC
- **Banco de Dados**: PostgreSQL
- **Frontend**: HTML, CSS, JavaScript, Bootstrap 5
- **Responsividade**: CSS Grid, Flexbox, Media Queries
- **Email**: SMTP via configuração personalizada

## 📁 Estrutura do Projeto

```
Gerente/
├── Controllers/
│   ├── BaseController.cs              # Controller base com parâmetros do sistema
│   ├── HomeController.cs              # Página inicial e acesso negado
│   ├── LoginController.cs             # Autenticação e reset de senha
│   ├── UsuarioController.cs           # Gerenciamento de usuários
│   ├── PerfilAcessoController.cs      # Gerenciamento de perfis
│   ├── ConfiguracaoEmailController.cs # Configuração de email
│   └── ParametroSistemaController.cs  # Gerenciamento de parâmetros do sistema
├── Models/
│   ├── Usuario.cs                     # Modelo de usuário
│   ├── PerfilAcesso.cs                # Modelo de perfil de acesso
│   ├── ConfiguracaoEmail.cs           # Modelo de configuração de email
│   ├── ParametroSistema.cs            # Modelo de parâmetros do sistema
│   └── ErrorViewModel.cs              # Modelo de erro
├── Services/
│   ├── AccessControlService.cs        # Serviço de controle de acesso
│   └── PasswordResetService.cs        # Serviço de reset de senha
├── Filters/
│   ├── AccessControlFilter.cs         # Filtros de autorização
│   └── RequireSystemParametersAccessAttribute.cs # Filtro para parâmetros do sistema
├── Views/
│   ├── Home/                          # Views da página inicial
│   ├── Login/                         # Views de autenticação
│   ├── Usuario/                       # Views de usuários
│   ├── PerfilAcesso/                  # Views de perfis
│   ├── ConfiguracaoEmail/             # Views de configuração
│   └── ParametroSistema/              # Views de parâmetros do sistema
├── wwwroot/                           # Arquivos estáticos
│   ├── css/                          # Estilos CSS
│   │   ├── site.css                  # Estilos principais
│   │   ├── login.css                 # Estilos da tela de login
│   │   └── messages.css              # Sistema de mensagens
│   └── js/                           # Scripts JavaScript
├── Program.cs                         # Configuração da aplicação
├── appsettings.json                  # Configurações
├── database_setup.sql                # Script de banco de dados
├── setup_perfis_simples.sql          # Script de perfis de acesso
└── criar_tabela_parametros_sistema.sql # Script de parâmetros do sistema
```

## ⚙️ Configuração e Instalação

### Pré-requisitos
- .NET 8.0 SDK
- PostgreSQL 12+
- Visual Studio 2022 ou VS Code

### 🚀 Instalação Automatizada (Recomendada)

#### Windows
```bash
# Execute o instalador automático
instalar_sistema_completo.bat
```

#### Linux/macOS
```bash
# Torne o script executável
chmod +x instalar_sistema_completo.sh

# Execute o instalador automático
./instalar_sistema_completo.sh
```

### 📋 Instalação Manual

#### 1. Configuração do Banco de Dados

##### 1.1 Instalar PostgreSQL
```bash
# Windows (via installer)
# Baixar e instalar de: https://www.postgresql.org/download/windows/

# Ou via Chocolatey
choco install postgresql

# Linux (Ubuntu/Debian)
sudo apt update
sudo apt install postgresql postgresql-contrib

# macOS (via Homebrew)
brew install postgresql
brew services start postgresql
```

##### 1.2 Criar Banco de Dados
```sql
-- Conectar ao PostgreSQL como superusuário
psql -U postgres

-- Criar usuário e banco
CREATE USER admin WITH PASSWORD 'admin123';
CREATE DATABASE Projetos OWNER admin;
GRANT ALL PRIVILEGES ON DATABASE Projetos TO admin;
```

##### 1.3 Executar Script Completo do Banco
```bash
# Executar o script completo (inclui tudo)
psql -U admin -d Projetos -f database_completo.sql
```

### 📁 Arquivos de Instalação Criados
- `database_completo.sql` - Script completo do banco (tabelas, views, functions, triggers, dados)
- `migrar_dados_existentes.sql` - Script para migrar dados de banco existente
- `instalar_sistema_completo.sh` - Instalador automatizado para Linux/macOS
- `instalar_sistema_completo.bat` - Instalador automatizado para Windows

### 🔄 Migração de Dados Existentes
Se você tem dados em um banco existente que deseja migrar:

#### 1. Backup dos Dados Existentes
```bash
# Fazer backup das tabelas existentes
pg_dump -U admin -d banco_antigo -t perfis_acesso > backup_perfis_acesso.sql
pg_dump -U admin -d banco_antigo -t usuarios > backup_usuarios.sql
pg_dump -U admin -d banco_antigo -t configuracao_email > backup_configuracao_email.sql
pg_dump -U admin -d banco_antigo -t parametros_sistema > backup_parametros_sistema.sql
```

#### 2. Executar Migração
```bash
# Após criar o novo banco com database_completo.sql
psql -U admin -d Projetos -f migrar_dados_existentes.sql
```

#### 3. Verificar Migração
```sql
-- Verificar dados migrados
SELECT * FROM vw_usuarios_com_perfil;
SELECT * FROM vw_estatisticas_sistema;
SELECT * FROM verificar_integridade_banco();
```

### 2. Configuração da Aplicação

#### 2.1 Configurar Connection String
Editar `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=Projetos;Username=admin;Password=admin"
  }
}
```

#### 2.2 Configurar Email (Opcional)
Acesse `/ConfiguracaoEmail` após o primeiro login para configurar:
- Servidor SMTP
- Porta
- Usuário
- Senha
- SSL/TLS

#### 2.3 Responsividade
O sistema é totalmente responsivo e funciona em:
- **Desktop**: > 1024px
- **Tablet**: 768px - 1023px
- **Mobile**: 480px - 767px
- **Mobile Pequeno**: 360px - 479px
- **Mobile Extra Pequeno**: < 360px

### 3. Executar a Aplicação

#### 3.1 Via Visual Studio
1. Abrir `Gerente.sln`
2. Restaurar pacotes NuGet
3. Pressionar F5 ou Ctrl+F5

#### 3.2 Via Linha de Comando
```bash
# Navegar para o diretório do projeto
cd Gerente

# Restaurar dependências
dotnet restore

# Executar aplicação
dotnet run
```

#### 3.3 Via Docker (Opcional)
```bash
# Construir e executar com Docker Compose
docker-compose up --build
```

#### 3.4 Produção (Linux)
```bash
# Publicar aplicação
dotnet publish -c Release -o ./publish

# Executar em produção
dotnet ./publish/Gerente.dll
```

## 🐧 Deploy em Servidor Linux Ubuntu

### Pré-requisitos do Servidor
```bash
# Atualizar sistema
sudo apt update && sudo apt upgrade -y

# Instalar dependências básicas
sudo apt install -y curl wget git unzip software-properties-common apt-transport-https ca-certificates gnupg lsb-release
```

### 1. Instalação do .NET 8.0
```bash
# Adicionar repositório Microsoft
wget https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

# Instalar .NET 8.0 SDK
sudo apt update
sudo apt install -y dotnet-sdk-8.0

# Verificar instalação
dotnet --version
```

### 2. Instalação do PostgreSQL
```bash
# Adicionar repositório PostgreSQL
sudo sh -c 'echo "deb http://apt.postgresql.org/pub/repos/apt $(lsb_release -cs)-pgdg main" > /etc/apt/sources.list.d/pgdg.list'
wget --quiet -O - https://www.postgresql.org/media/keys/ACCC4CF8.asc | sudo apt-key add -

# Instalar PostgreSQL
sudo apt update
sudo apt install -y postgresql postgresql-contrib

# Verificar instalação
sudo systemctl status postgresql
```

### 3. Configuração do PostgreSQL
```bash
# Acessar usuário postgres
sudo -u postgres psql

# Criar usuário e banco de dados
CREATE USER admin WITH PASSWORD 'sua_senha_segura';
CREATE DATABASE Projetos OWNER admin;
GRANT ALL PRIVILEGES ON DATABASE Projetos TO admin;
\q

# Configurar acesso remoto (opcional)
sudo nano /etc/postgresql/*/main/postgresql.conf
# Descomentar e alterar: listen_addresses = '*'

sudo nano /etc/postgresql/*/main/pg_hba.conf
# Adicionar linha: host all all 0.0.0.0/0 md5

# Reiniciar PostgreSQL
sudo systemctl restart postgresql
```

### 4. Configuração do Nginx (Proxy Reverso)
```bash
# Instalar Nginx
sudo apt install -y nginx

# Criar configuração do site
sudo nano /etc/nginx/sites-available/gerente

# Conteúdo da configuração:
server {
    listen 80;
    server_name seu_dominio.com www.seu_dominio.com;
    
    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;
    }
}

# Ativar site
sudo ln -s /etc/nginx/sites-available/gerente /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl restart nginx
```

### 5. Deploy da Aplicação
```bash
# Criar diretório da aplicação
sudo mkdir -p /var/www/gerente
sudo chown $USER:$USER /var/www/gerente

# Clonar ou copiar o projeto
cd /var/www/gerente
git clone https://github.com/seu-usuario/gerente.git .
# OU copiar arquivos via SCP/SFTP

# Configurar aplicação
cp appsettings.json appsettings.Production.json
nano appsettings.Production.json

# Configuração de produção:
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=Projetos;Username=admin;Password=sua_senha_segura"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}

# Restaurar dependências
dotnet restore

# Publicar aplicação
dotnet publish -c Release -o ./publish

# Configurar permissões
sudo chown -R www-data:www-data /var/www/gerente
sudo chmod -R 755 /var/www/gerente
```

### 6. Configuração do Systemd (Serviço)
```bash
# Criar arquivo de serviço
sudo nano /etc/systemd/system/gerente.service

# Conteúdo do serviço:
[Unit]
Description=Gerente Web Application
After=network.target postgresql.service

[Service]
WorkingDirectory=/var/www/gerente/publish
ExecStart=/usr/bin/dotnet /var/www/gerente/publish/Gerente.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=gerente
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

[Install]
WantedBy=multi-user.target

# Ativar e iniciar serviço
sudo systemctl enable gerente
sudo systemctl start gerente
sudo systemctl status gerente
```

### 7. Configuração do SSL (HTTPS)
```bash
# Instalar Certbot
sudo apt install -y certbot python3-certbot-nginx

# Obter certificado SSL
sudo certbot --nginx -d seu_dominio.com -d www.seu_dominio.com

# Configurar renovação automática
sudo crontab -e
# Adicionar linha: 0 12 * * * /usr/bin/certbot renew --quiet
```

### 8. Executar Scripts do Banco de Dados
```bash
# Conectar ao banco e executar scripts
psql -U admin -d Projetos -h localhost -f database_setup.sql
psql -U admin -d Projetos -h localhost -f setup_perfis_simples.sql
psql -U admin -d Projetos -h localhost -f criar_tabela_parametros_sistema.sql
```

### 9. Configuração de Firewall
```bash
# Instalar UFW
sudo apt install -y ufw

# Configurar regras
sudo ufw allow ssh
sudo ufw allow 'Nginx Full'
sudo ufw allow 5432/tcp  # PostgreSQL (se necessário acesso remoto)

# Ativar firewall
sudo ufw enable
sudo ufw status
```

### 10. Monitoramento e Logs
```bash
# Verificar logs da aplicação
sudo journalctl -u gerente -f

# Verificar logs do Nginx
sudo tail -f /var/log/nginx/access.log
sudo tail -f /var/log/nginx/error.log

# Verificar logs do PostgreSQL
sudo tail -f /var/log/postgresql/postgresql-*.log
```

### 11. Backup Automático
```bash
# Criar script de backup
sudo nano /usr/local/bin/backup-gerente.sh

# Conteúdo do script:
#!/bin/bash
BACKUP_DIR="/var/backups/gerente"
DATE=$(date +%Y%m%d_%H%M%S)
DB_NAME="Projetos"
DB_USER="admin"

# Criar diretório de backup
mkdir -p $BACKUP_DIR

# Backup do banco de dados
pg_dump -U $DB_USER -h localhost $DB_NAME > $BACKUP_DIR/gerente_db_$DATE.sql

# Backup dos arquivos da aplicação
tar -czf $BACKUP_DIR/gerente_app_$DATE.tar.gz /var/www/gerente/publish

# Manter apenas os últimos 7 backups
find $BACKUP_DIR -name "gerente_*" -mtime +7 -delete

# Tornar executável
sudo chmod +x /usr/local/bin/backup-gerente.sh

# Adicionar ao crontab
sudo crontab -e
# Adicionar linha: 0 2 * * * /usr/local/bin/backup-gerente.sh
```

### 12. Comandos Úteis para Manutenção
```bash
# Reiniciar aplicação
sudo systemctl restart gerente

# Verificar status dos serviços
sudo systemctl status gerente nginx postgresql

# Atualizar aplicação
cd /var/www/gerente
git pull
dotnet publish -c Release -o ./publish
sudo systemctl restart gerente

# Verificar uso de recursos
htop
df -h
free -h

# Monitorar conexões
netstat -tulpn | grep :80
netstat -tulpn | grep :5000
```

## 🔐 Funcionalidades de Segurança

### Autenticação
- Hash de senhas usando BCrypt
- Validação de entrada
- Proteção contra campos nulos
- Redirecionamento seguro

### Sistema de Perfis de Acesso
- **Controle Granular**: Permissões específicas por recurso
- **Filtros de Autorização**: Proteção de rotas baseada em perfil
- **Menu Dinâmico**: Interface adaptada às permissões
- **Validação em Tempo Real**: Verificação de acesso em cada requisição
- **Perfis Padrão Protegidos**: Administrador e Usuário não podem ser excluídos
- **Parâmetros do Sistema**: Controle de acesso aos parâmetros do sistema

### Interface Responsiva
- **Design Mobile-First**: Abordagem responsiva moderna
- **Sidebar Adaptativa**: Menu lateral com toggle para mobile
- **Formulários Touch-Friendly**: Campos otimizados para dispositivos móveis
- **Breakpoints Inteligentes**: Adaptação automática para diferentes telas
- **Acessibilidade**: Suporte a navegação por teclado e screen readers

### Redefinição de Senha
- Tokens únicos e seguros
- Expiração automática (24 horas)
- Validação de força da senha
- Confirmação obrigatória

### Configuração de Email
- Validação de configurações SMTP
- Teste de conectividade
- Armazenamento seguro de credenciais

## 🔐 Sistema de Perfis de Acesso

### Perfis Disponíveis

#### Administrador
- **Acesso Total**: Todas as funcionalidades do sistema
- **Configurações**: Gerenciamento de perfis e configurações
- **Usuários**: Cadastro e edição de usuários
- **Projetos**: Acesso completo a projetos
- **Relatórios**: Visualização de relatórios
- **Parâmetros do Sistema**: Configuração de cabeçalho, versão e rodapé

#### Usuário
- **Acesso Limitado**: Funcionalidades básicas
- **Projetos**: Visualização e edição de projetos
- **Relatórios**: Visualização de relatórios
- **Sem Configurações**: Não acessa menu de configurações

### Gerenciamento de Perfis
1. Acesse **Configurações > Perfis de Acesso**
2. Visualize perfis existentes
3. Crie novos perfis personalizados
4. Edite permissões conforme necessário
5. Associe perfis aos usuários

### Gerenciamento de Parâmetros do Sistema
1. Acesse **Configurações > Parâmetro do Sistema**
2. Configure o cabeçalho do sistema
3. Defina a versão do sistema
4. Personalize o nome do rodapé
5. Salve as alterações (aplicadas automaticamente)

### Associação de Usuários
1. Acesse **Configurações > Cadastro de Usuários**
2. Crie ou edite usuário
3. Selecione o perfil de acesso
4. Salve as alterações

## 📧 Processo de Redefinição de Senha

### 1. Solicitação de Reset
1. Acesse `/Login`
2. Clique em "Esqueci minha senha"
3. Digite seu email
4. Clique em "Enviar"

### 2. Recebimento do Email
- Verifique sua caixa de entrada
- Clique no link recebido
- O link expira em 24 horas

### 3. Definição da Nova Senha
1. Digite a nova senha (mínimo 6 caracteres)
2. Confirme a senha
3. Clique em "Redefinir Senha"
4. Faça login com a nova senha

## 🧪 Testes

### Teste de Login
1. Acesse `http://localhost:5144/Login` (desenvolvimento)
2. Use credenciais válidas
3. Verifique redirecionamento para `/Home`

### Teste em Produção
1. Acesse `https://seu_dominio.com/Login` (produção)
2. Verifique se SSL está funcionando
3. Teste todas as funcionalidades

### Teste de Reset de Senha
1. Acesse `http://localhost:5144/Login`
2. Clique em "Esqueci minha senha"
3. Digite um email válido
4. Verifique recebimento do email
5. Teste o link de reset

### Teste de Parâmetros do Sistema
1. Acesse `http://localhost:5144/ParametroSistema`
2. Configure os parâmetros do sistema
3. Verifique se as alterações são aplicadas automaticamente
4. Teste a personalização do cabeçalho e rodapé

### Teste de Responsividade
1. **Desktop**: Verifique o layout completo com sidebar fixa
2. **Tablet**: Redimensione para 768px-1023px e teste a sidebar
3. **Mobile**: Redimensione para <768px e teste o toggle da sidebar
4. **Touch**: Teste os formulários e botões em dispositivos touch
5. **Acessibilidade**: Teste navegação por teclado (Tab, Enter, Space)

### Teste de Performance em Produção
1. **Load Testing**: Use ferramentas como Apache Bench ou JMeter
2. **Monitoramento**: Verifique logs e métricas do servidor
3. **Backup**: Teste restauração do banco de dados
4. **SSL**: Verifique certificado e renovação automática

### Teste de Configuração de Email
1. Faça login no sistema
2. Acesse `/ConfiguracaoEmail`
3. Configure SMTP
4. Teste a conexão

## 📋 Menu de Configurações

### Ordem dos Itens
O menu "Configurações" está organizado na seguinte ordem:
1. **Cadastro de Usuários** - Gerenciamento de usuários do sistema
2. **Perfis de Acesso** - Controle de permissões e perfis
3. **E-mail** - Configuração de SMTP e envio de emails
4. **Parâmetro do Sistema** - Personalização de cabeçalho, versão e rodapé

### Controle de Acesso
- Cada item do menu só é exibido se o usuário tiver a permissão correspondente
- O acesso é controlado pelo sistema de perfis de acesso
- Apenas usuários com perfil "Administrador" têm acesso completo ao menu

### Responsividade do Menu
- **Desktop**: Menu dropdown completo na sidebar
- **Tablet**: Menu dropdown adaptado para telas médias
- **Mobile**: Menu dropdown otimizado para touch, com toggle da sidebar
- **Acessibilidade**: Suporte a navegação por teclado e screen readers

### Teste de Perfis de Acesso
1. Faça login como administrador
2. Acesse **Configurações > Perfis de Acesso**
3. Verifique perfis Administrador e Usuário
4. Crie um novo perfil personalizado
5. Teste a associação de perfil a usuário

### Teste de Controle de Acesso
1. Faça login com usuário de perfil "Usuário"
2. Verifique que o menu Configurações não aparece
3. Tente acessar diretamente `/PerfilAcesso` (deve redirecionar)
4. Faça login como administrador e verifique acesso total

## 🔧 Troubleshooting

### Problemas Comuns

#### 1. Erro de Conexão com Banco
```
- Verificar se PostgreSQL está rodando
- Confirmar connection string em appsettings.json
- Verificar credenciais do usuário admin
```

#### 2. Email não Enviado
```
- Verificar configuração SMTP em /ConfiguracaoEmail
- Confirmar credenciais de email
- Verificar firewall/antivírus
```

#### 3. Link de Reset Inválido
```
- Verificar se não expirou (24h)
- Confirmar se token está correto na URL
- Verificar se email foi configurado corretamente
```

#### 4. Erro de Porta
```
- Aplicação roda na porta 5144 por padrão
- Verificar se porta não está em uso
- Usar HTTP, não HTTPS (em desenvolvimento)
```

#### 5. Acesso Negado
```
- Verificar se usuário tem perfil associado
- Confirmar permissões do perfil
- Verificar se perfil está ativo
- Usar usuário admin@dorowcamp.com para acesso total
```

#### 6. Problemas de Responsividade
```
- Verificar se viewport meta tag está presente
- Confirmar se CSS responsivo está carregado
- Testar em diferentes dispositivos/browsers
- Verificar se JavaScript responsivo está funcionando
- Testar navegação por teclado
```

#### 7. Problemas de Deploy em Produção
```
- Verificar se .NET 8.0 está instalado: dotnet --version
- Confirmar se PostgreSQL está rodando: sudo systemctl status postgresql
- Verificar logs da aplicação: sudo journalctl -u gerente -f
- Testar conectividade do banco: psql -U admin -d Projetos -h localhost
- Verificar configuração do Nginx: sudo nginx -t
- Confirmar se firewall está configurado: sudo ufw status
- Verificar certificado SSL: sudo certbot certificates
```

### Logs
- Logs da aplicação em `bin/Debug/net8.0/`
- Logs do PostgreSQL em `/var/log/postgresql/` (Linux) ou `C:\Program Files\PostgreSQL\data\log\` (Windows)

## 🚀 Como Usar o Sistema de Perfis

### Primeiro Acesso
1. **Execute os scripts SQL**:
   ```bash
   psql -U admin -d Projetos -f database_setup.sql
   psql -U admin -d Projetos -f setup_perfis_simples.sql
   ```

2. **Faça login como administrador**:
   - Email: `admin@dorowcamp.com`
   - Senha: (sua senha atual)

3. **Verifique os perfis criados**:
   - Acesse **Configurações > Perfis de Acesso**
   - Confirme que existem os perfis "Administrador" e "Usuário"

## 📱 Como Usar a Interface Responsiva

### Testando em Diferentes Dispositivos
1. **Desktop (>1024px)**:
   - Sidebar fixa à esquerda
   - Menu dropdown completo
   - Layout otimizado para mouse

2. **Tablet (768px-1023px)**:
   - Sidebar reduzida (220px)
   - Menu adaptado para touch
   - Conteúdo responsivo

3. **Mobile (<768px)**:
   - Sidebar oculta com toggle
   - Menu otimizado para touch
   - Formulários touch-friendly

### Navegação por Teclado
- **Tab**: Navegar entre elementos
- **Enter/Space**: Ativar botões e links
- **Escape**: Fechar modais e dropdowns
- **Setas**: Navegar em menus dropdown

### Criando Novos Perfis
1. Acesse **Configurações > Perfis de Acesso**
2. Clique em **"Novo Perfil"**
3. Configure as permissões:
   - **Acesso Total**: Concede todas as permissões
   - **Configurações**: Menu de configurações do sistema
   - **Usuários**: Gerenciamento de usuários
   - **Projetos**: Criação e gestão de projetos
   - **Relatórios**: Visualização de relatórios

### Associando Perfis a Usuários
1. Acesse **Configurações > Cadastro de Usuários**
2. Crie um novo usuário ou edite existente
3. Selecione o **Perfil de Acesso** desejado
4. Salve as alterações

### Testando o Controle de Acesso
1. **Usuário Administrador**:
   - Vê todos os menus
   - Acesso total ao sistema
   - Pode gerenciar perfis e usuários

2. **Usuário Padrão**:
   - Não vê menu "Configurações"
   - Acesso limitado a projetos e relatórios
   - Redirecionado para "Acesso Negado" se tentar acessar áreas restritas

## 📝 Histórico de Alterações

### Versão 1.4.0 - Deploy em Produção
- ✅ Documentação completa para deploy em Ubuntu
- ✅ Configuração de Nginx como proxy reverso
- ✅ Sistema de serviço com Systemd
- ✅ Configuração de SSL com Let's Encrypt
- ✅ Script de backup automático
- ✅ Configuração de firewall (UFW)
- ✅ Monitoramento e logs centralizados
- ✅ Comandos de manutenção documentados

### Versão 1.3.0 - Interface Responsiva
- ✅ Design mobile-first implementado
- ✅ Sidebar responsiva com toggle para mobile
- ✅ Formulários otimizados para dispositivos touch
- ✅ Breakpoints múltiplos (desktop, tablet, mobile)
- ✅ Acessibilidade melhorada (teclado e screen readers)
- ✅ CSS Grid e Flexbox para layouts adaptativos
- ✅ Media queries otimizadas
- ✅ JavaScript responsivo para melhor UX

### Versão 1.2.0 - Parâmetros do Sistema
- ✅ Sistema de parâmetros do sistema completo
- ✅ Personalização de cabeçalho, versão e rodapé
- ✅ Aplicação automática em todo o sistema
- ✅ Controle de acesso aos parâmetros
- ✅ Interface moderna para configuração
- ✅ BaseController para carregamento automático
- ✅ Menu reordenado conforme especificação
- ✅ Validação e persistência no banco de dados

### Versão 1.1.0 - Sistema de Perfis de Acesso
- ✅ Sistema de perfis de acesso completo
- ✅ Perfis padrão: Administrador e Usuário
- ✅ Controle granular de permissões
- ✅ Filtros de autorização por rota
- ✅ Menu dinâmico baseado em perfil
- ✅ Gerenciamento de usuários com perfil
- ✅ Interface para criação e edição de perfis
- ✅ Proteção de perfis padrão
- ✅ Página de acesso negado
- ✅ Validação de permissões em tempo real

### Versão 1.0.0
- ✅ Sistema de autenticação básico
- ✅ Redefinição de senha com tokens
- ✅ Configuração de email SMTP
- ✅ Interface responsiva com Bootstrap
- ✅ Validação de formulários
- ✅ Hash seguro de senhas
- ✅ Proteção contra campos nulos
- ✅ Correção de bugs de conexão

### Melhorias Implementadas
1. **Segurança**:
   - Hash BCrypt para senhas
   - Tokens únicos para reset
   - Validação de entrada
   - Sistema de perfis de acesso
   - Controle granular de permissões
   - Filtros de autorização
   - Controle de acesso aos parâmetros do sistema

2. **UX/UI**:
   - Interface moderna com Bootstrap 5
   - Validação em tempo real
   - Mensagens de feedback
   - Toggle de visibilidade de senha
   - Menu dinâmico baseado em perfil
   - Interface intuitiva para gerenciamento de perfis
   - Personalização de cabeçalho e rodapé
   - Menu reordenado conforme especificação
   - Design mobile-first responsivo
   - Sidebar adaptativa com toggle
   - Formulários touch-friendly
   - Acessibilidade aprimorada

3. **Funcionalidades**:
   - Gerenciamento completo de usuários
   - Sistema de perfis personalizáveis
   - Controle de acesso por recurso
   - Proteção de rotas
   - Página de acesso negado informativa
   - Sistema de parâmetros do sistema
   - Aplicação automática de configurações
   - Sistema completo de reset de senha
   - Configuração flexível de email
   - Teste de conectividade SMTP
   - Interface responsiva multi-dispositivo
   - Navegação adaptativa para mobile
   - Formulários otimizados para touch
   - Suporte a diferentes resoluções
   - Deploy automatizado em servidor Linux
   - Configuração de produção com SSL
   - Sistema de backup automático
   - Monitoramento e logs centralizados

## 🤝 Contribuição
Para contribuir com o projeto:
1. Fork o repositório
2. Crie uma branch para sua feature
3. Commit suas mudanças
4. Push para a branch
5. Abra um Pull Request

## 📄 Licença
Este projeto está sob a licença MIT. Veja o arquivo LICENSE para mais detalhes.

## 📞 Suporte
Para suporte técnico ou dúvidas:
- Abra uma issue no repositório
- Consulte a documentação
- Verifique a seção de troubleshooting

---

**Desenvolvido com ❤️ usando ASP.NET Core**

