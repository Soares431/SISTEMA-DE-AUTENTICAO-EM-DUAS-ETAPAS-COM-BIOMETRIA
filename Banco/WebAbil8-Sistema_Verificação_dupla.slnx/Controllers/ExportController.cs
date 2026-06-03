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

        // GET /api/export/historico.pdf
        [HttpGet("historico.pdf")]
        public IActionResult HistoricoPdf()
        {
            var dados = _tentativaRepo.ListarTodos()
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

        // GET /api/export/logs.pdf
        [HttpGet("logs.pdf")]
        public IActionResult LogsPdf()
        {
            var dados = _logRepo.ListarTodos()
                .OrderByDescending(l => l.DataHora)
                .Take(500)
                .ToList();

            var cabecalhos = new List<string> { "Data/Hora", "Admin", "Ação", "Entidade", "Entidade ID" };
            var linhas = dados.Select(l => new List<string>
            {
                l.DataHora.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss"),
                l.Administrador?.NomeCompleto ?? $"Admin {l.AdminId}",
                l.Acao,
                l.EntidadeAfetada,
                l.EntidadeId?.ToString() ?? "-"
            }).ToList();

            return PdfFile("Logs de Auditoria", cabecalhos, linhas, "logs.pdf");
        }

        // GET /api/export/pessoas.pdf
        [HttpGet("pessoas.pdf")]
        public async Task<IActionResult> PessoasPdf()
        {
            var dados = (await _pessoaRepo.ListarTodos())
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

        // GET /api/export/admins.pdf
        [HttpGet("admins.pdf")]
        public IActionResult AdminsPdf()
        {
            var dados = _adminRepo.ListarTodos();
            var cabecalhos = new List<string> { "Nome Completo", "Login", "Cargo", "Email" };
            var linhas = dados.Select(a => new List<string>
            {
                a.NomeCompleto ?? "-",
                a.Login ?? "-",
                a.Cargo ?? "-",
                a.Email ?? "-"
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

        // GET /api/export/relatorio-ambiente/{ambienteId}.pdf?de=...&ate=...
        [HttpGet("relatorio-ambiente/{ambienteId:int}.pdf")]
        public async Task<IActionResult> RelatorioAmbientePdf(int ambienteId, [FromQuery] DateTime? de, [FromQuery] DateTime? ate)
        {
            var ambiente = _ambienteRepo.BuscarPorId(ambienteId);
            if (ambiente == null) return NotFound();

            var pessoasAmb = _ambientePessoaRepo.ListarPessoasDoAmbiente(ambienteId);
            var tentativas = _tentativaRepo.ListarPorAmbiente(ambienteId)
                .Where(t => (!de.HasValue || t.DataHora >= de.Value)
                         && (!ate.HasValue || t.DataHora <= ate.Value))
                .OrderByDescending(t => t.DataHora)
                .ToList();

            var bytes = Document.Create(c => c.Page(p =>
            {
                p.Margin(20);
                p.Header().Column(col =>
                {
                    col.Item().Text($"Relatório do Ambiente: {ambiente.Nome}").FontSize(18).Bold();
                    if (de.HasValue || ate.HasValue)
                        col.Item().Text($"Período: {de?.ToString("dd/MM/yyyy") ?? "-"} a {ate?.ToString("dd/MM/yyyy") ?? "-"}").FontSize(10);
                });
                p.Content().Column(col =>
                {
                    col.Item().PaddingTop(10).Text("Pessoas com Acesso").Bold();
                    foreach (var pe in pessoasAmb)
                        col.Item().Text($"• {pe.Nome} ({pe.Cargo})").FontSize(10);

                    col.Item().PaddingTop(15).Text($"Tentativas no Período ({tentativas.Count})").Bold();
                    foreach (var t in tentativas.Take(200))
                    {
                        col.Item().Text(
                            $"{t.DataHora.ToLocalTime():dd/MM/yyyy HH:mm} — " +
                            $"{(t.Pessoa?.Nome ?? "Desconhecido")} — " +
                            $"{(t.AcessoLiberado ? "Permitido" : $"Negado ({t.MotivoNegacao})")}"
                        ).FontSize(9);
                    }
                });
            })).GeneratePdf();

            return File(bytes, "application/pdf", $"relatorio_ambiente_{ambienteId}.pdf");
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
