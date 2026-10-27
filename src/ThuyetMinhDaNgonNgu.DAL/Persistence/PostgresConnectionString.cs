using Npgsql;

namespace ThuyetMinhDaNgonNgu.DAL.Persistence;

public static class PostgresConnectionString
{
    public static string Normalize(string value)
    {
        try
        {
            var builder = value.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
                          value.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase)
                ? FromUri(value)
                : new NpgsqlConnectionStringBuilder(value);

            if (string.IsNullOrWhiteSpace(builder.Host) ||
                string.IsNullOrWhiteSpace(builder.Database) ||
                string.IsNullOrWhiteSpace(builder.Username))
            {
                throw new ArgumentException();
            }

            return builder.ConnectionString;
        }
        catch (Exception exception) when (
            exception is ArgumentException or UriFormatException or FormatException)
        {
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection không hợp lệ. Dùng connection string PostgreSQL dạng " +
                "'Host=...;Port=5432;Database=postgres;Username=...;Password=...;SSL Mode=Require' " +
                "hoặc URI 'postgresql://username:password@host:5432/database?sslmode=require'. " +
                "Không dùng Supabase project URL https://... cho kết nối PostgreSQL.");
        }
    }

    private static NpgsqlConnectionStringBuilder FromUri(string value)
    {
        var uri = new Uri(value, UriKind.Absolute);
        if (uri.Scheme is not ("postgres" or "postgresql") ||
            string.IsNullOrWhiteSpace(uri.Host) ||
            string.IsNullOrWhiteSpace(uri.AbsolutePath.Trim('/')))
        {
            throw new ArgumentException();
        }

        string[] credentials = Uri.UnescapeDataString(uri.UserInfo).Split(':', 2);
        if (credentials.Length != 2 ||
            string.IsNullOrWhiteSpace(credentials[0]) ||
            string.IsNullOrWhiteSpace(credentials[1]))
        {
            throw new ArgumentException();
        }

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort ? 5432 : uri.Port,
            Database = Uri.UnescapeDataString(uri.AbsolutePath.Trim('/')),
            Username = credentials[0],
            Password = credentials[1]
        };

        foreach (string part in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            string[] pair = part.Split('=', 2);
            string key = Uri.UnescapeDataString(pair[0].Replace('+', ' '));
            string parameterValue = pair.Length == 2
                ? Uri.UnescapeDataString(pair[1].Replace('+', ' '))
                : string.Empty;

            if (key.Equals("sslmode", StringComparison.OrdinalIgnoreCase))
            {
                builder.SslMode = Enum.Parse<SslMode>(parameterValue, ignoreCase: true);
            }
            else if (key.Equals("application_name", StringComparison.OrdinalIgnoreCase))
            {
                builder.ApplicationName = parameterValue;
            }
            else if (key.Equals("timeout", StringComparison.OrdinalIgnoreCase))
            {
                builder.Timeout = int.Parse(parameterValue, System.Globalization.CultureInfo.InvariantCulture);
            }
            else if (key.Equals("command_timeout", StringComparison.OrdinalIgnoreCase))
            {
                builder.CommandTimeout = int.Parse(parameterValue, System.Globalization.CultureInfo.InvariantCulture);
            }
            else if (key.Equals("pooling", StringComparison.OrdinalIgnoreCase))
            {
                builder.Pooling = bool.Parse(parameterValue);
            }
            else
            {
                throw new ArgumentException();
            }
        }

        return builder;
    }
}
