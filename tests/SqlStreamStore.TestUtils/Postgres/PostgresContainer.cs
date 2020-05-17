namespace SqlStreamStore.TestUtils.Postgres
{
    using System;
    using Ductus.FluentDocker.Builders;
    using Npgsql;

    public class PostgresContainer : PostgresDatabaseManager
    {
        private const string Image = "postgres:10.4-alpine";
        private const string ContainerName = "sql-stream-store-tests-postgres";
        private const int Port = 5432;

        public override string ConnectionString => ConnectionStringBuilder.ConnectionString;

        public PostgresContainer(string databaseName)
            : base(databaseName)
        {
            new Builder()
                .UseContainer()
                .WithName(ContainerName)
                .UseImage(Image)
                .KeepRunning()
                .ReuseIfExists()
                .ExposePort(Port, Port)
                .Command("-N", "500")
                .WaitForPort($"{Port}/tcp", 5000, "127.0.0.1")
                .Build()
                .Start();
        }

        private NpgsqlConnectionStringBuilder ConnectionStringBuilder => new NpgsqlConnectionStringBuilder
        {
            Database = DatabaseName,
            Password = Environment.OSVersion.IsWindows()
                ? "password"
                : null,
            Port = Port,
            Username = "postgres",
            Host = "localhost",
            Pooling = true,
            MaxPoolSize = 1024
        };
    }
}