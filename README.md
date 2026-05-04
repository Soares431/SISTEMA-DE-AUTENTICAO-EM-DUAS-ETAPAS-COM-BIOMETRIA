


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


