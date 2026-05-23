using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Engine;

namespace SandBox.View.Map.Managers
{
	// Token: 0x02000071 RID: 113
	public abstract class EntityVisualManagerBase : CampaignEntityVisualComponent
	{
		// Token: 0x170000A5 RID: 165
		// (get) Token: 0x060004E4 RID: 1252 RVA: 0x00025D70 File Offset: 0x00023F70
		public Scene MapScene
		{
			get
			{
				if (this._mapScene == null && Campaign.Current != null && Campaign.Current.MapSceneWrapper != null)
				{
					this._mapScene = ((MapScene)Campaign.Current.MapSceneWrapper).Scene;
				}
				return this._mapScene;
			}
		}

		// Token: 0x0400022F RID: 559
		private Scene _mapScene;
	}
}
