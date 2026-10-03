using System;
using System.IO;
using Microsoft.ML.OnnxRuntime;

namespace AnimusForge;

internal static class RerankerDeviceRuntime
{
    // Captured only when the singleton initializes. MCM changes require a game restart.
    internal static IRerankerEncodedBackend CreateBackend()
    {
        if (DuelSettings.Instance?.RerankerDeviceDropdown?.SelectedIndex != 1)
        {
            Logger.Log("OnnxReranker", "requested=CPU active=CPU");
            return null;
        }
        try
        {
            string root = AnimusForgeModulePaths.GetCurrentModuleRoot();
            string worker = Path.Combine(root, "OptionalRuntimes", "RerankerCuda", "AnimusForge.RerankerCuda.exe");
            return RerankerCudaProcess.Start(worker, root, message => Logger.Log("OnnxReranker", message));
        }
        catch (Exception ex)
        {
            Logger.Log("OnnxReranker", "requested=CUDA active=CPU fallback=worker_start_" + ex.GetBaseException().GetType().Name + " retry_gpu=false");
            return null;
        }
    }

    internal static InferenceSession CreateLocalSession(string modelPath, SessionOptions options)
        => new InferenceSession(modelPath, options);
}
