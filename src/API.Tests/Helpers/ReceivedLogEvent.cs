using Microsoft.Extensions.Logging;

namespace API.Tests.Helpers;

public class ReceivedLogEvent
{
    public LogLevel Level { get; set; }

    public string? Message { get; set; }
}