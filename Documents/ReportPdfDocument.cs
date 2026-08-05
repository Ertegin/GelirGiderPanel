using GelirGiderPanel.ViewModels;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GelirGiderPanel.Documents
{
    /// <summary>
    /// Gelir-Gider raporunun PDF şablonu (QuestPDF).
    /// Ekran ve Excel ile aynı ReportViewModel verisini kullanır.
    /// </summary>
    public class ReportPdfDocument:IDocument
    {
        // Panel renk paleti
        private const string ColorInk = "#1b2733";
        private const string ColorIncome = "#0e7a5f";
        private const string ColorExpense = "#b3423a";
        private const string ColorHeaderBg = "#14231f";
        private const string ColorLightBg = "#f5f7f6";

        private readonly ReportViewModel _report;
        private readonly string _filterText;

        public ReportPdfDocument(ReportViewModel report, string filterText)
        {
            _report = report;
            _filterText = filterText;
        }

        public DocumentMetadata GetMetadata() => new()
        {
            Title = "Gelir-Gider Raporu",
            Author = "Hesap Defteri"
        };
        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36);
                page.DefaultTextStyle(x => x.FontSize(9).FontColor(ColorInk));

                page.Header().Element(ComposeHeader);
                page.Content().Element(ComposeContent);
                page.Footer().Element(ComposeFooter);
            });
        }

          // ================= BAŞLIK =================
        private void ComposeHeader(IContainer container)
        {
            container.PaddingBottom(12).Column(col =>
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("GELİR - GİDER RAPORU")
                            .FontSize(16).Bold().FontColor(ColorHeaderBg);
                        c.Item().Text($"Dönem: {_report.StartDate:dd.MM.yyyy} – {_report.EndDate:dd.MM.yyyy}")
                            .FontSize(10);
                        c.Item().Text($"Filtre: {_filterText}")
                            .FontSize(8).Italic().FontColor(Colors.Grey.Darken1);
                    });

                    row.ConstantItem(140).AlignRight().Column(c =>
                    {
                        c.Item().AlignRight().Text("Güven Tekstil")
                            .FontSize(12).Bold().FontColor(ColorIncome);
                        c.Item().AlignRight().Text($"Oluşturulma: {DateTime.Now:dd.MM.yyyy HH:mm}")
                            .FontSize(7).FontColor(Colors.Grey.Darken1);
                    });
                });

                col.Item().PaddingTop(8).LineHorizontal(1).LineColor(ColorHeaderBg);
            });
        }

        // ================= İÇERİK =================
        private void ComposeContent(IContainer container)
        {
            container.Column(col =>
            {
                col.Spacing(14);

                // ---- Özet kutuları ----
                col.Item().Row(row =>
                {
                    row.Spacing(10);
                    row.RelativeItem().Element(c => SummaryBox(c, "TOPLAM GELİR", _report.TotalIncome, ColorIncome));
                    row.RelativeItem().Element(c => SummaryBox(c, "TOPLAM GİDER", _report.TotalExpense, ColorExpense));
                    row.RelativeItem().Element(c => SummaryBox(c, "NET BAKİYE", _report.NetBalance,
                        _report.NetBalance >= 0 ? ColorIncome : ColorExpense));
                });

                // ---- Kategori dökümü ----
                col.Item().Text("Kategori Dökümü").FontSize(12).Bold();
                col.Item().Element(ComposeCategoryTable);

                // ---- İşlem detayları ----
                col.Item().Text($"İşlem Detayları ({_report.Transactions.Count} kayıt)").FontSize(12).Bold();
                col.Item().Element(ComposeTransactionTable);
            });
        }

        private static void SummaryBox(IContainer container, string title, decimal amount, string color)
        {
            container
                .Background(ColorLightBg)
                .Border(1).BorderColor(Colors.Grey.Lighten2)
                .Padding(10)
                .Column(c =>
                {
                    c.Item().Text(title).FontSize(7).Bold().FontColor(Colors.Grey.Darken1);
                    c.Item().Text($"{amount:N2} TL").FontSize(13).Bold().FontColor(color);
                });
        }

        private void ComposeCategoryTable(IContainer container)
        {
            if (!_report.CategorySummary.Any())
            {
                container.Text("Seçilen filtrelere uygun işlem bulunamadı.")
                    .Italic().FontColor(Colors.Grey.Darken1);
                return;
            }

            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3);   // Kategori
                    columns.RelativeColumn(2);   // Gelir
                    columns.RelativeColumn(2);   // Gider
                    columns.RelativeColumn(2);   // Net
                    columns.RelativeColumn(1.5f);// İşlem sayısı
                });

                table.Header(header =>
                {
                    header.Cell().Element(HeaderCell).Text("Kategori");
                    header.Cell().Element(HeaderCell).AlignRight().Text("Gelir");
                    header.Cell().Element(HeaderCell).AlignRight().Text("Gider");
                    header.Cell().Element(HeaderCell).AlignRight().Text("Net");
                    header.Cell().Element(HeaderCell).AlignCenter().Text("İşlem");
                });

                bool alternate = false;
                foreach (var c in _report.CategorySummary)
                {
                    var bg = alternate ? ColorLightBg : "#FFFFFF";
                    alternate = !alternate;

                    table.Cell().Element(x => DataCell(x, bg)).Text(c.CategoryName).SemiBold();
                    table.Cell().Element(x => DataCell(x, bg)).AlignRight()
                        .Text($"{c.Income:N2} TL").FontColor(ColorIncome);
                    table.Cell().Element(x => DataCell(x, bg)).AlignRight()
                        .Text($"{c.Expense:N2} TL").FontColor(ColorExpense);
                    table.Cell().Element(x => DataCell(x, bg)).AlignRight()
                        .Text($"{c.Net:N2} TL").SemiBold()
                        .FontColor(c.Net >= 0 ? ColorIncome : ColorExpense);
                    table.Cell().Element(x => DataCell(x, bg)).AlignCenter()
                        .Text(c.TransactionCount.ToString());
                }
            });
        }

        private void ComposeTransactionTable(IContainer container)
        {
            if (!_report.Transactions.Any())
            {
                container.Text("Seçilen filtrelere uygun işlem bulunamadı.")
                    .Italic().FontColor(Colors.Grey.Darken1);
                return;
            }

            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(60);  // Tarih
                    columns.RelativeColumn(4);   // Açıklama
                    columns.RelativeColumn(2);   // Kategori
                    columns.ConstantColumn(45);  // Tür
                    columns.RelativeColumn(2);   // Tutar
                });

                table.Header(header =>
                {
                    header.Cell().Element(HeaderCell).Text("Tarih");
                    header.Cell().Element(HeaderCell).Text("Açıklama");
                    header.Cell().Element(HeaderCell).Text("Kategori");
                    header.Cell().Element(HeaderCell).Text("Tür");
                    header.Cell().Element(HeaderCell).AlignRight().Text("Tutar");
                });

                bool alternate = false;
                foreach (var t in _report.Transactions)
                {
                    bool isIncome = t.TransactionTypeId == 1;
                    var bg = alternate ? ColorLightBg : "#FFFFFF";
                    alternate = !alternate;

                    table.Cell().Element(x => DataCell(x, bg)).Text(t.Date.ToString("dd.MM.yyyy"));
                    table.Cell().Element(x => DataCell(x, bg)).Text(t.Description);
                    table.Cell().Element(x => DataCell(x, bg)).Text(t.Category?.Name ?? "-");
                    table.Cell().Element(x => DataCell(x, bg))
                        .Text(t.TransactionType?.Name ?? "-")
                        .FontColor(isIncome ? ColorIncome : ColorExpense);
                    table.Cell().Element(x => DataCell(x, bg)).AlignRight()
                        .Text($"{(isIncome ? "+" : "-")}{t.Amount:N2} TL").SemiBold()
                        .FontColor(isIncome ? ColorIncome : ColorExpense);
                }

                // Net toplam satırı
                table.Cell().ColumnSpan(4).Element(TotalCell).AlignRight().Text("NET TOPLAM").Bold();
                table.Cell().Element(TotalCell).AlignRight()
                    .Text($"{_report.NetBalance:N2} TL").Bold()
                    .FontColor(_report.NetBalance >= 0 ? ColorIncome : ColorExpense);
            });
        }

        // ---- Hücre stilleri ----
        private static IContainer HeaderCell(IContainer container) =>
            container.Background(ColorHeaderBg).PaddingVertical(5).PaddingHorizontal(6)
                .DefaultTextStyle(x => x.FontColor(Colors.White).Bold().FontSize(8.5f));

        private static IContainer DataCell(IContainer container, string background) =>
            container.Background(background)
                .BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                .PaddingVertical(4).PaddingHorizontal(6);

        private static IContainer TotalCell(IContainer container) =>
            container.BorderTop(1.5f).BorderColor(ColorHeaderBg)
                .PaddingVertical(6).PaddingHorizontal(6);

        // ================= ALT BİLGİ =================
        private void ComposeFooter(IContainer container)
        {
            container.PaddingTop(8).Row(row =>
            {
                row.RelativeItem().Text($"DefterPanel • {_report.StartDate:dd.MM.yyyy} – {_report.EndDate:dd.MM.yyyy}")
                    .FontSize(7).FontColor(Colors.Grey.Darken1);

                row.RelativeItem().AlignRight().Text(text =>
                {
                    text.DefaultTextStyle(x => x.FontSize(7).FontColor(Colors.Grey.Darken1));
                    text.Span("Sayfa ");
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
            });
        }
    }
}
