using System;

namespace Mono.Cecil
{
	// Token: 0x020001EA RID: 490
	internal sealed class AssemblyLinkedResource : Resource
	{
		// Token: 0x1700023A RID: 570
		// (get) Token: 0x0600097A RID: 2426 RVA: 0x0001EBCA File Offset: 0x0001CDCA
		// (set) Token: 0x0600097B RID: 2427 RVA: 0x0001EBD2 File Offset: 0x0001CDD2
		public AssemblyNameReference Assembly
		{
			get
			{
				return this.reference;
			}
			set
			{
				this.reference = value;
			}
		}

		// Token: 0x1700023B RID: 571
		// (get) Token: 0x0600097C RID: 2428 RVA: 0x0001EBDB File Offset: 0x0001CDDB
		public override ResourceType ResourceType
		{
			get
			{
				return ResourceType.AssemblyLinked;
			}
		}

		// Token: 0x0600097D RID: 2429 RVA: 0x0001EBDE File Offset: 0x0001CDDE
		public AssemblyLinkedResource(string name, ManifestResourceAttributes flags)
			: base(name, flags)
		{
		}

		// Token: 0x0600097E RID: 2430 RVA: 0x0001EBE8 File Offset: 0x0001CDE8
		public AssemblyLinkedResource(string name, ManifestResourceAttributes flags, AssemblyNameReference reference)
			: base(name, flags)
		{
			this.reference = reference;
		}

		// Token: 0x04000330 RID: 816
		private AssemblyNameReference reference;
	}
}
