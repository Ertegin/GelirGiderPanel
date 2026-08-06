using System.ComponentModel.DataAnnotations;

namespace GelirGiderPanel.Models
{
    public class LoginLog
    {
        public int Id { get; set; }

        [StringLength(256)]
        [Display(Name = "Kullanıcı")]
        public string UserName { get; set; } = string.Empty;

        [StringLength(45)] // IPv6 en fazla 45 karakter
        [Display(Name = "IP Adresi")]
        public string? IpAddress { get; set; }

        [Display(Name = "Giriş Zamanı")]
        public DateTime LoginTime { get; set; } = DateTime.Now;
    }
}
