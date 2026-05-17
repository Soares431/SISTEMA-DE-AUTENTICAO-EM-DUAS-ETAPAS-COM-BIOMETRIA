
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

Comunicação via USB Serial a 9600 baud. Formato: `TIPO|CAMPO|CAMPO\n`

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

## Pessoas Mockadas (para testes)

Definidas em `EventProcessorArduinoSimulador.cs` dentro de `HardwareNosso/`.

| ID | Senha | Status | Biometria |
|---|---|---|---|
| 100001 | 123456 | ativa | ❌ sem biometria — primeiro acesso |
| 100002 | 654321 | ativa | ✅ com biometria |
| 100003 | 111111 | inativa | ❌ |
| 100004 | 999999 | ativa | ✅ com biometria |

## Como Testar o Fluxo (Wokwi sem C# conectado)

Com o simulador rodando, use o Monitor Serial do Wokwi para simular os comandos que viriam do C#.

### Teste 1 — Validação de 6 dígitos
1. Aperta 1, 2, 3 no teclado (só 3 dígitos)
2. Aperta `*`
3. Esperado no LCD: `ID: 6 digitos!` por 1,5s e volta
4. Continua digitando 4, 5, 6 e aperta `*`
5. Esperado no LCD: muda para `Senha:`

### Teste 2 — Primeiro acesso (ID 100001)
1. Digita `100001` + `*`
2. Digita `123456` + `*`
3. Esperado no LCD: `Verificando... / Aguarde`
4. Esperado no Serial: `EVT|AUTH|100001|123456`
5. No Monitor Serial digita: `CMD|FINGER|START_ENROLL`
6. Esperado no LCD: `1o Acesso / Coloque o dedo`
7. Aguarda 2 segundos
8. Esperado no LCD: `Digital / Cadastrada!`
9. Esperado no Serial: `EVT|FINGER|ENROLLED|100001`
10. Esperado no LCD: `Acesso Liberado / Bem vindo!`

### Teste 3 — Acesso normal com biometria (ID 100002)
1. Digita `100002` + `*`
2. Digita `654321` + `*`
3. Esperado no Serial: `EVT|AUTH|100002|654321`
4. No Monitor Serial digita: `CMD|FINGER|START_VERIFY`
5. Esperado no LCD: `Coloque o dedo`
6. Aguarda 2 segundos
7. Esperado no Serial: `EVT|FINGER|OK|100002`
8. Esperado no LCD: `Acesso Liberado!`

### Teste 4 — Pessoa inativa (ID 100003)
1. Digita `100003` + `*`
2. Digita `111111` + `*`
3. Esperado no Serial: `EVT|AUTH|100003|111111`
4. No Monitor Serial digita: `CMD|ACCESS|DENIED|inativo`
5. Esperado no LCD: `Acesso Negado! / inativo`
6. Após 2s: `Sistema Pronto / Digite o ID:`

### Teste 5 — Pessoa não cadastrada
1. Digita qualquer ID que não existe ex: `999999` + `*`
2. Digita qualquer senha + `*`
3. Esperado no Serial: `EVT|AUTH|999999|......`
4. No Monitor Serial digita: `CMD|ACCESS|DENIED|nao_cadastrado`
5. Esperado no LCD: `Acesso Negado! / nao_cadastrado`

### Teste 6 — Digital rejeitada (3ª tentativa)
1. Faz o Teste 3 três vezes seguidas
2. Na terceira vez esperado no Serial: `EVT|FINGER|FAIL`
3. Esperado no LCD: `Nao reconhecido`

### Teste 7 — Cancelamento
1. Digita alguns dígitos do ID
2. Aperta `#`
3. Esperado no LCD: `Cancelado / Digite o ID:`


