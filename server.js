/**
 * ╔══════════════════════════════════════════════════════════════════╗
 * ║  server.js — Backend Principal                                   ║
 * ║  5º CTA / 10ª Brigada de Infantaria                              ║
 * ║  Sistema de Controle de Acesso Biométrico                        ║
 * ╚══════════════════════════════════════════════════════════════════╝
 *
 * O QUE ESTE ARQUIVO FAZ:
 *   Este é o "cérebro" do sistema. Ele recebe pedidos do navegador,
 *   consulta o banco de dados e devolve as respostas.
 *   Pense nele como um garçom que leva pedidos para a cozinha (banco)
 *   e traz os pratos (dados) de volta para o cliente (navegador).
 *
 * PARA INICIAR:
 *   node server.js
 *
 * PORTA PADRÃO:
 *   http://localhost:3001
 */

// ── 1. IMPORTAÇÕES ────────────────────────────────────────────────────────────
// Cada "require" carrega uma biblioteca que foi instalada via "npm install"

const express        = require('express');         // Framework web — cria rotas HTTP
const cors           = require('cors');             // Permite que o HTML acesse este servidor
const session        = require('express-session'); // Controla login/logout (sessões)
const bcrypt         = require('bcryptjs');         // Criptografa senhas (nunca guarda senha pura)
const path           = require('path');             // Ajuda a montar caminhos de arquivo
const fs             = require('fs');               // Leitura e escrita de arquivos
const { v4: uuidv4 } = require('uuid');            // Gera IDs únicos para cada registro
const initSqlJs      = require('sql.js');           // Banco de dados SQLite em JavaScript puro

// ── 2. CONFIGURAÇÕES BÁSICAS ──────────────────────────────────────────────────

const app  = express();  // Cria a aplicação web
const PORT = 3001;       // Porta onde o servidor vai "escutar" pedidos

// Caminhos de arquivo — __dirname = pasta onde está este arquivo
const DB_FILE   = path.join(__dirname, 'cta_acesso.db'); // Banco de dados
const CLIPS_DIR = path.join(__dirname, 'clips');          // Pasta dos clipes de vídeo

// Criar pasta de clipes se não existir
if (!fs.existsSync(CLIPS_DIR)) fs.mkdirSync(CLIPS_DIR, { recursive: true });

// ── 3. MIDDLEWARES ─────────────────────────────────────────────────────────────
// Middlewares são funções que rodam em TODA requisição, antes das rotas.

// Permite que o arquivo HTML (em outra porta) acesse este servidor
app.use(cors({
  origin: true,           // Aceita qualquer origem (em produção, restringir para o IP do servidor)
  credentials: true       // Necessário para cookies de sessão funcionarem
}));

// Faz o Express entender JSON no corpo das requisições
app.use(express.json());

// Faz o Express entender formulários HTML tradicionais
app.use(express.urlencoded({ extended: true }));

// Serve os arquivos de clipe de vídeo como arquivos estáticos
// Ex: http://localhost:3001/clips/video.mp4
app.use('/clips', express.static(CLIPS_DIR));

// Serve o frontend diretamente pelo backend (evita problema de CORS)
app.use(express.static(__dirname));

// Configuração de sessão (controla quem está logado)
// A sessão funciona com um cookie no navegador que identifica o usuário
app.use(session({
  secret: 'CTA_5_BRIGADA_2024_SECRETO', // Chave para assinar o cookie (mude isso em produção!)
  resave: false,            // Não salva sessão se não houve mudança
  saveUninitialized: false, // Não cria sessão vazia
  cookie: {
    secure: false,          // false para HTTP local; true para HTTPS em produção
    httpOnly: true,         // Cookie não acessível por JavaScript (segurança)
    maxAge: 8 * 60 * 60 * 1000 // Sessão dura 8 horas (turno de trabalho)
  }
}));

// ── 4. VARIÁVEL DO BANCO DE DADOS ─────────────────────────────────────────────
// Esta variável vai guardar a conexão com o banco depois de inicializar
let db;

// ── 5. FUNÇÕES AUXILIARES DO BANCO ────────────────────────────────────────────
// Criamos funções simples para consultar o banco sem repetir código

/**
 * Salva o banco de dados no arquivo físico em disco.
 * O sql.js trabalha na memória RAM; este save grava no HD.
 */
