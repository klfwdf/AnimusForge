using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using HarmonyLib;

// Actual DLL TriggerShout and classifier; only game-world access and the GPU leaf are intercepted.
// No fields/DTO are copied from a historical host, and the producer's own callbacks stay intact.
internal static class SceneWheelProducerReplay
{
    private const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    private static readonly List<object> Values = new List<object>();
    private static object _menu;
    private static int _sessions;
    private static readonly HashSet<int> FactoryValues = new HashSet<int>();
    private static int _factories;
    public static object Value(int index) { if (FactoryValues.Contains(index)) _factories++; return Values[index]; }
    private static object Read(object value,string name) => value.GetType().GetProperty(name,All)?.GetValue(value) ?? value.GetType().GetField(name,All)?.GetValue(value);
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException("SceneWheel actual DLL: " + message); }
    private static bool Capture(object __0) { _menu = __0; return false; }
    private static bool OpenSession(ref bool __result) { _sessions++; __result = true; return false; }
    private static void Constant(Harmony harmony, MethodInfo method, object value, bool countFactory = false)
    {
        Check(method != null, "required world getter");
        int index = Values.Count; Values.Add(value);
        if(countFactory)FactoryValues.Add(index);
        var prefix = new DynamicMethod("WheelWorldLeaf" + index, typeof(bool), new[] { method.ReturnType.MakeByRefType() }, typeof(SceneWheelProducerReplay), true);
        prefix.DefineParameter(1, ParameterAttributes.None, "__result");
        ILGenerator il = prefix.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Ldc_I4, index);
        il.Emit(OpCodes.Call, typeof(SceneWheelProducerReplay).GetMethod(nameof(Value), All));
        il.Emit(method.ReturnType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, method.ReturnType);
        il.Emit(OpCodes.Stobj, method.ReturnType); il.Emit(OpCodes.Ldc_I4_0); il.Emit(OpCodes.Ret);
        harmony.Patch(method, prefix: new HarmonyMethod(prefix));
    }
    private static void PrepareTarget(Harmony harmony, MethodInfo method, object packet)
    {
        int index = Values.Count; Values.Add(packet);
        var argument = method.GetParameters().Single().ParameterType;
        var prefix = new DynamicMethod("WheelPrepareTarget", typeof(bool), new[] { typeof(bool).MakeByRefType(), argument }, typeof(SceneWheelProducerReplay), true);
        prefix.DefineParameter(1, ParameterAttributes.None, "__result"); prefix.DefineParameter(2, ParameterAttributes.None, "__0");
        ILGenerator il = prefix.GetILGenerator();
        il.Emit(OpCodes.Ldarg_1); il.Emit(OpCodes.Ldc_I4,index); il.Emit(OpCodes.Call,typeof(SceneWheelProducerReplay).GetMethod(nameof(Value),All));
        il.Emit(OpCodes.Castclass,argument.GetElementType()); il.Emit(OpCodes.Stobj,argument.GetElementType());
        il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Ldc_I4_1); il.Emit(OpCodes.Stind_I1); il.Emit(OpCodes.Ldc_I4_0); il.Emit(OpCodes.Ret);
        harmony.Patch(method,prefix:new HarmonyMethod(prefix));
    }
    internal static void Run(Assembly host,Harmony harmony)
    {
        Values.Clear(); FactoryValues.Clear(); _factories=0; _sessions=0; _menu=null;
        Type behavior=host.GetType("AnimusForge.ShoutBehavior",true);
        FieldInfo ownerField=behavior.GetField("_j17SceneShoutInputController",All); Type ownerType=ownerField.FieldType;
        object owner=FormatterServices.GetUninitializedObject(ownerType);
        object shout=FormatterServices.GetUninitializedObject(behavior); ownerField.SetValue(shout,owner);
        Type campaign=host.GetReferencedAssemblies().Where(x=>x.Name=="TaleWorlds.CampaignSystem").Select(Assembly.Load).Single().GetType("TaleWorlds.CampaignSystem.Campaign",true);
        Type mission=host.GetReferencedAssemblies().Where(x=>x.Name=="TaleWorlds.MountAndBlade").Select(Assembly.Load).Single().GetType("TaleWorlds.MountAndBlade.Mission",true);
        object campaignObject=FormatterServices.GetUninitializedObject(campaign),missionObject=FormatterServices.GetUninitializedObject(mission);
        Constant(harmony,campaign.GetProperty("Current",All).GetGetMethod(true),campaignObject);
        Constant(harmony,campaign.GetProperty("ConversationManager",All).GetGetMethod(true),null);
        MethodInfo getBehavior=campaign.GetMethods(All).Single(x=>x.Name=="GetCampaignBehavior"&&x.IsGenericMethodDefinition&&x.GetParameters().Length==0).MakeGenericMethod(behavior);
        Constant(harmony,getBehavior,shout);
        Constant(harmony,mission.GetProperty("Current",All).GetGetMethod(true),missionObject);
        Constant(harmony,mission.GetProperty("IsMissionEnding",All).GetGetMethod(true),false);
        Type mode=mission.GetProperty("Mode",All).PropertyType; Constant(harmony,mission.GetProperty("Mode",All).GetGetMethod(true),Enum.Parse(mode,"StartUp"));
        Constant(harmony,ownerType.GetProperty("_sceneConversationEpoch",All).GetGetMethod(true),0);
        Constant(harmony,host.GetType("AnimusForge.BattleSpeechRuntimeHost",true).GetMethod("CanOpenSpeechMenu",All),false);
        Constant(harmony,host.GetType("AnimusForge.MyBehavior",true).GetMethod("IsDevDataManagementEnabledForExternal",All),false);
        Type packetType=host.GetType("AnimusForge.NpcDataPacket",true); object packet=FormatterServices.GetUninitializedObject(packetType);
        FieldInfo name=packetType.GetField("Name",All); if(name!=null)name.SetValue(packet,"contract target");else packetType.GetProperty("Name",All).SetValue(packet,"contract target");
        PrepareTarget(harmony,ownerType.GetMethod("TryPrepareShoutTarget",All),packet);
        Type wheel=host.GetType("AnimusForge.DialogueUI.Scene.SceneWheel",true);
        wheel.GetMethod("Install",All).Invoke(null,new object[]{harmony});
        Type panel=host.GetType("AnimusForge.DialogueUI.Scene.SceneSessionPanel",true);
        Constant(harmony,panel.GetProperty("IsAvailable",All).GetGetMethod(true),false);
        Type inquiryType=ownerType.GetMethod("OwnsShoutModeInquiry",All).GetParameters()[0].ParameterType;
        Type info=inquiryType.Assembly.GetType("TaleWorlds.Core.MBInformationManager",true);
        harmony.Patch(info.GetMethod("ShowMultiSelectionInquiry",All),prefix:new HarmonyMethod(typeof(SceneWheelProducerReplay),nameof(Capture)));
        ownerType.GetMethod("TriggerShout",All).Invoke(owner,null);
        Check(_menu!=null,"actual producer emitted menu");
        MethodInfo classify=wheel.GetMethod("IsHostSceneMenu",All),show=wheel.GetMethod("ShowPrefix",All);
        Check((bool)classify.Invoke(null,new[]{_menu}),"migrated closure classifies with real current owner");
        Check((bool)show.Invoke(null,new object[]{_menu,true}),"disabled style leaves native route");
        FieldInfo savedMission=ownerType.GetField("_modeInquiryMission",All);
        savedMission.SetValue(owner,FormatterServices.GetUninitializedObject(mission));
        Check(!(bool)classify.Invoke(null,new[]{_menu}),"stored old mission rejected by actual lease");
        savedMission.SetValue(owner,missionObject);
        ownerField.SetValue(shout,FormatterServices.GetUninitializedObject(ownerType));
        Check(!(bool)classify.Invoke(null,new[]{_menu}),"stored old controller rejected by actual lease");
        ownerField.SetValue(shout,owner);
        object cloned=typeof(object).GetMethod("MemberwiseClone",All).Invoke(_menu,null);
        Check(!(bool)classify.Invoke(null,new[]{cloned}),"same menu/callbacks on another inquiry are rejected");
        MethodInfo availability=panel.GetProperty("IsAvailable",All).GetGetMethod(true);
        harmony.Unpatch(availability,HarmonyPatchType.Prefix,harmony.Id);
        Constant(harmony,availability,true);
        Type sprites=host.GetType("AnimusForge.DialogueUI.DialogueUiSprites",true);
        Constant(harmony,sprites.GetMethod("EnsureSceneLoaded",All),true);
        Type layer=host.GetType("AnimusForge.DialogueUI.Scene.WheelLayer",true);
        Constant(harmony,layer.GetMethod("TryOpen",All),FormatterServices.GetUninitializedObject(layer),true);
        Check(!(bool)show.Invoke(null,new object[]{_menu,true})&&_factories==1,"enabled style reaches actual ShowPrefix wheel factory exactly once");
        wheel.GetField("_open",All).SetValue(null,null); // Fake GPU leaf owns no native layer to close.
        MethodInfo session=ownerType.GetMethod("TryOpenPresentationSessionFromWheel",All);
        harmony.Patch(session,prefix:new HarmonyMethod(typeof(SceneWheelProducerReplay),nameof(OpenSession)));
        object elements=Read(_menu,"InquiryElements");
        object first=((IEnumerable)elements).Cast<object>().First();
        Type listType=typeof(List<>).MakeGenericType(first.GetType()); var selected=(IList)Activator.CreateInstance(listType);selected.Add(first);
        Delegate affirmative=(Delegate)Read(_menu,"AffirmativeAction");
        affirmative.DynamicInvoke(selected); affirmative.DynamicInvoke(selected);
        Check(_sessions==1,"real producer selection consumes exactly once into session callback");
        Check(!(bool)classify.Invoke(null,new[]{_menu}),"consumed menu no longer owned");
        Console.WriteLine("PASS actual DLL TriggerShout -> current classifier -> ShowPrefix/style + exact identity + once/session callback; GPU leaf NOT-RUN");
    }
}
