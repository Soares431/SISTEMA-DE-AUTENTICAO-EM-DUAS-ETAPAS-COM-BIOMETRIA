# Frontend - Versão 1.0

## Contexto

O projeto atual é uma Interface de **Controle de Acesso Biométrico para o 5° CTA** construído em **ASP.NET Core com Blazor Server**, com design visual com light e dark themes.

## Pré-requisitos

> [!IMPORTANT]
> Se o **.NET SDK não está instalado** não está instalado sua máquina, precisaremos instalar o .NET 8 SDK antes de iniciar. A instalação poderá ser feita via script oficial da Microsoft (`dotnet-install.sh`).

## Funcionalidades Mapeadas

| Rota Blazor       | Componente Blazor               |
| ----------------- | ------------------------------- |
| `/login`          | `Pages/Login.razor`             |
| `/`               | `Pages/Dashboard.razor`         |
| `/ambientes`      | `Pages/Ambientes/Index.razor`   |
| `/ambientes/{id}` | `Pages/Ambientes/Detalhe.razor` |
| `/pessoas`        | `Pages/Pessoas/Index.razor`     |
| `/pessoas/{id}`   | `Pages/Pessoas/Detalhe.razor`   |
| `/cameras`        | `Pages/Cameras.razor`           |
| `/historico`      | `Pages/Historico.razor`         |
| `/logs`           | `Pages/Logs.razor`              |
| `/configuracoes`  | `Pages/Configuracoes.razor`     |
| `/ajuda`          | `Pages/Ajuda.razor`             |

---

### Instalação do .NET SDK

- Instalar .NET 8 SDK via `dotnet-install.sh`
- Verificar com `dotnet --version`

---

### 3. Criação do Projeto Blazor Server

Estrutura:

```
PROJETO_CTA_FRONTEND/
├── Components/
│   ├── Layout/
│   │   ├── MainLayout.razor          # Layout com sidebar + content
│   │   ├── MainLayout.razor.css      # CSS do layout
│   │   ├── NavMenu.razor             # Sidebar navigation
│   │   └── NavMenu.razor.css         # CSS do sidebar
│   ├── Pages/
│   │   ├── Login.razor               # Página de login
│   │   ├── Dashboard.razor           # Dashboard principal
│   │   ├── Ambientes/
│   │   │   ├── Index.razor           # Lista de ambientes
│   │   │   └── Detalhe.razor         # Detalhe do ambiente (tabs)
│   │   ├── Pessoas/
│   │   │   ├── Index.razor           # Lista de pessoas
│   │   │   └── Detalhe.razor         # Detalhe da pessoa
│   │   ├── Cameras.razor             # Câmeras com live view
│   │   ├── Historico.razor           # Histórico de acessos
│   │   ├── Logs.razor                # Logs do sistema
│   │   ├── Configuracoes.razor       # Configurações globais
│   │   └── Ajuda.razor               # Central de ajuda
│   ├── Shared/
│   │   ├── StatCard.razor            # Card de estatística reutilizável
│   │   ├── StatusBadge.razor         # Badge de status
│   │   └── SearchFilter.razor        # Componente de filtro/busca
│   ├── _Imports.razor
│   ├── App.razor
│   └── Routes.razor
├── wwwroot/
│   ├── css/
│   │   └── app.css                   # Design system (themes com cores do projeto)
│   ├── images/                    
│   │   ├── icon.svg
│   │   └── outras imagens necessárias...
│   ├── app.js                        # JS interop (chart, etc.)
│   └── favicon.ico
├── Data/
│   └── MockData.cs                   # Todos os dados mock para testes de interface
├── Program.cs
├── FrontendControleAcesso.csproj
├── appsettings.json
└── .gitignore
```

---

### 4. System Design
#### wwwroot/css/app.css

Design system com temas (light e dark) usando CSS. Mapeamento de cores:

| Token           | Valor oklch Original   | Uso                                    |
| --------------- | ---------------------- | -------------------------------------- |
| `--background`  | `oklch(0.13 0.01 250)` | Fundo principal (azul-escuro profundo) |
| `--foreground`  | `oklch(0.95 0 0)`      | Texto principal (branco)               |
| `--card`        | `oklch(0.17 0.01 250)` | Fundo dos cards                        |
| `--primary`     | `oklch(0.65 0.18 160)` | Cor primária (verde-esmeralda)         |
| `--destructive` | `oklch(0.55 0.2 25)`   | Vermelho - erro                        |
| `--warning`     | `oklch(0.75 0.15 80)`  | Amarelo - alerta                       |
| `--success`     | `oklch(0.65 0.18 160)` | Verde - sucesso                        |
| `--muted`       | `oklch(0.22 0.01 250)` | Elementos secundários                  |
| `--border`      | `oklch(0.28 0.01 250)` | Bordas                                 |

