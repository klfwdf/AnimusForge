using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AnimusForge;

public partial class ShoutBehavior
{
    // Request-captured game identity stays in the adapter. The stage receives only detached
    // text and validation results; it cannot inspect the current Campaign or backend slot.
    private sealed class NativeConversationMainReplyHost : INativeConversationMainReplyHost
    {
        private readonly ShoutBehavior _owner;
        private readonly NativeConversationAdmission _admission;
        private readonly string _targetLog;
        private readonly string _pendingHistoryKey;
        private readonly long _pendingHistorySequence;

        internal NativeConversationMainReplyHost(ShoutBehavior owner, NativeConversationAdmission admission,
            string targetLog, string pendingHistoryKey, long pendingHistorySequence)
        {
            _owner = owner;
            _admission = admission;
            _targetLog = targetLog;
            _pendingHistoryKey = pendingHistoryKey;
            _pendingHistorySequence = pendingHistorySequence;
        }

        public Task<string> GenerateAsync(List<object> messages, Action<string> onStreamText)
            => CallNativeConversationApiAsync(messages, onStreamText);
        public bool IsGenerationStale()
            => SaveRuntimeGuard.IsStale(_admission.Generation, "native_conversation_reply");
        public string BuildStaleErrorText() => SaveRuntimeGuard.BuildStaleRequestErrorText();

        public async Task<NativeConversationReplyTargetValidation> ValidateTargetAsync()
        {
            string reason = "";
            bool isCurrent = await _owner.RunNativeConversationMainThreadFuncAsync(
                "main_reply_target_validation", _targetLog, _admission.AgentIndex,
                () => _owner.IsNativeConversationAdmissionCurrent(_admission, out reason),
                false).ConfigureAwait(false);
            return new NativeConversationReplyTargetValidation(isCurrent, reason);
        }

        public Task RollbackPendingPlayerHistoryAsync(string reason)
            => _owner.RollbackNativeConversationPendingPlayerHistoryAsync(_admission,
                _pendingHistoryKey, _pendingHistorySequence, reason);

        public string PreparePostprocessReply(string output)
        {
            string reply = StripNpcNamePrefixSafely((output ?? "").Replace("\r", "").Trim(), 30);
            return StripLeakedPromptContentForShout(reply);
        }

        public void ReportProviderFailure(string output)
            => LlmRetryPrompt.ShowFailurePopup("自由对话正文生成失败", output);
    }
}
