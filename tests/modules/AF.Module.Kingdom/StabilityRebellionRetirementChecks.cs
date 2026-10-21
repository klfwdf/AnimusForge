using System.Reflection;
using System.Runtime.Serialization;

// Real candidate DLL, detached early-admission calls only. Faction lifecycle has its own source-linked replay.
internal static class StabilityRebellionRetirementChecks
{
    const BindingFlags All = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    internal static void Run(Assembly product)
    {
        int checks=0;
        void Check(bool ok,string name) { checks++; if(!ok) throw new Exception("Rebellion retirement: "+name); }
        Type policy=product.GetType("AnimusForge.KingdomStabilityPolicy",true);
        object Call(Type type,object target,string method,params object[] args)=>type.GetMethod(method,All).Invoke(target,args);
        foreach(int value in new[]{int.MinValue,-1,0,9,10,24,25,39,40,59,60,74,75,89,90,100,int.MaxValue})
            Check((float)Call(policy,null,"GetKingdomRebellionWeeklyChance",value)==0f,"no independent split at stability "+value);
        Check((int)Call(policy,null,"GetKingdomStabilityRelationTargetOffset",0)==-25 && (int)Call(policy,null,"GetKingdomStabilityRelationTargetOffset",100)==25,"stability relation effects retained");
        Check((int)Call(policy,null,"GetKingdomStabilityWeeklyBalancingDelta",0)==5 && (int)Call(policy,null,"GetKingdomStabilityWeeklyBalancingDelta",100)==-5,"weekly stability recovery retained");
        Check((int)Call(policy,null,"GetLowClanCountRoyalDomainLoyaltyAdjustment",0,1)==-6 && (int)Call(policy,null,"GetLowClanCountRoyalDomainLoyaltyAdjustment",100,1)==6,"royal-domain loyalty effects retained");
        Type runtime=product.GetType("AnimusForge.KingdomRebellionRuntimeController",true);
        // Any use of its missing pump/game ports would throw: retired entry points must stop before those effects.
        object owner=FormatterServices.GetUninitializedObject(runtime);
        foreach(bool execute in new[]{false,true}) foreach(bool force in new[]{false,true})
        {
            object result=Call(runtime,owner,"ResolveKingdomRebellion",null,10,execute,force);
            object Read(string name)=>result.GetType().GetField(name,All)?.GetValue(result) ?? result.GetType().GetProperty(name,All)?.GetValue(result,null);
            Check(Read("SelectedClan")==null && !(bool)Read("Executed") && !(bool)Read("PassedChanceGate") && !string.IsNullOrEmpty((string)Read("Message")),"retired resolve cannot select or execute even when forced");
        }
        object[] oldExecute={null,null,10,true,null,null};
        Check(!(bool)Call(runtime,owner,"TryExecuteKingdomRebellion",oldExecute) && !string.IsNullOrEmpty((string)oldExecute[5]),"old direct executor stops before naming");
        object[] physical={null,null,10,true,0,1,0,null,null,null,null};
        Check(!(bool)Call(runtime,owner,"TryExecuteKingdomRebellionWithNaming",physical) && !string.IsNullOrEmpty((string)physical[9]),"physical founding rejects missing faction authority before touching game state");
        foreach(string id in new[]{null,""," "}) Call(runtime,owner,"QueueAutomaticKingdomRebellion",null,id);
        Check(true,"old untracked queues rejected before pump access");
        Check((bool)Call(runtime,null,"IsStaleCivilWarRebellion",new object[]{null}),"null pending request retired");
        object pending=Activator.CreateInstance(product.GetType("AnimusForge.MyBehavior+PendingAutomaticKingdomRebellionContext",true),true);
        Check((bool)Call(runtime,null,"IsStaleCivilWarRebellion",pending),"old pending naming result without a faction cannot found a kingdom");
        Call(product.GetType("AnimusForge.MyBehavior",true),null,"QueueCivilWarSplitForExternal",null,null);
        Check(true,"historical external split signature remains a no-op");
        Console.WriteLine("PASS standalone rebellion retirement: "+checks+" actual DLL assertions; no Campaign, founding or model request.");
    }
}
