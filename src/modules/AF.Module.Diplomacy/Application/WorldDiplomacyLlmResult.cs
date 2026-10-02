namespace AnimusForge;

// Detached completion; never contains canonical records or live game objects.
internal sealed class LlmJobResult
    {
        public string JobId = "";
        public long RuntimeGeneration;
        public bool Success;
        public string Content = "";
        public string Error = "";
        public bool IsServiceFailure;
        public bool IsOutputTruncated;
        public int? PromptTokens;
        public int? CompletionTokens;
        public int? PromptCacheHitTokens;
        public int? PromptCacheMissTokens;
        public int? PromptCacheCreationTokens;
        public int? PromptUncachedTokens;
    }
