using System.Collections.Concurrent;
using Zaphira.Domain;

namespace Zaphira.Server.Chat;

internal sealed class GenerationCancellationRegistry
{
    private readonly ConcurrentDictionary<MessageId, CancellationTokenSource> activeGenerations = new();

    public CancellationToken Register(MessageId messageId, CancellationToken requestAborted)
    {
        CancellationTokenSource cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(requestAborted);
        activeGenerations[messageId] = cancellationTokenSource;

        return cancellationTokenSource.Token;
    }

    public bool Cancel(MessageId messageId)
    {
        if (!activeGenerations.TryRemove(messageId, out CancellationTokenSource? cancellationTokenSource))
        {
            return false;
        }

        cancellationTokenSource.Cancel();
        cancellationTokenSource.Dispose();

        return true;
    }

    public void Complete(MessageId messageId)
    {
        if (activeGenerations.TryRemove(messageId, out CancellationTokenSource? cancellationTokenSource))
        {
            cancellationTokenSource.Dispose();
        }
    }
}
