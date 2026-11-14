using System;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace DemoForMuhin.Presentation;

public static class DatabaseError
{
    public static string Message(Exception error)
    {
        var cause = error is DbUpdateException ? error.InnerException ?? error : error;
        return cause switch
        {
            PostgresException { SqlState: PostgresErrorCodes.UndefinedTable } =>
                "В базе нет таблиц заказов. Для действующей базы выполните Database/orders.sql в pgAdmin или DBeaver.",
            PostgresException { SqlState: PostgresErrorCodes.InsufficientPrivilege } =>
                "Нет прав на операцию в PostgreSQL. Проверьте права пользователя базы данных.",
            PostgresException { SqlState: PostgresErrorCodes.LockNotAvailable or PostgresErrorCodes.QueryCanceled } =>
                "База данных занята. Повторите действие через несколько секунд.",
            InvalidOperationException invalid => invalid.Message,
            _ => "Не удалось выполнить операцию. Проверьте подключение к PostgreSQL и настройки приложения."
        };
    }
}
