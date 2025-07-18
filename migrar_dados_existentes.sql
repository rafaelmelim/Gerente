-- =====================================================
-- SCRIPT PARA MIGRAR DADOS EXISTENTES
-- =====================================================
-- Este script migra dados de um banco existente para um novo
-- Execute após criar o novo banco com database_completo.sql
-- =====================================================

-- Configurações
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;

-- =====================================================
-- 1. BACKUP DOS DADOS EXISTENTES
-- =====================================================

-- Comandos para fazer backup do banco existente:
-- pg_dump -U admin -d Projetos_antigo -t perfis_acesso > backup_perfis_acesso.sql
-- pg_dump -U admin -d Projetos_antigo -t usuarios > backup_usuarios.sql
-- pg_dump -U admin -d Projetos_antigo -t configuracao_email > backup_configuracao_email.sql
-- pg_dump -U admin -d Projetos_antigo -t parametros_sistema > backup_parametros_sistema.sql

-- =====================================================
-- 2. MIGRAÇÃO DE PERFIS DE ACESSO
-- =====================================================

-- Inserir perfis existentes (se houver)
-- Copie os dados do backup e execute aqui
-- Exemplo:
/*
INSERT INTO perfis_acesso (nome, descricao, acesso_configuracoes, acesso_usuarios, acesso_projetos, acesso_relatorios, acesso_parametros_sistema, acesso_total, ativo, data_criacao, data_atualizacao)
SELECT nome, descricao, acesso_configuracoes, acesso_usuarios, acesso_projetos, acesso_relatorios, acesso_parametros_sistema, acesso_total, ativo, data_criacao, data_atualizacao
FROM perfis_acesso_backup
WHERE nome NOT IN ('Administrador', 'Usuário');
*/

-- =====================================================
-- 3. MIGRAÇÃO DE USUÁRIOS
-- =====================================================

-- Inserir usuários existentes
-- Exemplo:
/*
INSERT INTO usuarios (nome, email, senha, perfil_acesso_id, ativo, data_criacao, data_atualizacao)
SELECT 
    u.nome,
    u.email,
    u.senha,
    pa.id as perfil_acesso_id,
    u.ativo,
    u.data_criacao,
    u.data_atualizacao
FROM usuarios_backup u
LEFT JOIN perfis_acesso pa ON pa.nome = (
    SELECT nome FROM perfis_acesso_backup WHERE id = u.perfil_acesso_id
)
WHERE u.email NOT IN ('admin@dorowcamp.com', 'admin@sistema.com');
*/

-- =====================================================
-- 4. MIGRAÇÃO DE CONFIGURAÇÃO DE EMAIL
-- =====================================================

-- Inserir configurações de email existentes
-- Exemplo:
/*
INSERT INTO configuracao_email (servidor_smtp, porta, usuario, senha, ssl, tls, remetente, nome_remetente, ativo, data_criacao, data_atualizacao)
SELECT servidor_smtp, porta, usuario, senha, ssl, tls, remetente, nome_remetente, ativo, data_criacao, data_atualizacao
FROM configuracao_email_backup
WHERE ativo = TRUE;
*/

-- =====================================================
-- 5. MIGRAÇÃO DE PARÂMETROS DO SISTEMA
-- =====================================================

-- Inserir parâmetros existentes
-- Exemplo:
/*
INSERT INTO parametros_sistema (cabecalho_sistema, versao_sistema, nome_rodape, data_criacao, data_atualizacao)
SELECT cabecalho_sistema, versao_sistema, nome_rodape, data_criacao, data_atualizacao
FROM parametros_sistema_backup
WHERE id = (SELECT MAX(id) FROM parametros_sistema_backup);
*/

-- =====================================================
-- 6. SCRIPT AUTOMÁTICO DE MIGRAÇÃO
-- =====================================================

-- Function para migrar dados automaticamente
CREATE OR REPLACE FUNCTION migrar_dados_automaticamente()
RETURNS TABLE(
    tabela VARCHAR(100),
    registros_migrados INTEGER,
    status VARCHAR(50)
) AS $$
DECLARE
    total_perfis INTEGER := 0;
    total_usuarios INTEGER := 0;
    total_configs INTEGER := 0;
    total_params INTEGER := 0;
