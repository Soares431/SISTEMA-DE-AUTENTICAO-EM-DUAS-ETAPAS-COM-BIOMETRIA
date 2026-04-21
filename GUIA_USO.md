# 📘 GUIA DE USO COMPLETO
## 5º CTA — Sistema de Controle de Acesso
### Versão 2.0 — Para iniciantes absolutos

---

## ÍNDICE
1. O que é este sistema e como funciona
2. Instalação (primeiro uso)
3. Criando o primeiro administrador
4. Iniciando o servidor
5. Fazendo o primeiro login
6. Usando cada função do sistema
7. Testando tudo (checklist completo)
8. Resolvendo problemas comuns

---

## 1. O QUE É ESTE SISTEMA E COMO FUNCIONA

### O que ele faz
Este sistema é um **painel de controle web** que:
- Exibe todo o **histórico de entradas e saídas** do batalhão
- Permite **filtrar registros** por nome, data, tipo, local etc.
- Mostra as **câmeras** de segurança cadastradas
- Gera **clipes de vídeo** de ±2 minutos de cada acesso
- Controla **quem pode acessar** o painel (login com senha)

### Como as partes se conectam
```
[Leitor Biométrico ANVIZ]
        ↓ envia dados
[Backend (server.js)]  ←──→  [Banco de Dados (cta_acesso.db)]
        ↑
        ↓ responde
[Navegador (index.html)]
        ↑
  [Você usa aqui]
```

### O que é o "backend"
O backend (`server.js`) é um programa que roda no computador e age como um
intermediário entre o banco de dados e o navegador.
Pense nele como um **garçom** que recebe pedidos (do navegador) e busca os
dados (no banco) para devolver.

### O que é o banco de dados
O arquivo `cta_acesso.db` guarda todos os dados: militares, acessos, câmeras, usuários.
Ele é um arquivo único — você pode fazer backup simplesmente copiando este arquivo.

---

## 2. INSTALAÇÃO (PRIMEIRO USO)

### Passo 1 — Instalar o Node.js

O Node.js é o programa que vai rodar o backend.

1. Abra o navegador e acesse: **https://nodejs.org**
2. Clique no botão verde grande que diz **"LTS"** (versão recomendada)
3. Baixe o instalador para Windows
4. Execute o instalador (clique em Next, Next, Install...)
5. Ao final, clique em **Finish**

**Como verificar se instalou corretamente:**
1. Pressione `Windows + R`
2. Digite `cmd` e pressione Enter
3. Na janela preta que abrir, digite:
   ```
   node --version
   ```
4. Deve aparecer algo como: `v20.11.0`
   Se aparecer, está instalado corretamente ✅

### Passo 2 — Extrair os arquivos do sistema

1. Pegue o arquivo ZIP do sistema (cta-acesso.zip)
2. Clique com botão direito → **Extrair aqui**
3. Você deve ter esta estrutura de pastas:
   ```
   cta-acesso/
   ├── backend/
   │   ├── server.js
   │   ├── package.json
   │   └── scripts/
   │       └── criar-admin.js
   ├── frontend/
   │   └── index.html
   └── docs/
       ├── GUIA_USO.md       ← este arquivo
       ├── INTEGRACAO.md
       └── MODIFICACAO.md
   ```

### Passo 3 — Instalar as dependências do sistema

1. Abra a pasta `backend` no Windows Explorer
2. Na barra de endereço do Explorer, clique e digite `cmd` e pressione Enter
   (isso abre o terminal JÁ NA PASTA CERTA)
3. No terminal, digite o comando abaixo e pressione Enter:
   ```
   npm install
   ```
4. Aguarde. Vai aparecer muito texto rolando na tela — isso é normal.
5. Ao final, deve aparecer algo como `added 120 packages` — isso significa sucesso ✅

**O que este comando faz?**
Ele lê o arquivo `package.json` e baixa todas as bibliotecas que o sistema precisa.
É como instalar as peças antes de montar um móvel.

---

## 3. CRIANDO O PRIMEIRO ADMINISTRADOR

Antes de usar o sistema, você precisa criar o usuário administrador.
**Este passo só precisa ser feito UMA VEZ.**

1. Com o terminal aberto na pasta `backend`, digite:
   ```
   node scripts/criar-admin.js
   ```
2. Pressione Enter
3. Deve aparecer:
   ```
   ✅ Administrador criado com sucesso!
   
   ┌─────────────────────────────────────────┐
   │  CREDENCIAIS DE ACESSO                  │
   │                                         │
   │  Usuário:  admin                        │
   │  Senha:    Admin@2024                   │
   │                                         │
   │  ⚠️  TROQUE A SENHA NO PRIMEIRO LOGIN!  │
   └─────────────────────────────────────────┘
   ```

✅ Anote essas credenciais! Você vai precisar delas para o primeiro login.

---

