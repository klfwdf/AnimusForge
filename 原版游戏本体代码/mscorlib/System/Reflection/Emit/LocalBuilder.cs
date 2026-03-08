using System;
using System.Runtime.InteropServices;

namespace System.Reflection.Emit
{
	// Token: 0x02000645 RID: 1605
	[ClassInterface(ClassInterfaceType.None)]
	[ComDefaultInterface(typeof(_LocalBuilder))]
	[ComVisible(true)]
	public sealed class LocalBuilder : LocalVariableInfo, _LocalBuilder
	{
		// Token: 0x06004B0D RID: 19213 RVA: 0x0010FEE3 File Offset: 0x0010E0E3
		private LocalBuilder()
		{
		}

		// Token: 0x06004B0E RID: 19214 RVA: 0x0010FEEB File Offset: 0x0010E0EB
		internal LocalBuilder(int localIndex, Type localType, MethodInfo methodBuilder)
			: this(localIndex, localType, methodBuilder, false)
		{
		}

		// Token: 0x06004B0F RID: 19215 RVA: 0x0010FEF7 File Offset: 0x0010E0F7
		internal LocalBuilder(int localIndex, Type localType, MethodInfo methodBuilder, bool isPinned)
		{
			this.m_isPinned = isPinned;
			this.m_localIndex = localIndex;
			this.m_localType = localType;
			this.m_methodBuilder = methodBuilder;
		}

		// Token: 0x06004B10 RID: 19216 RVA: 0x0010FF1C File Offset: 0x0010E11C
		internal int GetLocalIndex()
		{
			return this.m_localIndex;
		}

		// Token: 0x06004B11 RID: 19217 RVA: 0x0010FF24 File Offset: 0x0010E124
		internal MethodInfo GetMethodBuilder()
		{
			return this.m_methodBuilder;
		}

		// Token: 0x17000BAD RID: 2989
		// (get) Token: 0x06004B12 RID: 19218 RVA: 0x0010FF2C File Offset: 0x0010E12C
		public override bool IsPinned
		{
			get
			{
				return this.m_isPinned;
			}
		}

		// Token: 0x17000BAE RID: 2990
		// (get) Token: 0x06004B13 RID: 19219 RVA: 0x0010FF34 File Offset: 0x0010E134
		public override Type LocalType
		{
			get
			{
				return this.m_localType;
			}
		}

		// Token: 0x17000BAF RID: 2991
		// (get) Token: 0x06004B14 RID: 19220 RVA: 0x0010FF3C File Offset: 0x0010E13C
		public override int LocalIndex
		{
			get
			{
				return this.m_localIndex;
			}
		}

		// Token: 0x06004B15 RID: 19221 RVA: 0x0010FF44 File Offset: 0x0010E144
		public void SetLocalSymInfo(string name)
		{
			this.SetLocalSymInfo(name, 0, 0);
		}

		// Token: 0x06004B16 RID: 19222 RVA: 0x0010FF50 File Offset: 0x0010E150
		public void SetLocalSymInfo(string name, int startOffset, int endOffset)
		{
			MethodBuilder methodBuilder = this.m_methodBuilder as MethodBuilder;
			if (methodBuilder == null)
			{
				throw new NotSupportedException();
			}
			ModuleBuilder moduleBuilder = (ModuleBuilder)methodBuilder.Module;
			if (methodBuilder.IsTypeCreated())
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_TypeHasBeenCreated"));
			}
			if (moduleBuilder.GetSymWriter() == null)
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NotADebugModule"));
			}
			SignatureHelper fieldSigHelper = SignatureHelper.GetFieldSigHelper(moduleBuilder);
			fieldSigHelper.AddArgument(this.m_localType);
			int num;
			byte[] sourceArray = fieldSigHelper.InternalGetSignature(out num);
			byte[] array = new byte[num - 1];
			Array.Copy(sourceArray, 1, array, 0, num - 1);
			int currentActiveScopeIndex = methodBuilder.GetILGenerator().m_ScopeTree.GetCurrentActiveScopeIndex();
			if (currentActiveScopeIndex == -1)
			{
				methodBuilder.m_localSymInfo.AddLocalSymInfo(name, array, this.m_localIndex, startOffset, endOffset);
				return;
			}
			methodBuilder.GetILGenerator().m_ScopeTree.AddLocalSymInfoToCurrentScope(name, array, this.m_localIndex, startOffset, endOffset);
		}

		// Token: 0x06004B17 RID: 19223 RVA: 0x00110037 File Offset: 0x0010E237
		void _LocalBuilder.GetTypeInfoCount(out uint pcTInfo)
		{
			throw new NotImplementedException();
		}

		// Token: 0x06004B18 RID: 19224 RVA: 0x0011003E File Offset: 0x0010E23E
		void _LocalBuilder.GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo)
		{
			throw new NotImplementedException();
		}

		// Token: 0x06004B19 RID: 19225 RVA: 0x00110045 File Offset: 0x0010E245
		void _LocalBuilder.GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId)
		{
			throw new NotImplementedException();
		}

		// Token: 0x06004B1A RID: 19226 RVA: 0x0011004C File Offset: 0x0010E24C
		void _LocalBuilder.Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr)
		{
			throw new NotImplementedException();
		}

		// Token: 0x04001F07 RID: 7943
		private int m_localIndex;

		// Token: 0x04001F08 RID: 7944
		private Type m_localType;

		// Token: 0x04001F09 RID: 7945
		private MethodInfo m_methodBuilder;

		// Token: 0x04001F0A RID: 7946
		private bool m_isPinned;
	}
}
