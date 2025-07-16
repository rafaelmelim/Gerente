-- Script: 01_create_database.sql
-- Descrição: Cria o banco de dados principal para a aplicação de Projetos
CREATE DATABASE "Projetos";

-- Script: 02_create_users_table.sql
-- Descrição: Cria a tabela de usuários do sistema e insere o usuário admin padrão
CREATE TABLE usuarios (
    id SERIAL PRIMARY KEY,
    username VARCHAR(100) NOT NULL UNIQUE,
    senha VARCHAR(100) NOT NULL
);

-- Inserção do usuário administrador padrão
INSERT INTO usuarios (username, senha) VALUES ('admin', 'admin');

-- Script: 03_create_email_settings.sql
-- Descrição: Cria a tabela de configurações de e-mail para envio de notificações
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

-- Inserção de configurações padrão de e-mail
-- NOTA: A senha deve ser alterada para um valor seguro em produção
INSERT INTO configuracoes_email (servidor_smtp, porta, email_remetente, nome_remetente, usuario_smtp, senha_smtp) 
VALUES ('smtp.gmail.com', 587, 'sistema@empresa.com', 'Sistema Gerente', 'sistema@empresa.com', 'senha123')
ON CONFLICT DO NOTHING;

-- Script: 04_create_application_user.sql
-- Descrição: Cria o usuário de conexão da aplicação com o banco de dados
-- ATENÇÃO: Em ambientes de produção, utilize uma senha mais segura
CREATE USER admin WITH PASSWORD 'admin';

-- Concede privilégios elevados (apenas para desenvolvimento)
-- NOTA: Remova esta linha em ambientes de produção ou ajuste as permissões conforme necessário
ALTER USER admin WITH SUPERUSER;

ALTER TABLE configuracoes_email ADD COLUMN IF NOT EXISTS security_mode VARCHAR(10) DEFAULT 'None';