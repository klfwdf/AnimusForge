using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security.Claims;

namespace System.Security.Principal
{
	// Token: 0x02000322 RID: 802
	[ComVisible(true)]
	[Serializable]
	public class GenericIdentity : ClaimsIdentity
	{
		// Token: 0x0600288D RID: 10381 RVA: 0x00094CE1 File Offset: 0x00092EE1
		[SecuritySafeCritical]
		public GenericIdentity(string name)
		{
			if (name == null)
			{
				throw new ArgumentNullException("name");
			}
			this.m_name = name;
			this.m_type = "";
			this.AddNameClaim();
		}

		// Token: 0x0600288E RID: 10382 RVA: 0x00094D0F File Offset: 0x00092F0F
		[SecuritySafeCritical]
		public GenericIdentity(string name, string type)
		{
			if (name == null)
			{
				throw new ArgumentNullException("name");
			}
			if (type == null)
			{
				throw new ArgumentNullException("type");
			}
			this.m_name = name;
			this.m_type = type;
			this.AddNameClaim();
		}

		// Token: 0x0600288F RID: 10383 RVA: 0x00094D47 File Offset: 0x00092F47
		private GenericIdentity()
		{
		}

		// Token: 0x06002890 RID: 10384 RVA: 0x00094D4F File Offset: 0x00092F4F
		protected GenericIdentity(GenericIdentity identity)
			: base(identity)
		{
			this.m_name = identity.m_name;
			this.m_type = identity.m_type;
		}

		// Token: 0x06002891 RID: 10385 RVA: 0x00094D70 File Offset: 0x00092F70
		public override ClaimsIdentity Clone()
		{
			return new GenericIdentity(this);
		}

		// Token: 0x1700053A RID: 1338
		// (get) Token: 0x06002892 RID: 10386 RVA: 0x00094D78 File Offset: 0x00092F78
		public override IEnumerable<Claim> Claims
		{
			get
			{
				return base.Claims;
			}
		}

		// Token: 0x1700053B RID: 1339
		// (get) Token: 0x06002893 RID: 10387 RVA: 0x00094D80 File Offset: 0x00092F80
		public override string Name
		{
			get
			{
				return this.m_name;
			}
		}

		// Token: 0x1700053C RID: 1340
		// (get) Token: 0x06002894 RID: 10388 RVA: 0x00094D88 File Offset: 0x00092F88
		public override string AuthenticationType
		{
			get
			{
				return this.m_type;
			}
		}

		// Token: 0x1700053D RID: 1341
		// (get) Token: 0x06002895 RID: 10389 RVA: 0x00094D90 File Offset: 0x00092F90
		public override bool IsAuthenticated
		{
			get
			{
				return !this.m_name.Equals("");
			}
		}

		// Token: 0x06002896 RID: 10390 RVA: 0x00094DA8 File Offset: 0x00092FA8
		[OnDeserialized]
		private void OnDeserializedMethod(StreamingContext context)
		{
			bool flag = false;
			using (IEnumerator<Claim> enumerator = base.Claims.GetEnumerator())
			{
				if (enumerator.MoveNext())
				{
					Claim claim = enumerator.Current;
					flag = true;
				}
			}
			if (!flag)
			{
				this.AddNameClaim();
			}
		}

		// Token: 0x06002897 RID: 10391 RVA: 0x00094E00 File Offset: 0x00093000
		[SecuritySafeCritical]
		private void AddNameClaim()
		{
			if (this.m_name != null)
			{
				base.AddClaim(new Claim(base.NameClaimType, this.m_name, "http://www.w3.org/2001/XMLSchema#string", "LOCAL AUTHORITY", "LOCAL AUTHORITY", this));
			}
		}

		// Token: 0x0400100B RID: 4107
		private string m_name;

		// Token: 0x0400100C RID: 4108
		private string m_type;
	}
}
