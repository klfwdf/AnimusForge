using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Contexts;
using System.Threading;

namespace System.Runtime.Remoting
{
	// Token: 0x020007C4 RID: 1988
	[ComVisible(true)]
	public class WellKnownServiceTypeEntry : TypeEntry
	{
		// Token: 0x06005609 RID: 22025 RVA: 0x0013147C File Offset: 0x0012F67C
		public WellKnownServiceTypeEntry(string typeName, string assemblyName, string objectUri, WellKnownObjectMode mode)
		{
			if (typeName == null)
			{
				throw new ArgumentNullException("typeName");
			}
			if (assemblyName == null)
			{
				throw new ArgumentNullException("assemblyName");
			}
			if (objectUri == null)
			{
				throw new ArgumentNullException("objectUri");
			}
			base.TypeName = typeName;
			base.AssemblyName = assemblyName;
			this._objectUri = objectUri;
			this._mode = mode;
		}

		// Token: 0x0600560A RID: 22026 RVA: 0x001314D8 File Offset: 0x0012F6D8
		public WellKnownServiceTypeEntry(Type type, string objectUri, WellKnownObjectMode mode)
		{
			if (type == null)
			{
				throw new ArgumentNullException("type");
			}
			if (objectUri == null)
			{
				throw new ArgumentNullException("objectUri");
			}
			if (!(type is RuntimeType))
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeType"));
			}
			base.TypeName = type.FullName;
			base.AssemblyName = type.Module.Assembly.FullName;
			this._objectUri = objectUri;
			this._mode = mode;
		}

		// Token: 0x17000E33 RID: 3635
		// (get) Token: 0x0600560B RID: 22027 RVA: 0x00131555 File Offset: 0x0012F755
		public string ObjectUri
		{
			get
			{
				return this._objectUri;
			}
		}

		// Token: 0x17000E34 RID: 3636
		// (get) Token: 0x0600560C RID: 22028 RVA: 0x0013155D File Offset: 0x0012F75D
		public WellKnownObjectMode Mode
		{
			get
			{
				return this._mode;
			}
		}

		// Token: 0x17000E35 RID: 3637
		// (get) Token: 0x0600560D RID: 22029 RVA: 0x00131568 File Offset: 0x0012F768
		public Type ObjectType
		{
			[MethodImpl(MethodImplOptions.NoInlining)]
			get
			{
				StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
				return RuntimeTypeHandle.GetTypeByName(base.TypeName + ", " + base.AssemblyName, ref stackCrawlMark);
			}
		}

		// Token: 0x17000E36 RID: 3638
		// (get) Token: 0x0600560E RID: 22030 RVA: 0x00131594 File Offset: 0x0012F794
		// (set) Token: 0x0600560F RID: 22031 RVA: 0x0013159C File Offset: 0x0012F79C
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

		// Token: 0x06005610 RID: 22032 RVA: 0x001315A8 File Offset: 0x0012F7A8
		public override string ToString()
		{
			return string.Concat(new string[]
			{
				"type='",
				base.TypeName,
				", ",
				base.AssemblyName,
				"'; objectUri=",
				this._objectUri,
				"; mode=",
				this._mode.ToString()
			});
		}

		// Token: 0x04002786 RID: 10118
		private string _objectUri;

		// Token: 0x04002787 RID: 10119
		private WellKnownObjectMode _mode;

		// Token: 0x04002788 RID: 10120
		private IContextAttribute[] _contextAttributes;
	}
}
