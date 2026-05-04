
# Hardware & Serviço de Background

## Sobre este módulo
Worker Service em .NET 8 responsável pela comunicação com o dispositivo biométrico Anviz T50M e processamento dos eventos de acesso.

## Como trocar simulador pelo hardware real

### Program.cs
Quando o hardware estiver disponível, trocar esta linha:
```
builder.Services.AddSingleton<IAnvizConnector, AnvizConnectorSimulador>();
```
Por:
```
builder.Services.AddSingleton<IAnvizConnector, AnvizConnector>();
```

### AnvizService — Valores do Mode
- `Mode = 4` → Somente Senha+ID
- `Mode = 6` → Digital+ID e Senha+ID simultaneamente (padrão)

### IP padrão do T50M
- Dispositivo: `192.168.0.218`
- Servidor: `192.168.0.7`
- Porta: `5010`
