using System;
using System.Reflection;

internal static class EncounterConversationTargetOwnerReplay
{
    internal static void Run(Assembly assembly)
    {
        Type owner = assembly.GetType("AnimusForge.Refactor.Modules.EncounterConversationTargetOwner", true);
        MethodInfo resolve = owner.GetMethod("Resolve", BindingFlags.NonPublic | BindingFlags.Static)
            .MakeGenericMethod(typeof(object));
        object selected = new object(), leader = new object(), instance = new object(), invalid = new object();
        int fallbackCalls = 0;
        Func<object, object> extract = value => value;
        Func<object, bool> usable = value => value != null && !ReferenceEquals(value, invalid);
        object Resolve(object source, object[] args, object fallback) => resolve.Invoke(null, new object[]
        {
            source, args, extract, usable, (Func<object>)(() => { fallbackCalls++; return fallback; })
        });
        void Check(bool value, string label)
        {
            if (!value) throw new InvalidOperationException("Encounter conversation target owner: " + label);
        }

        Check(ReferenceEquals(Resolve(instance, new[] { invalid, selected, leader }, leader), selected)
            && fallbackCalls == 0, "selected member argument did not outrank instance/leader");
        Check(ReferenceEquals(Resolve(instance, new[] { invalid }, leader), instance)
            && fallbackCalls == 0, "instance target did not outrank leader");
        Check(ReferenceEquals(Resolve(invalid, null, leader), leader) && fallbackCalls == 1,
            "missing arguments did not use leader last");
        Check(Resolve(null, new[] { invalid }, invalid) == null,
            "unusable fallback became target");
        Console.WriteLine("PASS EncounterConversationTargetOwnerReplay current-DLL selected-member/instance/leader precedence and invalid fallback; native conversation=NOT_RUN");
    }
}
