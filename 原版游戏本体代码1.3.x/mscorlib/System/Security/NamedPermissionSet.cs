using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Threading;

namespace System.Security
{
	// Token: 0x020001DA RID: 474
	[ComVisible(true)]
	[Serializable]
	public sealed class NamedPermissionSet : PermissionSet
	{
		// Token: 0x06001CA7 RID: 7335 RVA: 0x00062289 File Offset: 0x00060489
		internal NamedPermissionSet()
		{
		}

		// Token: 0x06001CA8 RID: 7336 RVA: 0x00062291 File Offset: 0x00060491
		public NamedPermissionSet(string name)
		{
			NamedPermissionSet.CheckName(name);
			this.m_name = name;
		}

		// Token: 0x06001CA9 RID: 7337 RVA: 0x000622A6 File Offset: 0x000604A6
		public NamedPermissionSet(string name, PermissionState state)
			: base(state)
		{
			NamedPermissionSet.CheckName(name);
			this.m_name = name;
		}

		// Token: 0x06001CAA RID: 7338 RVA: 0x000622BC File Offset: 0x000604BC
		public NamedPermissionSet(string name, PermissionSet permSet)
			: base(permSet)
		{
			NamedPermissionSet.CheckName(name);
			this.m_name = name;
		}

		// Token: 0x06001CAB RID: 7339 RVA: 0x000622D2 File Offset: 0x000604D2
		public NamedPermissionSet(NamedPermissionSet permSet)
			: base(permSet)
		{
			this.m_name = permSet.m_name;
			this.m_description = permSet.Description;
		}

		// Token: 0x06001CAC RID: 7340 RVA: 0x000622F3 File Offset: 0x000604F3
		internal NamedPermissionSet(SecurityElement permissionSetXml)
			: base(PermissionState.None)
		{
			this.FromXml(permissionSetXml);
		}

		// Token: 0x17000339 RID: 825
		// (get) Token: 0x06001CAD RID: 7341 RVA: 0x00062303 File Offset: 0x00060503
		// (set) Token: 0x06001CAE RID: 7342 RVA: 0x0006230B File Offset: 0x0006050B
		public string Name
		{
			get
			{
				return this.m_name;
			}
			set
			{
				NamedPermissionSet.CheckName(value);
				this.m_name = value;
			}
		}

		// Token: 0x06001CAF RID: 7343 RVA: 0x0006231A File Offset: 0x0006051A
		private static void CheckName(string name)
		{
			if (name == null || name.Equals(""))
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_NPMSInvalidName"));
			}
		}

		// Token: 0x1700033A RID: 826
		// (get) Token: 0x06001CB0 RID: 7344 RVA: 0x0006233C File Offset: 0x0006053C
		// (set) Token: 0x06001CB1 RID: 7345 RVA: 0x00062364 File Offset: 0x00060564
		public string Description
		{
			get
			{
				if (this.m_descrResource != null)
				{
					this.m_description = Environment.GetResourceString(this.m_descrResource);
					this.m_descrResource = null;
				}
				return this.m_description;
			}
			set
			{
				this.m_description = value;
				this.m_descrResource = null;
			}
		}

		// Token: 0x06001CB2 RID: 7346 RVA: 0x00062374 File Offset: 0x00060574
		public override PermissionSet Copy()
		{
			return new NamedPermissionSet(this);
		}

		// Token: 0x06001CB3 RID: 7347 RVA: 0x0006237C File Offset: 0x0006057C
		public NamedPermissionSet Copy(string name)
		{
			return new NamedPermissionSet(this)
			{
				Name = name
			};
		}

		// Token: 0x06001CB4 RID: 7348 RVA: 0x00062398 File Offset: 0x00060598
		public override SecurityElement ToXml()
		{
			SecurityElement securityElement = base.ToXml("System.Security.NamedPermissionSet");
			if (this.m_name != null && !this.m_name.Equals(""))
			{
				securityElement.AddAttribute("Name", SecurityElement.Escape(this.m_name));
			}
			if (this.Description != null && !this.Description.Equals(""))
			{
				securityElement.AddAttribute("Description", SecurityElement.Escape(this.Description));
			}
			return securityElement;
		}

		// Token: 0x06001CB5 RID: 7349 RVA: 0x00062412 File Offset: 0x00060612
		public override void FromXml(SecurityElement et)
		{
			this.FromXml(et, false, false);
		}

		// Token: 0x06001CB6 RID: 7350 RVA: 0x00062420 File Offset: 0x00060620
		internal override void FromXml(SecurityElement et, bool allowInternalOnly, bool ignoreTypeLoadFailures)
		{
			if (et == null)
			{
				throw new ArgumentNullException("et");
			}
			string text = et.Attribute("Name");
			this.m_name = ((text == null) ? null : text);
			text = et.Attribute("Description");
			this.m_description = ((text == null) ? "" : text);
			this.m_descrResource = null;
			base.FromXml(et, allowInternalOnly, ignoreTypeLoadFailures);
		}

		// Token: 0x06001CB7 RID: 7351 RVA: 0x00062484 File Offset: 0x00060684
		internal void FromXmlNameOnly(SecurityElement et)
		{
			string text = et.Attribute("Name");
			this.m_name = ((text == null) ? null : text);
		}

		// Token: 0x06001CB8 RID: 7352 RVA: 0x000624AA File Offset: 0x000606AA
		[ComVisible(false)]
		public override bool Equals(object obj)
		{
			return base.Equals(obj);
		}

		// Token: 0x06001CB9 RID: 7353 RVA: 0x000624B3 File Offset: 0x000606B3
		[ComVisible(false)]
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		// Token: 0x1700033B RID: 827
		// (get) Token: 0x06001CBA RID: 7354 RVA: 0x000624BC File Offset: 0x000606BC
		private static object InternalSyncObject
		{
			get
			{
				if (NamedPermissionSet.s_InternalSyncObject == null)
				{
					object value = new object();
					Interlocked.CompareExchange(ref NamedPermissionSet.s_InternalSyncObject, value, null);
				}
				return NamedPermissionSet.s_InternalSyncObject;
			}
		}

		// Token: 0x04000A05 RID: 2565
		private string m_name;

		// Token: 0x04000A06 RID: 2566
		private string m_description;

		// Token: 0x04000A07 RID: 2567
		[OptionalField(VersionAdded = 2)]
		internal string m_descrResource;

		// Token: 0x04000A08 RID: 2568
		private static object s_InternalSyncObject;
	}
}
