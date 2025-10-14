using AppNomesBr.Domain.Entities;
using AppNomesBr.Domain.Interfaces.Context;
using Microsoft.Extensions.Configuration;
using SQLite;
using System.Runtime.InteropServices;

namespace AppNomesBr.Infrastructure.Context
{
    public class LocalDbContext : ILocalDbContext
    {
        protected readonly SQLiteAsyncConnection connection;

        public LocalDbContext(IConfiguration configuration)
        {
            connection = InitializeConnection(configuration["DbName"] ?? string.Empty);
            InitializeDatabase(connection);
        }

        private static SQLiteAsyncConnection InitializeConnection(string dbName)
        {
            var caminhobanco = string.Compare("Microsoft Windows", RuntimeInformation.OSDescription.Substring(0, 17)) == 0 ?
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dbName) :
                Path.Combine(FileSystem.AppDataDirectory, dbName);

            return new SQLiteAsyncConnection(caminhobanco);
        }

        private static void InitializeDatabase(SQLiteAsyncConnection connection)
        {
            connection.CreateTableAsync<NomesBr>();
        }
    }
}
