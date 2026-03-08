using System;
using System.Reflection;
using System.Runtime.Serialization;

namespace System.Runtime.Remoting.Metadata
{
	// Token: 0x020007D1 RID: 2001
	internal class RemotingFieldCachedData : RemotingCachedData
	{
		// Token: 0x060056CD RID: 22221 RVA: 0x001344B0 File Offset: 0x001326B0
		internal RemotingFieldCachedData(RuntimeFieldInfo ri)
		{
			this.RI = ri;
		}

		// Token: 0x060056CE RID: 22222 RVA: 0x001344BF File Offset: 0x001326BF
		internal RemotingFieldCachedData(SerializationFieldInfo ri)
		{
			this.RI = ri;
		}

		// Token: 0x060056CF RID: 22223 RVA: 0x001344D0 File Offset: 0x001326D0
		internal override SoapAttribute GetSoapAttributeNoLock()
		{
			object[] customAttributes = this.RI.GetCustomAttributes(typeof(SoapFieldAttribute), false);
			SoapAttribute soapAttribute;
			if (customAttributes != null && customAttributes.Length != 0)
			{
				soapAttribute = (SoapAttribute)customAttributes[0];
			}
			else
			{
				soapAttribute = new SoapFieldAttribute();
			}
			soapAttribute.SetReflectInfo(this.RI);
			return soapAttribute;
		}

		// Token: 0x040027B7 RID: 10167
		private FieldInfo RI;
	}
}
