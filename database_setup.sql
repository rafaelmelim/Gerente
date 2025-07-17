-- =====================================================
-- SCRIPT COMPLETO DE BANCO DE DADOS - SISTEMA GERENTE
-- Versão: 1.0.0
-- Data: 16/07/2025
-- Descrição: Setup completo do banco de dados PostgreSQL
-- =====================================================

-- Conectar ao banco de dados
\c Projetos;

-- =====================================================
-- 1. CRIAÇÃO DAS TABELAS PRINCIPAIS
-- =====================================================

-- Tabela de usuários
CREATE TABLE IF NOT EXISTS usuarios (
    id SERIAL PRIMARY KEY,
    nome VARCHAR(100) NOT NULL,
    email VARCHAR(100) UNIQUE NOT NULL,
    senha VARCHAR(255) NOT NULL,
    ativo BOOLEAN DEFAULT true,
    data_criacao TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    data_atualizacao TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Tabela de tokens de reset de senha
CREATE TABLE IF NOT EXISTS password_reset_tokens (
    id SERIAL PRIMARY KEY,
    email VARCHAR(100) NOT NULL,
    token VARCHAR(255) UNIQUE NOT NULL,
    expiracao TIMESTAMP NOT NULL,
    usado BOOLEAN DEFAULT false,
    data_criacao TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Tabela de configuração de email
CREATE TABLE IF NOT EXISTS configuracao_email (
    id SERIAL PRIMARY KEY,
    servidor_smtp VARCHAR(100) NOT NULL,
    porta INTEGER NOT NULL,
    usuario VARCHAR(100) NOT NULL,
    senha VARCHAR(255) NOT NULL,
    ssl_tls BOOLEAN DEFAULT false,
    ativo BOOLEAN DEFAULT true,
    data_criacao TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    data_atualizacao TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Tabela de projetos
CREATE TABLE IF NOT EXISTS projetos (
    id SERIAL PRIMARY KEY,
    nome VARCHAR(200) NOT NULL,
    descricao TEXT,
    status VARCHAR(50) DEFAULT 'Em Andamento',
    data_inicio DATE,
    data_fim DATE,
    orcamento DECIMAL(15,2),
    usuario_id INTEGER REFERENCES usuarios(id),
    ativo BOOLEAN DEFAULT true,
    data_criacao TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    data_atualizacao TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Tabela de tarefas
CREATE TABLE IF NOT EXISTS tarefas (
    id SERIAL PRIMARY KEY,
    titulo VARCHAR(200) NOT NULL,
    descricao TEXT,
    status VARCHAR(50) DEFAULT 'Pendente',
    prioridade VARCHAR(20) DEFAULT 'Média',
    data_inicio DATE,
    data_fim DATE,
    projeto_id INTEGER REFERENCES projetos(id) ON DELETE CASCADE,
    usuario_responsavel INTEGER REFERENCES usuarios(id),
    ativo BOOLEAN DEFAULT true,
    data_criacao TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    data_atualizacao TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Tabela de logs de atividades
CREATE TABLE IF NOT EXISTS logs_atividade (
    id SERIAL PRIMARY KEY,
    usuario_id INTEGER REFERENCES usuarios(id),
    acao VARCHAR(100) NOT NULL,
    tabela_afetada VARCHAR(50),
    registro_id INTEGER,
    dados_anteriores JSONB,
    dados_novos JSONB,
    ip_address INET,
    user_agent TEXT,
    data_criacao TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- =====================================================
-- 2. ÍNDICES PARA PERFORMANCE
-- =====================================================

-- Índices para usuários
CREATE INDEX IF NOT EXISTS idx_usuarios_email ON usuarios(email);
CREATE INDEX IF NOT EXISTS idx_usuarios_ativo ON usuarios(ativo);
CREATE INDEX IF NOT EXISTS idx_usuarios_data_criacao ON usuarios(data_criacao);

-- Índices para tokens de reset
CREATE INDEX IF NOT EXISTS idx_password_reset_tokens_email ON password_reset_tokens(email);
CREATE INDEX IF NOT EXISTS idx_password_reset_tokens_token ON password_reset_tokens(token);
CREATE INDEX IF NOT EXISTS idx_password_reset_tokens_expiracao ON password_reset_tokens(expiracao);
CREATE INDEX IF NOT EXISTS idx_password_reset_tokens_usado ON password_reset_tokens(usado);

-- Índices para projetos
CREATE INDEX IF NOT EXISTS idx_projetos_usuario_id ON projetos(usuario_id);
CREATE INDEX IF NOT EXISTS idx_projetos_status ON projetos(status);
CREATE INDEX IF NOT EXISTS idx_projetos_ativo ON projetos(ativo);
CREATE INDEX IF NOT EXISTS idx_projetos_data_inicio ON projetos(data_inicio);

-- Índices para tarefas
CREATE INDEX IF NOT EXISTS idx_tarefas_projeto_id ON tarefas(projeto_id);
CREATE INDEX IF NOT EXISTS idx_tarefas_usuario_responsavel ON tarefas(usuario_responsavel);
CREATE INDEX IF NOT EXISTS idx_tarefas_status ON tarefas(status);
CREATE INDEX IF NOT EXISTS idx_tarefas_prioridade ON tarefas(prioridade);
CREATE INDEX IF NOT EXISTS idx_tarefas_data_fim ON tarefas(data_fim);

-- Índices para logs
CREATE INDEX IF NOT EXISTS idx_logs_usuario_id ON logs_atividade(usuario_id);
CREATE INDEX IF NOT EXISTS idx_logs_acao ON logs_atividade(acao);
CREATE INDEX IF NOT EXISTS idx_logs_data_criacao ON logs_atividade(data_criacao);
CREATE INDEX IF NOT EXISTS idx_logs_tabela_afetada ON logs_atividade(tabela_afetada);

-- =====================================================
-- 3. CONSTRAINTS E VALIDAÇÕES
-- =====================================================

-- Constraints para usuários
ALTER TABLE usuarios ADD CONSTRAINT chk_email_valido 
    CHECK (email ~* '^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$');

ALTER TABLE usuarios ADD CONSTRAINT chk_nome_nao_vazio 
    CHECK (LENGTH(TRIM(nome)) > 0);

-- Constraints para projetos
ALTER TABLE projetos ADD CONSTRAINT chk_projeto_datas 
    CHECK (data_fim IS NULL OR data_inicio IS NULL OR data_fim >= data_inicio);

ALTER TABLE projetos ADD CONSTRAINT chk_projeto_orcamento 
    CHECK (orcamento IS NULL OR orcamento >= 0);

ALTER TABLE projetos ADD CONSTRAINT chk_projeto_status 
    CHECK (status IN ('Em Andamento', 'Concluído', 'Cancelado', 'Pausado'));

-- Constraints para tarefas
ALTER TABLE tarefas ADD CONSTRAINT chk_tarefa_datas 
    CHECK (data_fim IS NULL OR data_inicio IS NULL OR data_fim >= data_inicio);

ALTER TABLE tarefas ADD CONSTRAINT chk_tarefa_status 
    CHECK (status IN ('Pendente', 'Em Andamento', 'Concluída', 'Cancelada'));

ALTER TABLE tarefas ADD CONSTRAINT chk_tarefa_prioridade 
    CHECK (prioridade IN ('Baixa', 'Média', 'Alta', 'Crítica'));

-- Constraints para configuração de email
ALTER TABLE configuracao_email ADD CONSTRAINT chk_email_porta 
    CHECK (porta > 0 AND porta <= 65535);

ALTER TABLE configuracao_email ADD CONSTRAINT chk_email_servidor 
    CHECK (LENGTH(TRIM(servidor_smtp)) > 0);

-- =====================================================
-- 4. FUNÇÕES E TRIGGERS
-- =====================================================

-- Função para atualizar data_atualizacao automaticamente
CREATE OR REPLACE FUNCTION atualizar_data_atualizacao()
RETURNS TRIGGER AS $$
BEGIN
    NEW.data_atualizacao = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Triggers para atualizar data_atualizacao
CREATE TRIGGER trigger_usuarios_atualizacao
    BEFORE UPDATE ON usuarios
    FOR EACH ROW
    EXECUTE FUNCTION atualizar_data_atualizacao();

CREATE TRIGGER trigger_projetos_atualizacao
    BEFORE UPDATE ON projetos
    FOR EACH ROW
    EXECUTE FUNCTION atualizar_data_atualizacao();

CREATE TRIGGER trigger_tarefas_atualizacao
    BEFORE UPDATE ON tarefas
    FOR EACH ROW
    EXECUTE FUNCTION atualizar_data_atualizacao();

CREATE TRIGGER trigger_configuracao_email_atualizacao
    BEFORE UPDATE ON configuracao_email
    FOR EACH ROW
    EXECUTE FUNCTION atualizar_data_atualizacao();

-- Função para limpar tokens expirados
CREATE OR REPLACE FUNCTION limpar_tokens_expirados()
RETURNS INTEGER AS $$
DECLARE
    tokens_removidos INTEGER;
BEGIN
    DELETE FROM password_reset_tokens 
    WHERE expiracao < CURRENT_TIMESTAMP OR usado = true;
    
    GET DIAGNOSTICS tokens_removidos = ROW_COUNT;
    RETURN tokens_removidos;
END;
$$ LANGUAGE plpgsql;

-- Função para validar força da senha
CREATE OR REPLACE FUNCTION validar_forca_senha(senha TEXT)
RETURNS BOOLEAN AS $$
BEGIN
    -- Mínimo 6 caracteres
    IF LENGTH(senha) < 6 THEN
        RETURN FALSE;
    END IF;
    
    -- Pelo menos uma letra maiúscula
    IF senha !~ '[A-Z]' THEN
        RETURN FALSE;
    END IF;
    
    -- Pelo menos uma letra minúscula
    IF senha !~ '[a-z]' THEN
        RETURN FALSE;
    END IF;
    
    -- Pelo menos um número
    IF senha !~ '[0-9]' THEN
        RETURN FALSE;
    END IF;
    
    RETURN TRUE;
END;
$$ LANGUAGE plpgsql;

-- =====================================================
-- 5. DADOS INICIAIS
-- =====================================================

-- Inserir usuário administrador padrão
-- Senha: Admin123! (hash BCrypt)
INSERT INTO usuarios (nome, email, senha, ativo) VALUES 
('Administrador', 'admin@gerente.com', '$2a$11$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi', true)
ON CONFLICT (email) DO NOTHING;

-- Inserir usuário de teste
-- Senha: Teste123! (hash BCrypt)
INSERT INTO usuarios (nome, email, senha, ativo) VALUES 
('Usuário Teste', 'teste@gerente.com', '$2a$11$N.zmdr9k7uOCQb376NoUnuTJ8iAt6Z5EHsM8lE9lBOsl7iKTVEFDa', true)
ON CONFLICT (email) DO NOTHING;

-- Configuração de email padrão (Gmail)
INSERT INTO configuracao_email (servidor_smtp, porta, usuario, senha, ssl_tls, ativo) VALUES 
('smtp.gmail.com', 587, '', '', true, false)
ON CONFLICT DO NOTHING;

-- Projetos de exemplo
INSERT INTO projetos (nome, descricao, status, data_inicio, data_fim, orcamento, usuario_id) VALUES 
('Projeto Exemplo 1', 'Descrição do projeto exemplo 1', 'Em Andamento', CURRENT_DATE, CURRENT_DATE + INTERVAL '30 days', 50000.00, 1),
('Projeto Exemplo 2', 'Descrição do projeto exemplo 2', 'Pendente', CURRENT_DATE, CURRENT_DATE + INTERVAL '60 days', 75000.00, 1)
ON CONFLICT DO NOTHING;

-- Tarefas de exemplo
INSERT INTO tarefas (titulo, descricao, status, prioridade, data_inicio, data_fim, projeto_id, usuario_responsavel) VALUES 
('Análise de Requisitos', 'Realizar análise detalhada dos requisitos do projeto', 'Em Andamento', 'Alta', CURRENT_DATE, CURRENT_DATE + INTERVAL '7 days', 1, 1),
('Desenvolvimento Backend', 'Implementar APIs e lógica de negócio', 'Pendente', 'Média', CURRENT_DATE + INTERVAL '7 days', CURRENT_DATE + INTERVAL '21 days', 1, 1),
('Testes de Integração', 'Executar testes de integração entre módulos', 'Pendente', 'Baixa', CURRENT_DATE + INTERVAL '21 days', CURRENT_DATE + INTERVAL '28 days', 1, 1)
ON CONFLICT DO NOTHING;

-- =====================================================
-- 6. VIEWS ÚTEIS
-- =====================================================

-- View para dashboard de projetos
CREATE OR REPLACE VIEW vw_dashboard_projetos AS
SELECT 
    p.id,
    p.nome,
    p.status,
    p.data_inicio,
    p.data_fim,
    p.orcamento,
    u.nome as responsavel,
    COUNT(t.id) as total_tarefas,
    COUNT(CASE WHEN t.status = 'Concluída' THEN 1 END) as tarefas_concluidas,
    ROUND(
        CASE 
            WHEN COUNT(t.id) > 0 THEN 
                (COUNT(CASE WHEN t.status = 'Concluída' THEN 1 END)::DECIMAL / COUNT(t.id)::DECIMAL) * 100
            ELSE 0 
        END, 2
    ) as percentual_conclusao
FROM projetos p
LEFT JOIN usuarios u ON p.usuario_id = u.id
LEFT JOIN tarefas t ON p.id = t.projeto_id AND t.ativo = true
WHERE p.ativo = true
GROUP BY p.id, p.nome, p.status, p.data_inicio, p.data_fim, p.orcamento, u.nome;

-- View para relatório de tarefas
CREATE OR REPLACE VIEW vw_relatorio_tarefas AS
SELECT 
    t.id,
    t.titulo,
    t.status,
    t.prioridade,
    t.data_inicio,
    t.data_fim,
    p.nome as projeto,
    u.nome as responsavel,
    CASE 
        WHEN t.data_fim < CURRENT_DATE AND t.status != 'Concluída' THEN 'Atrasada'
        WHEN t.data_fim = CURRENT_DATE AND t.status != 'Concluída' THEN 'Vence Hoje'
        WHEN t.data_fim BETWEEN CURRENT_DATE AND CURRENT_DATE + INTERVAL '3 days' AND t.status != 'Concluída' THEN 'Vence em Breve'
        ELSE 'No Prazo'
    END as situacao_prazo
FROM tarefas t
LEFT JOIN projetos p ON t.projeto_id = p.id
LEFT JOIN usuarios u ON t.usuario_responsavel = u.id
WHERE t.ativo = true AND p.ativo = true;

-- View para estatísticas do sistema
CREATE OR REPLACE VIEW vw_estatisticas_sistema AS
SELECT 
    (SELECT COUNT(*) FROM usuarios WHERE ativo = true) as total_usuarios,
    (SELECT COUNT(*) FROM projetos WHERE ativo = true) as total_projetos,
    (SELECT COUNT(*) FROM tarefas WHERE ativo = true) as total_tarefas,
    (SELECT COUNT(*) FROM projetos WHERE status = 'Em Andamento' AND ativo = true) as projetos_em_andamento,
    (SELECT COUNT(*) FROM tarefas WHERE status = 'Pendente' AND ativo = true) as tarefas_pendentes,
    (SELECT COUNT(*) FROM tarefas WHERE status = 'Concluída' AND ativo = true) as tarefas_concluidas;

-- =====================================================
-- 7. PERMISSÕES E SEGURANÇA
-- =====================================================

-- Garantir que o usuário admin tenha todas as permissões
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO admin;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public TO admin;
GRANT EXECUTE ON ALL FUNCTIONS IN SCHEMA public TO admin;

-- Permitir que o usuário admin crie novas tabelas
GRANT CREATE ON SCHEMA public TO admin;

-- =====================================================
-- 8. MANUTENÇÃO E LIMPEZA
-- =====================================================

-- Criar job para limpar tokens expirados (executar manualmente ou via cron)
-- SELECT limpar_tokens_expirados();

-- =====================================================
-- 9. VERIFICAÇÃO FINAL
-- =====================================================

-- Verificar se todas as tabelas foram criadas
SELECT 
    table_name,
    (SELECT COUNT(*) FROM information_schema.columns WHERE table_name = t.table_name) as colunas
FROM information_schema.tables t
WHERE table_schema = 'public' 
AND table_type = 'BASE TABLE'
ORDER BY table_name;

-- Verificar se os dados iniciais foram inseridos
SELECT 'Usuários' as tabela, COUNT(*) as total FROM usuarios
UNION ALL
SELECT 'Projetos', COUNT(*) FROM projetos
UNION ALL
SELECT 'Tarefas', COUNT(*) FROM tarefas
UNION ALL
SELECT 'Configuração Email', COUNT(*) FROM configuracao_email;

-- =====================================================
-- FIM DO SCRIPT
-- =====================================================

-- Mensagem de conclusão
DO $$
BEGIN
    RAISE NOTICE 'Script de banco de dados executado com sucesso!';
    RAISE NOTICE 'Sistema Gerente v1.0.0 configurado.';
    RAISE NOTICE 'Usuário admin: admin@gerente.com / Admin123!';
    RAISE NOTICE 'Usuário teste: teste@gerente.com / Teste123!';
END $$; 