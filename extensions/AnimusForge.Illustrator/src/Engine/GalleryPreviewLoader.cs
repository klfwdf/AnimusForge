using System;
using System.Threading;
using System.Threading.Tasks;

namespace AnimusForge.Illustrator.Engine
{
    // Called from the UI thread. At most one read/decode is in flight per gallery; rapid
    // selections replace the single pending request instead of queuing a task per click.
    internal sealed class GalleryPreviewLoader : IDisposable
    {
        private sealed class Request
        {
            internal string Path;
            internal Action<GauntletTextureLoader.PreparedImage> Ready;
            internal Action<string> Failed;
            internal CancellationTokenSource Cancellation;
        }

        private readonly Func<bool> _isCurrent;
        private readonly Action<Action> _post;
        private readonly Func<string, CancellationToken, GauntletTextureLoader.PreparedImage> _prepare;
        private Request _active;
        private Request _pending;
        private Request _latest;
        private bool _disposed;

        internal GalleryPreviewLoader(Func<bool> isCurrent, Action<Action> post,
            Func<string, CancellationToken, GauntletTextureLoader.PreparedImage> prepare)
        {
            _isCurrent = isCurrent ?? throw new ArgumentNullException(nameof(isCurrent));
            _post = post ?? throw new ArgumentNullException(nameof(post));
            _prepare = prepare ?? throw new ArgumentNullException(nameof(prepare));
        }

        internal void Select(string path, Action<GauntletTextureLoader.PreparedImage> ready, Action<string> failed)
        {
            if (_disposed) return;
            _latest = _pending = new Request { Path = path, Ready = ready, Failed = failed };
            CancelActive();
            StartPending();
        }

        internal void Cancel()
        {
            _latest = _pending = null;
            CancelActive();
        }

        private void CancelActive()
        {
            try { _active?.Cancellation?.Cancel(); }
            catch (ObjectDisposedException) { } // Worker already finished; queued completion is still stale.
        }

        private void StartPending()
        {
            if (_disposed || _active != null || _pending == null) return;
            if (!_isCurrent()) { Cancel(); return; }
            var request = _pending;
            _pending = null;
            _active = request;
            request.Cancellation = new CancellationTokenSource();
            var token = request.Cancellation.Token;
            Task.Run(() =>
            {
                GauntletTextureLoader.PreparedImage prepared = null;
                Exception error = null;
                try
                {
                    token.ThrowIfCancellationRequested();
                    prepared = _prepare(request.Path, token);
                    token.ThrowIfCancellationRequested();
                }
                catch (Exception ex) { error = ex; }
                finally { request.Cancellation.Dispose(); }
                _post(() => Complete(request, prepared, error));
            });
        }

        private void Complete(Request request, GauntletTextureLoader.PreparedImage prepared, Exception error)
        {
            if (!ReferenceEquals(_active, request)) return;
            _active = null;
            try
            {
                // Evaluated on the game thread before invoking any GPU registration callback.
                if (_disposed || !ReferenceEquals(_latest, request) || !_isCurrent()) return;
                if (error != null) request.Failed?.Invoke(error.Message);
                else if (prepared == null) request.Failed?.Invoke("图片不存在或无法解码，请刷新画廊。");
                else request.Ready?.Invoke(prepared);
            }
            finally { StartPending(); }
        }

        public void Dispose()
        {
            _disposed = true;
            Cancel();
        }
    }
}
