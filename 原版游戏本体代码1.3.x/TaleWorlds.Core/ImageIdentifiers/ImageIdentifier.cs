using System;

namespace TaleWorlds.Core.ImageIdentifiers
{
	// Token: 0x020000E7 RID: 231
	public abstract class ImageIdentifier
	{
		// Token: 0x170003E3 RID: 995
		// (get) Token: 0x06000B81 RID: 2945 RVA: 0x0002507D File Offset: 0x0002327D
		// (set) Token: 0x06000B82 RID: 2946 RVA: 0x00025085 File Offset: 0x00023285
		public string Id { get; set; }

		// Token: 0x170003E4 RID: 996
		// (get) Token: 0x06000B83 RID: 2947 RVA: 0x0002508E File Offset: 0x0002328E
		// (set) Token: 0x06000B84 RID: 2948 RVA: 0x00025096 File Offset: 0x00023296
		public string TextureProviderName { get; protected set; }

		// Token: 0x170003E5 RID: 997
		// (get) Token: 0x06000B85 RID: 2949 RVA: 0x0002509F File Offset: 0x0002329F
		// (set) Token: 0x06000B86 RID: 2950 RVA: 0x000250A7 File Offset: 0x000232A7
		public string AdditionalArgs { get; protected set; }

		// Token: 0x06000B87 RID: 2951 RVA: 0x000250B0 File Offset: 0x000232B0
		public bool Equals(ImageIdentifier other)
		{
			return other != null && this.Id.Equals(other.Id) && this.AdditionalArgs.Equals(other.AdditionalArgs) && this.TextureProviderName.Equals(other.TextureProviderName);
		}
	}
}
