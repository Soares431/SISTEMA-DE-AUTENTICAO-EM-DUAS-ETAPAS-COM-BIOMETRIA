using Microsoft.EntityFrameworkCore;
using WebAbil8_Sistema_Verificação_dupla.slnx.Model.Context;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Configurations
{
    public static class DataBaseConfig
    {
        //  public static IServiceCollection AddDataBaseConfiguration(
        //    this IServiceCollection services, IConfiguration configuration)
        //{
        //    var connectionString = configuration["SQLiteConnections: QLiteConnectionsString"];
        //    if (string.IsNullOrEmpty(connectionString))
        //    {
        //        throw new ArgumentNullException("Connection String não encontrada");
        //    }
        //    services.AddDbContext<MSSQLContext>(options =>
        //    options.UseSqlite(connectionString));
        //    return services;
        //}

        public static IServiceCollection AddDataBaseConfiguration(
          this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration["SQLiteConnection:SQLiteConnectionString"];
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentNullException("Connection String não encontrada");
            }
            services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(connectionString));
            return services;
        }

    }
}
