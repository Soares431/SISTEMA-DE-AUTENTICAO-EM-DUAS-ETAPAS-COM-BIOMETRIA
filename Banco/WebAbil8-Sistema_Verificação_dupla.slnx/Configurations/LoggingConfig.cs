using Serilog;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Configurations
{
    public static class LoggingConfig
    {
        public static void AddSeriLogLogging(this
            WebApplicationBuilder builder)
        {
            Log.Logger = new LoggerConfiguration()
                   .ReadFrom.Configuration(builder.Configuration)
                   .Enrich.FromLogContext()
                   .WriteTo.Console()
                   .WriteTo.Debug()
                   .CreateLogger();
            builder.Host.UseSerilog()
                ;
        }
    }
}

