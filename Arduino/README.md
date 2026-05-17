# Arduino — Hardware Customizado (5º CTA)

Hardware customizado desenvolvido para substituir o T50M Anviz durante a apresentação do projeto. Funciona de forma **completamente independente** — não precisa do sistema C# rodando.

---

## Componentes

| Componente | Conexão |
|---|---|
| Arduino Uno | — |
| LCD 16x2 I2C | SDA → A4, SCL → A5 |
| Keypad 4x4 | Linhas → pinos 2-5, Colunas → pinos 6-9 |
| AS608 | Mock no Wokwi — sensor real na Fase 3 |

---

## Como Simular (Wokwi VS Code)

1. Abre a pasta `Arduino/` no VS Code
2. Compila o sketch:
   ```powershell
   .\compilar.ps1
   ```
3. Abre o `diagram.json`
4. `F1` → `Wokwi: Start Simulator`

> O Wokwi requer licença gratuita. Acessa wokwi.com → GET YOUR LICENSE → `F1` → `Wokwi: Request a new License`

---

## Pessoas Mockadas

Definidas diretamente no `Arduino.ino`.

| ID | Senha | Status | Biometria |
|---|---|---|---|
| 100001 | 123456 | ativa | ❌ primeiro acesso |
| 100002 | 654321 | ativa | ✅ acesso normal |
| 100003 | 111111 | inativa | ❌ |
| 100004 | 999999 | ativa | ✅ acesso normal |

---

## Fluxo de Autenticação

```
Usuário digita ID (6 dígitos) + *
          ↓
Arduino consulta mock interno
          ↓
┌─────────────────────────────────────┐
│ ID não existe    → Acesso Negado    │
│ Pessoa inativa   → Acesso Negado    │
│ Tem biometria    → Coloque o dedo   │
│ Sem biometria    → Digite a senha   │
└─────────────────────────────────────┘
          ↓ (se pedir digital)
Simula digital (a cada 3 tentativas falha)
          ↓ (se pedir senha)
Valida senha → cadastra biometria → libera acesso
```

---

## Como Testar

### Teste 1 — Validação de 6 dígitos
1. Digita menos de 6 dígitos e aperta `*`
2. Esperado: `ID: 6 digitos!` por 1,5s

### Teste 2 — Acesso direto por digital (ID 100002)
1. Digita `100002` + `*`
2. Esperado no LCD: `Coloque o dedo`
3. Aguarda 2 segundos
4. Esperado: `Acesso Liberado!`

### Teste 3 — Primeiro acesso com cadastro de digital (ID 100001)
1. Digita `100001` + `*`
2. Esperado no LCD: `1o Acesso / Senha:`
3. Digita `123456` + `*`
4. Esperado no LCD: `Coloque o dedo para cadastrar`
5. Aguarda 2 segundos
6. Esperado no LCD: `Digital / Cadastrada!` → `Acesso Liberado / Bem vindo!`
7. Da próxima vez que digitar `100001` → vai direto para digital

### Teste 4 — Senha incorreta (ID 100001)
1. Digita `100001` + `*`
2. Digita `000000` + `*` (senha errada)
3. Esperado: `Acesso Negado! / senha_incorreta`

### Teste 5 — Pessoa inativa (ID 100003)
1. Digita `100003` + `*`
2. Esperado: `Acesso Negado! / inativo`

### Teste 6 — ID não cadastrado
1. Digita `999999` + `*`
2. Esperado: `Acesso Negado! / nao_cadastrado`

### Teste 7 — Digital rejeitada (3ª tentativa)
1. Faz o Teste 2 três vezes seguidas
2. Na terceira vez esperado: `Nao reconhecido`

### Teste 8 — Cancelamento
1. Digita alguns dígitos
2. Aperta `#`
3. Esperado: `Cancelado / Digite o ID:`

---

## Fases do Projeto

| Fase | Status | Descrição |
|---|---|---|
| Fase 1 | ✅ | Simulação no Wokwi — LCD + Keypad + mock biometria |
| Fase 2 | ✅ | Fluxo independente com dados mockados internos |
| Fase 3 | ⏳ | Hardware real — substituir mock pelo AS608 físico |
| Fase 4 | ⏳ | Integração com o Worker C# via serial |
