using System;
using System.Collections.Generic;
using Helpers;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace SandBox.Missions
{
	// Token: 0x0200005B RID: 91
	public class CivilianPortShipSpawnMissionLogic : MissionLogic
	{
		// Token: 0x0600038B RID: 907 RVA: 0x00014994 File Offset: 0x00012B94
		public CivilianPortShipSpawnMissionLogic(List<Ship> mainPartyShips, List<Ship> townLordShips)
		{
			this._mainPartyShips = mainPartyShips;
			this._townLordShips = townLordShips;
		}

		// Token: 0x0600038C RID: 908 RVA: 0x000149E4 File Offset: 0x00012BE4
		public override void OnBehaviorInitialize()
		{
			base.OnBehaviorInitialize();
			foreach (GameEntity item in Mission.Current.Scene.FindEntitiesWithTag("shipyard_ship"))
			{
				this._shipyardShipSpawnPoints.Enqueue(item);
			}
		}

		// Token: 0x0600038D RID: 909 RVA: 0x00014A4C File Offset: 0x00012C4C
		public override void EarlyStart()
		{
			base.EarlyStart();
			if (!this._shipyardShipSpawnPoints.IsEmpty<GameEntity>())
			{
				if (!this._mainPartyShips.IsEmpty<Ship>())
				{
					Ship randomElement = this._mainPartyShips.GetRandomElement<Ship>();
					this.SpawnShip(randomElement);
				}
				while (!this._shipyardShipSpawnPoints.IsEmpty<GameEntity>() && !this._townLordShips.IsEmpty<Ship>())
				{
					Ship randomElement2 = this._townLordShips.GetRandomElement<Ship>();
					this._townLordShips.Remove(randomElement2);
					this.SpawnShip(randomElement2);
				}
			}
		}

		// Token: 0x0600038E RID: 910 RVA: 0x00014AC8 File Offset: 0x00012CC8
		public override void OnMissionTick(float dt)
		{
			base.OnMissionTick(dt);
			foreach (KeyValuePair<GameEntity, MatrixFrame> keyValuePair in this._spawnedShipVisuals)
			{
				GameEntity key = keyValuePair.Key;
				MatrixFrame value = keyValuePair.Value;
				this.TickShipAnimation(dt, key, value);
			}
		}

		// Token: 0x0600038F RID: 911 RVA: 0x00014B34 File Offset: 0x00012D34
		private void SpawnShip(Ship ship)
		{
			MissionShipObject @object = MBObjectManager.Instance.GetObject<MissionShipObject>(ship.ShipHull.MissionShipObjectId);
			ValueTuple<uint, uint> sailColors = ShipHelper.GetSailColors(ship, null);
			uint item = sailColors.Item1;
			uint item2 = sailColors.Item2;
			GameEntity gameEntity = VisualShipFactory.CreateVisualShip(@object.Prefab, base.Mission.Scene, ship.GetShipVisualSlotInfos(), ship.RandomValue, ship.HitPoints / ship.MaxSailHitPoints, item, item2, true);
			MatrixFrame globalFrame = this._shipyardShipSpawnPoints.Dequeue().GetGlobalFrame();
			float waterLevelAtPosition = base.Mission.Scene.GetWaterLevelAtPosition(globalFrame.origin.AsVec2, true, true);
			globalFrame.origin.z = waterLevelAtPosition;
			if (gameEntity != null)
			{
				gameEntity.SetFrame(ref globalFrame, true);
			}
			this._spawnedShipVisuals.Add(gameEntity, globalFrame);
		}

		// Token: 0x06000390 RID: 912 RVA: 0x00014BF4 File Offset: 0x00012DF4
		private void TickShipAnimation(float dt, GameEntity shipVisualEntity, in MatrixFrame initialFrame)
		{
			if (shipVisualEntity == null)
			{
				return;
			}
			MatrixFrame frame = shipVisualEntity.GetFrame();
			Vec3 vec = shipVisualEntity.GetBoundingBoxMin() + new Vec3(5f, 5f, 0f, -1f);
			Vec3 vec2 = shipVisualEntity.GetBoundingBoxMax() - new Vec3(5f, 5f, 0f, -1f);
			Vec2[] array = new Vec2[32];
			for (int i = 0; i < 4; i++)
			{
				float amount = (float)i / 3f;
				float x = MathF.Lerp(vec.x, vec2.x, amount, 1E-05f);
				for (int j = 0; j < 8; j++)
				{
					float amount2 = (float)j / 7f;
					float y = MathF.Lerp(vec.y, vec2.y, amount2, 1E-05f);
					Vec3 vec3 = frame.origin + new Vec3(x, y, 0f, -1f);
					int num = i * 8 + j;
					array[num] = vec3.AsVec2;
				}
			}
			Vec3 vec4 = Vec3.Zero;
			float num2 = 0f;
			float[] array2 = new float[array.Length];
			Vec3[] array3 = new Vec3[array.Length];
			base.Mission.Scene.GetBulkWaterLevelAtPositions(array, ref array2, ref array3);
			for (int k = 0; k < array3.Length; k++)
			{
				Vec3 v = array3[k];
				vec4 += v;
				num2 += array2[k];
			}
			vec4.Normalize();
			num2 /= (float)array3.Length;
			MatrixFrame matrixFrame = initialFrame;
			matrixFrame.origin.z = num2 + 0.5f;
			Mat3 identity = Mat3.Identity;
			identity.u = vec4;
			identity.u.Normalize();
			identity.s = Vec3.CrossProduct(Vec3.Forward, identity.u);
			identity.s.Normalize();
			identity.f = Vec3.CrossProduct(identity.u, identity.s);
			identity.f.Normalize();
			matrixFrame.rotation = identity;
			MatrixFrame matrixFrame2 = MatrixFrame.Slerp(frame, matrixFrame, dt * 1.5f);
			shipVisualEntity.SetFrame(ref matrixFrame2, true);
		}

		// Token: 0x040001CF RID: 463
		private const string ShipyardShipSpawnPointTag = "shipyard_ship";

		// Token: 0x040001D0 RID: 464
		private Queue<GameEntity> _shipyardShipSpawnPoints = new Queue<GameEntity>();

		// Token: 0x040001D1 RID: 465
		private List<Ship> _mainPartyShips = new List<Ship>();

		// Token: 0x040001D2 RID: 466
		private List<Ship> _townLordShips = new List<Ship>();

		// Token: 0x040001D3 RID: 467
		private Dictionary<GameEntity, MatrixFrame> _spawnedShipVisuals = new Dictionary<GameEntity, MatrixFrame>();
	}
}
