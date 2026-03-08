using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Permissions;

namespace System.Security.Policy
{
	// Token: 0x0200034E RID: 846
	[ComVisible(true)]
	[PermissionSet(SecurityAction.InheritanceDemand, Unrestricted = true)]
	[Serializable]
	public abstract class EvidenceBase
	{
		// Token: 0x06002A3D RID: 10813 RVA: 0x0009CB68 File Offset: 0x0009AD68
		protected EvidenceBase()
		{
			if (!base.GetType().IsSerializable)
			{
				throw new InvalidOperationException(Environment.GetResourceString("Policy_EvidenceMustBeSerializable"));
			}
		}

		// Token: 0x06002A3E RID: 10814 RVA: 0x0009CB90 File Offset: 0x0009AD90
		[SecuritySafeCritical]
		[SecurityPermission(SecurityAction.Assert, SerializationFormatter = true)]
		[PermissionSet(SecurityAction.InheritanceDemand, Unrestricted = true)]
		public virtual EvidenceBase Clone()
		{
			EvidenceBase result;
			using (MemoryStream memoryStream = new MemoryStream())
			{
				BinaryFormatter binaryFormatter = new BinaryFormatter();
				binaryFormatter.Serialize(memoryStream, this);
				memoryStream.Position = 0L;
				result = binaryFormatter.Deserialize(memoryStream) as EvidenceBase;
			}
			return result;
		}
	}
}
