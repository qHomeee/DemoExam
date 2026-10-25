using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace DemoForMuhin.Data;

public static class DatabaseSettings
{
    public static DbContextOptions<CatalogDbContext> CreateOptions()
    {
        var connection = Environment.GetEnvironmentVariable("DEMOFORMUHIN_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connection))
        {
            var configPath = FindLocalConfiguration();
            if (configPath is not null)
                connection = File.ReadAllText(configPath).Trim();
        }
        if (string.IsNullOrWhiteSpace(connection))
            throw new InvalidOperationException("Подключение не настроено. Выполните Scripts/Setup-Database.ps1 и нажмите «Обновить из БД».");

        var builder = new NpgsqlConnectionStringBuilder(connection)
        {
            Timeout = 5,
            CommandTimeout = 15,
            IncludeErrorDetail = false
        };
        return new DbContextOptionsBuilder<CatalogDbContext>().UseNpgsql(builder.ConnectionString).Options;
    }

    private static string? FindLocalConfiguration()
    {
        foreach (var start in new[] { AppContext.BaseDirectory, Environment.CurrentDirectory })
        {
            for (var directory = new DirectoryInfo(start); directory is not null; directory = directory.Parent)
            {
                var path = Path.Combine(directory.FullName, ".local", "connection-string.txt");
                if (File.Exists(path))
                    return path;
            }
        }
        return null;
    }
}
