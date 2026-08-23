using System;

namespace AnimusForge.XihaiAction
{
    internal interface ISceneActionsAfBridge : IDisposable
    {
        string BridgeId { get; }

        bool TryInstall(out string reason);
    }

    internal static class SceneActionsAfBridgeHost
    {
        private static readonly object Sync = new object();
        private static ISceneActionsAfBridge _activeBridge;

        public static string ActiveBridgeId
        {
            get
            {
                lock (Sync)
                {
                    return _activeBridge?.BridgeId ?? string.Empty;
                }
            }
        }

        public static bool TryInstall(out string reason)
        {
            lock (Sync)
            {
                if (_activeBridge != null)
                {
                    reason = "already installed: " + _activeBridge.BridgeId;
                    return true;
                }

                ISceneActionsAfBridge bridge = new AfV130ReflectionSceneBridge();
                try
                {
                    if (!bridge.TryInstall(out reason))
                    {
                        bridge.Dispose();
                        return false;
                    }

                    _activeBridge = bridge;
                    reason = bridge.BridgeId + ": " + (reason ?? "installed");
                    return true;
                }
                catch
                {
                    bridge.Dispose();
                    throw;
                }
            }
        }

        public static void Uninstall()
        {
            ISceneActionsAfBridge bridge;
            lock (Sync)
            {
                bridge = _activeBridge;
                _activeBridge = null;
            }

            bridge?.Dispose();
        }
    }

    internal sealed class AfV130ReflectionSceneBridge : ISceneActionsAfBridge
    {
        private bool _installed;
        private bool _disposed;

        public string BridgeId => "animusforge.reflection.v130";

        public bool TryInstall(out string reason)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(AfV130ReflectionSceneBridge));
            }
            if (_installed)
            {
                reason = "already installed";
                return true;
            }

            bool installed = AfCompatV130.TryInstall(out reason);
            _installed = installed;
            return installed;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            if (_installed)
            {
                AfCompatV130.Uninstall();
                _installed = false;
            }
        }
    }
}
