# SISTEMA-DE-AUTENTICA-O-EM-DUAS-ETAPAS-COM-BIOMETRIA

> Projeto desenvolvido no 5º CTA — 10ª Brigada de Infantaria

Sistema de controle de acesso com autenticação em duas etapas: senha numérica e biometria digital, gerenciado por um painel web administrativo e integrado ao hardware Anviz T50.

---

## Tecnologias

**Back-end**
- C# .NET 8
- Entity Framework Core 8
- SQLite
- Blazor Server / ASP.NET Core
- Hangfire (jobs agendados)

**Hardware**
- Anviz T50 (comunicação via Anviz SDK / protocolo OAE, porta 5010)
- Câmera IP com stream RTSP

**Integrações**
- MailKit (e-mail SMTP)
- BCrypt.Net (hash de senhas)
- FFmpeg (gravação de vídeo)

---

## Funcionamento

1. **Cadastro** — O sistema gera uma senha numérica de 6 dígitos e envia por e-mail ao usuário.
2. **Primeiro acesso** — Após autenticar com a senha, o hardware Anviz captura e registra a digital do usuário.
3. **Acessos seguintes** — Autenticação por digital (modo `ID+FP`). O serviço de background faz polling a cada 2 segundos no dispositivo, processa os eventos e registra cada tentativa.
4. **Painel administrativo** — Interface web (Blazor) para cadastro, edição, inativação e histórico de acessos.

---

## Estrutura do Projeto

```
/
├── Data/               # DbContext, Migrations
├── Models/             # Usuario, Administrador, TentativaAcesso, SenhaDisponivel
├── Repositories/       # UsuarioRepository, TentativaRepository, SenhaRepository
├── Services/
│   ├── AnvizService    # Comunicação com o hardware
│   ├── EmailService    # Envio de e-mails via MailKit
│   ├── HashService     # BCrypt
│   ├── CameraService   # Gravação FFmpeg
│   └── SenhaService    # Geração de senhas únicas
├── Workers/
│   └── PollingService  # IHostedService — lê eventos do T50
└── Web/                # Blazor Server — painel administrativo
```

---

## Equipe e Responsabilidades

| Integrante | Módulo | Área |
|---|---|---|
| Integrante 1 | Banco de Dados & Repositórios | C# · EF Core · SQLite |
| Integrante 2 | Hardware & Serviço de Background | C# · Anviz SDK · Windows Service |
| Integrante 3 | Painel Web de Administração | Blazor Server · ASP.NET |
| Integrante 4 | Integrações & Segurança | FFmpeg · MailKit · BCrypt · Hangfire |

> Detalhamento completo das tarefas em `direcional_equipe.docx`.

---

## Configuração

As credenciais e configurações sensíveis devem ser definidas via variáveis de ambiente ou .NET User Secrets. Nunca commitar no repositório.

```
SMTP_HOST=
SMTP_USER=
SMTP_PASS=
ANVIZ_IP=
CAMERA_RTSP_URL=
```

---

## Acompanhamento

O progresso das tarefas é gerenciado no Kanban do projeto:
🔗 [https://trello.com/b/SZvsdzGx/kanban](https://trello.com/b/SZvsdzGx/kanban)
