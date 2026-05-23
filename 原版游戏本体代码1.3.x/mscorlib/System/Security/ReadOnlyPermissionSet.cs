using System;
using System.Collections;
using System.Runtime.Serialization;

namespace System.Security
{
	// Token: 0x020001E6 RID: 486
	[Serializable]
	public sealed class ReadOnlyPermissionSet : PermissionSet
	{
		// Token: 0x06001D78 RID: 7544 RVA: 0x00066CE0 File Offset: 0x00064EE0
		public ReadOnlyPermissionSet(SecurityElement permissionSetXml)
		{
			if (permissionSetXml == null)
			{
				throw new ArgumentNullException("permissionSetXml");
			}
			this.m_originXml = permissionSetXml.Copy();
			base.FromXml(this.m_originXml);
		}

		// Token: 0x06001D79 RID: 7545 RVA: 0x00066D0E File Offset: 0x00064F0E
		[OnDeserializing]
		private void OnDeserializing(StreamingContext ctx)
		{
			this.m_deserializing = true;
		}

		// Token: 0x06001D7A RID: 7546 RVA: 0x00066D17 File Offset: 0x00064F17
		[OnDeserialized]
		private void OnDeserialized(StreamingContext ctx)
		{
			this.m_deserializing = false;
		}

		// Token: 0x17000346 RID: 838
		// (get) Token: 0x06001D7B RID: 7547 RVA: 0x00066D20 File Offset: 0x00064F20
		public override bool IsReadOnly
		{
			get
			{
				return true;
			}
		}

		// Token: 0x06001D7C RID: 7548 RVA: 0x00066D23 File Offset: 0x00064F23
		public override PermissionSet Copy()
		{
			return new ReadOnlyPermissionSet(this.m_originXml);
		}

		// Token: 0x06001D7D RID: 7549 RVA: 0x00066D30 File Offset: 0x00064F30
		public override SecurityElement ToXml()
		{
			return this.m_originXml.Copy();
		}

		// Token: 0x06001D7E RID: 7550 RVA: 0x00066D3D File Offset: 0x00064F3D
		protected override IEnumerator GetEnumeratorImpl()
		{
			return new ReadOnlyPermissionSetEnumerator(base.GetEnumeratorImpl());
		}

		// Token: 0x06001D7F RID: 7551 RVA: 0x00066D4C File Offset: 0x00064F4C
		protected override IPermission GetPermissionImpl(Type permClass)
		{
			IPermission permissionImpl = base.GetPermissionImpl(permClass);
			if (permissionImpl == null)
			{
				return null;
			}
			return permissionImpl.Copy();
		}

		// Token: 0x06001D80 RID: 7552 RVA: 0x00066D6C File Offset: 0x00064F6C
		protected override IPermission AddPermissionImpl(IPermission perm)
		{
			throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ModifyROPermSet"));
		}

		// Token: 0x06001D81 RID: 7553 RVA: 0x00066D7D File Offset: 0x00064F7D
		public override void FromXml(SecurityElement et)
		{
			if (this.m_deserializing)
			{
				base.FromXml(et);
				return;
			}
			throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ModifyROPermSet"));
		}

		// Token: 0x06001D82 RID: 7554 RVA: 0x00066D9E File Offset: 0x00064F9E
		protected override IPermission RemovePermissionImpl(Type permClass)
		{
			throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ModifyROPermSet"));
		}

		// Token: 0x06001D83 RID: 7555 RVA: 0x00066DAF File Offset: 0x00064FAF
		protected override IPermission SetPermissionImpl(IPermission perm)
		{
			throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ModifyROPermSet"));
		}

		// Token: 0x04000A47 RID: 2631
		private SecurityElement m_originXml;

		// Token: 0x04000A48 RID: 2632
		[NonSerialized]
		private bool m_deserializing;
	}
}
