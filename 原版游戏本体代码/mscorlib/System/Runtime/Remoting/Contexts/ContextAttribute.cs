using System;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Activation;
using System.Security;
using System.Security.Permissions;

namespace System.Runtime.Remoting.Contexts
{
	// Token: 0x0200080F RID: 2063
	[SecurityCritical]
	[AttributeUsage(AttributeTargets.Class)]
	[ComVisible(true)]
	[SecurityPermission(SecurityAction.InheritanceDemand, Flags = SecurityPermissionFlag.Infrastructure)]
	[Serializable]
	public class ContextAttribute : Attribute, IContextAttribute, IContextProperty
	{
		// Token: 0x060058C9 RID: 22729 RVA: 0x00138F0B File Offset: 0x0013710B
		public ContextAttribute(string name)
		{
			this.AttributeName = name;
		}

		// Token: 0x17000EB9 RID: 3769
		// (get) Token: 0x060058CA RID: 22730 RVA: 0x00138F1A File Offset: 0x0013711A
		public virtual string Name
		{
			[SecurityCritical]
			get
			{
				return this.AttributeName;
			}
		}

		// Token: 0x060058CB RID: 22731 RVA: 0x00138F22 File Offset: 0x00137122
		[SecurityCritical]
		public virtual bool IsNewContextOK(Context newCtx)
		{
			return true;
		}

		// Token: 0x060058CC RID: 22732 RVA: 0x00138F25 File Offset: 0x00137125
		[SecurityCritical]
		public virtual void Freeze(Context newContext)
		{
		}

		// Token: 0x060058CD RID: 22733 RVA: 0x00138F28 File Offset: 0x00137128
		[SecuritySafeCritical]
		public override bool Equals(object o)
		{
			IContextProperty contextProperty = o as IContextProperty;
			return contextProperty != null && this.AttributeName.Equals(contextProperty.Name);
		}

		// Token: 0x060058CE RID: 22734 RVA: 0x00138F52 File Offset: 0x00137152
		[SecuritySafeCritical]
		public override int GetHashCode()
		{
			return this.AttributeName.GetHashCode();
		}

		// Token: 0x060058CF RID: 22735 RVA: 0x00138F60 File Offset: 0x00137160
		[SecurityCritical]
		public virtual bool IsContextOK(Context ctx, IConstructionCallMessage ctorMsg)
		{
			if (ctx == null)
			{
				throw new ArgumentNullException("ctx");
			}
			if (ctorMsg == null)
			{
				throw new ArgumentNullException("ctorMsg");
			}
			if (!ctorMsg.ActivationType.IsContextful)
			{
				return true;
			}
			object property = ctx.GetProperty(this.AttributeName);
			return property != null && this.Equals(property);
		}

		// Token: 0x060058D0 RID: 22736 RVA: 0x00138FB4 File Offset: 0x001371B4
		[SecurityCritical]
		public virtual void GetPropertiesForNewContext(IConstructionCallMessage ctorMsg)
		{
			if (ctorMsg == null)
			{
				throw new ArgumentNullException("ctorMsg");
			}
			ctorMsg.ContextProperties.Add(this);
		}

		// Token: 0x04002875 RID: 10357
		protected string AttributeName;
	}
}
