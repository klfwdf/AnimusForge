using System;
using System.Threading;
using Mono.Collections.Generic;

namespace Mono.Cecil
{
	// Token: 0x02000297 RID: 663
	internal sealed class SecurityDeclaration
	{
		// Token: 0x1700044E RID: 1102
		// (get) Token: 0x06001078 RID: 4216 RVA: 0x0003206B File Offset: 0x0003026B
		// (set) Token: 0x06001079 RID: 4217 RVA: 0x00032073 File Offset: 0x00030273
		public SecurityAction Action
		{
			get
			{
				return this.action;
			}
			set
			{
				this.action = value;
			}
		}

		// Token: 0x1700044F RID: 1103
		// (get) Token: 0x0600107A RID: 4218 RVA: 0x0003207C File Offset: 0x0003027C
		public bool HasSecurityAttributes
		{
			get
			{
				this.Resolve();
				return !this.security_attributes.IsNullOrEmpty<SecurityAttribute>();
			}
		}

		// Token: 0x17000450 RID: 1104
		// (get) Token: 0x0600107B RID: 4219 RVA: 0x00032092 File Offset: 0x00030292
		public Collection<SecurityAttribute> SecurityAttributes
		{
			get
			{
				this.Resolve();
				if (this.security_attributes == null)
				{
					Interlocked.CompareExchange<Collection<SecurityAttribute>>(ref this.security_attributes, new Collection<SecurityAttribute>(), null);
				}
				return this.security_attributes;
			}
		}

		// Token: 0x17000451 RID: 1105
		// (get) Token: 0x0600107C RID: 4220 RVA: 0x000320BA File Offset: 0x000302BA
		internal bool HasImage
		{
			get
			{
				return this.module != null && this.module.HasImage;
			}
		}

		// Token: 0x0600107D RID: 4221 RVA: 0x000320D1 File Offset: 0x000302D1
		internal SecurityDeclaration(SecurityAction action, uint signature, ModuleDefinition module)
		{
			this.action = action;
			this.signature = signature;
			this.module = module;
		}

		// Token: 0x0600107E RID: 4222 RVA: 0x000320EE File Offset: 0x000302EE
		public SecurityDeclaration(SecurityAction action)
		{
			this.action = action;
			this.resolved = true;
		}

		// Token: 0x0600107F RID: 4223 RVA: 0x00032104 File Offset: 0x00030304
		public SecurityDeclaration(SecurityAction action, byte[] blob)
		{
			this.action = action;
			this.resolved = false;
			this.blob = blob;
		}

		// Token: 0x06001080 RID: 4224 RVA: 0x00032124 File Offset: 0x00030324
		public byte[] GetBlob()
		{
			if (this.blob != null)
			{
				return this.blob;
			}
			if (!this.HasImage || this.signature == 0U)
			{
				throw new NotSupportedException();
			}
			return this.module.Read<SecurityDeclaration, byte[]>(ref this.blob, this, (SecurityDeclaration declaration, MetadataReader reader) => reader.ReadSecurityDeclarationBlob(declaration.signature));
		}

		// Token: 0x06001081 RID: 4225 RVA: 0x00032188 File Offset: 0x00030388
		private void Resolve()
		{
			if (this.resolved || !this.HasImage)
			{
				return;
			}
			object syncRoot = this.module.SyncRoot;
			lock (syncRoot)
			{
				if (!this.resolved)
				{
					this.module.Read<SecurityDeclaration>(this, delegate(SecurityDeclaration declaration, MetadataReader reader)
					{
						reader.ReadSecurityDeclarationSignature(declaration);
					});
					this.resolved = true;
				}
			}
		}

		// Token: 0x04000589 RID: 1417
		internal readonly uint signature;

		// Token: 0x0400058A RID: 1418
		private byte[] blob;

		// Token: 0x0400058B RID: 1419
		private readonly ModuleDefinition module;

		// Token: 0x0400058C RID: 1420
		internal bool resolved;

		// Token: 0x0400058D RID: 1421
		private SecurityAction action;

		// Token: 0x0400058E RID: 1422
		internal Collection<SecurityAttribute> security_attributes;
	}
}
