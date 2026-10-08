using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
namespace TaleWorlds.Library {
 [AttributeUsage(AttributeTargets.Property)] public class DataSourcePropertyAttribute:Attribute{}
 public class ViewModel { protected bool SetField<T>(ref T field,T value,string name){field=value;return true;} public void OnPropertyChanged(string p){} public void OnPropertyChangedWithValue<T>(T v,string p){} public virtual void OnFinalize(){} }
}
namespace TaleWorlds.Core { }
namespace TaleWorlds.InputSystem { public static class Input { public static string Clipboard; public static string GetClipboardText()=>Clipboard; public static void SetClipboardText(string value)=>Clipboard=value; } }
namespace TaleWorlds.Core.ViewModelCollection.Selector {
 public class SelectorItemVM { }
 public class SelectorVM<T> { public int SelectedIndex; public List<string> Options; public SelectorVM(List<string> values,int selected,Action<SelectorVM<T>> callback){ Options=values;SelectedIndex=selected; callback(this); } }
}
namespace AnimusForge.Refactor.Contracts { public class ModelCatalogExchange {public bool IsSuccessStatusCode; public string ResponseBody;} }
namespace AnimusForge.Refactor.Adapters { public class LegacyModelCatalogGateway { public Task<AnimusForge.Refactor.Contracts.ModelCatalogExchange> FetchModelsAsync(string url,string key,CancellationToken token)=>Task.FromResult(new AnimusForge.Refactor.Contracts.ModelCatalogExchange()); } }
namespace AnimusForge.Refactor.Modules { }
namespace AnimusForge {
 public static class ExternalBrowserLauncher { public static bool TryOpen(string url,out bool fallback,out string failure){fallback=false;failure="";return true;} }
 public class DuelSettings {
  public static DuelSettings Current=new(); public static DuelSettings GetSettings()=>Current;
  public string ApiUrl="primary",ApiKey="p-key",ModelName="persisted-main",AuxiliaryApiUrl="aux",AuxiliaryApiKey="a-key",AuxiliaryModelName="persisted-aux",ActionPostprocessApiUrl="post",ActionPostprocessApiKey="s-key",ActionPostprocessModelName="persisted-post",EventAndRebellionApiUrl="event",EventAndRebellionApiKey="e-key",EventAndRebellionModelName="persisted-event";
  public bool UseAuxiliaryRuleApi,MainApiThinkingEnabled,AuxiliaryApiThinkingEnabled,ActionPostprocessApiThinkingEnabled,EventAndRebellionApiThinkingEnabled;
  public float MainApiTemperature,AuxiliaryApiTemperature,ActionPostprocessApiTemperature,EventAndRebellionApiTemperature;
  public const string ReasoningEffortMax="max",ReasoningEffortHigh="high";
  public void SetMainApiReasoningEffortForExternal(string s){} public void SetAuxiliaryApiReasoningEffortForExternal(string s){} public void SetActionPostprocessApiReasoningEffortForExternal(string s){} public void SetEventAndRebellionApiReasoningEffortForExternal(string s){} public static void OpenAfdianSupportPageForExternal(){}
 }
 public static class ModOnboardingBehavior {
  public enum ApiSetupTarget {Primary,Auxiliary,ActionPostprocess,EventAndRebellion}
  public class ApiValidationTargetInfo { public ApiSetupTarget Target; public string DisplayName,ApiUrl,ApiKey,ModelName; }
  public class ApiValidationTargetResult { public bool Success; public string FailureHint; }
  public static readonly ConcurrentQueue<ApiValidationTargetInfo> Calls=new(); public static int Saves;
  public static Func<ApiValidationTargetInfo,CancellationToken,Task<ApiValidationTargetResult>> Validate=(t,c)=>Task.FromResult(new ApiValidationTargetResult{Success=true});
  public static Task<ApiValidationTargetResult> ValidateApiTargetAsync(ApiValidationTargetInfo t,CancellationToken c){Calls.Enqueue(t);return Validate(t,c);}
  public static List<string> ExtractModelNamesFromResponse(string s)=>new();
  public static void TryPersistMcmSettings(DuelSettings s){Saves++;}
  public static void SetModelNameForTarget(DuelSettings s,ApiSetupTarget t,string model){ switch(t){case ApiSetupTarget.Primary:s.ModelName=model;break;case ApiSetupTarget.Auxiliary:s.AuxiliaryModelName=model;break;case ApiSetupTarget.ActionPostprocess:s.ActionPostprocessModelName=model;break;case ApiSetupTarget.EventAndRebellion:s.EventAndRebellionModelName=model;break;} }
 }
}
namespace MCM.Abstractions.Attributes { public class SettingPropertyGroupAttribute:Attribute {public SettingPropertyGroupAttribute(string s){}public int GroupOrder{get;set;}} }
namespace MCM.Abstractions.Attributes.v2 { public class SettingPropertyIntegerAttribute:Attribute {public SettingPropertyIntegerAttribute(string s,int min,int max,string format){} public int Order{get;set;}public bool RequireRestart{get;set;}public string HintText{get;set;}} public class SettingPropertyBoolAttribute:Attribute {public SettingPropertyBoolAttribute(string s){}public int Order{get;set;}public bool RequireRestart{get;set;}public string HintText{get;set;}}
 public class SettingPropertyDropdownAttribute:Attribute {public SettingPropertyDropdownAttribute(string s){}public int Order{get;set;}public bool RequireRestart{get;set;}public string HintText{get;set;}} }
namespace MCM.Abstractions.Base.Global { public abstract class AttributeGlobalSettings<T> {public static T Instance; public abstract string Id{get;}public abstract string DisplayName{get;}public abstract string FolderName{get;}public abstract string FormatType{get;}} }
namespace MCM.Common {public class Dropdown<T> {public int SelectedIndex;public Dropdown(T[] values,int index){SelectedIndex=index;}} }
namespace AnimusForge.DialogueUI {internal static class DialogueUiRuntime {internal static void Log(string message){} } }

namespace AnimusForge {
 internal static class AnimusForgeModulePaths { internal static string Root; public static string GetCurrentModuleRoot()=>Root; }
 internal static class Logger {internal static void Log(string category,string message){} }
}
