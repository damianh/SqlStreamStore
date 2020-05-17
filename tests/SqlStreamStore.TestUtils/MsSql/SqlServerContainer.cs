namespace SqlStreamStore.TestUtils.MsSql
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Ductus.FluentDocker.Builders;
    using Microsoft.Data.SqlClient;
    using Polly;
    using SqlStreamStore.Infrastructure;

    public class SqlServerContainer
    {
        private readonly string _databaseName;
        private const string ContainerName = "sql-stream-store-tests-mssql";
        private const string Password = "!Passw0rd";
        private const string Image = "microsoft/mssql-server-linux:2017-CU9";
        private const int Port = 11433;

        public SqlServerContainer(string databaseName)
        {
            _databaseName = databaseName;
            
            var containerService = new Builder()
                .UseContainer()
                .WithName(ContainerName)
                .UseImage(Image)
                .KeepRunning()
                .ReuseIfExists()
                .WithEnvironment("ACCEPT_EULA=Y", $"SA_PASSWORD={Password}")
                .ExposePort(Port, Port)
                .WaitForPort($"{Port}/tcp", 5000, "127.0.0.1")
                .Build();

            containerService.Start();
        }

        public SqlConnection CreateConnection()
            => new SqlConnection(CreateConnectionStringBuilder().ConnectionString);

        public SqlConnectionStringBuilder CreateConnectionStringBuilder()
            => new SqlConnectionStringBuilder($"server=localhost,{Port};User Id=sa;Password={Password};Initial Catalog=master");

        public async Task CreateDatabase(CancellationToken cancellationToken = default)
        {
            var policy = Policy
                .Handle<SqlException>()
                .WaitAndRetryAsync(3, i => TimeSpan.FromSeconds(1));

            await policy.ExecuteAsync(async () =>
            {
                using(var connection = CreateConnection())
                {
                    await connection.OpenAsync(cancellationToken).NotOnCapturedContext();

                    var createCommand = $@"CREATE DATABASE [{_databaseName}]
ALTER DATABASE [{_databaseName}] SET SINGLE_USER
ALTER DATABASE [{_databaseName}] SET COMPATIBILITY_LEVEL=110
ALTER DATABASE [{_databaseName}] SET MULTI_USER";

                    using(var command = new SqlCommand(createCommand, connection))
                    {
                        await command.ExecuteNonQueryAsync(cancellationToken).NotOnCapturedContext();
                    }
                }
            });
        }
    }
}
 