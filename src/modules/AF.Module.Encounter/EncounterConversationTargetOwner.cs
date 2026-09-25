using System;

namespace AnimusForge.Refactor.Modules;

// Owns the original-conversation selection order; the host supplies Bannerlord extraction and eligibility.
internal static class EncounterConversationTargetOwner
{
    internal static TTarget Resolve<TTarget>(object instance, object[] args,
        Func<object, TTarget> extract, Func<TTarget, bool> usable,
        Func<TTarget> encounterFallback) where TTarget : class
    {
        if (args != null)
        {
            foreach (object arg in args)
            {
                TTarget target = extract(arg);
                if (usable(target)) return target;
            }
        }

        TTarget instanceTarget = extract(instance);
        if (usable(instanceTarget)) return instanceTarget;

        TTarget fallback = encounterFallback();
        return usable(fallback) ? fallback : null;
    }
}
