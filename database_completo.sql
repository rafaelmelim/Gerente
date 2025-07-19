-- =====================================================
-- SISTEMA GERENTE - SCRIPT COMPLETO DE BANCO DE DADOS
-- =====================================================
-- Versão: 1.0.0
-- Data: 2025
-- Descrição: Script completo para criação do banco de dados do Sistema Gerente
-- =====================================================

-- Configurações iniciais
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SET check_function_bodies = false;
SET client_min_messages = warning;

-- =====================================================
-- 1. CRIAÇÃO DAS TABELAS PRINCIPAIS
-- =====================================================

-- Tabela de Perfis de Acesso
CREATE TABLE IF NOT EXISTS perfis_acesso (
    id SERIAL PRIMARY KEY,
    nome VARCHAR(100) NOT NULL UNIQUE,
    descricao TEXT,
    acesso_configuracoes BOOLEAN DEFAULT FALSE,
    acesso_usuarios BOOLEAN DEFAULT FALSE,
    acesso_projetos BOOLEAN DEFAULT FALSE,
    acesso_relatorios BOOLEAN DEFAULT FALSE,
    acesso_backlog_arquitetura BOOLEAN DEFAULT FALSE,
    acesso_parametros_sistema BOOLEAN DEFAULT FALSE,
    ativo BOOLEAN DEFAULT TRUE,
    data_criacao TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    data_atualizacao TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Tabela de Usuários
CREATE TABLE IF NOT EXISTS usuarios (
    id SERIAL PRIMARY KEY,
    nome VARCHAR(200) NOT NULL,
    email VARCHAR(255) NOT NULL UNIQUE,
    senha VARCHAR(255) NOT NULL,
    perfil_acesso_id INTEGER REFERENCES perfis_acesso(id),
    ativo BOOLEAN DEFAULT TRUE,
    data_criacao TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    data_atualizacao TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Tabela de Configuração de Email
CREATE TABLE IF NOT EXISTS configuracoes_email (
    id SERIAL PRIMARY KEY,
    servidor_smtp VARCHAR(255) NOT NULL,
    porta INTEGER NOT NULL,
    usuario VARCHAR(255) NOT NULL,
    senha_criptografada TEXT NOT NULL,
    usar_ssl BOOLEAN DEFAULT TRUE,
    ativo BOOLEAN DEFAULT TRUE,
    data_criacao TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    data_atualizacao TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Tabela de Parâmetros do Sistema
CREATE TABLE IF NOT EXISTS parametros_sistema (
    id SERIAL PRIMARY KEY,
    chave VARCHAR(100) NOT NULL UNIQUE,
    valor TEXT,
    descricao TEXT,
    ativo BOOLEAN DEFAULT TRUE,
    data_criacao TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    data_atualizacao TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Tabela de Tokens de Reset de Senha
CREATE TABLE IF NOT EXISTS password_reset_tokens (
    id SERIAL PRIMARY KEY,
    usuario_id INTEGER REFERENCES usuarios(id) ON DELETE CASCADE,
    token VARCHAR(255) NOT NULL UNIQUE,
    expira_em TIMESTAMP NOT NULL,
    usado BOOLEAN DEFAULT FALSE,
    data_criacao TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Tabela de Backlog de Arquitetura
CREATE TABLE IF NOT EXISTS backlog_arquitetura (
    id SERIAL PRIMARY KEY,
    titulo VARCHAR(255) NOT NULL,
    descricao TEXT,
    ambientes TEXT,
    prioridade VARCHAR(50) DEFAULT 'Média',
    status VARCHAR(50) DEFAULT 'Pendente',
    responsavel_id INTEGER REFERENCES usuarios(id),
    data_estimada DATE,
    data_criacao TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    data_atualizacao TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Tabela de Parâmetros de Ganho
CREATE TABLE IF NOT EXISTS parametros_ganho (
    id SERIAL PRIMARY KEY,
    nome VARCHAR(100) NOT NULL,
    valor DECIMAL(10,2) NOT NULL,
    descricao TEXT,
    ativo BOOLEAN DEFAULT TRUE,
    data_criacao TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    data_atualizacao TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Tabela de Parâmetros de Ambiente
CREATE TABLE IF NOT EXISTS parametros_ambiente (
    id SERIAL PRIMARY KEY,
    nome VARCHAR(100) NOT NULL,
    descricao TEXT,
    ativo BOOLEAN DEFAULT TRUE,
    data_criacao TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    data_atualizacao TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- =====================================================
-- 2. CRIAÇÃO DE ÍNDICES PARA PERFORMANCE
-- =====================================================

-- Índices para tabela usuarios
CREATE INDEX IF NOT EXISTS idx_usuarios_email ON usuarios(email);
CREATE INDEX IF NOT EXISTS idx_usuarios_perfil_acesso ON usuarios(perfil_acesso_id);
CREATE INDEX IF NOT EXISTS idx_usuarios_ativo ON usuarios(ativo);

-- Índices para tabela perfis_acesso
CREATE INDEX IF NOT EXISTS idx_perfis_acesso_nome ON perfis_acesso(nome);
CREATE INDEX IF NOT EXISTS idx_perfis_acesso_ativo ON perfis_acesso(ativo);

-- Índices para tabela password_reset_tokens
CREATE INDEX IF NOT EXISTS idx_password_reset_tokens_token ON password_reset_tokens(token);
CREATE INDEX IF NOT EXISTS idx_password_reset_tokens_usuario ON password_reset_tokens(usuario_id);
CREATE INDEX IF NOT EXISTS idx_password_reset_tokens_expiracao ON password_reset_tokens(expira_em);

-- Índices para tabela backlog_arquitetura
CREATE INDEX IF NOT EXISTS idx_backlog_arquitetura_status ON backlog_arquitetura(status);
CREATE INDEX IF NOT EXISTS idx_backlog_arquitetura_responsavel ON backlog_arquitetura(responsavel_id);
CREATE INDEX IF NOT EXISTS idx_backlog_arquitetura_prioridade ON backlog_arquitetura(prioridade);

-- Índices para tabela parametros_sistema
CREATE INDEX IF NOT EXISTS idx_parametros_sistema_chave ON parametros_sistema(chave);
CREATE INDEX IF NOT EXISTS idx_parametros_sistema_ativo ON parametros_sistema(ativo);

-- =====================================================
-- 3. CRIAÇÃO DE VIEWS
-- =====================================================

-- View de Usuários com Perfil
CREATE OR REPLACE VIEW vw_usuarios_com_perfil AS
SELECT 
    u.id,
    u.nome,
    u.email,
    u.ativo,
    p.nome as perfil_nome,
    p.acesso_configuracoes,
    p.acesso_usuarios,
    p.acesso_projetos,
    p.acesso_relatorios,
    p.acesso_backlog_arquitetura,
    p.acesso_parametros_sistema,
    u.data_criacao,
    u.data_atualizacao
FROM usuarios u
LEFT JOIN perfis_acesso p ON u.perfil_acesso_id = p.id;

-- View de Estatísticas do Sistema
CREATE OR REPLACE VIEW vw_estatisticas_sistema AS
SELECT 
    (SELECT COUNT(*) FROM usuarios WHERE ativo = true) as usuarios_ativos,
    (SELECT COUNT(*) FROM perfis_acesso WHERE ativo = true) as perfis_ativos,
    (SELECT COUNT(*) FROM backlog_arquitetura WHERE status = 'Pendente') as tarefas_pendentes,
    (SELECT COUNT(*) FROM backlog_arquitetura WHERE status = 'Concluído') as tarefas_concluidas,
    (SELECT COUNT(*) FROM parametros_sistema WHERE ativo = true) as parametros_ativos,
    (SELECT COUNT(*) FROM configuracoes_email WHERE ativo = true) as configs_email_ativas;

-- View de Backlog com Responsável
CREATE OR REPLACE VIEW vw_backlog_com_responsavel AS
SELECT 
    b.id,
    b.titulo,
    b.descricao,
    b.ambientes,
    b.prioridade,
    b.status,
    b.data_estimada,
    u.nome as responsavel_nome,
    u.email as responsavel_email,
    b.data_criacao,
    b.data_atualizacao
FROM backlog_arquitetura b
LEFT JOIN usuarios u ON b.responsavel_id = u.id;

-- =====================================================
-- 4. CRIAÇÃO DE FUNCTIONS
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
    SELECT 'usuarios'::VARCHAR, COUNT(*), 
           CASE WHEN COUNT(*) > 0 THEN 'OK' ELSE 'VAZIO' END
    FROM usuarios
    UNION ALL
    SELECT 'perfis_acesso'::VARCHAR, COUNT(*), 
           CASE WHEN COUNT(*) > 0 THEN 'OK' ELSE 'VAZIO' END
    FROM perfis_acesso
    UNION ALL
    SELECT 'parametros_sistema'::VARCHAR, COUNT(*), 
           CASE WHEN COUNT(*) > 0 THEN 'OK' ELSE 'VAZIO' END
    FROM parametros_sistema
    UNION ALL
    SELECT 'configuracoes_email'::VARCHAR, COUNT(*), 
           CASE WHEN COUNT(*) > 0 THEN 'OK' ELSE 'VAZIO' END
    FROM configuracoes_email;
END;
$$ LANGUAGE plpgsql;

-- Function para limpar tokens expirados
CREATE OR REPLACE FUNCTION limpar_tokens_expirados()
RETURNS INTEGER AS $$
DECLARE
    tokens_removidos INTEGER;
BEGIN
    DELETE FROM password_reset_tokens 
    WHERE expira_em < CURRENT_TIMESTAMP OR usado = true;
    
    GET DIAGNOSTICS tokens_removidos = ROW_COUNT;
    RETURN tokens_removidos;
END;
$$ LANGUAGE plpgsql;

-- Function para obter estatísticas de usuários por perfil
CREATE OR REPLACE FUNCTION estatisticas_usuarios_por_perfil()
RETURNS TABLE(
    perfil_nome VARCHAR(100),
    total_usuarios INTEGER,
    usuarios_ativos INTEGER,
    usuarios_inativos INTEGER
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        p.nome,
        COUNT(u.id) as total_usuarios,
        COUNT(CASE WHEN u.ativo = true THEN 1 END) as usuarios_ativos,
        COUNT(CASE WHEN u.ativo = false THEN 1 END) as usuarios_inativos
    FROM perfis_acesso p
    LEFT JOIN usuarios u ON p.id = u.perfil_acesso_id
    GROUP BY p.id, p.nome
    ORDER BY p.nome;
END;
$$ LANGUAGE plpgsql;

-- Function para criptografar senha (simulação)
CREATE OR REPLACE FUNCTION criptografar_senha(senha_original TEXT)
RETURNS TEXT AS $$
BEGIN
    -- Em produção, usar algoritmos de hash seguros
    RETURN encode(sha256(senha_original::bytea), 'hex');
END;
$$ LANGUAGE plpgsql;

-- =====================================================
-- 5. CRIAÇÃO DE TRIGGERS
-- =====================================================

-- Trigger para atualizar data_atualizacao automaticamente
CREATE OR REPLACE FUNCTION atualizar_data_atualizacao()
RETURNS TRIGGER AS $$
BEGIN
    NEW.data_atualizacao = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Aplicar trigger em todas as tabelas principais
CREATE TRIGGER trigger_usuarios_atualizacao
    BEFORE UPDATE ON usuarios
    FOR EACH ROW EXECUTE FUNCTION atualizar_data_atualizacao();

CREATE TRIGGER trigger_perfis_acesso_atualizacao
    BEFORE UPDATE ON perfis_acesso
    FOR EACH ROW EXECUTE FUNCTION atualizar_data_atualizacao();

CREATE TRIGGER trigger_configuracoes_email_atualizacao
    BEFORE UPDATE ON configuracoes_email
    FOR EACH ROW EXECUTE FUNCTION atualizar_data_atualizacao();

CREATE TRIGGER trigger_parametros_sistema_atualizacao
    BEFORE UPDATE ON parametros_sistema
    FOR EACH ROW EXECUTE FUNCTION atualizar_data_atualizacao();

CREATE TRIGGER trigger_backlog_arquitetura_atualizacao
    BEFORE UPDATE ON backlog_arquitetura
    FOR EACH ROW EXECUTE FUNCTION atualizar_data_atualizacao();

CREATE TRIGGER trigger_parametros_ganho_atualizacao
    BEFORE UPDATE ON parametros_ganho
    FOR EACH ROW EXECUTE FUNCTION atualizar_data_atualizacao();

CREATE TRIGGER trigger_parametros_ambiente_atualizacao
    BEFORE UPDATE ON parametros_ambiente
    FOR EACH ROW EXECUTE FUNCTION atualizar_data_atualizacao();

-- =====================================================
-- 6. INSERÇÃO DE DADOS INICIAIS
-- =====================================================

-- Inserir perfis de acesso padrão
INSERT INTO perfis_acesso (nome, descricao, acesso_configuracoes, acesso_usuarios, acesso_projetos, acesso_relatorios, acesso_backlog_arquitetura, acesso_parametros_sistema) VALUES
('Administrador', 'Acesso total ao sistema', true, true, true, true, true, true),
('Usuário', 'Acesso limitado ao sistema', false, false, true, true, false, false),
('Gerente', 'Acesso intermediário ao sistema', false, true, true, true, true, false)
ON CONFLICT (nome) DO NOTHING;

-- Inserir usuário administrador padrão
-- Senha: admin123 (hash SHA256)
INSERT INTO usuarios (nome, email, senha, perfil_acesso_id, ativo) VALUES
('Administrador', 'admin@gerente.com', '240be518fabd2724ddb6f04eeb1da5967448d7e831c08c8fa822809f74c720a9', 
 (SELECT id FROM perfis_acesso WHERE nome = 'Administrador'), true)
ON CONFLICT (email) DO NOTHING;

-- Inserir parâmetros do sistema padrão
INSERT INTO parametros_sistema (chave, valor, descricao) VALUES
('cabecalho_sistema', 'Sistema Gerente', 'Título do cabeçalho do sistema'),
('versao_sistema', '1.0.0', 'Versão atual do sistema'),
('nome_rodape', 'Sistema Gerente 2025©', 'Texto do rodapé do sistema'),
('tema_sistema', 'default', 'Tema visual do sistema'),
('timezone_sistema', 'America/Sao_Paulo', 'Fuso horário padrão do sistema')
ON CONFLICT (chave) DO NOTHING;

-- Inserir configuração de email padrão (desabilitada)
INSERT INTO configuracoes_email (servidor_smtp, porta, usuario, senha_criptografada, usar_ssl, ativo) VALUES
('smtp.gmail.com', 587, 'seu-email@gmail.com', 'senha_criptografada_aqui', true, false)
ON CONFLICT DO NOTHING;

-- Inserir parâmetros de ganho padrão
INSERT INTO parametros_ganho (nome, valor, descricao) VALUES
('Ganho Básico', 1000.00, 'Ganho básico padrão'),
('Ganho Premium', 2500.00, 'Ganho premium'),
('Ganho VIP', 5000.00, 'Ganho VIP')
ON CONFLICT DO NOTHING;

-- Inserir parâmetros de ambiente padrão
INSERT INTO parametros_ambiente (nome, descricao) VALUES
('Desenvolvimento', 'Ambiente de desenvolvimento'),
('Homologação', 'Ambiente de homologação'),
('Produção', 'Ambiente de produção'),
('Teste', 'Ambiente de testes')
ON CONFLICT DO NOTHING;

-- Inserir dados de exemplo para backlog
INSERT INTO backlog_arquitetura (titulo, descricao, ambientes, prioridade, status, responsavel_id) VALUES
('Implementar Autenticação', 'Implementar sistema de autenticação JWT', 'Desenvolvimento, Homologação', 'Alta', 'Pendente', 
 (SELECT id FROM usuarios WHERE email = 'admin@gerente.com')),
('Criar Dashboard', 'Criar dashboard principal do sistema', 'Desenvolvimento', 'Média', 'Em Andamento', 
 (SELECT id FROM usuarios WHERE email = 'admin@gerente.com')),
('Otimizar Performance', 'Otimizar consultas do banco de dados', 'Produção', 'Baixa', 'Pendente', 
 (SELECT id FROM usuarios WHERE email = 'admin@gerente.com'))
ON CONFLICT DO NOTHING;

-- =====================================================
-- 7. CONFIGURAÇÕES DE SEGURANÇA
-- =====================================================

-- Criar role específico para a aplicação
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'gerente_app') THEN
        CREATE ROLE gerente_app WITH LOGIN PASSWORD 'gerente_app_password';
    END IF;
END
$$;

-- Conceder permissões ao role da aplicação
GRANT CONNECT ON DATABASE "Projetos" TO gerente_app;
GRANT USAGE ON SCHEMA public TO gerente_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO gerente_app;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO gerente_app;

-- Configurar permissões para novas tabelas
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO gerente_app;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT USAGE, SELECT ON SEQUENCES TO gerente_app;

-- =====================================================
-- 8. CONFIGURAÇÕES DE PERFORMANCE
-- =====================================================

-- Configurar autovacuum
ALTER TABLE usuarios SET (autovacuum_vacuum_scale_factor = 0.1);
ALTER TABLE usuarios SET (autovacuum_analyze_scale_factor = 0.05);
ALTER TABLE password_reset_tokens SET (autovacuum_vacuum_scale_factor = 0.2);
ALTER TABLE backlog_arquitetura SET (autovacuum_vacuum_scale_factor = 0.1);

-- Configurar estatísticas
ALTER TABLE usuarios ALTER COLUMN email SET STATISTICS 1000;
ALTER TABLE usuarios ALTER COLUMN perfil_acesso_id SET STATISTICS 1000;
ALTER TABLE backlog_arquitetura ALTER COLUMN status SET STATISTICS 1000;

-- =====================================================
-- 9. VERIFICAÇÃO FINAL
-- =====================================================

-- Verificar se todas as tabelas foram criadas
SELECT 
    schemaname,
    tablename,
    tableowner
FROM pg_tables 
WHERE schemaname = 'public' 
ORDER BY tablename;

-- Verificar se todas as views foram criadas
SELECT 
    schemaname,
    viewname,
    viewowner
FROM pg_views 
WHERE schemaname = 'public' 
ORDER BY viewname;

-- Verificar se todas as functions foram criadas
SELECT 
    n.nspname as schema_name,
    p.proname as function_name,
    pg_get_function_result(p.oid) as return_type
FROM pg_proc p
LEFT JOIN pg_namespace n ON p.pronamespace = n.oid
WHERE n.nspname = 'public'
ORDER BY p.proname;

-- Verificar integridade do banco
SELECT * FROM verificar_integridade_banco();

-- Verificar estatísticas de usuários por perfil
SELECT * FROM estatisticas_usuarios_por_perfil();

-- =====================================================
-- FIM DO SCRIPT
-- =====================================================
-- Script executado com sucesso!
-- Sistema Gerente está pronto para uso.
-- ===================================================== 