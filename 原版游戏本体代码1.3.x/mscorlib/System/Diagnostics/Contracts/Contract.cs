using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Security;

namespace System.Diagnostics.Contracts
{
	// Token: 0x02000415 RID: 1045
	[__DynamicallyInvokable]
	public static class Contract
	{
		// Token: 0x06003402 RID: 13314 RVA: 0x000C68BD File Offset: 0x000C4ABD
		[Conditional("DEBUG")]
		[Conditional("CONTRACTS_FULL")]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		[__DynamicallyInvokable]
		public static void Assume(bool condition)
		{
			if (!condition)
			{
				Contract.ReportFailure(ContractFailureKind.Assume, null, null, null);
			}
		}

		// Token: 0x06003403 RID: 13315 RVA: 0x000C68CB File Offset: 0x000C4ACB
		[Conditional("DEBUG")]
		[Conditional("CONTRACTS_FULL")]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		[__DynamicallyInvokable]
		public static void Assume(bool condition, string userMessage)
		{
			if (!condition)
			{
				Contract.ReportFailure(ContractFailureKind.Assume, userMessage, null, null);
			}
		}

		// Token: 0x06003404 RID: 13316 RVA: 0x000C68D9 File Offset: 0x000C4AD9
		[Conditional("DEBUG")]
		[Conditional("CONTRACTS_FULL")]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		[__DynamicallyInvokable]
		public static void Assert(bool condition)
		{
			if (!condition)
			{
				Contract.ReportFailure(ContractFailureKind.Assert, null, null, null);
			}
		}

		// Token: 0x06003405 RID: 13317 RVA: 0x000C68E7 File Offset: 0x000C4AE7
		[Conditional("DEBUG")]
		[Conditional("CONTRACTS_FULL")]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		[__DynamicallyInvokable]
		public static void Assert(bool condition, string userMessage)
		{
			if (!condition)
			{
				Contract.ReportFailure(ContractFailureKind.Assert, userMessage, null, null);
			}
		}

		// Token: 0x06003406 RID: 13318 RVA: 0x000C68F5 File Offset: 0x000C4AF5
		[Conditional("CONTRACTS_FULL")]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		[__DynamicallyInvokable]
		public static void Requires(bool condition)
		{
			Contract.AssertMustUseRewriter(ContractFailureKind.Precondition, "Requires");
		}

		// Token: 0x06003407 RID: 13319 RVA: 0x000C6902 File Offset: 0x000C4B02
		[Conditional("CONTRACTS_FULL")]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		[__DynamicallyInvokable]
		public static void Requires(bool condition, string userMessage)
		{
			Contract.AssertMustUseRewriter(ContractFailureKind.Precondition, "Requires");
		}

		// Token: 0x06003408 RID: 13320 RVA: 0x000C690F File Offset: 0x000C4B0F
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		[__DynamicallyInvokable]
		public static void Requires<TException>(bool condition) where TException : Exception
		{
			Contract.AssertMustUseRewriter(ContractFailureKind.Precondition, "Requires<TException>");
		}

		// Token: 0x06003409 RID: 13321 RVA: 0x000C691C File Offset: 0x000C4B1C
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		[__DynamicallyInvokable]
		public static void Requires<TException>(bool condition, string userMessage) where TException : Exception
		{
			Contract.AssertMustUseRewriter(ContractFailureKind.Precondition, "Requires<TException>");
		}

		// Token: 0x0600340A RID: 13322 RVA: 0x000C6929 File Offset: 0x000C4B29
		[Conditional("CONTRACTS_FULL")]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		[__DynamicallyInvokable]
		public static void Ensures(bool condition)
		{
			Contract.AssertMustUseRewriter(ContractFailureKind.Postcondition, "Ensures");
		}

		// Token: 0x0600340B RID: 13323 RVA: 0x000C6936 File Offset: 0x000C4B36
		[Conditional("CONTRACTS_FULL")]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		[__DynamicallyInvokable]
		public static void Ensures(bool condition, string userMessage)
		{
			Contract.AssertMustUseRewriter(ContractFailureKind.Postcondition, "Ensures");
		}

		// Token: 0x0600340C RID: 13324 RVA: 0x000C6943 File Offset: 0x000C4B43
		[Conditional("CONTRACTS_FULL")]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		[__DynamicallyInvokable]
		public static void EnsuresOnThrow<TException>(bool condition) where TException : Exception
		{
			Contract.AssertMustUseRewriter(ContractFailureKind.PostconditionOnException, "EnsuresOnThrow");
		}

		// Token: 0x0600340D RID: 13325 RVA: 0x000C6950 File Offset: 0x000C4B50
		[Conditional("CONTRACTS_FULL")]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		[__DynamicallyInvokable]
		public static void EnsuresOnThrow<TException>(bool condition, string userMessage) where TException : Exception
		{
			Contract.AssertMustUseRewriter(ContractFailureKind.PostconditionOnException, "EnsuresOnThrow");
		}

		// Token: 0x0600340E RID: 13326 RVA: 0x000C6960 File Offset: 0x000C4B60
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[__DynamicallyInvokable]
		public static T Result<T>()
		{
			return default(T);
		}

		// Token: 0x0600340F RID: 13327 RVA: 0x000C6976 File Offset: 0x000C4B76
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[__DynamicallyInvokable]
		public static T ValueAtReturn<T>(out T value)
		{
			value = default(T);
			return value;
		}

		// Token: 0x06003410 RID: 13328 RVA: 0x000C6988 File Offset: 0x000C4B88
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[__DynamicallyInvokable]
		public static T OldValue<T>(T value)
		{
			return default(T);
		}

