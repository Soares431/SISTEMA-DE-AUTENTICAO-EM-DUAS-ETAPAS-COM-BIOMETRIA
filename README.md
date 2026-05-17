


# Hardware & Serviço de Background

## Sobre este módulo
Worker Service em .NET 8 responsável pela comunicação com o dispositivo biométrico Anviz T50M e processamento dos eventos de acesso.

## Como trocar simulador pelo hardware real

No `Program.cs`, trocar:
```
builder.Services.AddSingleton<IAnvizConnector, AnvizConnectorSimulador>();
builder.Services.AddSingleton<IAnvizService, AnvizServiceSimulador>();
```
Por:
```
builder.Services.AddSingleton<IAnvizConnector, AnvizConnector>();
builder.Services.AddSingleton<IAnvizService, AnvizService>();
```

## Configuração do T50M

- IP padrão do dispositivo: `192.168.0.218`
- IP padrão do servidor: `192.168.0.7`
- Porta de comunicação: `5010`

## Observações importantes

- `Mode = 4` → Somente Senha+ID
- `Mode = 6` → Digital+ID e Senha+ID simultaneamente (padrão do SDK)
- `BackupCode = 4` no evento de acesso → autenticação por senha; outros valores → digital

## EventProcessor — trocar simulador pelo real

No `Program.cs`, trocar:
```
builder.Services.AddSingleton<IEventProcessor, EventProcessorSimulador>();
```
Por:
```
builder.Services.AddSingleton<IEventProcessor, EventProcessor>();
```
## EventProcessor real — dependências adicionais

Além dos repositórios do banco, o `EventProcessor` real vai precisar do `IAnvizService` injetado para:

- `FluxoPrimeiroAcesso` → chamar `IniciarCapturaDigital(id)` para o T50M coletar a digital
- Após coleta → chamar `UploadTemplate(id, template)` para salvar a digital

No `Program.cs` já está registrado:
```
builder.Services.AddSingleton<IAnvizService, AnvizServiceSimulador>();
```
Trocar para `AnvizService` quando o hardware estiver disponível.

## Formato da senha no T50M

A senha é armazenada internamente em 3 bytes com formato especial:
- Bits 23-20 (4 bits): comprimento da senha em dígitos
- Bits 19-0 (20 bits): valor numérico da senha

Exemplo para senha "123456" (6 dígitos):
resultado = (123456 & 0xFFFFF) + ((6 & 0xF) << 20) = 6414912

O SDK .NET (Anviz.SDK NuGet) pode fazer essa conversão internamente.
Se a autenticação por senha falhar com hardware real, investigar o método
`AdicionarPessoa` no `AnvizService.cs`.

**Por isso as senhas geradas devem ter sempre 6 dígitos começando em 100000.**
Senhas com zeros à esquerda teriam comprimento errado nesse formato.

## RecordType — campo do evento de acesso

`RecordType` é um byte onde o **bit 7** indica se a porta abriu:
- `RecordType & 0x80 != 0` → porta abriu (AcessoLiberado = true)
- `RecordType & 0x80 == 0` → porta não abriu (AcessoLiberado = false)
Bits 3-0 indicam status de ponto (attendance status) — não usados no nosso sistema.

## Tamanho do template biométrico

O T50M usa 338 bytes por template de digital (FINGERPRINT_DATA_LEN_338).

## Tentativas falhas de acesso — limitação do hardware

O T50M **não envia eventos de rede** quando uma biometria ou senha não é reconhecida.
O dispositivo rejeita localmente (bip de erro) sem notificar o software.

Por isso, o sistema consegue registrar apenas dois cenários de falha:

| Situação | AcessoLiberado | Registrado no banco? |
|---|---|---|
| Digital/senha reconhecida, porta abriu | true | ✅ sim |
| Digital/senha reconhecida, porta NÃO abriu (falha da trava física) | false | ✅ sim |
| Digital NÃO reconhecida pelo T50M | — | ❌ não (sem evento) |
| Senha errada digitada no T50M | — | ❌ não (sem evento) |

**Consequência prática:** o sistema não tem como distinguir "ninguém tentou entrar"
de "alguém tentou entrar mas não foi reconhecido". Tentativas de acesso não
autorizado com biometria ou senha inválida são invisíveis para o software —
só ficam no log local do próprio dispositivo.

Isso é uma limitação de protocolo do SDK Anviz, não do nosso sistema.


# Arduino — Hardware Customizado (5º CTA)

Este diretório contém o código embarcado do hardware customizado desenvolvido para substituir o T50M Anviz durante a apresentação do projeto.

## Componentes

| Componente | Descrição |
|---|---|
| Arduino Uno | Microcontrolador principal |
| LCD 16x2 I2C | Display de interface com o usuário |
| Keypad 4x4 | Teclado para digitação de ID e senha |
| AS608 | Sensor biométrico (físico — mock no Wokwi) |

## Protocolo Serial

Comunicação via USB Serial a 9600 baud. Formato: `TIPO\|CAMPO\|CAMPO\n`

### Arduino → C# (Eventos)

| Mensagem | Descrição |
|---|---|
| `EVT\|READY` | Arduino inicializado |
| `EVT\|AUTH\|ID\|SENHA` | Usuário digitou ID e senha |
| `EVT\|FINGER\|OK\|ID` | Digital reconhecida |
| `EVT\|FINGER\|FAIL` | Digital não reconhecida |
| `EVT\|FINGER\|ENROLLED\|ID` | Digital cadastrada (primeiro acesso) |

