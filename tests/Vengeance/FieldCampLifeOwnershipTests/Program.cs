using AnimusForge;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;
using TaleWorlds.Engine;
using TaleWorlds.ObjectSystem;
int checks=0;
void Check(bool b,string name){if(!b)throw new Exception(name);checks++;}
FieldCampMissionLogic Setup(out Agent actor,out FieldCampMissionLogic.CampAgent entry)
{
    Mission.Current=new Mission();Agent.Main=new Agent{Index=0,Position=new Vec3(0,9)};
    actor=new Agent{Index=4,Position=new Vec3(0,0)};
    var camp=new FieldCampMissionLogic();entry=camp.Add(actor);
    MBObjectManager.Instance.Available=true;FieldCampProps.ArcheryTargets.Clear();
    FieldCampProps.ArcheryTargets.Add(new GameEntity{GlobalPosition=new Vec3(8,0)});
    return camp;
}
var camp=Setup(out var actor,out var e);
Check(camp.CanFollowPlayer(actor),"healthy registered camp resident eligible");
Check(!camp.CanFollowPlayer(Agent.Main)&&!camp.CanFollowPlayer(new Agent()),"player and unregistered agents rejected");
e.Activity=FieldCampMissionLogic.Activity.Wounded;Check(!camp.CanFollowPlayer(actor),"wounded resident not removed from infirmary");e.Activity=FieldCampMissionLogic.Activity.Work;
foreach(string state in new[]{"Duel","Ceremony","Envoy","SlaughterBusy","_assembled"})
{
    var field=typeof(FieldCampMissionLogic).GetField(state,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
    field.SetValue(camp,true);Check(!camp.CanFollowPlayer(actor),state+" owner excludes follow");field.SetValue(camp,false);
}
actor.Speed=0;e.Training=true;e.MoveBlocked=e.RouteQueued=e.RoutePrepared=true;e.Returning=true;
Check(camp.BeginFollowPlayer(actor)&&e.FollowingPlayer&&!e.Seated&&!actor.Paused,"follow takes active control");
Check(actor.Speed==-1,"follow releases the stationary training speed limit");
Check(!e.Training&&!e.Returning&&!e.MoveBlocked&&!e.RouteQueued&&!e.RoutePrepared&&camp.Releases==1,"follow releases training and stale daily move state");
Check(camp.BeginFollowPlayer(actor)&&camp.Releases==1,"duplicate follow is idempotent");
Check(camp.EndFollowPlayer(actor)&&!e.FollowingPlayer&&camp.Restores==1,"stop restores home owner once");
Check(!camp.EndFollowPlayer(actor)&&camp.Restores==1,"duplicate stop is harmless");
camp.BeginFollowPlayer(actor);camp.Conversation=true;camp.EndFollowPlayer(actor);Check(camp.Restores==1,"conversation retains owner until normal release");
camp=Setup(out actor,out e);camp.NavigationAvailable=false;Check(!camp.BeginFollowPlayer(actor)&&!e.FollowingPlayer,"failed navigator cannot strand daily owner");
camp=Setup(out actor,out e);Mission.Current=new Mission();Check(!camp.BeginFollowPlayer(actor),"stale mission follow rejected");
camp=Setup(out actor,out e);Check(camp.StartTraining(e)&&e.Training&&e.Seated&&!actor.Paused&&actor.Speed==0,"shooting post runs native AI stationary");
Check(actor.Target==FieldCampProps.ArcheryTargets[0]&&actor.Shots==1,"shooting targets actual placed entity");
Check(actor.Events.IndexOf("release")<actor.Events.IndexOf("equip:training_bow")&&actor.Events.IndexOf("wield")<actor.Events.IndexOf("target"),"release pose before equipping/wielding practice bow");
Check(actor.Events.Contains("equip:blunt_arrows"),"training uses blunt mission-only ammo");
camp._elapsed=e.NextTrainingTime-1;camp.TickTraining(e);Check(camp.Moves==0&&actor.Shots==1,"volley does not restart every tick");
camp._elapsed=e.NextTrainingTime;camp.TickTraining(e);Check(!e.Training&&e.Returning&&camp.Moves==1,"volley completion enters visit-return owner");
Check(!e.PracticeEquipmentHeld && actor.Equipment[TaleWorlds.Core.EquipmentIndex.Weapon2].Item.Id=="original_sword" && actor.Equipment[TaleWorlds.Core.EquipmentIndex.Weapon3].Item.Id=="original_shield","leaving practice restores original mission weapons");
camp=Setup(out actor,out e);Mission.Current.Nearby.Add(new Agent{Index=2,Position=new Vec3(4,.2f)});Check(!camp.StartTraining(e)&&actor.Shots==0,"occupied shooting lane refuses shooting");
Mission.Current.Nearby.Clear();Check(camp.StartTraining(e),"cleared lane can start");Mission.Current.Nearby.Add(new Agent{Index=2,Position=new Vec3(3,0)});camp.TickTraining(e);Check(!e.Training&&actor.Paused&&camp.Poses==1,"crossing person interrupts active volley at upkeep boundary");
camp=Setup(out actor,out e);MBObjectManager.Instance.Available=false;Check(!camp.StartTraining(e)&&!e.Training,"missing practice items do not claim shooting success");
camp=Setup(out actor,out e);FieldCampProps.ArcheryTargets.Clear();Check(!camp.StartTraining(e)&&actor.Shots==0,"missing target does not shoot into empty world");
camp=Setup(out actor,out e);e.FollowingPlayer=true;Check(!camp.StartTraining(e),"training cannot steal follower owner");
camp=Setup(out actor,out e);camp.StrollAvailable=false;camp.StartTraining(e);camp._elapsed=e.NextTrainingTime;camp.TickTraining(e);Check(e.Training&&!e.Returning&&actor.Shots==2,"busy stroll budget restarts bounded practice volley rather than abandoning station");
camp=Setup(out actor,out e);camp.StartTraining(e);camp.BeginFollowPlayer(actor);Check(!e.Training&&e.FollowingPlayer,"manual follow interrupts shooting and owns resident");
Console.WriteLine($"PASS {checks} linked production camp follow/training assertions; native engine/rendering are doubles, live game NOT RUN");
