using System;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace SandBox.View.Map
{
	// Token: 0x0200005B RID: 91
	public abstract class MapView : SandboxView
	{
		// Token: 0x1700005D RID: 93
		// (get) Token: 0x06000380 RID: 896 RVA: 0x0001CBC6 File Offset: 0x0001ADC6
		// (set) Token: 0x06000381 RID: 897 RVA: 0x0001CBCE File Offset: 0x0001ADCE
		public MapScreen MapScreen { get; internal set; }

		// Token: 0x1700005E RID: 94
		// (get) Token: 0x06000382 RID: 898 RVA: 0x0001CBD7 File Offset: 0x0001ADD7
		// (set) Token: 0x06000383 RID: 899 RVA: 0x0001CBDF File Offset: 0x0001ADDF
		public MapState MapState { get; internal set; }

		// Token: 0x06000384 RID: 900 RVA: 0x0001CBE8 File Offset: 0x0001ADE8
		protected internal virtual void CreateLayout()
		{
		}

		// Token: 0x06000385 RID: 901 RVA: 0x0001CBEA File Offset: 0x0001ADEA
		protected internal virtual void OnResume()
		{
		}

		// Token: 0x06000386 RID: 902 RVA: 0x0001CBEC File Offset: 0x0001ADEC
		protected internal virtual void OnHourlyTick()
		{
		}

		// Token: 0x06000387 RID: 903 RVA: 0x0001CBEE File Offset: 0x0001ADEE
		protected internal virtual void OnStartWait(string waitMenuId)
		{
		}

		// Token: 0x06000388 RID: 904 RVA: 0x0001CBF0 File Offset: 0x0001ADF0
		protected internal virtual void OnMainPartyEncounter()
		{
		}

		// Token: 0x06000389 RID: 905 RVA: 0x0001CBF2 File Offset: 0x0001ADF2
		protected internal virtual void OnDispersePlayerLeadedArmy()
		{
		}

		// Token: 0x0600038A RID: 906 RVA: 0x0001CBF4 File Offset: 0x0001ADF4
		protected internal virtual void OnArmyLeft()
		{
		}

		// Token: 0x0600038B RID: 907 RVA: 0x0001CBF6 File Offset: 0x0001ADF6
		protected internal virtual bool IsEscaped()
		{
			return false;
		}

		// Token: 0x0600038C RID: 908 RVA: 0x0001CBF9 File Offset: 0x0001ADF9
		protected internal virtual bool IsOpeningEscapeMenuOnFocusChangeAllowed()
		{
			return true;
		}

		// Token: 0x0600038D RID: 909 RVA: 0x0001CBFC File Offset: 0x0001ADFC
		protected internal virtual void OnOverlayCreated()
		{
		}

		// Token: 0x0600038E RID: 910 RVA: 0x0001CBFE File Offset: 0x0001ADFE
		protected internal virtual void OnOverlayClosed()
		{
		}

		// Token: 0x0600038F RID: 911 RVA: 0x0001CC00 File Offset: 0x0001AE00
		protected internal virtual void OnMenuModeTick(float dt)
		{
		}

		// Token: 0x06000390 RID: 912 RVA: 0x0001CC02 File Offset: 0x0001AE02
		protected internal virtual void OnMapScreenUpdate(float dt)
		{
		}

		// Token: 0x06000391 RID: 913 RVA: 0x0001CC04 File Offset: 0x0001AE04
		protected internal virtual void OnIdleTick(float dt)
		{
		}

		// Token: 0x06000392 RID: 914 RVA: 0x0001CC06 File Offset: 0x0001AE06
		protected internal virtual void OnMapTerrainClick()
		{
		}

		// Token: 0x06000393 RID: 915 RVA: 0x0001CC08 File Offset: 0x0001AE08
		protected internal virtual void OnSiegeEngineClick(MatrixFrame siegeEngineFrame)
		{
		}

		// Token: 0x06000394 RID: 916 RVA: 0x0001CC0A File Offset: 0x0001AE0A
		protected internal virtual void OnMapConversationStart()
		{
		}

		// Token: 0x06000395 RID: 917 RVA: 0x0001CC0C File Offset: 0x0001AE0C
		protected internal virtual void OnMapConversationOver()
		{
		}

		// Token: 0x06000396 RID: 918 RVA: 0x0001CC0E File Offset: 0x0001AE0E
		protected internal virtual TutorialContexts GetTutorialContext()
		{
			return TutorialContexts.MapWindow;
		}

		// Token: 0x040001DF RID: 479
		protected const float ContextAlphaModifier = 8.5f;
	}
}
