using Microsoft.ML.OnnxRuntime;

namespace AnimusForge;

internal static class RerankerDeviceRuntime
{
    // Retained for existing internal callers; there is no device setting or CUDA startup path.
    internal static InferenceSession CreateLocalSession(string modelPath, SessionOptions options)
        => new InferenceSession(modelPath, options);
}
