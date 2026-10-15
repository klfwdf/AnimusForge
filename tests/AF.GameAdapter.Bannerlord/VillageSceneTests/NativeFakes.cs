// Only native/neighboring boundaries are fake. The scene lifecycle under test is production source.
using TaleWorlds.MountAndBlade;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
namespace TaleWorlds.Library { public struct Vec3 { public float x; public Vec2 AsVec2 => new(); } }
namespace TaleWorlds.Core { public interface IAgentOriginBase { } public class Banner { } public static class FaceGen { public static object GetMonsterWithSuffix(int race,string suffix) => suffix; } }
namespace TaleWorlds.Localization {
    public class TextObject(string text) {
        public void SetTextVariable(string key, int value) => text = text.Replace("{"+key+"}", value.ToString());
        public override string ToString() => text;
    }
}
namespace TaleWorlds.CampaignSystem {
    public class CharacterObject {
        public int Race; public string StringId = "adult"; public bool IsSoldier, IsChildTemplate; public float Age = 30;
        public Hero HeroObject; public float MaxHitPoints() => 100;
    }
    public class Hero { public bool IsAlive = true, IsNotable, IsPrisoner; }
}
namespace TaleWorlds.CampaignSystem.Party { public class PartyBase { public static PartyBase MainParty = new(); } }
namespace TaleWorlds.CampaignSystem.Settlements {
    public class Settlement {
        public string StringId; public List<Hero> Notables = new();
        public static Dictionary<string,Settlement> All = new();
        public static Settlement Find(string id) => id != null && All.TryGetValue(id,out var s) ? s : null;
    }
}
namespace SandBox.Missions.AgentBehaviors { public class DailyBehaviorGroup { public void DisableScriptedBehavior() { } public void DisableAllBehaviors() { } } }
namespace SandBox {
    public class AgentNavigator { public void ClearTarget() { } public T GetBehaviorGroup<T>() where T:new() => new(); }
    public class CampaignAgentComponent { public AgentNavigator AgentNavigator = new(); }
}
namespace TaleWorlds.MountAndBlade {
    public enum MissionBehaviorType { Other }
    public enum MissionMode { StartUp, Conversation, Barter }
    public class MissionLogic {
        public Mission Mission; public virtual MissionBehaviorType BehaviorType => MissionBehaviorType.Other;
        public virtual void AfterStart() { } public virtual void OnAgentBuild(Agent a, TaleWorlds.Core.Banner b) { }
        public virtual void OnMissionTick(float dt) { } protected virtual void OnEndMission() { }
        public void EndForTest() => OnEndMission();
    }
    public class Mission {
        public static Mission Current; public List<Agent> Agents = new(); public Agent MainAgent;
        public MissionMode Mode; public bool IsMissionEnding; public float CurrentTime;
        public List<MissionLogic> Behaviors = new();
        public T GetMissionBehavior<T>() where T:class => Behaviors.OfType<T>().FirstOrDefault();
        public void Add(MissionLogic b) { b.Mission = this; Behaviors.Add(b); }
        public void EndMission() { IsMissionEnding = true; foreach(var b in Behaviors) b.EndForTest(); }
    }
    public class Origin { public object BattleCombatant; }
    public enum AgentControllerType { AI }
    public class Formation { }
    public class Team { }
    public enum FormationClass { Infantry }
    public class Agent {
        public enum MortalityState { Invulnerable, Mortal }
        public Mission Mission; public CharacterObject Character = new(); public bool IsHuman = true, IsMainAgent, Active = true, Faded;
        public int Index; public float Health = 100, HealthLimit = 100, BaseHealthLimit = 100;
        public Origin Origin = new(); public TaleWorlds.Library.Vec3 Position; public MortalityState Mortality;
        public bool IsActive() => Active;
        public void FadeOut(bool hideInstantly,bool hideMount) { Faded = true; Active = false; }
        public void SetMortalityState(MortalityState s) => Mortality = s;
        public T GetComponent<T>() where T:new() => new();
        public void SetIsAIPaused(bool p) { } public void InvalidateTargetAgent() { }
        public void DisableScriptedMovement() { } public void ClearTargetFrame() { }
    }
    public class InformationMessage(string text) { public string Text = text; }
    public static class InformationManager { public static List<string> Messages = new(); public static void DisplayMessage(InformationMessage m) => Messages.Add(m.Text); }
}
namespace AnimusForge.SiegeAftermathIntervention {
    public static class SiegeLocalAttackProfile { public static bool ShouldNormalizeCivilianHealth(float health,float normal) => health > normal*2; }
}
namespace AnimusForge {
    public static class SceneTauntBehavior { public static bool IsChildSceneProtectedTarget(CharacterObject c) => c.IsChildTemplate || c.Age > 0 && c.Age < 18; }
    public static class ShoutBehavior { public static void CancelAgentSpeechForRemovalExternal(int i,string s) { } }
    public static class GcczDiagnosticLog { public static List<string> Lines = new(); public static void Log(string s,string m) => Lines.Add(m); }
    public static class VillageAftermathBehavior {
        public static Mission Active; public static bool ActivationAllowed = true, GatheringAllowed = true; public static int Ends, Ticks;
        public static bool TryActivateForSetsVillage(TaleWorlds.CampaignSystem.Settlements.Settlement v,Mission m,string source,bool hostile) {
            if (!ActivationAllowed || !VillageDispositionSceneMissionBehavior.Admits(m,v?.StringId)) return false;
            Active = m; return true;
        }
        public static bool IsActiveForMission(Mission m) => ReferenceEquals(Active,m) && VillageDispositionSceneMissionBehavior.Controls(m);
        public static void TickForSetsMission(Mission m) => Ticks++;
        public static bool CanGatherInDispositionScene() => GatheringAllowed;
        public static void EndForSetsMission(Mission m,string s) { if(Active == m) { Active = null; Ends++; } }
    }
    public partial class SiegeAiInterventionBehavior {
        public static List<int> Moved = new();
        public static bool TownActive; public static int Restored, Unfollowed, Followed;
        internal static readonly HashSet<int> AlliedAgentIndexes = new(), BannerBearerAgentIndexes = new(), MassacreReadySoldierAgentIndexes = new(), MassacreCombatPreparedAgentIndexes = new(), CommandableOriginRuntimeIds = new();
        private static bool IsActiveInCurrentMission() => TownActive;
        private static void MarkAgentOriginUnderPlayerCommand(Agent a) => CommandableOriginRuntimeIds.Add(System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(a.Origin));
        private static void RestoreAlliedSoldierFriendlyState(Agent a,float hp,string source,bool forceFollow) => Restored++;
        private static void DisableCompanionStyleFollow(Agent a) => Unfollowed++;
        private static bool TrySetPlayerFormationFollowOrder(FormationClass f,string source) { Followed++;return true; }
        public static bool TestEscortGate(Mission m) => IsInterventionEscortScene(m);
        public static bool TryMoveForeignVillageAgent(Agent a,TaleWorlds.Library.Vec3 target,string source) { Moved.Add(a.Index); return true; }
    }
}

