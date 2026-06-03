# Entrega no Cliente — 5º CTA

Guia operacional para a entrega do Sistema de Controle de Acesso Biométrico no 5º CTA — 10ª Brigada de Infantaria. Cobre tudo que **não pode ser feito no laboratório** porque depende de hardware físico, rede do quartel, credenciais de produção ou decisão do cliente.

> Documento vivo. Atualize após cada etapa concluída — marcar data e responsável.

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

---

## 1. Sumário do que depende do cliente

Itens que **não** podem ser concluídos antes de estar no ambiente do 5º CTA:

| Item | Por quê | Onde se resolve |
|---|---|---|
| IPs dos dispositivos T50M | Topologia da rede interna do quartel | Reunião técnica + tela `/dispositivos` |
| Validar formato de senha do T50M | Aviso no código `AnvizService.cs:26` — pode precisar conversão | Teste físico com 1 usuário antes de cadastrar todos |
| Credenciais SMTP do EBMail/Zimbra | Senhas e endereço do servidor em Salvador | Variáveis de ambiente no servidor de produção |
| URLs RTSP e endereços ONVIF das câmeras | IPs e logins fornecidos pela equipe que instalou as câmeras | Tela `/cameras` e `/ambientes/{id}` |
| Tipo de fechadura (Fail-Safe vs Fail-Secure) | Verificação física porta a porta | Vistoria com instalador + relatório de risco |
| Posicionamento de câmeras (interna/externa) | Onde a câmera está fisicamente em relação à porta | Vistoria + cadastrar `Tipo` correto |
| Calibrar `TempoEsperaGravacaoSeg` por ambiente | Latência real da rede + tempo de gravação da câmera | Tela `/ambientes/{id}` configuração |
| Cadastro de administradores reais | Decisão de quem terá acesso administrativo | INSERT direto no banco (regra de segurança da doc) |
| Cadastro inicial das pessoas | LGPD + lista oficial do 5º CTA | Importação em massa OU cadastro manual via painel |
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
- [ ] Comando interno IP e máscara já configurados no T50M? (Sim/Não)
- [ ] Senha de admin do T50M (para CrossChex se precisar reconfigurar)

### 2.2 Servidor que vai hospedar o sistema

- [ ] Sistema operacional (Windows Server / Windows 10/11)
- [ ] Versão do .NET 8 SDK já instalada? (Sim/Não — se não, levar instalador offline)
- [ ] IP fixo do servidor na rede interna
- [ ] Portas liberadas: `5018` (Int1 API) e `8080` (Int3 painel)
- [ ] Servidor consegue alcançar os T50M na porta TCP 5010?
- [ ] Servidor consegue alcançar as câmeras nas portas RTSP/ONVIF?
- [ ] Servidor consegue alcançar o SMTP Zimbra (EBMail)?
- [ ] Tem permissão de escrita em algum diretório para armazenar gravações? Qual caminho?

### 2.3 Câmeras

Para cada câmera:

- [ ] Nome descritivo
- [ ] Fabricante e modelo
- [ ] URL RTSP completa (ex: `rtsp://usuario:senha@192.168.1.100:554/Streaming/Channels/101`)
- [ ] Endereço ONVIF (ex: `http://192.168.1.100:80/onvif/device_service`)
- [ ] Usuário e senha ONVIF
- [ ] Tipo (interna: dentro do ambiente / externa: do lado de fora)
- [ ] Ambiente ao qual ela pertence
- [ ] Caminho onde os arquivos `.mp4` são salvos pela câmera (NVR ou local)

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

1. Atualizar `appsettings.json` do Int1 com:
   - `JwtKey` único de produção (gerar com `openssl rand -base64 64`)
   - `AesKey` único de produção (32 chars exatos)
2. Gerar build de release dos 3 projetos:
   ```powershell
   dotnet publish -c Release -r win-x64 --self-contained false
   ```
3. Copiar para pen drive: pasta `Banco/.../bin/Release/net8.0/`, `Frontend/bin/Release/net8.0/`, `Worker/bin/Release/net8.0/`, `iniciar.ps1`, este documento, planilhas vazias para preencher in loco.
4. Levar instalador offline do .NET 8 SDK (caso o servidor do cliente não tenha internet).

### Fase 1 — Validação do ambiente do cliente

