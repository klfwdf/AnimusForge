using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Security;
using System.Security.Permissions;
using System.Threading;

namespace System.Diagnostics
{
	// Token: 0x020003F7 RID: 1015
	[Serializable]
	internal class StackFrameHelper : IDisposable
	{
		// Token: 0x0600335D RID: 13149 RVA: 0x000C5490 File Offset: 0x000C3690
		public StackFrameHelper(Thread target)
		{
			this.targetThread = target;
			this.rgMethodBase = null;
			this.rgMethodHandle = null;
			this.rgiMethodToken = null;
			this.rgiOffset = null;
			this.rgiILOffset = null;
			this.rgAssemblyPath = null;
			this.rgLoadedPeAddress = null;
			this.rgiLoadedPeSize = null;
			this.rgInMemoryPdbAddress = null;
			this.rgiInMemoryPdbSize = null;
			this.dynamicMethods = null;
			this.rgFilename = null;
			this.rgiLineNumber = null;
			this.rgiColumnNumber = null;
			this.rgiLastFrameFromForeignExceptionStackTrace = null;
			this.iFrameCount = 0;
		}

		// Token: 0x0600335E RID: 13150 RVA: 0x000C551C File Offset: 0x000C371C
		[SecuritySafeCritical]
		internal void InitializeSourceInfo(int iSkip, bool fNeedFileInfo, Exception exception)
		{
			StackTrace.GetStackFramesInternal(this, iSkip, fNeedFileInfo, exception);
			if (!fNeedFileInfo)
			{
				return;
			}
			if (!RuntimeFeature.IsSupported("PortablePdb"))
			{
				return;
			}
			if (StackFrameHelper.t_reentrancy > 0)
			{
				return;
			}
			StackFrameHelper.t_reentrancy++;
			try
			{
				if (!CodeAccessSecurityEngine.QuickCheckForAllDemands())
				{
					new ReflectionPermission(ReflectionPermissionFlag.MemberAccess).Assert();
					new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Assert();
				}
				if (StackFrameHelper.s_getSourceLineInfo == null)
				{
					Type type = Type.GetType("System.Diagnostics.StackTraceSymbols, System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", false);
					if (type == null)
					{
						return;
					}
					MethodInfo method = type.GetMethod("GetSourceLineInfoWithoutCasAssert", new Type[]
					{
						typeof(string),
						typeof(IntPtr),
						typeof(int),
						typeof(IntPtr),
						typeof(int),
						typeof(int),
						typeof(int),
						typeof(string).MakeByRefType(),
						typeof(int).MakeByRefType(),
						typeof(int).MakeByRefType()
					});
					if (method == null)
					{
						method = type.GetMethod("GetSourceLineInfo", new Type[]
						{
							typeof(string),
							typeof(IntPtr),
							typeof(int),
							typeof(IntPtr),
							typeof(int),
							typeof(int),
							typeof(int),
							typeof(string).MakeByRefType(),
							typeof(int).MakeByRefType(),
							typeof(int).MakeByRefType()
						});
					}
					if (method == null)
					{
						return;
					}
					object target = Activator.CreateInstance(type);
					StackFrameHelper.GetSourceLineInfoDelegate value = (StackFrameHelper.GetSourceLineInfoDelegate)method.CreateDelegate(typeof(StackFrameHelper.GetSourceLineInfoDelegate), target);
					Interlocked.CompareExchange<StackFrameHelper.GetSourceLineInfoDelegate>(ref StackFrameHelper.s_getSourceLineInfo, value, null);
				}
				for (int i = 0; i < this.iFrameCount; i++)
				{
					if (this.rgiMethodToken[i] != 0)
					{
						StackFrameHelper.s_getSourceLineInfo(this.rgAssemblyPath[i], this.rgLoadedPeAddress[i], this.rgiLoadedPeSize[i], this.rgInMemoryPdbAddress[i], this.rgiInMemoryPdbSize[i], this.rgiMethodToken[i], this.rgiILOffset[i], out this.rgFilename[i], out this.rgiLineNumber[i], out this.rgiColumnNumber[i]);
					}
				}
			}
			catch
			{
			}
			finally
			{
				StackFrameHelper.t_reentrancy--;
			}
		}

		// Token: 0x0600335F RID: 13151 RVA: 0x000C5804 File Offset: 0x000C3A04
		void IDisposable.Dispose()
		{
		}

		// Token: 0x06003360 RID: 13152 RVA: 0x000C5808 File Offset: 0x000C3A08
		[SecuritySafeCritical]
		public virtual MethodBase GetMethodBase(int i)
		{
			IntPtr methodHandleValue = this.rgMethodHandle[i];
			if (methodHandleValue.IsNull())
			{
				return null;
			}
			IRuntimeMethodInfo typicalMethodDefinition = RuntimeMethodHandle.GetTypicalMethodDefinition(new RuntimeMethodInfoStub(methodHandleValue, this));
			return RuntimeType.GetMethodBase(typicalMethodDefinition);
		}

		// Token: 0x06003361 RID: 13153 RVA: 0x000C583C File Offset: 0x000C3A3C
		public virtual int GetOffset(int i)
		{
			return this.rgiOffset[i];
		}

		// Token: 0x06003362 RID: 13154 RVA: 0x000C5846 File Offset: 0x000C3A46
		public virtual int GetILOffset(int i)
		{
			return this.rgiILOffset[i];
		}

