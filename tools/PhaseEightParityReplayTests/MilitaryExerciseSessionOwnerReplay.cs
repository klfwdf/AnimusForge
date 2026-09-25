using System;
using System.Reflection;

internal static class MilitaryExerciseSessionOwnerReplay
{
    internal static void Run(Assembly assembly)
    {
        const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        Type generic = assembly.GetType("AnimusForge.Refactor.Modules.MilitaryExerciseSessionOwner`2", true);
        Type type = generic.MakeGenericType(typeof(object), typeof(object));
        object second = new object(), first = new object(), runtime = new object();
        bool settled = false;
        object owner = Activator.CreateInstance(type, All, null,
            new object[] { (Func<object, bool>)(selection => ReferenceEquals(selection, second)),
                (Func<object, bool>)(_ => settled) }, null);
        object Get(string name) => type.GetProperty(name, All).GetValue(owner);
        void Set(string name, object value) => type.GetProperty(name, All).SetValue(owner, value);
        object Call(string name, params object[] args) => type.GetMethod(name, All).Invoke(owner, args);
        void Check(bool condition, string name)
        {
            if (!condition) throw new InvalidOperationException("Exercise session owner: " + name);
        }

        Set("Selection", first);
        Check((bool)Call("IsCurrentSelection", first)
            && !(bool)Call("IsCurrentSelection", second), "selection callback identity drifted");
        Call("QueueSecond", 10f, 0.2f);
        Check(!(bool)Call("IsSecondDue", 11f) && !(bool)Get("SecondQueued"),
            "first-stage selection retained second-stage ticket");
        Set("Selection", second);
        Check(!(bool)Call("IsCurrentSelection", first)
            && (bool)Call("IsCurrentSelection", second), "stale first-stage callback retained authority");
        Call("QueueSecond", 10f, 0.2f);
        Check(!(bool)Call("IsSecondDue", 10.19f) && (bool)Call("IsSecondDue", 10.21f),
            "second-stage delay was not respected");
        Check((bool)Call("BeginSecond") && !(bool)Get("SecondQueued") && (bool)Get("IsOpening"),
            "second-stage ticket was not consumed once");
        Check(!(bool)Call("BeginSecond"), "second-stage ticket replayed");
        Call("ResetSelection");
        Check(Get("Selection") == null && !(bool)Get("IsOpening"), "selection cancellation leaked state");

        Set("Runtime", runtime);
        Call("QueueBattle", 20f, 0.35f);
        Check(!(bool)Call("IsBattleDue", 20.34f) && (bool)Call("IsBattleDue", 20.36f),
            "battle delay was not respected");
        Check((bool)Call("BeginBattle") && !(bool)Get("BattleQueued") && (bool)Get("IsOpening"),
            "battle ticket was not consumed once");
        Check(!(bool)Call("BeginBattle"), "battle ticket replayed");
        Check((bool)Call("NeedsEngineTick", true, false, false), "active runtime lost tick");
        settled = true;
        Check(!(bool)Call("NeedsEngineTick", true, false, false), "settled runtime kept tick");
        Call("QueueBattle", 30f, 0.35f);
        Check(!(bool)Call("IsBattleDue", 31f) && !(bool)Get("BattleQueued"),
            "settled runtime kept battle ticket");
        settled = false;
        Call("ReleaseRuntime", new object());
        Check(ReferenceEquals(Get("Runtime"), runtime), "unrelated cleanup released active runtime");
        Call("ReleaseRuntime", runtime);
        Check(Get("Runtime") == null && !(bool)Get("IsOpening"), "runtime cleanup leaked session");
        Check((bool)Call("NeedsEngineTick", false, false, false)
            && (bool)Call("NeedsEngineTick", true, true, false)
            && (bool)Call("NeedsEngineTick", true, false, true), "patch/orphan tick gates drifted");
        Console.WriteLine("PASS MilitaryExerciseSessionOwnerReplay current-DLL stage/delay/one-shot/cancel/settled/identity cleanup; live UI/Mission=NOT_RUN");
    }
}
