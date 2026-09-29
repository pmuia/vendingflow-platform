namespace PaymentService.Infrastructure.Database;

public interface IConnection
{
    string ConnectionString { get; }
}

public class Connection(string connectionString) : IConnection
{
    public string ConnectionString { get; private set; } = connectionString;
}
