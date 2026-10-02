using System;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using AnimusForge.Illustrator.Core;
using AnimusForge.Illustrator.UI.Gallery;
using AnimusForge.Illustrator.UI.Patches;
using AnimusForge.Illustrator.UI.Overlays;

namespace AnimusForge.Illustrator
{
    public sealed class SubModule : MBSubModuleBase
    {
        private const string HarmonyId = "AnimusForge.Illustrator";
        private static Harmony _harmony;
        public const string ModuleId = "AnimusForge_Illustrator";

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            if (HostOwnsModule()) return;
            Start();
        }

        internal static void Start() => TryStart();

        internal static bool TryStart()
        {
            try
            {
                IllustratorRuntime.Initialize();
                _harmony = new Harmony(HarmonyId);
                WeeklyReportPopupIllustrationPatch.Patch(_harmony);
                EncyclopediaHeroIllustrationPatch.EnsurePatched(_harmony);
                ConversationIllustrationPatch.EnsurePatched(_harmony);

                Debug.Print("[AnimusForge.Illustrator] SubModule and all illustration patches loaded successfully. implementationMvid=" +
                    typeof(SubModule).Module.ModuleVersionId + ", sceneCapture=isolated-panorama-30m+map-presented, characterReferences=full-body+head-detail-no-screen-draw");
                return true;
            }
            catch (Exception ex)
            {
                Debug.Print($"[AnimusForge.Illustrator] Failed to load SubModule: {ex.Message}");
                return false;
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
            try
            {
                IllustratorRuntime.Tick();
                Engine.ScreenCaptureHelper.ObservePanoramaFrame(dt);
            }
            catch
            {
            }
        }

        protected override void OnSubModuleUnloaded()
        {
            if (!HostOwnsModule()) Shutdown();
            base.OnSubModuleUnloaded();
        }

        internal static void Shutdown()
        {
            try
            {
                _harmony?.UnpatchAll(HarmonyId);
                _harmony = null;
                ConversationIllustrationPatch.Reset();
                EncyclopediaHeroIllustrationPatch.Reset();
                IllustrationCardPopup.ClearConversationSessionCache();
                WeeklyReportPopupIllustrationPatch.CloseOverlay();
                IllustratorRuntime.Shutdown();
            }
            catch
            {
            }
        }

        protected override void InitializeGameStarter(Game game, IGameStarter starterObject)
        {
            base.InitializeGameStarter(game, starterObject);
            if (HostOwnsModule()) return;
            RegisterCampaign(starterObject);
        }

        internal static void RegisterCampaign(IGameStarter starterObject)
        {
            if (starterObject is CampaignGameStarter campaignStarter)
            {
                campaignStarter.AddBehavior(new IllustratorCampaignBehavior());
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
                    PropertyInfo property = host?.GetProperty("OwnsIllustrator", BindingFlags.Public | BindingFlags.Static);
                    return property?.GetValue(null) is bool owned && owned;
                }
            }
            catch
            {
            }
            return false;
        }
    }

    public sealed class IllustratorCampaignBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.OnAgentJoinedConversationEvent.AddNonSerializedListener(this, ConversationIllustrationPatch.OnAgentJoinedConversation);
            CampaignEvents.ConversationEnded.AddNonSerializedListener(this, ConversationIllustrationPatch.OnConversationEnded);
        }

        public override void SyncData(IDataStore dataStore)
        {
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            try
            {
                IllustrationCardPopup.ClearConversationSessionCache();
                IllustratorRuntime.SetCampaign(Campaign.Current?.UniqueGameId ?? "unknown_campaign");
                starter.AddGameMenuOption(
                    "camp",
                    "af_illustrator_gallery",
                    "卡拉迪亚纪事画廊 (AI 画卷)",
                    args =>
                    {
                        args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                        return IllustratorRuntime.IsEnabled();
                    },
                    args =>
                    {
                        IllustratorGalleryPopup.Show();
                    },
                    false,
                    3
                );
            }
            catch (Exception ex)
            {
                Debug.Print($"[AnimusForge.Illustrator] Failed to register camp menu: {ex.Message}");
            }
        }
    }
}
