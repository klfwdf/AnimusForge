using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Metadata;
using System.Security;

namespace System.Runtime.Serialization.Formatters
{
	// Token: 0x02000766 RID: 1894
	[SoapType(Embedded = true)]
	[ComVisible(true)]
	[Serializable]
	public sealed class SoapFault : ISerializable
	{
		// Token: 0x06005322 RID: 21282 RVA: 0x00123DD0 File Offset: 0x00121FD0
		public SoapFault()
		{
		}

		// Token: 0x06005323 RID: 21283 RVA: 0x00123DD8 File Offset: 0x00121FD8
		public SoapFault(string faultCode, string faultString, string faultActor, ServerFault serverFault)
		{
			this.faultCode = faultCode;
			this.faultString = faultString;
			this.faultActor = faultActor;
			this.detail = serverFault;
		}

		// Token: 0x06005324 RID: 21284 RVA: 0x00123E00 File Offset: 0x00122000
		internal SoapFault(SerializationInfo info, StreamingContext context)
		{
			SerializationInfoEnumerator enumerator = info.GetEnumerator();
			while (enumerator.MoveNext())
			{
				string name = enumerator.Name;
				object value = enumerator.Value;
				if (string.Compare(name, "faultCode", true, CultureInfo.InvariantCulture) == 0)
				{
					int num = ((string)value).IndexOf(':');
					if (num > -1)
					{
						this.faultCode = ((string)value).Substring(num + 1);
					}
					else
					{
						this.faultCode = (string)value;
					}
				}
				else if (string.Compare(name, "faultString", true, CultureInfo.InvariantCulture) == 0)
				{
					this.faultString = (string)value;
				}
				else if (string.Compare(name, "faultActor", true, CultureInfo.InvariantCulture) == 0)
				{
					this.faultActor = (string)value;
				}
				else if (string.Compare(name, "detail", true, CultureInfo.InvariantCulture) == 0)
				{
					this.detail = value;
				}
			}
		}

		// Token: 0x06005325 RID: 21285 RVA: 0x00123EE0 File Offset: 0x001220E0
		[SecurityCritical]
		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("faultcode", "SOAP-ENV:" + this.faultCode);
			info.AddValue("faultstring", this.faultString);
			if (this.faultActor != null)
			{
				info.AddValue("faultactor", this.faultActor);
			}
			info.AddValue("detail", this.detail, typeof(object));
		}

		// Token: 0x17000DC6 RID: 3526
		// (get) Token: 0x06005326 RID: 21286 RVA: 0x00123F4D File Offset: 0x0012214D
		// (set) Token: 0x06005327 RID: 21287 RVA: 0x00123F55 File Offset: 0x00122155
		public string FaultCode
		{
			get
			{
				return this.faultCode;
			}
			set
			{
				this.faultCode = value;
			}
		}

		// Token: 0x17000DC7 RID: 3527
		// (get) Token: 0x06005328 RID: 21288 RVA: 0x00123F5E File Offset: 0x0012215E
		// (set) Token: 0x06005329 RID: 21289 RVA: 0x00123F66 File Offset: 0x00122166
		public string FaultString
		{
			get
			{
				return this.faultString;
			}
			set
			{
				this.faultString = value;
			}
		}

		// Token: 0x17000DC8 RID: 3528
		// (get) Token: 0x0600532A RID: 21290 RVA: 0x00123F6F File Offset: 0x0012216F
		// (set) Token: 0x0600532B RID: 21291 RVA: 0x00123F77 File Offset: 0x00122177
		public string FaultActor
		{
			get
			{
				return this.faultActor;
			}
			set
			{
				this.faultActor = value;
			}
		}

		// Token: 0x17000DC9 RID: 3529
		// (get) Token: 0x0600532C RID: 21292 RVA: 0x00123F80 File Offset: 0x00122180
		// (set) Token: 0x0600532D RID: 21293 RVA: 0x00123F88 File Offset: 0x00122188
		public object Detail
		{
			get
			{
				return this.detail;
			}
			set
			{
				this.detail = value;
			}
		}

		// Token: 0x040024DD RID: 9437
		private string faultCode;

		// Token: 0x040024DE RID: 9438
		private string faultString;

		// Token: 0x040024DF RID: 9439
		private string faultActor;

		// Token: 0x040024E0 RID: 9440
		[SoapField(Embedded = true)]
		private object detail;
	}
}
