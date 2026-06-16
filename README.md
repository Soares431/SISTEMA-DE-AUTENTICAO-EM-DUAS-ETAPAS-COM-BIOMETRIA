# Sistema de Controle de Acesso Biométrico — 5º CTA

> Projeto universitário desenvolvido para o 5º CTA / 10ª Brigada de Infantaria

Sistema de controle de acesso com autenticação em duas etapas: senha numérica e biometria digital, gerenciado por um painel web administrativo e integrado ao hardware Anviz T50M.

---

## Estrutura do Repositório

```
/
├── Banco/                          # Banco de dados, modelos e repositórios
├── Hardware & Serviço de Background/  # Worker Service — comunicação com o T50M
├── PainelWeb/                      # Painel administrativo Blazor
└── InfraestruturaBloco1/           # Integrações e infraestrutura (FFmpeg, MailKit)
```

---

## Tecnologias

| Área | Tecnologia |
|---|---|
| Back-end | C# .NET 8, Worker Service |
| Banco de dados | SQLite + Entity Framework Core 8 |
| Front-end | Blazor Server |
| Hardware | Anviz T50M (SDK OAE, porta 5010) |
| Integrações | MailKit, BCrypt.Net, FFmpeg |

---

## Equipe

| Integrante | Módulo | Responsabilidade |
|---|---|---|
| Integrante 1 | Banco | EF Core, SQLite, repositórios |
| Integrante 2 | Hardware & Worker | Anviz SDK, Worker Service |
| Integrante 3 | Painel Web | Blazor Server, interface administrativa |
| Integrante 4 | Infraestrutura | FFmpeg, MailKit, BCrypt, Hangfire |

---

## Funcionamento Geral

1. **Cadastro** — Sistema gera senha numérica de 6 dígitos e envia por e-mail ao usuário.
2. **Primeiro acesso** — Usuário autentica com ID + senha, hardware captura e registra a digital.
3. **Acessos seguintes** — Autenticação por ID + digital. Worker faz polling a cada 2 segundos, processa eventos e registra tentativas.
4. **Painel administrativo** — Interface web para cadastro, edição, inativação e histórico de acessos.

---

## Como Rodar

Cada módulo tem seu próprio README com instruções detalhadas. Consulte:

- `Hardware & Serviço de Background/README.md` — Worker Service e hardware

---

## Acompanhamento

🔗 [Kanban do projeto](https://trello.com/b/SZvsdzGx/kanban)