		// Token: 0x06003363 RID: 13155 RVA: 0x000C5850 File Offset: 0x000C3A50
		public virtual string GetFilename(int i)
		{
			if (this.rgFilename != null)
			{
				return this.rgFilename[i];
			}
			return null;
		}

		// Token: 0x06003364 RID: 13156 RVA: 0x000C5864 File Offset: 0x000C3A64
		public virtual int GetLineNumber(int i)
		{
			if (this.rgiLineNumber != null)
			{
				return this.rgiLineNumber[i];
			}
			return 0;
		}

		// Token: 0x06003365 RID: 13157 RVA: 0x000C5878 File Offset: 0x000C3A78
		public virtual int GetColumnNumber(int i)
		{
			if (this.rgiColumnNumber != null)
			{
				return this.rgiColumnNumber[i];
			}
			return 0;
		}

		// Token: 0x06003366 RID: 13158 RVA: 0x000C588C File Offset: 0x000C3A8C
		public virtual bool IsLastFrameFromForeignExceptionStackTrace(int i)
		{
			return this.rgiLastFrameFromForeignExceptionStackTrace != null && this.rgiLastFrameFromForeignExceptionStackTrace[i];
		}

		// Token: 0x06003367 RID: 13159 RVA: 0x000C58A0 File Offset: 0x000C3AA0
		public virtual int GetNumberOfFrames()
		{
			return this.iFrameCount;
		}

		// Token: 0x06003368 RID: 13160 RVA: 0x000C58A8 File Offset: 0x000C3AA8
		public virtual void SetNumberOfFrames(int i)
		{
			this.iFrameCount = i;
		}

		// Token: 0x06003369 RID: 13161 RVA: 0x000C58B4 File Offset: 0x000C3AB4
		[OnSerializing]
		[SecuritySafeCritical]
		private void OnSerializing(StreamingContext context)
		{
			this.rgMethodBase = ((this.rgMethodHandle == null) ? null : new MethodBase[this.rgMethodHandle.Length]);
			if (this.rgMethodHandle != null)
			{
				for (int i = 0; i < this.rgMethodHandle.Length; i++)
				{
					if (!this.rgMethodHandle[i].IsNull())
					{
						this.rgMethodBase[i] = RuntimeType.GetMethodBase(new RuntimeMethodInfoStub(this.rgMethodHandle[i], this));
					}
				}
			}
		}

		// Token: 0x0600336A RID: 13162 RVA: 0x000C5928 File Offset: 0x000C3B28
		[OnSerialized]
		private void OnSerialized(StreamingContext context)
		{
			this.rgMethodBase = null;
		}

		// Token: 0x0600336B RID: 13163 RVA: 0x000C5934 File Offset: 0x000C3B34
		[OnDeserialized]
		[SecuritySafeCritical]
		private void OnDeserialized(StreamingContext context)
		{
			this.rgMethodHandle = ((this.rgMethodBase == null) ? null : new IntPtr[this.rgMethodBase.Length]);
			if (this.rgMethodBase != null)
			{
				for (int i = 0; i < this.rgMethodBase.Length; i++)
				{
					if (this.rgMethodBase[i] != null)
					{
						this.rgMethodHandle[i] = this.rgMethodBase[i].MethodHandle.Value;
					}
				}
			}
			this.rgMethodBase = null;
		}

		// Token: 0x040016CF RID: 5839
		[NonSerialized]
		private Thread targetThread;

		// Token: 0x040016D0 RID: 5840
		private int[] rgiOffset;

		// Token: 0x040016D1 RID: 5841
		private int[] rgiILOffset;

		// Token: 0x040016D2 RID: 5842
		private MethodBase[] rgMethodBase;

		// Token: 0x040016D3 RID: 5843
		private object dynamicMethods;

		// Token: 0x040016D4 RID: 5844
		[NonSerialized]
		private IntPtr[] rgMethodHandle;

		// Token: 0x040016D5 RID: 5845
		private string[] rgAssemblyPath;

		// Token: 0x040016D6 RID: 5846
		private IntPtr[] rgLoadedPeAddress;

		// Token: 0x040016D7 RID: 5847
		private int[] rgiLoadedPeSize;

		// Token: 0x040016D8 RID: 5848
		private IntPtr[] rgInMemoryPdbAddress;

		// Token: 0x040016D9 RID: 5849
		private int[] rgiInMemoryPdbSize;

		// Token: 0x040016DA RID: 5850
		private int[] rgiMethodToken;

		// Token: 0x040016DB RID: 5851
		private string[] rgFilename;

		// Token: 0x040016DC RID: 5852
		private int[] rgiLineNumber;

		// Token: 0x040016DD RID: 5853
		private int[] rgiColumnNumber;

		// Token: 0x040016DE RID: 5854
		[OptionalField]
		private bool[] rgiLastFrameFromForeignExceptionStackTrace;

		// Token: 0x040016DF RID: 5855
		private int iFrameCount;

		// Token: 0x040016E0 RID: 5856
		private static StackFrameHelper.GetSourceLineInfoDelegate s_getSourceLineInfo;

		// Token: 0x040016E1 RID: 5857
		[ThreadStatic]
		private static int t_reentrancy;

		// Token: 0x02000B83 RID: 2947
		// (Invoke) Token: 0x06006C5F RID: 27743
		private delegate void GetSourceLineInfoDelegate(string assemblyPath, IntPtr loadedPeAddress, int loadedPeSize, IntPtr inMemoryPdbAddress, int inMemoryPdbSize, int methodToken, int ilOffset, out string sourceFile, out int sourceLine, out int sourceColumn);
	}
}
