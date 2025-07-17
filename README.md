# Sistema Gerente - Sistema de Gerenciamento de Projetos

## 📋 Descrição
Sistema web desenvolvido em ASP.NET Core MVC para gerenciamento de projetos, com funcionalidades de autenticação, redefinição de senha e configuração de email.

## 🚀 Funcionalidades Implementadas

### 1. Autenticação de Usuários
- Login com email e senha
- Validação de credenciais
- Hash seguro de senhas
- Redirecionamento baseado em autenticação

### 2. Redefinição de Senha
- Geração de tokens seguros
- Envio de email com link de redefinição
- Validação de tokens
- Interface para nova senha com validação
- Confirmação de senha em tempo real

### 3. Configuração de Email
- Interface para configuração de SMTP
- Validação de configurações
- Teste de conexão de email

## 🛠️ Tecnologias Utilizadas
- **Backend**: ASP.NET Core 8.0 MVC
- **Banco de Dados**: PostgreSQL
- **Frontend**: HTML, CSS, JavaScript, Bootstrap
- **Email**: SMTP via configuração personalizada

## 📁 Estrutura do Projeto

```
Gerente/
├── Controllers/
│   ├── HomeController.cs          # Página inicial
│   ├── LoginController.cs         # Autenticação e reset de senha
│   └── ConfiguracaoEmailController.cs # Configuração de email
├── Models/
│   ├── ConfiguracaoEmail.cs      # Modelo de configuração de email
│   └── ErrorViewModel.cs          # Modelo de erro
├── Views/
│   ├── Home/                      # Views da página inicial
│   ├── Login/                     # Views de autenticação
│   └── ConfiguracaoEmail/         # Views de configuração
├── wwwroot/                       # Arquivos estáticos
├── Program.cs                     # Configuração da aplicação
├── appsettings.json              # Configurações
└── database_setup.sql            # Script de banco de dados
```

## ⚙️ Configuração e Instalação

### Pré-requisitos
- .NET 8.0 SDK
- PostgreSQL 12+
- Visual Studio 2022 ou VS Code

### 1. Configuração do Banco de Dados

#### 1.1 Instalar PostgreSQL
```bash
# Windows (via installer)
# Baixar e instalar de: https://www.postgresql.org/download/windows/

# Ou via Chocolatey
choco install postgresql
```

#### 1.2 Criar Banco de Dados
```sql
-- Conectar ao PostgreSQL como superusuário
psql -U postgres

-- Criar usuário e banco
CREATE USER admin WITH PASSWORD 'admin';
CREATE DATABASE Projetos OWNER admin;
GRANT ALL PRIVILEGES ON DATABASE Projetos TO admin;
```

#### 1.3 Executar Script de Setup
```bash
# Executar o script completo
psql -U admin -d Projetos -f database_setup.sql
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

## 🔐 Funcionalidades de Segurança

### Autenticação
- Hash de senhas usando BCrypt
- Validação de entrada
- Proteção contra campos nulos
- Redirecionamento seguro

### Redefinição de Senha
- Tokens únicos e seguros
- Expiração automática (24 horas)
- Validação de força da senha
- Confirmação obrigatória

### Configuração de Email
- Validação de configurações SMTP
- Teste de conectividade
- Armazenamento seguro de credenciais

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
1. Acesse `http://localhost:5144/Login`
2. Use credenciais válidas
3. Verifique redirecionamento para `/Home`

### Teste de Reset de Senha
1. Acesse `http://localhost:5144/Login`
2. Clique em "Esqueci minha senha"
3. Digite um email válido
4. Verifique recebimento do email
5. Teste o link de reset

### Teste de Configuração de Email
1. Faça login no sistema
2. Acesse `/ConfiguracaoEmail`
3. Configure SMTP
4. Teste a conexão

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

### Logs
- Logs da aplicação em `bin/Debug/net8.0/`
- Logs do PostgreSQL em `/var/log/postgresql/` (Linux) ou `C:\Program Files\PostgreSQL\data\log\` (Windows)

## 📝 Histórico de Alterações

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

2. **UX/UI**:
   - Interface moderna com Bootstrap
   - Validação em tempo real
   - Mensagens de feedback
   - Toggle de visibilidade de senha

3. **Funcionalidades**:
   - Sistema completo de reset de senha
   - Configuração flexível de email
   - Teste de conectividade SMTP

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

