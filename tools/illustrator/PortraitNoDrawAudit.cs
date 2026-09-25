using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;

// Checks the shipped widget and creation path without constructing UI/native objects.
// It does not validate the GPU export or assert a cause for live Mission flickering.
public static class PortraitNoDrawAudit
{
    private const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
    private static int checks;
    private static void Check(bool result, string label)
    {
        if (!result) throw new Exception("FAIL " + label);
        checks++;
        Console.WriteLine("PASS " + label);
    }

    private static List<Tuple<OpCode, object>> ReadIl(MethodBase method)
    {
        var result = new List<Tuple<OpCode, object>>();
        var body = method.GetMethodBody();
        if (body == null) return result;
        byte[] bytes = body.GetILAsByteArray();
        var opcodes = typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(OpCode)).Select(f => (OpCode)f.GetValue(null))
            .ToDictionary(op => unchecked((ushort)op.Value));
        for (int i = 0; i < bytes.Length;)
        {
            ushort value = bytes[i++];
            if (value == 0xfe) value = (ushort)(0xfe00 | bytes[i++]);
            OpCode op = opcodes[value];
            int size;
            switch (op.OperandType)
            {
                case OperandType.InlineNone: size = 0; break;
                case OperandType.ShortInlineBrTarget: case OperandType.ShortInlineI: case OperandType.ShortInlineVar: size = 1; break;
                case OperandType.InlineVar: size = 2; break;
                case OperandType.InlineI8: case OperandType.InlineR: size = 8; break;
                case OperandType.InlineSwitch: size = 4 + 4 * BitConverter.ToInt32(bytes, i); break;
                default: size = 4; break;
            }
            object operand = null;
            if (op.OperandType == OperandType.InlineMethod)
                operand = method.Module.ResolveMethod(BitConverter.ToInt32(bytes, i), method.DeclaringType.GetGenericArguments(), method.IsGenericMethod ? method.GetGenericArguments() : null);
            else if (op.OperandType == OperandType.InlineField)
                operand = method.Module.ResolveField(BitConverter.ToInt32(bytes, i), method.DeclaringType.GetGenericArguments(), method.IsGenericMethod ? method.GetGenericArguments() : null);
            else if (op.OperandType == OperandType.InlineString)
                operand = method.Module.ResolveString(BitConverter.ToInt32(bytes, i));
            result.Add(Tuple.Create(op, operand));
            i += size;
        }
        return result;
    }

    public static void Run(string dllPath)
    {
        checks = 0;
        Assembly assembly = Assembly.LoadFrom(dllPath);
        Type widget = assembly.GetType("AnimusForge.Illustrator.Engine.NativeCharacterExportWidget", true);
        Type nativeWidget = widget.BaseType;
        Check(nativeWidget.FullName == "TaleWorlds.MountAndBlade.GauntletUI.Widgets.CharacterTableauWidget",
            "portrait export retains the native character widget and provider contract");
        MethodInfo render = widget.GetMethod("OnRender", All | BindingFlags.DeclaredOnly);
        Check(render != null && render.GetParameters().Select(p => p.ParameterType.FullName).SequenceEqual(new[] {
            "TaleWorlds.TwoDimension.TwoDimensionContext", "TaleWorlds.TwoDimension.TwoDimensionDrawContext" }),
            "export overrides the actual native render signature");
        var renderIl = ReadIl(render);
        Check(!renderIl.Any(i => i.Item2 is MethodBase || i.Item1 == OpCodes.Calli),
            "render submits no Draw, base render, or indirect calls");
        var writtenFields = renderIl.Where(i => i.Item1 == OpCodes.Stfld).Select(i => (FieldInfo)i.Item2).ToArray();
        Check(writtenFields.Length == 1 && writtenFields[0].Name == "_isRenderRequestedPreviousFrame" &&
            writtenFields[0].DeclaringType.FullName == "TaleWorlds.GauntletUI.BaseTypes.TextureWidget",
            "render only writes the native provider tick request flag");

        // The production render method needs no initialized native UI. Invoke only
        // that override on a managed shell; no constructor, engine or GPU is called.
        object shell = FormatterServices.GetUninitializedObject(widget);
        writtenFields[0].SetValue(shell, false);
        render.Invoke(shell, new object[] { null, null });
        Check((bool)writtenFields[0].GetValue(shell), "render keeps provider creation and ticking requested");
        var update = widget.GetMethod("OnUpdate", All | BindingFlags.DeclaredOnly);
        Check(update != null && ReadIl(update).Count(i => i.Item2 is MethodInfo &&
            ((MethodInfo)i.Item2).Name == "OnUpdate" && ((MethodInfo)i.Item2).DeclaringType == nativeWidget) == 1 &&
            widget.GetMethod("OnClearTextureProvider", All).DeclaringType != widget,
            "warmup counting calls native update once and keeps native deferred cleanup");
        var ctor = widget.GetConstructors(All).Single();
        var ctorIl = ReadIl(ctor);
        Check(ctorIl.Any(i => object.Equals(i.Item2, "act_inventory_idle_start")) &&
            ctorIl.Count(i => i.Item2 is MethodInfo && ((MethodInfo)i.Item2).Name == "set_IdleAction") == 1,
            "export explicitly initializes the native display idle once");
        foreach (string setter in new[] { "set_IsEquipmentAnimActive", "set_IsPlayingCustomAnimations", "set_ShouldLoopCustomAnimation" })
        {
            int index = ctorIl.FindIndex(i => i.Item2 is MethodInfo && ((MethodInfo)i.Item2).Name == setter);
            Check(index > 0 && ctorIl[index - 1].Item1 == OpCodes.Ldc_I4_0, "export disables " + setter);
        }
        Check(ReadIl(ctor).Count(i => i.Item2 is ConstructorInfo && ((ConstructorInfo)i.Item2).DeclaringType == nativeWidget) == 1,
            "export construction calls the original character widget constructor");
        Check(ReadIl(nativeWidget.GetConstructor(ctor.GetParameters().Select(p => p.ParameterType).ToArray()))
            .Any(i => object.Equals(i.Item2, "CharacterTableauTextureProvider")),
            "native construction selects CharacterTableauTextureProvider");

        Type helper = assembly.GetType("AnimusForge.Illustrator.Engine.ScreenCaptureHelper", true);
        var creators = assembly.GetTypes().Where(t => t == helper || t.FullName.StartsWith(helper.FullName + "+", StringComparison.Ordinal))
            .SelectMany(t => t.GetMethods(All | BindingFlags.DeclaredOnly))
            .Where(m => m.Name.Contains("ExtractViaStageAsync"))
            .Select(m => new { Method = m, Il = ReadIl(m) })
            .Where(entry => entry.Il.Any(i => i.Item1 == OpCodes.Newobj && i.Item2 is ConstructorInfo && ((ConstructorInfo)i.Item2).DeclaringType == widget))
            .ToArray();
        Check(creators.Length == 1, "production stage creation instantiates the no-draw export widget");
        var creationIl = creators[0].Il;
        Check(creationIl.Any(i => object.Equals(i.Item2, "OffscreenCharacter")) &&
            creationIl.Any(i => object.Equals(i.Item2, "NativeCharacterExport")),
            "portrait stage has a dedicated branch and a distinct ID from the hidden prefab placeholder");
        Check(creationIl.Any(i => i.Item2 is MethodInfo && ((MethodInfo)i.Item2).Name == "AddChild") &&
            creationIl.Any(i => i.Item2 is MethodInfo && ((MethodInfo)i.Item2).Name == "AddLayer"),
            "export remains attached to the normal Gauntlet update pipeline");
        var pumpIl = ReadIl(helper.GetMethod("PumpOffscreenStage", All));
        Check(pumpIl.Any(i => i.Item2 is MethodInfo && ((MethodInfo)i.Item2).Name == "ResolveTableauView") &&
            pumpIl.Any(i => i.Item2 is MethodInfo && ((MethodInfo)i.Item2).Name == "TriggerTableauViewSave"),
            "the stage still resolves and exports the native tableau view");
        Console.WriteLine("PORTRAIT NO DRAW AUDIT: " + checks + " PASS / 0 FAIL (no GPU/live-game acceptance)");
    }
}
