using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Contexts;
using System.Threading;

namespace System.Runtime.Remoting
{
	// Token: 0x020007C1 RID: 1985
	[ComVisible(true)]
	public class ActivatedClientTypeEntry : TypeEntry
	{
		// Token: 0x060055F5 RID: 22005 RVA: 0x0013109C File Offset: 0x0012F29C
		public ActivatedClientTypeEntry(string typeName, string assemblyName, string appUrl)
		{
			if (typeName == null)
			{
				throw new ArgumentNullException("typeName");
			}
			if (assemblyName == null)
			{
				throw new ArgumentNullException("assemblyName");
			}
			if (appUrl == null)
			{
				throw new ArgumentNullException("appUrl");
			}
			base.TypeName = typeName;
			base.AssemblyName = assemblyName;
			this._appUrl = appUrl;
		}

		// Token: 0x060055F6 RID: 22006 RVA: 0x001310F0 File Offset: 0x0012F2F0
		public ActivatedClientTypeEntry(Type type, string appUrl)
		{
			if (type == null)
			{
				throw new ArgumentNullException("type");
			}
			if (appUrl == null)
			{
				throw new ArgumentNullException("appUrl");
			}
			RuntimeType runtimeType = type as RuntimeType;
			if (runtimeType == null)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeAssembly"));
			}
			base.TypeName = type.FullName;
			base.AssemblyName = runtimeType.GetRuntimeAssembly().GetSimpleName();
			this._appUrl = appUrl;
		}

		// Token: 0x17000E2B RID: 3627
		// (get) Token: 0x060055F7 RID: 22007 RVA: 0x00131169 File Offset: 0x0012F369
		public string ApplicationUrl
		{
			get
			{
				return this._appUrl;
			}
		}

		// Token: 0x17000E2C RID: 3628
		// (get) Token: 0x060055F8 RID: 22008 RVA: 0x00131174 File Offset: 0x0012F374
		public Type ObjectType
		{
			[MethodImpl(MethodImplOptions.NoInlining)]
			get
			{
				StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
				return RuntimeTypeHandle.GetTypeByName(base.TypeName + ", " + base.AssemblyName, ref stackCrawlMark);
			}
		}

		// Token: 0x17000E2D RID: 3629
		// (get) Token: 0x060055F9 RID: 22009 RVA: 0x001311A0 File Offset: 0x0012F3A0
		// (set) Token: 0x060055FA RID: 22010 RVA: 0x001311A8 File Offset: 0x0012F3A8
		public IContextAttribute[] ContextAttributes
		{
			get
			{
				return this._contextAttributes;
			}
			set
			{
				this._contextAttributes = value;
			}
		}

		// Token: 0x060055FB RID: 22011 RVA: 0x001311B1 File Offset: 0x0012F3B1
		public override string ToString()
		{
			return string.Concat(new string[] { "type='", base.TypeName, ", ", base.AssemblyName, "'; appUrl=", this._appUrl });
		}

		// Token: 0x04002781 RID: 10113
		private string _appUrl;

		// Token: 0x04002782 RID: 10114
		private IContextAttribute[] _contextAttributes;
	}
}
