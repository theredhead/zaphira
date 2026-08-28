namespace Zaphira.Domain;

public enum MessageStatus
{
    Pending = 0,
    Streaming = 1,
    Completed = 2,
    Cancelled = 3,
    Failed = 4
}
