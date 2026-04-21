# 🔧 GUIA DE MODIFICAÇÃO DO SISTEMA
## Como alterar qualquer parte do sistema

Este guia explica, em linguagem simples, como fazer as modificações mais
comuns no sistema sem precisar de experiência em programação.

---

## ESTRUTURA DOS ARQUIVOS — ONDE ESTÁ CADA COISA

```
cta-acesso/
│
├── backend/
│   ├── server.js          ← Todo o comportamento do servidor
│   │                         (rotas, banco de dados, autenticação)
│   ├── package.json       ← Lista de bibliotecas instaladas
│   ├── cta_acesso.db      ← O banco de dados (gerado automaticamente)
│   ├── clips/             ← Pasta onde os clipes de vídeo são salvos
│   └── scripts/
│       └── criar-admin.js ← Script de criação do admin inicial
│
├── frontend/
│   └── index.html         ← TODO o visual e interações do site
│                            (HTML + CSS + JavaScript em um só arquivo)
│
└── docs/
    ├── GUIA_USO.md        ← Como usar o sistema
    ├── INTEGRACAO.md      ← Como conectar leitor e câmeras
    └── MODIFICACAO.md     ← Este arquivo
```

---

## COMO EDITAR OS ARQUIVOS

Use qualquer editor de texto. Recomendações:
- **VS Code** (gratuito, melhor opção): https://code.visualstudio.com
- **Notepad++** (gratuito, simples): https://notepad-plus-plus.org
- **Bloco de Notas** do Windows (já instalado, mas limitado)

**Após editar qualquer arquivo:**
1. Salve o arquivo (Ctrl + S)
2. Se editou `server.js`: reinicie o servidor (Ctrl+C no terminal, depois `node server.js`)
3. Se editou `index.html`: apenas recarregue o navegador (F5)

---

## MODIFICAÇÕES COMUNS

### 1. Trocar o nome da instalação

**No frontend (aparece na tela de login e sidebar):**
Abra `frontend/index.html`, procure por "5º CTA" e substitua pelo nome desejado.

Locais onde aparece (busque e substitua todos):
```
5º CTA — 10ª Brigada de Infantaria
10ª Brigada de Infantaria
5º CTA
```

**Via interface (sem editar código):**
1. Faça login como admin
2. Vá em Configurações
3. Altere o campo "Nome da Instalação"

---

### 2. Mudar a porta do servidor (padrão: 3001)

Abra `backend/server.js`, localize:
```javascript
const PORT = 3001;
```
Troque 3001 pelo número desejado (ex: 8080):
```javascript
const PORT = 8080;
```
Reinicie o servidor. Agora acesse `http://localhost:8080`.

---

### 3. Adicionar um novo campo no cadastro de militares

**Exemplo: adicionar campo "Telefone"**

**Passo 1 — No banco de dados (server.js)**
Localize o CREATE TABLE de pessoas e adicione o campo:
```javascript
db.run(`CREATE TABLE IF NOT EXISTS pessoas (
  id            TEXT PRIMARY KEY,
  nome          TEXT NOT NULL,
  matricula     TEXT UNIQUE NOT NULL,
  patente       TEXT,
  departamento  TEXT,
  telefone      TEXT,          ← ADICIONAR ESTA LINHA
  ativo         INTEGER DEFAULT 1,
  criado_em     TEXT DEFAULT (datetime('now','localtime'))
)`);
```
⚠️ Se o banco já existir, adicione a coluna manualmente rodando no terminal:
```
node -e "
const initSqlJs = require('sql.js');
const fs = require('fs');
initSqlJs().then(SQL => {
  const db = new SQL.Database(fs.readFileSync('cta_acesso.db'));
  db.run('ALTER TABLE pessoas ADD COLUMN telefone TEXT');
  fs.writeFileSync('cta_acesso.db', Buffer.from(db.export()));
  console.log('Coluna adicionada!');
  process.exit(0);
});
"
```

**Passo 2 — Na rota POST /api/pessoas (server.js)**
Localize e adicione o campo:
```javascript
app.post('/api/pessoas', autenticar, apenasAdmin, (req, res) => {
  const { nome, matricula, patente, departamento, telefone } = req.body; // ← adicionar 'telefone'
  
  dbRun(
    `INSERT INTO pessoas (id,nome,matricula,patente,departamento,telefone) VALUES (?,?,?,?,?,?)`, // ← adicionar campo e ?
    [id, nome, matricula, patente || '', departamento || '', telefone || ''] // ← adicionar valor
  );
```

**Passo 3 — No modal de militares (index.html)**
Localize o modal com id="modal-pessoa" e adicione o campo HTML:
```html
<div class="form-group">
  <label>Telefone</label>
  <input type="text" id="pes-tel" placeholder="ex: (81) 9999-9999">
</div>
```

**Passo 4 — Na função salvarPessoa (index.html)**
```javascript
async function salvarPessoa() {
  const nome       = document.getElementById('pes-nome').value.trim();
  const matricula  = document.getElementById('pes-mat').value.trim();
  const patente    = document.getElementById('pes-pat').value.trim();
  const departamento = document.getElementById('pes-dep').value.trim();
  const telefone   = document.getElementById('pes-tel').value.trim(); // ← ADICIONAR
  
  await api('POST', '/pessoas', { nome, matricula, patente, departamento, telefone }); // ← adicionar telefone
```

**Passo 5 — Na tabela de militares (index.html)**
```html
<!-- No cabeçalho da tabela, adicionar: -->
<th>Telefone</th>

<!-- Na linha de cada militar, adicionar: -->
<td>${p.telefone || '—'}</td>
```

