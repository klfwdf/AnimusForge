using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Reflection.Emit
{
	// Token: 0x02000633 RID: 1587
	[ComVisible(true)]
	public class DynamicILInfo
	{
		// Token: 0x06004A0B RID: 18955 RVA: 0x0010C234 File Offset: 0x0010A434
		internal DynamicILInfo(DynamicScope scope, DynamicMethod method, byte[] methodSignature)
		{
			this.m_method = method;
			this.m_scope = scope;
			this.m_methodSignature = this.m_scope.GetTokenFor(methodSignature);
			this.m_exceptions = EmptyArray<byte>.Value;
			this.m_code = EmptyArray<byte>.Value;
			this.m_localSignature = EmptyArray<byte>.Value;
		}

		// Token: 0x06004A0C RID: 18956 RVA: 0x0010C288 File Offset: 0x0010A488
		[SecurityCritical]
		internal void GetCallableMethod(RuntimeModule module, DynamicMethod dm)
		{
			dm.m_methodHandle = ModuleHandle.GetDynamicMethod(dm, module, this.m_method.Name, (byte[])this.m_scope[this.m_methodSignature], new DynamicResolver(this));
		}

		// Token: 0x17000B8A RID: 2954
		// (get) Token: 0x06004A0D RID: 18957 RVA: 0x0010C2BE File Offset: 0x0010A4BE
		internal byte[] LocalSignature
		{
			get
			{
				if (this.m_localSignature == null)
				{
					this.m_localSignature = SignatureHelper.GetLocalVarSigHelper().InternalGetSignatureArray();
				}
				return this.m_localSignature;
			}
		}

		// Token: 0x17000B8B RID: 2955
		// (get) Token: 0x06004A0E RID: 18958 RVA: 0x0010C2DE File Offset: 0x0010A4DE
		internal byte[] Exceptions
		{
			get
			{
				return this.m_exceptions;
			}
		}

		// Token: 0x17000B8C RID: 2956
		// (get) Token: 0x06004A0F RID: 18959 RVA: 0x0010C2E6 File Offset: 0x0010A4E6
		internal byte[] Code
		{
			get
			{
				return this.m_code;
			}
		}

		// Token: 0x17000B8D RID: 2957
		// (get) Token: 0x06004A10 RID: 18960 RVA: 0x0010C2EE File Offset: 0x0010A4EE
		internal int MaxStackSize
		{
			get
			{
				return this.m_maxStackSize;
			}
		}

		// Token: 0x17000B8E RID: 2958
		// (get) Token: 0x06004A11 RID: 18961 RVA: 0x0010C2F6 File Offset: 0x0010A4F6
		public DynamicMethod DynamicMethod
		{
			get
			{
				return this.m_method;
			}
		}

		// Token: 0x17000B8F RID: 2959
		// (get) Token: 0x06004A12 RID: 18962 RVA: 0x0010C2FE File Offset: 0x0010A4FE
		internal DynamicScope DynamicScope
		{
			get
			{
				return this.m_scope;
			}
		}

		// Token: 0x06004A13 RID: 18963 RVA: 0x0010C306 File Offset: 0x0010A506
		public void SetCode(byte[] code, int maxStackSize)
		{
			this.m_code = ((code != null) ? ((byte[])code.Clone()) : EmptyArray<byte>.Value);
			this.m_maxStackSize = maxStackSize;
		}

		// Token: 0x06004A14 RID: 18964 RVA: 0x0010C32C File Offset: 0x0010A52C
		[SecurityCritical]
		[CLSCompliant(false)]
		public unsafe void SetCode(byte* code, int codeSize, int maxStackSize)
		{
			if (codeSize < 0)
			{
				throw new ArgumentOutOfRangeException("codeSize", Environment.GetResourceString("ArgumentOutOfRange_GenericPositive"));
			}
			if (codeSize > 0 && code == null)
			{
				throw new ArgumentNullException("code");
			}
			this.m_code = new byte[codeSize];
			for (int i = 0; i < codeSize; i++)
			{
				this.m_code[i] = *code;
				code++;
			}
			this.m_maxStackSize = maxStackSize;
		}

		// Token: 0x06004A15 RID: 18965 RVA: 0x0010C394 File Offset: 0x0010A594
		public void SetExceptions(byte[] exceptions)
		{
			this.m_exceptions = ((exceptions != null) ? ((byte[])exceptions.Clone()) : EmptyArray<byte>.Value);
		}

		// Token: 0x06004A16 RID: 18966 RVA: 0x0010C3B4 File Offset: 0x0010A5B4
		[SecurityCritical]
		[CLSCompliant(false)]
		public unsafe void SetExceptions(byte* exceptions, int exceptionsSize)
		{
			if (exceptionsSize < 0)
			{
				throw new ArgumentOutOfRangeException("exceptionsSize", Environment.GetResourceString("ArgumentOutOfRange_GenericPositive"));
			}
			if (exceptionsSize > 0 && exceptions == null)
			{
				throw new ArgumentNullException("exceptions");
			}
			this.m_exceptions = new byte[exceptionsSize];
			for (int i = 0; i < exceptionsSize; i++)
			{
				this.m_exceptions[i] = *exceptions;
				exceptions++;
			}
		}

		// Token: 0x06004A17 RID: 18967 RVA: 0x0010C415 File Offset: 0x0010A615
		public void SetLocalSignature(byte[] localSignature)
		{
			this.m_localSignature = ((localSignature != null) ? ((byte[])localSignature.Clone()) : EmptyArray<byte>.Value);
		}

		// Token: 0x06004A18 RID: 18968 RVA: 0x0010C434 File Offset: 0x0010A634
		[SecurityCritical]
		[CLSCompliant(false)]
		public unsafe void SetLocalSignature(byte* localSignature, int signatureSize)
		{
			if (signatureSize < 0)
			{
				throw new ArgumentOutOfRangeException("signatureSize", Environment.GetResourceString("ArgumentOutOfRange_GenericPositive"));
			}
			if (signatureSize > 0 && localSignature == null)
			{
				throw new ArgumentNullException("localSignature");
			}
			this.m_localSignature = new byte[signatureSize];
			for (int i = 0; i < signatureSize; i++)
			{
				this.m_localSignature[i] = *localSignature;
				localSignature++;
			}
		}

		// Token: 0x06004A19 RID: 18969 RVA: 0x0010C495 File Offset: 0x0010A695
		[SecuritySafeCritical]
		public int GetTokenFor(RuntimeMethodHandle method)
		{
			return this.DynamicScope.GetTokenFor(method);
		}

		// Token: 0x06004A1A RID: 18970 RVA: 0x0010C4A3 File Offset: 0x0010A6A3
		public int GetTokenFor(DynamicMethod method)
		{
			return this.DynamicScope.GetTokenFor(method);
		}

		// Token: 0x06004A1B RID: 18971 RVA: 0x0010C4B1 File Offset: 0x0010A6B1
		public int GetTokenFor(RuntimeMethodHandle method, RuntimeTypeHandle contextType)
		{
			return this.DynamicScope.GetTokenFor(method, contextType);
		}

		// Token: 0x06004A1C RID: 18972 RVA: 0x0010C4C0 File Offset: 0x0010A6C0
		public int GetTokenFor(RuntimeFieldHandle field)
		{
			return this.DynamicScope.GetTokenFor(field);
		}

		// Token: 0x06004A1D RID: 18973 RVA: 0x0010C4CE File Offset: 0x0010A6CE
		public int GetTokenFor(RuntimeFieldHandle field, RuntimeTypeHandle contextType)
		{
			return this.DynamicScope.GetTokenFor(field, contextType);
		}

		// Token: 0x06004A1E RID: 18974 RVA: 0x0010C4DD File Offset: 0x0010A6DD
		public int GetTokenFor(RuntimeTypeHandle type)
		{
			return this.DynamicScope.GetTokenFor(type);
		}

		// Token: 0x06004A1F RID: 18975 RVA: 0x0010C4EB File Offset: 0x0010A6EB
		public int GetTokenFor(string literal)
		{
			return this.DynamicScope.GetTokenFor(literal);
		}

		// Token: 0x06004A20 RID: 18976 RVA: 0x0010C4F9 File Offset: 0x0010A6F9
		public int GetTokenFor(byte[] signature)
		{
			return this.DynamicScope.GetTokenFor(signature);
		}

		// Token: 0x04001E8F RID: 7823
		private DynamicMethod m_method;

		// Token: 0x04001E90 RID: 7824
		private DynamicScope m_scope;

		// Token: 0x04001E91 RID: 7825
		private byte[] m_exceptions;

		// Token: 0x04001E92 RID: 7826
		private byte[] m_code;

		// Token: 0x04001E93 RID: 7827
		private byte[] m_localSignature;

		// Token: 0x04001E94 RID: 7828
		private int m_maxStackSize;

		// Token: 0x04001E95 RID: 7829
		private int m_methodSignature;
	}
}
