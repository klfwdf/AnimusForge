using System;
using System.Reflection;

internal static class OnboardingOperationVersionOwnerReplay
{
    internal static void Run(Assembly assembly)
    {
        const BindingFlags All = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        Type type = assembly.GetType("AnimusForge.Refactor.Modules.OnboardingOperationVersionOwner", true);
        Type kind = type.GetNestedType("Kind", All);
        object owner = Activator.CreateInstance(type, true);
        object Api = Enum.Parse(kind, "ApiValidation");
        object Url = Enum.Parse(kind, "BaseUrlValidation");
        object Models = Enum.Parse(kind, "ModelFetch");
        int Begin(object k) => (int)type.GetMethod("Begin", All).Invoke(owner, new[] { k });
        void Cancel(object k) => type.GetMethod("Cancel", All).Invoke(owner, new[] { k });
        bool Current(object k, int n) => (bool)type.GetMethod("IsCurrent", All).Invoke(owner, new[] { k, (object)n });
        void Check(bool condition, string name)
        {
            if (!condition) throw new InvalidOperationException("Onboarding operation version: " + name);
        }

        int api1 = Begin(Api), url1 = Begin(Url), models1 = Begin(Models);
        Check(Current(Api, api1) && Current(Url, url1) && Current(Models, models1), "independent begins");
        Cancel(Api);
        Check(!Current(Api, api1) && Current(Url, url1) && Current(Models, models1), "cancel invalidated unrelated work");
        int api2 = Begin(Api);
        Check(Current(Api, api2) && !Current(Api, api1), "reopened API admitted old completion");
        Cancel(Url);
        Cancel(Models);
        Check(!Current(Url, url1) && !Current(Models, models1), "late URL/model completion accepted");
        Console.WriteLine("PASS OnboardingOperationVersionOwnerReplay current-DLL independent/cancel/reopen/stale; live provider=NOT_RUN");
    }
}
