using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Runtime.Remoting
{
	// Token: 0x020007C3 RID: 1987
	[ComVisible(true)]
	public class WellKnownClientTypeEntry : TypeEntry
	{
		// Token: 0x06005602 RID: 22018 RVA: 0x001312FC File Offset: 0x0012F4FC
		public WellKnownClientTypeEntry(string typeName, string assemblyName, string objectUrl)
		{
			if (typeName == null)
			{
				throw new ArgumentNullException("typeName");
			}
			if (assemblyName == null)
			{
				throw new ArgumentNullException("assemblyName");
			}
			if (objectUrl == null)
			{
				throw new ArgumentNullException("objectUrl");
			}
			base.TypeName = typeName;
			base.AssemblyName = assemblyName;
			this._objectUrl = objectUrl;
		}

		// Token: 0x06005603 RID: 22019 RVA: 0x00131350 File Offset: 0x0012F550
		public WellKnownClientTypeEntry(Type type, string objectUrl)
		{
			if (type == null)
			{
				throw new ArgumentNullException("type");
			}
			if (objectUrl == null)
			{
				throw new ArgumentNullException("objectUrl");
			}
			RuntimeType runtimeType = type as RuntimeType;
			if (runtimeType == null)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeType"));
			}
			base.TypeName = type.FullName;
			base.AssemblyName = runtimeType.GetRuntimeAssembly().GetSimpleName();
			this._objectUrl = objectUrl;
		}

		// Token: 0x17000E30 RID: 3632
		// (get) Token: 0x06005604 RID: 22020 RVA: 0x001313C9 File Offset: 0x0012F5C9
		public string ObjectUrl
		{
			get
			{
				return this._objectUrl;
			}
		}

		// Token: 0x17000E31 RID: 3633
		// (get) Token: 0x06005605 RID: 22021 RVA: 0x001313D4 File Offset: 0x0012F5D4
		public Type ObjectType
		{
			[MethodImpl(MethodImplOptions.NoInlining)]
			get
			{
				StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
				return RuntimeTypeHandle.GetTypeByName(base.TypeName + ", " + base.AssemblyName, ref stackCrawlMark);
			}
		}

		// Token: 0x17000E32 RID: 3634
		// (get) Token: 0x06005606 RID: 22022 RVA: 0x00131400 File Offset: 0x0012F600
		// (set) Token: 0x06005607 RID: 22023 RVA: 0x00131408 File Offset: 0x0012F608
		public string ApplicationUrl
		{
			get
			{
				return this._appUrl;
			}
			set
			{
				this._appUrl = value;
			}
		}

		// Token: 0x06005608 RID: 22024 RVA: 0x00131414 File Offset: 0x0012F614
		public override string ToString()
		{
			string text = string.Concat(new string[] { "type='", base.TypeName, ", ", base.AssemblyName, "'; url=", this._objectUrl });
			if (this._appUrl != null)
			{
				text = text + "; appUrl=" + this._appUrl;
			}
			return text;
		}

		// Token: 0x04002784 RID: 10116
		private string _objectUrl;

		// Token: 0x04002785 RID: 10117
		private string _appUrl;
	}
}
