using System;
using SandBox.View.Map.Managers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace SandBox.View.Map.Visuals
{
	// Token: 0x02000065 RID: 101
	public class TrackVisual : MapEntityVisual<Track>
	{
		// Token: 0x06000451 RID: 1105 RVA: 0x000243F0 File Offset: 0x000225F0
		public TrackVisual(Track track)
			: base(track)
		{
		}

		// Token: 0x1700007C RID: 124
		// (get) Token: 0x06000452 RID: 1106 RVA: 0x000243F9 File Offset: 0x000225F9
		public override CampaignVec2 InteractionPositionForPlayer
		{
			get
			{
				return base.MapEntity.Position;
			}
		}

		// Token: 0x1700007D RID: 125
		// (get) Token: 0x06000453 RID: 1107 RVA: 0x00024406 File Offset: 0x00022606
		public override MapEntityVisual AttachedTo
		{
			get
			{
				return null;
			}
		}

		// Token: 0x06000454 RID: 1108 RVA: 0x00024409 File Offset: 0x00022609
		public override Vec3 GetVisualPosition()
		{
			return base.MapEntity.Position.AsVec3();
		}

		// Token: 0x06000455 RID: 1109 RVA: 0x0002441B File Offset: 0x0002261B
		public override bool IsVisibleOrFadingOut()
		{
			return base.MapEntity.IsDetected;
		}

		// Token: 0x06000456 RID: 1110 RVA: 0x00024428 File Offset: 0x00022628
		public override void OnHover()
		{
			InformationManager.ShowTooltip(typeof(Track), new object[] { base.MapEntity });
		}

		// Token: 0x06000457 RID: 1111 RVA: 0x00024448 File Offset: 0x00022648
		public override bool OnMapClick(bool followModifierUsed)
		{
			return false;
		}

		// Token: 0x06000458 RID: 1112 RVA: 0x0002444B File Offset: 0x0002264B
		public override void OnOpenEncyclopedia()
		{
		}

		// Token: 0x06000459 RID: 1113 RVA: 0x0002444D File Offset: 0x0002264D
		public override void ReleaseResources()
		{
			MapTracksVisualManager.Current.ReleaseResources(base.MapEntity);
		}

		// Token: 0x04000225 RID: 549
		private static TextObject _defaultTrackTitle = new TextObject("{=maptrack}Track", null);
	}
}
