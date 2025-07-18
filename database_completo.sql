-- =====================================================
-- SISTEMA GERENTE - SCRIPT COMPLETO DE BANCO DE DADOS
-- =====================================================
-- Este script cria todo o banco de dados do zero
-- Inclui: tabelas, views, functions, triggers e dados
-- Versão: 1.4.0
-- Data: 2025
-- =====================================================

-- Configurações iniciais
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SET check_function_bodies = false;
SET client_min_messages = warning;

-- =====================================================
-- 1. CRIAÇÃO DAS TABELAS
-- =====================================================

-- Tabela de perfis de acesso
CREATE TABLE IF NOT EXISTS perfis_acesso (
    id SERIAL PRIMARY KEY,
    nome VARCHAR(100) NOT NULL UNIQUE,
    descricao TEXT,
    acesso_configuracoes BOOLEAN DEFAULT FALSE,
    acesso_usuarios BOOLEAN DEFAULT FALSE,
    acesso_projetos BOOLEAN DEFAULT FALSE,
    acesso_relatorios BOOLEAN DEFAULT FALSE,
    acesso_parametros_sistema BOOLEAN DEFAULT FALSE,
    acesso_total BOOLEAN DEFAULT FALSE,
    ativo BOOLEAN DEFAULT TRUE,
    data_criacao TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    data_atualizacao TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Tabela de usuários
CREATE TABLE IF NOT EXISTS usuarios (
    id SERIAL PRIMARY KEY,
    nome VARCHAR(200) NOT NULL,
    email VARCHAR(200) NOT NULL UNIQUE,
    senha VARCHAR(255) NOT NULL,
    perfil_acesso_id INTEGER REFERENCES perfis_acesso(id),
    ativo BOOLEAN DEFAULT TRUE,
    data_criacao TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    data_atualizacao TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Tabela de configuração de email
CREATE TABLE IF NOT EXISTS configuracao_email (
    id SERIAL PRIMARY KEY,
    servidor_smtp VARCHAR(200) NOT NULL,
    porta INTEGER NOT NULL,
    usuario VARCHAR(200) NOT NULL,
    senha VARCHAR(255) NOT NULL,
    ssl BOOLEAN DEFAULT FALSE,
    tls BOOLEAN DEFAULT FALSE,
    remetente VARCHAR(200) NOT NULL,
    nome_remetente VARCHAR(200) NOT NULL,
    ativo BOOLEAN DEFAULT TRUE,
    data_criacao TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    data_atualizacao TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Tabela de tokens de reset de senha
CREATE TABLE IF NOT EXISTS password_reset_tokens (
    id SERIAL PRIMARY KEY,
    email VARCHAR(200) NOT NULL,
    token VARCHAR(255) NOT NULL UNIQUE,
    expiracao TIMESTAMP NOT NULL,
    usado BOOLEAN DEFAULT FALSE,
    data_criacao TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Tabela de parâmetros do sistema
CREATE TABLE IF NOT EXISTS parametros_sistema (
    id SERIAL PRIMARY KEY,
    cabecalho_sistema VARCHAR(200) NOT NULL,
    versao_sistema VARCHAR(50) NOT NULL,
    nome_rodape VARCHAR(200) NOT NULL,
    data_criacao TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    data_atualizacao TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- =====================================================
-- 2. CRIAÇÃO DOS ÍNDICES
-- =====================================================

-- Índices para performance
CREATE INDEX IF NOT EXISTS idx_usuarios_email ON usuarios(email);
CREATE INDEX IF NOT EXISTS idx_usuarios_perfil_acesso_id ON usuarios(perfil_acesso_id);
CREATE INDEX IF NOT EXISTS idx_usuarios_ativo ON usuarios(ativo);
CREATE INDEX IF NOT EXISTS idx_password_reset_tokens_email ON password_reset_tokens(email);
CREATE INDEX IF NOT EXISTS idx_password_reset_tokens_token ON password_reset_tokens(token);
CREATE INDEX IF NOT EXISTS idx_password_reset_tokens_expiracao ON password_reset_tokens(expiracao);
CREATE INDEX IF NOT EXISTS idx_perfis_acesso_nome ON perfis_acesso(nome);
CREATE INDEX IF NOT EXISTS idx_perfis_acesso_ativo ON perfis_acesso(ativo);

-- =====================================================
-- 3. CRIAÇÃO DAS FUNCTIONS
-- =====================================================

-- Function para atualizar data_atualizacao automaticamente
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.data_atualizacao = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Function para atualizar data_atualizacao de parametros_sistema
CREATE OR REPLACE FUNCTION update_parametros_sistema_timestamp()
RETURNS TRIGGER AS $$
BEGIN
    NEW.data_atualizacao = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Function para limpar tokens expirados
CREATE OR REPLACE FUNCTION limpar_tokens_expirados()
RETURNS void AS $$
BEGIN
    DELETE FROM password_reset_tokens 
    WHERE expiracao < CURRENT_TIMESTAMP OR usado = TRUE;
END;
$$ LANGUAGE plpgsql;

-- Function para gerar hash SHA256
CREATE OR REPLACE FUNCTION gerar_hash_senha(senha_texto TEXT)
RETURNS TEXT AS $$
BEGIN
    RETURN encode(sha256(senha_texto::bytea), 'base64');
END;
$$ LANGUAGE plpgsql;

-- Function para verificar se email existe
CREATE OR REPLACE FUNCTION email_existe(email_verificar VARCHAR(200))
RETURNS BOOLEAN AS $$
DECLARE
    existe BOOLEAN;
BEGIN
    SELECT EXISTS(SELECT 1 FROM usuarios WHERE email = email_verificar) INTO existe;
    RETURN existe;
END;
$$ LANGUAGE plpgsql;

-- Function para obter perfil de acesso do usuário
CREATE OR REPLACE FUNCTION obter_perfil_usuario(user_id INTEGER)
RETURNS TABLE(
    perfil_id INTEGER,
    perfil_nome VARCHAR(100),
    acesso_config BOOLEAN,
    acesso_users BOOLEAN,
    acesso_proj BOOLEAN,
    acesso_rel BOOLEAN,
    acesso_param BOOLEAN,
    acesso_total BOOLEAN
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        pa.id,
        pa.nome,
        pa.acesso_configuracoes,
        pa.acesso_usuarios,
        pa.acesso_projetos,
        pa.acesso_relatorios,
        pa.acesso_parametros_sistema,
        pa.acesso_total
    FROM perfis_acesso pa
    INNER JOIN usuarios u ON u.perfil_acesso_id = pa.id
    WHERE u.id = user_id AND u.ativo = TRUE AND pa.ativo = TRUE;
END;
$$ LANGUAGE plpgsql;

-- =====================================================
-- 4. CRIAÇÃO DOS TRIGGERS
-- =====================================================

-- Trigger para atualizar data_atualizacao em perfis_acesso
DROP TRIGGER IF EXISTS trigger_update_perfis_acesso_timestamp ON perfis_acesso;
CREATE TRIGGER trigger_update_perfis_acesso_timestamp
    BEFORE UPDATE ON perfis_acesso
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- Trigger para atualizar data_atualizacao em usuarios
DROP TRIGGER IF EXISTS trigger_update_usuarios_timestamp ON usuarios;
CREATE TRIGGER trigger_update_usuarios_timestamp
    BEFORE UPDATE ON usuarios
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- Trigger para atualizar data_atualizacao em configuracao_email
DROP TRIGGER IF EXISTS trigger_update_configuracao_email_timestamp ON configuracao_email;
CREATE TRIGGER trigger_update_configuracao_email_timestamp
    BEFORE UPDATE ON configuracao_email
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- Trigger para atualizar data_atualizacao em parametros_sistema
DROP TRIGGER IF EXISTS trigger_update_parametros_sistema_timestamp ON parametros_sistema;
CREATE TRIGGER trigger_update_parametros_sistema_timestamp
    BEFORE UPDATE ON parametros_sistema
    FOR EACH ROW
    EXECUTE FUNCTION update_parametros_sistema_timestamp();

-- =====================================================
-- 5. CRIAÇÃO DAS VIEWS
-- =====================================================

-- View para listar usuários com informações do perfil
CREATE OR REPLACE VIEW vw_usuarios_com_perfil AS
SELECT 
    u.id,
    u.nome,
    u.email,
    u.ativo,
    u.data_criacao,
    u.data_atualizacao,
    pa.id as perfil_id,
    pa.nome as perfil_nome,
    pa.acesso_configuracoes,
    pa.acesso_usuarios,
    pa.acesso_projetos,
    pa.acesso_relatorios,
    pa.acesso_parametros_sistema,
    pa.acesso_total
FROM usuarios u
LEFT JOIN perfis_acesso pa ON u.perfil_acesso_id = pa.id
WHERE u.ativo = TRUE;

-- View para estatísticas do sistema
CREATE OR REPLACE VIEW vw_estatisticas_sistema AS
SELECT 
    (SELECT COUNT(*) FROM usuarios WHERE ativo = TRUE) as total_usuarios_ativos,
    (SELECT COUNT(*) FROM perfis_acesso WHERE ativo = TRUE) as total_perfis_ativos,
    (SELECT COUNT(*) FROM usuarios WHERE perfil_acesso_id = (SELECT id FROM perfis_acesso WHERE nome = 'Administrador')) as total_administradores,
    (SELECT COUNT(*) FROM password_reset_tokens WHERE expiracao > CURRENT_TIMESTAMP AND usado = FALSE) as tokens_ativos,
    (SELECT COUNT(*) FROM configuracao_email WHERE ativo = TRUE) as configs_email_ativas;

-- View para tokens de reset válidos
CREATE OR REPLACE VIEW vw_tokens_validos AS
SELECT 
    id,
    email,
    token,
    expiracao,
    usado,
    data_criacao
FROM password_reset_tokens
WHERE expiracao > CURRENT_TIMESTAMP AND usado = FALSE;

-- =====================================================
-- 6. INSERÇÃO DOS DADOS INICIAIS
-- =====================================================

-- Inserir perfis de acesso padrão
INSERT INTO perfis_acesso (nome, descricao, acesso_configuracoes, acesso_usuarios, acesso_projetos, acesso_relatorios, acesso_parametros_sistema, acesso_total) VALUES
('Administrador', 'Perfil com acesso total ao sistema', TRUE, TRUE, TRUE, TRUE, TRUE, TRUE),
('Usuário', 'Perfil com acesso limitado ao sistema', FALSE, FALSE, TRUE, TRUE, FALSE, FALSE)
ON CONFLICT (nome) DO NOTHING;

-- Inserir usuário administrador padrão
-- Senha: admin (hash SHA256)
INSERT INTO usuarios (nome, email, senha, perfil_acesso_id) VALUES
('Administrador', 'admin@dorowcamp.com', 'jGl25bVBBBW96Qi9Te4V37Fnqchz/Eu4qB9vKrRIqRg=', 
 (SELECT id FROM perfis_acesso WHERE nome = 'Administrador'))
ON CONFLICT (email) DO NOTHING;

-- Inserir usuário admin@sistema.com
INSERT INTO usuarios (nome, email, senha, perfil_acesso_id) VALUES
('Administrador Sistema', 'admin@sistema.com', 'jGl25bVBBBW96Qi9Te4V37Fnqchz/Eu4qB9vKrRIqRg=', 
 (SELECT id FROM perfis_acesso WHERE nome = 'Administrador'))
ON CONFLICT (email) DO NOTHING;

-- Inserir configuração de email padrão
INSERT INTO configuracao_email (servidor_smtp, porta, usuario, senha, ssl, tls, remetente, nome_remetente) VALUES
('smtp.gmail.com', 587, 'seu-email@gmail.com', 'sua-senha-app', TRUE, TRUE, 'seu-email@gmail.com', 'Sistema Gerente')
ON CONFLICT DO NOTHING;

-- Inserir parâmetros do sistema padrão
INSERT INTO parametros_sistema (cabecalho_sistema, versao_sistema, nome_rodapé) VALUES
('DorowCamp', '1.4.0', 'Sistema DorowCamp 2025©')
ON CONFLICT DO NOTHING;

-- =====================================================
-- 7. CONFIGURAÇÕES DE SEGURANÇA
-- =====================================================

-- Criar usuário específico para a aplicação (opcional)
-- CREATE USER gerente_app WITH PASSWORD 'senha_segura_app';
-- GRANT CONNECT ON DATABASE Projetos TO gerente_app;
-- GRANT USAGE ON SCHEMA public TO gerente_app;
-- GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO gerente_app;
-- GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO gerente_app;

-- =====================================================
-- 8. PROCEDURES PARA MANUTENÇÃO
-- =====================================================

-- Procedure para limpeza automática de tokens expirados
CREATE OR REPLACE PROCEDURE limpeza_automatica_tokens()
LANGUAGE plpgsql
AS $$
BEGIN
    -- Deletar tokens expirados
    DELETE FROM password_reset_tokens 
    WHERE expiracao < CURRENT_TIMESTAMP OR usado = TRUE;
    
    -- Log da limpeza
    RAISE NOTICE 'Limpeza de tokens expirados concluída em %', CURRENT_TIMESTAMP;
END;
$$;

-- Procedure para backup de dados críticos
CREATE OR REPLACE PROCEDURE backup_dados_criticos()
LANGUAGE plpgsql
AS $$
BEGIN
    -- Aqui você pode adicionar lógica para backup
    -- Por exemplo, criar tabelas de backup ou exportar dados
    RAISE NOTICE 'Backup de dados críticos iniciado em %', CURRENT_TIMESTAMP;
END;
$$;

-- =====================================================
-- 9. FUNCTIONS DE UTILIDADE
-- =====================================================

-- Function para verificar integridade do banco
CREATE OR REPLACE FUNCTION verificar_integridade_banco()
RETURNS TABLE(
    tabela VARCHAR(100),
    registros INTEGER,
    status VARCHAR(50)
) AS $$
BEGIN
    RETURN QUERY
    SELECT 'perfis_acesso'::VARCHAR, COUNT(*), 
           CASE WHEN COUNT(*) > 0 THEN 'OK' ELSE 'VAZIO' END
    FROM perfis_acesso
    UNION ALL
    SELECT 'usuarios'::VARCHAR, COUNT(*), 
           CASE WHEN COUNT(*) > 0 THEN 'OK' ELSE 'VAZIO' END
    FROM usuarios
    UNION ALL
    SELECT 'configuracao_email'::VARCHAR, COUNT(*), 
           CASE WHEN COUNT(*) > 0 THEN 'OK' ELSE 'VAZIO' END
    FROM configuracao_email
    UNION ALL
    SELECT 'parametros_sistema'::VARCHAR, COUNT(*), 
           CASE WHEN COUNT(*) > 0 THEN 'OK' ELSE 'VAZIO' END
    FROM parametros_sistema;
END;
$$ LANGUAGE plpgsql;

-- Function para obter informações do sistema
CREATE OR REPLACE FUNCTION obter_info_sistema()
RETURNS TABLE(
    versao VARCHAR(50),
    total_usuarios INTEGER,
    total_perfis INTEGER,
    ultima_atualizacao TIMESTAMP
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        ps.versao_sistema,
        (SELECT COUNT(*) FROM usuarios WHERE ativo = TRUE),
        (SELECT COUNT(*) FROM perfis_acesso WHERE ativo = TRUE),
        ps.data_atualizacao
    FROM parametros_sistema ps
    LIMIT 1;
END;
$$ LANGUAGE plpgsql;

-- =====================================================
-- 10. CONFIGURAÇÕES FINAIS
-- =====================================================

-- Configurar timezone
SET timezone = 'America/Sao_Paulo';

-- Configurar locale
SET lc_messages = 'pt_BR.UTF-8';
SET lc_monetary = 'pt_BR.UTF-8';
SET lc_numeric = 'pt_BR.UTF-8';
SET lc_time = 'pt_BR.UTF-8';

-- =====================================================
-- 11. VERIFICAÇÕES FINAIS
-- =====================================================

-- Verificar se todas as tabelas foram criadas
DO $$
DECLARE
    tabela_count INTEGER;
BEGIN
    SELECT COUNT(*) INTO tabela_count
    FROM information_schema.tables 
    WHERE table_schema = 'public' 
    AND table_name IN ('perfis_acesso', 'usuarios', 'configuracao_email', 'password_reset_tokens', 'parametros_sistema');
    
    IF tabela_count = 5 THEN
        RAISE NOTICE '✅ Todas as tabelas foram criadas com sucesso!';
    ELSE
        RAISE NOTICE '⚠️  Apenas % tabelas foram criadas. Verificar script.', tabela_count;
    END IF;
END $$;

-- Verificar se os dados iniciais foram inseridos
DO $$
DECLARE
    admin_count INTEGER;
    perfis_count INTEGER;
BEGIN
    SELECT COUNT(*) INTO admin_count FROM usuarios WHERE email = 'admin@dorowcamp.com';
    SELECT COUNT(*) INTO perfis_count FROM perfis_acesso WHERE nome IN ('Administrador', 'Usuário');
    
    IF admin_count > 0 AND perfis_count = 2 THEN
        RAISE NOTICE '✅ Dados iniciais inseridos com sucesso!';
        RAISE NOTICE '📧 Email admin: admin@dorowcamp.com';
        RAISE NOTICE '🔑 Senha admin: admin';
    ELSE
        RAISE NOTICE '⚠️  Verificar inserção dos dados iniciais.';
    END IF;
END $$;

-- =====================================================
-- 12. COMANDOS DE VERIFICAÇÃO
-- =====================================================

-- Para verificar a estrutura criada, execute:
-- \dt  -- Listar tabelas
-- \df  -- Listar functions
-- \dv  -- Listar views
-- \dT  -- Listar triggers

-- Para verificar os dados, execute:
-- SELECT * FROM vw_usuarios_com_perfil;
-- SELECT * FROM vw_estatisticas_sistema;
-- SELECT * FROM verificar_integridade_banco();
-- SELECT * FROM obter_info_sistema();

-- =====================================================
-- FIM DO SCRIPT
-- =====================================================
-- Script criado com sucesso!
-- Execute este arquivo para criar o banco completo do zero.
-- ===================================================== 