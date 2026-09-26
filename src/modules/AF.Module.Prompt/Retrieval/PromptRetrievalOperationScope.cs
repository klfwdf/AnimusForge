using System;
using System.Threading;

namespace AnimusForge;

// Restore ambient request context before releasing its pinned configuration revision.
internal sealed class PromptRetrievalOperationScope : IDisposable
{
    private IDisposable _context;
    private IDisposable _configuration;

    internal PromptRetrievalOperationScope(IDisposable context, IDisposable configuration)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public void Dispose()
    {
        IDisposable context = Interlocked.Exchange(ref _context, null);
        if (context == null) return;
        IDisposable configuration = Interlocked.Exchange(ref _configuration, null);
        try { context.Dispose(); }
        finally { configuration?.Dispose(); }
    }
}
