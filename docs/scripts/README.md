# Scripts de Apoio

## gerar-hash.csx

Gera um hash BCrypt (fator 10) compatível com o `AuthController` do Int1. Use quando precisar inserir um administrador via SQL direto no banco — a coluna `senhaHash` precisa ser BCrypt, NÃO texto puro.

### Pré-requisito

```powershell
dotnet tool install -g dotnet-script
```

### Uso

```powershell
# Com argumento:
dotnet script gerar-hash.csx -- "MinhaSenha@123"

# Interativo (pede a senha):
dotnet script gerar-hash.csx
```

A saída inclui o hash pronto e um exemplo de `INSERT` para colar no DB Browser for SQLite.

### Por que isso é necessário

O `AuthController` valida login com `BCrypt.Verify(senhaInformada, admin.senhaHash)`. Se você fizer um INSERT colocando a senha em texto puro na coluna `senhaHash`, o verify retorna sempre `false` e o admin nunca consegue logar.
