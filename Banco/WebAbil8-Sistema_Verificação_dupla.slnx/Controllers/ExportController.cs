using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using WebAbil8_Sistema_Verificação_dupla.slnx.Services;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ExportController : ControllerBase
    {
        private readonly ITentativaAcessoRepository _tentativaRepo;
        private readonly ILogAdminRepository _logRepo;
        private readonly IAmbienteRepository _ambienteRepo;
        private readonly IPessoaRepository _pessoaRepo;
        private readonly IAmbientePessoaRepository _ambientePessoaRepo;
        private readonly IAdministradorRepository _adminRepo;

        static ExportController()
        {
            // QuestPDF community license — necessário rodar uma vez no startup do processo
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public ExportController(
            ITentativaAcessoRepository tentativaRepo,
            ILogAdminRepository logRepo,
            IAmbienteRepository ambienteRepo,
            IPessoaRepository pessoaRepo,
            IAmbientePessoaRepository ambientePessoaRepo,
            IAdministradorRepository adminRepo)
        {
            _tentativaRepo = tentativaRepo;
            _logRepo = logRepo;
            _ambienteRepo = ambienteRepo;
            _pessoaRepo = pessoaRepo;
            _ambientePessoaRepo = ambientePessoaRepo;
            _adminRepo = adminRepo;
        }

        // GET /api/export/historico.pdf?search=&status=&ambienteId=&tipo=&gravacao=&de=&ate=
        [HttpGet("historico.pdf")]
        public IActionResult HistoricoPdf(
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromQuery] int? ambienteId,
            [FromQuery] string? tipo,
            [FromQuery] string? gravacao,
            [FromQuery] DateTime? de,
            [FromQuery] DateTime? ate)
        {
            var dados = AplicarFiltrosHistorico(
                _tentativaRepo.ListarTodos(),
                search, status, ambienteId, tipo, gravacao, de, ate)
                .OrderByDescending(t => t.DataHora)
                .Take(500)
                .ToList();

            var cabecalhos = new List<string> { "Data/Hora", "Pessoa", "Ambiente", "Status", "Motivo" };
            var linhas = dados.Select(t => new List<string>
            {
                t.DataHora.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss"),
                t.Pessoa?.Nome ?? (t.PessoaId.HasValue ? $"ID {t.PessoaId}" : "Desconhecido"),
                t.Ambiente?.Nome ?? $"Ambiente {t.AmbienteId}",
                t.AcessoLiberado ? "Permitido" : "Negado",
                t.MotivoNegacao ?? "-"
            }).ToList();

            return PdfFile("Histórico de Acessos", cabecalhos, linhas, "historico.pdf");
        }

        private static IEnumerable<Model.TentativaAcesso> AplicarFiltrosHistorico(
            IEnumerable<Model.TentativaAcesso> fonte,
            string? search, string? status, int? ambienteId,
            string? tipo, string? gravacao, DateTime? de, DateTime? ate)
        {
            return fonte.Where(h =>
                (string.IsNullOrEmpty(search) ||
                    (h.Pessoa?.Nome?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (h.Ambiente?.Nome?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)) &&
                (string.IsNullOrEmpty(status) || status == "todos" ||
                    (status == "permitido" ? h.AcessoLiberado : !h.AcessoLiberado)) &&
                (!ambienteId.HasValue || h.AmbienteId == ambienteId.Value) &&
                (string.IsNullOrEmpty(tipo) || tipo == "todos" || h.TipoVerificacao == tipo) &&
                (string.IsNullOrEmpty(gravacao) || gravacao == "todos" ||
                    (gravacao == "com" ? !string.IsNullOrEmpty(h.GravacaoPath) : string.IsNullOrEmpty(h.GravacaoPath))) &&
                (!de.HasValue  || h.DataHora.Date >= de.Value.Date) &&
                (!ate.HasValue || h.DataHora.Date <= ate.Value.Date));
        }

        // GET /api/export/logs.pdf?search=&acao=&entidade=&adminId=
        [HttpGet("logs.pdf")]
        public IActionResult LogsPdf(
            [FromQuery] string? search,
            [FromQuery] string? acao,
            [FromQuery] string? entidade,
            [FromQuery] int? adminId)
        {
            var dados = _logRepo.ListarTodos().Where(l =>
                (string.IsNullOrEmpty(search) ||
                    l.Acao.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    l.EntidadeAfetada.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    (l.Administrador?.NomeCompleto?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)) &&
                (string.IsNullOrEmpty(acao) || acao == "todos" || l.Acao == acao) &&
                (string.IsNullOrEmpty(entidade) || entidade == "todos" || l.EntidadeAfetada == entidade) &&
                (!adminId.HasValue || l.AdminId == adminId.Value))
                .OrderByDescending(l => l.DataHora)
                .Take(500)
                .ToList();

            var cabecalhos = new List<string> { "Data/Hora", "Admin", "Ação", "Entidade" };
            var linhas = dados.Select(l => new List<string>
            {
                l.DataHora.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss"),
                l.Administrador?.NomeCompleto ?? $"Admin {l.AdminId}",
                l.Acao,
                l.EntidadeAfetada
            }).ToList();

            return PdfFile("Logs de Auditoria", cabecalhos, linhas, "logs.pdf");
        }

        // GET /api/export/pessoas.pdf?search=&status=&modo=&biometria=
        [HttpGet("pessoas.pdf")]
        public async Task<IActionResult> PessoasPdf(
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromQuery] string? modo,
            [FromQuery] string? biometria)
        {
            var dados = (await _pessoaRepo.ListarTodos()).Where(p =>
                (string.IsNullOrEmpty(search) ||
                    (p.Nome?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (p.Email?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (p.Cargo?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)) &&
                (string.IsNullOrEmpty(status) || status == "todos" || p.Status == status) &&
                (string.IsNullOrEmpty(modo) || modo == "todos" || p.modoAcesso == modo) &&
                (string.IsNullOrEmpty(biometria) || biometria == "todos" ||
                    (biometria == "sim" ? p.biometriaCadastrada != null : p.biometriaCadastrada == null)))
                .OrderBy(p => p.Nome)
                .ToList();

            var cabecalhos = new List<string> { "Nome Completo", "CPF", "Cargo" };
            var linhas = dados.Select(p => new List<string>
            {
                p.Nome ?? "-",
                FormatarCpf(p.Cpf),
                p.Cargo ?? "-"
            }).ToList();

            return PdfFile("Pessoas Cadastradas", cabecalhos, linhas, "pessoas.pdf");
        }

        // GET /api/export/admins.pdf?search=
        // Por decisão de segurança: NÃO exporta Login (vetor de ataque dispensável).
        [HttpGet("admins.pdf")]
        public IActionResult AdminsPdf([FromQuery] string? search)
        {
            var dados = _adminRepo.ListarTodos().Where(a =>
                string.IsNullOrEmpty(search) ||
                (a.NomeCompleto?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (a.Cargo?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (a.Email?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false))
                .ToList();

            var cabecalhos = new List<string> { "Nome Completo", "CPF", "Cargo", "Email", "Telefone", "Cadastro" };
            var linhas = dados.Select(a => new List<string>
            {
                a.NomeCompleto ?? "-",
                FormatarCpf(a.Cpf),
                a.Cargo ?? "-",
                a.Email ?? "-",
                FormatarTelefone(a.Telefone),
                a.DataCriacao.ToLocalTime().ToString("dd/MM/yyyy")
            }).ToList();

            return PdfFile("Administradores", cabecalhos, linhas, "admins.pdf");
        }

        private static string FormatarCpf(string? cpf)
        {
            if (string.IsNullOrWhiteSpace(cpf)) return "-";
            var digitos = new string(cpf.Where(char.IsDigit).ToArray());
            if (digitos.Length != 11) return cpf;
            return $"{digitos.Substring(0,3)}.{digitos.Substring(3,3)}.{digitos.Substring(6,3)}-{digitos.Substring(9,2)}";
        }

        private static string FormatarTelefone(string? telefone)
        {
            if (string.IsNullOrWhiteSpace(telefone)) return "-";
            var d = new string(telefone.Where(char.IsDigit).ToArray());
            return d.Length switch
            {
                11 => $"({d.Substring(0,2)}) {d.Substring(2,5)}-{d.Substring(7,4)}",
                10 => $"({d.Substring(0,2)}) {d.Substring(2,4)}-{d.Substring(6,4)}",
                _ => telefone
            };
        }

        private IActionResult PdfFile(string titulo, List<string> cabecalhos, List<List<string>> linhas, string filename)
        {
            var bytes = Document.Create(c => c.Page(p =>
            {
                p.Margin(20);
                p.Header().Text(titulo).FontSize(18).Bold();
                p.Content().Table(t =>
                {
                    t.ColumnsDefinition(cd => { foreach (var _ in cabecalhos) cd.RelativeColumn(); });
                    t.Header(h => { foreach (var c in cabecalhos) h.Cell().Text(c).Bold(); });
                    foreach (var linha in linhas)
                        foreach (var v in linha) t.Cell().Text(v).FontSize(9);
                });
                p.Footer().AlignRight().Text(x =>
                {
                    x.Span("Gerado em ").FontSize(8);
                    x.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm")).FontSize(8);
                });
            })).GeneratePdf();
            return File(bytes, "application/pdf", filename);
        }
    }
}
