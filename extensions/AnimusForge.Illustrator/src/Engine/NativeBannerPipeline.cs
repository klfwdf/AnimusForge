using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AnimusForge.Illustrator.Engine
{
    // Holds only validated PNG strings, never shared native textures.
    internal sealed class NativeBannerPipeline
    {
        private readonly Func<string, int, bool, CancellationToken, Task<byte[]>> _render;
        private readonly SemaphoreSlim _serial = new SemaphoreSlim(1, 1);
        private readonly object _sync = new object();
        private readonly Dictionary<string, string> _cache = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly Queue<string> _order = new Queue<string>();
        private CancellationTokenSource _lifetime = new CancellationTokenSource();
        private int _epoch;
        private long _cacheBytes;
        private const int MaxEntries = 32;
        private const long MaxCacheBytes = 8 * 1024 * 1024;

        internal NativeBannerPipeline(Func<string, int, bool, CancellationToken, Task<byte[]>> render)
        {
            _render = render ?? throw new ArgumentNullException(nameof(render));
        }

        internal void Reset()
        {
            CancellationTokenSource old;
            lock (_sync)
            {
                old = _lifetime;
                _lifetime = new CancellationTokenSource();
                _epoch++;
                _cache.Clear();
                _order.Clear();
                _cacheBytes = 0;
            }
            old.Cancel();
            old.Dispose();
        }

        internal async Task<string> GetAsync(string bannerCode, int size, bool cleanTempFiles, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(bannerCode) || bannerCode.Length > 32768) return null;
            size = Math.Max(128, Math.Min(1024, size));
            // Cleared per campaign; mod resources change only on process restart.
            string key = "native-tableau-png-v1|" + size + "|" + bannerCode;
            int epoch;
            CancellationTokenSource linked;
            lock (_sync)
            {
                if (_cache.TryGetValue(key, out string hit)) return hit;
                epoch = _epoch;
                linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _lifetime.Token);
            }
            using (linked)
            {
                linked.CancelAfter(20000);
                bool entered = false;
                try
                {
                    await _serial.WaitAsync(linked.Token).ConfigureAwait(false);
                    entered = true;
                    linked.Token.ThrowIfCancellationRequested();
                    lock (_sync)
                    {
                        if (epoch != _epoch) return null;
                        if (_cache.TryGetValue(key, out string hit)) return hit;
                    }
                    byte[] bytes = await _render(bannerCode, size, cleanTempFiles, linked.Token).ConfigureAwait(false);
                    linked.Token.ThrowIfCancellationRequested();
                    string result = NativeBannerImage.Encode(bytes, size);
                    if (result == null) return null;
                    lock (_sync)
                    {
                        linked.Token.ThrowIfCancellationRequested();
                        if (epoch != _epoch) return null;
                        long cost = result.Length * 2L;
                        if (cost <= MaxCacheBytes)
                        {
                            while (_order.Count > 0 && (_cache.Count >= MaxEntries || _cacheBytes + cost > MaxCacheBytes))
                            {
                                string oldest = _order.Dequeue();
                                _cacheBytes -= _cache[oldest].Length * 2L;
                                _cache.Remove(oldest);
                            }
                            _cache.Add(key, result);
                            _order.Enqueue(key);
                            _cacheBytes += cost;
                        }
                    }
                    return result;
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    return null; // Campaign change or local deadline: omit this reference.
                }
                finally { if (entered) _serial.Release(); }
            }
        }
    }
}
