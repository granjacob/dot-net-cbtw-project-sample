using ServiceFlow.Requests.Application.Abstractions;

namespace ServiceFlow.Requests.Application.Services;

public sealed class RequestIdGenerator(TimeProvider timeProvider, int nodeId = 0) : IRequestIdGenerator
{
    public const long MaxJavaScriptSafeInteger = 9_007_199_254_740_991;
    public const int MaxNodeId = 63;

    private const long EpochUnixMilliseconds = 1_735_689_600_000; // 2025-01-01T00:00:00Z
    private const int SequenceBits = 6;
    private const int NodeBits = 6;
    private const int NodeShift = SequenceBits;
    private const int TimestampShift = SequenceBits + NodeBits;
    private const long MaxSequence = (1L << SequenceBits) - 1;
    private const long MaxTimestamp = (1L << 41) - 1;

    private readonly object _sync = new();
    private readonly int _nodeId = ValidateNodeId(nodeId);
    private long _lastTimestamp = -1;
    private long _sequence;

    public long NewId()
    {
        lock (_sync)
        {
            var timestamp = timeProvider.GetUtcNow().ToUnixTimeMilliseconds() - EpochUnixMilliseconds;
            if (timestamp < 0)
            {
                throw new InvalidOperationException("The system clock is earlier than the request id epoch.");
            }

            // A clock adjustment must never make ids repeat. When more than 64 ids are
            // requested in one millisecond, advance the logical clock by one millisecond.
            timestamp = Math.Max(timestamp, _lastTimestamp);
            if (timestamp == _lastTimestamp)
            {
                if (_sequence == MaxSequence)
                {
                    timestamp = _lastTimestamp + 1;
                    _sequence = 0;
                }
                else
                {
                    _sequence++;
                }
            }
            else
            {
                _sequence = 0;
            }

            if (timestamp > MaxTimestamp)
            {
                throw new InvalidOperationException("The request id timestamp exceeds the 53-bit allocation.");
            }

            _lastTimestamp = timestamp;
            var id = (timestamp << TimestampShift) | ((long)_nodeId << NodeShift) | _sequence;
            if (id <= 0 || id > MaxJavaScriptSafeInteger)
            {
                throw new InvalidOperationException("The generated request id is outside JavaScript's safe integer range.");
            }

            return id;
        }
    }

    private static int ValidateNodeId(int nodeId) => nodeId is >= 0 and <= MaxNodeId
        ? nodeId
        : throw new ArgumentOutOfRangeException(
            nameof(nodeId),
            nodeId,
            $"Node id must be between 0 and {MaxNodeId}.");
    }
