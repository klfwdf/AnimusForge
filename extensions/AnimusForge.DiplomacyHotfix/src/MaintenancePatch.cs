using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace AnimusForge.DiplomacyHotfix;

internal static class MaintenancePatch
{
    internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase original)
    {
        var code = instructions.Select(i => new CodeInstruction(i)).ToList();
        var owner = original.DeclaringType;
        var cursor = AccessTools.Field(owner, "_playerRoundCursor");
        var storage = AccessTools.PropertyGetter(owner, "Storage");
        var storageType = storage?.ReturnType;
        var concurrent = AccessTools.PropertyGetter(storageType, "ConcurrentRounds");
        var active = AccessTools.PropertyGetter(storageType, "ActiveRound");
        if (cursor == null || storage == null || concurrent == null || active == null)
            throw new InvalidOperationException("maintenance_fields_not_supported");
        Type round = active.ReturnType;
        var item = concurrent.ReturnType.GetProperty("Item")?.GetGetMethod();
        if (code.Any(i => i.opcode == OpCodes.Newarr && Equals(i.operand, round)))
            throw new InvalidOperationException("maintenance_already_has_snapshot");
        int itemIndex = code.FindIndex(i => i.Calls(item));
        int activeIndex = code.FindIndex(i => i.Calls(active));
        // The old method has one indexed read and one active-slot read in its loop.
        if (itemIndex < 0 || activeIndex < 0 || code.Count(i => i.Calls(item)) != 1
            || code.Count(i => i.Calls(active)) != 1)
            throw new InvalidOperationException("maintenance_reads_not_supported");
        int remainder = code.FindIndex(i => i.opcode == OpCodes.Rem);
        if (remainder < 1 || remainder > itemIndex || !code.Take(remainder).Any(i => Equals(i.operand, cursor)))
            throw new InvalidOperationException("maintenance_cursor_not_supported");
        var countLoad = new CodeInstruction(code[remainder - 1].opcode, code[remainder - 1].operand);
        if (!IsLoadLocal(countLoad)) throw new InvalidOperationException("maintenance_count_not_supported");
        int countStore = -1;
        for (int i = remainder - 1; i >= 0; i--) if (StoresSameLocal(code[i], countLoad)) { countStore = i; break; }
        if (countStore < 0 || countStore >= remainder) throw new InvalidOperationException("maintenance_count_store_not_supported");
        int cursorStore = code.FindIndex(i => i.opcode == OpCodes.Stfld && Equals(i.operand, cursor));
        if (cursorStore <= countStore || cursorStore >= remainder) throw new InvalidOperationException("maintenance_cursor_store_not_supported");
        var snapshot = generator.DeclareLocal(round.MakeArrayType());
        var start = generator.DeclareLocal(typeof(int));
        var capture = typeof(MaintenancePatch).GetMethod(nameof(Capture), BindingFlags.Public | BindingFlags.Static).MakeGenericMethod(round);
        var read = typeof(MaintenancePatch).GetMethod(nameof(Read), BindingFlags.Public | BindingFlags.Static).MakeGenericMethod(round);
        var readActive = typeof(MaintenancePatch).GetMethod(nameof(ReadActive), BindingFlags.Public | BindingFlags.Static).MakeGenericMethod(round);
        var result = new List<CodeInstruction>();
        for (int n = 0; n < code.Count; n++)
        {
            CodeInstruction current = code[n];
            if (n == cursorStore)
            {
                var first = new CodeInstruction(countLoad);
                first.labels.AddRange(current.labels); current.labels.Clear();
                first.blocks.AddRange(current.blocks); current.blocks.Clear();
                result.Add(first);
                result.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(MaintenancePatch), nameof(BoundCursor))));
            }
            if (n == itemIndex || n == activeIndex)
            {
                var first = new CodeInstruction(OpCodes.Ldloc, snapshot);
                first.labels.AddRange(current.labels); current.labels.Clear();
                first.blocks.AddRange(current.blocks); current.blocks.Clear();
                result.Add(first);
                result.Add(new CodeInstruction(OpCodes.Ldloc, start));
                result.Add(new CodeInstruction(countLoad));
                current.opcode = OpCodes.Call; current.operand = n == itemIndex ? read : readActive;
            }
            result.Add(current);
            if (n == countStore)
            {
                result.Add(new CodeInstruction(OpCodes.Ldarg_0)); result.Add(new CodeInstruction(OpCodes.Call, storage));
                result.Add(new CodeInstruction(OpCodes.Callvirt, concurrent));
                result.Add(new CodeInstruction(OpCodes.Ldarg_0)); result.Add(new CodeInstruction(OpCodes.Call, storage));
                result.Add(new CodeInstruction(OpCodes.Callvirt, active));
                result.Add(new CodeInstruction(OpCodes.Ldarg_0)); result.Add(new CodeInstruction(OpCodes.Ldflda, cursor));
                result.Add(new CodeInstruction(OpCodes.Ldloca, start));
                result.Add(new CodeInstruction(OpCodes.Call, capture)); result.Add(new CodeInstruction(OpCodes.Stloc, snapshot));
            }
        }
        return result;
    }

    // The owning game-hour gate runs before this injection. No reflection or full-list copy here.
    public static T[] Capture<T>(List<T> concurrent, T active, ref int cursor, out int start)
    {
        int count = (concurrent?.Count ?? 0) + 1;
        start = cursor < 0 ? 0 : cursor % count;
        cursor = start;
        var slice = new T[Math.Min(8, count)];
        for (int i = 0; i < slice.Length; i++)
        {
            int index = (start + i) % count;
            slice[i] = index == 0 ? active : concurrent[index - 1];
        }
        return slice;
    }

    public static T Read<T>(List<T> unused, int index, T[] slice, int start, int count)
        => slice[(index + 1 - start + count) % count];
    public static T ReadActive<T>(object unused, T[] slice, int start, int count)
        => slice[(count - start) % count];
    public static int BoundCursor(int next, int count) => next < 0 || next >= count ? 0 : next;

    private static bool IsLoadLocal(CodeInstruction i) => i.opcode == OpCodes.Ldloc || i.opcode == OpCodes.Ldloc_S
        || i.opcode == OpCodes.Ldloc_0 || i.opcode == OpCodes.Ldloc_1 || i.opcode == OpCodes.Ldloc_2 || i.opcode == OpCodes.Ldloc_3;
    private static int LocalIndex(CodeInstruction i)
    {
        if (i.operand is LocalBuilder b) return b.LocalIndex;
        if (i.operand is LocalVariableInfo v) return v.LocalIndex;
        if (i.operand is byte n) return n;
        if (i.operand is int x) return x;
        if (i.opcode == OpCodes.Ldloc_0 || i.opcode == OpCodes.Stloc_0) return 0;
        if (i.opcode == OpCodes.Ldloc_1 || i.opcode == OpCodes.Stloc_1) return 1;
        if (i.opcode == OpCodes.Ldloc_2 || i.opcode == OpCodes.Stloc_2) return 2;
        if (i.opcode == OpCodes.Ldloc_3 || i.opcode == OpCodes.Stloc_3) return 3;
        return -1;
    }
    private static bool StoresSameLocal(CodeInstruction store, CodeInstruction load) =>
        (store.opcode == OpCodes.Stloc || store.opcode == OpCodes.Stloc_S || store.opcode == OpCodes.Stloc_0
        || store.opcode == OpCodes.Stloc_1 || store.opcode == OpCodes.Stloc_2 || store.opcode == OpCodes.Stloc_3)
        && LocalIndex(store) == LocalIndex(load);
}