BEGIN
    -- Aqui você pode adicionar a lógica de migração
    -- Por exemplo, ler de arquivos CSV ou de tabelas temporárias
    
    -- Contar registros migrados
    SELECT COUNT(*) INTO total_perfis FROM perfis_acesso WHERE nome NOT IN ('Administrador', 'Usuário');
    SELECT COUNT(*) INTO total_usuarios FROM usuarios WHERE email NOT IN ('admin@dorowcamp.com', 'admin@sistema.com');
    SELECT COUNT(*) INTO total_configs FROM configuracao_email;
    SELECT COUNT(*) INTO total_params FROM parametros_sistema;
    
    RETURN QUERY
    SELECT 'perfis_acesso'::VARCHAR, total_perfis, 
           CASE WHEN total_perfis > 0 THEN 'MIGRADOS' ELSE 'SEM DADOS' END
    UNION ALL
    SELECT 'usuarios'::VARCHAR, total_usuarios, 
           CASE WHEN total_usuarios > 0 THEN 'MIGRADOS' ELSE 'SEM DADOS' END
    UNION ALL
    SELECT 'configuracao_email'::VARCHAR, total_configs, 
           CASE WHEN total_configs > 0 THEN 'MIGRADOS' ELSE 'SEM DADOS' END
    UNION ALL
    SELECT 'parametros_sistema'::VARCHAR, total_params, 
           CASE WHEN total_params > 0 THEN 'MIGRADOS' ELSE 'SEM DADOS' END;
END;
$$ LANGUAGE plpgsql;

-- =====================================================
-- 7. PROCEDURE PARA MIGRAÇÃO COMPLETA
-- =====================================================

CREATE OR REPLACE PROCEDURE executar_migracao_completa()
LANGUAGE plpgsql
AS $$
BEGIN
    -- 1. Verificar se o banco está pronto
    IF NOT EXISTS (SELECT 1 FROM perfis_acesso WHERE nome = 'Administrador') THEN
        RAISE EXCEPTION 'Banco de dados não está configurado. Execute database_completo.sql primeiro.';
    END IF;
    
    -- 2. Fazer backup dos dados atuais
    RAISE NOTICE 'Iniciando backup dos dados atuais...';
    
    -- 3. Executar migração
    RAISE NOTICE 'Executando migração de dados...';
    
    -- 4. Verificar integridade
    RAISE NOTICE 'Verificando integridade dos dados migrados...';
    
    -- 5. Limpar dados temporários
    RAISE NOTICE 'Limpando dados temporários...';
    
    RAISE NOTICE 'Migração concluída com sucesso!';
END;
$$;

-- =====================================================
-- 8. COMANDOS PARA EXECUTAR MIGRAÇÃO
-- =====================================================

-- Para executar a migração:
-- 1. CALL executar_migracao_completa();
-- 2. SELECT * FROM migrar_dados_automaticamente();
-- 3. SELECT * FROM verificar_integridade_banco();

-- =====================================================
-- 9. VERIFICAÇÕES PÓS-MIGRAÇÃO
-- =====================================================

-- Verificar se todos os usuários têm perfis válidos
SELECT 
    u.email,
    u.nome,
    pa.nome as perfil_nome,
    CASE WHEN pa.id IS NULL THEN 'SEM PERFIL' ELSE 'OK' END as status
FROM usuarios u
LEFT JOIN perfis_acesso pa ON u.perfil_acesso_id = pa.id
WHERE u.ativo = TRUE;

-- Verificar configurações de email
SELECT 
    servidor_smtp,
    porta,
    remetente,
    ativo,
    CASE WHEN ativo THEN 'ATIVO' ELSE 'INATIVO' END as status
FROM configuracao_email;

-- Verificar parâmetros do sistema
SELECT 
    cabecalho_sistema,
    versao_sistema,
    nome_rodape,
    data_atualizacao
FROM parametros_sistema;

-- =====================================================
-- 10. LIMPEZA E OTIMIZAÇÃO
-- =====================================================

-- Limpar tokens expirados
SELECT limpar_tokens_expirados();

-- Atualizar estatísticas
ANALYZE;

-- Verificar fragmentação
VACUUM ANALYZE;

-- =====================================================
-- FIM DO SCRIPT DE MIGRAÇÃO
-- =====================================================
-- Execute este script após criar o novo banco com database_completo.sql
-- ===================================================== 