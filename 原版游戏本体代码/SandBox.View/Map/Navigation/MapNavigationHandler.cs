using System;
using System.Linq;
using SandBox.View.Map.Navigation.NavigationElements;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace SandBox.View.Map.Navigation
{
	// Token: 0x02000067 RID: 103
	public class MapNavigationHandler : INavigationHandler
	{
		// Token: 0x0600046A RID: 1130 RVA: 0x000244AF File Offset: 0x000226AF
		public INavigationElement[] GetElements()
		{
			return this._elements;
		}

		// Token: 0x17000086 RID: 134
		// (get) Token: 0x0600046B RID: 1131 RVA: 0x000244B7 File Offset: 0x000226B7
		// (set) Token: 0x0600046C RID: 1132 RVA: 0x000244BF File Offset: 0x000226BF
		public bool IsNavigationLocked { get; set; }

		// Token: 0x17000087 RID: 135
		// (get) Token: 0x0600046D RID: 1133 RVA: 0x000244C8 File Offset: 0x000226C8
		public bool IsEscapeMenuActive
		{
			get
			{
				return this._elements.Any(delegate(INavigationElement e)
				{
					EscapeMenuNavigationElement escapeMenuNavigationElement;
					return (escapeMenuNavigationElement = e as EscapeMenuNavigationElement) != null && escapeMenuNavigationElement.IsActive;
				});
			}
		}

		// Token: 0x0600046E RID: 1134 RVA: 0x000244F4 File Offset: 0x000226F4
		public MapNavigationHandler()
		{
			this._game = Game.Current;
			this._elements = this.OnCreateElements();
		}

		// Token: 0x0600046F RID: 1135 RVA: 0x00024514 File Offset: 0x00022714
		public bool IsAnyElementActive()
		{
			for (int i = 0; i < this._elements.Length; i++)
			{
				if (this._elements[i].IsActive)
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x06000470 RID: 1136 RVA: 0x00024548 File Offset: 0x00022748
		protected virtual INavigationElement[] OnCreateElements()
		{
			return new INavigationElement[]
			{
				new EscapeMenuNavigationElement(this),
				new CharacterDeveloperNavigationElement(this),
				new InventoryNavigationElement(this),
				new PartyNavigationElement(this),
				new QuestsNavigationElement(this),
				new ClanNavigationElement(this),
				new KingdomNavigationElement(this)
			};
		}

		// Token: 0x06000471 RID: 1137 RVA: 0x0002459C File Offset: 0x0002279C
		public INavigationElement GetElement(string id)
		{
			for (int i = 0; i < this._elements.Length; i++)
			{
				if (this._elements[i].StringId == id)
				{
					return this._elements[i];
				}
			}
			return null;
		}

		// Token: 0x04000229 RID: 553
		protected readonly Game _game;

		// Token: 0x0400022A RID: 554
		private INavigationElement[] _elements;
	}
}
