# 🔌 GUIA DE INTEGRAÇÃO
## Como conectar o sistema ao leitor ANVIZ e câmeras reais

---

## PARTE 1 — INTEGRAÇÃO COM O LEITOR BIOMÉTRICO ANVIZ

### Como os dados chegam ao sistema

O leitor ANVIZ TC550/T50M guarda os registros de acesso internamente.
Para que apareçam no nosso painel, precisamos "puxar" esses dados.
Existem 3 formas de fazer isso:

---

### Método A — Banco de Dados do Software ANVIZ (mais simples)

O software ANVIZ (que você provavelmente já tem instalado) cria um arquivo
chamado `Att2003.mdb` no computador. Este arquivo é um banco de dados
Microsoft Access com todos os registros.

**Onde encontrar o arquivo:**
- Pasta padrão: `C:\Program Files\Anviz\`
- Ou procure "Att2003.mdb" no Windows Explorer

**Como fazer a sincronização:**

1. Instale a biblioteca para ler arquivos .mdb:
   ```
   cd backend
   npm install mdb-reader
   ```

2. Crie um arquivo `sync-anviz.js` na pasta `backend/scripts/`:
   ```javascript
   // sync-anviz.js — Importa registros do software ANVIZ para nosso banco
   
   const MDBReader = require('mdb-reader');
   const fs        = require('fs');
   const path      = require('path');
   const { v4: uuidv4 } = require('uuid');
   
   // ⚠️ AJUSTE AQUI: caminho do arquivo do software ANVIZ no seu computador
   const CAMINHO_MDB = 'C:\\Program Files\\Anviz\\Att2003.mdb';
   
   async function sincronizar(db) {
     console.log('Lendo arquivo ANVIZ:', CAMINHO_MDB);
     
     const buffer  = fs.readFileSync(CAMINHO_MDB);
     const reader  = new MDBReader(buffer);
     
     // Nome da tabela de registros no ANVIZ
     // Pode ser: CHECKINOUT, att_log, ou outro — verifique no software
     const tabela   = reader.getTable('CHECKINOUT');
     const registros = tabela.getData();
     
     let importados = 0;
     
     for (const r of registros) {
       // Campos típicos do ANVIZ:
       // r.USERID    = matrícula do usuário
       // r.CHECKTIME = data e hora do acesso
       // r.CHECKTYPE = 0 = entrada, 1 = saída
       
       const matricula = String(r.USERID).padStart(6, '0');
       const timestamp = r.CHECKTIME; // formato: 'YYYY-MM-DD HH:MM:SS'
       const tipo      = r.CHECKTYPE === 0 ? 'entrada' : 'saida';
       
       // Busca a pessoa pela matrícula
       // (use a função dbGet do server.js)
       
       console.log(`Importando: ${matricula} - ${tipo} - ${timestamp}`);
       importados++;
     }
     
     console.log(`✅ ${importados} registros importados`);
   }
   
   module.exports = { sincronizar };
   ```

3. No `server.js`, adicione a sincronização automática a cada 5 minutos.
   Localize o comentário `// SINCRONIZAÇÃO AUTOMÁTICA` e adicione:
   ```javascript
   const { sincronizar } = require('./scripts/sync-anviz');
   
   // Sincronizar a cada 5 minutos
   setInterval(() => {
     sincronizar(db).catch(console.error);
   }, 5 * 60 * 1000);
   ```

---

### Método B — Comunicação TCP/IP direta com o leitor (avançado)

O leitor ANVIZ se comunica via rede local na porta 5005 por padrão.
Isso permite puxar os dados diretamente, sem precisar do software ANVIZ.

**Configurar o leitor:**
1. No display do TC550, acesse: Menu → Comunicação → IP
2. Configure o IP do leitor (ex: 192.168.1.100)
3. Certifique-se que o leitor e o computador estão na mesma rede

