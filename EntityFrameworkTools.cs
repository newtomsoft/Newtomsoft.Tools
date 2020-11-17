using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;

namespace Newtomsoft.Tools
{
    public static class EntityFrameworkTools
    {
        public static string AddPathToSqliteConectionString(string path, string connectionString)
        {
            string[] splitConnectionString = connectionString.Split("#PATH#");
            return splitConnectionString[0] + Path.Combine(path, splitConnectionString[1]);
        }
    }

    public static class EntityFrameworkTools<T> where T : DbContext
    {
        private const string SQLITE = "Sqlite";
        private const string SQLSERVER = "SqlServer";
        private const string POSTGRESQL = "PostgreSql";
        private const string IN_MEMORY = "InMemory";
        private const string DEVELOPMENT = "Development";

        private static void UseDatabase(DbContextOptionsBuilder<T> optionBuilder, string persistence, string connectionString)
        {
            if (persistence == SQLSERVER)
                optionBuilder.UseSqlServer(connectionString);
            else if (persistence == POSTGRESQL)
                optionBuilder.UseNpgsql(connectionString);
            else if (persistence == SQLITE)
                optionBuilder.UseSqlite(connectionString);
        }

        public static void AddDbContext(IServiceCollection services, IConfiguration configuration)
        {
            string persistence = Environment.GetEnvironmentVariable("PERSISTENCE");
            if (persistence == IN_MEMORY)
                services.AddDbContext<T>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()), ServiceLifetime.Scoped);
            else if (persistence == SQLITE)
                services.AddDbContext<T>(options => options.UseSqlite(EntityFrameworkTools.AddPathToSqliteConectionString(Path.Combine(Directory.GetCurrentDirectory()), configuration.GetConnectionString(SQLITE))));
            else if (persistence == SQLSERVER)
                services.AddDbContext<T>(options => options.UseSqlServer(configuration.GetConnectionString(SQLSERVER)), ServiceLifetime.Scoped);
            else
                throw new ArgumentException("No DbContext defined !");
        }

        public static T CreateDbContext(string guiProjectName)
        {
            DbContextOptionsBuilder<T> optionBuilder = new DbContextOptionsBuilder<T>();
            string path = Path.Combine(Directory.GetCurrentDirectory(), "..", guiProjectName);
            string env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? DEVELOPMENT;
            string persistence = Environment.GetEnvironmentVariable("PERSISTENCE") ?? SQLITE;
            Console.WriteLine($"ASPNETCORE_ENVIRONMENT is : {env} ; PERSISTENCE is : {persistence}");
            IConfigurationBuilder builder = new ConfigurationBuilder()
                               .SetBasePath(path)
                               .AddJsonFile($"appsettings.{env}.json");
            IConfigurationRoot config = builder.Build();
            string connectionString = config.GetConnectionString(persistence);
            if (persistence == SQLITE)
                connectionString = EntityFrameworkTools.AddPathToSqliteConectionString(path, connectionString);
            Console.WriteLine($"connectionString is : {connectionString}");
            UseDatabase(optionBuilder, persistence, connectionString);
            return (T)Activator.CreateInstance(typeof(T), optionBuilder.Options);
        }
    }
}
