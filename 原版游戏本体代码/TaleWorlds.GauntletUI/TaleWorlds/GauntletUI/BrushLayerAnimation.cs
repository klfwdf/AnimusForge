using System;
using TaleWorlds.Library;

namespace TaleWorlds.GauntletUI
{
	// Token: 0x02000013 RID: 19
	public class BrushLayerAnimation
	{
		// Token: 0x1700005C RID: 92
		// (get) Token: 0x06000145 RID: 325 RVA: 0x0000709F File Offset: 0x0000529F
		// (set) Token: 0x06000146 RID: 326 RVA: 0x000070A7 File Offset: 0x000052A7
		public string LayerName { get; set; }

		// Token: 0x1700005D RID: 93
		// (get) Token: 0x06000147 RID: 327 RVA: 0x000070B0 File Offset: 0x000052B0
		public MBReadOnlyList<BrushAnimationProperty> Collections
		{
			get
			{
				return this._collections;
			}
		}

		// Token: 0x06000148 RID: 328 RVA: 0x000070B8 File Offset: 0x000052B8
		public BrushLayerAnimation()
		{
			this.LayerName = null;
			this._collections = new MBList<BrushAnimationProperty>();
		}

		// Token: 0x06000149 RID: 329 RVA: 0x000070D2 File Offset: 0x000052D2
		internal void RemoveAnimationProperty(BrushAnimationProperty property)
		{
			this._collections.Remove(property);
		}

		// Token: 0x0600014A RID: 330 RVA: 0x000070E1 File Offset: 0x000052E1
		public void AddAnimationProperty(BrushAnimationProperty property)
		{
			this._collections.Add(property);
		}

		// Token: 0x0600014B RID: 331 RVA: 0x000070F0 File Offset: 0x000052F0
		private void FillFrom(BrushLayerAnimation brushLayerAnimation)
		{
			this.LayerName = brushLayerAnimation.LayerName;
			this._collections = new MBList<BrushAnimationProperty>();
			foreach (BrushAnimationProperty brushAnimationProperty in brushLayerAnimation._collections)
			{
				BrushAnimationProperty item = brushAnimationProperty.Clone();
				this._collections.Add(item);
			}
		}

		// Token: 0x0600014C RID: 332 RVA: 0x00007164 File Offset: 0x00005364
		public BrushLayerAnimation Clone()
		{
			BrushLayerAnimation brushLayerAnimation = new BrushLayerAnimation();
			brushLayerAnimation.FillFrom(this);
			return brushLayerAnimation;
		}

		// Token: 0x04000072 RID: 114
		private MBList<BrushAnimationProperty> _collections;
	}
}