**Código para conectar:**
```javascript
// No server.js, adicione uma rota para buscar dados do leitor

const net = require('net'); // já vem com o Node.js, sem precisar instalar

app.post('/api/sync-leitor', autenticar, apenasAdmin, (req, res) => {
  const IP_LEITOR = '192.168.1.100'; // ⚠️ Troque pelo IP do seu leitor
  const PORTA    = 5005;
  
  const client = new net.Socket();
  const dados  = [];
  
  client.connect(PORTA, IP_LEITOR, () => {
    console.log('Conectado ao leitor ANVIZ');
    
    // Comando para baixar novos registros
    // (protocolo ANVIZ série 2000)
    const cmd = Buffer.from([0x40, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0b]);
    client.write(cmd);
  });
  
  client.on('data', (buffer) => {
    dados.push(buffer);
  });
  
  client.on('close', () => {
    console.log(`Recebidos ${dados.length} pacotes`);
    // Processar dados aqui...
    res.json({ sucesso: true, pacotes: dados.length });
  });
  
  client.on('error', (err) => {
    res.status(500).json({ erro: 'Não foi possível conectar: ' + err.message });
  });
  
  // Timeout de 10 segundos
  setTimeout(() => client.destroy(), 10000);
});
```

**Como usar:** Clique em "Sincronizar" no painel (adicione um botão em Configurações).

---

### Método C — Pendrive (USB) — mais fácil, manual

O TC550 permite exportar registros em pendrive.

**Passos:**
1. Insira um pendrive no leitor ANVIZ
2. No display: Menu → Gerenciamento de USB → Backup de registros
3. O arquivo `BAK.KQ` será criado no pendrive
4. Conecte o pendrive ao computador com o servidor
5. Use o botão de importação no painel (a implementar)

**Implementação básica:**
```javascript
// Rota para receber arquivo BAK.KQ via upload
// (requer: npm install multer)

const multer = require('multer');
const upload = multer({ dest: 'uploads/' });

app.post('/api/import-udisk', autenticar, apenasAdmin, upload.single('arquivo'), (req, res) => {
  const buffer = fs.readFileSync(req.file.path);
  
  // Cada registro no BAK.KQ ocupa 12 bytes:
  // Bytes 0-4:  ID do usuário (BCD)
  // Bytes 5-8:  Timestamp Unix
  // Byte  9:    Tipo (0=entrada, 1=saida)
  // Bytes 10-11: Flags
  
  let importados = 0;
  for (let i = 0; i + 12 <= buffer.length; i += 12) {
    const userId   = buffer.readUInt32LE(0);
    const unixTime = buffer.readUInt32LE(5);
    const tipo     = buffer[9] === 0 ? 'entrada' : 'saida';
    const data     = new Date(unixTime * 1000).toISOString().replace('T', ' ').substring(0, 19);
    
    // Inserir no banco...
    importados++;
  }
  
  // Limpar arquivo temporário
  fs.unlinkSync(req.file.path);
  
  res.json({ importados });
});
```

---

## PARTE 2 — INTEGRAÇÃO COM CÂMERAS REAIS

### Como funciona a captura de clipe

Quando você clica "Gerar Clipe" no sistema, o servidor precisa:
1. Saber qual câmera estava ativa no momento do acesso
2. Ir buscar o trecho de vídeo de ±2 minutos
3. Salvar o clipe como arquivo .mp4
4. Disponibilizar para visualização

Para fazer isso, usamos o **ffmpeg** — um programa gratuito de processamento de vídeo.

---

### Passo 1 — Instalar o ffmpeg

**Windows:**
1. Acesse: https://ffmpeg.org/download.html
2. Clique em "Windows builds from gyan.dev"
3. Baixe o arquivo "ffmpeg-git-essentials.7z"
4. Extraia em `C:\ffmpeg\`
5. Adicione ao PATH do Windows:
   - Pesquise "variáveis de ambiente" no Windows
   - Editar variáveis do sistema → PATH → Novo
   - Adicione: `C:\ffmpeg\bin`
6. Abra novo terminal e teste:
   ```
   ffmpeg -version
   ```
   Se aparecer a versão, está instalado ✅

**Instalar biblioteca Node.js para ffmpeg:**
```
cd backend
npm install fluent-ffmpeg
```

---

### Passo 2 — Substituir a função de clipe no server.js

Localize no `server.js` este trecho (na rota `POST /api/acessos/:id/clip`):
```javascript
// ── MODO DEMO ──
setTimeout(() => {
  dbRun(`UPDATE acessos SET clip_url='DEMO_MODE', clip_status='demo' WHERE id=?`, [acesso.id]);
}, 2000);
```

Substitua por:
```javascript
// ── PRODUÇÃO — Captura real com ffmpeg ──────────────────────────────────────

const ffmpeg = require('fluent-ffmpeg');

// Buscar câmeras cadastradas
const cameras = dbAll(`SELECT * FROM cameras WHERE ativa=1 AND tipo='rtsp'`);

