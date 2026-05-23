using System;
using System.Collections;
using System.Reflection;
using System.Security;

namespace System.Runtime.Remoting.Messaging
{
	// Token: 0x02000871 RID: 2161
	internal class ErrorMessage : IMethodCallMessage, IMethodMessage, IMessage
	{
		// Token: 0x17000FB2 RID: 4018
		// (get) Token: 0x06005BEB RID: 23531 RVA: 0x00142BC9 File Offset: 0x00140DC9
		public IDictionary Properties
		{
			[SecurityCritical]
			get
			{
				return null;
			}
		}

		// Token: 0x17000FB3 RID: 4019
		// (get) Token: 0x06005BEC RID: 23532 RVA: 0x00142BCC File Offset: 0x00140DCC
		public string Uri
		{
			[SecurityCritical]
			get
			{
				return this.m_URI;
			}
		}

		// Token: 0x17000FB4 RID: 4020
		// (get) Token: 0x06005BED RID: 23533 RVA: 0x00142BD4 File Offset: 0x00140DD4
		public string MethodName
		{
			[SecurityCritical]
			get
			{
				return this.m_MethodName;
			}
		}

		// Token: 0x17000FB5 RID: 4021
		// (get) Token: 0x06005BEE RID: 23534 RVA: 0x00142BDC File Offset: 0x00140DDC
		public string TypeName
		{
			[SecurityCritical]
			get
			{
				return this.m_TypeName;
			}
		}

		// Token: 0x17000FB6 RID: 4022
		// (get) Token: 0x06005BEF RID: 23535 RVA: 0x00142BE4 File Offset: 0x00140DE4
		public object MethodSignature
		{
			[SecurityCritical]
			get
			{
				return this.m_MethodSignature;
			}
		}

		// Token: 0x17000FB7 RID: 4023
		// (get) Token: 0x06005BF0 RID: 23536 RVA: 0x00142BEC File Offset: 0x00140DEC
		public MethodBase MethodBase
		{
			[SecurityCritical]
			get
			{
				return null;
			}
		}

		// Token: 0x17000FB8 RID: 4024
		// (get) Token: 0x06005BF1 RID: 23537 RVA: 0x00142BEF File Offset: 0x00140DEF
		public int ArgCount
		{
			[SecurityCritical]
			get
			{
				return this.m_ArgCount;
			}
		}

		// Token: 0x06005BF2 RID: 23538 RVA: 0x00142BF7 File Offset: 0x00140DF7
		[SecurityCritical]
		public string GetArgName(int index)
		{
			return this.m_ArgName;
		}

		// Token: 0x06005BF3 RID: 23539 RVA: 0x00142BFF File Offset: 0x00140DFF
		[SecurityCritical]
		public object GetArg(int argNum)
		{
			return null;
		}

		// Token: 0x17000FB9 RID: 4025
		// (get) Token: 0x06005BF4 RID: 23540 RVA: 0x00142C02 File Offset: 0x00140E02
		public object[] Args
		{
			[SecurityCritical]
			get
			{
				return null;
			}
		}

		// Token: 0x17000FBA RID: 4026
		// (get) Token: 0x06005BF5 RID: 23541 RVA: 0x00142C05 File Offset: 0x00140E05
		public bool HasVarArgs
		{
			[SecurityCritical]
			get
			{
				return false;
			}
		}

		// Token: 0x17000FBB RID: 4027
		// (get) Token: 0x06005BF6 RID: 23542 RVA: 0x00142C08 File Offset: 0x00140E08
		public int InArgCount
		{
			[SecurityCritical]
			get
			{
				return this.m_ArgCount;
			}
		}

		// Token: 0x06005BF7 RID: 23543 RVA: 0x00142C10 File Offset: 0x00140E10
		[SecurityCritical]
		public string GetInArgName(int index)
		{
			return null;
		}

		// Token: 0x06005BF8 RID: 23544 RVA: 0x00142C13 File Offset: 0x00140E13
		[SecurityCritical]
		public object GetInArg(int argNum)
		{
			return null;
		}

		// Token: 0x17000FBC RID: 4028
		// (get) Token: 0x06005BF9 RID: 23545 RVA: 0x00142C16 File Offset: 0x00140E16
		public object[] InArgs
		{
			[SecurityCritical]
			get
			{
				return null;
			}
		}

		// Token: 0x17000FBD RID: 4029
		// (get) Token: 0x06005BFA RID: 23546 RVA: 0x00142C19 File Offset: 0x00140E19
		public LogicalCallContext LogicalCallContext
		{
			[SecurityCritical]
			get
			{
				return null;
			}
		}

		// Token: 0x0400298E RID: 10638
		private string m_URI = "Exception";

		// Token: 0x0400298F RID: 10639
		private string m_MethodName = "Unknown";

		// Token: 0x04002990 RID: 10640
		private string m_TypeName = "Unknown";

		// Token: 0x04002991 RID: 10641
		private object m_MethodSignature;

		// Token: 0x04002992 RID: 10642
		private int m_ArgCount;

		// Token: 0x04002993 RID: 10643
		private string m_ArgName = "Unknown";
	}
}
