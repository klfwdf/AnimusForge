using System;
using System.Collections;
using System.Reflection;
using System.Runtime.Serialization;
using System.Xml;

// Header geometry uses native managed Widget/StackLayout, with clock/render/input absent.
public static class InteriorHeaderLayoutChecks
{
    private const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    private static object Bare(Type t) { return FormatterServices.GetUninitializedObject(t); }
    private static void Set(object target, string name, object value) { target.GetType().GetProperty(name, All).SetValue(target, value, null); }
    private static object Get(object target, string name) { return target.GetType().GetProperty(name, All).GetValue(target, null); }
    public static void Run(XmlDocument layout, Action<bool, string> check)
    {
        var header = (XmlElement)layout.SelectSingleNode("//TextWidget[@Text='@HeaderTitle']/../..");
        var children = header.SelectNodes("Children/*");
        check(children.Count == 3, "header has spacer, text column and deadline block");
        var ui = Assembly.Load("TaleWorlds.GauntletUI");
        var contextType = ui.GetType("TaleWorlds.GauntletUI.UIContext", true);
        var widgetType = ui.GetType("TaleWorlds.GauntletUI.BaseTypes.Widget", true);
        var listType = ui.GetType("TaleWorlds.GauntletUI.BaseTypes.ListPanel", true);
        var context = Bare(contextType);
        foreach (string name in new[] { "Scale", "CustomScale", "InverseScale", "CustomInverseScale" }) Set(context, name, 1f);
        var eventType = ui.GetType("TaleWorlds.GauntletUI.EventManager", true);
        var events = Bare(eventType); Set(context, "EventManager", events);
        var containersField = eventType.GetField("_widgetContainers", All);
        var containers = (IDictionary)Activator.CreateInstance(containersField.FieldType);
        var containerArgs = containersField.FieldType.GetGenericArguments();
        foreach (object key in Enum.GetValues(containerArgs[0])) containers[key] = Activator.CreateInstance(containerArgs[1], All, null, new object[] { 32 }, null);
        containersField.SetValue(events, containers);
        var factoryType = ui.GetType("TaleWorlds.GauntletUI.BrushFactory", true);
        var factory = Bare(factoryType); Set(context, "BrushFactory", factory);
        var brushesField = factoryType.GetField("_brushes", All);
        var brushes = (IDictionary)Activator.CreateInstance(brushesField.FieldType);
        var brushType = brushesField.FieldType.GetGenericArguments()[1];
        var brush = Activator.CreateInstance(brushType); Set(brush, "Name", "DefaultBrush"); brushes.Add("DefaultBrush", brush); brushesField.SetValue(factory, brushes);
        var infoType = ui.GetType("TaleWorlds.GauntletUI.WidgetInfo", true);
        var infosField = infoType.GetField("_widgetInfos", All);
        var infos = (IDictionary)Activator.CreateInstance(infosField.FieldType);
        foreach (var type in new[] { widgetType, listType }) infos.Add(type, Activator.CreateInstance(infoType, new object[] { type }));
        infosField.SetValue(null, infos);
        var measure = widgetType.GetMethod("Measure");
        var vectorType = measure.GetParameters()[0].ParameterType;
        foreach (bool hasBlock in new[] { false, true })
        foreach (float width in new[] { 900f, 1200f, 1600f })
        {
            var row = Activator.CreateInstance(listType, new[] { context });
            Set(row, "WidthSizePolicy", Enum.Parse(widgetType.GetProperty("WidthSizePolicy").PropertyType, "StretchToParent"));
            Set(row, "HeightSizePolicy", Enum.Parse(widgetType.GetProperty("HeightSizePolicy").PropertyType, "Fixed")); Set(row, "SuggestedHeight", 112f);
            var stack = Get(row, "StackLayout"); Set(stack, "LayoutMethod", Enum.Parse(stack.GetType().GetProperty("LayoutMethod").PropertyType, header.GetAttribute("StackLayout.LayoutMethod")));
            var cells = new object[3];
            for (int i = 0; i < cells.Length; i++)
            {
                var xml = (XmlElement)children[i]; var cell = cells[i] = Activator.CreateInstance(widgetType, new[] { context });
                foreach (string axis in new[] { "Width", "Height" })
                {
                    string policy = axis + "SizePolicy"; Set(cell, policy, Enum.Parse(widgetType.GetProperty(policy).PropertyType, xml.GetAttribute(policy)));
                    if (xml.HasAttribute("Suggested" + axis)) Set(cell, "Suggested" + axis, float.Parse(xml.GetAttribute("Suggested" + axis), System.Globalization.CultureInfo.InvariantCulture));
                }
                Set(cell, "IsVisible", xml.GetAttribute("IsVisible") != "@HasBlock" || hasBlock);
                widgetType.GetMethod("AddChild").Invoke(row, new[] { cell });
            }
            measure.Invoke(row, new[] { Activator.CreateInstance(vectorType, new object[] { width, 112f }) });
            widgetType.GetMethod("Layout").Invoke(row, new object[] { 0f, 112f, width, 0f });
            float center = (float)Get(cells[1], "Left") + (float)Get(cells[1], "Right");
            check(Math.Abs(center - width) < .01f, "header centered across dossier width=" + width + " HasBlock=" + hasBlock);
        }
    }
}
