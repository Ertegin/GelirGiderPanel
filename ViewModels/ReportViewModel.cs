using GelirGiderPanel.Models;

namespace GelirGiderPanel.ViewModels
{
    /// <summary>
    /// Tarih aralığına göre rapor ekranı verileri.
    /// </summary>
    public class ReportViewModel
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public decimal TotalIncome { get; set; }
        public decimal TotalExpense { get; set; }
        public decimal NetBalance => TotalIncome - TotalExpense;

        /// <summary>Seçilen aralıktaki tüm işlemler (tarih sıralı).</summary>
        public List<Transaction> Transactions { get; set; } = new();

        /// <summary>Kategori bazında gelir/gider dökümü.</summary>
        public List<CategoryReportRow> CategorySummary { get; set; } = new();
    }

    public class CategoryReportRow
    {
        public string CategoryName { get; set; } = null!;
        public decimal Income { get; set; }
        public decimal Expense { get; set; }
        public decimal Net => Income - Expense;
        public int TransactionCount { get; set; }
    }
}
