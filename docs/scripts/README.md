# Gerenciamento de Administradores

> **Decisão de segurança**: por se tratar de projeto militar (5º CTA), a criação, edição e troca de senha de administradores **só é feita via acesso direto ao banco**. O painel web não expõe nenhuma dessas operações. Quem opera o banco já tem o servidor — não há ganho de segurança em permitir essas ações pela UI, e há risco real de escalação de privilégio se um admin comum conseguir acesso ao painel.

Esta pasta contém o script auxiliar para gerar a senha criptografada exigida pelo banco.

---

## 1. Pré-requisito

Instale o `dotnet-script` (uma única vez no servidor):

```powershell
dotnet tool install -g dotnet-script
```

---

## 2. Criar um novo administrador

### 2.1 Gerar o hash da senha

```powershell
cd "C:\caminho\para\docs\scripts"

# Forma 1: passa a senha como argumento
dotnet script gerar-hash.csx -- "MinhaSenha@Forte123"

# Forma 2: interativo (não exibe no histórico do PowerShell)
dotnet script gerar-hash.csx
```

A saída inclui o hash BCrypt e um exemplo de `INSERT` pronto:

```
Hash BCrypt (fator 10):
$2a$10$abcd1234efgh5678ijkl9012mnop3456qrst7890uvwxyzABCDEF

Exemplo de INSERT:
INSERT INTO administrador (login, senhaHash, nomeCompleto, ...)
VALUES ('login_aqui', '$2a$10$...', ...);
```

### 2.2 Executar o INSERT no banco

Use uma das duas ferramentas abaixo:

#### Opção A — DBeaver (recomendado)

1. Abra o DBeaver. Conexão SQLite → aponte para `Banco/WebAbil8-Sistema_Verificação_dupla.slnx/banco.db`.
2. Clique em "SQL Editor" (Ctrl+Enter abre uma nova aba SQL).
3. Cole o INSERT (modelo abaixo).
4. Selecione a linha do INSERT e pressione **Ctrl+Enter** para executar.
5. **IMPORTANTE:** pressione **Ctrl+Shift+C** para fazer commit da transação — sem isto, o INSERT continua só na transação local e o Int1 não vê o novo admin. (Alternativamente: clique no botão de commit na barra de transações ou ative auto-commit em Connection View → ☑ Auto-commit).

> Sintoma comum: "Inseri o admin mas o login dele não funciona." Quase sempre é falta de commit no DBeaver.

#### Opção B — DB Browser for SQLite (mais simples)

1. Arquivo → Abrir → `Banco/WebAbil8-Sistema_Verificação_dupla.slnx/banco.db`
2. Aba "Executar SQL"
3. Cole o INSERT, clique em "Executar" (ou F5)
4. **Salve as alterações** (Ctrl+S) — sem isso a alteração fica só em memória

#### Modelo do INSERT

```sql
INSERT INTO administrador (login, senhaHash, nomeCompleto, cpf, email, cargo, telefone, dataCriacao)
VALUES (
    'capitao.silva',                                                  -- login (curto, identificador único)
    '$2a$10$...hash gerado no passo 2.1...',                           -- senha cifrada com BCrypt
    'Cap José da Silva',                                              -- nome completo
    '12345678901',                                                    -- CPF apenas dígitos
    'jose.silva@ebmail.eb.mil.br',                                    -- email
    'Capitão',                                                        -- cargo/patente
    '11987654321',                                                    -- telefone apenas dígitos
    datetime('now')                                                   -- data de criação
);
```

> O Int1 **não precisa ser reiniciado**. O próximo login do novo admin já funciona — desde que o commit tenha sido feito.

---

## 3. Trocar a senha de um administrador existente

### 3.1 Gerar o hash da nova senha

```powershell
dotnet script gerar-hash.csx -- "NovaSenha@Segura456"
```

### 3.2 Atualizar o registro

```sql
UPDATE administrador
SET senhaHash = '$2a$10$novo-hash-aqui...'
WHERE login = 'capitao.silva';
```

No DBeaver: **Ctrl+Shift+C** para commitar.
No DB Browser: **Ctrl+S** para salvar.

---

## 4. Inativar/remover um administrador

A tabela `administrador` não tem coluna de status. Para impedir o login, há duas opções:

### 4.1 Remover definitivamente

```sql
DELETE FROM administrador WHERE login = 'admin_antigo';
```

> Cuidado: os logs antigos do admin removido ficam com `adminId` órfão (FK não cascateia). É proposital — auditoria de ações passadas se mantém. A exibição vira "Admin {id}" no painel.

### 4.2 Trocar a senha pra um valor aleatório

Se quiser manter o registro pra rastreamento futuro:

```powershell
dotnet script gerar-hash.csx -- "$(New-Guid)"
```

E executar o UPDATE. A senha gerada é descartada — ninguém consegue logar.

---

## 5. Por que isso é feito assim

- **Login** não aparece na UI nem em exportações — saber o login é metade do trabalho de um atacante.
- **Senha** só é armazenada como hash BCrypt fator 10 — mesmo com dump do banco, atacante precisa fazer brute-force.
- **Quem tem acesso ao banco já tem acesso ao servidor**. Permitir CRUD de admins pela UI seria um vetor de escalação: um admin comprometido poderia criar outro admin pra si sem deixar rastro distinguível.
- **Auditoria preservada**: todas as ações dos admins ficam registradas em `logAdmin` com `adminId` — a remoção do registro do admin não apaga os logs históricos.

---

## 6. Script disponível

### gerar-hash.csx

Recebe uma senha (argumento ou interativo) e retorna o hash BCrypt fator 10, compatível com a verificação feita pelo `AuthController` do Int1.

```csharp
// Validação no AuthController:
BCrypt.Net.BCrypt.Verify(senhaInformada, admin.senhaHash)
```

Se você inserir a senha em texto puro na coluna `senhaHash`, o `Verify` retorna sempre `false` e ninguém consegue logar — daí a necessidade do script.