function salvarBanco() {
  const data = db.export();                           // Exporta o banco da RAM
  fs.writeFileSync(DB_FILE, Buffer.from(data));       // Grava no arquivo .db
}

/**
 * Executa uma query SQL e retorna APENAS UMA linha.
 * Exemplo: buscar um usuário pelo login.
 * @param {string} sql    - A query SQL (ex: "SELECT * FROM usuarios WHERE login=?")
 * @param {Array}  params - Os valores dos ? na query (ex: ['admin'])
 * @returns {Object|null} - Um objeto com os dados, ou null se não encontrar
 */
function dbGet(sql, params = []) {
  const stmt = db.prepare(sql);  // Prepara a query (evita SQL injection)
  stmt.bind(params);              // Substitui os ? pelos valores
  const row = stmt.step() ? stmt.getAsObject() : null; // Pega a primeira linha
  stmt.free();                    // Libera memória
  return row;
}

/**
 * Executa uma query SQL e retorna TODAS as linhas.
 * Exemplo: listar todos os acessos.
 * @param {string} sql    - A query SQL
 * @param {Array}  params - Os valores dos ?
 * @returns {Array} - Array de objetos com os dados
 */
function dbAll(sql, params = []) {
  const rows = [];
  const stmt = db.prepare(sql);
  stmt.bind(params);
  while (stmt.step()) rows.push(stmt.getAsObject()); // Percorre todas as linhas
  stmt.free();
  return rows;
}

/**
 * Executa uma query que MODIFICA dados (INSERT, UPDATE, DELETE).
 * Automaticamente salva o banco em disco após a modificação.
 * @param {string} sql    - A query SQL
 * @param {Array}  params - Os valores dos ?
 */
function dbRun(sql, params = []) {
  db.run(sql, params);  // Executa no banco em memória
  salvarBanco();         // Persiste em disco
}

// ── 6. MIDDLEWARE DE AUTENTICAÇÃO ─────────────────────────────────────────────
/**
 * Esta função verifica se o usuário está logado antes de acessar rotas protegidas.
 * Se não estiver logado, retorna erro 401 (não autorizado).
 * Usamos ela nas rotas que precisam de login: app.get('/api/...', autenticar, ...)
 */
function autenticar(req, res, next) {
  if (req.session && req.session.usuarioId) {
    next(); // Está logado → passa para a próxima função
  } else {
    res.status(401).json({ erro: 'Acesso negado. Faça login primeiro.' });
  }
}

/**
 * Verifica se o usuário logado é administrador.
 * Usamos depois do autenticar para rotas restritas a admins.
 */
function apenasAdmin(req, res, next) {
  if (req.session && req.session.perfil === 'admin') {
    next();
  } else {
    res.status(403).json({ erro: 'Acesso negado. Apenas administradores.' });
  }
}

// ── 7. INICIALIZAÇÃO DO BANCO DE DADOS ───────────────────────────────────────
/**
 * Função principal de inicialização do banco.
 * Cria as tabelas se não existirem e carrega dados de teste.
 * Roda UMA VEZ quando o servidor inicia.
 */
