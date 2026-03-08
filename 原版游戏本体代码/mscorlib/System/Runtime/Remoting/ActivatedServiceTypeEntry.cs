using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Contexts;
using System.Threading;

namespace System.Runtime.Remoting
{
	// Token: 0x020007C2 RID: 1986
	[ComVisible(true)]
	public class ActivatedServiceTypeEntry : TypeEntry
	{
		// Token: 0x060055FC RID: 22012 RVA: 0x001311F1 File Offset: 0x0012F3F1
		public ActivatedServiceTypeEntry(string typeName, string assemblyName)
		{
			if (typeName == null)
			{
				throw new ArgumentNullException("typeName");
			}
			if (assemblyName == null)
			{
				throw new ArgumentNullException("assemblyName");
			}
			base.TypeName = typeName;
			base.AssemblyName = assemblyName;
		}

		// Token: 0x060055FD RID: 22013 RVA: 0x00131224 File Offset: 0x0012F424
		public ActivatedServiceTypeEntry(Type type)
		{
			if (type == null)
			{
				throw new ArgumentNullException("type");
			}
			RuntimeType runtimeType = type as RuntimeType;
			if (runtimeType == null)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeAssembly"));
			}
			base.TypeName = type.FullName;
			base.AssemblyName = runtimeType.GetRuntimeAssembly().GetSimpleName();
		}

		// Token: 0x17000E2E RID: 3630
		// (get) Token: 0x060055FE RID: 22014 RVA: 0x00131288 File Offset: 0x0012F488
		public Type ObjectType
		{
			[MethodImpl(MethodImplOptions.NoInlining)]
			get
			{
				StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
				return RuntimeTypeHandle.GetTypeByName(base.TypeName + ", " + base.AssemblyName, ref stackCrawlMark);
			}
		}

		// Token: 0x17000E2F RID: 3631
		// (get) Token: 0x060055FF RID: 22015 RVA: 0x001312B4 File Offset: 0x0012F4B4
		// (set) Token: 0x06005600 RID: 22016 RVA: 0x001312BC File Offset: 0x0012F4BC
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

		// Token: 0x06005601 RID: 22017 RVA: 0x001312C5 File Offset: 0x0012F4C5
		public override string ToString()
		{
			return string.Concat(new string[] { "type='", base.TypeName, ", ", base.AssemblyName, "'" });
		}

		// Token: 0x04002783 RID: 10115
		private IContextAttribute[] _contextAttributes;
	}
}
