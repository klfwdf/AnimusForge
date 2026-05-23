using System;
using Mono.Cecil.PE;

namespace Mono.Cecil
{
	// Token: 0x020001EF RID: 495
	internal sealed class DeferredModuleReader : ModuleReader
	{
		// Token: 0x060009BF RID: 2495 RVA: 0x0001F984 File Offset: 0x0001DB84
		public DeferredModuleReader(Image image)
			: base(image, ReadingMode.Deferred)
		{
		}

		// Token: 0x060009C0 RID: 2496 RVA: 0x0001F98E File Offset: 0x0001DB8E
		protected override void ReadModule()
		{
			this.module.Read<ModuleDefinition>(this.module, delegate(ModuleDefinition _, MetadataReader reader)
			{
				base.ReadModuleManifest(reader);
			});
		}

		// Token: 0x060009C1 RID: 2497 RVA: 0x0001B842 File Offset: 0x00019A42
		public override void ReadSymbols(ModuleDefinition module)
		{
		}
	}
}
