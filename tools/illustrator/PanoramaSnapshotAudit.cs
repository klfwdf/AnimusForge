using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

// Offline contract checks only. No Scene, GameEntity or renderer is constructed.
public static class PanoramaSnapshotAudit
{
    private const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
    private static int checks;
    private static void Check(bool result, string label)
    {
        if (!result) throw new Exception("FAIL " + label);
        checks++; Console.WriteLine("PASS " + label);
    }
    private static List<Tuple<OpCode, object>> ReadIl(MethodBase method)
    {
        var result = new List<Tuple<OpCode, object>>();
        if (method.GetMethodBody() == null) return result;
        byte[] bytes = method.GetMethodBody().GetILAsByteArray();
        var codes = typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(OpCode)).Select(f => (OpCode)f.GetValue(null))
            .ToDictionary(o => unchecked((ushort)o.Value));
        for (int i = 0; i < bytes.Length;)
        {
            ushort value = bytes[i++];
            if (value == 0xfe) value = (ushort)(0xfe00 | bytes[i++]);
            OpCode op = codes[value];
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
            else if (op.OperandType == OperandType.InlineString) operand = method.Module.ResolveString(BitConverter.ToInt32(bytes, i));
            result.Add(Tuple.Create(op, operand)); i += size;
        }
        return result;
    }
    public static void Run(string path)
    {
        checks = 0;
        Assembly assembly = Assembly.LoadFrom(path);
        Type helper = assembly.GetType("AnimusForge.Illustrator.Engine.ScreenCaptureHelper", true);
        Type snapshot = assembly.GetType("AnimusForge.Illustrator.Engine.PanoramaSceneSnapshot", true);
        Type builder = helper.GetNestedType("PanoramaSnapshotBuilder", All);
        MethodInfo eligible = helper.GetMethod("ShouldCopyPanoramaRoot", All);
        bool correct = true;
        for (int bits = 0; bits < 32; bits++)
        {
            bool valid = (bits & 1) != 0, visible = (bits & 2) != 0, marker = (bits & 4) != 0;
            bool animated = (bits & 8) != 0, agent = (bits & 16) != 0;
            correct &= (bool)eligible.Invoke(null, new object[] { valid, visible, marker, animated, agent }) == (valid && visible && !marker && !animated && !agent);
        }
        Check(correct, "all 32 eligibility combinations preserve only actual visible safe entities");
        MethodInfo mode = helper.GetMethod("SelectPanoramaCopyMode", All);
        Func<bool, bool, bool, int> select = (a, b, c) => (int)mode.Invoke(null, new object[] { a, b, c });
        Check(select(true, false, false) == 1, "safe static prefab keeps its complete current subtree");
        Check(select(true, true, false) == 2, "mixed parent retains own static meshes instead of dropping the building");
        Check(select(false, true, false) == 0, "animated branch itself is excluded");
        Check(select(true, false, true) == 0, "a child of an already copied subtree is not duplicated");
        Check(select(true, false, false) == 1, "safe sibling under split parent remains selectable");
        MethodInfo budget = helper.GetMethod("ContinuePanoramaSnapshotBatch", All);
        Check((bool)budget.Invoke(null, new object[] { 7, 3.9 }) && !(bool)budget.Invoke(null, new object[] { 8, 0d }), "eight native copies is the hard per-batch count cap");
        Check(!(bool)budget.Invoke(null, new object[] { 1, 4d }) && (bool)budget.Invoke(null, new object[] { 0, 5d }), "4ms soft budget yields after the first indivisible copy");
        MethodInfo count = helper.GetMethod("PanoramaSnapshotRootCountAllowed", All);
        Check((bool)count.Invoke(null, new object[] { 4096 }) && !(bool)count.Invoke(null, new object[] { 4097 }) && !(bool)count.Invoke(null, new object[] { -1 }), "root count bound rejects overflow rather than returning truncated success");
        Check((int)helper.GetField("PanoramaSnapshotMaxNodes", All).GetRawConstantValue() == 32768, "descendant inspection has a finite total node limit");

        var methods = builder.GetMethods(All | BindingFlags.DeclaredOnly).Cast<MethodBase>()
            .Concat(snapshot.GetMethods(All | BindingFlags.DeclaredOnly)).ToArray();
        var calls = methods.SelectMany(ReadIl).Select(i => i.Item2).OfType<MethodBase>().ToArray();
        Check(calls.Any(m => m.DeclaringType.FullName == "TaleWorlds.Engine.Scene" && m.Name == "CreateNewScene"), "production creates an independent Scene");
        Check(calls.Any(m => m.Name == "GetRootEntities") && !calls.Any(m => m.Name == "GetEntities"), "production enumerates native roots once, not repeated flattened scene scans");
        Check(calls.Any(m => m.Name == "CopyFrom" && m.DeclaringType.FullName == "TaleWorlds.Engine.GameEntity"), "production copies actual GameEntity instances");
        var copyIl = ReadIl(builder.GetMethod("CopyBatch", All));
        int copyIndex = copyIl.FindIndex(i => i.Item2 is MethodBase && ((MethodBase)i.Item2).Name == "CopyFrom");
        Check(copyIndex >= 2 && copyIl[copyIndex - 1].Item1 == OpCodes.Ldc_I4_0 && copyIl[copyIndex - 2].Item1 == OpCodes.Ldc_I4_0,
            "native entity copy disables physics and script callbacks");
        Check(calls.Any(m => m.DeclaringType.FullName == "TaleWorlds.Engine.MetaMesh" && m.Name == "CreateCopy"), "mixed-parent meshes copy current mesh data instead of resolving asset names");
        Check(calls.Any(m => m.Name == "GetGlobalFrame") && calls.Any(m => m.Name == "SetFrame"), "detached safe subtrees keep their global placement");
        Check(!calls.Any(m => m.Name == "Tick" || m.Name == "OnTick" || m.Name == "Read" || m.Name == "Instantiate"), "snapshot never manually ticks or reloads template scenes or prefabs");
        Check(!calls.Any(m => m.Name == "SetCamera" || m.Name == "set_CustomCamera" || m.Name == "SetCameraFrame" || m.Name == "SetVisible"), "snapshot never changes live cameras or character visibility");
        Check(calls.Any(m => m.Name == "SetDoNotAddEntitiesToTickList") && calls.Any(m => m.Name == "SetClothSimulationState"), "private scene suppresses script ticking and cloth simulation");
        Check(calls.Any(m => m.Name == "ThrowIfCancellationRequested") && calls.Any(m => m.Name == "get_CurrentState"), "production checks cancellation and mission lifecycle");
        Check(ReadIl(builder.GetMethod("ReleaseRoots", All)).Any(i => i.Item2 is MethodBase && ((MethodBase)i.Item2).Name == "ManualInvalidate"), "native root array is explicitly released after selection");
        Check(snapshot.GetMethod("MarkRendered", All) != null && snapshot.GetMethod("DisposeUnrendered", All) != null,
            "snapshot exposes distinct pre-render and engine-owned retirement paths");
        string[] notes = ReadIl(snapshot.GetProperty("Notes", All).GetGetMethod(true)).Select(i => i.Item2).OfType<string>().ToArray();
        Check(notes.Any(s => s.Contains("中性材质观察光")) && notes.Any(s => s.Contains("原生地形")), "reference notes disclose inspection lighting and missing native terrain");
        Console.WriteLine("PANORAMA SNAPSHOT AUDIT: " + checks + " PASS / 0 FAIL (no GPU/live-game acceptance)");
    }
}
