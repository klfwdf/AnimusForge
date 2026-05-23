using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;

namespace System.Reflection.Emit
{
	// Token: 0x02000639 RID: 1593
	[ClassInterface(ClassInterfaceType.None)]
	[ComDefaultInterface(typeof(_EventBuilder))]
	[ComVisible(true)]
	[HostProtection(SecurityAction.LinkDemand, MayLeakOnAbort = true)]
	public sealed class EventBuilder : _EventBuilder
	{
		// Token: 0x06004A63 RID: 19043 RVA: 0x0010D427 File Offset: 0x0010B627
		private EventBuilder()
		{
		}

		// Token: 0x06004A64 RID: 19044 RVA: 0x0010D42F File Offset: 0x0010B62F
		internal EventBuilder(ModuleBuilder mod, string name, EventAttributes attr, TypeBuilder type, EventToken evToken)
		{
			this.m_name = name;
			this.m_module = mod;
			this.m_attributes = attr;
			this.m_evToken = evToken;
			this.m_type = type;
		}

		// Token: 0x06004A65 RID: 19045 RVA: 0x0010D45C File Offset: 0x0010B65C
		public EventToken GetEventToken()
		{
			return this.m_evToken;
		}

		// Token: 0x06004A66 RID: 19046 RVA: 0x0010D464 File Offset: 0x0010B664
		[SecurityCritical]
		private void SetMethodSemantics(MethodBuilder mdBuilder, MethodSemanticsAttributes semantics)
		{
			if (mdBuilder == null)
			{
				throw new ArgumentNullException("mdBuilder");
			}
			this.m_type.ThrowIfCreated();
			TypeBuilder.DefineMethodSemantics(this.m_module.GetNativeHandle(), this.m_evToken.Token, semantics, mdBuilder.GetToken().Token);
		}

		// Token: 0x06004A67 RID: 19047 RVA: 0x0010D4BA File Offset: 0x0010B6BA
		[SecuritySafeCritical]
		public void SetAddOnMethod(MethodBuilder mdBuilder)
		{
			this.SetMethodSemantics(mdBuilder, MethodSemanticsAttributes.AddOn);
		}

		// Token: 0x06004A68 RID: 19048 RVA: 0x0010D4C4 File Offset: 0x0010B6C4
		[SecuritySafeCritical]
		public void SetRemoveOnMethod(MethodBuilder mdBuilder)
		{
			this.SetMethodSemantics(mdBuilder, MethodSemanticsAttributes.RemoveOn);
		}

		// Token: 0x06004A69 RID: 19049 RVA: 0x0010D4CF File Offset: 0x0010B6CF
		[SecuritySafeCritical]
		public void SetRaiseMethod(MethodBuilder mdBuilder)
		{
			this.SetMethodSemantics(mdBuilder, MethodSemanticsAttributes.Fire);
		}

		// Token: 0x06004A6A RID: 19050 RVA: 0x0010D4DA File Offset: 0x0010B6DA
		[SecuritySafeCritical]
		public void AddOtherMethod(MethodBuilder mdBuilder)
		{
			this.SetMethodSemantics(mdBuilder, MethodSemanticsAttributes.Other);
		}

		// Token: 0x06004A6B RID: 19051 RVA: 0x0010D4E4 File Offset: 0x0010B6E4
		[SecuritySafeCritical]
		[ComVisible(true)]
		public void SetCustomAttribute(ConstructorInfo con, byte[] binaryAttribute)
		{
			if (con == null)
			{
				throw new ArgumentNullException("con");
			}
			if (binaryAttribute == null)
			{
				throw new ArgumentNullException("binaryAttribute");
			}
			this.m_type.ThrowIfCreated();
			TypeBuilder.DefineCustomAttribute(this.m_module, this.m_evToken.Token, this.m_module.GetConstructorToken(con).Token, binaryAttribute, false, false);
		}

		// Token: 0x06004A6C RID: 19052 RVA: 0x0010D54B File Offset: 0x0010B74B
		[SecuritySafeCritical]
		public void SetCustomAttribute(CustomAttributeBuilder customBuilder)
		{
			if (customBuilder == null)
			{
				throw new ArgumentNullException("customBuilder");
			}
			this.m_type.ThrowIfCreated();
			customBuilder.CreateCustomAttribute(this.m_module, this.m_evToken.Token);
		}

		// Token: 0x06004A6D RID: 19053 RVA: 0x0010D57D File Offset: 0x0010B77D
		void _EventBuilder.GetTypeInfoCount(out uint pcTInfo)
		{
			throw new NotImplementedException();
		}

		// Token: 0x06004A6E RID: 19054 RVA: 0x0010D584 File Offset: 0x0010B784
		void _EventBuilder.GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo)
		{
			throw new NotImplementedException();
		}

		// Token: 0x06004A6F RID: 19055 RVA: 0x0010D58B File Offset: 0x0010B78B
		void _EventBuilder.GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId)
		{
			throw new NotImplementedException();
		}

		// Token: 0x06004A70 RID: 19056 RVA: 0x0010D592 File Offset: 0x0010B792
		void _EventBuilder.Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr)
		{
			throw new NotImplementedException();
		}

		// Token: 0x04001EAF RID: 7855
		private string m_name;

		// Token: 0x04001EB0 RID: 7856
		private EventToken m_evToken;

		// Token: 0x04001EB1 RID: 7857
		private ModuleBuilder m_module;

		// Token: 0x04001EB2 RID: 7858
		private EventAttributes m_attributes;

		// Token: 0x04001EB3 RID: 7859
		private TypeBuilder m_type;
	}
}
