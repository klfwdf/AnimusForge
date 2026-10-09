using System;
using System.Collections.Generic;
using System.Linq;
namespace TaleWorlds.Core {}
namespace TaleWorlds.Library {
 [Flags] public enum InputUsageMask {Invalid=0,MouseButtons=1,MouseWheels=2,All=7}
 public sealed class DataSourcePropertyAttribute:Attribute{}
 public class ViewModel {public void OnPropertyChangedWithValue<T>(T value,string property){}public virtual void OnFinalize(){}}
 public sealed class TextInquiryData {public readonly Action<string> Save;public readonly Action Cancel;public TextInquiryData(string title,string desc,bool isAffirmativeOptionShown,bool isNegativeOptionShown,string affirmativeText,string negativeText,Action<string> affirmativeAction,Action negativeAction,bool shouldInputBeObfuscated=false,Func<string,Tuple<bool,string>> isInputValid=null,string soundEventPath="",string defaultInputText=""){Save=affirmativeAction;Cancel=negativeAction;}}
 public static class InformationManager {public static TextInquiryData Inquiry;public static void ShowTextInquiry(TextInquiryData inquiry){Inquiry=inquiry;}}
}
namespace TaleWorlds.InputSystem {public sealed class InputContext {public void RegisterHotKeyCategory(object category){}}public static class HotKeyManager {public static object GetCategory(string id)=>null;}}
namespace TaleWorlds.ScreenSystem {
 using TaleWorlds.Library;using TaleWorlds.InputSystem;
 public class InputRestrictions {public int Order;public bool MouseVisibility;public InputUsageMask InputUsageMask;public InputRestrictions(int order){Order=order;}public void SetInputRestrictions(bool visible=true,InputUsageMask mask=InputUsageMask.All){MouseVisibility=visible;InputUsageMask=mask;}public void ResetInputRestrictions(){MouseVisibility=false;InputUsageMask=InputUsageMask.Invalid;}}
 public class ScreenLayer {public static event Action<ScreenLayer> OnLayerActiveStateChanged;public string Name;public bool IsFinalized,IsActive,IsFocusLayer,LastActiveState=true;public bool Hit=true;public InputRestrictions InputRestrictions;public InputContext Input=new();public InputUsageMask InputUsageMask=>InputRestrictions.InputUsageMask;public ScreenLayer(string name,int order){Name=name;InputRestrictions=new(order);}public void SetActive(bool active){IsActive=active;OnLayerActiveStateChanged?.Invoke(this);}}
 public class ScreenBase {public bool IsFinalized;public List<ScreenLayer> Layers=new();public bool HasLayer(ScreenLayer l)=>Layers.Contains(l);public void AddLayer(ScreenLayer l){Layers.Add(l);l.SetActive(true);}public void RemoveLayer(ScreenLayer l){Layers.Remove(l);l.SetActive(false);l.IsFinalized=true;}}
 public static class ScreenManager {
  public static ScreenBase TopScreen;public static ScreenLayer FocusedLayer,FirstHitLayer;public static event Action<ScreenBase> OnPushScreen,OnPopScreen;public static int PushSubscribers=>OnPushScreen?.GetInvocationList().Length??0;
  public static void SetSuspendLayer(ScreenLayer l,bool isSuspended){l.SetActive(!isSuspended);l.LastActiveState=!isSuspended;}
  public static void TrySetFocus(ScreenLayer l){if((FocusedLayer==null||FocusedLayer.InputRestrictions.Order<=l.InputRestrictions.Order||!l.IsActive)&&l.IsFocusLayer)FocusedLayer=l;}
  public static void TryLoseFocus(ScreenLayer l){if(ReferenceEquals(FocusedLayer,l))FocusedLayer=TopScreen?.Layers.Where(x=>x!=l&&x.IsActive&&x.IsFocusLayer).OrderByDescending(x=>x.InputRestrictions.Order).FirstOrDefault();}
  public static (ScreenLayer Mouse,ScreenLayer Keyboard) Dispatch(){var mouse=TopScreen.Layers.Where(x=>x.IsActive&&!x.IsFinalized&&x.Hit&&(x.InputUsageMask&InputUsageMask.MouseButtons)!=0).OrderByDescending(x=>x.InputRestrictions.Order).FirstOrDefault();return(mouse,FocusedLayer?.IsActive==true?FocusedLayer:null);}
  public static void Push(ScreenBase screen){foreach(var l in TopScreen.Layers.ToArray()){if(l.IsActive){l.LastActiveState=true;l.SetActive(false);}}TopScreen=screen;OnPushScreen?.Invoke(screen);}
 }
}
namespace TaleWorlds.Engine.GauntletUI {
 using TaleWorlds.ScreenSystem;
 public sealed class GauntletLayer:ScreenLayer {public object MovieViewModel;public static bool FailLoad;public GauntletLayer(string name,int order,bool isFocus):base(name,order){IsFocusLayer=isFocus;}public void LoadMovie(string name,object vm){if(FailLoad)throw new InvalidOperationException("load failure");MovieViewModel=vm;}}
}
namespace AnimusForge {
 public static class AnimusForgeTextInputSanitizer {public const int MaxLongEditorChars=60000;public static string SanitizeMultiline(string text,int max)=>text??"";}
 public static class Logger {public static void Log(string owner,string message){}}
}