Incluir no futuro:

- Reset CSS e tipografia (Fonte: Inter)
- Classes utilitárias para cards, badges, botões, tabelas, inputs, alerts
- Animações (pulse para status, transitions para hover)
- Layout responsivo (grid, sidebar collapse em mobile)
- Classes para gráficos (usaremos Chart.js via JS interop)

---

### 5. Componentes Layout

#### Components/Layout/MainLayout.razor

- Sidebar colapsável à esquerda
- Conteúdo principal à direita com header
- Indicador de status "Admin" com dot pulsante
- Botão de toggle da sidebar

#### Components/Layout/NavMenu.razor

- Logo do Frontend com ícone Shield Genérico
- 3 grupos de navegação:
  - **Principal**: Dashboard, Ambientes, Pessoas, Câmeras
  - **Gerenciamento**: Histórico de Acessos, Logs do Sistema
  - **Sistema**: Configurações, Ajuda
- Botão "Sair" no footer (Colocar login funcional)
- Active state highlight baseado na URL atual

---

### 6. Páginas

#### Pages/Login.razor

- Tela centralizada com card glassmorphism
- Campos email/senha com toggle de visibilidade
- Botão de loading ao submeter
- Background com blobs decorativos em blur

#### Pages/Dashboard.razor

- 4 Status Cards: Entradas Permitidas, Acessos Negados, Alertas T50, Pessoas Ativas
- Gráfico de área (Chart.js via JS Interop) — Acessos por Hora
- Card de Alertas Ativos com badges de gravidade
- Tabela de últimos acessos negados

#### Pages/Ambientes/Index.razor

- Grid de cards com status T50 (Online/Alerta/Offline)
- Busca + filtro por status
- Modal para criar novo ambiente
- Link para detalhe

#### Pages/Ambientes/Detalhe.razor

- Header com back button e status badge
- 4 tabs: Dispositivo T50, Pessoas com Acesso, Câmeras, Configurações
- Progress bar para capacidade biométrica
- Tabelas e modais para adicionar pessoa/câmera

#### Pages/Pessoas/Index.razor

- Grid de cards com avatar, badges de status e biometria
- Filtros: busca, status (ativo/inativo), departamento
- Modal para cadastrar nova pessoa

#### Pages/Pessoas/Detalhe.razor

- Layout 1/3 + 2/3 (perfil lateral + tabs)
- Ações: Editar Perfil, Resetar Biometria, Reenviar Senha, Inativar
- Tabs: Ambientes com Acesso, Histórico de Entradas

#### Pages/Cameras.razor

- Grid de cards com preview area (16:9)
- Indicador REC pulsante
- Filtros: busca, status, ambiente
- Modal para Live View e cadastro de câmera

#### Pages/Historico.razor

- 4 status cards resumo
- Card de filtros avançados
- Tabela com dados de acesso, status badge, link para gravação
- Botão exportar (CSV/PDF)

#### Pages/Logs.razor

- Filtros por categoria, usuário, busca
- Tabela com badges coloridas por categoria
- Botão exportar

#### Pages/Configuracoes.razor

- Card Retenção de Dados com select e alerta warning
- Card Tempo de Espera de Gravação com input numérico
- Estatísticas de armazenamento
- Botão salvar com loading state

#### Pages/Ajuda.razor

- Card de boas-vindas
- Grid de cards por seção
- Accordion detalhado com todos os tópicos
- Card de dicas de segurança

---

### 7. Dados Mock

#### Data/MockData.cs

- Classes: Ambiente, Pessoa, Camera, RegistroAcesso, LogSistema, Alerta
- Listas estáticas com os mesmos dados do projeto React

---

### 8. JavaScript Interop (Gráficos)

#### wwwroot/app.js

- Integração com Chart.js CDN para o gráfico de área do Dashboard
- Função `renderChart(canvasId, data)` chamada via `IJSRuntime`

---

### Rodar localmente

1. `dotnet build` — Compilação sem erros
2. `dotnet run` — Servidor inicia corretamente

### Rodar via Docker

1. [Baixe o Docker](https://docs.docker.com/desktop/setup/install/windows-install/)

2. Execute: 

```bash 

docker run -p 8100:8080 salomao1dev/frontend_cta_blazor_app

```

## Futuras Melhorias (Pré-requisitos para a versão 2.0.0)

#### Integrações
- Adicionar login funcional
- Adicionar registro de usuário
- Adicionar autenticação
- Adicionar banco de dados

#### UI
- Adicionar mais cores ao design system, principalmente ao light theme, que está muito claro
- Adicionar mais componentes ao design system
- Adicionar mais animações ao design system

#### Páginas
- Adicionar mais páginas ao dashboard
- Adicionar mais páginas ao sistema
- Adicionar mais páginas de gerenciamento
