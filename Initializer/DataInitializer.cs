using GelirGiderPanel.Models;
using Microsoft.EntityFrameworkCore;

namespace GelirGiderPanel.Initializer
{
    public static class DataInitializer
    {
        
        public static void Seed(ModelBuilder modelBuilder)
        {
            string pass1 = BCrypt.Net.BCrypt.HashPassword("guvenTek");
            string pass2 = BCrypt.Net.BCrypt.HashPassword("9988");
            /*
             modelBuilder.Entity<AppUser>().HasData(
                new AppUser() { ID=1,UserName="Admin",Password=pass1,Role=Enums.Role.Admin},
                new AppUser() { ID = 2, UserName = "test", Password = pass2, Role = Enums.Role.User }
                );

             */

            modelBuilder.Entity<AppUser>().HasData(

                new AppUser() { ID=1, UserName="Admin", Password=pass1, Role=Enums.Role.Admin},
                new AppUser() { ID = 2, UserName = "Guven", Password = pass2, Role = Enums.Role.User }
                );
        }
    }
}
