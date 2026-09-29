using System;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.MountAndBlade;
using AnimusForge.DialogueUI.Native;
using AnimusForge.DialogueUI.Scene;
using AnimusForge.DialogueUI.Shout;

namespace AnimusForge.DialogueUI
{
    public sealed class SubModule : MBSubModuleBase
    {
        private const string HarmonyId = "AnimusForge.DialogueUI";
        private static Harmony _harmony;

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            if (HostOwnsModule()) return;
            Start();
        }

        internal static void Start()
        {
            try { DialogueUiRuntime.Initialize(); }
            catch (Exception ex)
            {
                DialogueUiRuntime.Disable();
                DialogueUiRuntime.Log("UI integration unavailable; original interfaces retained: " + ex);
            }
        }

        // Patching must wait until native action types/sets are loaded (after Module.Initialize).
        // Harmony JITs a replacement for each patched original; CharacterTableau.AdjustCharacterForStanceIndex
        // reads ActionIndexCache statics, and ActionIndexCache is beforefieldinit, so patching during
        // OnSubModuleLoad resolved every ActionIndexCache static to -1 for the whole session
        // (character creation preview lost its idle action and fell back to the bind pose).
        // Called on every return to the initial screen; installs once.
        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            base.OnBeforeInitialModuleScreenSetAsRoot();
            if (HostOwnsModule()) return;
            InstallPresentation();
        }

        internal static void InstallPresentation()
        {
            if (_harmony != null || !DialogueUiRuntime.Enabled) return;
            try
            {
                _harmony = new Harmony(HarmonyId);
                ShoutUiAdapter.Install(_harmony);
                NativeUiAdapter.Install(_harmony);
                DialogueUiSprites.Install(_harmony);
                PresentationRouter.Install(_harmony);
                InstallSceneSession();
                DialogueUiRuntime.Log("Presentation bridges installed; no dialogue submission API or campaign state added.");
            }
            catch (Exception ex)
            {
                DialogueUiRuntime.Disable();
                _harmony?.UnpatchAll(HarmonyId);
                _harmony = null;
                DialogueUiRuntime.Log("UI integration unavailable; original interfaces retained: " + ex);
            }
        }

        // Wheel + persistent session are optional: on failure the host keeps its original T/Y flow.
        private static void InstallSceneSession()
        {
            try
            {
                SceneWheel.Install(_harmony);
                SceneSessionPanel.Install(_harmony);
                ShoutBehavior.ScenePresentationSessionHook = () => SceneSessionPanel.IsAvailable;
                ShoutBehavior.ScenePresentationBlocksHotkeysHook = () => SceneWheel.IsOpen;
            }
            catch (Exception ex)
            {
                ShoutBehavior.ScenePresentationSessionHook = null;
                ShoutBehavior.ScenePresentationBlocksHotkeysHook = null;
                DialogueUiRuntime.Log("Scene wheel/session unavailable; host T/Y flow retained: " + ex.Message);
            }
        }

        protected override void OnApplicationTick(float dt)
        {
            base.OnApplicationTick(dt);
            if (HostOwnsModule()) return;
            Tick(dt);
        }

        internal static void Tick(float dt)
        {
            if (!DialogueUiRuntime.Enabled) return;
            try { ShoutUiAdapter.Tick(); NativeUiAdapter.Tick(dt); SceneWheel.Tick(); SceneSessionPanel.Tick(dt); }
            catch (Exception ex) { DialogueUiRuntime.LogOnce("tick-error", "Presentation update: " + ex.Message); }
        }

        protected override void OnSubModuleUnloaded()
        {
            if (!HostOwnsModule()) Shutdown();
            base.OnSubModuleUnloaded();
        }

        internal static void Shutdown()
        {
            DialogueUiRuntime.Disable();
            ShoutBehavior.ScenePresentationSessionHook = null;
            ShoutBehavior.ScenePresentationBlocksHotkeysHook = null;
            try { SceneWheel.Shutdown(); SceneSessionPanel.Shutdown(); } catch { }
            try { PresentationRouter.Shutdown(); }
            finally
            {
                _harmony?.UnpatchAll(HarmonyId);
                _harmony = null;
                DialogueUiSprites.Shutdown();
            }
        }

        private static bool HostOwnsModule()
        {
            try
            {
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (!string.Equals(assembly.GetName().Name, "AnimusForge", StringComparison.Ordinal)) continue;
                    Type host = assembly.GetType("AnimusForge.IntegratedModuleHost");
                    PropertyInfo property = host?.GetProperty("OwnsDialogueUi", BindingFlags.Public | BindingFlags.Static);
                    return property?.GetValue(null) is bool owned && owned;
                }
            }
            catch
            {
            }
            return false;
        }
    }
}
