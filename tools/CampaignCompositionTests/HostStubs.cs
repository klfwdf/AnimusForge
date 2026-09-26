// Engine/behavior test doubles only. Production registration methods are compiled unchanged.
using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.CampaignSystem;

internal static class HostProbe
{
    internal static readonly List<string> Events = new();
    internal static int ConstructorCalls;
    internal static int FailConstructor = -1;
    internal static string FailWrapper;
    internal static void Reset() { Events.Clear(); ConstructorCalls = 0; FailConstructor = -1; FailWrapper = null; }
    internal static void Construct(string name)
    {
        int index = ConstructorCalls++;
        Events.Add("construct:" + name);
        if (index == FailConstructor) throw new InvalidOperationException("test.constructor");
    }
    internal static GameModel Wrap(string name, GameModel inner)
    {
        if (FailWrapper == name) throw new InvalidOperationException("test.wrapper");
        return inner;
    }
}
internal abstract class StubSubModule
{
    protected virtual void InitializeGameStarter(Game game, IGameStarter starterObject) { }
    internal void Run(IGameStarter starterObject) => InitializeGameStarter(null, starterObject);
}
internal interface IWrapped { GameModel Inner { get; } }
namespace TaleWorlds.Core
{
    public interface IGameStarter { }
    public class Game { }
    public abstract class GameModel { }
    internal sealed class OtherStarter : IGameStarter { }
}
namespace TaleWorlds.CampaignSystem
{
    public abstract class CampaignBehaviorBase
    {
        protected CampaignBehaviorBase() { HostProbe.Construct(GetType().Name); }
    }
    public sealed class CampaignGameStarter : IGameStarter
    {
        internal readonly List<GameModel> ModelList = new();
        internal readonly List<CampaignBehaviorBase> Behaviors = new();
        internal int FailModel = -1, FailBehavior = -1, ModelAttempts, BehaviorAttempts;
        internal bool ThrowModelRead;
        public IEnumerable<GameModel> Models => ThrowModelRead ? throw new InvalidOperationException("test.models") : ModelList;
        public void AddModel<T>(T model) where T : GameModel
        {
            int index = ModelAttempts++;
            var wrapped = (IWrapped)model;
            HostProbe.Events.Add("model:" + model.GetType().Name + ":" + wrapped.Inner.GetType().Name);
            if (index == FailModel) throw new InvalidOperationException("test.add_model");
            ModelList.Add(model);
        }
        public void AddBehavior(CampaignBehaviorBase behavior)
        {
            int index = BehaviorAttempts++;
            HostProbe.Events.Add("behavior:" + behavior.GetType().Name);
            if (index == FailBehavior) throw new InvalidOperationException("test.add_behavior");
            Behaviors.Add(behavior);
        }
    }
}
namespace AnimusForge
{
    // The real SubModule Campaign callback is compiled by this harness. Lifetime behavior has
    // its own production-linked suite; these no-op hooks isolate composition without rewriting it.
    internal static class AfCampaignRuntimeLifecycle
    {
        internal static void Begin(Game game) { }
        internal static void CaptureOwners(Game game, CampaignGameStarter starter) { }
        internal static void End(Game game) { }
    }
    internal static class Logger
    {
        internal static void LogTrace(string category, string message)
        {
            // Keep original category/text; strip only exception stack (moved source coordinates differ).
            HostProbe.Events.Add("log:" + category + ":" + message.Split(new[] { ": System." }, StringSplitOptions.None)[0]);
        }
    }
}
namespace TaleWorlds.CampaignSystem.ComponentInterfaces { public abstract class MobilePartyFoodConsumptionModel : GameModel { } }
namespace TaleWorlds.CampaignSystem.GameComponents { public sealed class DefaultMobilePartyFoodConsumptionModel : ComponentInterfaces.MobilePartyFoodConsumptionModel { } }
internal sealed class CustomMobilePartyFoodConsumptionModel : TaleWorlds.CampaignSystem.ComponentInterfaces.MobilePartyFoodConsumptionModel { }
namespace AnimusForge { internal sealed class CourierFoodConsumptionModel : TaleWorlds.CampaignSystem.ComponentInterfaces.MobilePartyFoodConsumptionModel, IWrapped { public GameModel Inner { get; } internal CourierFoodConsumptionModel(TaleWorlds.CampaignSystem.ComponentInterfaces.MobilePartyFoodConsumptionModel inner) { Inner = HostProbe.Wrap(nameof(CourierFoodConsumptionModel), inner); } } }
namespace TaleWorlds.CampaignSystem.ComponentInterfaces { public abstract class MobilePartyAIModel : GameModel { } }
namespace TaleWorlds.CampaignSystem.GameComponents { public sealed class DefaultMobilePartyAIModel : ComponentInterfaces.MobilePartyAIModel { } }
internal sealed class CustomMobilePartyAIModel : TaleWorlds.CampaignSystem.ComponentInterfaces.MobilePartyAIModel { }
namespace AnimusForge { internal sealed class CourierMobilePartyAIModel : TaleWorlds.CampaignSystem.ComponentInterfaces.MobilePartyAIModel, IWrapped { public GameModel Inner { get; } internal CourierMobilePartyAIModel(TaleWorlds.CampaignSystem.ComponentInterfaces.MobilePartyAIModel inner) { Inner = HostProbe.Wrap(nameof(CourierMobilePartyAIModel), inner); } } }
namespace TaleWorlds.CampaignSystem.ComponentInterfaces { public abstract class SettlementAccessModel : GameModel { } }
namespace TaleWorlds.CampaignSystem.GameComponents { public sealed class DefaultSettlementAccessModel : ComponentInterfaces.SettlementAccessModel { } }
internal sealed class CustomSettlementAccessModel : TaleWorlds.CampaignSystem.ComponentInterfaces.SettlementAccessModel { }
namespace AnimusForge { internal sealed class AnimusForgeSettlementAccessModel : TaleWorlds.CampaignSystem.ComponentInterfaces.SettlementAccessModel, IWrapped { public GameModel Inner { get; } internal AnimusForgeSettlementAccessModel(TaleWorlds.CampaignSystem.ComponentInterfaces.SettlementAccessModel inner) { Inner = HostProbe.Wrap(nameof(AnimusForgeSettlementAccessModel), inner); } } }
namespace TaleWorlds.CampaignSystem.ComponentInterfaces { public abstract class SettlementLoyaltyModel : GameModel { } }
namespace TaleWorlds.CampaignSystem.GameComponents { public sealed class DefaultSettlementLoyaltyModel : ComponentInterfaces.SettlementLoyaltyModel { } }
internal sealed class CustomSettlementLoyaltyModel : TaleWorlds.CampaignSystem.ComponentInterfaces.SettlementLoyaltyModel { }
namespace AnimusForge.PolicyEffects { internal sealed class AnimusForgeSettlementLoyaltyModel : TaleWorlds.CampaignSystem.ComponentInterfaces.SettlementLoyaltyModel, IWrapped { public GameModel Inner { get; } internal AnimusForgeSettlementLoyaltyModel(TaleWorlds.CampaignSystem.ComponentInterfaces.SettlementLoyaltyModel inner) { Inner = HostProbe.Wrap(nameof(AnimusForgeSettlementLoyaltyModel), inner); } } }

// This suite exercises AfApi.GetSnapshot only; Native submission is covered by its own source-linked tests.
namespace AnimusForge.Api.V1
{
    public sealed class AfDialogueClient
    {
        internal AfDialogueClient(object _) => throw new InvalidOperationException("composition.native_api_not_executed");
    }
}
namespace AnimusForge.Refactor.Modules
{
    internal static class CoreDialogueServices
    {
        internal static object CreateClient() => throw new InvalidOperationException("composition.native_api_not_executed");
    }
}
