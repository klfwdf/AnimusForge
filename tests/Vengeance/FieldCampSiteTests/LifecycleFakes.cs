using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Library;
internal static class NativeCalls { internal static readonly List<string> Events=new(); }
namespace TaleWorlds.Core
{
    public interface IAgentOriginBase { }
    public sealed class Monster { }
    public static class FaceGen { public static Monster GetMonsterWithSuffix(int race,string suffix)=>new(); }
    public sealed class Game
    {
        public static Game Current=new();public EventManager EventManager=new();
    }
    public sealed class EventManager { public object Last;public void TriggerEvent(object value){Last=value;NativeCalls.Events.Add("event");} }
}
namespace TaleWorlds.CampaignSystem
{
    public sealed class CharacterObject { public int Race; }
    public sealed class AgentData
    {
        public readonly TaleWorlds.Core.IAgentOriginBase Origin; public AgentData(TaleWorlds.Core.IAgentOriginBase origin)=>Origin=origin;
        public AgentData Monster(TaleWorlds.Core.Monster monster)=>this;public AgentData NoHorses(bool noHorses)=>this;
    }
}
namespace TaleWorlds.CampaignSystem.Party { public sealed class PartyBase { } }
namespace TaleWorlds.CampaignSystem.AgentOrigins
{
    public sealed class PartyAgentOrigin : TaleWorlds.Core.IAgentOriginBase
    {
        public readonly bool Immortal;public PartyAgentOrigin(Party.PartyBase party,CharacterObject character,int rank,TaleWorlds.Core.UniqueTroopDescriptor descriptor,bool owns,bool immortal)=>Immortal=immortal;
    }
}
namespace TaleWorlds.Core { public struct UniqueTroopDescriptor { } }
namespace TaleWorlds.CampaignSystem.Settlements.Locations
{
    public sealed class LocationCharacter
    {
        public enum CharacterRelations{Friendly}
        public readonly AgentData AgentData;public Action<TaleWorlds.MountAndBlade.Agent> AddBehaviors,AfterAgentCreated;
        public LocationCharacter(AgentData data,Action<TaleWorlds.MountAndBlade.Agent> add,string tag,bool fixedLocation,CharacterRelations relation,string actionSet,bool civilian)
        {AgentData=data;AddBehaviors=agent=>{NativeCalls.Events.Add("behaviors");add(agent);};AfterAgentCreated=agent=>NativeCalls.Events.Add("after");}
    }
}
namespace SandBox
{
    public static class LocationCharacterMissionExtensions
    {public static TaleWorlds.MountAndBlade.AgentBuildData GetAgentBuildData(this LocationCharacter resident)=>new(resident.AgentData);}
    public sealed class CampaignAgentComponent
    {public void CreateAgentNavigator(LocationCharacter resident)=>NativeCalls.Events.Add("navigator");}
}
namespace SandBox.Missions.MissionLogics
{
    public class MissionAgentHandler
    {
        public TaleWorlds.MountAndBlade.Mission Mission;
        public virtual void EarlyStart()=>throw new Exception("settlement-only native startup invoked");
        public virtual void OnMissionTick(float dt)=>throw new Exception("settlement-only passage tick invoked");
        public virtual void OnMissionModeChange(TaleWorlds.MountAndBlade.MissionMode mode,bool start)=>throw new Exception("stale settlement face mask invoked");
    }
    public sealed class LocationCharacterAgentSpawnedMissionEvent
    {
        public readonly LocationCharacter Resident;public readonly TaleWorlds.MountAndBlade.Agent Agent;
        public LocationCharacterAgentSpawnedMissionEvent(LocationCharacter resident,TaleWorlds.MountAndBlade.Agent agent,TaleWorlds.Engine.WeakGameEntity entity){Resident=resident;Agent=agent;}
    }
}
namespace TaleWorlds.MountAndBlade
{
    public enum MissionMode { StartUp,Conversation,Battle } public enum AgentControllerType { AI }
    public sealed class Team { public uint Color=1,Color2=2; }
    public sealed class Agent { public T GetComponent<T>() where T:new()=>new(); }
    public sealed class AgentBuildData
    {
        public readonly TaleWorlds.CampaignSystem.AgentData Data;public Vec3 Position;public bool Civilian;
        public AgentBuildData(TaleWorlds.CampaignSystem.AgentData data)=>Data=data;
        public AgentBuildData Team(Team team)=>this;public AgentBuildData InitialPosition(in Vec3 position){Position=position;return this;}
        public AgentBuildData InitialDirection(in Vec2 dir)=>this;public AgentBuildData ClothingColor1(uint c)=>this;public AgentBuildData ClothingColor2(uint c)=>this;
        public AgentBuildData CivilianEquipment(bool b){Civilian=b;return this;}public AgentBuildData NoHorses(bool b)=>this;public AgentBuildData Controller(AgentControllerType c)=>this;
    }
}
namespace AnimusForge {internal static class FieldCampMissionLogic{internal static void ApplyCampActionSet(TaleWorlds.MountAndBlade.Agent agent)=>NativeCalls.Events.Add("action_set");}}