		// Token: 0x06003411 RID: 13329 RVA: 0x000C699E File Offset: 0x000C4B9E
		[Conditional("CONTRACTS_FULL")]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		[__DynamicallyInvokable]
		public static void Invariant(bool condition)
		{
			Contract.AssertMustUseRewriter(ContractFailureKind.Invariant, "Invariant");
		}

		// Token: 0x06003412 RID: 13330 RVA: 0x000C69AB File Offset: 0x000C4BAB
		[Conditional("CONTRACTS_FULL")]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		[__DynamicallyInvokable]
		public static void Invariant(bool condition, string userMessage)
		{
			Contract.AssertMustUseRewriter(ContractFailureKind.Invariant, "Invariant");
		}

		// Token: 0x06003413 RID: 13331 RVA: 0x000C69B8 File Offset: 0x000C4BB8
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		[__DynamicallyInvokable]
		public static bool ForAll(int fromInclusive, int toExclusive, Predicate<int> predicate)
		{
			if (fromInclusive > toExclusive)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_ToExclusiveLessThanFromExclusive"));
			}
			if (predicate == null)
			{
				throw new ArgumentNullException("predicate");
			}
			for (int i = fromInclusive; i < toExclusive; i++)
			{
				if (!predicate(i))
				{
					return false;
				}
			}
			return true;
		}

		// Token: 0x06003414 RID: 13332 RVA: 0x000C6A00 File Offset: 0x000C4C00
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		[__DynamicallyInvokable]
		public static bool ForAll<T>(IEnumerable<T> collection, Predicate<T> predicate)
		{
			if (collection == null)
			{
				throw new ArgumentNullException("collection");
			}
			if (predicate == null)
			{
				throw new ArgumentNullException("predicate");
			}
			foreach (T obj in collection)
			{
				if (!predicate(obj))
				{
					return false;
				}
			}
			return true;
		}

		// Token: 0x06003415 RID: 13333 RVA: 0x000C6A70 File Offset: 0x000C4C70
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		[__DynamicallyInvokable]
		public static bool Exists(int fromInclusive, int toExclusive, Predicate<int> predicate)
		{
			if (fromInclusive > toExclusive)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_ToExclusiveLessThanFromExclusive"));
			}
			if (predicate == null)
			{
				throw new ArgumentNullException("predicate");
			}
			for (int i = fromInclusive; i < toExclusive; i++)
			{
				if (predicate(i))
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x06003416 RID: 13334 RVA: 0x000C6AB8 File Offset: 0x000C4CB8
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		[__DynamicallyInvokable]
		public static bool Exists<T>(IEnumerable<T> collection, Predicate<T> predicate)
		{
			if (collection == null)
			{
				throw new ArgumentNullException("collection");
			}
			if (predicate == null)
			{
				throw new ArgumentNullException("predicate");
			}
			foreach (T obj in collection)
			{
				if (predicate(obj))
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x06003417 RID: 13335 RVA: 0x000C6B28 File Offset: 0x000C4D28
		[Conditional("CONTRACTS_FULL")]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[__DynamicallyInvokable]
		public static void EndContractBlock()
		{
		}

		// Token: 0x06003418 RID: 13336 RVA: 0x000C6B2C File Offset: 0x000C4D2C
		[DebuggerNonUserCode]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		private static void ReportFailure(ContractFailureKind failureKind, string userMessage, string conditionText, Exception innerException)
		{
			if (failureKind < ContractFailureKind.Precondition || failureKind > ContractFailureKind.Assume)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_EnumIllegalVal", new object[] { failureKind }), "failureKind");
			}
			string text = ContractHelper.RaiseContractFailedEvent(failureKind, userMessage, conditionText, innerException);
			if (text == null)
			{
				return;
			}
			ContractHelper.TriggerFailure(failureKind, text, userMessage, conditionText, innerException);
		}

		// Token: 0x06003419 RID: 13337 RVA: 0x000C6B80 File Offset: 0x000C4D80
		[SecuritySafeCritical]
		private static void AssertMustUseRewriter(ContractFailureKind kind, string contractKind)
		{
			if (Contract._assertingMustUseRewriter)
			{
				System.Diagnostics.Assert.Fail("Asserting that we must use the rewriter went reentrant.", "Didn't rewrite this mscorlib?");
			}
			Contract._assertingMustUseRewriter = true;
			Assembly assembly = typeof(Contract).Assembly;
			StackTrace stackTrace = new StackTrace();
			Assembly assembly2 = null;
			for (int i = 0; i < stackTrace.FrameCount; i++)
			{
				Assembly assembly3 = stackTrace.GetFrame(i).GetMethod().DeclaringType.Assembly;
				if (assembly3 != assembly)
				{
					assembly2 = assembly3;
					break;
				}
			}
			if (assembly2 == null)
			{
				assembly2 = assembly;
			}
			string name = assembly2.GetName().Name;
			ContractHelper.TriggerFailure(kind, Environment.GetResourceString("MustUseCCRewrite", new object[] { contractKind, name }), null, null, null);
			Contract._assertingMustUseRewriter = false;
		}

		// Token: 0x14000014 RID: 20
		// (add) Token: 0x0600341A RID: 13338 RVA: 0x000C6C3C File Offset: 0x000C4E3C
		// (remove) Token: 0x0600341B RID: 13339 RVA: 0x000C6C44 File Offset: 0x000C4E44
		[__DynamicallyInvokable]
		public static event EventHandler<ContractFailedEventArgs> ContractFailed
		{
			[SecurityCritical]
			[__DynamicallyInvokable]
			add
			{
				ContractHelper.InternalContractFailed += value;
			}
			[SecurityCritical]
			[__DynamicallyInvokable]
			remove
			{
				ContractHelper.InternalContractFailed -= value;
			}
		}

		// Token: 0x04001710 RID: 5904
		[ThreadStatic]
		private static bool _assertingMustUseRewriter;
	}
}