| # | Ação | Como verificar |
|---|---|---|
| 1.1 | Confirmar .NET 8 instalado | `dotnet --version` no PowerShell deve retornar `8.0.x` |
| 1.2 | Confirmar conectividade com cada T50M | `Test-NetConnection 192.168.1.50 -Port 5010` retorna `TcpTestSucceeded: True` |
| 1.3 | Confirmar conectividade com cada câmera (RTSP) | Abrir URL RTSP no VLC funciona |
| 1.4 | Confirmar conectividade SMTP | `Test-NetConnection smtp-host -Port 587` retorna `TcpTestSucceeded: True` |
| 1.5 | Criar pasta para gravações | Ex: `C:\5cta\cameras\` com permissão de leitura/escrita |
| 1.6 | Definir variável de ambiente `CAMERA_BASE_PATH` | `[Environment]::SetEnvironmentVariable("CAMERA_BASE_PATH", "C:\5cta\cameras", "Machine")` |
| 1.7 | Definir variáveis SMTP | `SMTP_HOST`, `SMTP_PORT`, `SMTP_USER`, `SMTP_PASS` no escopo `Machine` |

### Fase 2 — Implantação

1. Copiar pastas de build para `C:\5cta\Int1\`, `C:\5cta\Int3\`, `C:\5cta\Int2\`
2. Copiar `banco.db` vazio para `C:\5cta\Banco\` (caminho relativo respeitado por `iniciar.ps1`)
3. Editar `iniciar.ps1` se os caminhos relativos não baterem
4. Rodar `iniciar.ps1` — confirmar que Int1 sobe em `:5018`, Int3 em `:8080`, Worker conecta
5. Abrir navegador em `http://servidor:8080` e fazer login com `admin` / `Admin@123`
6. **Imediatamente** trocar a senha do admin padrão (via SQL direto ou tela `/admins/{id}`)

### Fase 3 — Cadastro de admins reais

Para cada admin da seção 2.5, executar via DB Browser for SQLite (não há tela):

```sql
INSERT INTO administrador (login, senhaHash, nomeCompleto, cpf, email, cargo, telefone, dataCriacao)
VALUES ('login_aqui',
        '<BCrypt hash da senha>',  -- gerar com BCrypt.HashPassword no painel C# auxiliar
        'Nome Completo', '00000000000', 'email@ebmail', 'Cargo', '(00)00000-0000',
        datetime('now'));
```

> Gerar hashes BCrypt: rodar um script C# auxiliar ou usar o admin padrão pra logar, depois acessar `/admins` e usar a tela de "Redefinir Senha" do admin recém-criado (insere com hash vazio e troca pela tela).

### Fase 4 — Cadastro dos dispositivos T50M

1. Logar no painel
2. Acessar `/dispositivos` → Nova T50
3. Para cada T50 da seção 2.1: cadastrar nome, IP, porta `5010`
4. Verificar no Worker (console aberto) se ele consegue conectar

### Fase 5 — Cadastro de ambientes e câmeras

