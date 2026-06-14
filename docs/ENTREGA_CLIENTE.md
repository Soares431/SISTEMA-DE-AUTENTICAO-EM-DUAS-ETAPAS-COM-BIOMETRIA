# Entrega no Cliente — 5º CTA

Guia operacional para a entrega do Sistema de Controle de Acesso Biométrico no 5º CTA — 10ª Brigada de Infantaria. Cobre tudo que **não pode ser feito no laboratório** porque depende de hardware físico, rede do quartel, credenciais de produção ou decisão do cliente.

> Documento vivo. Atualize após cada etapa concluída — marcar data e responsável.
> Última atualização: 2026-06-09 (UX de cadastro simplificada — multi-seleção de pessoas, "Adicionar a Ambientes" no perfil, formulários de câmera enxutos)

---

## Índice

1. [Sumário do que depende do cliente](#1-sumário-do-que-depende-do-cliente)
2. [Informações que precisamos coletar antes da entrega](#2-informações-que-precisamos-coletar-antes-da-entrega)
3. [Roteiro de instalação passo a passo](#3-roteiro-de-instalação-passo-a-passo)
4. [Configurações que só o cliente fornece](#4-configurações-que-só-o-cliente-fornece)
5. [Riscos técnicos conhecidos e mitigação](#5-riscos-técnicos-conhecidos-e-mitigação)
6. [Testes de aceitação presenciais](#6-testes-de-aceitação-presenciais)
7. [Treinamento dos administradores](#7-treinamento-dos-administradores)
8. [Pós-entrega — operação contínua](#8-pós-entrega--operação-contínua)
9. [Sign-off](#9-sign-off)
10. **[Como o cliente ativa as partes que faltam (PRODUÇÃO REAL)](#10-como-o-cliente-ativa-as-partes-que-faltam-produção-real)** — **NOVO**
11. [Apêndices](#apêndice-a--scripts-úteis-para-a-entrega)

---

## 1. Sumário do que depende do cliente

Itens que **não** podem ser concluídos antes de estar no ambiente do 5º CTA:

| Item | Por quê | Onde se resolve |
|---|---|---|
| IPs dos dispositivos T50M | Topologia da rede interna do quartel | Tela `/dispositivosT50` |
| Credenciais SMTP do EBMail/Zimbra | Senhas e endereço do servidor em Salvador | Variáveis de ambiente no servidor |
| URLs RTSP, endereços ONVIF e URLs HLS das câmeras | IPs e logins fornecidos pela equipe que instalou as câmeras | Tela `/cameras` e `/ambientes/{id}` |
| Servidor RTSP→HLS (ex: MediaMTX) | Browser não toca RTSP nativo — precisa conversor externo | Instalar no mesmo servidor — ver §10.2 |
| Tipo de fechadura (Fail-Safe vs Fail-Secure) | Verificação física porta a porta | Vistoria com instalador + relatório de risco |
| Posicionamento de câmeras (interna/externa) | Onde a câmera está fisicamente em relação à porta | Vistoria + cadastrar `Tipo` correto |
| Calibrar `TempoEsperaGravacaoSeg` por ambiente | Latência real da rede + tempo de gravação da câmera | Tela `/ambientes/{id}` configuração |
| Cadastro de administradores reais | Decisão de quem terá acesso administrativo | INSERT direto no banco (regra de segurança da doc) |
| Cadastro inicial das pessoas | LGPD + lista oficial do 5º CTA | Cadastro manual via painel |
| Servidor de produção | Hardware do quartel onde Int1/Int2/Int3 vão rodar | Disponibilizado pelo cliente |
| Treinamento dos operadores | Conhecimento operacional do painel | Sessão presencial com `/ajuda` aberta |

---

## 2. Informações que precisamos coletar antes da entrega

Enviar essa lista por escrito ao cliente **uma semana antes** da visita. Sem isso a entrega trava no local.

### 2.1 Rede e dispositivos T50M

Para cada T50M instalado:

- [ ] Nome descritivo (ex: "T50-Entrada-Principal", "T50-Sala-Cofre")
- [ ] Endereço IP fixo na rede local (ex: `192.168.1.50`)
- [ ] Porta TCP (padrão Anviz: `5010`)
- [ ] Localização física (qual porta protege)
- [ ] IP e máscara já configurados no T50M? (Sim/Não)
- [ ] Senha de admin do T50M (para CrossChex se precisar reconfigurar)

### 2.2 Servidor que vai hospedar o sistema

- [ ] Sistema operacional (Windows Server / Windows 10/11)
- [ ] Versão do .NET 8 SDK já instalada? (Sim/Não — se não, levar instalador offline)
- [ ] IP fixo do servidor na rede interna
- [ ] Portas liberadas: `5018` (Int1 API), `8080` (Int3 painel), `8554` (MediaMTX RTSP), `8888` (MediaMTX HLS)
- [ ] Servidor consegue alcançar os T50M na porta TCP 5010?
- [ ] Servidor consegue alcançar as câmeras nas portas RTSP (554) e ONVIF (80/8080)?
- [ ] Servidor consegue alcançar o SMTP Zimbra (EBMail)?

### 2.3 Câmeras

Para cada câmera:

- [ ] Nome descritivo
- [ ] Fabricante e modelo (Hikvision / Dahua / Intelbras VIP / outro)
- [ ] URL RTSP completa (ex: `rtsp://usuario:senha@192.168.1.100:554/Streaming/Channels/101`)
- [ ] Endereço ONVIF (ex: `http://192.168.1.100:80/onvif/device_service`)
- [ ] Usuário e senha ONVIF
- [ ] Tipo (interna: dentro do ambiente / externa: do lado de fora)
- [ ] Ambiente ao qual ela pertence
- [ ] Câmera suporta ONVIF Profile G (Recording Search)? (Sim/Não — se Não, gravação via fallback URL Replay padrão Hikvision)

### 2.4 EBMail / Zimbra

- [ ] Endereço SMTP do servidor Zimbra (ex: `smtp.ebmail.eb.mil.br` — confirmar)
- [ ] Porta SMTP (587 com STARTTLS é o padrão Zimbra)
- [ ] Usuário SMTP (conta institucional do sistema)
- [ ] Senha SMTP
- [ ] Endereço "From" autorizado a enviar emails

### 2.5 Administradores iniciais

Para cada administrador que terá acesso ao painel:

- [ ] Nome completo
- [ ] Login desejado
- [ ] Email
- [ ] Cargo
- [ ] CPF
- [ ] Telefone

> A doc define que admins são criados via INSERT direto no banco — não há tela de cadastro. Levar script SQL pré-preparado.

### 2.6 Lista inicial de pessoas

- [ ] Planilha (CSV ou Excel) com colunas: Nome, CPF, Cargo, Email
- [ ] Quantas pessoas no total?
- [ ] Para qual(is) ambiente(s) cada uma terá acesso?
- [ ] Confirmar que cada pessoa tem email EBMail válido (senão receberá senha exibida no painel)

---

## 3. Roteiro de instalação passo a passo

Tempo estimado total: **1 dia útil** (com lista da seção 2 preenchida).

### Fase 0 — Preparação remota (no laboratório, antes da viagem)

1. Atualizar `appsettings.Production.json` do Int1 com:
   - `JwtKey` único de produção (gerar com `openssl rand -base64 64`)
   - `AesKey` único de produção (32 chars exatos) — **ou** deixar vazio e usar env `AES_KEY`
2. Gerar build de release dos 3 projetos:
   ```powershell
   dotnet publish -c Release -r win-x64 --self-contained false
   ```
3. Copiar para pen drive: pasta `Banco/.../bin/Release/net8.0/`, `Frontend/bin/Release/net8.0/`, `Worker/bin/Release/net8.0/`, `iniciar.ps1`, este documento, planilhas vazias para preencher in loco, **instalador MediaMTX** (binário Windows).
4. Levar instalador offline do .NET 8 SDK (caso o servidor do cliente não tenha internet).

### Fase 1 — Validação do ambiente do cliente

| # | Ação | Como verificar |
|---|---|---|
| 1.1 | Confirmar .NET 8 instalado | `dotnet --version` no PowerShell deve retornar `8.0.x` |
| 1.2 | Confirmar conectividade com cada T50M | `Test-NetConnection 192.168.1.50 -Port 5010` retorna `TcpTestSucceeded: True` |
| 1.3 | Confirmar conectividade com cada câmera (RTSP) | Abrir URL RTSP no VLC funciona |
| 1.4 | Confirmar conectividade SMTP | `Test-NetConnection smtp-host -Port 587` retorna `TcpTestSucceeded: True` |
| 1.5 | Definir variáveis de ambiente | Ver §4.1 |
| 1.6 | Instalar e configurar MediaMTX | Ver §10.2 |

### Fase 2 — Implantação

1. Copiar pastas de build para `C:\5cta\Int1\`, `C:\5cta\Int3\`, `C:\5cta\Int2\`
2. Copiar `banco.db` (ou deixar vazio — Int1 cria automaticamente no primeiro startup)
3. Editar `iniciar.ps1` se os caminhos relativos não baterem
4. Rodar `iniciar.ps1` — confirmar que Int1 sobe em `:5018`, Int3 em `:8080`, Worker conecta
5. Abrir navegador em `http://servidor:8080` e fazer login com `admin` / `Admin@123`
6. **Imediatamente** trocar a senha do admin padrão (`/admins/1` → "Redefinir senha")

### Fase 3 — Cadastro de admins reais

Para cada admin da seção 2.5, executar via DB Browser for SQLite (não há tela de criação por design):

```sql
INSERT INTO administrador (login, senhaHash, nomeCompleto, cpf, email, cargo, telefone, dataCriacao)
VALUES ('login_aqui',
        '<BCrypt hash da senha>',  -- gerar com script do Apêndice A.1
        'Nome Completo', '00000000000', 'email@ebmail', 'Cargo', '(00)00000-0000',
        datetime('now'));
```

Depois de criar os admins reais, **remover ou desabilitar o `admin/Admin@123`** padrão.

### Fase 4 — Cadastro dos dispositivos T50M

1. Logar no painel
2. Acessar `/dispositivosT50` → Novo Dispositivo
3. Para cada T50 da seção 2.1: cadastrar nome, IP, porta `5010`
4. Verificar no Worker (console aberto) se ele consegue conectar

> **Atenção**: a versão atual roda como **OPÇÃO 1B (simulador)**. Para hardware T50M real, ver §10.4.

### Fase 5 — Cadastro de ambientes e câmeras

1. `/ambientes` → Novo ambiente → Nome + selecionar T50 + `TempoEsperaGravacaoSeg` inicial 60s
2. Entrar no detalhe do ambiente → Adicionar câmera:
   - Nome + URL RTSP (com user:pass) + EnderecoONVIF + URL HLS + Tipo (interna/externa)
3. Validar visualização ao vivo (ver §5.4)

### Fase 6 — Teste piloto com 1 pessoa

**Não pular esta fase.**

1. Cadastrar 1 pessoa de teste com email válido
2. Adicionar ao ambiente de teste — confirmar que a fila `T50Pendencia` recebeu a pendência (`SELECT * FROM t50Pendencia WHERE sincronizado = 0`)
3. Após ~10s, confirmar que o Worker marcou `sincronizado = 1`
4. **Físico:** ir até o T50M, digitar ID + senha que o painel mostrou
5. **Resultado esperado:** porta abre, painel mostra tentativa em `/historico` em até 5s
6. Se falhar → ver §5.1 ou §5.2

### Fase 7 — Cadastro em massa

Só depois que a Fase 6 passou:

1. Cadastrar pessoas pelo painel (uma a uma — não há importação CSV ainda)
2. Distribuir senhas: por email (se SMTP funcionar) ou em papel selado (fallback)
3. Vincular cada pessoa aos ambientes corretos

### Fase 8 — Configurar serviços como Windows Service

Para o sistema sobreviver a reinícios do servidor:

```powershell
sc.exe create "5CTA-Int1-API" binPath= "C:\5cta\Int1\WebAbil8-Sistema_Verificação_dupla.slnx.exe" start= auto
sc.exe start "5CTA-Int1-API"

sc.exe create "5CTA-Int2-Worker" binPath= "C:\5cta\Int2\BiometricAcess.Worker.exe" start= auto
sc.exe start "5CTA-Int2-Worker"

sc.exe create "5CTA-Int3-Painel" binPath= "C:\5cta\Int3\FrontendControleAcesso.exe" start= auto
sc.exe start "5CTA-Int3-Painel"
```

Para MediaMTX como serviço, ver §10.2.

---

## 4. Configurações que só o cliente fornece

### 4.1 Variáveis de ambiente do servidor

Devem ser setadas no escopo `Machine` (sobrevive a logoff):

```powershell
[Environment]::SetEnvironmentVariable("SMTP_HOST", "smtp.ebmail.eb.mil.br", "Machine")
[Environment]::SetEnvironmentVariable("SMTP_PORT", "587",                   "Machine")
[Environment]::SetEnvironmentVariable("SMTP_USER", "sistema@ebmail",        "Machine")
[Environment]::SetEnvironmentVariable("SMTP_PASS", "<senha>",               "Machine")
[Environment]::SetEnvironmentVariable("AES_KEY",   "<32 chars exatos>",     "Machine")
```

`AES_KEY` tem prioridade sobre o `appsettings.json` `AesKey`. Trocar a chave em produção **invalida todas as `senhaClear` existentes** — só fazer em sistema vazio.

Depois de setar, **reiniciar os serviços** para as variáveis serem lidas.

### 4.2 `appsettings.Production.json` do Int1

Trocar antes da instalação:

```json
{
  "Jwt": {
    "Key":      "<chave gerada com openssl rand -base64 64>",
    "Issuer":   "5cta-producao",
    "Audience": "5cta-painel"
  },
  "AesKey": "<32 chars únicos — opcional se AES_KEY env definida>",
  "BancoApiUrl": "http://localhost:5018/"
}
```

> `JwtKey` em produção **não pode** ser igual ao de desenvolvimento. Se vazar, qualquer pessoa forja tokens válidos.

### 4.3 `appsettings.json` do Int3 (Painel)

```json
{
  "BancoApiUrl": "http://localhost:5018/"
}
```

Se Int1 e Int3 estiverem em servidores diferentes, trocar `localhost` pelo IP do servidor do Int1.

### 4.4 Configurações editáveis pelo admin no painel

Na tela `/configuracoes`:

- **Retenção de tentativas**: 30 a 180 dias. Padrão 90.
- **Retenção de logs administrativos**: 90 a 365 dias. Padrão 180.
- **Período de inativação automática**: 3 a 24 meses. Padrão 24.
- **Tempo de espera de gravação (padrão global)**: 30 a 120 segundos.

Por ambiente (tela `/ambientes/{id}`):
- **Tempo de espera de gravação**: 30 a 120 segundos. Sobrescreve o global.

---

## 5. Riscos técnicos conhecidos e mitigação

### 5.1 Senha não funciona no T50M

**Sintoma:** pessoa digita ID + senha correta no T50M, mas o display dá negação ou nada acontece.

**Causa provável:** o T50M usa formato proprietário de 3 bytes para senha. O código atual em `AnvizService.cs` faz `ulong.Parse(senha)` direto — pode não ser o formato aceito pelo firmware.

**Como diagnosticar:**
1. Pegar senha que o painel gerou (ex: `100001`)
2. Cadastrar manualmente no Anviz CrossChex (software oficial) a mesma senha pro mesmo ID
3. Se entrar pelo CrossChex e não pelo sistema → bug de formato confirmado

**Solução:** ver patch em §10.4.3.

### 5.2 Worker não conecta no T50M

**Sintoma:** log do Worker mostra `Falha ao conectar ao T50M. Tentando novamente em 10 segundos...` em loop.

| Causa | Verificação |
|---|---|
| IP errado no cadastro | Painel `/dispositivosT50` → conferir |
| Porta bloqueada por firewall | `Test-NetConnection IP -Port 5010` |
| T50M sem energia | Verificar LED do display |
| T50M com IP estático diferente | Acessar CrossChex e ver IP real |
| Cabo Ethernet desconectado | Verificar fisicamente |
| **OPÇÃO 1B (simulador) ainda ativa** | Editar `Worker/Program.cs` — ver §10.4 |

### 5.3 Gravação não aparece no histórico

**Sintoma:** entrada liberada aparece em `/historico`, mas coluna "Gravação" fica como "—".

| Causa | Como resolver |
|---|---|
| Câmera sem `EnderecoONVIF` cadastrado | Tela `/cameras` → editar |
| Câmera fora do ar (não responde ONVIF) | Verificar com ONVIF Device Manager |
| `TempoEsperaGravacaoSeg` curto demais | Aumentar até 120s no ambiente |
| Ambiente sem câmera ativa | Cadastrar câmera no ambiente |
| Câmera de fabricante não suportado pela URL Replay padrão | Verificar §10.3 |
| Acesso negado | Comportamento correto — negados nunca têm gravação (doc §5.11) |

### 5.4 Streaming RTSP ao vivo no painel

**Estado atual:** o modal "Ver ao Vivo" depende de URL HLS (`.m3u8`) configurada por câmera. Browsers **não tocam RTSP nativo**.

**Solução:** instalar **MediaMTX** (gratuito, binário único Windows) no servidor para converter RTSP→HLS. Ver §10.2 para passo a passo completo.

Sem MediaMTX, o modal mostra "URL HLS não configurada" — funcionalidade graceful, sem crash.

### 5.5 Email Zimbra falha

**Sintoma:** ao cadastrar pessoa, o painel mostra a senha em vez de enviá-la por email.

| Causa | Verificação |
|---|---|
| Variáveis SMTP não setadas | `Get-ChildItem Env:SMTP_*` |
| Senha SMTP errada | Testar manualmente no Outlook/Thunderbird |
| Firewall bloqueando porta 587 | `Test-NetConnection smtp-host -Port 587` |
| Conta exige OAuth | Pedir conta SMTP tradicional |

**Comportamento atual:** se SMTP falha, o painel **mostra a senha em alerta + grava no console do servidor** para o admin entregar manualmente. Sem perda de dados.

### 5.6 Limite de 1000 digitais por T50M

**Comportamento esperado** (doc §2.3) — não é erro. O painel oferece cadastrar em modo "Somente Senha" quando T50 atinge 1000.

**Se o cliente quer >1000 com biometria no mesmo ambiente:** o sistema suporta múltiplos T50 por ambiente (tabela `AmbienteT50`). Adicionar outro T50 ao mesmo ambiente pela tela `/ambientes/{id}` — pessoas são copiadas respeitando capacidade.

### 5.7 Banco SQLite corrompido

**Prevenção:**
- Backup diário do `banco.db` (ver Apêndice A.2)
- Não rodar 2 instâncias do Int1 apontando para o mesmo arquivo
- Encerrar serviços com `sc.exe stop` antes de copiar o arquivo

**Recuperação:**
- Parar todos os serviços
- Restaurar último backup
- Reiniciar serviços

### 5.8 Hora do T50M desincronizada

`TimeSyncWorker` no Int2 chama `AnvizService.SincronizarHora()` diariamente. Se houver divergência:
- Verificar no log do Worker se `TimeSyncWorker` executou
- No simulador, é no-op (sem T50 real)

**Workaround imediato:** sincronizar manualmente via CrossChex.

### 5.9 Fechadura Fail-Secure (risco de vida)

Durante teste, simular queda de energia (desligar disjuntor) e verificar se a porta destrava.

**Se não destravar:** fechadura é Fail-Secure — risco de aprisionar pessoas em emergência.

**O que fazer:** **NÃO aceitar a entrega sem o cliente confirmar fechaduras Fail-Safe**, ou ter sistema de bypass mecânico. Registrar no termo de entrega.

### 5.10 Câmera externa não captura tentativas negadas

**Comportamento correto** conforme doc §5.11. A câmera só grava com movimento; tentativa negada não tem movimento dentro do ambiente.

**Sugestão para o cliente:** posicionar câmeras externas cobrindo a área antes da porta — assim mesmo entradas negadas geram gravação da pessoa tentando.

### 5.11 Bug arquitetural pré-existente nas OPÇÕES 2/3 do Worker

`EventProcessor.cs` (T50M) e `EventProcessorArduino.cs` (Arduino) são registrados como Singleton mas recebem repositórios Scoped via construtor → erro de DI no startup quando ativadas.

**Impacto:** afeta apenas se você trocar OPÇÃO 1B → 2 ou 3.

**Mitigação:** ver §10.4.2 para o patch antes de ativar hardware real.

---

## 6. Testes de aceitação presenciais

Estes 9 cenários devem ser executados **com o cliente assistindo** e marcados na ata.

### Teste 1 — Login do admin
- Abrir `http://servidor:8080`
- Logar com credenciais do admin real
- Resultado: redirect para dashboard, nome do admin no topo

### Teste 2 — Primeiro acesso de biometria
- Cadastrar pessoa "Teste001" com modo Digital+Senha
- Adicionar ao Ambiente1
- **Físico:** pessoa vai ao T50M, digita ID + senha
- Display deve mostrar "PLACE FINGER"
- Pessoa coloca o dedo 2 vezes
- Display mostra "OK", porta abre por 15s
- Painel `/historico` registra entrada em até 5s
- Painel `/pessoas/{id}` mostra "Biometria cadastrada"

### Teste 3 — Acesso normal com digital
- Pessoa do Teste 2 vai ao T50M
- Digita ID + coloca o dedo
- Porta abre em menos de 1s
- Painel registra `TipoVerificacao = digital_id`

### Teste 4 — Acesso negado por permissão
- Pessoa cadastrada mas **não vinculada** ao Ambiente2
- Vai ao T50M do Ambiente2, digita ID + senha
- Porta não abre
- Painel registra `MotivoNegacao = sem_permissao`

### Teste 5 — Acesso negado por inatividade
- Pessoa cadastrada, inativada manualmente
- Vai ao T50M
- Porta não abre
- Painel registra `MotivoNegacao = inativo`

### Teste 6 — Pessoa não cadastrada
- Alguém digita ID inexistente no T50M
- Porta não abre
- Painel registra `PessoaId = NULL` e `MotivoNegacao = nao_cadastrado`

### Teste 7 — Gravação ONVIF associada
- Repetir Teste 3 com câmera ativa no ambiente (EnderecoONVIF configurado)
- Aguardar `TempoEsperaGravacaoSeg`
- Painel `/historico` mostra botão "Ver" na coluna Gravação
- Clicar abre modal com URL RTSP de replay (copia URL → abre no VLC)

### Teste 8 — Reenvio de senha
- Admin clica "Reenviar Senha" no perfil de uma pessoa
- Se SMTP OK: pessoa recebe email
- Se SMTP falha: alert "SMTP indisponível — senha exibida no console"

### Teste 9 — Exportação PDF
- Histórico, Logs, Pessoas, Admins, Relatório de Ambiente — todos baixam PDFs válidos
- Validar com Adobe Reader que o conteúdo está correto

---

## 7. Treinamento dos administradores

Duração: **2 horas** com até 5 admins por sessão.

### Agenda

| Tempo | Tópico | Tela |
|---|---|---|
| 10 min | Visão geral do sistema | Dashboard |
| 15 min | Cadastrar nova pessoa, entender ID + senha gerados, modo de acesso fixado no perfil | /pessoas |
| 15 min | Adicionar **várias pessoas de uma vez** a um ambiente (busca + chips) | /ambientes/{id} |
| 10 min | Adicionar **uma pessoa a vários ambientes** (seleção visual com cards no perfil) | /pessoas/{id} |
| 10 min | Inativar pessoa (confirmação dupla) | /pessoas/{id} |
| 10 min | Resetar biometria | /pessoas/{id} |
| 10 min | Reenviar credenciais (ID + senha) por email | /pessoas/{id} |
| 10 min | Cadastrar câmera + visualização ao vivo (HLS) | /cameras |
| 10 min | Consultar histórico com filtros + ver gravação ONVIF | /historico |
| 10 min | Exportar PDFs (incluindo Relatório de Ambiente) | /historico, /ambientes/{id} |
| 10 min | Consultar logs de auditoria | /logs |
| 10 min | Configurações de retenção e tempo de gravação | /configuracoes |
| 10 min | O que fazer quando T50M offline | n/a |
| 10 min | Q&A | n/a |

### Material de apoio
- Tela `/ajuda` do próprio painel (manual embutido)
- Este documento impresso
- Folha A4 com URL do painel e telefones de suporte

### Cuidados a destacar
- **Não compartilhar credenciais de admin**
- **Toda ação fica registrada em logs** (mostrar `/logs` com a ação que ele mesmo acabou de fazer)
- **Inativação remove acesso imediatamente**, não há "desfazer"
- **Senhas das pessoas são exibidas só 1 vez no cadastro** — anotar ou confiar no email
- **Modo de acesso (digital+senha vs somente senha) é decidido no perfil da pessoa, não no vínculo com ambiente** — não há mais escolha por T50 ou por ambiente

### Como o vínculo Pessoa ↔ Ambiente ↔ T50 funciona (regra simplificada)

> Importante para o treinamento: explicar essa regra logo no início pra evitar dúvidas durante o cadastro.

- **Toda pessoa vinculada a um ambiente é cadastrada automaticamente em TODOS os T50 daquele ambiente.**
- **Se um novo T50 é vinculado ao ambiente depois, todas as pessoas já existentes são copiadas para ele** automaticamente (respeitando capacidade de 1000 digitais).
- **Modo de acesso vem do perfil da pessoa:**
  - `digital + senha` → ocupa 1 slot em cada T50 do ambiente
  - `somente senha` → vincula só ao ambiente, **não ocupa slot de nenhum T50**
- **Não existe mais escolha de "em qual T50 cadastrar a digital".** Se o admin quer separar pessoas por T50, precisa criar ambientes diferentes.

---

## 8. Pós-entrega — operação contínua

### Rotinas que precisam ser configuradas

| Item | Frequência | Como configurar |
|---|---|---|
| Backup do `banco.db` | Diário, 02:00 | Windows Task Scheduler (Apêndice A.2) |
| Limpeza de dados expirados | Diário, 03:00 | Já automatizado (Hangfire) |
| Inativação por 2 anos sem acesso | Diário, 03:00 | Já automatizado (Hangfire) |
| Sincronização de hora dos T50M | Diário | Já automatizado (`TimeSyncWorker` no Int2) |
| Verificação de espaço em disco | Semanal | Script PowerShell + alerta SMTP |
| Verificação de logs do Worker | Semanal | Event Viewer / arquivo de log |
| Monitorar fila T50Pendencia com falhas | Semanal | `SELECT * FROM t50Pendencia WHERE tentativasFalhas >= 5` |

### Indicadores para o cliente acompanhar

Acessível em `/` (Dashboard):
- Total de entradas permitidas/negadas por dia
- T50M com mais de 950 digitais (alerta)
- Pessoas ativas no sistema
- Hangfire dashboard em `/hangfire` (do Int1)

### Contato para suporte

- **Responsável técnico:** Gustavo Henrique
- **Email:** gh33493@gmail.com
- **SLA proposto:** 24h úteis para resposta inicial em incidentes não críticos. Críticos (sistema fora do ar): 4h úteis.

---

## 9. Sign-off

### Checklist final antes de assinar a entrega

- [ ] Os 3 serviços rodam como Windows Service e sobem após reboot
- [ ] MediaMTX rodando como Windows Service
- [ ] Todos os T50M conectam (verificar no log do Worker)
- [ ] Pelo menos um administrador real consegue logar
- [ ] **Senha do admin padrão foi trocada ou admin foi removido**
- [ ] Testes 1 a 9 da seção 6 passaram com cliente assistindo
- [ ] Variáveis de ambiente SMTP e AES_KEY setadas em escopo Machine
- [ ] Pelo menos uma pessoa de teste foi cadastrada, recebeu senha, entrou com biometria
- [ ] Backup automático do banco está agendado
- [ ] Treinamento dos admins foi feito
- [ ] Cliente assinou ciente das limitações:
  - [ ] Tipo de fechadura (Fail-Safe) é responsabilidade do cliente
  - [ ] Posicionamento de câmeras externas para capturar negados é responsabilidade do cliente
  - [ ] Câmeras de outros fabricantes que não Hikvision/Dahua/Intelbras podem precisar de URL Replay customizada

### Ata de entrega

| Item | Valor |
|---|---|
| Data da entrega | __/__/____ |
| Local | 5º CTA — 10ª Brigada de Infantaria |
| Responsável técnico (entrega) | Gustavo Henrique |
| Responsável (cliente) | _________________________ |
| Quantidade de T50M instalados | _____ |
| Quantidade de câmeras integradas | _____ |
| Quantidade de pessoas cadastradas | _____ |
| Quantidade de admins criados | _____ |
| Testes da seção 6 que passaram | _____ / 9 |

Assinaturas: ___________________ (técnico) ___________________ (cliente)

---

## 10. Como o cliente ativa as partes que faltam (PRODUÇÃO REAL)

> **Esta seção é o coração da entrega.** O sistema chega no cliente em modo "simulador" funcional. Para ele entrar em produção real (hardware T50M físico + câmeras gravando + emails saindo), é preciso seguir os passos abaixo **na ordem**. Cada passo é independente — você pode adotar parcialmente (ex: ativar email mas deixar câmera para depois).

### Mapa do que está pronto vs. o que precisa ser ativado

| Componente | Estado no laboratório | O que falta para produção |
|---|---|---|
| Painel web, login, JWT, CRUD | ✅ 100% funcional | Nada — só trocar admin padrão |
| Banco SQLite + migrations | ✅ 100% funcional | Nada — cria sozinho no startup |
| Exportações PDF/CSV | ✅ 100% funcional | Nada |
| Hangfire jobs (limpeza, inativação) | ✅ 100% funcional | Nada |
| Validação Senha ≠ ID, CPF duplicado | ✅ 100% funcional | Nada |
| Email Zimbra/EBMail | ⚠️ Fallback console | §10.1 — setar variáveis SMTP |
| **Stream RTSP ao vivo nas câmeras** | ❌ Placeholder | **§10.2 — instalar MediaMTX** |
| **Gravação ONVIF associada a acessos** | ⚠️ Retorna null sem hardware | **§10.3 — cadastrar EnderecoONVIF** |
| **Hardware T50M real** | ⚠️ Roda em OPÇÃO 1B simulador | **§10.4 — trocar para OPÇÃO 3** |
| **Fila T50Pendencia (cadastro automático)** | ✅ Worker processa | Funciona automaticamente quando OPÇÃO 3 ativa |
| Sincronização de hora T50 | ✅ Automatizado diário | Funciona automaticamente quando OPÇÃO 3 ativa |

---

### 10.1 Ativar email Zimbra/EBMail

**Antes:** ao cadastrar pessoa, painel mostra a senha gerada na tela com aviso "SMTP indisponível".

**Depois:** pessoa recebe email automático com sua senha.

**Como fazer:**

1. Obter as credenciais SMTP do EBMail com o setor de TI do EB (item §2.4 da pré-coleta)
2. No servidor de produção, abrir PowerShell **como Administrador**:

   ```powershell
   [Environment]::SetEnvironmentVariable("SMTP_HOST", "smtp.ebmail.eb.mil.br", "Machine")
   [Environment]::SetEnvironmentVariable("SMTP_PORT", "587",                   "Machine")
   [Environment]::SetEnvironmentVariable("SMTP_USER", "sistema@5cta.eb.mil.br", "Machine")
   [Environment]::SetEnvironmentVariable("SMTP_PASS", "<senha-do-zimbra>",     "Machine")
   ```

3. Reiniciar os 3 serviços (Apêndice A.4)

4. **Testar:** cadastrar uma pessoa de teste no painel. O email deve chegar em segundos. Se não chegar, ver §5.5.

5. **Confirmar no health check:**
   ```powershell
   curl http://localhost:5018/health
   # Procurar por "smtp":{"status":"configurado", ...}
   ```

---

### 10.2 Ativar stream RTSP ao vivo no painel (MediaMTX)

**Antes:** modal "Ver ao Vivo" das câmeras mostra "URL HLS não configurada".

**Depois:** modal toca o stream da câmera em tempo real no navegador.

**Por que isso é necessário:** browsers não tocam RTSP nativamente. Precisa de um conversor RTSP→HLS rodando em paralelo.

**Como fazer:**

#### 10.2.1 Instalar MediaMTX

1. Baixar a release Windows mais recente: https://github.com/bluenviron/mediamtx/releases
   - Procurar por `mediamtx_v*_windows_amd64.zip`
2. Extrair em `C:\mediamtx\`
3. Editar `C:\mediamtx\mediamtx.yml`:

   ```yaml
   # Habilita endpoint HLS na porta 8888
   hls: yes
   hlsAddress: :8888
   hlsAllowOrigin: '*'
   hlsAlwaysRemux: yes

   paths:
     # Para cada câmera, criar um path. Exemplo para 3 câmeras:
     entrada:
       source: rtsp://admin:senha@192.168.1.100:554/Streaming/Channels/101
     servidores:
       source: rtsp://admin:senha@192.168.1.101:554/Streaming/Channels/101
     cofre:
       source: rtsp://admin:senha@192.168.1.102:554/Streaming/Channels/101
   ```

4. Testar manualmente: abrir terminal em `C:\mediamtx\` e rodar `mediamtx.exe`. Deve aparecer:
   ```
   INF [HLS] [server] listener opened on :8888
   ```

5. Abrir `http://localhost:8888/entrada/index.m3u8` no VLC — deve tocar o stream.

#### 10.2.2 Instalar MediaMTX como serviço Windows

Para o conversor sobreviver a reboots:

```powershell
sc.exe create "5CTA-MediaMTX" binPath= "C:\mediamtx\mediamtx.exe C:\mediamtx\mediamtx.yml" start= auto
sc.exe start "5CTA-MediaMTX"
```

#### 10.2.3 Cadastrar URL HLS de cada câmera no painel

1. Logar no painel
2. `/cameras` → editar cada câmera
3. Preencher campo **URL HLS** com o endereço HLS do MediaMTX:
   - `http://localhost:8888/entrada/index.m3u8` (para a câmera chamada `entrada` no `mediamtx.yml`)
   - `http://localhost:8888/servidores/index.m3u8`
   - etc.
4. Salvar

#### 10.2.4 Validar

- Abrir `/cameras` → clicar em "Ver ao Vivo" em qualquer câmera
- Modal deve mostrar o vídeo ao vivo em poucos segundos
- Latência típica: 4-8 segundos (limitação do HLS)

> Para latência menor (~1s) considere usar **LL-HLS** ou **WebRTC** — disponíveis no MediaMTX mas precisam configuração adicional.

#### 10.2.5 Referência rápida — formatos de URL aceitos pelo painel

Os formulários `/cameras` (cadastrar/editar câmera) e `/ambientes/{id}` (modal "Adicionar Câmera") aceitam três campos de URL. Os formulários intencionalmente exibem apenas placeholders curtos — a referência completa de formato fica abaixo:

| Campo do formulário | Para que serve | Exemplo de preenchimento |
|---|---|---|
| **URL RTSP** | Stream da câmera, usado pra construir a URL de Replay ONVIF (§10.3). **Inclua usuário:senha** — o sistema extrai dela pra autenticar no ONVIF. | `rtsp://admin:Senha123@192.168.1.100:554/Streaming/Channels/101` |
| **URL HLS** *(opcional)* | URL servida pelo MediaMTX para tocar o stream ao vivo no painel (`/cameras` modal "Ver ao Vivo"). Deixe em branco se não vai usar streaming ao vivo. | `http://localhost:8888/entrada/index.m3u8` |
| **Endereço ONVIF** | Endpoint SOAP da câmera para validar conexão antes de montar URL de Replay. | `http://192.168.1.100:80/onvif/device_service` |

**Webcam local para testes (sem câmera IP):** o sistema atual aceita `dshow://Integrated Camera` no campo URL RTSP **apenas** se você rodar o MediaMTX com um path apontando pra dshow — caso contrário, deixe esse campo como uma RTSP normal e use apenas o painel sem stream ao vivo.

**Câmeras Hikvision (URL Channel padrão):**
- RTSP principal: `rtsp://user:pass@ip:554/Streaming/Channels/101`
- RTSP secundário (qualidade menor): `rtsp://user:pass@ip:554/Streaming/Channels/102`
- ONVIF: `http://ip:80/onvif/device_service`

**Câmeras Dahua:**
- RTSP: `rtsp://user:pass@ip:554/cam/realmonitor?channel=1&subtype=0`
- ONVIF: `http://ip:80/onvif/device_service`

**Câmeras Intelbras VIP:** mesmo padrão da Hikvision (`/Streaming/Channels/101`).

> Se tiver dúvida de qual URL exata a câmera aceita: use o **ONVIF Device Manager** (gratuito) para descobrir RTSP e ONVIF na mesma tela.

---

### 10.3 Ativar gravação ONVIF associada a acessos

**Antes:** acesso liberado registra no `/historico` sem gravação (coluna "Gravação" mostra "—").

**Depois:** acesso liberado fica com link "Ver" que abre o trecho gravado no VLC.

**Como funciona o sistema (§5.11 da doc técnica):**

1. Pessoa abre porta no T50M
2. `EventProcessor` chama `CameraService.MonitorarNovoArquivo(ambienteId, timestamp, tempoEspera)` em background
3. Sistema aguarda `TempoEsperaGravacaoSeg` segundos (30-120s) para a câmera capturar o movimento
4. Sistema conecta na câmera via ONVIF (no `EnderecoONVIF` cadastrado) e valida que ela está online
5. Sistema monta uma **URL RTSP de Replay** no formato padrão Hikvision com o timestamp do acesso
6. URL é gravada em `TentativaAcesso.GravacaoPath`
7. Painel exibe botão "Ver" no histórico — clicar abre modal com a URL pra copiar e abrir no VLC

**Como fazer (3 passos):**

#### 10.3.1 Cadastrar `EnderecoONVIF` em cada câmera

Para cada câmera, descobrir o endereço ONVIF (geralmente `http://IP/onvif/device_service`). Ferramenta útil: **ONVIF Device Manager** (free).

No painel:
1. `/cameras` → editar câmera
2. Preencher **EnderecoONVIF**: `http://192.168.1.100/onvif/device_service`
3. **Garantir que o usuário/senha estão na URL RTSP** (`rtsp://user:pass@ip:554/...`) — o sistema extrai dela pra autenticar no ONVIF
4. Salvar

#### 10.3.2 Confirmar fabricante compatível

A URL Replay padrão usada é a do Hikvision: `rtsp://user:pass@ip:554/Streaming/tracks/101?starttime=YYYYMMDDThhmmssZ&endtime=...`.

Compatível por padrão: **Hikvision, Intelbras VIP, Dahua (firmware recente)**.

Câmeras de outros fabricantes (Axis, Bosch, etc.) podem precisar de URL Replay customizada — editar `InfraestruturaBloco1/Services/CameraService.cs` método `MontarUrlReplay`. Exemplo Dahua antigo:

```csharp
return $"rtsp://{uri.UserInfo}@{uri.Host}:{port}/cam/playback?channel=1&starttime={dataFormatadaDahua}";
```

#### 10.3.3 Validar

- Liberar um acesso de teste no T50M (real ou simulador)
- Aguardar `TempoEsperaGravacaoSeg` (padrão 60s)
- Abrir `/historico` → clicar "Ver" na nova tentativa
- Modal mostra URL RTSP de replay (senha mascarada)
- Copiar URL completa (já desmascarada no banco) e abrir no VLC → vídeo deve tocar

> Se a coluna "Gravação" continuar "—" mesmo com `EnderecoONVIF` configurado, ver §5.3.

---

### 10.4 Ativar hardware T50M Anviz real (sair do simulador)

**Antes:** Worker roda em OPÇÃO 1B (simulador) — gera eventos de acesso aleatórios, não fala com hardware.

**Depois:** Worker conecta nos T50M físicos via TCP/IP, recebe eventos reais e cadastra/remove pessoas via Anviz SDK.

#### 10.4.1 Pré-requisitos

- T50M físicos com IPs configurados na rede (item §2.1 preenchido)
- Conectividade testada (§3, fase 1.2)
- Dispositivos T50 cadastrados no painel `/dispositivosT50`

#### 10.4.2 ⚠️ Antes de ativar: corrigir bug arquitetural pré-existente

`EventProcessor.cs` e `EventProcessorArduino.cs` são registrados como Singleton mas recebem repositórios Scoped. Vai dar erro de DI no startup quando ativados.

**Patch necessário (Worker/Services/EventProcessor.cs e HardwareNosso/EventProcessorArduino.cs):**

Trocar todos os repositórios injetados via construtor por **`IServiceScopeFactory _scopeFactory`** (igual ao `EventProcessorSimuladorBanco.cs`). Cada chamada de método cria um scope e resolve os repositórios dentro.

**Modelo a seguir:** `Hardware & Serviço de Background/BiometricAcess.Worker/BiometricAcess.Worker/Simulador/EventProcessorSimuladorBanco.cs` (já implementado corretamente).

**Esse refactor está planejado** mas não foi feito porque o usuário priorizou outras coisas. Estimativa: 1-2h.

#### 10.4.3 Patch do formato da senha (se Teste 6 falhar)

Em `Worker/Services/AnvizService.cs` método `AdicionarPessoa`:

```csharp
// Atual (pode falhar):
userInfo.Password = ulong.Parse(senha);

// Patch sugerido (formato T50M de 3 bytes — tamanho << 20 | valor):
var senhaNum = ulong.Parse(senha);
var tamanho = (ulong)senha.Length;
userInfo.Password = (tamanho << 20) | senhaNum;
```

#### 10.4.4 Trocar OPÇÃO 1B → OPÇÃO 3 (T50M real)

Em `Hardware & Serviço de Background/BiometricAcess.Worker/BiometricAcess.Worker/Program.cs`:

```csharp
// COMENTAR OPÇÃO 1B (simulador):
// builder.Services.AddSingleton<IAnvizConnector, AnvizConnectorSimulador>();
// builder.Services.AddSingleton<IAnvizService, AnvizServiceSimulador>();
// builder.Services.AddSingleton<IEventProcessor, EventProcessorSimuladorBanco>();

// DESCOMENTAR OPÇÃO 3 (T50M real):
builder.Services.AddSingleton<IAnvizConnector, AnvizConnector>();
builder.Services.AddSingleton<IAnvizService, AnvizService>();
builder.Services.AddSingleton<IEventProcessor, EventProcessor>();
```

Recompilar e reiniciar serviço Worker.

#### 10.4.5 Validar

- Log do Worker mostra `Conectado ao T50M. Buscando eventos armazenados...`
- Cadastrar pessoa de teste no painel — em 10s, fila `T50Pendencia` esvazia (`SELECT * FROM t50Pendencia WHERE sincronizado = 0` retorna vazio)
- Pessoa vai fisicamente ao T50M, digita ID + senha → porta abre, painel registra em até 5s

#### 10.4.6 Sincronização T50 → painel já é automática

Os 2 fluxos críticos rodam sozinhos depois que OPÇÃO 3 está ativa:

| Fluxo | Como funciona |
|---|---|
| **Cadastro/remoção de pessoa no T50** | Frontend enfileira em `T50Pendencia` → `SincronizadorT50Worker` processa a cada 10s via `IAnvizService` |
| **Eventos de acesso (T50 → painel)** | `Worker` faz polling no T50 a cada 2s, `EventProcessor` aplica regras de negócio e registra `TentativaAcesso` |

---

### 10.5 Checklist de "está em produção real?"

Marque conforme avança:

- [ ] **Email**: variáveis SMTP setadas, teste de reenvio chegou no inbox
- [ ] **Streaming ao vivo**: MediaMTX rodando como serviço, URLs HLS cadastradas nas câmeras, modal "Ver ao Vivo" toca o stream
- [ ] **Gravação ONVIF**: EnderecoONVIF cadastrado em todas câmeras ativas, teste de acesso liberado gerou link "Ver" no histórico
- [ ] **Hardware T50M**: OPÇÃO 3 ativa, patch do EventProcessor aplicado (§10.4.2), Worker conectado, fila T50Pendencia esvaziando, evento de acesso real chegou ao painel
- [ ] **Admins reais**: pelo menos 1 admin não-padrão criado via SQL
- [ ] **Admin padrão**: senha trocada (NUNCA deixar `Admin@123`) ou desativado
- [ ] **Backup**: script agendado e validado restaurando para outra pasta

---

## Apêndice A — Scripts úteis para a entrega

### A.1 Gerar hash BCrypt de uma senha

Console app pronto em `docs/scripts/GerarHash`. Roda apenas com o .NET 8 SDK — não precisa instalar `dotnet-script` nem outras ferramentas:

```powershell
cd docs\scripts\GerarHash
dotnet run -- "Admin@123"
```

Saída inclui o hash e um `INSERT` de exemplo pronto pra colar no DBeaver/DB Browser.

### A.2 Backup diário do banco

Salvar como `backup-banco.ps1` e agendar no Task Scheduler:

```powershell
$origem  = "C:\5cta\Banco\banco.db"
$destino = "C:\5cta\Backups\banco-$(Get-Date -Format 'yyyy-MM-dd').db"
Copy-Item $origem $destino
# Mantém apenas os últimos 30 backups
Get-ChildItem "C:\5cta\Backups\banco-*.db" |
    Sort-Object LastWriteTime -Descending |
    Select-Object -Skip 30 |
    Remove-Item
```

### A.3 Verificar conectividade com todos os T50M

```powershell
$ips = @("192.168.1.50", "192.168.1.51", "192.168.1.52")
foreach ($ip in $ips) {
    $r = Test-NetConnection $ip -Port 5010 -WarningAction SilentlyContinue
    "$ip : " + $(if ($r.TcpTestSucceeded) { "OK" } else { "FALHA" })
}
```

### A.4 Restart limpo dos serviços

```powershell
sc.exe stop "5CTA-Int3-Painel"
sc.exe stop "5CTA-Int2-Worker"
sc.exe stop "5CTA-Int1-API"
sc.exe stop "5CTA-MediaMTX"
Start-Sleep -Seconds 5
sc.exe start "5CTA-MediaMTX"
sc.exe start "5CTA-Int1-API"
Start-Sleep -Seconds 10  # aguarda Int1 subir antes do Int2/Int3
sc.exe start "5CTA-Int2-Worker"
sc.exe start "5CTA-Int3-Painel"
```

### A.5 Inspecionar fila T50Pendencia

```powershell
# Quantas pendências aguardando
sqlite3 "C:\5cta\Banco\banco.db" "SELECT acao, COUNT(*) FROM t50Pendencia WHERE sincronizado=0 GROUP BY acao;"

# Pendências com falhas (5+ tentativas)
sqlite3 "C:\5cta\Banco\banco.db" "SELECT id, acao, pessoaId, dispositivoT50Id, tentativasFalhas, erroUltimaTentativa FROM t50Pendencia WHERE tentativasFalhas >= 5;"

# Resetar pendências com falha para reprocessar (depois de corrigir o problema)
sqlite3 "C:\5cta\Banco\banco.db" "UPDATE t50Pendencia SET tentativasFalhas=0, erroUltimaTentativa=NULL WHERE tentativasFalhas >= 5;"
```

---

## Apêndice B — Glossário rápido

| Termo | Significado |
|---|---|
| T50M | Terminal biométrico Anviz instalado em cada ambiente |
| OAE | Open Anviz Ethernet — protocolo TCP do T50M na porta 5010 |
| ONVIF | Padrão aberto para integração com câmeras IP |
| RTSP | Real Time Streaming Protocol — streaming ao vivo de câmeras |
| HLS | HTTP Live Streaming — formato suportado por navegador |
| MediaMTX | Servidor open source que converte RTSP em HLS |
| EBMail | Servidor de email do Exército Brasileiro (Zimbra) |
| 5º CTA | 5º Centro de Telemática de Área (cliente) |
| Fail-Safe | Fechadura que abre na falta de energia (segura para pessoas) |
| Fail-Secure | Fechadura que trava na falta de energia (risco de aprisionamento) |
| AES | Advanced Encryption Standard — usado para cifrar `senhaClear` |
| BCrypt | Algoritmo de hash de senha usado em `senhaHash` |
| JWT | JSON Web Token — autenticação do painel (8h de validade) |
| Profile G ONVIF | Subset ONVIF para Recording Search e Replay |
| T50Pendencia | Fila no banco onde Frontend enfileira comandos para o T50M, consumida pelo Worker |
