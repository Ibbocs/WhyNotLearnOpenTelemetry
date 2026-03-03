using StackExchange.Redis;

namespace Order.API.RedisServices;

public class RedisService
{
    public RedisService(IConfiguration configuration)
    {
        var host = configuration.GetSection("Redis")["Host"];
        var port = configuration.GetSection("Redis")["Port"];

        var config = $"{host}:{port}";
        GetConnectionMultiplexer = ConnectionMultiplexer.Connect(config);
    }

    public ConnectionMultiplexer GetConnectionMultiplexer { get; }

    public IDatabase GetDb(int db) => GetConnectionMultiplexer.GetDatabase(db);
}