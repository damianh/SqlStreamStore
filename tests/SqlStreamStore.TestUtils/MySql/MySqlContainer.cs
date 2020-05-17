namespace SqlStreamStore.TestUtils.MySql
{
    using System.Threading;
    using System.Threading.Tasks;
    using Ductus.FluentDocker.Builders;
    using global::MySql.Data.MySqlClient;
    using SqlStreamStore.Infrastructure;

    public class MySqlContainer
    {
        private const string Image = "mysql:5.6";
        private const string ContainerName = "sql-stream-store-tests-mysql";
        private const int Port = 3306;

        private readonly string _databaseName;

        public MySqlContainer(string databaseName)
        {
            _databaseName = databaseName;

            var containerService = new Builder()
                .UseContainer()
                .WithName(ContainerName)
                .UseImage(Image)
                .KeepRunning()
                .ReuseIfExists()
                .WithEnvironment("MYSQL_ALLOW_EMPTY_PASSWORD=1")
                .ExposePort(Port, Port)
                .WaitForPort($"{Port}/tcp", 15000, "127.0.0.1")
                .Build();

            containerService.Start();
        }

        public string ConnectionString => ConnectionStringBuilder.ConnectionString;

        public async Task CreateDatabase(CancellationToken cancellationToken = default)
        {
            var commandText = $"CREATE DATABASE IF NOT EXISTS `{_databaseName}`";
            using (var connection = new MySqlConnection(DefaultConnectionString))
            {
                await connection.OpenAsync(cancellationToken).NotOnCapturedContext();

                using (var command = new MySqlCommand(commandText, connection))
                {
                    await command.ExecuteNonQueryAsync(cancellationToken).NotOnCapturedContext();
                }
            }
        }

        private string DefaultConnectionString => new MySqlConnectionStringBuilder(ConnectionString)
            {
                Database = null
            }.ConnectionString;

        private MySqlConnectionStringBuilder ConnectionStringBuilder => new MySqlConnectionStringBuilder
        {
            Database = _databaseName,
            Port = Port,
            UserID = "root",
            Pooling = true,
            MaximumPoolSize = 1000
        };
    }
}