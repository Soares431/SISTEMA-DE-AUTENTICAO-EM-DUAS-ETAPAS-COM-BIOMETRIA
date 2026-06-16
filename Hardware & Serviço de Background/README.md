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
│   ├── EventProcessor.cs
│   ├── SincronizadorT50Worker.cs
│   ├── TimeSyncWorker.cs
│   ├── ILogService.cs
│   └── LogService.cs
├── Worker.cs
└── Program.cs
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

## Pendências

- **HW-16** — `EventProcessor.AguardarGravacaoCamera()` — aguardando `CameraService.MonitorarNovoArquivo()` do Integrante 4
