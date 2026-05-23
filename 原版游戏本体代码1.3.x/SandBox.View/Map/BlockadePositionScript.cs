using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace SandBox.View.Map
{
	// Token: 0x0200003D RID: 61
	public class BlockadePositionScript : ScriptComponentBehavior
	{
		// Token: 0x060001F4 RID: 500 RVA: 0x00013A73 File Offset: 0x00011C73
		protected override void OnEditorTick(float dt)
		{
			if (this.IsVisualizationEnabled)
			{
				this.VisualizeArcs();
			}
		}

		// Token: 0x060001F5 RID: 501 RVA: 0x00013A84 File Offset: 0x00011C84
		private void VisualizeArcs()
		{
			if (this._pointsOfArcs == null || !this.IsRandomizationEnabled)
			{
				this._pointsOfArcs = this.GetBlockadeArc(this.MaximumNumberOfShips, out this._center);
			}
			if (this._pointsOfArcs != null)
			{
				foreach (List<Vec3> list in this._pointsOfArcs)
				{
					foreach (Vec3 vec in list)
					{
					}
				}
			}
		}

		// Token: 0x060001F6 RID: 502 RVA: 0x00013B34 File Offset: 0x00011D34
		protected override void OnEditorVariableChanged(string variableName)
		{
			base.OnEditorVariableChanged(variableName);
			if (variableName == "RefreshVisualization")
			{
				this._pointsOfArcs = this.GetBlockadeArc(this.MaximumNumberOfShips, out this._center);
				if (!this._shipEntities.IsEmpty<GameEntity>())
				{
					Utilities.DeleteEntitiesInEditorScene(this._shipEntities);
				}
				this._shipEntities.Clear();
				if (this.IsShipVisualizationEnabled)
				{
					foreach (List<Vec3> list in this._pointsOfArcs)
					{
						foreach (Vec3 origin in list)
						{
							Vec2 vec = origin.AsVec2 - this._center.AsVec2;
							MatrixFrame identity = MatrixFrame.Identity;
							identity.origin = origin;
							float num = vec.AngleBetween(identity.rotation.f.AsVec2);
							identity.Rotate(1.5707964f - num, Vec3.Up);
							identity.rotation.ApplyScaleLocal(this.ShipScaleFactor);
							GameEntity gameEntity = TaleWorlds.Engine.GameEntity.Instantiate(base.GameEntity.Scene, this.MissionShipId, false, true, "");
							if (gameEntity == null)
							{
								break;
							}
							gameEntity.SetFrame(ref identity, true);
							this._shipEntities.Add(gameEntity);
						}
					}
				}
			}
		}

		// Token: 0x060001F7 RID: 503 RVA: 0x00013CCC File Offset: 0x00011ECC
		public List<List<Vec3>> GetBlockadeArc(int totalNumberOfShips, out Vec3 center)
		{
			int num = this.MaximumNumberOfShips;
			if (totalNumberOfShips < num)
			{
				num = totalNumberOfShips;
			}
			List<List<Vec3>> list = new List<List<Vec3>>();
			WeakGameEntity firstChildEntityWithTag = base.GameEntity.GetFirstChildEntityWithTag("Blockade_Arc_Start");
			WeakGameEntity firstChildEntityWithTag2 = base.GameEntity.GetFirstChildEntityWithTag("Blockade_Arc_End");
			center = Vec3.Invalid;
			if (firstChildEntityWithTag == null || firstChildEntityWithTag2 == null)
			{
				return list;
			}
			center = this.FindCenterOfCircle(firstChildEntityWithTag2.GlobalPosition, firstChildEntityWithTag.GlobalPosition);
			Vec3 vec = firstChildEntityWithTag2.GlobalPosition - center;
			vec.Normalize();
			Vec3 v = vec;
			int i = this.NumberOfArcs;
			float f = firstChildEntityWithTag2.GlobalPosition.Distance(center) / (float)this.NumberOfArcs;
			int num2 = 0;
			float num3 = this.DistanceBetweenShips;
			while (i > 0)
			{
				int num4 = MathF.Round(this.Angle * (float)i / this.DistanceBetweenShips);
				if (num - num2 < num4)
				{
					num3 *= (float)num4 / (float)(num - num2);
					num4 = num - num2;
				}
				v.RotateAboutZ(num3 / (float)(i * 2));
				List<Vec3> list2 = new List<Vec3>();
				for (int j = 0; j < num4; j++)
				{
					float a = MBRandom.RandomFloatRanged(-this.DistanceRandomizationOnArcs, this.DistanceRandomizationOnArcs);
					float f2 = MBRandom.RandomFloatRanged(0f, this.DistanceRandomizationBetweenArcs);
					Vec3 vec2 = center + v * (float)i * f;
					v.RotateAboutZ(num3 / (float)i);
					if (this.IsRandomizationEnabled)
					{
						vec2 += v * f2;
						v.RotateAboutZ(a);
					}
					list2.Add(vec2);
					num2++;
					if (num2 >= num)
					{
						break;
					}
				}
				list.Add(list2);
				if (num2 >= num)
				{
					return list;
				}
				v = vec;
				i--;
			}
			return list;
		}

		// Token: 0x060001F8 RID: 504 RVA: 0x00013EB4 File Offset: 0x000120B4
		private Vec3 FindCenterOfCircle(Vec3 arcPointStart, Vec3 arcPointEnd)
		{
			Vec3 v = arcPointEnd + (arcPointStart - arcPointEnd) / 2f;
			Vec3 vec = (arcPointStart - arcPointEnd) / 2f;
			float num = arcPointEnd.Distance(v);
			float num2 = num / MathF.Tan(this.Angle / 2f);
			return new Vec3(v.X + num2 * vec.Y / num, v.Y - num2 * vec.X / num, arcPointStart.Z, -1f);
		}

		// Token: 0x04000110 RID: 272
		public int MaximumNumberOfShips = 12;

		// Token: 0x04000111 RID: 273
		public int NumberOfArcs = 4;

		// Token: 0x04000112 RID: 274
		public float DistanceBetweenShips = 0.7853982f;

		// Token: 0x04000113 RID: 275
		public float DistanceRandomizationOnArcs = 0.1f;

		// Token: 0x04000114 RID: 276
		public float DistanceRandomizationBetweenArcs = 0.1f;

		// Token: 0x04000115 RID: 277
		public float Angle = 1.5707964f;

		// Token: 0x04000116 RID: 278
		public string MissionShipId = "dromon_ship_nested";

		// Token: 0x04000117 RID: 279
		public float ShipScaleFactor = 0.052f;

		// Token: 0x04000118 RID: 280
		public bool IsVisualizationEnabled;

		// Token: 0x04000119 RID: 281
		public bool IsRandomizationEnabled;

		// Token: 0x0400011A RID: 282
		public bool IsShipVisualizationEnabled;

		// Token: 0x0400011B RID: 283
		public SimpleButton RefreshVisualization;

		// Token: 0x0400011C RID: 284
		private List<List<Vec3>> _pointsOfArcs;

		// Token: 0x0400011D RID: 285
		private Vec3 _center;

		// Token: 0x0400011E RID: 286
		private List<GameEntity> _shipEntities = new List<GameEntity>();
	}
}
