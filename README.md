


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

## Dependências do banco de dados (Integrante 1)

Para implementar o `EventProcessor` real, são necessários os seguintes repositórios:

- `IPessoaRepository` — métodos: `BuscarPorId`, `AlterarStatus`, `MarcarBiometriaCadastrada`, `SalvarTemplate`, `AtualizarUltimoAcesso`
- `IAmbientePessoaRepository` — métodos: `PessoaTemAcesso`
- `IDispositivoT50Repository` — métodos: `ContarDigitaisCadastradas`, `TemVagaDigital`
- `ITentativaAcessoRepository` — métodos: `Registrar`
- `ILogAdminRepository` — métodos: `Registrar` (necessário para HW-17)





