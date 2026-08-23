using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AnimusForge.SceneActions.Core
{
    public sealed class RequestGate
    {
        private readonly HashSet<Guid> _seen = new HashSet<Guid>();
        private readonly Dictionary<Guid, double> _pending =
            new Dictionary<Guid, double>();

        public bool TryAccept(
            Guid requestId,
            double submittedAtMissionTime,
            double currentMissionTime,
            SceneActionSettings settings,
            out ExecutionResultCode failure)
        {
            failure = ExecutionResultCode.Queued;
            Cleanup(currentMissionTime);
            if (_seen.Contains(requestId))
            {
                failure = ExecutionResultCode.DuplicateRequest;
                return false;
            }

            _seen.Add(requestId);

            if (double.IsNaN(submittedAtMissionTime) ||
                double.IsInfinity(submittedAtMissionTime) ||
                submittedAtMissionTime > currentMissionTime + 0.001d)
            {
                failure = ExecutionResultCode.MissionChanged;
                return false;
            }

            double ttlSeconds = Math.Max(0.001, settings.RequestTtlMs / 1000d);
            double expiresAt = submittedAtMissionTime + ttlSeconds;
            if (currentMissionTime > expiresAt)
            {
                failure = ExecutionResultCode.Expired;
                return false;
            }
            if (_pending.Count >= settings.MaxPendingRequests)
            {
                failure = ExecutionResultCode.QueueFull;
                return false;
            }

            _pending.Add(requestId, expiresAt);
            return true;
        }

        public void Complete(Guid requestId)
        {
            _pending.Remove(requestId);
        }

        public void Clear()
        {
            _seen.Clear();
            _pending.Clear();
        }

        private void Cleanup(double missionTime)
        {
            List<Guid> expired = null;
            foreach (KeyValuePair<Guid, double> entry in _pending)
            {
                if (entry.Value < missionTime)
                {
                    if (expired == null)
                    {
                        expired = new List<Guid>();
                    }
                    expired.Add(entry.Key);
                }
            }
            if (expired == null)
            {
                return;
            }
            foreach (Guid requestId in expired)
            {
                _pending.Remove(requestId);
            }
        }
    }

    public sealed class ClassifierRequest
    {
        public Guid RequestId { get; set; }
        public SceneInputSource InputSource { get; set; }
        public string Text { get; set; }
        public string PreviousPlayerText { get; set; }
        public string FullNpcReplyText { get; set; }
        public IReadOnlyList<string> AllowedIntentKeys { get; set; } =
            Array.Empty<string>();
        public IReadOnlyList<string> ImplicitEmotionIntentKeys { get; set; } =
            Array.Empty<string>();
    }

    public interface IAuxiliaryTextClassifierV1
    {
        Task<string> ClassifyAsync(
            ClassifierRequest request,
            CancellationToken cancellationToken);
    }
}
