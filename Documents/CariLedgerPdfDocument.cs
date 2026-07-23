using GelirGiderPanel.Controllers;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GelirGiderPanel.Documents
{
    public class CariLedgerPdfDocument:IDocument
    {
        private readonly CariLedgerVm _vm;

        private const string DarkColor = "#14231f";
        private const string DebitColor = "#b3423a";   // Borç
        private const string CreditColor = "#0e7a5f";  // Alacak
        private const string BgColor = "#f5f7f6";

        public CariLedgerPdfDocument(CariLedgerVm vm) => _vm = vm;

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(9));

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
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text($"Cari Defter — {_vm.Account.Name}")
                            .FontSize(16).Bold().FontColor(DarkColor);
                        string donem = _vm.IsFiltered
                            ? $"Dönem: {_vm.StartDate?.ToString("dd.MM.yyyy") ?? "…"} – {_vm.EndDate?.ToString("dd.MM.yyyy") ?? "…"}"
                            : "Dönem: Tüm kayıtlar";
                        c.Item().Text(donem).FontSize(10).FontColor("#666666");
                    });
                    row.ConstantItem(140).AlignRight().Text($"Oluşturma: {DateTime.Now:dd.MM.yyyy HH:mm}")
                        .FontSize(8).FontColor("#999999");
                });

                // Özet kutuları
                col.Item().PaddingTop(10).Row(row =>
                {
                    SummaryBox(row.RelativeItem(),
                        _vm.IsFiltered ? "Dönem Başı" : "Devir",
                        _vm.PeriodOpeningBalance, DarkColor);
                    row.ConstantItem(8);
                    SummaryBox(row.RelativeItem(), "Toplam Borç", _vm.TotalDebit, DebitColor);
                    row.ConstantItem(8);
                    SummaryBox(row.RelativeItem(), "Toplam Alacak", _vm.TotalCredit, CreditColor);
                    row.ConstantItem(8);
                    SummaryBox(row.RelativeItem(),
                        _vm.IsFiltered ? "Dönem Sonu" : "Güncel Bakiye",
                        _vm.CurrentBalance,
                        _vm.CurrentBalance >= 0 ? CreditColor : DebitColor);
                });

                col.Item().PaddingTop(10);
            });
        }

        private static void SummaryBox(IContainer container, string label, decimal value, string color)
        {
            container.Background(BgColor).Padding(8).Column(c =>
            {
                c.Item().Text(label).FontSize(8).FontColor("#666666");
                c.Item().Text($"{value:N2} TL").FontSize(11).Bold().FontColor(color);
            });
        }

        private void ComposeContent(IContainer container)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.ConstantColumn(58);   // Tarih
                    cols.RelativeColumn(3);    // Açıklama
                    cols.ConstantColumn(42);   // Miktar
                    cols.ConstantColumn(52);   // B.Fiyat
                    cols.ConstantColumn(65);   // Borç
                    cols.ConstantColumn(65);   // Alacak
                    cols.ConstantColumn(70);   // Bakiye
                });

                table.Header(header =>
                {
                    string[] titles = { "Tarih", "Açıklama", "Miktar", "B.Fiyat",
                                        "Borç", "Alacak", "Bakiye" };
                    foreach (var t in titles)
                    {
                        header.Cell().Background(DarkColor).Padding(4)
                            .Text(t).FontColor("#ffffff").Bold().FontSize(8);
                    }
                });

                bool zebra = false;
                foreach (var r in _vm.Rows)
                {
                    var t = r.Transaction;
                    string bg = zebra ? BgColor : "#ffffff";
                    zebra = !zebra;

                    table.Cell().Background(bg).Padding(3).Text(t.Date.ToString("dd.MM.yyyy"));
                    table.Cell().Background(bg).Padding(3).Text(t.Description);
                    table.Cell().Background(bg).Padding(3).AlignRight()
                        .Text(t.Quantity.HasValue ? $"{t.Quantity:N0}" : "");
                    table.Cell().Background(bg).Padding(3).AlignRight()
                        .Text(t.UnitPrice.HasValue ? $"{t.UnitPrice:N2}" : "");
                    table.Cell().Background(bg).Padding(3).AlignRight()
                        .Text(t.DebitAmount > 0 ? $"{t.DebitAmount:N2}" : "")
                        .FontColor(DebitColor);
                    table.Cell().Background(bg).Padding(3).AlignRight()
                        .Text(t.CreditAmount > 0 ? $"{t.CreditAmount:N2}" : "")
                        .FontColor(CreditColor);
                    table.Cell().Background(bg).Padding(3).AlignRight()
                        .Text($"{r.RunningBalance:N2}").Bold()
                        .FontColor(r.RunningBalance >= 0 ? CreditColor : DebitColor);
                }

                // Toplam satırı
                table.Cell().ColumnSpan(4).Background(DarkColor).Padding(4)
                    .Text("TOPLAM").FontColor("#ffffff").Bold();
                table.Cell().Background(DarkColor).Padding(4).AlignRight()
                    .Text($"{_vm.TotalDebit:N2}").FontColor("#ffffff").Bold();
                table.Cell().Background(DarkColor).Padding(4).AlignRight()
                    .Text($"{_vm.TotalCredit:N2}").FontColor("#ffffff").Bold();
                table.Cell().Background(DarkColor).Padding(4).AlignRight()
                    .Text($"{_vm.CurrentBalance:N2} TL").FontColor("#ffffff").Bold();
            });
        }
    }
}
