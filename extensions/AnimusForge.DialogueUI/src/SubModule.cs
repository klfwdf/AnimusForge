using System;
using HarmonyLib;
using TaleWorlds.MountAndBlade;
using AnimusForge.DialogueUI.Native;
using AnimusForge.DialogueUI.Shout;

namespace AnimusForge.DialogueUI
{
    public sealed class SubModule : MBSubModuleBase
    {
        private const string HarmonyId = "AnimusForge.DialogueUI";
        private Harmony _harmony;
        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            try
            {
                DialogueUiRuntime.Initialize();
                if (!DialogueUiRuntime.Enabled) return;
                _harmony = new Harmony(HarmonyId);
                ShoutUiAdapter.Install(_harmony);
                NativeUiAdapter.Install(_harmony);
                DialogueUiSprites.Install(_harmony);
                PresentationRouter.Install(_harmony);
                DialogueUiRuntime.Log("Presentation bridges installed; no dialogue submission API or campaign state added.");
            }
            catch (Exception ex)
            {
                DialogueUiRuntime.Disable();
                _harmony?.UnpatchAll(HarmonyId);
                DialogueUiRuntime.Log("UI integration unavailable; original interfaces retained: " + ex);
            }
        }

        protected override void OnApplicationTick(float dt)
        {
            base.OnApplicationTick(dt);
            if (!DialogueUiRuntime.Enabled) return;
            try { ShoutUiAdapter.Tick(); NativeUiAdapter.Tick(); }
            catch (Exception ex) { DialogueUiRuntime.LogOnce("tick-error", "Presentation update: " + ex.Message); }
        }

        protected override void OnSubModuleUnloaded()
        {
            DialogueUiRuntime.Disable();
            try { PresentationRouter.Shutdown(); }
            finally
            {
                _harmony?.UnpatchAll(HarmonyId);
                DialogueUiSprites.Shutdown();
                base.OnSubModuleUnloaded();
            }
        }
    }
}
