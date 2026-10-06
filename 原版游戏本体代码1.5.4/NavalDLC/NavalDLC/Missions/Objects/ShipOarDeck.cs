using System;
using System.Collections.Generic;
using NavalDLC.Missions.ShipActuators;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.Missions.Objects;

public class ShipOarDeck : ScriptComponentBehavior
{
	public const string OarEntityName = "oar";

	public const string OarRetractedFrameEntityName = "retracted_frame";

	public const string RightOarMachinesHolderName = "right_oar_machines";

	public const string LeftOarMachinesHolderName = "left_oar_machines";

	public const string LeftOarGateTag = "oar_gate_left";

	public const string RightOarGateTag = "oar_gate_right";

	public const string HandTargetEntityName = "hand_position";

	public const string OarEntityTag = "oar_entity";

	public const string RetractedEntityTag = "retracted_entity";

	public const string HandTargetEntityTag = "hand_target_entity";

	public const string SeatLocationEntity = "seat_location_entity";

	public const string ShipBodyPhysicsEntityTag = "body_mesh";

	public const string SeatMeshTag = "seat_mesh_entity";

	[EditableScriptComponentVariable(true, "")]
	private float _verticalBaseAngle = 15f;

	[EditableScriptComponentVariable(true, "")]
	private float _lateralBaseAngle;

	[EditableScriptComponentVariable(true, "")]
	private float _verticalRotationAngle = 10f;

	[EditableScriptComponentVariable(true, "")]
	private float _lateralRotationAngle = 17.2f;

	private float _oarLength;

	private OarDeckParameters _oarDeckParameters;

	public OarDeckParameters GetParameters()
	{
		if (_oarDeckParameters == null)
		{
			_oarDeckParameters = new OarDeckParameters(_verticalBaseAngle * (System.MathF.PI / 180f), _lateralBaseAngle * (System.MathF.PI / 180f), _verticalRotationAngle * (System.MathF.PI / 180f), _lateralRotationAngle * (System.MathF.PI / 180f), _oarLength);
		}
		else
		{
			_oarDeckParameters.SetParameters(_verticalBaseAngle * (System.MathF.PI / 180f), _lateralBaseAngle * (System.MathF.PI / 180f), _verticalRotationAngle * (System.MathF.PI / 180f), _lateralRotationAngle * (System.MathF.PI / 180f), _oarLength);
		}
		return _oarDeckParameters;
	}

	protected override void OnInit()
	{
		base.OnInit();
		SetScriptComponentToTick(GetTickRequirement());
		UpdateOarLength();
		foreach (WeakGameEntity item in base.GameEntity.CollectChildrenEntitiesWithTag("seat_mesh_entity"))
		{
			WeakGameEntity firstChildEntityWithName = item.GetFirstChildEntityWithName("floor");
			if (firstChildEntityWithName != null)
			{
				firstChildEntityWithName.Remove(78);
			}
		}
	}