if (!cameras.length) {
  dbRun(`UPDATE acessos SET clip_status='erro' WHERE id=?`, [acesso.id]);
  return;
}

// Usar a primeira câmera (ou escolher pela lógica do local)
const camera = cameras[0];

// Calcular início e fim do clipe
const momentoAcesso = new Date(acesso.timestamp.replace(' ', 'T'));
const inicio        = new Date(momentoAcesso.getTime() - 2 * 60 * 1000); // 2 min antes
const duracao       = 4 * 60; // 4 minutos total (2 antes + 2 depois)

// URL RTSP da câmera
// Formato: rtsp://usuario:senha@ip:porta/stream
// Exemplos por fabricante:
//   Intelbras: rtsp://admin:senha@192.168.1.x:554/cam/realmonitor?channel=1&subtype=0
//   Hikvision: rtsp://admin:senha@192.168.1.x:554/Streaming/Channels/101
//   Dahua:     rtsp://admin:senha@192.168.1.x:554/cam/realmonitor?channel=1&subtype=0
const rtspUrl = camera.url;

const nomeArquivo = `clip_${acesso.id}_${Date.now()}.mp4`;
const caminhoSaida = path.join(CLIPS_DIR, nomeArquivo);

// Capturar com ffmpeg em background
ffmpeg(rtspUrl)
  .seekInput(inicio.toISOString()) // ir para o ponto de início
  .duration(duracao)               // capturar X segundos
  .videoCodec('copy')              // copiar sem recodificar (mais rápido)
  .audioCodec('aac')
  .output(caminhoSaida)
  .on('end', () => {
    const url = `/clips/${nomeArquivo}`;
    dbRun(
      `UPDATE acessos SET clip_url=?, clip_status='pronto' WHERE id=?`,
      [url, acesso.id]
    );
    console.log(`✅ Clipe gerado: ${caminhoSaida}`);
  })
  .on('error', (err) => {
    console.error('❌ Erro no ffmpeg:', err);
    dbRun(`UPDATE acessos SET clip_status='erro' WHERE id=?`, [acesso.id]);
  })
  .run();
```

---

### Câmeras com NVR (gravação contínua) — RECOMENDADO

Se o batalhão tem um **NVR** (gravador de rede), é possível recuperar qualquer
horário histórico — o clipe funciona mesmo solicitado horas depois.

**Para NVR Hikvision:**
```javascript
// URL com horário específico
const urlNvr = `rtsp://admin:senha@192.168.1.x:554/Streaming/tracks/101` +
               `?starttime=${inicio.toISOString().replace(/[:-]/g,'').split('.')[0]}Z` +
               `&endtime=${fim.toISOString().replace(/[:-]/g,'').split('.')[0]}Z`;
```

**Para NVR Intelbras:**
```javascript
// Protocolo ONVIF (instalar: npm install node-onvif)
const onvif = require('node-onvif');
```

---

### Testar câmera com URL pública (sem câmera real)

Para testar o sistema sem câmera real, use um stream público:

1. No painel, vá em **Câmeras → + Adicionar Câmera**
2. Tipo: **Câmera Pública**
3. URL: `https://www.youtube.com/watch?v=ydYDqZQpim8`
   (este é um stream de câmera de cidade que funciona 24h)
4. Salve

O sistema consegue exibir o stream ao vivo.
Para captura de clipes, o ffmpeg precisa de streams RTSP.

---

## PARTE 3 — COLOCAR NA REDE LOCAL DO BATALHÃO

Para que outros computadores da rede acessem o sistema:

**1. Descobrir o IP do servidor:**
```
ipconfig
```
Anote o IP na linha "Endereço IPv4" — exemplo: `192.168.1.50`

**2. Abrir a porta no Firewall:**
- Windows + R → `firewall.cpl` → Enter
- "Regras de Entrada" → "Nova Regra"
- Tipo: Porta → TCP → Porta: `3001` → Permitir → Salvar

**3. Outros computadores acessam por:**
```
http://192.168.1.50:3001
```

**4. Atualizar o endereço no frontend (opcional):**
Se o frontend estiver em outro computador que não o servidor, abra
`frontend/index.html`, linha 3 do JavaScript:
```javascript
const API = 'http://192.168.1.50:3001/api'; // substitua pelo IP do servidor
```

---

*Documento técnico — 5º CTA / 10ª Brigada de Infantaria*
