using System.Data.OleDb;
using System.Runtime.Versioning;
using Microsoft.Extensions.Options;

namespace ECommerce.Infrastructure.Data
{
    [SupportedOSPlatform("windows")]
    public class AccessDbContext : IDisposable
    {
        private readonly string _connectionString;
        private OleDbConnection? _connection;
        private bool _disposed;

        public AccessDbContext(IOptions<DatabaseOptions> options)
        {
            if (!OperatingSystem.IsWindows())
            {
                throw new PlatformNotSupportedException("This database provider is only supported on Windows.");
            }

            if (string.IsNullOrEmpty(options.Value.DatabasePath))
            {
                throw new ArgumentNullException(nameof(options.Value.DatabasePath), "DatabasePath is not configured.");
            }

            _connectionString = $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={options.Value.DatabasePath};Persist Security Info=False;";
            Console.WriteLine($"Connection String: {_connectionString}");
        }

        public AccessDbContext(string dbPath)
        {
            if (!OperatingSystem.IsWindows())
            {
                throw new PlatformNotSupportedException("This database provider is only supported on Windows.");
            }

            _connectionString = $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={dbPath};Persist Security Info=False;";
        }

        public OleDbConnection GetConnection()
        {
            if (_connection == null)
            {
                _connection = new OleDbConnection(_connectionString);
            }

            if (_connection.State != System.Data.ConnectionState.Open)
            {
                _connection.Open();
            }

            return _connection;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (_connection != null)
                    {
                        if (_connection.State == System.Data.ConnectionState.Open)
                        {
                            _connection.Close();
                        }
                        _connection.Dispose();
                        _connection = null;
                    }
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~AccessDbContext()
        {
            Dispose(false);
        }
    }

    public class DatabaseOptions
    {
        public required string DatabasePath { get; set; }
    }
}