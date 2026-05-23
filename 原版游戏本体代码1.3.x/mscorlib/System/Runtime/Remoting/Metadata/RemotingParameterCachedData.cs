using System;
using System.Reflection;

namespace System.Runtime.Remoting.Metadata
{
	// Token: 0x020007D2 RID: 2002
	internal class RemotingParameterCachedData : RemotingCachedData
	{
		// Token: 0x060056D0 RID: 22224 RVA: 0x0013451B File Offset: 0x0013271B
		internal RemotingParameterCachedData(RuntimeParameterInfo ri)
		{
			this.RI = ri;
		}

		// Token: 0x060056D1 RID: 22225 RVA: 0x0013452C File Offset: 0x0013272C
		internal override SoapAttribute GetSoapAttributeNoLock()
		{
			object[] customAttributes = this.RI.GetCustomAttributes(typeof(SoapParameterAttribute), true);
			SoapAttribute soapAttribute;
			if (customAttributes != null && customAttributes.Length != 0)
			{
				soapAttribute = (SoapParameterAttribute)customAttributes[0];
			}
			else
			{
				soapAttribute = new SoapParameterAttribute();
			}
			soapAttribute.SetReflectInfo(this.RI);
			return soapAttribute;
		}

		// Token: 0x040027B8 RID: 10168
		private RuntimeParameterInfo RI;
	}
}
