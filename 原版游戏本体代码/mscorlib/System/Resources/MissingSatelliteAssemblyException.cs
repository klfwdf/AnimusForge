using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System.Resources
{
	// Token: 0x02000392 RID: 914
	[ComVisible(true)]
	[Serializable]
	public class MissingSatelliteAssemblyException : SystemException
	{
		// Token: 0x06002D08 RID: 11528 RVA: 0x000AA283 File Offset: 0x000A8483
		public MissingSatelliteAssemblyException()
			: base(Environment.GetResourceString("MissingSatelliteAssembly_Default"))
		{
			base.SetErrorCode(-2146233034);
		}

		// Token: 0x06002D09 RID: 11529 RVA: 0x000AA2A0 File Offset: 0x000A84A0
		public MissingSatelliteAssemblyException(string message)
			: base(message)
		{
			base.SetErrorCode(-2146233034);
		}

		// Token: 0x06002D0A RID: 11530 RVA: 0x000AA2B4 File Offset: 0x000A84B4
		public MissingSatelliteAssemblyException(string message, string cultureName)
			: base(message)
		{
			base.SetErrorCode(-2146233034);
			this._cultureName = cultureName;
		}

		// Token: 0x06002D0B RID: 11531 RVA: 0x000AA2CF File Offset: 0x000A84CF
		public MissingSatelliteAssemblyException(string message, Exception inner)
			: base(message, inner)
		{
			base.SetErrorCode(-2146233034);
		}

		// Token: 0x06002D0C RID: 11532 RVA: 0x000AA2E4 File Offset: 0x000A84E4
		protected MissingSatelliteAssemblyException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}

		// Token: 0x170005E5 RID: 1509
		// (get) Token: 0x06002D0D RID: 11533 RVA: 0x000AA2EE File Offset: 0x000A84EE
		public string CultureName
		{
			get
			{
				return this._cultureName;
			}
		}

		// Token: 0x0400122E RID: 4654
		private string _cultureName;
	}
}
