using Microsoft.Data.Sqlite;

namespace Zaphira.Infrastructure.Persistence;

internal sealed class SqliteConnectionFactory
{
    public SqliteConnectionFactory(string databaseFile)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseFile);

        DatabaseFile = Path.GetFullPath(databaseFile);
    }

    public string DatabaseFile { get; }

    public async Task<SqliteConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        SqliteConnectionStringBuilder connectionStringBuilder = new()
        {
            DataSource = DatabaseFile,
            ForeignKeys = true
        };

        SqliteConnection connection = new(connectionStringBuilder.ToString());
        await connection.OpenAsync(cancellationToken);

        return connection;
    }
}
