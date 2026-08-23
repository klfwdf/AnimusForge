using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace AnimusForge.SceneActions.Core
{
    public enum ConsentDecision
    {
        Accept,
        Refuse,
        Unclear
    }

    public sealed class ConsentClassifierRequest
    {
        public Guid RequestId { get; set; }
        public string FrozenIntentKey { get; set; }
        public string FrozenProgram { get; set; }
        public string ReplyText { get; set; }
    }

    public interface IAuxiliaryConsentClassifierV1
    {
        Task<string> ClassifyConsentAsync(
            ConsentClassifierRequest request,
            CancellationToken cancellationToken);
    }

    public sealed class FrozenConsentRequest
    {
        public FrozenConsentRequest(
            Guid requestId,
            string targetKey,
            string intentKey,
            long sessionGeneration,
            double requestedAtMissionTime,
            double expiresAtMissionTime)
            : this(
                requestId,
                targetKey,
                ActionProgramV2.FromSingle(intentKey),
                sessionGeneration,
                requestedAtMissionTime,
                expiresAtMissionTime)
        {
        }

        public FrozenConsentRequest(
            Guid requestId,
            string targetKey,
            ActionProgramV2 program,
            long sessionGeneration,
            double requestedAtMissionTime,
            double expiresAtMissionTime)
            : this(
                requestId,
                targetKey,
                program,
                program == null ? null : ActionProgramV3.FromV2(program),
                program == null ? null : ActionProgramV4.FromV2(program),
                sessionGeneration,
                requestedAtMissionTime,
                expiresAtMissionTime)
        {
        }

        public FrozenConsentRequest(
            Guid requestId,
            string targetKey,
            ActionProgramV3 program,
            long sessionGeneration,
            double requestedAtMissionTime,
            double expiresAtMissionTime)
            : this(
                requestId,
                targetKey,
                TryGetLegacyProgram(program),
                program,
                program == null ? null : ActionProgramV4.FromV3(program),
                sessionGeneration,
                requestedAtMissionTime,
                expiresAtMissionTime)
        {
        }

        public FrozenConsentRequest(
            Guid requestId,
            string targetKey,
            ActionProgramV4 program,
            long sessionGeneration,
            double requestedAtMissionTime,
            double expiresAtMissionTime)
            : this(
                requestId,
                targetKey,
                TryGetLegacyProgramV2(program),
                TryGetLegacyProgramV3(program),
                program,
                sessionGeneration,
                requestedAtMissionTime,
                expiresAtMissionTime)
        {
        }

        private FrozenConsentRequest(
            Guid requestId,
            string targetKey,
            ActionProgramV2 legacyProgram,
            ActionProgramV3 program,
            ActionProgramV4 programV4,
            long sessionGeneration,
            double requestedAtMissionTime,
            double expiresAtMissionTime)
        {
            if (requestId == Guid.Empty)
            {
                throw new ArgumentException("Request id must not be empty.", nameof(requestId));
            }
            if (string.IsNullOrWhiteSpace(targetKey))
            {
                throw new ArgumentException("Target key is required.", nameof(targetKey));
            }
            if (programV4 == null)
            {
                throw new ArgumentNullException(nameof(programV4));
            }
            if (sessionGeneration <= 0 ||
                double.IsNaN(requestedAtMissionTime) ||
                double.IsInfinity(requestedAtMissionTime) ||
                double.IsNaN(expiresAtMissionTime) ||
                double.IsInfinity(expiresAtMissionTime) ||
                expiresAtMissionTime <= requestedAtMissionTime)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(expiresAtMissionTime),
                    "Consent Mission-time bounds are invalid.");
            }

            RequestId = requestId;
            TargetKey = targetKey;
            Program = legacyProgram;
            ProgramV3 = program;
            ProgramV4 = programV4;
            IntentKey = programV4.SingleIntentKey ?? programV4.Steps[0].IntentKeys[0];
            SessionGeneration = sessionGeneration;
            RequestedAtMissionTime = requestedAtMissionTime;
            ExpiresAtMissionTime = expiresAtMissionTime;
        }

        public Guid RequestId { get; }
        public string TargetKey { get; }
        public string IntentKey { get; }
        public ActionProgramV2 Program { get; }
        public ActionProgramV3 ProgramV3 { get; }
        public ActionProgramV4 ProgramV4 { get; }
        public string ProgramExpression => ProgramV4.ProtocolExpression;
        public long SessionGeneration { get; }
        public double RequestedAtMissionTime { get; }
        public double ExpiresAtMissionTime { get; }

        private static ActionProgramV2 TryGetLegacyProgram(ActionProgramV3 program)
        {
            if (program == null)
            {
                return null;
            }
            program.TryToV2(out ActionProgramV2 legacy);
            return legacy;
        }

        private static ActionProgramV3 TryGetLegacyProgramV3(ActionProgramV4 program)
        {
            if (program == null)
            {
                return null;
            }
            program.TryToV3(out ActionProgramV3 legacy);
            return legacy;
        }

        private static ActionProgramV2 TryGetLegacyProgramV2(ActionProgramV4 program)
        {
            ActionProgramV3 legacyV3 = TryGetLegacyProgramV3(program);
            return TryGetLegacyProgram(legacyV3);
        }
    }

    public sealed class PendingConsentLedger
    {
        private readonly Dictionary<string, FrozenConsentRequest> _entries =
            new Dictionary<string, FrozenConsentRequest>(StringComparer.Ordinal);

        public int Count => _entries.Count;

        public FrozenConsentRequest Register(FrozenConsentRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            _entries.TryGetValue(request.TargetKey, out FrozenConsentRequest replaced);
            _entries[request.TargetKey] = request;
            return replaced;
        }

        public bool TryGet(
            string targetKey,
            long sessionGeneration,
            double missionTime,
            out FrozenConsentRequest request)
        {
            request = null;
            if (string.IsNullOrWhiteSpace(targetKey) ||
                !_entries.TryGetValue(targetKey, out FrozenConsentRequest candidate))
            {
                return false;
            }
            if (!IsCurrent(candidate, sessionGeneration, missionTime))
            {
                _entries.Remove(targetKey);
                return false;
            }
            request = candidate;
            return true;
        }

        public bool TryConsume(
            string targetKey,
            Guid expectedRequestId,
            long sessionGeneration,
            double missionTime,
            out FrozenConsentRequest request)
        {
            request = null;
            if (!TryGet(targetKey, sessionGeneration, missionTime, out FrozenConsentRequest current) ||
                current.RequestId != expectedRequestId)
            {
                return false;
            }
            _entries.Remove(targetKey);
            request = current;
            return true;
        }

        public bool TryRemove(string targetKey, out FrozenConsentRequest request)
        {
            request = null;
            if (string.IsNullOrWhiteSpace(targetKey) ||
                !_entries.TryGetValue(targetKey, out request))
            {
                return false;
            }
            _entries.Remove(targetKey);
            return true;
        }

        public IReadOnlyList<FrozenConsentRequest> RemoveExpired(
            long sessionGeneration,
            double missionTime)
        {
            List<FrozenConsentRequest> removed = _entries.Values
                .Where(value => !IsCurrent(value, sessionGeneration, missionTime))
                .ToList();
            foreach (FrozenConsentRequest value in removed)
            {
                _entries.Remove(value.TargetKey);
            }
            return removed;
        }

        public void Clear()
        {
            _entries.Clear();
        }

        private static bool IsCurrent(
            FrozenConsentRequest request,
            long sessionGeneration,
            double missionTime)
        {
            return request != null &&
                   request.SessionGeneration == sessionGeneration &&
                   !double.IsNaN(missionTime) &&
                   !double.IsInfinity(missionTime) &&
                   missionTime >= request.RequestedAtMissionTime &&
                   missionTime <= request.ExpiresAtMissionTime;
        }
    }

    public static class ConsentReplyInterpreter
    {
        private static readonly Regex StageDirectionPattern = new Regex(
            "(?<!\\*)\\*[^*]{1,512}\\*(?!\\*)",
            RegexOptions.CultureInvariant);

        private static readonly HashSet<string> AcceptReplies =
            new HashSet<string>(new[]
            {
                "好我答应",
                "好的我答应",
                "我答应",
                "我答应你",
                "答应你",
                "我同意",
                "同意",
                "我愿意",
                "愿意",
                "遵命",
                "是遵命",
                "如你所愿",
                "我会照做",
                "我照做",
                "照办",
                "没问题我照做",
                "yes",
                "iagree",
                "iaccept",
                "iwilldoit"
            }, StringComparer.Ordinal);

        private static readonly HashSet<string> RefuseReplies =
            new HashSet<string>(new[]
            {
                "绝不",
                "我拒绝",
                "拒绝",
                "我不答应",
                "不答应",
                "我不同意",
                "不同意",
                "我不愿意",
                "不愿意",
                "不可能",
                "休想",
                "做梦",
                "没门",
                "办不到",
                "恕难从命",
                "我不会照做",
                "我不照做",
                "no",
                "irefuse",
                "iwillnot"
            }, StringComparer.Ordinal);

        private static readonly HashSet<string> UnclearReplies =
            new HashSet<string>(new[]
            {
                "让我考虑",
                "让我考虑一下",
                "容我考虑",
                "容我想想",
                "让我想想",
                "我想一想",
                "我再想想",
                "稍后再说",
                "以后再说",
                "等一下",
                "等一等",
                "也许",
                "或许",
                "看情况",
                "不一定",
                "maybe",
                "letmethink",
                "iwillconsiderit"
            }, StringComparer.Ordinal);

        public static bool TryResolveLocal(
            string rawReply,
            out ConsentDecision decision)
        {
            decision = ConsentDecision.Unclear;
            string normalized = NormalizeReply(rawReply);
            if (normalized.Length == 0)
            {
                return false;
            }
            if (RefuseReplies.Contains(normalized))
            {
                decision = ConsentDecision.Refuse;
                return true;
            }
            if (UnclearReplies.Contains(normalized))
            {
                decision = ConsentDecision.Unclear;
                return true;
            }
            if (AcceptReplies.Contains(normalized))
            {
                decision = ConsentDecision.Accept;
                return true;
            }
            return false;
        }

        public static bool TryParseClassifierOutput(
            string output,
            out ConsentDecision decision)
        {
            decision = ConsentDecision.Unclear;
            if (string.IsNullOrEmpty(output) ||
                output.IndexOf('\r') >= 0 ||
                output.IndexOf('\n') >= 0)
            {
                return false;
            }

            string normalized = output.Trim();
            if (string.Equals(normalized, "ACCEPT", StringComparison.Ordinal))
            {
                decision = ConsentDecision.Accept;
                return true;
            }
            if (string.Equals(normalized, "REFUSE", StringComparison.Ordinal))
            {
                decision = ConsentDecision.Refuse;
                return true;
            }
            if (string.Equals(normalized, "UNCLEAR", StringComparison.Ordinal))
            {
                decision = ConsentDecision.Unclear;
                return true;
            }
            return false;
        }

        private static string NormalizeReply(string rawReply)
        {
            if (string.IsNullOrWhiteSpace(rawReply))
            {
                return string.Empty;
            }

            string withoutStageDirections = StageDirectionPattern.Replace(rawReply, " ")
                .Normalize(NormalizationForm.FormC);
            StringBuilder builder = new StringBuilder(withoutStageDirections.Length);
            foreach (char character in withoutStageDirections)
            {
                if (char.IsLetterOrDigit(character))
                {
                    builder.Append(character >= 'A' && character <= 'Z'
                        ? (char)(character + 32)
                        : character);
                }
            }
            return builder.ToString();
        }
    }
}
