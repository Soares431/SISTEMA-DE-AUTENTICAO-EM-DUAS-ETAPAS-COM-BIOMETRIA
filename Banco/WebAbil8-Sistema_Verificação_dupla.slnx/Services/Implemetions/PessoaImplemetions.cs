using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;
using WebAbil8_Sistema_Verificação_dupla.slnx.Model;
using WebAbil8_Sistema_Verificação_dupla.slnx.Model.Context;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Services.Implemetions
{
    public class PessoaImplemetions : IPessoaRepository
    {

            private readonly AppDbContext _context;
            private readonly string _aesKey;

            public PessoaImplemetions(AppDbContext context, IConfiguration configuration)
            {
                _context = context;
                _aesKey = configuration["AesKey"] ?? "5cta-aes-key-senha-segura-32char";
            }

            public async Task<Pessoa> Adicionar(Pessoa pessoa)
            {
                var cpfExistente = await _context.Pessoas.AnyAsync(p => p.Cpf == pessoa.Cpf);
                if (cpfExistente)
                    throw new InvalidOperationException("CPF já cadastrado.");

                // Criptografa senhaClear com AES antes de persistir (bug #4)
                if (!string.IsNullOrEmpty(pessoa.senhaClear))
                    pessoa.senhaClear = EncryptAes(pessoa.senhaClear, _aesKey);

                await _context.Pessoas.AddAsync(pessoa);
                await _context.SaveChangesAsync();
                return pessoa;
            }

            public async Task<List<Pessoa>> ListarTodos()
            {
                return await _context.Pessoas.ToListAsync();
            }

            public async Task<Pessoa> BuscarPorCPF(string cpf)
            {
                return await _context.Pessoas
                    .FirstOrDefaultAsync(p => p.Cpf == cpf);
            }

            public async Task<Pessoa> BuscarPorId(long id)
            {
                return await _context.Pessoas.FindAsync(id);
            }

            public async Task<Pessoa> Atualizar(Pessoa pessoa)
            {
                var existing = await _context.Pessoas.FindAsync(pessoa.Id);
                if (existing == null)
                    throw new ArgumentNullException("ID não encontrado.");
                _context.Entry(existing).CurrentValues.SetValues(pessoa);
                await _context.SaveChangesAsync();
                return pessoa;
            }

            public async Task Remover(long id)
            {
                var pessoa = await _context.Pessoas.FindAsync(id);
                if (pessoa == null)
                    throw new ArgumentNullException("ID não encontrado.");
                _context.Pessoas.Remove(pessoa);
                await _context.SaveChangesAsync();
            }

            public async Task<Pessoa> AtualizarUltimoAcesso(long pessoaId)
            {
                var pessoa = await _context.Pessoas.FindAsync(pessoaId);
                if (pessoa == null) throw new ArgumentNullException("Usuário inexistente.");
                pessoa.dataUltimoAcesso = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return pessoa;
            }

            public async Task AlterarStatus(long pessoaId, bool status)
            {
                var pessoa = await _context.Pessoas.FindAsync(pessoaId);
                if (pessoa == null) throw new ArgumentNullException("Usuário inexistente.");
                pessoa.Status = status ? "ativo" : "inativo";
                await _context.SaveChangesAsync();
            }

            public async Task<Pessoa> MarcarBiometriaCadastrada(long pessoaId)
            {
                var pessoa = await _context.Pessoas.FindAsync(pessoaId);
                if (pessoa == null) throw new ArgumentNullException("Usuário inexistente.");
                pessoa.biometriaCadastrada = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return pessoa;
            }

            public async Task<Pessoa> SalvarTemplate(long pessoaId, byte[] template)
            {
                var pessoa = await _context.Pessoas.FindAsync(pessoaId);
                if (pessoa == null) throw new ArgumentNullException("Usuário inexistente.");
                pessoa.templateBackup = template;
                await _context.SaveChangesAsync();
                return pessoa;
            }

            // Mesmo algoritmo do AesService no Int4 (sem referência circular: Int4 já referencia Int1)
            private static string EncryptAes(string plainText, string key)
            {
                using var aes = Aes.Create();
                aes.Key = Encoding.UTF8.GetBytes(key.PadRight(32).Substring(0, 32));
                aes.GenerateIV();

                using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                using var ms = new MemoryStream();
                ms.Write(aes.IV, 0, aes.IV.Length);

                using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                using (var sw = new StreamWriter(cs))
                {
                    sw.Write(plainText);
                }

                return Convert.ToBase64String(ms.ToArray());
            }
        }

    }
