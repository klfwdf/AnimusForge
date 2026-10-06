namespace MCM.Abstractions.Attributes
{
    public class SettingPropertyGroupAttribute:Attribute { public int GroupOrder{get;set;} public SettingPropertyGroupAttribute(string name){} }
}
namespace MCM.Abstractions.Attributes.v2
{
    public class SettingPropertyDropdownAttribute:Attribute
    { public int Order{get;set;} public bool RequireRestart{get;set;} public string HintText{get;set;} public SettingPropertyDropdownAttribute(string name){} }
    public class SettingPropertyBoolAttribute:SettingPropertyDropdownAttribute {public SettingPropertyBoolAttribute(string name):base(name){} }
    public class SettingPropertyIntegerAttribute:SettingPropertyDropdownAttribute {public SettingPropertyIntegerAttribute(string name,int min,int max,string format):base(name){} }
}
namespace MCM.Abstractions.Base.Global
{
    public abstract class AttributeGlobalSettings<T>
    {public static T Instance{get;set;} public abstract string Id{get;}public abstract string DisplayName{get;}public abstract string FolderName{get;}public abstract string FormatType{get;} }
}
namespace MCM.Common
{
    public class Dropdown<T> {public int SelectedIndex{get;set;} public T[] Values;public Dropdown(T[] values,int selected){Values=values;SelectedIndex=selected;} }
}
namespace AnimusForge.DialogueUI
{ internal static class DialogueUiRuntime { internal static void Log(string s){} } }
namespace TaleWorlds.GauntletUI {public class UIContext{}public class Brush{public int FontSize{get;set;}}}
namespace TaleWorlds.GauntletUI.BaseTypes
{
    public class TextWidget
    {public TaleWorlds.GauntletUI.Brush Brush=new();public TextWidget(TaleWorlds.GauntletUI.UIContext c){} protected virtual void OnUpdate(float dt){} public void Frame()=>OnUpdate(.016f);}
    public class RichTextWidget:TextWidget{public RichTextWidget(TaleWorlds.GauntletUI.UIContext c):base(c){} }
}
namespace AnimusForge
{public class DevMultilineEditableTextWidget:TaleWorlds.GauntletUI.BaseTypes.TextWidget
 {public int EditorFontSize{get;set;}public DevMultilineEditableTextWidget(TaleWorlds.GauntletUI.UIContext c):base(c){} } }
