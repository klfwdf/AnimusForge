using System;
using AnimusForge.CoupSystem;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AnimusForge.Coup;

public sealed class SubModule : MBSubModuleBase
{
    private Harmony _harmony;
    protected override void OnSubModuleLoad()
    {
        base.OnSubModuleLoad();
        _harmony = new Harmony("AnimusForge.Coup");
        CoupSystem.SettlementEntryTroopSelectionBehavior.Register(_harmony);
        CoupRebellionBridge.Initialize();
        CoupGuards.Register(_harmony);
        Logger.Log("Coup", "Independent module loaded. mvid=" + typeof(SubModule).Module.ModuleVersionId
            + " mission=" + CoupGuards.MissionProtectionAvailable + " host=" + CoupSystem.SettlementEntryTroopSelectionBehavior.IsAvailable
            + " rebellion=" + CoupRebellionBridge.IsAvailable);
    }

    protected override void InitializeGameStarter(Game game, IGameStarter starterObject)
    {
        base.InitializeGameStarter(game, starterObject);
        if (starterObject is CampaignGameStarter starter)
        {
            starter.AddBehavior(new CoupRebellionBridge());
            starter.AddBehavior(new CoupCampaignBehavior());
            starter.AddBehavior(new CoupCaptivityBehavior());
        }
    }

    protected override void OnApplicationTick(float dt)
    {
        base.OnApplicationTick(dt);
        CoupCampaignBehavior.Instance?.OnEngineTick(dt);
        CoupRebellionBridge.Instance?.OnEngineTick(dt);
    }

    protected override void OnSubModuleUnloaded()
    {
        _harmony?.UnpatchAll("AnimusForge.Coup");
        base.OnSubModuleUnloaded();
    }
}
