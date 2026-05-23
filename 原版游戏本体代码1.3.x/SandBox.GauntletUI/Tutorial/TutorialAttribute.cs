using System;

namespace SandBox.GauntletUI.Tutorial
{
	// Token: 0x02000016 RID: 22
	public class TutorialAttribute : Attribute
	{
		// Token: 0x06000137 RID: 311 RVA: 0x0000A0E1 File Offset: 0x000082E1
		public TutorialAttribute(string tutorialIdentifier)
		{
			this.TutorialIdentifier = tutorialIdentifier;
		}

		// Token: 0x0400006C RID: 108
		public readonly string TutorialIdentifier;
	}
}
