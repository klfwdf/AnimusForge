using System;

namespace System.Threading.Tasks
{
	// Token: 0x0200054F RID: 1359
	internal static class GenericDelegateCache<TAntecedentResult, TResult>
	{
		// Token: 0x04001AC7 RID: 6855
		internal static Func<Task<Task>, object, TResult> CWAnyFuncDelegate = delegate(Task<Task> wrappedWinner, object state)
		{
			Func<Task<TAntecedentResult>, TResult> func = (Func<Task<TAntecedentResult>, TResult>)state;
			Task<TAntecedentResult> arg = (Task<TAntecedentResult>)wrappedWinner.Result;
			return func(arg);
		};

		// Token: 0x04001AC8 RID: 6856
		internal static Func<Task<Task>, object, TResult> CWAnyActionDelegate = delegate(Task<Task> wrappedWinner, object state)
		{
			Action<Task<TAntecedentResult>> action = (Action<Task<TAntecedentResult>>)state;
			Task<TAntecedentResult> obj = (Task<TAntecedentResult>)wrappedWinner.Result;
			action(obj);
			return default(TResult);
		};

		// Token: 0x04001AC9 RID: 6857
		internal static Func<Task<Task<TAntecedentResult>[]>, object, TResult> CWAllFuncDelegate = delegate(Task<Task<TAntecedentResult>[]> wrappedAntecedents, object state)
		{
			wrappedAntecedents.NotifyDebuggerOfWaitCompletionIfNecessary();
			Func<Task<TAntecedentResult>[], TResult> func = (Func<Task<TAntecedentResult>[], TResult>)state;
			return func(wrappedAntecedents.Result);
		};

		// Token: 0x04001ACA RID: 6858
		internal static Func<Task<Task<TAntecedentResult>[]>, object, TResult> CWAllActionDelegate = delegate(Task<Task<TAntecedentResult>[]> wrappedAntecedents, object state)
		{
			wrappedAntecedents.NotifyDebuggerOfWaitCompletionIfNecessary();
			Action<Task<TAntecedentResult>[]> action = (Action<Task<TAntecedentResult>[]>)state;
			action(wrappedAntecedents.Result);
			return default(TResult);
		};
	}
}
