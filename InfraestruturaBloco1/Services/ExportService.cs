using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
namespace InfraestruturaBloco1.Services
{  
    public class ExportService
    {
        // Exportar CSV genérico
        public static void ExportarCSV<T>(IEnumerable<T> lista, string nomeArquivo)
        {
            using var writer = new StreamWriter(nomeArquivo);
            using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture));
            csv.WriteRecords(lista);
        }

        // Exportar PDF genérico
        public static void ExportarPDF(string titulo, List<string> cabecalhos, List<List<string>> linhas, string nomeArquivo)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(20);
                    page.Header().Text(titulo).FontSize(20).Bold();
                    page.Content().Table(table =>
                    {
                        // Definição das colunas
                        table.ColumnsDefinition(columns =>
                        {
                            foreach (var _ in cabecalhos)
                                columns.RelativeColumn();
                        });

                        // Cabeçalhos
                        table.Header(header =>
                        {
                            foreach (var cabecalho in cabecalhos)
                                header.Cell().Text(cabecalho).Bold();
                        });

                        // Linhas
                        foreach (var linha in linhas)
                        {
                            foreach (var valor in linha)
                                table.Cell().Text(valor);
                        }
                    });
                });
            });

            document.GeneratePdf(nomeArquivo);
        }
    }
}