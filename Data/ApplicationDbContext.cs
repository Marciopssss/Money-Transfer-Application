using Microsoft.EntityFrameworkCore;

namespace Money_Trasnfer_Application.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) :
       base(options)
        {
        }
    }
}
