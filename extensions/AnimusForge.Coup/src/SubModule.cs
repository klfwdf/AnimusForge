using System;
using System.Reflection;
using AnimusForge.CoupSystem;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AnimusForge.Coup;

public sealed class SubModule : MBSubModuleBase
{
    private const string HarmonyId = "AnimusForge.Coup";
    private static Harmony _harmony;

    protected override void OnSubModuleLoad()
    {
        base.OnSubModuleLoad();
        if (HostOwnsModule()) return;
        Start();
    }

    internal static void Start()
    {
        if (_harmony != null) return;
        _harmony = new Harmony(HarmonyId);
        try
        {
            CoupSystem.SettlementEntryTroopSelectionBehavior.Register(_harmony);
            CoupRebellionBridge.Initialize();
            CoupGuards.Register(_harmony);
            Logger.Log("Coup", "Integrated module loaded. mvid=" + typeof(SubModule).Module.ModuleVersionId
                + " mission=" + CoupGuards.MissionProtectionAvailable + " host=" + CoupSystem.SettlementEntryTroopSelectionBehavior.IsAvailable
                + " rebellion=" + CoupRebellionBridge.IsAvailable);
        }
        catch
        {
            Shutdown();
            throw;
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
        if (HostOwnsModule()) return;
        Tick(dt);
    }

    internal static void Tick(float dt)
    {
        CoupCampaignBehavior.Instance?.OnEngineTick(dt);
        CoupRebellionBridge.Instance?.OnEngineTick(dt);
    }

    protected override void OnSubModuleUnloaded()
    {
        if (!HostOwnsModule()) Shutdown();
        base.OnSubModuleUnloaded();
    }

    internal static void Shutdown()
    {
        if (_harmony == null) return;
        _harmony.UnpatchAll(HarmonyId);
        _harmony = null;
        CoupGuards.Reset();
        CoupSystem.SettlementEntryTroopSelectionBehavior.Reset();
    }

    private static bool HostOwnsModule()
    {
        try
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!string.Equals(assembly.GetName().Name, "AnimusForge", StringComparison.Ordinal)) continue;
                Type host = assembly.GetType("AnimusForge.IntegratedModuleHost");
                PropertyInfo property = host?.GetProperty("OwnsCoup", BindingFlags.Public | BindingFlags.Static);
                return property?.GetValue(null) is bool owned && owned;
            }
        }
        catch
        {
        }
        return false;
    }
}
