-- Script para criar a tabela de configurações de e-mail
-- Execute este script no seu banco de dados PostgreSQL

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

-- Inserir configurações padrão (exemplo)
INSERT INTO configuracoes_email (servidor_smtp, porta, email_remetente, nome_remetente, usuario_smtp, senha_smtp) 
VALUES ('smtp.gmail.com', 587, 'sistema@empresa.com', 'Sistema Gerente', 'sistema@empresa.com', 'senha123')
ON CONFLICT DO NOTHING; 