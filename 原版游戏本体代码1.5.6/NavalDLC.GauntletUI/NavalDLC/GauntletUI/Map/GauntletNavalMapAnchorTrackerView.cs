using NavalDLC.View.Map;
using NavalDLC.ViewModelCollection.Map;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.ScreenSystem;

namespace NavalDLC.GauntletUI.Map;

[OverrideView(typeof(NavalMapAnchorTrackerView))]
public class GauntletNavalMapAnchorTrackerView : MapView
{
	private GauntletLayer _gauntletLayer;

	private MapAnchorTrackerVM _dataSource;

	protected override void OnMapConversationStart()
	{
		base.OnMapConversationStart();
		if (_gauntletLayer != null)
		{
			ScreenManager.SetSuspendLayer(_gauntletLayer, isSuspended: true);
		}
	}

	protected override void OnMapConversationOver()
	{
		base.OnMapConversationOver();
		if (_gauntletLayer != null)
		{
			ScreenManager.SetSuspendLayer(_gauntletLayer, isSuspended: false);
		}
	}

	protected override void CreateLayout()
	{
		base.CreateLayout();
		_dataSource = new MapAnchorTrackerVM(OnMoveCameraToAnchor);
		_gauntletLayer = new GauntletLayer("NavalAnchorTracker", 15);
		base.Layer = _gauntletLayer;
		_gauntletLayer.InputRestrictions.SetInputRestrictions(isMouseVisible: false, InputUsageMask.Mouse);
		_gauntletLayer.LoadMovie("AnchorTracker", _dataSource);
		base.MapScreen.AddLayer(base.Layer);
	}

	private void OnMoveCameraToAnchor()
	{
		MobileParty mainParty = MobileParty.MainParty;
		if (mainParty != null && mainParty.Anchor?.IsValid == true)
		{
			base.MapScreen.FastMoveCameraToPosition(MobileParty.MainParty.Anchor.Position);
		}
	}

	protected override void OnFinalize()
	{
		base.OnFinalize();
		_dataSource.OnFinalize();
		base.MapScreen.RemoveLayer(base.Layer);
	}

	protected override void OnMapScreenUpdate(float dt)
	{
		base.OnMapScreenUpdate(dt);
		AnchorPoint anchor = MobileParty.MainParty.Anchor;
		float seeingRange = MobileParty.MainParty.SeeingRange;
		float num = anchor.Position.Distance(MobileParty.MainParty.Position);
		float num2 = base.MapScreen.MapCameraView.Camera.Position.Distance(MobileParty.MainParty.GetPositionAsVec3());
		float screenX = -5000f;
		float screenY = -5000f;
		float screenW = -5000f;
		if (anchor != null && anchor.IsValid && !anchor.IsDisabled && (num > seeingRange || num2 >= 110f))
		{
			GetAnchorScreenPosition(anchor, out screenX, out screenY, out screenW);
		}
		_dataSource.IsVisible = screenW >= 0f;
		_dataSource.PositionX = screenX;
		_dataSource.PositionY = screenY;
		_dataSource.PositionW = screenW;
	}

	private void GetAnchorScreenPosition(AnchorPoint anchor, out float screenX, out float screenY, out float screenW)
	{
		Vec3 position = anchor.GetPosition();
		screenX = -5000f;
		screenY = -5000f;
		screenW = -1f;
		MBWindowManager.WorldToScreenInsideUsableArea(base.MapScreen.MapCameraView.Camera, position, ref screenX, ref screenY, ref screenW);
	}
}