namespace TaleWorlds.CampaignSystem.Roster { public class TroopRoster { } }
namespace TaleWorlds.Library { public struct Vec2 { public Vec2 Normalized() => this; } }
namespace TaleWorlds.MountAndBlade {
 public class AgentBuildData(CharacterObject troop) {
  public CharacterObject Troop = troop; public bool Civilian, HorsesDisabled, OwnFormation, OwnClass;public int Count, Index;public Formation AssignedFormation;public IAgentOriginBase Origin;
  public AgentBuildData TroopOrigin(IAgentOriginBase o) { Origin=o;return this; }
  public AgentBuildData Monster(object o)=>this;public AgentBuildData Team(Team t)=>this;
  public AgentBuildData InitialPosition(in TaleWorlds.Library.Vec3 p)=>this;public AgentBuildData InitialDirection(TaleWorlds.Library.Vec2 d)=>this;
  public AgentBuildData Controller(AgentControllerType c)=>this;
  public AgentBuildData CivilianEquipment(bool b) { Civilian=b;return this; }
  public AgentBuildData NoHorses(bool b) { HorsesDisabled=b;return this; }
  public AgentBuildData Formation(Formation f) { AssignedFormation=f;return this; }
  public AgentBuildData FormationTroopSpawnCount(int n) { Count=n;return this; }
  public AgentBuildData FormationTroopSpawnIndex(int n) { Index=n;return this; }
  public AgentBuildData SpawnsIntoOwnFormation(bool b) { OwnFormation=b;return this; }
  public AgentBuildData SpawnsUsingOwnTroopClass(bool b) { OwnClass=b;return this; }
 }
}
