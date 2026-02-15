using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Board_Game_Software.Data
{
    public class ApplicationDbContextMain : IdentityDbContext
    {
        public ApplicationDbContextMain(DbContextOptions<ApplicationDbContextMain> options)
            : base(options)
        {
        }
    }
}
