using GelirGiderPanel.Models;

namespace GelirGiderPanel.ViewModels
{
    /// <summary>
    /// Ana sayfa (Dashboard) özet verileri.
    /// </summary>
    public class DashboardViewModel
    {
        public decimal TotalIncome { get; set; }     // Toplam Gelir
        public decimal TotalExpense { get; set; }    // Toplam Gider
        public decimal NetBalance => TotalIncome - TotalExpense; // Net Bakiye

        public int TransactionCount { get; set; }    // Toplam işlem sayısı
        public List<Transaction> RecentTransactions { get; set; } = new(); // Son 5 işlem
    }
}
