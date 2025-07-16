# Sistema DorowCamp

## Visão Geral
Sistema web para gerenciamento de projetos, usuários e configurações de e-mail, desenvolvido em ASP.NET Core 8, com banco de dados PostgreSQL.

---

## Requisitos

- .NET 8 SDK
- Docker e Docker Compose
- PostgreSQL 15+
- (Opcional) Editor de código como VS Code ou Visual Studio

---

## Estrutura do Projeto
- **Controllers/**: Lógica de controle das rotas e autenticação
- **Models/**: Modelos de dados
- **Views/**: Páginas Razor (interface)
- **wwwroot/**: Arquivos estáticos (CSS, JS)
- **database_setup.sql**: Script para criação do banco de dados e tabelas
- **appsettings.json**: Configurações da aplicação

---

## Banco de Dados

O sistema utiliza PostgreSQL. A estrutura básica é:

```sql
CREATE DATABASE "Projetos";

CREATE TABLE usuarios (
    id SERIAL PRIMARY KEY,
    username VARCHAR(100) NOT NULL UNIQUE,
    senha VARCHAR(100) NOT NULL
);

INSERT INTO usuarios (username, senha) VALUES ('admin', 'admin');

CREATE TABLE IF NOT EXISTS configuracoes_email (
    id SERIAL PRIMARY KEY,
    servidor_smtp VARCHAR(255) NOT NULL,
    porta INTEGER NOT NULL,
    email_remetente VARCHAR(255) NOT NULL,
    nome_remetente VARCHAR(255) NOT NULL,
    usuario_smtp VARCHAR(255) NOT NULL,
    senha_smtp VARCHAR(255) NOT NULL,
    data_criacao TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    data_atualizacao TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

INSERT INTO configuracoes_email (servidor_smtp, porta, email_remetente, nome_remetente, usuario_smtp, senha_smtp) 
VALUES ('smtp.gmail.com', 587, 'sistema@empresa.com', 'Sistema DorowCamp', 'sistema@empresa.com', 'senha123')
ON CONFLICT DO NOTHING;
```

---

## Docker Compose

Crie um arquivo `docker-compose.yml` na raiz do projeto com o seguinte conteúdo:

```yaml
version: '3.8'
services:
  db:
    image: postgres:15
    container_name: dorowcamp_db
    environment:
      POSTGRES_DB: Projetos
      POSTGRES_USER: admin
      POSTGRES_PASSWORD: admin
    ports:
      - "5432:5432"
    volumes:
      - ./database_setup.sql:/docker-entrypoint-initdb.d/database_setup.sql:ro

  app:
    build: .
    container_name: dorowcamp_app
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__DefaultConnection=Host=db;Port=5432;Database=Projetos;Username=admin;Password=admin
    ports:
      - "8080:80"
    depends_on:
      - db
```

> **Nota:** Certifique-se de que o Dockerfile está configurado para build de projetos ASP.NET Core. Se não existir, solicite a criação.

---

## Como Executar com Docker Compose

1. Instale Docker e Docker Compose.
2. Execute:
   ```sh
   docker-compose up --build
   ```
3. Acesse o sistema em [http://localhost:8080](http://localhost:8080)

---

## Como Executar Localmente (sem Docker)

1. Instale o .NET 8 SDK e PostgreSQL.
2. Crie o banco de dados e tabelas usando o script `database_setup.sql`.
3. Ajuste a string de conexão em `appsettings.json` se necessário.
4. Execute:
   ```sh
   dotnet build
   dotnet run
   ```
5. Acesse o sistema em [http://localhost:5144](http://localhost:5144) ou conforme indicado no terminal.

---

## Usuário Inicial
- Usuário: `admin`
- Senha: `admin`

---

## Observações
- Altere as senhas padrão antes de usar em produção.
- Para configurações de e-mail, acesse o menu de configurações no sistema.
- O sistema utiliza Npgsql para integração com PostgreSQL.

---

## Melhorias na Configuração de E-mail/SMTP

- Nova tela de configurações SMTP na área de Configurações > E-mail, mantendo o layout existente.
- Inclusão do campo de segurança (SSL/TLS/Nenhuma) para conexão SMTP.
- Senha SMTP agora é armazenada criptografada no banco de dados (AES, sem dependências externas).
- Botão "Testar Conexão" permite validar as credenciais e a comunicação com o servidor SMTP, exibindo feedback visual de sucesso ou erro detalhado.
- Todos os campos são validados no backend antes do salvamento e do teste de conexão.
- Proteção contra CSRF implementada em todos os formulários de configuração.
- Todas as demais funcionalidades existentes foram mantidas intactas.

