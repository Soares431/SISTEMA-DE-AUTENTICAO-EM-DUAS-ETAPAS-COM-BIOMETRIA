using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
                _aesKey = AesHelper.ResolverChave(configuration);
            }

            public async Task<Pessoa> Adicionar(Pessoa pessoa)
            {
                var cpfExistente = await _context.Pessoas.AnyAsync(p => p.Cpf == pessoa.Cpf);
                if (cpfExistente)
                    throw new InvalidOperationException("CPF já cadastrado.");

                if (!string.IsNullOrEmpty(pessoa.senhaClear) && !string.IsNullOrEmpty(pessoa.CodigoUsuario)
                    && pessoa.senhaClear == pessoa.CodigoUsuario)
                    throw new InvalidOperationException("A senha não pode ser igual ao ID da pessoa.");

                if (!string.IsNullOrEmpty(pessoa.senhaClear))
                    pessoa.senhaClear = AesHelper.Encrypt(pessoa.senhaClear, _aesKey);

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

            public async Task<Pessoa?> BuscarPorCodigoUsuario(string codigoUsuario)
            {
                return await _context.Pessoas
                    .FirstOrDefaultAsync(p => p.CodigoUsuario == codigoUsuario);
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

            public async Task<Pessoa> AtualizarSenha(long pessoaId, string novaSenhaClear, string novoSenhaHash)
            {
                var pessoa = await _context.Pessoas.FindAsync(pessoaId);
                if (pessoa == null) throw new ArgumentNullException("Usuário inexistente.");

                if (!string.IsNullOrEmpty(novaSenhaClear) && !string.IsNullOrEmpty(pessoa.CodigoUsuario)
                    && novaSenhaClear == pessoa.CodigoUsuario)
                    throw new InvalidOperationException("A senha não pode ser igual ao ID da pessoa.");

                pessoa.senhaClear = AesHelper.Encrypt(novaSenhaClear, _aesKey);
                pessoa.senhaHash = novoSenhaHash;
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

            public async Task<int?> AlocarSlotAs608Livre()
            {
                var ocupados = await _context.Pessoas
                    .Where(p => p.SlotAs608 != null)
                    .Select(p => p.SlotAs608!.Value)
                    .ToListAsync();
                var setOcupados = new HashSet<int>(ocupados);
                for (int s = 1; s <= 127; s++)
                {
                    if (!setOcupados.Contains(s)) return s;
                }
                return null;
            }

            public async Task DefinirSlotAs608(long pessoaId, int slot)
            {
                var pessoa = await _context.Pessoas.FindAsync(pessoaId);
                if (pessoa == null) throw new ArgumentNullException("Usuário inexistente.");
                pessoa.SlotAs608 = slot;
                await _context.SaveChangesAsync();
            }

            public async Task EnfileirarSlotParaApagar(long pessoaId)
            {
                var pessoa = await _context.Pessoas.FindAsync(pessoaId);
                if (pessoa == null) throw new ArgumentNullException("Usuário inexistente.");

                if (pessoa.SlotAs608 != null)
                {
                    pessoa.SlotAs608ParaApagar = pessoa.SlotAs608;
                    pessoa.SlotAs608 = null;
                    await _context.SaveChangesAsync();
                }
            }

            public async Task<List<Pessoa>> ListarSlotsPendentesApagar()
            {
                return await _context.Pessoas
                    .Where(p => p.SlotAs608ParaApagar != null)
                    .ToListAsync();
            }

            public async Task LimparSlotPendenteApagar(long pessoaId)
            {
                var pessoa = await _context.Pessoas.FindAsync(pessoaId);
                if (pessoa == null) return;
                pessoa.SlotAs608ParaApagar = null;
                await _context.SaveChangesAsync();
            }

        }

    }

