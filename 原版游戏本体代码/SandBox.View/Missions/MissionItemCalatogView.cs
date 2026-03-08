using System;
using SandBox.Missions.MissionLogics;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace SandBox.View.Missions
{
	// Token: 0x0200001C RID: 28
	public class MissionItemCalatogView : MissionView
	{
		// Token: 0x060000C4 RID: 196 RVA: 0x00009C64 File Offset: 0x00007E64
		public override void AfterStart()
		{
			base.AfterStart();
			this._itemCatalogController = base.Mission.GetMissionBehavior<ItemCatalogController>();
			this._itemCatalogController.BeforeCatalogTick += this.OnBeforeCatalogTick;
			this._itemCatalogController.AfterCatalogTick += this.OnAfterCatalogTick;
		}

		// Token: 0x060000C5 RID: 197 RVA: 0x00009CB6 File Offset: 0x00007EB6
		private void OnBeforeCatalogTick(int currentItemIndex)
		{
			Utilities.TakeScreenshot("ItemCatalog/" + this._itemCatalogController.AllItems[currentItemIndex - 1].Name + ".bmp");
		}

		// Token: 0x060000C6 RID: 198 RVA: 0x00009CE4 File Offset: 0x00007EE4
		private void OnAfterCatalogTick()
		{
			MatrixFrame frame = default(MatrixFrame);
			Vec3 lookDirection = base.Mission.MainAgent.LookDirection;
			frame.origin = base.Mission.MainAgent.Position + lookDirection * 2f + new Vec3(0f, 0f, 1.273f, -1f);
			frame.rotation.u = lookDirection;
			frame.rotation.s = new Vec3(1f, 0f, 0f, -1f);
			frame.rotation.f = new Vec3(0f, 0f, 1f, -1f);
			frame.rotation.Orthonormalize();
			base.Mission.SetCameraFrame(ref frame, 1f);
			Camera camera = Camera.CreateCamera();
			camera.Frame = frame;
			base.MissionScreen.CustomCamera = camera;
		}

		// Token: 0x0400007A RID: 122
		private ItemCatalogController _itemCatalogController;
	}
}