async function inicializarBanco() {
  const SQL = await initSqlJs(); // Carrega o motor SQL.js

  // Carrega banco existente ou cria um novo
  if (fs.existsSync(DB_FILE)) {
    db = new SQL.Database(fs.readFileSync(DB_FILE));
    console.log('📂 Banco carregado:', DB_FILE);
  } else {
    db = new SQL.Database();
    console.log('🆕 Banco novo criado:', DB_FILE);
  }

  // ── CRIAÇÃO DAS TABELAS ──────────────────────────────────────────────────
  // Cada CREATE TABLE IF NOT EXISTS cria a tabela apenas se ela não existir.
  // Ou seja, é seguro rodar várias vezes sem perder dados.

  // Tabela de usuários do SISTEMA (quem faz login no painel)
  db.run(`CREATE TABLE IF NOT EXISTS usuarios (
    id            TEXT PRIMARY KEY,
    nome          TEXT NOT NULL,
    login         TEXT UNIQUE NOT NULL,
    senha_hash    TEXT NOT NULL,
    perfil        TEXT DEFAULT 'operador',  -- 'admin' ou 'operador'
    ativo         INTEGER DEFAULT 1,
    criado_em     TEXT DEFAULT (datetime('now','localtime')),
    ultimo_acesso TEXT
  )`);

  // Tabela de pessoas cadastradas no sistema biométrico (militares)
  db.run(`CREATE TABLE IF NOT EXISTS pessoas (
    id            TEXT PRIMARY KEY,
    nome          TEXT NOT NULL,
    matricula     TEXT UNIQUE NOT NULL,
    patente       TEXT,
    departamento  TEXT,
    ativo         INTEGER DEFAULT 1,
    criado_em     TEXT DEFAULT (datetime('now','localtime'))
  )`);

  // Tabela de registros de acesso (entrada/saída)
  db.run(`CREATE TABLE IF NOT EXISTS acessos (
    id          TEXT PRIMARY KEY,
    pessoa_id   TEXT NOT NULL,
    tipo        TEXT NOT NULL,        -- 'entrada' ou 'saida'
    timestamp   TEXT NOT NULL,        -- Data e hora do acesso
    local       TEXT DEFAULT 'Porta Principal',
    metodo      TEXT DEFAULT 'digital', -- 'digital', 'cartao', 'cartao+digital'
    autorizado  INTEGER DEFAULT 1,    -- 1=permitido, 0=negado
    clip_url    TEXT,                 -- URL do clipe de vídeo (gerado depois)
    clip_status TEXT DEFAULT 'pendente', -- 'pendente','processando','pronto','erro','demo'
    FOREIGN KEY (pessoa_id) REFERENCES pessoas(id)
  )`);

  // Tabela de câmeras cadastradas
  db.run(`CREATE TABLE IF NOT EXISTS cameras (
    id      TEXT PRIMARY KEY,
    nome    TEXT NOT NULL,
    local   TEXT,
    url     TEXT NOT NULL,  -- URL completa: rtsp://... ou http://... ou 'webcam'
    tipo    TEXT DEFAULT 'rtsp',  -- 'rtsp','http','webcam','publica'
    ativa   INTEGER DEFAULT 1,
    criado_em TEXT DEFAULT (datetime('now','localtime'))
  )`);

  // Tabela de configurações gerais do sistema
  db.run(`CREATE TABLE IF NOT EXISTS configuracoes (
    chave TEXT PRIMARY KEY,
    valor TEXT NOT NULL
  )`);

  salvarBanco();

  // ── DADOS INICIAIS ───────────────────────────────────────────────────────
  // Inserir configurações padrão se não existirem
  const cfgExiste = dbGet("SELECT chave FROM configuracoes WHERE chave='nome_instalacao'");
  if (!cfgExiste) {
    const configs = [
      ['nome_instalacao', '5º CTA — 10ª Brigada de Infantaria'],
      ['tempo_sessao_horas', '8'],
      ['exibir_negados_dashboard', '1'],
      ['max_tentativas_login', '5'],
    ];
    configs.forEach(([k, v]) => db.run(`INSERT INTO configuracoes VALUES (?,?)`, [k, v]));
    salvarBanco();
  }

  // Verificar se há pessoas cadastradas; se não, inserir dados de TESTE
  const totalPessoas = dbGet("SELECT COUNT(*) as n FROM pessoas");
  if (!totalPessoas || totalPessoas.n === 0) {
    await inserirDadosDeTeste();
  }
}

/**
 * Insere dados fictícios para permitir testes completos.
 * Esta função só roda quando o banco está vazio (primeira vez).
 */
