using GelirGiderPanel.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GelirGiderPanel.Documents
{
    /// <summary>
    /// Maaş listesi PDF şablonu 
    /// </summary>
    public class SalaryPdfDocument : IDocument
    {
        private readonly List<Salary> _salaries;

        private const string DarkColor = "#14231f";
        private const string ExpenseColor = "#b3423a";
        private const string BgColor = "#f5f7f6";

        public SalaryPdfDocument(List<Salary> salaries) => _salaries = salaries;

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(ComposeHeader);
                page.Content().Element(ComposeContent);
                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("Sayfa ");
                    t.CurrentPageNumber();
                    t.Span(" / ");
                    t.TotalPages();
                });
            });
        }

        private void ComposeHeader(IContainer container)
        {
            container.Column(col =>
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem().Text("Maaş Listesi")
                        .FontSize(16).Bold().FontColor(DarkColor);
                    row.ConstantItem(140).AlignRight()
                        .Text($"Tarih: {DateTime.Now:dd.MM.yyyy}")
                        .FontSize(9).FontColor("#666666");
                });

                col.Item().PaddingTop(10).Row(row =>
                {
                    row.RelativeItem().Background(BgColor).Padding(10).Column(c =>
                    {
                        c.Item().Text("Personel Sayısı").FontSize(8).FontColor("#666666");
                        c.Item().Text($"{_salaries.Count}").FontSize(12).Bold().FontColor(DarkColor);
                    });
                    row.ConstantItem(8);
                    row.RelativeItem().Background(BgColor).Padding(10).Column(c =>
                    {
                        c.Item().Text("Toplam Maaş").FontSize(8).FontColor("#666666");
                        c.Item().Text($"{_salaries.Sum(s => s.Amount):N2} TL")
                            .FontSize(12).Bold().FontColor(ExpenseColor);
                    });
                });

                col.Item().PaddingTop(10);
            });
        }

        private void ComposeContent(IContainer container)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.RelativeColumn(2);   // İsim
                    cols.ConstantColumn(90);  // Maaş
                    cols.RelativeColumn(3);   // Açıklama
                });

                table.Header(header =>
                {
                    foreach (var t in new[] { "İsim", "Maaş", "Açıklama" })
                    {
                        header.Cell().Background(DarkColor).Padding(5)
                            .Text(t).FontColor("#ffffff").Bold().FontSize(9);
                    }
                });

                bool zebra = false;
                foreach (var s in _salaries)
                {
                    string bg = zebra ? BgColor : "#ffffff";
                    zebra = !zebra;

                    table.Cell().Background(bg).Padding(4).Text(s.Name);
                    table.Cell().Background(bg).Padding(4).AlignRight()
                        .Text($"{s.Amount:N2}");
                    table.Cell().Background(bg).Padding(4)
                        .Text(s.Description ?? "").FontColor("#666666");
                }

                // Toplam satırı
                table.Cell().Background(DarkColor).Padding(5)
                    .Text("TOPLAM").FontColor("#ffffff").Bold();
                table.Cell().Background(DarkColor).Padding(5).AlignRight()
                    .Text($"{_salaries.Sum(s => s.Amount):N2} TL")
                    .FontColor("#ffffff").Bold();
                table.Cell().Background(DarkColor).Padding(5).Text("");
            });
        }
    }
}