## 4. INICIANDO O SERVIDOR

**Toda vez** que quiser usar o sistema, você precisa iniciar o servidor.

1. Abra o terminal na pasta `backend` (como explicado no Passo 3 da instalação)
2. Digite:
   ```
   node server.js
   ```
3. Pressione Enter
4. Deve aparecer:
   ```
   ═══════════════════════════════════════════════════════
     5º CTA — Sistema de Controle de Acesso
     10ª Brigada de Infantaria
   ═══════════════════════════════════════════════════════
   
     🟢 Servidor: http://localhost:3001
     📂 Banco:    C:\...\backend\cta_acesso.db
   
     ⚡ Abra http://localhost:3001 no navegador
   ═══════════════════════════════════════════════════════
   ```
5. **Não feche esta janela!** O servidor precisa ficar aberto enquanto você usa o sistema.

### Para parar o servidor
Pressione `Ctrl + C` no terminal.

### Dica: Criar atalho rápido
Crie um arquivo chamado `iniciar.bat` dentro da pasta `backend` com o conteúdo:
```
@echo off
echo Iniciando 5 CTA - Sistema de Controle de Acesso...
node server.js
pause
```
Assim você pode dar duplo clique neste arquivo para iniciar o servidor.

---

## 5. FAZENDO O PRIMEIRO LOGIN

1. Com o servidor rodando, abra o navegador (Chrome, Firefox ou Edge)
2. Na barra de endereço, digite:
   ```
   http://localhost:3001
   ```
3. Pressione Enter
4. A tela de login aparecerá
5. Digite:
   - **Usuário:** `admin`
   - **Senha:** `Admin@2024`
6. Clique em **Entrar**

Se aparecer o dashboard, o login funcionou! ✅

### ⚠️ IMPORTANTE: Troque a senha padrão
1. Clique em **Configurações** no menu lateral
2. Role até **"Alterar Minha Senha"**
3. Digite a senha atual (`Admin@2024`)
4. Digite e confirme sua nova senha
5. Clique em **Salvar Nova Senha**

---

## 6. USANDO CADA FUNÇÃO

### 6.1 Dashboard
A tela inicial. Mostra:
- **Acessos Hoje** — quantos registros houve no dia
- **Acessos Negados** — tentativas bloqueadas
- **Total Cadastrados** — militares no sistema
- **Gráfico por Hora** — distribuição dos acessos ao longo do dia
- **Últimos Registros** — os 8 acessos mais recentes

### 6.2 Histórico de Acessos
Lista todos os registros com filtros.

**Como filtrar:**
1. Preencha os campos desejados (pode usar um ou vários ao mesmo tempo)
2. Clique em **Buscar**
3. A tabela atualiza mostrando apenas os registros que correspondem

**Filtros disponíveis:**
- **Nome** — busca parcial (ex: "ricardo" encontra "Cap. Ricardo Alves")
- **Matrícula** — busca parcial (ex: "123" encontra "001234")
- **Tipo** — Entrada, Saída ou Todos
- **Local** — pelo local do acesso
- **Método** — como a pessoa se identificou
- **Autorização** — mostrar só permitidos, só negados, ou todos
- **Data Início / Fim** — período de tempo

**Botão "Limpar"** — remove todos os filtros e mostra todos os registros.

**Ver Clipe de Vídeo:**
1. Clique no botão **🎬 Ver clipe** na linha do acesso
2. Um painel abre com informações do acesso
3. Clique em **▶ Gerar Clipe**
4. O sistema busca o vídeo das câmeras (±2 min antes e depois)
5. Em modo DEMO, simula o processo

### 6.3 Câmeras
Gerencia as câmeras de segurança.

**Adicionar câmera:**
1. Clique em **+ Adicionar Câmera**
2. Preencha o nome, local e tipo
3. Para testar, use **Câmera Pública** e cole a URL de um stream do YouTube
4. Clique em **Salvar**

**Tipos de câmera:**
- **RTSP** — câmeras IP profissionais na rede local (uso real)
- **HTTP/MJPEG** — algumas câmeras transmitem em HTTP
- **Câmera Pública** — links de YouTube Live (para teste)
- **Webcam do Computador** — webcam conectada ao seu PC (para teste)

**Testar sua webcam:**
1. Role até a seção **"Teste — Webcam do seu computador"**
2. Clique em **Ativar Webcam**
3. O navegador pode pedir permissão — clique em **Permitir**
4. Sua webcam aparecerá na tela ✅

### 6.4 Militares
Gerencia as pessoas cadastradas no sistema biométrico.

**Cadastrar novo militar:**
1. Clique em **+ Cadastrar Militar**
2. Preencha nome, matrícula (obrigatórios), patente e departamento
3. Clique em **Salvar**

**Editar:**
1. Clique no botão **Editar** na linha do militar
2. Faça as alterações
3. Clique em **Salvar**

