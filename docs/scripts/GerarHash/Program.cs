string senha;
if (args.Length > 0)
{
    senha = args[0];
}
else
{
    Console.Write("Senha: ");
    senha = Console.ReadLine() ?? "";
}

if (string.IsNullOrWhiteSpace(senha))
{
    Console.Error.WriteLine("ERRO: senha vazia.");
    Environment.Exit(1);
}

var hash = BCrypt.Net.BCrypt.HashPassword(senha, workFactor: 10);
Console.WriteLine();
Console.WriteLine("Hash BCrypt (fator 10):");
Console.WriteLine(hash);
Console.WriteLine();
Console.WriteLine("Exemplo de INSERT:");
Console.WriteLine($@"INSERT INTO administrador (login, senhaHash, nomeCompleto, cpf, email, cargo, telefone, dataCriacao)
VALUES (
    'login_aqui',
    '{hash}',
    'Nome Completo',
    '00000000000',
    'email@ebmail.eb.mil.br',
    'Cargo',
    '(00) 00000-0000',
    datetime('now')
);");