async function inserirDadosDeTeste() {
  console.log('📊 Inserindo dados de teste...');

  // 6 militares de teste
  const pessoas = [
    { id: 'p1', nome: 'Cap. Ricardo Alves',    mat: '001234', pat: 'Capitão',   dep: 'Comando' },
    { id: 'p2', nome: 'Sgt. Marcos Ferreira',  mat: '002345', pat: 'Sargento',  dep: 'CTA' },
    { id: 'p3', nome: 'Cb. Ana Paula Lima',    mat: '003456', pat: 'Cabo',      dep: 'TI' },
    { id: 'p4', nome: 'Ten. Carlos Eduardo',   mat: '004567', pat: 'Tenente',   dep: 'Operações' },
    { id: 'p5', nome: 'Sd. Bruno Nascimento',  mat: '005678', pat: 'Soldado',   dep: 'Logística' },
    { id: 'p6', nome: 'Maj. Fernanda Costa',   mat: '006789', pat: 'Major',     dep: 'Administração' },
  ];
  pessoas.forEach(p =>
    db.run(`INSERT INTO pessoas (id,nome,matricula,patente,departamento) VALUES (?,?,?,?,?)`,
      [p.id, p.nome, p.mat, p.pat, p.dep])
  );

  // 150 registros de acesso espalhados pelos últimos 7 dias
  const locais   = ['Porta Principal', 'Sala Servidores', 'Almoxarifado', 'CTA - Sala Técnica'];
  const metodos  = ['digital', 'digital', 'cartao+digital'];
  const agora    = Date.now();

  for (let i = 0; i < 150; i++) {
    const p   = pessoas[Math.floor(Math.random() * pessoas.length)];
    const min = Math.floor(Math.random() * 60 * 24 * 7); // até 7 dias atrás
    const dt  = new Date(agora - min * 60000);
    const ts  = dt.toISOString().replace('T', ' ').substring(0, 19);
    db.run(
      `INSERT INTO acessos (id,pessoa_id,tipo,timestamp,local,metodo,autorizado,clip_status)
       VALUES (?,?,?,?,?,?,?,'demo')`,
      [
        uuidv4(),
        p.id,
        Math.random() > 0.45 ? 'entrada' : 'saida',
        ts,
        locais[Math.floor(Math.random() * locais.length)],
        metodos[Math.floor(Math.random() * metodos.length)],
        Math.random() > 0.1 ? 1 : 0,   // ~10% negados
      ]
    );
  }

  // 3 câmeras de teste (URLs públicas ou webcam)
  db.run(`INSERT INTO cameras (id,nome,local,url,tipo) VALUES (?,?,?,?,?)`,
    ['cam1', 'Webcam Local (Teste)', 'Servidor', 'webcam', 'webcam']);
  db.run(`INSERT INTO cameras (id,nome,local,url,tipo) VALUES (?,?,?,?,?)`,
    ['cam2', 'Câmera Pública de Teste', 'Externo', 'https://www.youtube.com/watch?v=ydYDqZQpim8', 'publica']);
  db.run(`INSERT INTO cameras (id,nome,local,url,tipo) VALUES (?,?,?,?,?)`,
    ['cam3', 'Câmera RTSP (Produção)', 'Porta Principal', 'rtsp://192.168.1.101:554/stream1', 'rtsp']);

  salvarBanco();
  console.log('✅ Dados de teste inseridos (150 acessos, 6 militares, 3 câmeras)');
}

// ════════════════════════════════════════════════════════════════════════════
//  ROTAS DE AUTENTICAÇÃO
//  Estas rotas controlam login e logout do painel.
// ════════════════════════════════════════════════════════════════════════════

/**
 * POST /api/auth/login
 * Recebe: { login: "admin", senha: "Admin@2024" }
 * Retorna: dados do usuário logado ou erro
 */
app.post('/api/auth/login', async (req, res) => {
  const { login, senha } = req.body;

  // Validação básica — campos obrigatórios
  if (!login || !senha) {
    return res.status(400).json({ erro: 'Preencha usuário e senha.' });
  }

  // Buscar usuário no banco
  const usuario = dbGet(
    `SELECT * FROM usuarios WHERE login = ? AND ativo = 1`,
    [login.toLowerCase().trim()]
  );

  // Usuário não encontrado (não revelamos qual campo está errado — segurança)
  if (!usuario) {
    return res.status(401).json({ erro: 'Usuário ou senha incorretos.' });
  }

  // Comparar senha enviada com o hash armazenado
  // bcrypt.compare verifica a senha sem precisar decriptografar
  const senhaCorreta = await bcrypt.compare(senha, usuario.senha_hash);
  if (!senhaCorreta) {
    return res.status(401).json({ erro: 'Usuário ou senha incorretos.' });
  }

  // ✅ Login válido — criar sessão
  req.session.usuarioId = usuario.id;
  req.session.nome      = usuario.nome;
  req.session.perfil    = usuario.perfil;
  req.session.login     = usuario.login;

  // Registrar data/hora do último acesso
  dbRun(`UPDATE usuarios SET ultimo_acesso = datetime('now','localtime') WHERE id = ?`, [usuario.id]);

  res.json({
    sucesso: true,
    usuario: {
      id:     usuario.id,
      nome:   usuario.nome,
      login:  usuario.login,
      perfil: usuario.perfil,
    }
  });
});