1. `/ambientes` → Novo ambiente → Nome + selecionar T50 + `TempoEsperaGravacaoSeg` inicial 60s
2. Entrar no detalhe do ambiente → Adicionar câmera (preencher com URL RTSP, ONVIF, tipo)
3. Validar visualização ao vivo (ver seção [5.4 Streaming RTSP](#54-streaming-rtsp))

### Fase 6 — Teste piloto com 1 pessoa

**Não pular esta fase.** Detecta o problema mais provável: formato de senha do T50M.

1. Cadastrar 1 pessoa de teste com email válido
2. Adicionar ao ambiente de teste — confirmar que o Worker cadastrou a pessoa no T50M (log do console)
3. **Físico:** ir até o T50M, digitar ID + senha que o painel mostrou
4. **Resultado esperado:** porta abre, painel mostra tentativa em `/historico` em até 5s
5. Se falhar → ver seção [5.1 Senha não funciona](#51-senha-não-funciona-no-t50m)

### Fase 7 — Cadastro em massa

Só depois que a Fase 6 passou:

1. Importar planilha de pessoas (manual via painel, uma a uma — não há importação CSV ainda)
2. Distribuir senhas: ou por email (se SMTP funcionar) ou em papel selado (fallback)
3. Vincular cada pessoa aos ambientes corretos

### Fase 8 — Configurar serviços como Windows Service

Para o sistema sobreviver a reinícios do servidor:

```powershell
# Int1 (API)
sc.exe create "5CTA-Int1-API" binPath= "C:\5cta\Int1\WebAbil8-Sistema_Verificação_dupla.slnx.exe" start= auto
sc.exe start "5CTA-Int1-API"

# Worker (já tem AddWindowsService() no Program.cs)
sc.exe create "5CTA-Int2-Worker" binPath= "C:\5cta\Int2\BiometricAcess.Worker.exe" start= auto
sc.exe start "5CTA-Int2-Worker"

# Int3 (Painel)
sc.exe create "5CTA-Int3-Painel" binPath= "C:\5cta\Int3\FrontendControleAcesso.exe" start= auto
sc.exe start "5CTA-Int3-Painel"
```

Confirmar nos serviços do Windows (`services.msc`) que os 3 ficam `Running` e `Automatic`.

---

## 4. Configurações que só o cliente fornece

### 4.1 Variáveis de ambiente do servidor

Devem ser setadas no escopo `Machine` (sobrevive a logoff):

```powershell
[Environment]::SetEnvironmentVariable("SMTP_HOST",        "smtp.ebmail.eb.mil.br", "Machine")
[Environment]::SetEnvironmentVariable("SMTP_PORT",        "587",                    "Machine")
[Environment]::SetEnvironmentVariable("SMTP_USER",        "sistema@ebmail",         "Machine")
[Environment]::SetEnvironmentVariable("SMTP_PASS",        "<senha>",                "Machine")
[Environment]::SetEnvironmentVariable("CAMERA_BASE_PATH", "C:\5cta\cameras",        "Machine")
```

Depois de setar, **reiniciar os serviços** para as variáveis serem lidas.

### 4.2 `appsettings.json` do Int1 (produção)

Trocar antes da instalação:

```json
{
  "JwtKey":      "<chave gerada com openssl rand -base64 64>",
  "JwtIssuer":   "5cta-producao",
  "JwtAudience": "5cta-painel",
  "AesKey":      "<32 chars únicos de produção>",
  "BancoApiUrl": "http://localhost:5018/"
}
```

> `JwtKey` em produção **não pode** ser igual ao do desenvolvimento. Se vazar, qualquer pessoa forja tokens válidos.

### 4.3 `appsettings.json` do Int3 (produção)

```json
{
  "BancoApiUrl": "http://localhost:5018/"
}
```

Se Int1 e Int3 estiverem em servidores diferentes, trocar `localhost` pelo IP do servidor do Int1.

### 4.4 Configurações editáveis pelo admin no painel

Na tela `/configuracoes`:

- **Retenção de tentativas/gravações**: 30 a 180 dias. Padrão 90. Definir conforme política de auditoria do 5º CTA.
- **Retenção de logs administrativos**: 90 a 365 dias. Padrão 180.
- **Período de inativação automática**: 3 a 24 meses. Padrão 24.

Por ambiente (tela `/ambientes/{id}`):

- **Tempo de espera de gravação**: 30 a 120 segundos. Calibrar conforme câmera.

---

## 5. Riscos técnicos conhecidos e mitigação

### 5.1 Senha não funciona no T50M

**Sintoma:** pessoa digita ID + senha correta no T50M, mas o display dá negação ou nada acontece.

**Causa provável:** o T50M usa formato proprietário de 3 bytes para senha (bits 23-20 = tamanho, bits 19-0 = valor numérico). O código atual em `AnvizService.cs:26` faz `ulong.Parse(senha)` direto — pode não ser o formato aceito pelo firmware.

**Como diagnosticar:**
1. Pegar senha válida que o painel gerou (ex: `100001`)
2. Cadastrar manualmente no Anviz CrossChex (software oficial) a mesma senha pro mesmo ID
3. Se entrar pelo CrossChex e não pelo sistema → bug de formato confirmado

**Solução:**
Editar `Hardware & Serviço de Background/.../Services/AnvizService.cs` método `AdicionarPessoa`. Substituir:
```csharp
userInfo.Password = ulong.Parse(senha);
```
por:
```csharp
var senhaNum = ulong.Parse(senha);
var tamanho = (ulong)senha.Length;
userInfo.Password = (tamanho << 20) | senhaNum;
```

Recompilar Worker, reiniciar serviço, testar novamente.

### 5.2 Worker não conecta no T50M

**Sintoma:** log do Worker mostra `Falha ao conectar ao T50M. Tentando novamente em 10 segundos...` em loop.

**Causas possíveis:**

| Causa | Verificação |
|---|---|
| IP errado no cadastro | Painel `/dispositivos` → conferir |
| Porta bloqueada por firewall | `Test-NetConnection IP -Port 5010` |
| T50M sem energia | Verificar LED do display |
| T50M com IP estático diferente | Acessar CrossChex e ver IP real |
| Cabo Ethernet desconectado | Verificar fisicamente |

**Solução:** corrigir o item identificado e o Worker reconecta sozinho em até 10s.

### 5.3 Gravação não aparece no histórico

**Sintoma:** entrada liberada aparece em `/historico`, mas coluna "Gravação" fica como "-".

**Causas possíveis:**

| Causa | Como resolver |
|---|---|
| Câmera não detecta movimento | Ajustar sensibilidade na câmera |
| `CAMERA_BASE_PATH` errado | Verificar variável de ambiente |
| Câmera salva arquivo em outro caminho | Criar symlink ou ajustar `CameraService` |
| `TempoEsperaGravacaoSeg` muito curto | Aumentar (até 120s) no ambiente |
| Arquivo não é `.mp4` | `CameraService.MonitorarNovoArquivo` filtra por `*.mp4` — ajustar para extensão real (`.avi`, `.h264`) |
| Câmera marcada como "interna" + entrada negada | Comportamento correto, doc §5.11 |

### 5.4 Streaming RTSP

**Estado atual:** botão "Ao Vivo" abre modal com placeholder estático. Não há streaming real.

**Por que não dá pra fazer só com C#:** navegadores não suportam RTSP nativamente. Precisa de proxy RTSP → HLS/WebRTC rodando ao lado do sistema.

**Como entregar (3 opções, do mais simples ao mais robusto):**

1. **VLC externo (rápido, ruim)**: admin clica "Ao Vivo" e o painel mostra a URL RTSP que ele copia e cola no VLC. Sem integração real.

2. **FFmpeg + HLS (recomendado)**:
   - Instalar FFmpeg no servidor
   - Script PowerShell que para cada câmera ativa roda: `ffmpeg -i rtsp://... -c:v copy -f hls -hls_time 4 -hls_list_size 5 C:\5cta\hls\camera_{id}\index.m3u8`
   - Configurar IIS ou nginx para servir `/hls/camera_{id}/index.m3u8`
   - No painel substituir o placeholder por `<video controls><source src="/hls/camera_{id}/index.m3u8" type="application/vnd.apple.mpegurl"></video>` + biblioteca [hls.js](https://github.com/video-dev/hls.js)

3. **nginx-rtmp (mais robusto)**: nginx compilado com módulo RTMP atua como bridge RTSP → HLS automático. Documentação extensa online.

> Recomendação: combinar com cliente que vamos entregar **opção 1** no sign-off + **opção 2 documentada** para ser implementada em uma manutenção posterior.

### 5.5 Email Zimbra falha

**Sintoma:** ao cadastrar pessoa, o painel mostra a senha em vez de enviá-la por email.

**Causas possíveis:**

| Causa | Verificação |
|---|---|
| Variáveis SMTP não setadas | `Get-ChildItem Env:SMTP_*` |
| Senha SMTP errada | Testar manualmente no Outlook/Thunderbird com mesma conta |
| Firewall bloqueando porta 587 | `Test-NetConnection smtp-host -Port 587` |
| TLS rejeitado | Ver logs do servidor Zimbra |
| Conta exige autenticação por OAuth | Pedir conta SMTP tradicional ao admin do EBMail |

**Comportamento atual:** se SMTP falha, o painel **mostra a senha em alerta** para o admin entregar manualmente. Não há perda de funcionalidade — só processo manual.

### 5.6 Limite de 1000 digitais por T50M

**Sintoma:** ao adicionar a milésima primeira pessoa ao ambiente, o painel oferece cadastrar em modo "Somente Senha".

**Comportamento esperado** — não é erro. Conforme doc §2.3.

**Quando vira problema:** se o cliente quer mais de 1000 pessoas com biometria no mesmo ambiente.

**Solução de longo prazo:** o cliente precisa instalar um segundo T50M no mesmo ambiente. O sistema atual assume um T50M por ambiente — exigiria mudança de schema (`AmbienteT50` N:N). Discutir antes de aceitar requisito.

### 5.7 Banco SQLite corrompido

**Sintoma:** Int1 sobe mas todas as queries retornam erro.

**Prevenção:**
- Backup diário do `banco.db` (script PowerShell agendado)
- Não rodar 2 instâncias do Int1 apontando para o mesmo arquivo
- Encerrar serviços com `sc.exe stop` antes de copiar o arquivo

**Recuperação:**
- Parar todos os serviços
- Restaurar último backup
- Reiniciar serviços

> Sugestão: agendar task no Windows Task Scheduler para `Copy-Item banco.db banco.db.backup-$(Get-Date -Format yyyy-MM-dd).db` toda meia-noite, mantendo 30 dias.

### 5.8 Hora do T50M desincronizada

**Sintoma:** tentativas de acesso aparecem com horário errado em `/historico`.

**Solução automática:** o método `AnvizService.SincronizarHora()` existe mas não é chamado periodicamente. **Sugestão para manutenção futura:** criar job Hangfire diário que chama esse método para todos os T50M cadastrados.

**Workaround imediato:** ir até cada T50M com o CrossChex e sincronizar manualmente.

### 5.9 Fechadura Fail-Secure (risco de vida)

**Sintoma:** durante teste, simular queda de energia (desligar disjuntor) e verificar se a porta destrava.

**Se não destravar:** fechadura é Fail-Secure — risco de aprisionar pessoas em emergência.

**O que fazer:** **NÃO aceitar a entrega sem o cliente confirmar fechaduras Fail-Safe em todos os ambientes**, ou ter sistema de bypass mecânico (chave física do lado de dentro). Registrar no termo de entrega como responsabilidade do cliente — doc §9.1.

### 5.10 Câmera externa não captura tentativas negadas

**Sintoma:** entrada negada não tem gravação associada.

**Comportamento correto** conforme doc §5.11. A câmera só grava com movimento; tentativa negada não tem movimento dentro do ambiente.

**Sugestão para o cliente:** posicionar câmeras externas cobrindo a área antes da porta (doc §9.2) — assim mesmo entradas negadas geram gravação da pessoa tentando.

---

## 6. Testes de aceitação presenciais

Estes 7 cenários devem ser executados **com o cliente assistindo** e marcados na ata.

### Teste 1 — Login do admin
- Abrir `http://servidor:8080`
- Logar com credenciais do admin real (não o `admin/Admin@123` padrão)
- Resultado esperado: redirect para dashboard, nome do admin no topo

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
- Painel registra com `MotivoNegacao = sem_permissao`

### Teste 5 — Acesso negado por inatividade
- Pessoa cadastrada, inativada manualmente pelo admin
- Vai ao T50M
- Porta não abre
- Painel registra com `MotivoNegacao = inativo`

### Teste 6 — Pessoa não cadastrada
- Alguém digita ID inexistente (ex: `999999`) no T50M
- Porta não abre
- Painel registra com `PessoaId = NULL` e `MotivoNegacao = nao_cadastrado`

### Teste 7 — Gravação de câmera associada
- Repetir Teste 3 com câmera ativa no ambiente
- Aguardar `TempoEsperaGravacaoSeg`
- Painel `/historico` mostra link "Ver" na coluna Gravação
- Clicar abre o vídeo em nova aba

### Teste 8 (opcional) — Reenvio de senha
- Admin clica "Reenviar Senha" no perfil de uma pessoa
- Se SMTP OK: pessoa recebe email
- Se SMTP falha: alert no painel mostra "SMTP indisponível — senha exibida no console do servidor"

### Teste 9 (opcional) — Exportação PDF
- Admin clica "PDF" no histórico
- Browser baixa `historico.pdf` com a lista filtrada
- Repetir para Logs e Relatório de Ambiente

---

## 7. Treinamento dos administradores

Duração: **2 horas** com até 5 admins por sessão.

### Agenda

| Tempo | Tópico | Tela |
|---|---|---|
| 10 min | Visão geral do sistema (o que ele faz, o que não faz) | Dashboard |
| 15 min | Cadastrar nova pessoa, entender senha gerada, enviar email | /pessoas |
| 15 min | Adicionar pessoa a ambiente, entender T50 cheio + modo somente senha | /ambientes/{id} |
| 10 min | Inativar pessoa (confirmação dupla) | /pessoas/{id} |
| 10 min | Resetar biometria | /pessoas/{id} |
| 10 min | Reenviar senha | /pessoas/{id} |
| 10 min | Cadastrar câmera + visualização ao vivo | /cameras |
| 10 min | Consultar histórico com filtros + exportar CSV/PDF | /historico |
| 10 min | Consultar logs de auditoria | /logs |
| 10 min | Configurações de retenção | /configuracoes |
| 10 min | O que fazer quando T50M offline (recorrer a chave física) | n/a |
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

---

## 8. Pós-entrega — operação contínua

### Rotinas que precisam ser configuradas

| Item | Frequência | Como configurar |
|---|---|---|
| Backup do `banco.db` | Diário, 02:00 | Windows Task Scheduler |
| Limpeza de dados expirados | Diário, 03:00 | Já automatizado (Hangfire) |
| Inativação por 2 anos sem acesso | Diário, 03:00 | Já automatizado (Hangfire) |
| Sincronização de hora dos T50M | Semanal | **Pendente** — implementar job |
| Verificação de espaço em disco | Semanal | Script PowerShell + alerta SMTP |
| Verificação de logs de erro do Worker | Semanal | Visitar Event Viewer / arquivo de log |

### Indicadores para o cliente acompanhar

Acessível em `/` (Dashboard):
- Total de entradas permitidas/negadas por dia
- T50M com mais de 950 digitais (precisa de atenção)
- Pessoas ativas no sistema

### Contato para suporte

- **Responsável técnico:** Gustavo Henrique
- **Email:** gh33493@gmail.com
- **SLA proposto:** 24h úteis para resposta inicial em incidentes não críticos. Críticos (sistema fora do ar): 4h úteis.

---

## 9. Sign-off

### Checklist final antes de assinar a entrega

- [ ] Os 3 serviços rodam como Windows Service e sobem após reboot
- [ ] Todos os T50M conectam (verificar no log do Worker)
- [ ] Pelo menos um administrador real (não o `admin` padrão) consegue logar
- [ ] **Senha do admin padrão foi trocada ou admin foi removido**
- [ ] Testes 1 a 7 da seção 6 passaram com cliente assistindo
- [ ] Variáveis de ambiente SMTP e CAMERA_BASE_PATH estão setadas em escopo Machine
- [ ] Pelo menos uma pessoa de teste foi cadastrada, recebeu senha, entrou com biometria
- [ ] Backup automático do banco está agendado
- [ ] Treinamento dos admins foi feito (sessão da seção 7)
- [ ] Cliente assinou ciente das limitações:
  - [ ] Streaming RTSP é placeholder (entregue manual via VLC)
  - [ ] Sincronização de hora dos T50M é manual
  - [ ] Tipo de fechadura é responsabilidade do cliente
  - [ ] Posicionamento de câmeras externas para capturar negados é responsabilidade do cliente

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
| Pendências aceitas pelo cliente | Streaming RTSP, sincronização hora, fechaduras |

Assinaturas: ___________________ (técnico) ___________________ (cliente)

---

## Apêndice A — Scripts úteis para a entrega

### A.1 Gerar hash BCrypt de uma senha

```csharp
// Salvar como gerar-hash.cs e rodar com: dotnet script gerar-hash.cs <senha>
#r "nuget: BCrypt.Net-Next, 4.1.0"
using System;
var senha = Args.Count > 0 ? Args[0] : "Admin@123";
Console.WriteLine(BCrypt.Net.BCrypt.HashPassword(senha, workFactor: 10));
```

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

### A.4 Restart limpo dos 3 serviços

```powershell
sc.exe stop "5CTA-Int3-Painel"
sc.exe stop "5CTA-Int2-Worker"
sc.exe stop "5CTA-Int1-API"
Start-Sleep -Seconds 5
sc.exe start "5CTA-Int1-API"
Start-Sleep -Seconds 10  # aguarda Int1 subir antes do Int2/Int3
sc.exe start "5CTA-Int2-Worker"
sc.exe start "5CTA-Int3-Painel"
```

---

## Apêndice B — Glossário rápido

| Termo | Significado |
|---|---|
| T50M | Terminal biométrico Anviz instalado em cada ambiente |
| OAE | Open Anviz Ethernet — protocolo TCP do T50M na porta 5010 |
| ONVIF | Padrão aberto para integração com câmeras IP |
| RTSP | Real Time Streaming Protocol — streaming ao vivo de câmeras |
| HLS | HTTP Live Streaming — formato de streaming compatível com navegador |
| EBMail | Servidor de email do Exército Brasileiro (Zimbra) |
| 5º CTA | 5º Centro de Telemática de Área (cliente) |
| Fail-Safe | Fechadura que abre na falta de energia (segura para pessoas) |
| Fail-Secure | Fechadura que trava na falta de energia (risco de aprisionamento) |
| AES | Advanced Encryption Standard — usado para cifrar `senhaClear` |
| BCrypt | Algoritmo de hash de senha usado em `senhaHash` |
| JWT | JSON Web Token — autenticação do painel (8h de validade) |
