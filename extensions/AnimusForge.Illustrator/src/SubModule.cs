using System;
using System.IO;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using AnimusForge.Illustrator.UI.Gallery;
using AnimusForge.Illustrator.UI.Patches;

namespace AnimusForge.Illustrator
{
    public sealed class SubModule : MBSubModuleBase
    {
        private static Harmony _harmony;
        public const string ModuleId = "AnimusForge_Illustrator";

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();

            try
            {
                _harmony = new Harmony("AnimusForge.Illustrator");
                WeeklyReportPopupIllustrationPatch.Patch(_harmony);
                EncyclopediaHeroIllustrationPatch.EnsurePatched(_harmony);
                ConversationIllustrationPatch.EnsurePatched(_harmony);

                Debug.Print("[AnimusForge.Illustrator] SubModule and all illustration patches loaded successfully.");
            }
            catch (Exception ex)
            {
                Debug.Print($"[AnimusForge.Illustrator] Failed to load SubModule: {ex.Message}");
            }
        }

        protected override void OnSubModuleUnloaded()
        {
            base.OnSubModuleUnloaded();
            try
            {
                _harmony?.UnpatchAll("AnimusForge.Illustrator");
                WeeklyReportPopupIllustrationPatch.CloseOverlay();
            }
            catch
            {
            }
        }

        protected override void InitializeGameStarter(Game game, IGameStarter starterObject)
        {
            base.InitializeGameStarter(game, starterObject);
            if (starterObject is CampaignGameStarter campaignStarter)
            {
                campaignStarter.AddBehavior(new IllustratorCampaignBehavior());
            }
        }
    }

    public sealed class IllustratorCampaignBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        public override void SyncData(IDataStore dataStore)
        {
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            try
            {
                // 在大地图营地菜单 (Camp Menu) 增加一个画廊浏览入口
                starter.AddGameMenuOption(
                    "camp",
                    "af_illustrator_gallery",
                    "卡拉迪亚纪事画廊 (AI 画卷)",
                    args =>
                    {
                        args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                        return IllustratorSettings.Instance?.EnableImageGeneration ?? true;
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
