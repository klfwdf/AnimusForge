using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using Newtonsoft.Json.Linq;

namespace AnimusForge.DiplomacyHotfix;

internal static class FrozenRequestPatch
{
    private static readonly AsyncLocal<Func<bool>> SendAdmission = new AsyncLocal<Func<bool>>();
    internal static MethodInfo FrozenEntry;

    internal static void Configure(Type client, Type result)
    {
        typeof(FrozenRequestPatch).GetMethod(nameof(ConfigureResult), BindingFlags.NonPublic | BindingFlags.Static)
            .MakeGenericMethod(result).Invoke(null, new object[] { client });
        FrozenEntry = typeof(FrozenRequestPatch).GetMethod(nameof(CallFrozenAsync), BindingFlags.Public | BindingFlags.Static)
            .MakeGenericMethod(result);
    }

    private delegate bool Prepare<T>(JArray messages, int tokens, int timeout, string source, long generation,
        out Func<CancellationToken, Task<T>> call, out string error);

    private static class Result<T>
    {
        internal static Prepare<T> Prepare;
        internal static Func<T, bool> Success, BudgetDeferred, Truncated, AuthFailure, QuotaLimit, NonRetryable;
        internal static Func<T, int?> RetryAfter;
        internal static Action<T, int> SetAttempts;
        internal static Func<string, T> Error;
        internal static Func<long, string, T, bool> Accept;
    }

    private static void ConfigureResult<T>(Type client)
    {
        var prepare = AccessTools.Method(client, "TryPrepareSingleCall");
        if (prepare == null || prepare.GetParameters().Length != 7)
            throw new InvalidOperationException("prepared_call_shape_not_supported");
        Result<T>.Prepare = (Prepare<T>)Delegate.CreateDelegate(typeof(Prepare<T>), prepare);
        Result<T>.Success = Flag<T>("Success"); Result<T>.BudgetDeferred = Flag<T>("BudgetDeferred");
        Result<T>.Truncated = Flag<T>("IsOutputTruncated"); Result<T>.AuthFailure = Flag<T>("IsAuthFailure");
        Result<T>.QuotaLimit = Flag<T>("IsQuotaLimit");
        Result<T>.NonRetryable = (Func<T, bool>)Delegate.CreateDelegate(typeof(Func<T, bool>), AccessTools.Method(client, "IsNonRetryableClientError"));
        Result<T>.Accept = (Func<long, string, T, bool>)Delegate.CreateDelegate(typeof(Func<long, string, T, bool>), AccessTools.Method(client, "AcceptWorldDiplomacyStage"));
        var value = Expression.Parameter(typeof(T), "value");
        Result<T>.RetryAfter = Expression.Lambda<Func<T, int?>>(Expression.Field(value, "RetryAfterSeconds"), value).Compile();
        var attempts = Expression.Parameter(typeof(int), "attempts");
        Result<T>.SetAttempts = Expression.Lambda<Action<T, int>>(Expression.Assign(Expression.Field(value, "AttemptsUsed"), attempts), value, attempts).Compile();
        var error = Expression.Parameter(typeof(string), "error");
        Result<T>.Error = Expression.Lambda<Func<string, T>>(Expression.MemberInit(Expression.New(typeof(T)),
            Expression.Bind(typeof(T).GetField("ErrorMessage"), error)), error).Compile();
        var delay = AccessTools.Field(client, "MaxRetryDelaySeconds");
        if (delay == null || !delay.IsLiteral || !Equals(delay.GetRawConstantValue(), 10))
            throw new InvalidOperationException("retry_delay_policy_not_supported");
    }

    private static Func<T, bool> Flag<T>(string name)
    {
        var value = Expression.Parameter(typeof(T), "value");
        return Expression.Lambda<Func<T, bool>>(Expression.Field(value, name), value).Compile();
    }

    // Replaces only Task.Run(Func<Task>) at the verified main-thread diplomacy dispatch site.
    // Preparation executes before the first network await; continuations remain asynchronous.
    public static Task StartOnOwnerThread(Func<Task> work) => work();

    public static async Task<T> CallFrozenAsync<T>(JArray messages, int maxTokens, int timeout, string source,
        long generation, int maxAttempts, CancellationToken token, Func<bool> admitRequest)
    {
        token.ThrowIfCancellationRequested();
        if (!Result<T>.Prepare(messages, maxTokens, timeout, source, generation, out var attempt, out string error))
            return Result<T>.Error(error);
        Func<bool> previous = SendAdmission.Value;
        SendAdmission.Value = admitRequest;
        try
        {
            int attempts = Math.Max(1, maxAttempts);
            for (int n = 1; ; n++)
            {
                token.ThrowIfCancellationRequested();
                T stale = Result<T>.Error("");
                if (!Result<T>.Accept(generation, "world_diplomacy_api_attempt", stale))
                { Result<T>.SetAttempts(stale, n); return stale; }
                T result = await attempt(token).ConfigureAwait(false);
                Result<T>.SetAttempts(result, n);
                if (Result<T>.Success(result) || Result<T>.BudgetDeferred(result) || Result<T>.Truncated(result)
                    || Result<T>.AuthFailure(result) || Result<T>.QuotaLimit(result) || Result<T>.NonRetryable(result) || n >= attempts)
                    return result;
                int seconds = Math.Max(1, Math.Min(10, Result<T>.RetryAfter(result) ?? n * 2));
                await Task.Delay(TimeSpan.FromSeconds(seconds), token).ConfigureAwait(false);
            }
        }
        finally { SendAdmission.Value = previous; }
    }

    internal static void SendPrefix(ref Func<bool> admitRequest)
    {
        if (admitRequest == null) admitRequest = SendAdmission.Value;
    }

    internal static IEnumerable<CodeInstruction> DispatchTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        var code = instructions.Select(i => new CodeInstruction(i)).ToList();
        var run = typeof(Task).GetMethod("Run", new[] { typeof(Func<Task>) });
        var replacements = code.Where(i => i.Calls(run)).ToList();
        if (replacements.Count != 1) throw new InvalidOperationException("dispatch_worker_shape_not_supported");
        replacements[0].opcode = OpCodes.Call;
        replacements[0].operand = AccessTools.Method(typeof(FrozenRequestPatch), nameof(StartOnOwnerThread));
        return code;
    }

    internal static IEnumerable<CodeInstruction> ClientTranspiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
    {
        if (FrozenEntry == null || original.GetParameters().Length != 8)
            throw new InvalidOperationException("retry_entry_shape_not_supported");
        for (int i = 0; i < 8; i++) yield return new CodeInstruction(OpCodes.Ldarg, (short)i);
        yield return new CodeInstruction(OpCodes.Call, FrozenEntry);
        yield return new CodeInstruction(OpCodes.Ret);
    }
}