/**
 * POST /api/auth/logout
 * Destrói a sessão do usuário.
 */
app.post('/api/auth/logout', (req, res) => {
  req.session.destroy(() => {
    res.json({ sucesso: true, mensagem: 'Sessão encerrada.' });
  });
});

/**
 * GET /api/auth/me
 * Retorna os dados do usuário logado (ou 401 se não logado).
 * O frontend usa esta rota para verificar se ainda está autenticado.
 */
app.get('/api/auth/me', autenticar, (req, res) => {
  res.json({
    id:     req.session.usuarioId,
    nome:   req.session.nome,
    perfil: req.session.perfil,
    login:  req.session.login,
  });
});

// ════════════════════════════════════════════════════════════════════════════
//  ROTAS DE ACESSOS (requerem login)
// ════════════════════════════════════════════════════════════════════════════

/**
 * GET /api/acessos
 * Lista acessos com filtros e paginação.
 * Todos os parâmetros são opcionais (query string).
 *
 * Parâmetros aceitos:
 *   nome, matricula, tipo, local, metodo, autorizado,
 *   data_inicio, data_fim, page (padrão=1), limit (padrão=50)
 *
 * Exemplo: /api/acessos?tipo=entrada&page=2&limit=20
 */
app.get('/api/acessos', autenticar, (req, res) => {
  const {
    nome, matricula, tipo, local, metodo, autorizado,
    data_inicio, data_fim, page = 1, limit = 50
  } = req.query;

  // Montamos a cláusula WHERE dinamicamente conforme os filtros enviados
  const where  = [];
  const params = [];

  if (nome)       { where.push("p.nome LIKE ?");      params.push(`%${nome}%`); }
  if (matricula)  { where.push("p.matricula LIKE ?"); params.push(`%${matricula}%`); }
  if (tipo)       { where.push("a.tipo = ?");          params.push(tipo); }
  if (local)      { where.push("a.local LIKE ?");      params.push(`%${local}%`); }
  if (metodo)     { where.push("a.metodo = ?");        params.push(metodo); }
  if (autorizado !== undefined && autorizado !== '') {
    where.push("a.autorizado = ?");
    params.push(parseInt(autorizado));
  }
  if (data_inicio) { where.push("a.timestamp >= ?"); params.push(data_inicio + ' 00:00:00'); }
  if (data_fim)    { where.push("a.timestamp <= ?"); params.push(data_fim + ' 23:59:59'); }

  const w      = where.length ? 'WHERE ' + where.join(' AND ') : '';
  const offset = (parseInt(page) - 1) * parseInt(limit);

  // Contar total para a paginação
  const cnt = dbGet(
    `SELECT COUNT(*) as total FROM acessos a JOIN pessoas p ON a.pessoa_id=p.id ${w}`,
    params
  );

  // Buscar os registros desta página
  const rows = dbAll(
    `SELECT a.id, a.tipo, a.timestamp, a.local, a.metodo, a.autorizado,
            a.clip_url, a.clip_status,
            p.nome, p.matricula, p.patente, p.departamento
     FROM acessos a
     JOIN pessoas p ON a.pessoa_id = p.id
     ${w}
     ORDER BY a.timestamp DESC
     LIMIT ? OFFSET ?`,
    [...params, parseInt(limit), offset]
  );

  res.json({
    data:   rows,
    total:  cnt ? cnt.total : 0,
    page:   parseInt(page),
    limit:  parseInt(limit),
    paginas: Math.ceil((cnt ? cnt.total : 0) / parseInt(limit))
  });
});

/**
 * POST /api/acessos
 * Registra um novo acesso manualmente (ou recebe do leitor biométrico).
 * Body: { matricula, tipo, local, metodo }
 */
app.post('/api/acessos', autenticar, (req, res) => {
  const { matricula, tipo, local, metodo } = req.body;
  if (!matricula) return res.status(400).json({ erro: 'Matrícula obrigatória.' });

  const pessoa = dbGet(`SELECT id FROM pessoas WHERE matricula = ?`, [matricula]);
  if (!pessoa)   return res.status(404).json({ erro: 'Militar não cadastrado.' });

  const id = uuidv4();
  const ts = new Date().toISOString().replace('T', ' ').substring(0, 19);

  dbRun(
    `INSERT INTO acessos (id,pessoa_id,tipo,timestamp,local,metodo,autorizado,clip_status)
     VALUES (?,?,?,?,?,?,1,'pendente')`,
    [id, pessoa.id, tipo || 'entrada', ts, local || 'Porta Principal', metodo || 'digital']
  );

  res.json({ sucesso: true, id, timestamp: ts });
});

