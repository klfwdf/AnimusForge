using System;
using System.Reflection;
using System.Security;

namespace System.Runtime.Remoting.Metadata
{
	// Token: 0x020007D3 RID: 2003
	internal class RemotingTypeCachedData : RemotingCachedData
	{
		// Token: 0x060056D2 RID: 22226 RVA: 0x00134577 File Offset: 0x00132777
		internal RemotingTypeCachedData(RuntimeType ri)
		{
			this.RI = ri;
		}

		// Token: 0x060056D3 RID: 22227 RVA: 0x00134588 File Offset: 0x00132788
		internal override SoapAttribute GetSoapAttributeNoLock()
		{
			object[] customAttributes = this.RI.GetCustomAttributes(typeof(SoapTypeAttribute), true);
			SoapAttribute soapAttribute;
			if (customAttributes != null && customAttributes.Length != 0)
			{
				soapAttribute = (SoapAttribute)customAttributes[0];
			}
			else
			{
				soapAttribute = new SoapTypeAttribute();
			}
			soapAttribute.SetReflectInfo(this.RI);
			return soapAttribute;
		}

		// Token: 0x060056D4 RID: 22228 RVA: 0x001345D4 File Offset: 0x001327D4
		internal MethodBase GetLastCalledMethod(string newMeth)
		{
			RemotingTypeCachedData.LastCalledMethodClass lastMethodCalled = this._lastMethodCalled;
			if (lastMethodCalled == null)
			{
				return null;
			}
			string methodName = lastMethodCalled.methodName;
			MethodBase mb = lastMethodCalled.MB;
			if (mb == null || methodName == null)
			{
				return null;
			}
			if (methodName.Equals(newMeth))
			{
				return mb;
			}
			return null;
		}

		// Token: 0x060056D5 RID: 22229 RVA: 0x00134618 File Offset: 0x00132818
		internal void SetLastCalledMethod(string newMethName, MethodBase newMB)
		{
			this._lastMethodCalled = new RemotingTypeCachedData.LastCalledMethodClass
			{
				methodName = newMethName,
				MB = newMB
			};
		}

		// Token: 0x17000E3F RID: 3647
		// (get) Token: 0x060056D6 RID: 22230 RVA: 0x00134640 File Offset: 0x00132840
		internal TypeInfo TypeInfo
		{
			[SecurityCritical]
			get
			{
				if (this._typeInfo == null)
				{
					this._typeInfo = new TypeInfo(this.RI);
				}
				return this._typeInfo;
			}
		}

		// Token: 0x17000E40 RID: 3648
		// (get) Token: 0x060056D7 RID: 22231 RVA: 0x00134661 File Offset: 0x00132861
		internal string QualifiedTypeName
		{
			[SecurityCritical]
			get
			{
				if (this._qualifiedTypeName == null)
				{
					this._qualifiedTypeName = RemotingServices.DetermineDefaultQualifiedTypeName(this.RI);
				}
				return this._qualifiedTypeName;
			}
		}

		// Token: 0x17000E41 RID: 3649
		// (get) Token: 0x060056D8 RID: 22232 RVA: 0x00134682 File Offset: 0x00132882
		internal string AssemblyName
		{
			get
			{
				if (this._assemblyName == null)
				{
					this._assemblyName = this.RI.Module.Assembly.FullName;
				}
				return this._assemblyName;
			}
		}

		// Token: 0x17000E42 RID: 3650
		// (get) Token: 0x060056D9 RID: 22233 RVA: 0x001346AD File Offset: 0x001328AD
		internal string SimpleAssemblyName
		{
			[SecurityCritical]
			get
			{
				if (this._simpleAssemblyName == null)
				{
					this._simpleAssemblyName = this.RI.GetRuntimeAssembly().GetSimpleName();
				}
				return this._simpleAssemblyName;
			}
		}

		// Token: 0x040027B9 RID: 10169
		private RuntimeType RI;

		// Token: 0x040027BA RID: 10170
		private RemotingTypeCachedData.LastCalledMethodClass _lastMethodCalled;

		// Token: 0x040027BB RID: 10171
		private TypeInfo _typeInfo;

		// Token: 0x040027BC RID: 10172
		private string _qualifiedTypeName;

		// Token: 0x040027BD RID: 10173
		private string _assemblyName;

		// Token: 0x040027BE RID: 10174
		private string _simpleAssemblyName;

		// Token: 0x02000C6E RID: 3182
		private class LastCalledMethodClass
		{
			// Token: 0x040037E5 RID: 14309
			public string methodName;

			// Token: 0x040037E6 RID: 14310
			public MethodBase MB;
		}
	}
}
