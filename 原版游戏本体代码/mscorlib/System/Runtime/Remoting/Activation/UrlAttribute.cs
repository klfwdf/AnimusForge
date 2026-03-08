using System;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Contexts;
using System.Security;

namespace System.Runtime.Remoting.Activation
{
	// Token: 0x0200089F RID: 2207
	[SecurityCritical]
	[ComVisible(true)]
	[Serializable]
	public sealed class UrlAttribute : ContextAttribute
	{
		// Token: 0x06005D66 RID: 23910 RVA: 0x00149024 File Offset: 0x00147224
		[SecurityCritical]
		public UrlAttribute(string callsiteURL)
			: base(UrlAttribute.propertyName)
		{
			if (callsiteURL == null)
			{
				throw new ArgumentNullException("callsiteURL");
			}
			this.url = callsiteURL;
		}

		// Token: 0x06005D67 RID: 23911 RVA: 0x00149046 File Offset: 0x00147246
		[SecuritySafeCritical]
		public override bool Equals(object o)
		{
			return o is IContextProperty && o is UrlAttribute && ((UrlAttribute)o).UrlValue.Equals(this.url);
		}

		// Token: 0x06005D68 RID: 23912 RVA: 0x00149070 File Offset: 0x00147270
		[SecuritySafeCritical]
		public override int GetHashCode()
		{
			return this.url.GetHashCode();
		}

		// Token: 0x06005D69 RID: 23913 RVA: 0x0014907D File Offset: 0x0014727D
		[SecurityCritical]
		[ComVisible(true)]
		public override bool IsContextOK(Context ctx, IConstructionCallMessage msg)
		{
			return false;
		}

		// Token: 0x06005D6A RID: 23914 RVA: 0x00149080 File Offset: 0x00147280
		[SecurityCritical]
		[ComVisible(true)]
		public override void GetPropertiesForNewContext(IConstructionCallMessage ctorMsg)
		{
		}

		// Token: 0x1700100D RID: 4109
		// (get) Token: 0x06005D6B RID: 23915 RVA: 0x00149082 File Offset: 0x00147282
		public string UrlValue
		{
			[SecurityCritical]
			get
			{
				return this.url;
			}
		}

		// Token: 0x04002A0B RID: 10763
		private string url;

		// Token: 0x04002A0C RID: 10764
		private static string propertyName = "UrlAttribute";
	}
}