/**
 * GET /api/stats
 * Retorna estatísticas para o dashboard.
 */
app.get('/api/stats', autenticar, (req, res) => {
  const hoje = new Date().toISOString().split('T')[0];
  const iH   = hoje + ' 00:00:00';
  const fH   = hoje + ' 23:59:59';

  const acessosHoje  = dbGet(`SELECT COUNT(*) as n FROM acessos WHERE timestamp >= ?`, [iH]);
  const negados      = dbGet(`SELECT COUNT(*) as n FROM acessos WHERE autorizado=0 AND timestamp >= ?`, [iH]);
  const totalPessoas = dbGet(`SELECT COUNT(*) as n FROM pessoas WHERE ativo=1`);

  const porHora = dbAll(
    `SELECT substr(timestamp,12,2) as hora, COUNT(*) as total
     FROM acessos WHERE timestamp BETWEEN ? AND ?
     GROUP BY hora ORDER BY hora`,
    [iH, fH]
  );

  const ultimos = dbAll(
    `SELECT a.tipo, a.timestamp, a.local, a.autorizado, p.nome, p.patente
     FROM acessos a JOIN pessoas p ON a.pessoa_id=p.id
     ORDER BY a.timestamp DESC LIMIT 8`
  );

  // Distribuição por local (pizza)
  const porLocal = dbAll(
    `SELECT local, COUNT(*) as total FROM acessos GROUP BY local ORDER BY total DESC`
  );

  res.json({
    acessosHoje:    acessosHoje  ? acessosHoje.n  : 0,
    acessosNegados: negados      ? negados.n       : 0,
    totalPessoas:   totalPessoas ? totalPessoas.n  : 0,
    porHora,
    ultimos,
    porLocal,
  });
});

/**
 * GET /api/locais
 * Lista todos os locais distintos cadastrados nos acessos.
 */
app.get('/api/locais', autenticar, (req, res) => {
  const rows = dbAll(`SELECT DISTINCT local FROM acessos ORDER BY local`);
  res.json(rows.map(r => r.local));
});

// ════════════════════════════════════════════════════════════════════════════
//  ROTAS DE CLIPE DE VÍDEO
// ════════════════════════════════════════════════════════════════════════════

/**
 * POST /api/acessos/:id/clip
 * Solicita a geração de clipe de vídeo para um acesso específico.
 *
 * MODO DEMO: Simula processamento e retorna status 'demo'.
 * MODO PRODUÇÃO: Ver INTEGRACAO.md para integrar ffmpeg com câmeras RTSP.
 */
app.post('/api/acessos/:id/clip', autenticar, (req, res) => {
  const acesso = dbGet(
    `SELECT a.*, p.nome FROM acessos a JOIN pessoas p ON a.pessoa_id=p.id WHERE a.id=?`,
    [req.params.id]
  );

  if (!acesso) return res.status(404).json({ erro: 'Acesso não encontrado.' });

  // Se já tem clipe pronto, retornar imediatamente
  if (acesso.clip_status === 'pronto') {
    return res.json({ url: acesso.clip_url, status: 'pronto' });
  }

  // Marcar como processando
  dbRun(`UPDATE acessos SET clip_status='processando' WHERE id=?`, [acesso.id]);

  // ───────────────────────────────────────────────────────────────────────
  // SUBSTITUIÇÃO PARA PRODUÇÃO:
  // Aqui você chama o ffmpeg para capturar o vídeo da câmera.
  // Veja o arquivo docs/INTEGRACAO.md → seção "Câmeras"
  // ───────────────────────────────────────────────────────────────────────
  setTimeout(() => {
    dbRun(
      `UPDATE acessos SET clip_url='DEMO_MODE', clip_status='demo' WHERE id=?`,
      [acesso.id]
    );
  }, 2000);

  res.json({ status: 'processando', mensagem: 'Gerando clipe...' });
});

/**
 * GET /api/acessos/:id/clip
 * Consulta o status do clipe de um acesso.
 */
