using System;
using TaleWorlds.Core.ImageIdentifiers;

namespace TaleWorlds.Core.ViewModelCollection.ImageIdentifiers
{
	// Token: 0x02000020 RID: 32
	public class GenericImageIdentifierVM : ImageIdentifierVM
	{
		// Token: 0x0600019C RID: 412 RVA: 0x000059E7 File Offset: 0x00003BE7
		public GenericImageIdentifierVM(ImageIdentifier imageIdentifier)
		{
			if (imageIdentifier == null)
			{
				base.ImageIdentifier = new EmptyImageIdentifier();
				return;
			}
			base.ImageIdentifier = imageIdentifier;
		}
	}
}
