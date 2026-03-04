using Moq;
using StackExchange.Redis;

namespace Tests.IntegrationTests;

public class RedisMockBuilder
{
    private readonly Dictionary<RedisKey, RedisValue> _kv = new();
    private readonly Dictionary<RedisKey, List<RedisValue>> _lists = new();
    
    public IConnectionMultiplexer BuildRedisMock()
    {
        var db = new Mock<IDatabase>();
        var sub = new Mock<ISubscriber>();
        var mux = new Mock<IConnectionMultiplexer>();

        // === Synchronous Methods ===
        
        // Overload 1: StringSet(KeyValuePair[] values, When when, CommandFlags flags)
        db.Setup(d => d.StringSet(
                It.IsAny<KeyValuePair<RedisKey, RedisValue>[]>(),
                It.IsAny<When>(),
                It.IsAny<CommandFlags>()))
            .Returns(true);

        // Overload 2: StringSet(key, value, expiry, when)
        db.Setup(d => d.StringSet(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<When>()))
            .Callback<RedisKey, RedisValue, TimeSpan?, When>((key, value, expiry, when) =>
            {
                _kv[key] = value;
            })
            .Returns(true);

        // Overload 3: StringSet(KeyValuePair[] values, When when, Expiration expiry, CommandFlags flags)
        db.Setup(d => d.StringSet(
                It.IsAny<KeyValuePair<RedisKey, RedisValue>[]>(),
                It.IsAny<When>(),
                It.IsAny<Expiration>(),
                It.IsAny<CommandFlags>()))
            .Returns(true);

        // Overload 4: StringSet(key, value, expiry, when, flags)
        db.Setup(d => d.StringSet(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<When>(),
                It.IsAny<CommandFlags>()))
            .Callback<RedisKey, RedisValue, TimeSpan?, When, CommandFlags>((key, value, expiry, when, flags) =>
            {
                _kv[key] = value;
            })
            .Returns(true);

        // Overload 5: StringSet(key, value, expiration, condition, flags)
        db.Setup(d => d.StringSet(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<Expiration>(),
                It.IsAny<ValueCondition>(),
                It.IsAny<CommandFlags>()))
            .Callback<RedisKey, RedisValue, Expiration, ValueCondition, CommandFlags>((key, value, expiration, condition, flags) =>
            {
                _kv[key] = value;
            })
            .Returns(true);

        // Overload 6: StringSet(key, value, expiry, keepTtl, when, flags)
        db.Setup(d => d.StringSet(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<bool>(),
                It.IsAny<When>(),
                It.IsAny<CommandFlags>()))
            .Callback<RedisKey, RedisValue, TimeSpan?, bool, When, CommandFlags>((key, value, expiry, keepttl, when, flags) =>
            {
                _kv[key] = value;
            })
            .Returns(true);

        // StringGet
        db.Setup(d => d.StringGet(
                It.IsAny<RedisKey>(),
                It.IsAny<CommandFlags>()))
            .Returns<RedisKey, CommandFlags>((key, flags) =>
            {
                if (_kv.TryGetValue(key, out var value))
                {
                    return value;
                }
                return RedisValue.Null;
            });

        // === Async Methods ===

        // StringSetAsync - Used by LocationService
        db.Setup(d => d.StringSetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<Expiration>(),
                It.IsAny<ValueCondition>(),
                It.IsAny<CommandFlags>()))
            .Callback<RedisKey, RedisValue, Expiration, ValueCondition, CommandFlags>((key, value, expiration, condition, flags) =>
            {
                _kv[key] = value;
            })
            .ReturnsAsync(true);

        // StringGetAsync - Used by LocationService
        db.Setup(d => d.StringGetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<CommandFlags>()))
            .Returns<RedisKey, CommandFlags>((key, flags) =>
            {
                if (_kv.TryGetValue(key, out var value))
                {
                    return Task.FromResult(value);
                }
                return Task.FromResult(RedisValue.Null);
            });

        // ListLeftPushAsync
        db.Setup(d => d.ListLeftPushAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<When>(),
                It.IsAny<CommandFlags>()))
            .Returns<RedisKey, RedisValue, When, CommandFlags>((key, value, when, flags) =>
            {
                if (!_lists.TryGetValue(key, out var list))
                {
                    list = new List<RedisValue>();
                    _lists[key] = list;
                }
                list.Insert(0, value);
                return Task.FromResult((long)list.Count);
            });

        // ListRangeAsync - Used by LocationService
        db.Setup(d => d.ListRangeAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<long>(),
                It.IsAny<long>(),
                It.IsAny<CommandFlags>()))
            .Returns<RedisKey, long, long, CommandFlags>((key, start, stop, flags) =>
            {
                if (!_lists.TryGetValue(key, out var list))
                    return Task.FromResult(Array.Empty<RedisValue>());

                var count = list.Count;
                var s = (int)Math.Max(0, start);
                var e = (int)Math.Min(stop, count - 1);
                if (count == 0 || s > e)
                    return Task.FromResult(Array.Empty<RedisValue>());

                return Task.FromResult(list.Skip(s).Take(e - s + 1).ToArray());
            });

        // ListTrimAsync
        db.Setup(d => d.ListTrimAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<long>(),
                It.IsAny<long>(),
                It.IsAny<CommandFlags>()))
            .Returns(Task.FromResult(true));

        // KeyExpireAsync
        db.Setup(d => d.KeyExpireAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CommandFlags>()))
            .Returns(Task.FromResult(true));

        // PublishAsync
        sub.Setup(s => s.PublishAsync(
                It.IsAny<RedisChannel>(),
                It.IsAny<RedisValue>(),
                It.IsAny<CommandFlags>()))
            .Returns(Task.FromResult(0L));

        // IConnectionMultiplexer setup
        mux.Setup(m => m.GetDatabase(It.IsAny<int>(), It.IsAny<object?>()))
            .Returns(db.Object);
        
        mux.Setup(m => m.GetSubscriber(It.IsAny<object?>()))
            .Returns(sub.Object);

        return mux.Object;
    }

    public void Clear()
    {
        _kv.Clear();
        _lists.Clear();
    }
}