app.get('/api/acessos/:id/clip', autenticar, (req, res) => {
  const row = dbGet(
    `SELECT clip_url, clip_status FROM acessos WHERE id=?`,
    [req.params.id]
  );
  if (!row) return res.status(404).json({ erro: 'Não encontrado.' });
  res.json(row);
});

// ════════════════════════════════════════════════════════════════════════════
//  ROTAS DE CÂMERAS
// ════════════════════════════════════════════════════════════════════════════

app.get('/api/cameras', autenticar, (req, res) => {
  res.json(dbAll(`SELECT * FROM cameras ORDER BY criado_em`));
});

app.post('/api/cameras', autenticar, apenasAdmin, (req, res) => {
  const { nome, local, url, tipo } = req.body;
  if (!nome || !url) return res.status(400).json({ erro: 'Nome e URL são obrigatórios.' });

  const id = uuidv4();
  dbRun(
    `INSERT INTO cameras (id,nome,local,url,tipo) VALUES (?,?,?,?,?)`,
    [id, nome, local || '', url, tipo || 'rtsp']
  );
  res.json({ sucesso: true, id });
});

app.put('/api/cameras/:id', autenticar, apenasAdmin, (req, res) => {
  const { nome, local, url, tipo, ativa } = req.body;
  dbRun(
    `UPDATE cameras SET nome=?,local=?,url=?,tipo=?,ativa=? WHERE id=?`,
    [nome, local, url, tipo, ativa !== undefined ? ativa : 1, req.params.id]
  );
  res.json({ sucesso: true });
});

app.delete('/api/cameras/:id', autenticar, apenasAdmin, (req, res) => {
  dbRun(`DELETE FROM cameras WHERE id=?`, [req.params.id]);
  res.json({ sucesso: true });
});

// ════════════════════════════════════════════════════════════════════════════
//  ROTAS DE PESSOAS (MILITARES)
// ════════════════════════════════════════════════════════════════════════════

app.get('/api/pessoas', autenticar, (req, res) => {
  const { nome, matricula, departamento } = req.query;
  const where = [], params = [];
  if (nome)        { where.push("nome LIKE ?");        params.push(`%${nome}%`); }
  if (matricula)   { where.push("matricula LIKE ?");   params.push(`%${matricula}%`); }
  if (departamento){ where.push("departamento LIKE ?"); params.push(`%${departamento}%`); }
  const w = where.length ? 'WHERE ' + where.join(' AND ') : '';
  res.json(dbAll(`SELECT * FROM pessoas ${w} ORDER BY nome`, params));
});

app.post('/api/pessoas', autenticar, apenasAdmin, (req, res) => {
  const { nome, matricula, patente, departamento } = req.body;
  if (!nome || !matricula) return res.status(400).json({ erro: 'Nome e matrícula obrigatórios.' });

  const existe = dbGet(`SELECT id FROM pessoas WHERE matricula=?`, [matricula]);
  if (existe) return res.status(409).json({ erro: 'Matrícula já cadastrada.' });

  const id = uuidv4();
  dbRun(
    `INSERT INTO pessoas (id,nome,matricula,patente,departamento) VALUES (?,?,?,?,?)`,
    [id, nome, matricula, patente || '', departamento || '']
  );
  res.json({ sucesso: true, id });
});

app.put('/api/pessoas/:id', autenticar, apenasAdmin, (req, res) => {
  const { nome, matricula, patente, departamento, ativo } = req.body;
  dbRun(
    `UPDATE pessoas SET nome=?,matricula=?,patente=?,departamento=?,ativo=? WHERE id=?`,
    [nome, matricula, patente, departamento, ativo !== undefined ? ativo : 1, req.params.id]
  );
  res.json({ sucesso: true });
});

app.delete('/api/pessoas/:id', autenticar, apenasAdmin, (req, res) => {
  // Soft delete — apenas desativa, não remove para preservar histórico
  dbRun(`UPDATE pessoas SET ativo=0 WHERE id=?`, [req.params.id]);
  res.json({ sucesso: true });
});

// ════════════════════════════════════════════════════════════════════════════
//  ROTAS DE USUÁRIOS DO SISTEMA (admin apenas)
// ════════════════════════════════════════════════════════════════════════════

app.get('/api/usuarios', autenticar, apenasAdmin, (req, res) => {
  const rows = dbAll(`SELECT id,nome,login,perfil,ativo,criado_em,ultimo_acesso FROM usuarios`);
  res.json(rows);
});

