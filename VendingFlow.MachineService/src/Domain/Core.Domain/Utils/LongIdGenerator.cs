namespace MachineService.Domain.Utils;

public static class LongIdGenerator
{
    private static readonly long EpochMilliseconds = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds();
    private static readonly object Lock = new();
    private static long _lastTimestamp = -1;
    private static long _sequence;

    public static long NextId()
    {
        lock (Lock)
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (timestamp < EpochMilliseconds)
                throw new InvalidOperationException("System clock is before the id epoch.");

            if (timestamp == _lastTimestamp)
            {
                _sequence = (_sequence + 1) & 4095;
                if (_sequence == 0)
                    timestamp = WaitForNextMillisecond(timestamp);
            }
            else
            {
                _sequence = 0;
            }

            _lastTimestamp = timestamp;
            return ((timestamp - EpochMilliseconds) << 12) | _sequence;
        }
    }

    private static long WaitForNextMillisecond(long timestamp)
    {
        var nextTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        while (nextTimestamp <= timestamp)
            nextTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        return nextTimestamp;
    }
}