**Desativar:**
- Clica em **Desativar** — o militar fica invisível para o sistema biométrico
  mas o histórico de acessos dele é preservado

### 6.5 Usuários do Sistema
(Apenas para administradores)
Gerencia quem pode acessar o painel.

**Perfis:**
- **Administrador** — acesso total (pode cadastrar, editar, configurar)
- **Operador** — apenas visualiza (sem poder de edição)

**Criar novo usuário:**
1. Clique em **+ Novo Usuário**
2. Preencha nome, login e senha
3. Escolha o perfil
4. Clique em **Salvar**

### 6.6 Configurações
- **Nome da Instalação** — aparece no cabeçalho
- **Duração da Sessão** — quantas horas antes de pedir login novamente
- **Alterar Minha Senha** — trocar a senha do usuário logado

---

## 7. CHECKLIST DE TESTES

Após instalar, faça estes testes para confirmar que tudo funciona:

### ✅ Teste 1 — Servidor
- [ ] `node server.js` iniciou sem erros
- [ ] Apareceu "🟢 Servidor: http://localhost:3001"

### ✅ Teste 2 — Login
- [ ] Abriu http://localhost:3001 no navegador
- [ ] Tela de login apareceu
- [ ] Login com admin/Admin@2024 funcionou
- [ ] Dashboard apareceu com dados

### ✅ Teste 3 — Dashboard
- [ ] Cards de estatísticas mostram números (não "—")
- [ ] Gráfico de barras aparece
- [ ] Feed de últimos registros mostra eventos

### ✅ Teste 4 — Histórico
- [ ] Tabela carrega com registros
- [ ] Filtro por tipo "entrada" funciona
- [ ] Filtro por data funciona
- [ ] Paginação funciona (se houver mais de 50 registros)

### ✅ Teste 5 — Clipe de vídeo
- [ ] Clicou em "🎬 Ver clipe" em algum registro
- [ ] Modal abriu com informações do acesso
- [ ] Clicou em "▶ Gerar Clipe"
- [ ] Status mudou para "processando"
- [ ] Após ~3 segundos, status mudou para "concluído"

### ✅ Teste 6 — Câmeras
- [ ] Adicionou uma câmera pública (YouTube)
- [ ] A câmera apareceu na grade
- [ ] Ativou a webcam do computador
- [ ] Webcam apareceu e mostrou imagem

### ✅ Teste 7 — Militares
- [ ] Cadastrou um novo militar
- [ ] Militar aparece na lista
- [ ] Editou os dados do militar
- [ ] Desativou o militar

### ✅ Teste 8 — Usuários
- [ ] Criou um novo usuário com perfil "operador"
- [ ] Fez logout
- [ ] Fez login com o novo usuário
- [ ] Verificou que itens de admin estão ocultos

### ✅ Teste 9 — Segurança
- [ ] Tentou acessar http://localhost:3001/api/acessos sem login
- [ ] Recebeu erro "401 - Acesso negado"
- [ ] Sessão expirou corretamente após logout

---

## 8. RESOLVENDO PROBLEMAS COMUNS

### Problema: "node não é reconhecido como comando"
**Causa:** Node.js não foi instalado corretamente
**Solução:**
1. Reinstale o Node.js de https://nodejs.org
2. Reinicie o computador após instalar
3. Tente novamente

### Problema: "Cannot find module 'sql.js'"
**Causa:** As dependências não foram instaladas
**Solução:**
```
cd backend
npm install
```

### Problema: "Porta 3001 já está em uso"
**Causa:** Outra instância do servidor está rodando
**Solução 1:** Feche o outro terminal com o servidor
**Solução 2:** Reinicie o computador
**Solução 3:** Abra o Gerenciador de Tarefas → Processos → procure "node" → Finalizar tarefa

### Problema: "Não consegue conectar ao servidor" no login
**Cause:** O servidor não está rodando
**Solução:**
1. Verifique se o terminal com `node server.js` está aberto
2. Se estiver fechado, inicie novamente
3. Verifique se aparece "🟢 Servidor rodando"

### Problema: Login diz "usuário ou senha incorretos"
**Causa:** Credenciais erradas ou admin não foi criado
**Solução:**
1. Certifique-se de ter rodado `node scripts/criar-admin.js`
2. O usuário padrão é `admin` (tudo minúsculo)
3. A senha padrão é `Admin@2024` (atenção às maiúsculas e @)

### Problema: Webcam não ativa
**Causa:** Permissão negada ou webcam em uso
**Solução:**
1. Verifique se outro programa não está usando a webcam (Skype, Teams, etc.)
2. Ao clicar "Ativar Webcam", aceite a permissão no navegador
3. Verifique se a URL é http://localhost (não file://)

---

*Documento técnico — 5º CTA / 10ª Brigada de Infantaria*
*Última atualização: 2024*