app.post('/api/usuarios', autenticar, apenasAdmin, async (req, res) => {
  const { nome, login, senha, perfil } = req.body;
  if (!nome || !login || !senha) return res.status(400).json({ erro: 'Nome, login e senha obrigatórios.' });

  const existe = dbGet(`SELECT id FROM usuarios WHERE login=?`, [login]);
  if (existe) return res.status(409).json({ erro: 'Login já existe.' });

  const hash = await bcrypt.hash(senha, 12);
  const id   = uuidv4();
  dbRun(
    `INSERT INTO usuarios (id,nome,login,senha_hash,perfil) VALUES (?,?,?,?,?)`,
    [id, nome, login.toLowerCase(), hash, perfil || 'operador']
  );
  res.json({ sucesso: true, id });
});

app.put('/api/usuarios/:id/senha', autenticar, async (req, res) => {
  // Um admin pode trocar qualquer senha; operador só pode trocar a própria
  const { senhaAtual, novaSenha } = req.body;
  const ehAdmin    = req.session.perfil === 'admin';
  const ehProprioUser = req.session.usuarioId === req.params.id;

  if (!ehAdmin && !ehProprioUser) {
    return res.status(403).json({ erro: 'Permissão negada.' });
  }

  // Operadores precisam confirmar a senha atual
  if (!ehAdmin) {
    const usuario = dbGet(`SELECT senha_hash FROM usuarios WHERE id=?`, [req.params.id]);
    const ok = await bcrypt.compare(senhaAtual, usuario.senha_hash);
    if (!ok) return res.status(401).json({ erro: 'Senha atual incorreta.' });
  }

  const hash = await bcrypt.hash(novaSenha, 12);
  dbRun(`UPDATE usuarios SET senha_hash=? WHERE id=?`, [hash, req.params.id]);
  res.json({ sucesso: true });
});

app.put('/api/usuarios/:id', autenticar, apenasAdmin, (req, res) => {
  const { nome, perfil, ativo } = req.body;
  dbRun(
    `UPDATE usuarios SET nome=?,perfil=?,ativo=? WHERE id=?`,
    [nome, perfil, ativo !== undefined ? ativo : 1, req.params.id]
  );
  res.json({ sucesso: true });
});

// ════════════════════════════════════════════════════════════════════════════
//  ROTAS DE CONFIGURAÇÕES
// ════════════════════════════════════════════════════════════════════════════

app.get('/api/configuracoes', autenticar, apenasAdmin, (req, res) => {
  const rows = dbAll(`SELECT * FROM configuracoes`);
  const cfg  = {};
  rows.forEach(r => cfg[r.chave] = r.valor);
  res.json(cfg);
});

app.put('/api/configuracoes', autenticar, apenasAdmin, (req, res) => {
  const configs = req.body; // { chave: valor, ... }
  Object.entries(configs).forEach(([chave, valor]) => {
    db.run(`INSERT OR REPLACE INTO configuracoes (chave,valor) VALUES (?,?)`, [chave, String(valor)]);
  });
  salvarBanco();
  res.json({ sucesso: true });
});

// ════════════════════════════════════════════════════════════════════════════
//  ROTA CATCH-ALL — Serve o frontend para qualquer rota não-API
// ════════════════════════════════════════════════════════════════════════════
app.get('*', (req, res) => {
  res.sendFile(path.join(__dirname, 'index.html'));
});

// ════════════════════════════════════════════════════════════════════════════
//  INICIALIZAÇÃO DO SERVIDOR
// ════════════════════════════════════════════════════════════════════════════
inicializarBanco().then(() => {
  app.listen(PORT, () => {
    console.log('\n' + '═'.repeat(55));
    console.log('  5º CTA — Sistema de Controle de Acesso');
    console.log('  10ª Brigada de Infantaria');
    console.log('═'.repeat(55));
    console.log(`\n  🟢 Servidor: http://localhost:${PORT}`);
    console.log(`  📂 Banco:    ${DB_FILE}`);
    console.log(`  📁 Clipes:   ${CLIPS_DIR}`);
    console.log('\n  ⚡ Abra http://localhost:3001 no navegador');
    console.log('═'.repeat(55) + '\n');
  });
}).catch(err => {
  console.error('❌ Erro fatal ao inicializar:', err);
  process.exit(1);
});