### C# → Arduino (Comandos)

| Mensagem | Descrição |
|---|---|
| `CMD\|LCD\|LINE1\|texto` | Escreve na linha 1 do LCD |
| `CMD\|LCD\|LINE2\|texto` | Escreve na linha 2 do LCD |
| `CMD\|LCD\|CLEAR` | Limpa o LCD |
| `CMD\|FINGER\|START_VERIFY` | Inicia verificação de digital |
| `CMD\|FINGER\|START_ENROLL\|ID` | Inicia cadastro de digital |
| `CMD\|FINGER\|CANCEL` | Cancela operação de digital |
| `CMD\|BUZZER\|OK` | Sinaliza acesso liberado |
| `CMD\|BUZZER\|FAIL` | Sinaliza acesso negado |
| `CMD\|ACCESS\|DENIED\|motivo` | Acesso negado com motivo |

## Fluxo de Autenticação

```
Usuário digita ID (6 dígitos) + *
       ↓
Usuário digita Senha (6 dígitos) + *
       ↓
Arduino envia EVT|AUTH|ID|SENHA
       ↓
C# consulta banco de dados
       ↓
┌──────────────────────────────────┐
│ Pessoa não cadastrada            │ → CMD|ACCESS|DENIED|nao_cadastrado
│ Pessoa inativa                   │ → CMD|ACCESS|DENIED|inativo
│ Sem permissão no ambiente        │ → CMD|ACCESS|DENIED|sem_permissao
│ Primeiro acesso (sem biometria)  │ → CMD|FINGER|START_ENROLL
│ Acesso normal (com biometria)    │ → CMD|FINGER|START_VERIFY
└──────────────────────────────────┘
```

## Como Compilar

```powershell
.\compilar.ps1
```

## Como Simular (Wokwi VS Code)

1. Compile o sketch com `.\compilar.ps1`
2. Abra o `diagram.json` no VS Code
3. `F1` → `Wokwi: Start Simulator`

## Como usar com Hardware Real

1. Suba o `Arduino.ino` para o Arduino Uno pelo Arduino IDE
2. Verifique a porta COM no Gerenciador de Dispositivos do Windows
3. No `Program.cs` descomente a OPÇÃO 2 e ajuste a porta:
   ```csharp
   var arduinoConnector = new ArduinoConnector(porta: "COM3"); // ajuste aqui
   ```
4. Rode o Worker

## Fases do Projeto

| Fase | Status | Descrição |
|---|---|---|
| Fase 1 | ✅ | Simulação no Wokwi (LCD + Keypad + mock biometria) |
| Fase 2 | ✅ | Integração C# — protocolo serial completo |
| Fase 3 | ⏳ | Hardware real — substituir mock pelo AS608 físico |
| Fase 4 | ⏳ | Integração completa com backend |

## Como Testar o Fluxo (Wokwi sem C# conectado)

Com o simulador rodando, use o Monitor Serial do Wokwi para simular os comandos que viriam do C#.

### Teste 1 — Validação de 6 dígitos
1. Aperta 1, 2, 3 no teclado (só 3 dígitos)
2. Aperta `*`
3. Esperado no LCD: `ID: 6 digitos!` por 1,5s e volta
4. Continua digitando 4, 5, 6
5. Aperta `*`
6. Esperado no LCD: muda para `Senha:`

### Teste 2 — Fluxo completo até EVT|AUTH
1. Digita 6 dígitos de ID + `*`
2. Digita 6 dígitos de senha + `*`
3. Esperado no LCD: `Verificando... / Aguarde`
4. Esperado no Serial: `EVT|AUTH|123456|123456`

### Teste 3 — Primeiro acesso
1. Faz o Teste 2 até aparecer `EVT|AUTH` no Serial
2. No Monitor Serial digita: `CMD|FINGER|START_ENROLL`
3. Esperado no LCD: `1o Acesso / Coloque o dedo`
4. Aguarda 2 segundos
5. Esperado no LCD: `Digital / Cadastrada!`
6. Esperado no Serial: `EVT|FINGER|ENROLLED|123456`
7. Esperado no LCD: `Acesso Liberado / Bem vindo!`

### Teste 4 — Acesso normal com biometria
1. Faz o Teste 2 até aparecer `EVT|AUTH` no Serial
2. No Monitor Serial digita: `CMD|FINGER|START_VERIFY`
3. Esperado no LCD: `Coloque o dedo`
4. Aguarda 2 segundos
5. Esperado no Serial: `EVT|FINGER|OK|123456`
6. Esperado no LCD: `Acesso Liberado!`

### Teste 5 — Acesso negado
1. Faz o Teste 2 até aparecer `EVT|AUTH` no Serial
2. No Monitor Serial digita: `CMD|ACCESS|DENIED|sem_permissao`
3. Esperado no LCD: `Acesso Negado! / sem_permissao`
4. Após 2s: `Sistema Pronto / Digite o ID:`

### Teste 6 — Digital rejeitada (3ª tentativa)
1. Faz o Teste 4 três vezes seguidas
2. Na terceira vez esperado no Serial: `EVT|FINGER|FAIL`
3. Esperado no LCD: `Nao reconhecido`

### Teste 7 — Cancelamento
1. Digita alguns dígitos do ID
2. Aperta `#`
3. Esperado no LCD: `Cancelado / Digite o ID:`


