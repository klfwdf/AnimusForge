using System;
using System.Xml;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.Core
{
	// Token: 0x0200009C RID: 156
	public class MBCharacterSkills : MBObjectBase
	{
		// Token: 0x17000317 RID: 791
		// (get) Token: 0x060008EE RID: 2286 RVA: 0x0001D680 File Offset: 0x0001B880
		// (set) Token: 0x060008EF RID: 2287 RVA: 0x0001D688 File Offset: 0x0001B888
		public PropertyOwner<SkillObject> Skills { get; private set; }

		// Token: 0x060008F0 RID: 2288 RVA: 0x0001D691 File Offset: 0x0001B891
		public MBCharacterSkills()
		{
			this.Skills = new PropertyOwner<SkillObject>();
		}

		// Token: 0x060008F1 RID: 2289 RVA: 0x0001D6A4 File Offset: 0x0001B8A4
		public void Init(MBObjectManager objectManager, XmlNode node)
		{
			base.Initialize();
			this.Skills.Deserialize(objectManager, node);
			base.AfterInitialized();
		}

		// Token: 0x060008F2 RID: 2290 RVA: 0x0001D6BF File Offset: 0x0001B8BF
		public override void Deserialize(MBObjectManager objectManager, XmlNode node)
		{
			base.Deserialize(objectManager, node);
			this.Skills.Deserialize(objectManager, node);
		}
	}
}
