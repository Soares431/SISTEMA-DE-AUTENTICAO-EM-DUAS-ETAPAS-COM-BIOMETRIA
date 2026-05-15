


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

