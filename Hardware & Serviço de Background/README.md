# Hardware & Serviço de Background

Worker Service em .NET 8 responsável pela comunicação com o dispositivo biométrico Anviz T50M e processamento dos eventos de acesso.

---

## Estrutura

```
BiometricAcess.Worker/
├── Models/
│   └── EventoAcesso.cs
├── Services/                    # Interfaces e implementações reais (T50M)
│   ├── IAnvizConnector.cs
│   ├── AnvizConnector.cs
│   ├── IAnvizService.cs
│   ├── AnvizService.cs
│   ├── IEventProcessor.cs
│   └── EventProcessor.cs
├── Simulador/                   # Dados mockados — sem hardware
│   ├── AnvizConnectorSimulador.cs
│   ├── AnvizServiceSimulador.cs
│   └── EventProcessorSimulador.cs
├── HardwareNosso/               # Arduino customizado
│   ├── Arduino/
│   │   └── Arduino_Real.ino    # Código para o Arduino físico
│   ├── Simulador/
│   │   └── EventProcessorArduinoSimulador.cs
│   ├── ArduinoConnector.cs
│   ├── ArduinoService.cs
│   ├── ArduinoServiceExtras.cs
│   ├── EventProcessorArduino.cs
│   ├── IAnvizArduinoService.cs
│   └── SerialProtocol.cs
├── Worker.cs
└── Program.cs
```

---

## Como Trocar de Modo

Tudo é controlado pelo `Program.cs`. Apenas uma opção deve estar ativa por vez.

```csharp
// ── OPÇÃO 1 — Simulador falso (padrão, sem hardware) ──────────────
builder.Services.AddSingleton<IAnvizConnector, AnvizConnectorSimulador>();
builder.Services.AddSingleton<IAnvizService, AnvizServiceSimulador>();
builder.Services.AddSingleton<IEventProcessor, EventProcessorSimulador>();

// ── OPÇÃO 2 — Nosso Arduino (hardware físico) ──────────────────────
// Ajuste a porta COM antes de ativar
//var arduinoConnector = new ArduinoConnector(porta: "COM3");
//builder.Services.AddSingleton<IAnvizConnector>(arduinoConnector);
//builder.Services.AddSingleton<IAnvizService>(new ArduinoService(arduinoConnector));
//builder.Services.AddSingleton<IAnvizArduinoService>(new ArduinoServiceExtras(arduinoConnector));

// OPÇÃO 2A — banco vazio / dados mockados
//builder.Services.AddSingleton<IEventProcessor, EventProcessorArduinoSimulador>();

// OPÇÃO 2B — banco real
//builder.Services.AddSingleton<IEventProcessor, EventProcessorArduino>();

// ── OPÇÃO 3 — T50M real (hardware Anviz) ──────────────────────────
//builder.Services.AddSingleton<IAnvizConnector, AnvizConnector>();
//builder.Services.AddSingleton<IAnvizService, AnvizService>();
//builder.Services.AddSingleton<IEventProcessor, EventProcessor>();
```

---

## Configuração do T50M

| Parâmetro | Valor |
|---|---|
| IP do dispositivo | `192.168.0.218` |
| IP do servidor | `192.168.0.7` |
| Porta | `5010` |

---

## Informações do T50M

**BackupCode do evento de acesso:**
- Valor `4` → autenticação por senha
- Outros valores → autenticação por digital

**RecordType:**
- `RecordType & 0x80 != 0` → porta abriu (AcessoLiberado = true)
- `RecordType & 0x80 == 0` → porta não abriu (AcessoLiberado = false)

**Modos de acesso:**
- `Mode = 4` → somente Senha+ID
- `Mode = 6` → Digital+ID e Senha+ID simultaneamente (padrão)

**Template biométrico:** 338 bytes (FINGERPRINT_DATA_LEN_338)

**Senhas:** sempre 6 dígitos, range 100000–999999

**Limitação importante:** O T50M não envia eventos quando digital ou senha não é reconhecida — tentativas negadas por biometria/senha errada são invisíveis para o software.

---

## Incertezas Técnicas Documentadas

| # | Descrição | Arquivo | Como resolver |
|---|---|---|---|
| 1 | Mode=6 deduzido via flags, não testado com hardware | `AnvizService.cs → AlterarModo` | Testar com hardware e ajustar se necessário |
| 2 | `verifyCount=2` padrão do SDK, comportamento real desconhecido | `AnvizService.cs → IniciarCapturaDigital` | Testar com hardware e ajustar se necessário |
| 3 | Timeout de reconexão TCP de 20s definido no SDK | `Worker.cs + AnvizConnector.cs` | Monitorar logs de DeviceError em produção |

Confiança geral de funcionamento com hardware real: ~80%

---

## Como Usar o Arduino Real

1. Suba o `HardwareNosso/Arduino/Arduino_Real.ino` para o Arduino Uno pelo Arduino IDE
2. Verifique a porta COM no Gerenciador de Dispositivos do Windows
3. No `Program.cs` descomente a OPÇÃO 2 e ajuste a porta:
   ```csharp
   var arduinoConnector = new ArduinoConnector(porta: "COM3"); // ajuste aqui
   ```
4. Escolha entre OPÇÃO 2A (mockado) ou OPÇÃO 2B (banco real)
5. Rode o Worker

---

## Pendências

- **HW-16** — `EventProcessor.AguardarGravacaoCamera()` — aguardando `CameraService.MonitorarNovoArquivo()` do Integrante 4
