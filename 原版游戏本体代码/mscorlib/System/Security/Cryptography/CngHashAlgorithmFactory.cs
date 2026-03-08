using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Permissions;

namespace System.Security.Cryptography
{
	// Token: 0x02000251 RID: 593
	internal sealed class CngHashAlgorithmFactory<THashAlgorithm> where THashAlgorithm : HashAlgorithm
	{
		// Token: 0x0600210C RID: 8460 RVA: 0x000732EC File Offset: 0x000714EC
		internal CngHashAlgorithmFactory(string fullTypeName)
		{
			this._fullTypeName = fullTypeName;
		}

		// Token: 0x0600210D RID: 8461 RVA: 0x000732FC File Offset: 0x000714FC
		internal THashAlgorithm CreateInstance()
		{
			THashAlgorithm thashAlgorithm = default(THashAlgorithm);
			if (!this._innerFactoryInitialized && !AppDomain.IsStillInEarlyInit())
			{
				this._innerFactory = CngHashAlgorithmFactory<THashAlgorithm>.FactoryBuilder.SafeCreateFactory(this._fullTypeName, out this._mostRecentException);
				this._innerFactoryInitialized = true;
			}
			if (this._innerFactoryInitialized)
			{
				try
				{
					try
					{
						Func<THashAlgorithm> innerFactory = this._innerFactory;
						thashAlgorithm = ((innerFactory != null) ? innerFactory() : default(THashAlgorithm));
					}
					catch (Exception mostRecentException)
					{
						this._mostRecentException = mostRecentException;
					}
				}
				catch
				{
				}
			}
			if (thashAlgorithm == null)
			{
				thashAlgorithm = (THashAlgorithm)((object)CryptoConfig.CreateFromName(this._fullTypeName));
			}
			return thashAlgorithm;
		}

		// Token: 0x04000BF1 RID: 3057
		private readonly string _fullTypeName;

		// Token: 0x04000BF2 RID: 3058
		private Func<THashAlgorithm> _innerFactory;

		// Token: 0x04000BF3 RID: 3059
		private volatile bool _innerFactoryInitialized;

		// Token: 0x04000BF4 RID: 3060
		private Exception _mostRecentException;

		// Token: 0x02000B42 RID: 2882
		private static class FactoryBuilder
		{
			// Token: 0x06006B89 RID: 27529 RVA: 0x00173AED File Offset: 0x00171CED
			[MethodImpl(MethodImplOptions.NoInlining)]
			internal static Func<THashAlgorithm> SafeCreateFactory(string fullTypeName, out Exception exception)
			{
				return CngHashAlgorithmFactory<THashAlgorithm>.FactoryBuilder.Impl.SafeCreateFactory(fullTypeName, out exception);
			}

			// Token: 0x02000D01 RID: 3329
			private static class Impl
			{
				// Token: 0x060071F2 RID: 29170 RVA: 0x00188A64 File Offset: 0x00186C64
				internal static Func<THashAlgorithm> SafeCreateFactory(string fullTypeName, out Exception exception)
				{
					exception = null;
					try
					{
						try
						{
							Func<THashAlgorithm> func = CngHashAlgorithmFactory<THashAlgorithm>.FactoryBuilder.Impl.DangerousFetchFactoryFromSystemCore(fullTypeName);
							THashAlgorithm thashAlgorithm = func();
							if (thashAlgorithm != null)
							{
								thashAlgorithm.Dispose();
								return func;
							}
						}
						catch (Exception ex)
						{
							exception = ex;
						}
					}
					catch
					{
					}
					return null;
				}

				// Token: 0x060071F3 RID: 29171 RVA: 0x00188AC8 File Offset: 0x00186CC8
				[SecuritySafeCritical]
				[ReflectionPermission(SecurityAction.Assert, Flags = ReflectionPermissionFlag.AllFlags)]
				private static Func<THashAlgorithm> DangerousFetchFactoryFromSystemCore(string fullTypeName)
				{
					Assembly assembly = Assembly.Load("System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
					Type type = assembly.GetType(fullTypeName + "Factory");
					MethodInfo method = type.GetMethod("CreateNew", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
					return (Func<THashAlgorithm>)method.CreateDelegate(typeof(Func<THashAlgorithm>));
				}
			}
		}
	}
}