---

### 4. Mudar tempo de expiração da sessão

Abra `backend/server.js`, localize:
```javascript
maxAge: 8 * 60 * 60 * 1000 // 8 horas
```
Troque o `8` pelo número de horas desejado.

---

### 5. Adicionar um novo tipo de acesso/local

Os locais são inseridos quando os acessos são registrados.
Para adicionar um local padrão nos dados de teste, no `server.js` localize:
```javascript
const locais = ['Porta Principal', 'Sala Servidores', 'Almoxarifado', 'CTA - Sala Técnica'];
```
E adicione o novo local à lista.

---

### 6. Mudar as cores do sistema

Abra `frontend/index.html`, localize a seção `:root` no CSS (início do arquivo):
```css
:root {
  --bg:         #f0f2f4;    /* cor do fundo geral */
  --surface:    #ffffff;    /* cor dos cards */
  --accent:     #2c4a2e;   /* cor de destaque (verde oliva) */
  --accent2:    #3d6b40;   /* cor de destaque mais clara */
  --danger:     #b52a2a;   /* cor de alerta/negado */
  --info:       #1a4a7a;   /* cor informativa */
```

Para mudar a cor principal (verde oliva) para azul, por exemplo:
```css
--accent:     #1a3a5c;   /* azul escuro */
--accent2:    #2a5a8c;   /* azul médio */
```

---

### 7. Exportar dados para Excel/CSV

Adicione um botão de exportação no histórico:

**No frontend (index.html):**
Localize o `filter-actions` e adicione:
```html
<button class="btn btn-secondary" onclick="exportarCSV()">⬇ Exportar CSV</button>
```

**A função JavaScript:**
```javascript
async function exportarCSV() {
  // Busca TODOS os registros com os filtros atuais (sem paginação)
  const params = new URLSearchParams({
    nome:       document.getElementById('f-nome').value,
    // ... outros filtros ...
    limit: 9999 // sem limite
  });
  
  const d = await api('GET', `/acessos?${params}`);
  if (!d) return;
  
  // Montar CSV
  const linhas = [
    'Nome,Matrícula,Tipo,Data/Hora,Local,Método,Status', // cabeçalho
    ...d.data.map(a =>
      `"${a.nome}","${a.matricula}","${a.tipo}","${formatarData(a.timestamp)}","${a.local}","${a.metodo}","${a.autorizado ? 'Autorizado' : 'Negado'}"`
    )
  ];
  
  const csv = linhas.join('\n');
  const blob = new Blob(['\uFEFF' + csv], { type: 'text/csv;charset=utf-8' }); // \uFEFF para acentos no Excel
  const url  = URL.createObjectURL(blob);
  const a    = document.createElement('a');
  a.href     = url;
  a.download = `acessos_${new Date().toISOString().split('T')[0]}.csv`;
  a.click();
  URL.revokeObjectURL(url);
  toast('Exportação concluída!', 'success');
}
```

---

### 8. Adicionar registro manual de acesso pelo painel

No arquivo `index.html`, adicione um botão no topo do histórico:
```html
<button class="btn btn-primary" onclick="abrirModalRegistro()">+ Registrar Acesso Manual</button>
```

Modal e função:
```javascript
function abrirModalRegistro() {
  // Crie um modal simples com campos: matrícula, tipo, local
  abrirModal('modal-registro');
}

async function registrarAcesso() {
  const matricula = document.getElementById('reg-mat').value.trim();
  const tipo      = document.getElementById('reg-tipo').value;
  const local     = document.getElementById('reg-local').value;
  
  try {
    await api('POST', '/acessos', { matricula, tipo, local });
    toast('Acesso registrado.', 'success');
    fecharModal('modal-registro');
    buscarAcessos(1); // recarregar tabela
  } catch (e) {
    toast(e.message, 'error');
  }
}
```

---

## COMO FAZER BACKUP DO SISTEMA

O banco de dados é um único arquivo: `backend/cta_acesso.db`

**Backup manual:**
1. Pare o servidor (Ctrl+C)
2. Copie o arquivo `cta_acesso.db` para um pendrive ou outra pasta
3. Reinicie o servidor

**Backup automático com script:**
Crie um arquivo `backup.bat` na pasta `backend`:
```batch
@echo off
set DATA=%date:~6,4%-%date:~3,2%-%date:~0,2%
copy cta_acesso.db "C:\Backups\cta_acesso_%DATA%.db"
echo Backup criado: cta_acesso_%DATA%.db
```

**Restaurar backup:**
1. Pare o servidor
2. Substitua o arquivo `cta_acesso.db` pelo backup desejado
3. Reinicie o servidor

---

## COMO LIMPAR OS DADOS DE TESTE

Os dados fictícios inseridos automaticamente podem ser removidos:

1. Abra o terminal na pasta `backend`
2. Digite:
```
node -e "
const initSqlJs = require('sql.js');
const fs = require('fs');
initSqlJs().then(SQL => {
  const db = new SQL.Database(fs.readFileSync('cta_acesso.db'));
  db.run('DELETE FROM acessos WHERE clip_status = \'demo\'');
  db.run('DELETE FROM pessoas WHERE matricula IN (\'001234\',\'002345\',\'003456\',\'004567\',\'005678\',\'006789\')');
  fs.writeFileSync('cta_acesso.db', Buffer.from(db.export()));
  console.log('Dados de teste removidos!');
  process.exit(0);
});
"
```

---

*Documento técnico — 5º CTA / 10ª Brigada de Infantaria*
