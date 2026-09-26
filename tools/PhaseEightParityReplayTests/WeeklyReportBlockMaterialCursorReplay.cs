using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

internal static class WeeklyReportBlockMaterialCursorReplay
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    internal static void Run(Assembly af)
    {
        Type type = af.GetType("AnimusForge.WeeklyReportBlockMaterialCursor`1", true).MakeGenericType(typeof(string));
        object cursor = Activator.CreateInstance(type, Members, null,
            new object[] { new List<string> { "first", "drop", "last" } }, null);
        Func<string, string> clone = item => item == "drop" ? null : new string(item.ToCharArray());
        bool Advance() => (bool)type.GetMethod("Advance", Members).Invoke(cursor, new object[] { clone });
        bool Complete() => (bool)type.GetProperty("Complete", Members).GetValue(cursor);
        IList Cloned() => (IList)type.GetProperty("Cloned", Members).GetValue(cursor);
        Check(!Complete() && Advance() && Cloned().Count == 1 && !ReferenceEquals(Cloned()[0], "first"),
            "first material cloned once");
        Check(Advance() && Cloned().Count == 1 && !Complete(), "null clone skipped but cursor advanced");
        Check(Advance() && Complete() && Cloned().Count == 2 && (string)Cloned()[1] == "last",
            "resume and finish without dropping last material");
        Check(!Advance() && Cloned().Count == 2, "completed cursor cannot clone twice");
        Console.WriteLine("PASS WeeklyReportBlockMaterialCursorReplay one-step/null/resume/no-replay live=NOT_RUN");
    }

    private static void Check(bool passed, string name)
    {
        if (!passed) throw new InvalidOperationException("WeeklyReportBlockMaterialCursorReplay: " + name);
    }
}