	internal void UpdateOarLength()
	{
		List<WeakGameEntity> list = base.GameEntity.CollectChildrenEntitiesWithTag("oar_gate_left");
		list.AddRange(base.GameEntity.CollectChildrenEntitiesWithTag("oar_gate_right"));
		if (list.Count > 0)
		{
			float num = -1f;
			foreach (WeakGameEntity item in list)
			{
				Mesh firstMesh = item.GetFirstMesh();
				WeakGameEntity weakGameEntity = item;
				if (firstMesh == null)
				{
					WeakGameEntity firstChildEntityWithTag = item.GetFirstChildEntityWithTag("upgrade_slot");
					if (firstChildEntityWithTag.ChildCount > 0)
					{
						WeakGameEntity weakGameEntity2 = firstChildEntityWithTag.GetFirstChildEntityWithTag("base");
						if (!weakGameEntity2.IsValid)
						{
							weakGameEntity2 = firstChildEntityWithTag.GetChild(0);
						}
						firstMesh = weakGameEntity2.GetFirstMesh();
						weakGameEntity = weakGameEntity2;
					}
				}
				if (!(firstMesh != null))
				{
					continue;
				}
				float num2 = float.MinValue;
				if (weakGameEntity.MultiMeshComponentCount == 0)
				{
					Vec3 boundingBoxMax = firstMesh.GetBoundingBoxMax();
					num2 = TaleWorlds.Library.MathF.Max(boundingBoxMax.x, boundingBoxMax.y, boundingBoxMax.z);
				}
				else
				{
					for (int i = 0; i < weakGameEntity.MultiMeshComponentCount; i++)
					{
						MetaMesh metaMesh = weakGameEntity.GetMetaMesh(i);
						for (int j = 0; j < metaMesh.MeshCount; j++)
						{
							Vec3 boundingBoxMax2 = metaMesh.GetMeshAtIndex(j).GetBoundingBoxMax();
							num2 = TaleWorlds.Library.MathF.Max(TaleWorlds.Library.MathF.Max(boundingBoxMax2.x, boundingBoxMax2.y, boundingBoxMax2.z), num2);
						}
					}
				}
				if (num >= 0f)
				{
					MBMath.ApproximatelyEquals(num2, num);
					num = TaleWorlds.Library.MathF.Max(num, num2);
				}
				else
				{
					num = num2;
				}
			}
			_oarLength = num;
		}
		else
		{
			_oarLength = 0f;
		}
	}

	public static WeakGameEntity GetOarEntity(WeakGameEntity oarScriptEntity)
	{
		WeakGameEntity result = oarScriptEntity.GetFirstChildEntityWithTag("oar_entity");
		if (!result.IsValid)
		{
			foreach (WeakGameEntity child in oarScriptEntity.GetChildren())
			{
				if (child.Name == "oar")
				{
					result = child;
				}
			}
		}
		return result;
	}

	public static void LoadOarScriptEntity(WeakGameEntity oarScriptEntity, out WeakGameEntity oarEntity, ref MatrixFrame oarExtractedEntitialFrame, ref MatrixFrame oarRetractedEntitialFrame, out WeakGameEntity handTargetEntity)
	{
		handTargetEntity = WeakGameEntity.Invalid;
		oarEntity = GetOarEntity(oarScriptEntity);
		WeakGameEntity weakGameEntity = oarScriptEntity.GetFirstChildEntityWithTag("retracted_entity");
		if (!oarEntity.IsValid)
		{
			return;
		}
		oarExtractedEntitialFrame = oarEntity.GetFrame();
		handTargetEntity = oarEntity.GetFirstChildEntityWithTag("hand_target_entity");
		if (weakGameEntity.IsValid)
		{
			oarRetractedEntitialFrame = weakGameEntity.GetFrame();
		}
		if (!handTargetEntity.IsValid)
		{
			foreach (WeakGameEntity child in oarEntity.GetChildren())
			{
				if (child.Name == "hand_position")
				{
					handTargetEntity = child;
				}
			}
		}
		if (!weakGameEntity.IsValid)
		{
			foreach (WeakGameEntity child2 in oarEntity.GetChildren())
			{
				if (child2.Name == "retracted_frame")
				{
					oarRetractedEntitialFrame = child2.GetFrame();
					weakGameEntity = child2;
				}
			}
		}
		if (weakGameEntity != null)
		{
			weakGameEntity.Remove(66);
		}
	}

	private static WeakGameEntity GetRetractedFrameEntity(WeakGameEntity oarMachine)
	{
		WeakGameEntity result = oarMachine.GetFirstChildEntityWithTag("retracted_entity");
		if (result.IsValid)
		{
			return result;
		}
		WeakGameEntity oarEntity = GetOarEntity(oarMachine);
		if (oarEntity.IsValid && !result.IsValid)
		{
			foreach (WeakGameEntity child in oarEntity.GetChildren())
			{
				if (child.Name == "retracted_frame")
				{
					result = child;
				}
			}
		}
		return result;
	}
}
