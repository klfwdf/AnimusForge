using System;
using System.Collections.Generic;
using System.Reflection;

internal static class WeeklyMaterialStageCursorReplay
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    internal static void Run(Assembly af)
    {
        Type type = af.GetType("AnimusForge.WeeklyMaterialStageCursor`1", true).MakeGenericType(typeof(string));
        object Cursor(IReadOnlyList<string> values) => Activator.CreateInstance(type, Members, null, new object[] { values }, null);
        bool Complete(object cursor) => (bool)type.GetProperty("Complete", Members).GetValue(cursor);
        bool Take(object cursor, out string value)
        {
            object[] args = { null };
            bool taken = (bool)type.GetMethod("TryTake", Members).Invoke(cursor, args);
            value = (string)args[0];
            return taken;
        }
        object empty = Cursor(new List<string>());
        Check(Complete(empty) && !Take(empty, out _), "empty stage completes without work");
        object cursor = Cursor(new List<string> { null, "first", "second" });
        Check(!Complete(cursor) && Take(cursor, out string skipped) && skipped == null && !Complete(cursor),
            "null group consumes exactly one budget step");
        Check(Take(cursor, out string first) && first == "first" && !Complete(cursor), "first group identity");
        Check(Take(cursor, out string second) && second == "second" && Complete(cursor), "last group completes stage");
        Check(!Take(cursor, out string after) && after == null && Complete(cursor), "completed stage cannot replay");
        Console.WriteLine("PASS WeeklyMaterialStageCursorReplay empty/null/one-step/resume/complete live=NOT_RUN");
    }

    private static void Check(bool passed, string name)
    {
        if (!passed) throw new InvalidOperationException("WeeklyMaterialStageCursorReplay: " + name);
    }
}
