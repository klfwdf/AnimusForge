using System;
using System.IO;
using RichExecutions.Core;
static class Program
{
    private static int checks;
    private static void Check(bool ok, string message) { if (!ok) throw new Exception(message); checks++; }
    static void Main()
    {
        Check(VengeanceIntegration.SceneHotkeysBlocked == null, "standalone has no AF input callback");
        Check(VengeanceIntegration.TryClaimEmbeddedHost(), "embedded host claim");
        VengeanceIntegration.SceneHotkeysBlocked = () => true;
        Check(VengeanceIntegration.SceneHotkeysBlocked.Invoke(), "active host blocks hotkeys");
        VengeanceIntegration.SceneHotkeysBlocked = () => false;
        Check(!VengeanceIntegration.SceneHotkeysBlocked.Invoke(), "inactive input permits hotkeys");
        VengeanceIntegration.ReleaseEmbeddedHost();
        Check(!VengeanceIntegration.IsEmbeddedHostActive && VengeanceIntegration.SceneHotkeysBlocked == null, "shutdown clears callback and lease");
        Check(VengeanceIntegration.TryClaimEmbeddedHost() && VengeanceIntegration.SceneHotkeysBlocked == null, "new host inherits no stale callback");
        VengeanceIntegration.ReleaseEmbeddedHost();
        string root = Directory.GetCurrentDirectory();
        string controller = File.ReadAllText(Path.Combine(root, "Vengeance/Source/Customization/ExecutionSiteBuilderController.Core.cs"));
        Check(!controller.Contains("AnimusForge."), "shared builder cannot depend directly on AF UI");
        Check(controller.Contains("!_panelOpen && VengeanceIntegration.SceneHotkeysBlocked?.Invoke() == true"), "own panel retains controls while host text input blocks scene shortcuts");
        string bridge = File.ReadAllText(Path.Combine(root, "src/bridges/Vengeance/Host/VengeanceRuntimeBridge.cs"));
        Check(bridge.Contains("VengeanceIntegration.SceneHotkeysBlocked = HotkeyInputGuard.IsTextInputFocused;"), "AF registers the actual input owner");
        Console.WriteLine($"PASS {checks} production integration/input ownership assertions; native UI and full standalone build NOT RUN");
    }
}
namespace TaleWorlds.CampaignSystem { public sealed class Hero { } }
namespace TaleWorlds.MountAndBlade { public sealed class Agent { public int Index; } public sealed class Mission { public static Mission? Current; } }
namespace RichExecutions.Scene { public sealed class TownExecutionMissionBehavior { } }
