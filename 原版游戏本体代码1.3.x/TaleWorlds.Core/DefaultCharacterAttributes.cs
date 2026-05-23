using System;
using TaleWorlds.Localization;

namespace TaleWorlds.Core
{
	// Token: 0x0200004F RID: 79
	public class DefaultCharacterAttributes
	{
		// Token: 0x17000226 RID: 550
		// (get) Token: 0x06000655 RID: 1621 RVA: 0x00015F32 File Offset: 0x00014132
		private static DefaultCharacterAttributes Instance
		{
			get
			{
				return Game.Current.DefaultCharacterAttributes;
			}
		}

		// Token: 0x17000227 RID: 551
		// (get) Token: 0x06000656 RID: 1622 RVA: 0x00015F3E File Offset: 0x0001413E
		public static CharacterAttribute Vigor
		{
			get
			{
				return DefaultCharacterAttributes.Instance._vigor;
			}
		}

		// Token: 0x17000228 RID: 552
		// (get) Token: 0x06000657 RID: 1623 RVA: 0x00015F4A File Offset: 0x0001414A
		public static CharacterAttribute Control
		{
			get
			{
				return DefaultCharacterAttributes.Instance._control;
			}
		}

		// Token: 0x17000229 RID: 553
		// (get) Token: 0x06000658 RID: 1624 RVA: 0x00015F56 File Offset: 0x00014156
		public static CharacterAttribute Endurance
		{
			get
			{
				return DefaultCharacterAttributes.Instance._endurance;
			}
		}

		// Token: 0x1700022A RID: 554
		// (get) Token: 0x06000659 RID: 1625 RVA: 0x00015F62 File Offset: 0x00014162
		public static CharacterAttribute Cunning
		{
			get
			{
				return DefaultCharacterAttributes.Instance._cunning;
			}
		}

		// Token: 0x1700022B RID: 555
		// (get) Token: 0x0600065A RID: 1626 RVA: 0x00015F6E File Offset: 0x0001416E
		public static CharacterAttribute Social
		{
			get
			{
				return DefaultCharacterAttributes.Instance._social;
			}
		}

		// Token: 0x1700022C RID: 556
		// (get) Token: 0x0600065B RID: 1627 RVA: 0x00015F7A File Offset: 0x0001417A
		public static CharacterAttribute Intelligence
		{
			get
			{
				return DefaultCharacterAttributes.Instance._intelligence;
			}
		}

		// Token: 0x0600065C RID: 1628 RVA: 0x00015F86 File Offset: 0x00014186
		private CharacterAttribute Create(string stringId)
		{
			return Game.Current.ObjectManager.RegisterPresumedObject<CharacterAttribute>(new CharacterAttribute(stringId));
		}

		// Token: 0x0600065D RID: 1629 RVA: 0x00015F9D File Offset: 0x0001419D
		internal DefaultCharacterAttributes()
		{
			this.RegisterAll();
		}

		// Token: 0x0600065E RID: 1630 RVA: 0x00015FAC File Offset: 0x000141AC
		private void RegisterAll()
		{
			this._vigor = this.Create("vigor");
			this._control = this.Create("control");
			this._endurance = this.Create("endurance");
			this._cunning = this.Create("cunning");
			this._social = this.Create("social");
			this._intelligence = this.Create("intelligence");
			this.InitializeAll();
		}

		// Token: 0x0600065F RID: 1631 RVA: 0x00016028 File Offset: 0x00014228
		private void InitializeAll()
		{
			this._vigor.Initialize(new TextObject("{=YWkdD7Ki}Vigor", null), new TextObject("{=jJ9sLOLb}Vigor represents the ability to move with speed and force. It's important for melee combat.", null), new TextObject("{=Ve8xoa3i}VIG", null));
			this._control.Initialize(new TextObject("{=controlskill}Control", null), new TextObject("{=vx0OCvaj}Control represents the ability to use strength without sacrificing precision. It's necessary for using ranged weapons.", null), new TextObject("{=HuXafdmR}CTR", null));
			this._endurance.Initialize(new TextObject("{=kvOavzcs}Endurance", null), new TextObject("{=K8rCOQUZ}Endurance is the ability to perform taxing physical activity for a long time.", null), new TextObject("{=d2ApwXJr}END", null));
			this._cunning.Initialize(new TextObject("{=JZM1mQvb}Cunning", null), new TextObject("{=YO5LUfiO}Cunning is the ability to predict what other people will do, and to outwit their plans.", null), new TextObject("{=tH6Ooj0P}CNG", null));
			this._social.Initialize(new TextObject("{=socialskill}Social", null), new TextObject("{=XMDTt96y}Social is the ability to understand people's motivations and to sway them.", null), new TextObject("{=PHoxdReD}SOC", null));
			this._intelligence.Initialize(new TextObject("{=sOrJoxiC}Intelligence", null), new TextObject("{=TeUtEGV0}Intelligence represents aptitude for reading and theoretical learning.", null), new TextObject("{=Bn7IsMpu}INT", null));
		}

		// Token: 0x04000308 RID: 776
		private CharacterAttribute _control;

		// Token: 0x04000309 RID: 777
		private CharacterAttribute _vigor;

		// Token: 0x0400030A RID: 778
		private CharacterAttribute _endurance;

		// Token: 0x0400030B RID: 779
		private CharacterAttribute _cunning;

		// Token: 0x0400030C RID: 780
		private CharacterAttribute _social;

		// Token: 0x0400030D RID: 781
		private CharacterAttribute _intelligence;
	}
}
