using Microsoft.EntityFrameworkCore;
using InfraestruturaBloco1.Models;

namespace InfraestruturaBloco1.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // DbSet para os logs de auditoria
        public DbSet<AuditLog> AuditLogs { get; set; }
    }
}
