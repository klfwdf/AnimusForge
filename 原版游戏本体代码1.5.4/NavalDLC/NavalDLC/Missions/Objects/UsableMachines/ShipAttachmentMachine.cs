using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using NavalDLC.Missions.AI.TeamAI;
using NavalDLC.Missions.AI.UsableMachineAIs;
using NavalDLC.Missions.MissionLogics;
using NavalDLC.Missions.NavalPhysics;
using TaleWorlds.Core;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Objects.Usables;

namespace NavalDLC.Missions.Objects.UsableMachines;

public class ShipAttachmentMachine : UsableMachine
{
	public class ShipBridgeNavmeshHolder : MissionObject
	{
		private const float StepWidth = 0.8f;

		private Vec3 _startLeftPosition;

		private Vec3 _startRightPosition;

		private Vec3 _endLeftPosition;

		private Vec3 _endRightPosition;

		private int[] _customVertexIndices;

		private Vec3[] _bridgeCustomVertexPositionsArray;

		private PathFaceRecord _face1PathFaceRecord;

		private PathFaceRecord _face2PathFaceRecord;

		private Vec3 _rightVector;

		private Vec3 _leftVector;

		private int _attachedFaceCount;

		public int BridgeNavmeshId { get; private set; }

		public ShipAttachment CurrentAttachment { get; private set; }

		public int GetFace1GroupIndex()
		{
			return _face1PathFaceRecord.FaceGroupIndex;
		}

		public int GetFace2GroupIndex()
		{
			return _face2PathFaceRecord.FaceGroupIndex;
		}

		public void Initialize(int bridgeNavmeshId, ShipAttachmentMachine attachmentSource)
		{
			_face1PathFaceRecord = PathFaceRecord.NullFaceRecord;
			_face2PathFaceRecord = PathFaceRecord.NullFaceRecord;
			BridgeNavmeshId = bridgeNavmeshId;
			CurrentAttachment = attachmentSource.CurrentAttachment;
			base.GameEntity.Scene.ImportNavigationMeshPrefab("ship_connection_plank_navmesh_1", BridgeNavmeshId);
			base.GameEntity.AttachNavigationMeshFaces(BridgeNavmeshId, isConnected: false, isBlocker: false, autoLocalize: false, finalizeBlockerConvexHullComputation: false, updateEntityFrame: false);
			base.GameEntity.AttachNavigationMeshFaces(BridgeNavmeshId + 1, isConnected: false, isBlocker: false, autoLocalize: false, finalizeBlockerConvexHullComputation: false, updateEntityFrame: false);
			base.GameEntity.AttachNavigationMeshFaces(BridgeNavmeshId + 2, isConnected: false, isBlocker: false, autoLocalize: false, finalizeBlockerConvexHullComputation: false, updateEntityFrame: false);
			base.GameEntity.AttachNavigationMeshFaces(BridgeNavmeshId + 3, isConnected: false, isBlocker: false, autoLocalize: false, finalizeBlockerConvexHullComputation: false, updateEntityFrame: false);
			base.GameEntity.AttachNavigationMeshFaces(BridgeNavmeshId + 4, isConnected: false);
			base.GameEntity.SetUpdateValidtyOnFrameChangedOfFacesWithId(BridgeNavmeshId + 1, updateValidity: true);
			base.GameEntity.SetUpdateValidtyOnFrameChangedOfFacesWithId(BridgeNavmeshId + 2, updateValidity: true);
			Mission.Current.Scene.SetAbilityOfFacesWithId(BridgeNavmeshId + 3, isEnabled: false);
			Mission.Current.Scene.SetAbilityOfFacesWithId(BridgeNavmeshId + 4, isEnabled: false);
			_customVertexIndices = new int[6];
			_bridgeCustomVertexPositionsArray = new Vec3[6];
			_attachedFaceCount = base.GameEntity.GetAttachedNavmeshFaceCount();
			PathFaceRecord[] array = new PathFaceRecord[_attachedFaceCount];
			base.GameEntity.GetAttachedNavmeshFaceRecords(array);
			PathFaceRecord[] array2 = array;
			for (int i = 0; i < array2.Length; i++)
			{
				PathFaceRecord pathFaceRecord = array2[i];
				if (pathFaceRecord.FaceGroupIndex == BridgeNavmeshId + 1)
				{
					_face1PathFaceRecord = pathFaceRecord;
				}
				else if (pathFaceRecord.FaceGroupIndex == BridgeNavmeshId + 2)
				{
					_face2PathFaceRecord = pathFaceRecord;
				}
			}
			int[] array3 = new int[4];
			int[] array4 = new int[4];
			base.GameEntity.GetAttachedNavmeshFaceVertexIndices(in _face1PathFaceRecord, array3);
			base.GameEntity.GetAttachedNavmeshFaceVertexIndices(in _face2PathFaceRecord, array4);
			int num = -1;
			int num2 = -1;
			int num3 = -1;
			int num4 = -1;
			for (int j = 0; j < 4; j++)
			{
				for (int k = 0; k < 4; k++)
				{
					if (array3[j] == array4[k])
					{
						if (num == -1 && num3 == -1)
						{
							num = j;
							num3 = k;
						}
						else
						{
							num2 = j;
							num4 = k;
						}
						break;
					}
				}
			}
			int num5 = (num + 1) % 4;
			int num6 = (num + 2) % 4;
			int num7 = (num4 + 1) % 4;
			int num8 = (num4 + 2) % 4;
			SetCustomNavmeshVertexIndices(array4[num7], array3[num2], array3[num6], array4[num8], array3[num], array3[num5]);
			CurrentAttachment.AttachmentSource.SteppedAgentManager.SetNavmeshHolder(this);
		}

		public void SetCustomNavmeshVertexIndices(int v1, int v2, int v3, int v4, int v5, int v6)
		{
			_customVertexIndices[0] = v1;
			_customVertexIndices[1] = v2;
			_customVertexIndices[2] = v3;
			_customVertexIndices[3] = v4;
			_customVertexIndices[4] = v5;
			_customVertexIndices[5] = v6;
			base.GameEntity.SetCustomVertexPositionEnabled(customVertexPositionEnabled: true);
		}

		public void SetShipBridgeStartEndPositions(Vec3 startLeftPosition, Vec3 startRightPosition, Vec3 endLeftPosition, Vec3 endRightPosition)
		{
			_startLeftPosition = startLeftPosition;
			_startRightPosition = startRightPosition;
			_endLeftPosition = endLeftPosition;
			_endRightPosition = endRightPosition;
			_rightVector = _endRightPosition - _startRightPosition;
			_leftVector = _endLeftPosition - _startLeftPosition;
		}

		protected override void OnDynamicNavmeshVertexUpdate()
		{
			float num = 0.25f;
			for (int i = 1; i < 4; i++)
			{
				Vec3 vec = _startRightPosition + _rightVector * num;
				Vec3 vec2 = _startLeftPosition + _leftVector * num;
				Vec3 vec3 = (vec + vec2) * 0.5f;
				Vec3 vec4 = (vec2 - vec) * 0.5f;
				_bridgeCustomVertexPositionsArray[i - 1] = vec3 - vec4 * 0.8f;
				_bridgeCustomVertexPositionsArray[i + 2] = vec3 + vec4 * 0.8f;
				num += 0.25f;
			}
			base.GameEntity.SetPositionsForAttachedNavmeshVertices(_customVertexIndices, 6, _bridgeCustomVertexPositionsArray);
		}
	}

	public class ShipBridge : MissionObject
	{
	}

	public class ShipAttachmentJoint
	{
		private const string RopeSnapSoundEvent = "event:/mission/movement/vessel/rope_snap";

		private const float LeftoverImpulseDecay = 0.9f;

		private readonly int RopeStressSoundEventId = SoundManager.GetEventGlobalIndex("event:/mission/movement/vessel/rope_stress");

		private float _lastActualDistance;

		private const float SlackPullBoostMax = 3f;

		private const float SlackPullBoostFullAtMeters = 2f;

		private readonly GameEntity _shipSource;

		private readonly GameEntity _shipTarget;

		private readonly MissionShip _shipSourceScript;

		private readonly MissionShip _shipTargetScript;

		private readonly ShipAttachmentMachine _attachmentEntitySource;

		private readonly ShipAttachmentPointMachine _attachmentEntityTarget;

		private float _age;

		private float _stiffness;

		private bool _unbreakableJoint;

		private Vec3 _ropeLeftoverImpulse;

		private Vec3 _bridgeDirectionLeftoverImpulse;

		private Vec3 _bridgeAlignmentLeftoverImpulse;

		private Vec3 _bridgeXYLeftoverImpulse;

		private ShipAttachment.ShipAttachmentState _currentAttachmentState;

		private float _currentPullSpeed;

		private float _prevDistanceLambda;

		private float _ropesPullDt;

		private NavalShipsLogic _navalShipsLogic;

		private SoundEvent _ropeStressSoundEvent;

		public float AccumulatedDistanceError { get; private set; }

		public float AccumulatedXYError { get; private set; }

		public float AccumulatedAlignmentError { get; private set; }

		public float CurrentXYError { get; private set; }

		public float CurrentAlignmentError { get; private set; }

		public float TensionRatio { get; private set; }

		public bool IsBroken { get; private set; }

		public float CurrentDistanceError { get; private set; }

		public ShipAttachmentJoint(ShipAttachmentMachine attachmentSource, ShipAttachmentPointMachine attachmentTarget, bool unbreakableJoint = false)
		{
			TensionRatio = 0f;
			_shipSource = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(attachmentSource.GameEntity.Root);
			_shipTarget = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(attachmentTarget.GameEntity.Root);
			_attachmentEntitySource = attachmentSource;
			_attachmentEntityTarget = attachmentTarget;
			_shipSourceScript = _shipSource.GetFirstScriptOfType<MissionShip>();
			_shipTargetScript = _shipTarget.GetFirstScriptOfType<MissionShip>();
			_unbreakableJoint = unbreakableJoint;
			InitializeJointParameters();
			UpdateRopeMinLength();
			_currentPullSpeed = 0f;
			_prevDistanceLambda = 0f;
			_ropesPullDt = 0f;
			_ropeStressSoundEvent = SoundEvent.CreateEvent(RopeStressSoundEventId, Mission.Current.Scene);
			_navalShipsLogic = Mission.Current.GetMissionBehavior<NavalShipsLogic>();
		}

		public void OnBreak()
		{
			if (_currentAttachmentState == ShipAttachment.ShipAttachmentState.RopesPulling)
			{
				if (Agent.Main != null && Agent.Main.IsActive())
				{
					if (_attachmentEntitySource.OwnerShip.GetIsAgentOnShip(Agent.Main))
					{
						MatrixFrame globalFrameImpreciseForFixedTick = _attachmentEntitySource.GameEntity.GetGlobalFrameImpreciseForFixedTick();
						SoundManager.StartOneShotEvent("event:/mission/movement/vessel/rope_snap", in globalFrameImpreciseForFixedTick.origin, "isPlayer", 1f);
					}
					else if (_attachmentEntitySource.OwnerShip.GetIsAgentOnShip(Agent.Main))
					{
						MatrixFrame globalFrameImpreciseForFixedTick = _attachmentEntityTarget.GameEntity.GetGlobalFrameImpreciseForFixedTick();
						SoundManager.StartOneShotEvent("event:/mission/movement/vessel/rope_snap", in globalFrameImpreciseForFixedTick.origin, "isPlayer", 1f);
					}
				}
				else
				{
					SoundManager.StartOneShotEvent("event:/mission/movement/vessel/rope_snap", (_attachmentEntityTarget.GameEntity.GetGlobalFrameImpreciseForFixedTick().origin + _attachmentEntityTarget.GameEntity.GetGlobalFrameImpreciseForFixedTick().origin) * 0.5f, "isPlayer", 0f);
				}
				if (_ropeStressSoundEvent != null)
				{
					_ropeStressSoundEvent.Stop();
					_ropeStressSoundEvent = null;
				}
			}
			_navalShipsLogic.OnShipAttachmentLost(_attachmentEntitySource.OwnerShip, _attachmentEntityTarget.OwnerShip);
		}

		public void OnFixedTick(float fixedDt, ShipAttachment currentAttachment, ref float currentRopeLength)
		{
			if (_attachmentEntitySource.IsShipAttachmentJointPhysicsEnabled)
			{
				StabilizeShipUps(15f);
				AlignShips();
				Update(fixedDt, ref currentRopeLength, currentAttachment);
				ReduceRelativeDrift(1f, 15f);
			}
			UpdateRopeLength(fixedDt, ref currentRopeLength, currentAttachment);
		}

		private void StabilizeShipUps(float correctionTorqueCoefficient)
		{
			int num = _shipSourceScript.ComputeActiveShipAttachmentCount();
			int num2 = _shipTargetScript.ComputeActiveShipAttachmentCount();
			Mat3 rotation = _shipSource.GetBodyWorldTransform().rotation;
			Mat3 rotation2 = _shipTarget.GetBodyWorldTransform().rotation;
			float mass = _shipSourceScript.Physics.Mass;
			float mass2 = _shipTargetScript.Physics.Mass;
			Vec3 u = rotation.u;
			Vec3 u2 = rotation2.u;
			Vec3 f = rotation.f;
			Vec3 f2 = rotation2.f;
			Vec3 v = u.CrossProductWithUp() * (correctionTorqueCoefficient * mass * _stiffness);
			Vec3 v2 = u2.CrossProductWithUp() * (correctionTorqueCoefficient * mass2 * _stiffness);
			v = Vec3.DotProduct(v, f) * f;
			v2 = Vec3.DotProduct(v2, f2) * f2;
			_shipSourceScript.Physics.ApplyTorque(v / num);
			_shipTargetScript.Physics.ApplyTorque(v2 / num2);
		}

		public void UpdateRopeMinLength()
		{
			_attachmentEntitySource.RopeMinLength = CalculatePossibleRopeMinLength(_attachmentEntitySource, _attachmentEntityTarget);
			if (_attachmentEntitySource.BridgeConnectionLengthSquared < _attachmentEntitySource.RopeMinLength * _attachmentEntitySource.RopeMinLength)
			{
				float num = _attachmentEntitySource.RopeMinLength + 1f;
				_attachmentEntitySource.BridgeConnectionLengthSquared = num * num;
			}
		}

		public static float CalculatePossibleBridgeConnectionLengthSquared(ShipAttachmentMachine attachmentMachine, ShipAttachmentPointMachine attachmentPointMachine)
		{
			float num = CalculatePossibleRopeMinLength(attachmentMachine, attachmentPointMachine) + 2.5f;
			return num * num;
		}

		public static float CalculatePossibleRopeMinLength(ShipAttachmentMachine attachmentMachine, ShipAttachmentPointMachine attachmentPointMachine)
		{
			MissionShip ownerShip = attachmentMachine.OwnerShip;
			MissionShip ownerShip2 = attachmentPointMachine.OwnerShip;
			MatrixFrame globalFrame = ownerShip.GameEntity.GetGlobalFrame();
			MatrixFrame globalFrame2 = ownerShip2.GameEntity.GetGlobalFrame();
			Vec3 v = attachmentMachine.ConnectionClipPlaneEntity.GetGlobalFrame().origin;
			Vec3 v2 = attachmentPointMachine.ConnectionClipPlaneEntity.GetGlobalFrame().origin;
			MatrixFrame m = attachmentMachine.GameEntity.GetGlobalFrame();
			MatrixFrame m2 = attachmentPointMachine.GameEntity.GetGlobalFrame();
			float num = Vec3.DotProduct(globalFrame.rotation.f, globalFrame2.rotation.f);
			Vec3 vec = globalFrame.TransformToLocal(in v);
			Vec3 vec2 = globalFrame2.TransformToLocal(in v2);
			float num2 = vec.z - ownerShip.Physics.StabilitySubmergedHeightOfShip;
			float num3 = vec2.z - ownerShip2.Physics.StabilitySubmergedHeightOfShip;
			float num4 = TaleWorlds.Library.MathF.Abs(num2 - num3);
			MatrixFrame matrixFrame = globalFrame.TransformToLocal(in m);
			MatrixFrame matrixFrame2 = globalFrame2.TransformToLocal(in m2);
			Vec2[] localPhysicsBoundingBoxXYPlaneVertices = ownerShip.GetLocalPhysicsBoundingBoxXYPlaneVertices(0.9f);
			Vec2[] localPhysicsBoundingBoxXYPlaneVertices2 = ownerShip2.GetLocalPhysicsBoundingBoxXYPlaneVertices(0.9f);
			float num5 = Vec2.DotProduct(m.rotation.f.AsVec2, globalFrame.rotation.s.AsVec2);
			float num6 = Vec2.DotProduct(m2.rotation.f.AsVec2, globalFrame2.rotation.s.AsVec2);
			Vec2 segmentA = ((num5 > 0f) ? localPhysicsBoundingBoxXYPlaneVertices[3] : localPhysicsBoundingBoxXYPlaneVertices[0]);
			Vec2 segmentB = ((num5 > 0f) ? localPhysicsBoundingBoxXYPlaneVertices[2] : localPhysicsBoundingBoxXYPlaneVertices[1]);
			Vec2 lineDirection = ((num5 > 0f) ? Vec2.Side : (-Vec2.Side));
			Vec2 segmentA2 = ((num6 > 0f) ? localPhysicsBoundingBoxXYPlaneVertices2[3] : localPhysicsBoundingBoxXYPlaneVertices2[0]);
			Vec2 segmentB2 = ((num6 > 0f) ? localPhysicsBoundingBoxXYPlaneVertices2[2] : localPhysicsBoundingBoxXYPlaneVertices2[1]);
			Vec2 lineDirection2 = ((num6 > 0f) ? Vec2.Side : (-Vec2.Side));
			MBMath.CheckLineToLineSegmentIntersection(matrixFrame.origin.AsVec2, lineDirection, segmentA, segmentB, out var t, out var _);
			MBMath.CheckLineToLineSegmentIntersection(matrixFrame2.origin.AsVec2, lineDirection2, segmentA2, segmentB2, out var t2, out var _);
			float num7 = t;
			float num8 = t2;
			float num9 = TaleWorlds.Library.MathF.Abs(segmentA.y - segmentB.y);
			float num10 = (vec.y - segmentA.y) / num9;
			float num11 = TaleWorlds.Library.MathF.Abs(segmentA2.y - segmentB2.y);
			float num12 = (vec2.y - segmentA2.y) / num11;
			if (num < 0f)
			{
				num12 = 1f - num12;
			}
			float num13 = TaleWorlds.Library.MathF.Abs(num10 - num12);
			float num14 = 1.5f + (num7 + num8) * (1f - num13);
			return TaleWorlds.Library.MathF.Sqrt(num14 * num14 + num4 * num4);
		}

		public void InitializeJointParameters()
		{
			_age = 0f;
			_stiffness = 0f;
			AccumulatedDistanceError = 0f;
			AccumulatedXYError = 0f;
			AccumulatedAlignmentError = 0f;
			CurrentDistanceError = 0f;
			CurrentXYError = 0f;
			CurrentAlignmentError = 0f;
			IsBroken = false;
			_ropeLeftoverImpulse = new Vec3(0f, 0f, 0f, -1f);
			_bridgeDirectionLeftoverImpulse = new Vec3(0f, 0f, 0f, -1f);
			_bridgeAlignmentLeftoverImpulse = new Vec3(0f, 0f, 0f, -1f);
			_bridgeXYLeftoverImpulse = new Vec3(0f, 0f, 0f, -1f);
		}

		private void SmoothApproachRopeLength(float dt, ref float currentLength, float target)
		{
			_ropesPullDt += dt;
			float num = TaleWorlds.Library.MathF.Sin(_ropesPullDt * 2f * System.MathF.PI * 1f) * 0.5f + 0.5f;
			float num2 = 0.25f * (1f + 0.6f * num);
			_currentPullSpeed = TaleWorlds.Library.MathF.Min(_currentPullSpeed + num2 * dt, 0.65f);
			float num3 = _currentPullSpeed * dt;
			float num4 = currentLength - _lastActualDistance;
			if (num4 > 0f)
			{
				float num5 = 1f + TaleWorlds.Library.MathF.Min(num4 / 2f, 1f) * 2f;
				num3 *= num5;
			}
			currentLength = Math.Max(target, currentLength - num3);
		}

		private void UpdateRopeLength(float fixedDt, ref float currentRopeLength, ShipAttachment currentAttachment)
		{
			if (currentAttachment.State == ShipAttachment.ShipAttachmentState.RopesPulling)
			{
				float currentDistanceError = CurrentDistanceError;
				float num = 10f;
				if (currentDistanceError > num * 0.75f)
				{
					float num2 = currentDistanceError / num;
					currentRopeLength = Math.Max(_attachmentEntitySource.RopeMinLength, currentRopeLength + 0.05f * num2 * fixedDt);
					_currentPullSpeed = 0f;
				}
				else
				{
					float ropeMinLength = _attachmentEntitySource.RopeMinLength;
					SmoothApproachRopeLength(fixedDt, ref currentRopeLength, ropeMinLength);
				}
			}
			else if (currentRopeLength < _attachmentEntitySource.RopeMinLength)
			{
				currentRopeLength = TaleWorlds.Library.MathF.Min(_attachmentEntitySource.RopeMinLength, currentRopeLength + 0.25f * fixedDt);
			}
			else
			{
				currentRopeLength = Math.Max(_attachmentEntitySource.RopeMinLength, currentRopeLength - 0.25f * fixedDt);
			}
		}

		private void Update(float fixedDt, ref float currentRopeLength, ShipAttachment currentAttachment)
		{
			if (IsBroken)
			{
				return;
			}
			_ropeLeftoverImpulse *= 0.9f;
			_bridgeDirectionLeftoverImpulse *= 0.9f;
			_bridgeAlignmentLeftoverImpulse *= 0.9f;
			_bridgeXYLeftoverImpulse *= 0.9f;
			if (currentAttachment.State != _currentAttachmentState)
			{
				if (_ropeStressSoundEvent != null && _currentAttachmentState != ShipAttachment.ShipAttachmentState.RopesPulling)
				{
					_ropeStressSoundEvent.Stop();
					_ropeStressSoundEvent = null;
				}
				InitializeJointParameters();
				_currentAttachmentState = currentAttachment.State;
				if (_currentAttachmentState == ShipAttachment.ShipAttachmentState.BridgeConnected)
				{
					_navalShipsLogic.OnBridgeConnected(_shipSourceScript, _shipTargetScript);
				}
			}
			_age += fixedDt;
			_stiffness = TaleWorlds.Library.MathF.Min(_age / 5f, 1f);
			CurrentDistanceError = 0f;
			CurrentXYError = 0f;
			CurrentAlignmentError = 0f;
			MatrixFrame globalMassFrame = _shipSourceScript.Physics.GetGlobalMassFrame();
			MatrixFrame globalMassFrame2 = _shipTargetScript.Physics.GetGlobalMassFrame();
			Vec3 origin = _attachmentEntitySource.ConnectionClipPlaneEntity.GetGlobalFrameImpreciseForFixedTick().origin;
			Vec3 origin2 = _attachmentEntityTarget.ConnectionClipPlaneEntity.GetGlobalFrameImpreciseForFixedTick().origin;
			_lastActualDistance = origin.Distance(origin2);
			Vec3 linearVelocityAtGlobalPointForEntityWithDynamicBody = _shipSource.GetLinearVelocityAtGlobalPointForEntityWithDynamicBody(origin);
			Vec3 relativeVelocityVector = _shipTarget.GetLinearVelocityAtGlobalPointForEntityWithDynamicBody(origin2) - linearVelocityAtGlobalPointForEntityWithDynamicBody;
			float mass = _shipSourceScript.Physics.Mass;
			float mass2 = _shipTargetScript.Physics.Mass;
			if (_currentAttachmentState == ShipAttachment.ShipAttachmentState.RopesPulling)
			{
				UpdateRopeConstraint(fixedDt, currentRopeLength, globalMassFrame, globalMassFrame2, origin, origin2, mass, mass2, relativeVelocityVector);
			}
			else if (_currentAttachmentState == ShipAttachment.ShipAttachmentState.BridgeConnected)
			{
				UpdateBridgeConstraints(fixedDt, currentRopeLength, globalMassFrame, globalMassFrame2, origin, origin2, mass, mass2, relativeVelocityVector);
			}
			else if (_currentAttachmentState == ShipAttachment.ShipAttachmentState.BridgeThrown)
			{
				UpdateBridgeConstraints(fixedDt, currentRopeLength, globalMassFrame, globalMassFrame2, origin, origin2, mass, mass2, relativeVelocityVector);
			}
			if (!_unbreakableJoint)
			{
				CheckBreaking(fixedDt, currentAttachment);
			}
			TensionRatio = (globalMassFrame.origin - globalMassFrame2.origin).Length / currentRopeLength;
		}

		private void AlignShips()
		{
			Mat3 rotation = _shipSource.GetBodyWorldTransform().rotation;
			Mat3 rotation2 = _shipTarget.GetBodyWorldTransform().rotation;
			float num = TaleWorlds.Library.MathF.Atan2(rotation.f.y, rotation.f.x);
			float angle = TaleWorlds.Library.MathF.Atan2(rotation2.f.y, rotation2.f.x) - num;
			angle = MBMath.WrapAngle(angle);
			if (TaleWorlds.Library.MathF.Abs(angle) > System.MathF.PI / 2f)
			{
				angle = ((!(angle > 0f)) ? (angle + System.MathF.PI) : (angle - System.MathF.PI));
			}
			if (TaleWorlds.Library.MathF.Abs(angle) >= 0.017f)
			{
				int num2 = _shipSourceScript.ComputeActiveShipAttachmentCount();
				int num3 = _shipTargetScript.ComputeActiveShipAttachmentCount();
				float num4 = angle * 0.5f;
				float num5 = (0f - angle) * 0.5f;
				float num6 = (_shipSourceScript.Physics.Mass + _shipTargetScript.Physics.Mass) * 0.5f;
				float num7 = num4 * num6 * 25f * _stiffness;
				float num8 = num5 * num6 * 25f * _stiffness;
				num7 -= _shipSourceScript.Physics.AngularVelocity.z * num6 * 50f;
				num8 -= _shipTargetScript.Physics.AngularVelocity.z * num6 * 50f;
				float num9 = ((_currentAttachmentState != ShipAttachment.ShipAttachmentState.RopesPulling) ? 1f : 0.25f);
				_shipSourceScript.Physics.ApplyTorque(new Vec3(0f, 0f, num7 / (float)num2 * num9));
				_shipTargetScript.Physics.ApplyTorque(new Vec3(0f, 0f, num8 / (float)num3 * num9));
			}
		}

		private void UpdateRopeConstraint(float fixedDt, float currentRopeLength, MatrixFrame shipSourceGlobalFrame, MatrixFrame shipTargetGlobalFrame, Vec3 sourceAttachmentPosition, Vec3 targetAttachmentPosition, float sourceShipMass, float targetShipMass, Vec3 relativeVelocityVector)
		{
			Vec3 vec = targetAttachmentPosition - sourceAttachmentPosition;
			if (!(vec.LengthSquared > currentRopeLength * currentRopeLength))
			{
				return;
			}
			float num = vec.Normalize();
			float relativeVelocity = Vec3.DotProduct(relativeVelocityVector, vec);
			float num2 = (CurrentDistanceError = num - currentRopeLength);
			float num4 = 2f;
			float num5 = TaleWorlds.Library.MathF.Clamp(num2 / num4, 0f, 1f);
			float num6 = MBMath.SmoothStep(0f, num4, num5);
			num6 = (float)TaleWorlds.Library.MathF.Sign(num2) * num6;
			if (_ropeStressSoundEvent != null)
			{
				if (num5 > 2f)
				{
					if (!_ropeStressSoundEvent.IsPlaying())
					{
						_ropeStressSoundEvent.Play();
					}
					else if (_ropeStressSoundEvent.IsPaused())
					{
						_ropeStressSoundEvent.Resume();
					}
					_ropeStressSoundEvent.SetPosition((_attachmentEntitySource.GameEntity.GetGlobalFrameImpreciseForFixedTick().origin + _attachmentEntityTarget.GameEntity.GetGlobalFrameImpreciseForFixedTick().origin) * 0.5f);
				}
				else if (_ropeStressSoundEvent.IsPlaying())
				{
					_ropeStressSoundEvent.Pause();
				}
			}
			float reducedMass = sourceShipMass * targetShipMass / (sourceShipMass + targetShipMass);
			float beta = 0.1f * _stiffness;
			float damping = 0.1f;
			float num7 = TaleWorlds.Library.MathF.Min(CurrentDistanceError / 10f, 1f);
			float num8 = sourceShipMass + targetShipMass;
			float num9 = TaleWorlds.Library.MathF.Min(sourceShipMass, targetShipMass) * 2f / num8;
			float maxAcceleration = TaleWorlds.Library.MathF.Lerp(1.2f, 5f, num7 * (1f - num9));
			float f = SolveImpulseConstraint(relativeVelocity, num6, reducedMass, beta, damping, fixedDt);
			f = TaleWorlds.Library.MathF.Abs(f) * (float)TaleWorlds.Library.MathF.Sign(num6);
			float num10 = (_prevDistanceLambda = TaleWorlds.Library.MathF.Lerp(_prevDistanceLambda, f, fixedDt * 2f));
			ApplyConstraintImpulse(vec * num10, shipSourceGlobalFrame, shipTargetGlobalFrame, sourceAttachmentPosition, targetAttachmentPosition, maxAcceleration, sourceShipMass, targetShipMass, fixedDt, ref _ropeLeftoverImpulse);
		}

		public float SolveSpringMassSystemFromTargetPeriod(float dt, float reducedMass, float targetPeriod, float dampingRatio, float distance, float relativeSpeed)
		{
			float num = System.MathF.PI * 2f / targetPeriod;
			float num2 = reducedMass * num * num;
			float num3 = 2f * reducedMass * dampingRatio * num;
			return ((0f - num2) * distance - num3 * relativeSpeed) * dt;
		}

		private void UpdateBridgeConstraints(float dt, float currentRopeLength, MatrixFrame shipSourceGlobalFrame, MatrixFrame shipTargetGlobalFrame, Vec3 sourceAttachmentPosition, Vec3 targetAttachmentPosition, float sourceShipMass, float targetShipMass, Vec3 relativeVelocityVector)
		{
			float reducedMass = sourceShipMass * targetShipMass / (sourceShipMass + targetShipMass);
			Vec3 vec = targetAttachmentPosition - sourceAttachmentPosition;
			float num = (CurrentDistanceError = vec.Normalize() - currentRopeLength);
			float relativeSpeed = Vec3.DotProduct(relativeVelocityVector, vec);
			float num3 = SolveSpringMassSystemFromTargetPeriod(dt, reducedMass, 2f, 0.3f, num, relativeSpeed);
			float maxAcceleration = TaleWorlds.Library.MathF.Lerp(0.01f, 5f, TaleWorlds.Library.MathF.Min(1f, TaleWorlds.Library.MathF.Abs(num)));
			ApplyConstraintImpulse((0f - num3) * vec * _stiffness, shipSourceGlobalFrame, shipTargetGlobalFrame, sourceAttachmentPosition, targetAttachmentPosition, maxAcceleration, sourceShipMass, targetShipMass, dt, ref _bridgeDirectionLeftoverImpulse);
			float num4 = Vec3.DotProduct(shipSourceGlobalFrame.rotation.f, shipTargetGlobalFrame.rotation.f);
			Vec3 vec2 = shipTargetGlobalFrame.rotation.f;
			if (num4 < 1E-05f)
			{
				vec2 = -1f * shipTargetGlobalFrame.rotation.f;
			}
			Vec3 vec3 = (shipSourceGlobalFrame.rotation.f.AsVec2.Normalized() + vec2.AsVec2.Normalized()).Normalized().ToVec3();
			float num5 = (CurrentAlignmentError = Vec3.DotProduct(vec, vec3));
			float relativeSpeed2 = Vec3.DotProduct(relativeVelocityVector, vec3);
			float num7 = SolveSpringMassSystemFromTargetPeriod(dt, reducedMass, 1.75f, 0.8f, num5, relativeSpeed2);
			float maxAcceleration2 = TaleWorlds.Library.MathF.Lerp(0.01f, 5f, TaleWorlds.Library.MathF.Min(1f, TaleWorlds.Library.MathF.Abs(num5)));
			ApplyConstraintImpulse((0f - num7) * vec3 * _stiffness, shipSourceGlobalFrame, shipTargetGlobalFrame, sourceAttachmentPosition, targetAttachmentPosition, maxAcceleration2, sourceShipMass, targetShipMass, dt, ref _bridgeAlignmentLeftoverImpulse);
			Vec3 vec4 = targetAttachmentPosition - sourceAttachmentPosition;
			Vec2 vb = new Vec2(vec4.x, vec4.y);
			float num8 = vb.Normalize();
			float num9 = (CurrentXYError = currentRopeLength * TaleWorlds.Library.MathF.Sin(System.MathF.PI * 13f / 36f) - num8);
			if (num9 > 0f)
			{
				float num11 = Vec2.DotProduct(relativeVelocityVector.AsVec2, vb);
				float num12 = SolveSpringMassSystemFromTargetPeriod(dt, reducedMass, 0.75f, 0.5f, num9, 0f - num11);
				float maxAcceleration3 = TaleWorlds.Library.MathF.Lerp(0.01f, 15f, TaleWorlds.Library.MathF.Min(1f, TaleWorlds.Library.MathF.Abs(num9)));
				ApplyConstraintImpulse(num12 * vb.ToVec3() * _stiffness, shipSourceGlobalFrame, shipTargetGlobalFrame, sourceAttachmentPosition, targetAttachmentPosition, maxAcceleration3, sourceShipMass, targetShipMass, dt, ref _bridgeXYLeftoverImpulse);
			}
		}

		private float SolveImpulseConstraint(float relativeVelocity, float positionError, float reducedMass, float beta, float damping, float fixedDt)
		{
			return ((0f - beta / fixedDt) * positionError - damping * relativeVelocity) * reducedMass;
		}

		private void ApplyConstraintImpulse(Vec3 impulse, MatrixFrame shipSourceGlobalFrame, MatrixFrame shipTargetGlobalFrame, Vec3 attachmentSourceGlobalPosition, Vec3 attachmentTargetGlobalPosition, float maxAcceleration, float sourceShipMass, float targetShipMass, float fixedDt, ref Vec3 leftoverImpulse)
		{
			float f = impulse.Normalize();
			Vec3 vec = impulse;
			float num = TaleWorlds.Library.MathF.Abs(f);
			float a = sourceShipMass * maxAcceleration * fixedDt;
			float b = targetShipMass * maxAcceleration * fixedDt;
			float b2 = TaleWorlds.Library.MathF.Min(a, b);
			float num2 = TaleWorlds.Library.MathF.Min(num, b2);
			float num3 = num2 * (float)TaleWorlds.Library.MathF.Sign(f);
			float num4 = num - num2;
			leftoverImpulse += num4 * 0.5f * vec;
			Vec3 globalForceVec = vec * num3;
			Vec3 globalForceVec2 = -globalForceVec;
			_shipSourceScript.Physics.ApplyGlobalForceAtLocalPos(shipSourceGlobalFrame.TransformToLocal(in attachmentSourceGlobalPosition), in globalForceVec, GameEntityPhysicsExtensions.ForceMode.Impulse);
			_shipTargetScript.Physics.ApplyGlobalForceAtLocalPos(shipTargetGlobalFrame.TransformToLocal(in attachmentTargetGlobalPosition), in globalForceVec2, GameEntityPhysicsExtensions.ForceMode.Impulse);
		}

		private void CheckBreaking(float dt, ShipAttachment currentAttachment)
		{
			float num = ((_currentAttachmentState == ShipAttachment.ShipAttachmentState.BridgeThrown || _currentAttachmentState == ShipAttachment.ShipAttachmentState.BridgeConnected) ? 5f : 10f);
			if (CurrentDistanceError > num * 0.5f)
			{
				AccumulatedDistanceError += CurrentDistanceError * 4f * dt;
				if (CurrentDistanceError > num || AccumulatedDistanceError > num)
				{
					IsBroken = true;
				}
			}
			if (CurrentAlignmentError > 0.95f)
			{
				AccumulatedAlignmentError += CurrentAlignmentError * 4f * dt;
				if (AccumulatedAlignmentError > 20f)
				{
					IsBroken = true;
				}
			}
			if (CurrentXYError > 2.0625f)
			{
				AccumulatedXYError += CurrentXYError * 4f * dt;
				if (CurrentXYError > 2.75f || AccumulatedXYError > 2.75f)
				{
					IsBroken = true;
				}
			}
			if (IsBroken)
			{
				OnBreak();
			}
		}

		private void ReduceRelativeDrift(float linearDamping, float angularDamping)
		{
			int num = _shipSourceScript.ComputeActiveShipAttachmentCount();
			int num2 = _shipTargetScript.ComputeActiveShipAttachmentCount();
			int num3 = num + num2;
			Vec3 linearVelocity = _shipSourceScript.Physics.LinearVelocity;
			Vec3 linearVelocity2 = _shipTargetScript.Physics.LinearVelocity;
			Vec3 angularVelocity = _shipSourceScript.Physics.AngularVelocity;
			Vec3 angularVelocity2 = _shipTargetScript.Physics.AngularVelocity;
			float mass = _shipSourceScript.Physics.Mass;
			float mass2 = _shipTargetScript.Physics.Mass;
			Vec2 vec = (linearVelocity.AsVec2 * mass + linearVelocity2.AsVec2 * mass2) / (mass + mass2);
			Vec2 vec2 = vec * mass;
			Vec2 vec3 = vec * mass2;
			float max = 2f * mass * 9.806f;
			float max2 = 2f * mass2 * 9.806f;
			vec2.ClampMagnitude(0f, max);
			vec3.ClampMagnitude(0f, max2);
			_shipSourceScript.Physics.ApplyForceToDynamicBody((-vec2 * linearDamping * _stiffness / num3).ToVec3());
			_shipTargetScript.Physics.ApplyForceToDynamicBody((-vec3 * linearDamping * _stiffness / num3).ToVec3());
			float num4 = (angularVelocity.z * mass + angularVelocity2.z * mass2) / (mass + mass2);
			if (num4 != 0f)
			{
				float value = num4 * mass;
				float value2 = num4 * mass2;
				float num5 = System.MathF.PI / 9f * mass;
				float num6 = System.MathF.PI / 9f * mass2;
				value = TaleWorlds.Library.MathF.Clamp(value, 0f - num5, num5);
				value2 = TaleWorlds.Library.MathF.Clamp(value2, 0f - num6, num6);
				_shipSourceScript.Physics.ApplyTorque(new Vec3(0f, 0f, 0f - value) * angularDamping * _stiffness / num3);
				_shipTargetScript.Physics.ApplyTorque(new Vec3(0f, 0f, 0f - value2) * angularDamping * _stiffness / num3);
			}
		}
	}

	public class ShipAttachment
	{
		public struct FlightData
		{
			public Vec3 SourceGlobalPosition;

			public Vec3 TargetGlobalPosition;

			public Vec3 GlobalPositionError;

			public Vec3 GlobalVelocity;

			public float AngleDegree;

			public float Time;

			public bool IsUnderWater;

			public FlightData(in Vec3 sourceGlobalPosition, in Vec3 targetGlobalPosition, in Vec3 globalVelocity, float angleDegree, float time)
			{
				SourceGlobalPosition = sourceGlobalPosition;
				TargetGlobalPosition = targetGlobalPosition;
				GlobalVelocity = globalVelocity;
				AngleDegree = angleDegree;
				Time = time;
				GlobalPositionError = Vec3.Zero;
				IsUnderWater = false;
			}
		}

		internal struct BridgeFlightData
		{
			internal float DtSinceFlightStart;

			internal float CurveLerpVelocity;

			internal float CurveLerpValue;

			internal float ThrowFinishValue;

			internal float CurrentFrameTotalLightTime;

			internal Vec3 CurrentFrameInitialVelocity;
		}

		internal struct RopeSegment
		{
			internal GameEntity ParentEntity;

			internal GameEntity RopeStart;

			internal GameEntity RopeEnd;

			internal int StartSegmentIndex;

			internal int EndSegmentIndex;

			internal float SideStartShift;

			internal float SideEndShift;
		}

		public enum ShipAttachmentState
		{
			RopeThrown,
			RopesPulling,
			BridgeThrown,
			BridgeConnected,
			BrokenAndWaitingForRemoval,
			RopeFailedAndReloading
		}

		private const string NavMeshHolderTag = "navmesh_holder";

		private const string HookImpactWater = "event:/mission/movement/vessel/hook_impact_fail_water_splash";

		private const string HookImpactAttachSuccess = "event:/mission/movement/vessel/hook_impact_attach";

		private const string HookImpactAttachFail = "event:/mission/movement/vessel/hook_impact_fail_to_attach";

		private const string HookThrowingSoundEvent = "event:/mission/movement/vessel/hook_throw";

		private const string BridgeThrownSoundEvent = "event:/mission/movement/vessel/bridge_connect";

		private const string BridgeBrokenSoundEvent = "event:/mission/movement/vessel/bridge_fall";

		private const string HookBeforeAttachmentSoundEvent = "event:/mission/movement/vessel/hook_attach_point_snap";

		private const float ForwardRotationLimitAngleCos = 0.17364818f;

		private const float RopesPullingInteractionDistanceSquared = 2500f;

		private const float BridgeConnectedInteractionDistanceSquared = 100f;

		private const float BridgeConnectedAngleCosLimit = 0.18f;

		private const int BridgeCurveLinearSampleCount = 16;

		private const int MaximumPlankCount = 80;

		private static readonly Comparer<KeyValuePair<float, Vec3>> _cacheCompareDelegate = Comparer<KeyValuePair<float, Vec3>>.Create((KeyValuePair<float, Vec3> x, KeyValuePair<float, Vec3> y) => x.Key.CompareTo(y.Key));

		private bool _attachmentInitializedByPlayer;

		private static List<string> _shipConnectionPlankVariations = new List<string> { "ship_connection_plank_no_physics_a", "ship_connection_plank_no_physics_b", "ship_connection_plank_no_physics_c", "ship_connection_plank_no_physics_d" };

		private static List<string> _ropeClothFragmentPrefabList = new List<string> { "cloth_fragment_a", "cloth_fragment_b", "cloth_fragment_c", "cloth_fragment_g", "cloth_fragment_i", "cloth_fragment_d" };

		private float _shipBetweenAttachmentsCheckTimer;

		private MissionTimer _ropesPullingTimer;

		private GameEntity _bridge;

		private GameEntity _navMeshBridge;

		private GameEntity _navMeshBridgeNavMeshHolder;

		private ShipBridgeNavmeshHolder _shipBridgeNavmeshHolder;

		private int _bridgeNavmeshId;

		private List<GameEntity> _planks = new List<GameEntity>();

		private List<GameEntity> _targetSafetyPlanks = new List<GameEntity>();

		private List<GameEntity> _sourceSafetyPlanks = new List<GameEntity>();

		private KeyValuePair<float, Vec3>[] _bridgeCurveLinearAccessCache = new KeyValuePair<float, Vec3>[16];

		private int _previousNumberOfPlanksNeeded = 80;

		private int _numberOfPlanksNeeded = 80;

		private List<RopeSegment> _ropes = new List<RopeSegment>();

		private BridgeFlightData _bridgeFlightData;

		private bool _isNavmeshBridgeDisabled;

		private float _plankVerticalSize;

		private float _plankHorizontalSize;

		private ShipAttachmentState _state;

		private PhysicsMaterial _woodPhysicsMaterialCached;

		private PhysicsMaterial _defaultPhysicsMaterialCached;

		private Vec3[] _sideBarrierQuadsCached = new Vec3[4];

		private UIntPtr _sideBarriersQuadPinnedPointer = UIntPtr.Zero;

		private GCHandle _sideBarriersQuadPinnedGCHandler;

		private UIntPtr _sideBarriersIndicesPinnedPointer = UIntPtr.Zero;

		private GCHandle _sideBarriersIndicesPinnedGCHandler;

		private int[] _sideBarrierIndicesCached = new int[6];

		private Vec3[] _vFoldQuadsCached = new Vec3[4];

		private UIntPtr _vFoldQuadPinnedPointer = UIntPtr.Zero;

		private GCHandle _vFoldQuadPinnedGCHandler;

		private UIntPtr _vFoldIndicesPinnedPointer = UIntPtr.Zero;

		private GCHandle _vFoldIndicesPinnedGCHandler;

		private int[] _vFoldQuadsIndicesCached = new int[6];

		private int[] _alreadyAddedVertexDataForPhysicsClipPlaneIntersection = new int[4];

		private Vec3[] _registeredVerticesAfterPhysicsClipPlaneIntersection = new Vec3[5];

		private Vec3[] _quadVerticesCCWCached = new Vec3[4];

		private Vec3[] _currentFramePlankPhysicsVertices = new Vec3[200];

		private UIntPtr _currentFramePlankPhysicsVerticesPinnedPointer = UIntPtr.Zero;

		private GCHandle _currentFramePlankPhysicsVerticesPinnedGCHandler;

		private int _currentFramePlankPhysicsVertexCount;

		private int[] _currentFramePlankPhysicsIndices = new int[300];

		private int _currentFramePlankPhysicsIndexCount;

		private UIntPtr _currentFramePlankPhysicsIndicesPinnedPointer = UIntPtr.Zero;

		private GCHandle _currentFramePlankPhysicsIndicesPinnedGCHandler;

		private bool _faceSwapSideOneDone = true;

		private bool _faceSwapSideTwoDone = true;

		private bool _bridgeCreated;

		private bool _hookAttachSoundAlreadyTriggered;

		private Timer _bridgeSwapTimer;

		private float _ropeThrownTimer;

		private MatrixFrame _hookGlobalFrame;

		private FlightData _launchFlightData;

		private bool _currentRopeLengthFirstReachedFinalValue = true;

		private float _currentRopeLength;

		public ShipAttachmentMachine AttachmentSource { get; private set; }

		public ShipAttachmentPointMachine AttachmentTarget { get; private set; }

		public Vec3 CommittedWeightedPosition { get; private set; }

		public float CommittedTotalMass { get; private set; }

		public float CommittedAgentCount { get; private set; }

		public bool BridgeConnectionInteractionDistanceCheck { get; private set; }

		public ShipAttachmentState State => _state;

		public MatrixFrame HookGlobalFrame => _hookGlobalFrame;

		public bool IsNavmeshConnected
		{
			get
			{
				if (_state == ShipAttachmentState.BridgeConnected && _faceSwapSideOneDone)
				{
					return _faceSwapSideTwoDone;
				}
				return false;
			}
		}

		public bool ShipIslandsConnected { get; private set; } = true;

		public ShipAttachmentJoint ShipAttachmentJoint { get; private set; }

		public void ClearCommittedAgentInformation()
		{
			CommittedTotalMass = 0f;
			CommittedWeightedPosition = Vec3.Zero;
			CommittedAgentCount = 0f;
		}

		public void SetAttachmentState(ShipAttachmentState state)
		{
			if (_state != state)
			{
				ShipAttachmentState state2 = _state;
				_state = state;
				UpdateAttachmentMachineEntityVisibilities(state2);
				if (state == ShipAttachmentState.BrokenAndWaitingForRemoval)
				{
					AttachmentSource.OwnerShip.ShipsLogic.OnAttachmentBroken(AttachmentSource, AttachmentTarget);
				}
			}
		}

		public ShipAttachment(ShipAttachmentMachine attachmentSource, ShipAttachmentPointMachine attachmentTarget, in Vec3 globalPosition, in Vec3 globalDirection, bool bridgeConnectionInteractionDistanceCheck = true, bool attachmentInitializedByPlayer = false)
		{
			_state = ShipAttachmentState.RopeThrown;
			AttachmentSource = attachmentSource;
			AttachmentTarget = attachmentTarget;
			_ropesPullingTimer = new MissionTimer(30f);
			_shipBetweenAttachmentsCheckTimer = 0.1f;
			_attachmentInitializedByPlayer = attachmentInitializedByPlayer;
			BridgeConnectionInteractionDistanceCheck = bridgeConnectionInteractionDistanceCheck;
			if (AttachmentTarget != null)
			{
				InitializeRopeFlightDataAccordingToTargetPoint(in globalPosition, AttachmentTarget.GameEntity.GetGlobalFrame().TransformToParent(AttachmentTarget.HookAttachLocalPosition));
			}
			else
			{
				InitializeRopeFlightDataAccordingToTargetDirection(in globalPosition, in globalDirection);
			}
			AttachmentSource.RopeVisual.GameEntity.SetVisibilityExcludeParents(visible: true);
			SoundManager.StartOneShotEvent("event:/mission/movement/vessel/hook_throw", in globalPosition);
			SpawnPlankEntities();
			_woodPhysicsMaterialCached = PhysicsMaterial.GetFromName("wood_nonstick");
			_defaultPhysicsMaterialCached = PhysicsMaterial.GetFromName("default");
			_currentFramePlankPhysicsVerticesPinnedGCHandler = GCHandle.Alloc(_currentFramePlankPhysicsVertices, GCHandleType.Pinned);
			_currentFramePlankPhysicsVerticesPinnedPointer = (UIntPtr)(ulong)(long)_currentFramePlankPhysicsVerticesPinnedGCHandler.AddrOfPinnedObject();
			_currentFramePlankPhysicsIndicesPinnedGCHandler = GCHandle.Alloc(_currentFramePlankPhysicsIndices, GCHandleType.Pinned);
			_currentFramePlankPhysicsIndicesPinnedPointer = (UIntPtr)(ulong)(long)_currentFramePlankPhysicsIndicesPinnedGCHandler.AddrOfPinnedObject();
			_sideBarriersQuadPinnedGCHandler = GCHandle.Alloc(_sideBarrierQuadsCached, GCHandleType.Pinned);
			_sideBarriersQuadPinnedPointer = (UIntPtr)(ulong)(long)_sideBarriersQuadPinnedGCHandler.AddrOfPinnedObject();
			_sideBarriersIndicesPinnedGCHandler = GCHandle.Alloc(_sideBarrierIndicesCached, GCHandleType.Pinned);
			_sideBarriersIndicesPinnedPointer = (UIntPtr)(ulong)(long)_sideBarriersIndicesPinnedGCHandler.AddrOfPinnedObject();
			_vFoldQuadPinnedGCHandler = GCHandle.Alloc(_vFoldQuadsCached, GCHandleType.Pinned);
			_vFoldQuadPinnedPointer = (UIntPtr)(ulong)(long)_vFoldQuadPinnedGCHandler.AddrOfPinnedObject();
			_vFoldIndicesPinnedGCHandler = GCHandle.Alloc(_vFoldQuadsIndicesCached, GCHandleType.Pinned);
			_vFoldIndicesPinnedPointer = (UIntPtr)(ulong)(long)_vFoldIndicesPinnedGCHandler.AddrOfPinnedObject();
			ClearCommittedAgentInformation();
		}

		private void UpdateAttachmentMachineEntityVisibilities(ShipAttachmentState oldState)
		{
			bool flag;
			bool visibilityExcludeParents;
			bool visibilityExcludeParents2;
			bool connectionPhysicsEntitiesVisibility;
			switch (_state)
			{
			case ShipAttachmentState.RopeThrown:
			case ShipAttachmentState.RopesPulling:
				flag = false;
				visibilityExcludeParents = true;
				visibilityExcludeParents2 = true;
				connectionPhysicsEntitiesVisibility = false;
				break;
			case ShipAttachmentState.BridgeThrown:
				flag = true;
				visibilityExcludeParents = false;
				visibilityExcludeParents2 = false;
				connectionPhysicsEntitiesVisibility = true;
				break;
			case ShipAttachmentState.BridgeConnected:
				flag = true;
				visibilityExcludeParents = false;
				visibilityExcludeParents2 = false;
				connectionPhysicsEntitiesVisibility = true;
				SetOarsAvailability(value: false);
				SetShieldsVisibility(visible: false);
				break;
			case ShipAttachmentState.BrokenAndWaitingForRemoval:
				flag = false;
				visibilityExcludeParents = false;
				visibilityExcludeParents2 = true;
				connectionPhysicsEntitiesVisibility = false;
				break;
			case ShipAttachmentState.RopeFailedAndReloading:
				flag = false;
				visibilityExcludeParents = true;
				visibilityExcludeParents2 = true;
				connectionPhysicsEntitiesVisibility = false;
				break;
			default:
				flag = false;
				visibilityExcludeParents = false;
				visibilityExcludeParents2 = false;
				connectionPhysicsEntitiesVisibility = false;
				break;
			}
			if (oldState == ShipAttachmentState.BridgeConnected)
			{
				SetShieldsVisibility(visible: true);
				SetOarsAvailability(value: true);
			}
			foreach (GameEntity rampPhysics in AttachmentSource.RampPhysicsList)
			{
				rampPhysics.SetVisibilityExcludeParents(flag);
			}
			AttachmentSource.RampVisualEntity.SetVisibilityExcludeParents(flag);
			AttachmentSource.RampBarrier.SetVisibilityExcludeParents(!flag);
			AttachmentSource.RopeVisual.GameEntity.SetVisibilityExcludeParents(visibilityExcludeParents);
			AttachmentSource.Hook.SetVisibilityExcludeParents(visibilityExcludeParents2);
			AttachmentSource.SetConnectionPhysicsEntitiesVisibility(connectionPhysicsEntitiesVisibility);
			if (AttachmentTarget == null)
			{
				return;
			}
			AttachmentTarget.RampVisualEntity.SetVisibilityExcludeParents(flag);
			foreach (GameEntity rampPhysics2 in AttachmentTarget.RampPhysicsList)
			{
				rampPhysics2.SetVisibilityExcludeParents(flag);
			}
			AttachmentTarget.RampBarrier.SetVisibilityExcludeParents(!flag);
		}

		public bool ShouldLookForBetterConnections()
		{
			return AttachmentTarget != null;
		}

		public void OnParallelTick(float dt)
		{
			if (_state == ShipAttachmentState.BridgeConnected)
			{
				ArrangePlanksMT();
			}
		}

		public void OnTick(float dt)
		{
			ClearCommittedAgentInformation();
			if (_state == ShipAttachmentState.BrokenAndWaitingForRemoval)
			{
				Vec3 sourceGlobalPosition = AttachmentSource.RopeVisual.GameEntity.GetGlobalFrame().origin;
				AttachmentSource.RopeVisual.SetRopeState(RopePileBaked.RopeSlackPolicy.Collapsed, 0f);
				AttachmentSource.RopeVisual.ClearClipPlanes();
				AttachmentSource.RopeVisual.UpdateRopeMeshVisualAccordingToTargetPointLinear(in sourceGlobalPosition, in sourceGlobalPosition);
				return;
			}
			if (_state == ShipAttachmentState.RopeThrown || _state == ShipAttachmentState.RopeFailedAndReloading)
			{
				UpdateRopeThrowingBehavior(dt);
			}
			else
			{
				Vec3 targetGlobalPosition = AttachmentTarget.GameEntity.GetGlobalFrame().TransformToParent(AttachmentTarget.HookAttachLocalPosition);
				Vec3 sourceGlobalPosition2 = AttachmentSource.RopeVisual.GameEntity.GetGlobalFrame().origin;
				float tension = ((ShipAttachmentJoint != null) ? TaleWorlds.Library.MathF.Clamp(ShipAttachmentJoint.CurrentDistanceError / 10f, 0f, 1f) : 0f);
				AttachmentSource.RopeVisual.SetRopeState(RopePileBaked.RopeSlackPolicy.Taut, _currentRopeLength, tension);
				AttachmentSource.RopeVisual.ClearMeshUnfurlOverride();
				AttachmentSource.RopeVisual.SetClipPlaneEntities(AttachmentSource.ConnectionClipPlaneEntity, AttachmentTarget.ConnectionClipPlaneEntity);
				_hookGlobalFrame.origin = AttachmentSource.RopeVisual.UpdateRopeMeshVisualAccordingToTargetPointLinear(in sourceGlobalPosition2, in targetGlobalPosition);
				_hookGlobalFrame.rotation.f = (targetGlobalPosition - sourceGlobalPosition2).NormalizedCopy();
				_hookGlobalFrame.rotation.s = _hookGlobalFrame.rotation.f.CrossProductWithUp().NormalizedCopy();
				_hookGlobalFrame.rotation.u = Vec3.CrossProduct(_hookGlobalFrame.rotation.s, _hookGlobalFrame.rotation.f);
				_hookGlobalFrame.rotation.RotateAboutSide(-System.MathF.PI / 2f);
				if (_currentRopeLengthFirstReachedFinalValue && MBMath.ApproximatelyEquals(_currentRopeLength, AttachmentSource.RopeMinLength, 0.05f))
				{
					_ropesPullingTimer.Reset();
					_currentRopeLengthFirstReachedFinalValue = false;
				}
				if (_state == ShipAttachmentState.RopesPulling)
				{
					CheckAndConnectBridge();
				}
				else if (_state == ShipAttachmentState.BridgeThrown)
				{
					TickThrownBridge(dt);
					ArrangeNavmeshBridgeSideBarriersAndVFoldQuads();
				}
				else if (_state == ShipAttachmentState.BridgeConnected)
				{
					ArrangePlanks();
					ArrangeNavmeshBridgeSideBarriersAndVFoldQuads();
				}
			}
			if (AttachmentTarget != null)
			{
				CheckAndBreakAttachment(dt);
			}
			if (_state == ShipAttachmentState.BridgeConnected || _state == ShipAttachmentState.BridgeThrown)
			{
				if ((!_faceSwapSideOneDone || !_faceSwapSideTwoDone) && _bridgeSwapTimer.Check(Mission.Current.CurrentTime))
				{
					if (!_faceSwapSideOneDone && Mission.Current.Scene.SwapFaceConnectionsWithID(_bridgeNavmeshId + 1, _bridgeNavmeshId + 3, AttachmentTarget.RelatedShipNavmeshOffset + AttachmentTarget.OwnerShip.GetDynamicNavmeshIdStart(), canFail: true))
					{
						_faceSwapSideOneDone = true;
					}
					if (!_faceSwapSideTwoDone && Mission.Current.Scene.SwapFaceConnectionsWithID(_bridgeNavmeshId + 2, _bridgeNavmeshId + 4, AttachmentSource.RelatedShipNavmeshOffset + AttachmentSource.OwnerShip.GetDynamicNavmeshIdStart(), canFail: true))
					{
						_faceSwapSideTwoDone = true;
					}
					_bridgeCreated = true;
				}
				if (_faceSwapSideOneDone && _faceSwapSideTwoDone && !ShipIslandsConnected)
				{
					ShipIslandsConnected = true;
					MissionShip.MergeShipIslands(AttachmentSource.OwnerShip, AttachmentTarget.OwnerShip);
				}
			}
			if (_state == ShipAttachmentState.BridgeThrown || _state == ShipAttachmentState.BridgeConnected)
			{
				CommittedWeightedPosition = AttachmentSource.SteppedAgentManager.WeightedPosition;
				CommittedAgentCount = AttachmentSource.SteppedAgentManager.AgentCount;
				CommittedTotalMass = AttachmentSource.SteppedAgentManager.TotalMass;
				AttachmentSource.SteppedAgentManager.ClearAgentWeightAndPositionInformation();
			}
		}

		private void CheckAndBreakAttachment(float dt)
		{
			_shipBetweenAttachmentsCheckTimer -= dt;
			MatrixFrame globalFrame = AttachmentSource.GameEntity.GetGlobalFrame();
			MatrixFrame globalFrame2 = AttachmentTarget.GameEntity.GetGlobalFrame();
			if (globalFrame.rotation.u.z < 0.17364818f || globalFrame2.rotation.u.z < 0.17364818f)
			{
				BreakWithCutRope(0.5f, Vec3.Zero);
				return;
			}
			if (_shipBetweenAttachmentsCheckTimer <= 0f)
			{
				_shipBetweenAttachmentsCheckTimer = MBRandom.RandomFloatRanged(0.1f, 0.15f);
				if (TryFindShipBetweenAttachments(AttachmentSource, AttachmentTarget, out var offendingShip))
				{
					if (State == ShipAttachmentState.RopesPulling)
					{
						Vec3 linearVelocity = offendingShip.Physics.LinearVelocity;
						float num = TaleWorlds.Library.MathF.Min(linearVelocity.Normalize() * 1.5f, 8f);
						Vec3 impulseAtBreakPoint = ((linearVelocity.LengthSquared > 0.0001f) ? (linearVelocity * num) : Vec3.Zero);
						float breakFractionAlongRope = ClosestPointFractionOnSegment(offendingShip.GameEntity.GlobalPosition, AttachmentSource.GameEntity.GlobalPosition, AttachmentTarget.GameEntity.GlobalPosition);
						BreakWithCutRope(breakFractionAlongRope, impulseAtBreakPoint, ShipAttachmentJoint.TensionRatio);
						return;
					}
					SetAttachmentState(ShipAttachmentState.BrokenAndWaitingForRemoval);
				}
			}
			if (!CheckAttachmentsFacingEachOther(AttachmentSource, AttachmentTarget))
			{
				BreakWithCutRope(0.5f, Vec3.Zero);
				return;
			}
			ShipAttachmentMachine attachmentSource = AttachmentSource;
			if (attachmentSource != null && attachmentSource.OwnerShip?.Physics.NavalSinkingState == NavalDLC.Missions.NavalPhysics.NavalPhysics.SinkingState.Floating)
			{
				ShipAttachmentPointMachine attachmentTarget = AttachmentTarget;
				if (attachmentTarget != null && attachmentTarget.OwnerShip?.Physics.NavalSinkingState == NavalDLC.Missions.NavalPhysics.NavalPhysics.SinkingState.Floating)
				{
					if (_state == ShipAttachmentState.RopesPulling && ((_ropesPullingTimer.Check() && (MBMath.ApproximatelyEquals(_currentRopeLength, AttachmentSource.RopeMinLength, 0.05f) || (AttachmentSource.OwnerShip.Team != null && (AttachmentSource.OwnerShip.Team.TeamAI as TeamAINavalComponent).TeamNavalQuerySystem.IsAnyShipInCriticalZoneBetween(AttachmentSource.OwnerShip, AttachmentTarget.OwnerShip)))) || CheckIntersectionsBetweenConnectionsWithState(AttachmentSource, AttachmentTarget, ShipAttachmentState.BridgeConnected)))
					{
						BreakWithCutRope(0.5f, Vec3.Zero, ShipAttachmentJoint.TensionRatio);
					}
					else if (ShipAttachmentJoint != null && ShipAttachmentJoint.IsBroken)
					{
						if (_state == ShipAttachmentState.RopesPulling)
						{
							float breakFractionAlongRope2 = MBRandom.RandomFloatRanged(0.25f, 0.75f);
							BreakWithCutRope(breakFractionAlongRope2, Vec3.Zero, ShipAttachmentJoint.TensionRatio);
						}
						else
						{
							SetAttachmentState(ShipAttachmentState.BrokenAndWaitingForRemoval);
						}
					}
					return;
				}
			}
			BreakWithCutRope(0.5f, Vec3.Zero);
		}

		public void BreakWithCutRope(float breakFractionAlongRope, Vec3 impulseAtBreakPoint, float tensionRatio = 0f)
		{
			GameEntity ropeVisualEntity = AttachmentSource.RopeVisualEntity;
			GameEntity gameEntity = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(AttachmentTarget.GameEntity);
			Vec3 globalPosition = ropeVisualEntity.GlobalPosition;
			Vec3 v = AttachmentTarget.HookAttachLocalPosition;
			Vec3 vec = gameEntity.GetGlobalFrame().TransformToParent(in v);
			float num = MBMath.ClampFloat(breakFractionAlongRope, 0.05f, 0.95f);
			Vec3 freeEndGlobalPosition = Vec3.Lerp(globalPosition, vec, num);
			RopePileBaked ropeVisual = AttachmentSource.RopeVisual;
			GameEntity connectionClipPlaneEntity = AttachmentSource.ConnectionClipPlaneEntity;
			GameEntity connectionClipPlaneEntity2 = AttachmentTarget.ConnectionClipPlaneEntity;
			AttachmentSource.ActivateCutRopeSegment(0, ropeVisualEntity, Vec3.Zero, globalPosition, freeEndGlobalPosition, impulseAtBreakPoint, ropeVisual, 0f, num, connectionClipPlaneEntity, tensionRatio);
			AttachmentSource.ActivateCutRopeSegment(1, gameEntity, v, vec, freeEndGlobalPosition, impulseAtBreakPoint, ropeVisual, 1f, num, connectionClipPlaneEntity2, tensionRatio);
			ShipAttachmentMachine attachmentSource = AttachmentSource;
			if (attachmentSource != null && (attachmentSource.RopeVisual?.GameEntity).HasValue)
			{
				AttachmentSource.RopeVisual.GameEntity.SetVisibilityExcludeParents(visible: false);
			}
			SetAttachmentState(ShipAttachmentState.BrokenAndWaitingForRemoval);
		}

		private static float ClosestPointFractionOnSegment(Vec3 point, Vec3 a, Vec3 b)
		{
			Vec3 v = b - a;
			float lengthSquared = v.LengthSquared;
			if (lengthSquared < 1E-06f)
			{
				return 0.5f;
			}
			return MBMath.ClampFloat(Vec3.DotProduct(point - a, v) / lengthSquared, 0f, 1f);
		}

		public void InitializeRopeFlightDataAccordingToTargetPoint(in Vec3 sourceGlobalPosition, in Vec3 targetGlobalPosition)
		{
			float num = CalculateLaunchAngleDegree(sourceGlobalPosition, targetGlobalPosition, 20f);
			if (num == float.MinValue)
			{
				num = TaleWorlds.Library.MathF.Clamp(num, Math.Min(44.9999f, CalculateDifferenceVectorAngle(in sourceGlobalPosition, in targetGlobalPosition) + 0.1f), 45f);
			}
			(Vec3, float) tuple = CalculateInitialVelocityAndTime(sourceGlobalPosition, targetGlobalPosition, num);
			_launchFlightData = new FlightData(in sourceGlobalPosition, in targetGlobalPosition, in tuple.Item1, num, tuple.Item2);
			AttachmentSource.RopeVisual?.ClearMeshUnfurlOverride();
			PrimeChainForThrow(in sourceGlobalPosition, in targetGlobalPosition, in tuple.Item1);
		}

		public void InitializeRopeFlightDataAccordingToTargetDirection(in Vec3 sourceGlobalPosition, in Vec3 targetGlobalDirection)
		{
			Vec3 globalVelocity = targetGlobalDirection * 25f;
			_launchFlightData = new FlightData(in sourceGlobalPosition, in Vec3.Zero, in globalVelocity, TaleWorlds.Library.MathF.Asin(targetGlobalDirection.z) * 180f / System.MathF.PI, 0f);
			AttachmentSource.RopeVisual?.ClearMeshUnfurlOverride();
			PrimeChainForThrow(in sourceGlobalPosition, sourceGlobalPosition + targetGlobalDirection * 5f, in globalVelocity);
		}

		private void PrimeChainForThrow(in Vec3 sourceGlobalPosition, in Vec3 targetGlobalPosition, in Vec3 launchVelocity)
		{
			if (AttachmentSource?.RopeVisual != null)
			{
				AttachmentSource.RopeVisual.SnapRopeState(RopePileBaked.RopeSlackPolicy.Natural, 0.5f);
				AttachmentSource.RopeVisual.ResetChain();
				ApplyThrowHandWobble(launchVelocity);
			}
		}

		private void ApplyThrowHandWobble(Vec3 launchVelocity)
		{
			if (AttachmentSource?.RopeVisual != null && launchVelocity.LengthSquared >= 0.0001f)
			{
				Vec3 vec = launchVelocity.NormalizedCopy().CrossProductWithUp();
				if (vec.LengthSquared < 0.0001f)
				{
					vec = new Vec3(1f);
				}
				vec = vec.NormalizedCopy();
				vec.z = 0.35f;
				vec = vec.NormalizedCopy();
				AttachmentSource.RopeVisual.ApplyWobble(vec, 2.5f, 1f, 0.85f);
			}
		}

		private Vec3 CalculateRelativeVelocityBetweenAttachments()
		{
			MissionShip ownerShip = AttachmentSource.OwnerShip;
			MissionShip ownerShip2 = AttachmentTarget.OwnerShip;
			MatrixFrame globalFrame = ownerShip.GameEntity.GetGlobalFrame();
			MatrixFrame globalFrame2 = ownerShip2.GameEntity.GetGlobalFrame();
			Vec3 vec = globalFrame.TransformToParent(ownerShip.Physics.LocalCenterOfMass);
			Vec3 vec2 = globalFrame2.TransformToParent(ownerShip2.Physics.LocalCenterOfMass);
			MatrixFrame globalFrame3 = AttachmentSource.ConnectionClipPlaneEntity.GetGlobalFrame();
			MatrixFrame globalFrame4 = AttachmentTarget.ConnectionClipPlaneEntity.GetGlobalFrame();
			Vec3 vec3 = ownerShip.Physics.LinearVelocity + Vec3.CrossProduct(ownerShip.Physics.AngularVelocity, globalFrame3.origin - vec);
			return ownerShip2.Physics.LinearVelocity + Vec3.CrossProduct(ownerShip2.Physics.AngularVelocity, globalFrame4.origin - vec2) - vec3;
		}

		private void UpdateRopeMeshVisualAccordingToTargetPoint(in Vec3 sourceGlobalPosition, in Vec3 targetGlobalPosition, float throwingAngleDegree)
		{
			float num = sourceGlobalPosition.Distance(targetGlobalPosition);
			AttachmentSource.RopeVisual.SnapRopeState(RopePileBaked.RopeSlackPolicy.Natural, TaleWorlds.Library.MathF.Max(num * 1.1f, 0.5f));
			_hookGlobalFrame.origin = AttachmentSource.RopeVisual.UpdateRopeMeshVisualAccordingToTargetPointLinear(in sourceGlobalPosition, in targetGlobalPosition);
			Vec3 vec = targetGlobalPosition - sourceGlobalPosition;
			if (vec.LengthSquared > 0.0001f)
			{
				_hookGlobalFrame.rotation.f = vec.NormalizedCopy();
				_hookGlobalFrame.rotation.s = _hookGlobalFrame.rotation.f.CrossProductWithUp().NormalizedCopy();
				_hookGlobalFrame.rotation.u = Vec3.CrossProduct(_hookGlobalFrame.rotation.s, _hookGlobalFrame.rotation.f);
				_hookGlobalFrame.rotation.RotateAboutSide(-System.MathF.PI / 2f);
			}
		}

		public void CheckAndConnectBridge(bool forceBridge = false)
		{
			MatrixFrame globalFrame = AttachmentSource.ConnectionClipPlaneEntity.GetGlobalFrame();
			MatrixFrame globalFrame2 = AttachmentTarget.ConnectionClipPlaneEntity.GetGlobalFrame();
			float num = globalFrame.origin.DistanceSquared(globalFrame2.origin);
			float lengthSquared = CalculateRelativeVelocityBetweenAttachments().LengthSquared;
			Vec3 vec = globalFrame2.origin - globalFrame.origin;
			vec.Normalize();
			float num2 = Vec2.DotProduct(globalFrame.rotation.f.AsVec2.Normalized(), vec.AsVec2);
			float num3 = Vec2.DotProduct(globalFrame2.rotation.f.AsVec2.Normalized(), -vec.AsVec2);
			float num4 = (num2 + num3) * 0.5f;
			ShipAttachmentPointMachine shipAttachmentPointMachine = null;
			if (!forceBridge)
			{
				foreach (ShipAttachmentPointMachine item in AttachmentTarget.OwnerShip?.AttachmentPointMachines)
				{
					MatrixFrame globalFrame3 = item.GameEntity.GetGlobalFrame();
					Vec3 vec2 = globalFrame3.origin - globalFrame.origin;
					float lengthSquared2 = vec2.LengthSquared;
					vec2.Normalize();
					float num5 = Vec2.DotProduct(globalFrame.rotation.f.AsVec2.Normalized(), vec2.AsVec2);
					float num6 = Vec2.DotProduct(globalFrame3.rotation.f.AsVec2.Normalized(), -vec2.AsVec2);
					float num7 = (num5 + num6) * 0.5f;
					if (item.CurrentAttachment == null && item.LinkedAttachmentMachine?.CurrentAttachment == null && lengthSquared2 < ShipAttachmentJoint.CalculatePossibleBridgeConnectionLengthSquared(AttachmentSource, item) && lengthSquared <= 4f && num5 > 0.18f && num6 > 0.18f && num7 > num4 && !CheckIntersectionsBetweenConnections(AttachmentSource, item))
					{
						shipAttachmentPointMachine = item;
					}
				}
			}
			if (shipAttachmentPointMachine != null)
			{
				Destroy();
				AttachmentSource.ConnectWithAttachmentPointMachine(shipAttachmentPointMachine, forceBridge: true);
			}
			else if (forceBridge || (num < AttachmentSource.BridgeConnectionLengthSquared && lengthSquared <= 4f && num2 > 0.18f && num3 > 0.18f))
			{
				StartBridgeThrowAnimation();
				SoundManager.StartOneShotEvent("event:/mission/movement/vessel/bridge_connect", (globalFrame.origin + globalFrame2.origin) / 2f);
			}
		}

		public void InitializeShipAttachmentJoint(Vec3 attachmentSourceGlobalPosition, Vec3 attachmentTargetGlobalPosition, bool unbreakableJoint = false)
		{
			_currentRopeLength = attachmentSourceGlobalPosition.AsVec2.Distance(attachmentTargetGlobalPosition.AsVec2) + 0.1f;
			ShipAttachmentJoint = new ShipAttachmentJoint(AttachmentSource, AttachmentTarget, unbreakableJoint);
			SetAttachmentState(ShipAttachmentState.RopesPulling);
			GameEntity gameEntity = TaleWorlds.Engine.GameEntity.Instantiate(Mission.Current.Scene, _shipConnectionPlankVariations[0], callScriptCallbacks: false);
			Vec3 vec = gameEntity.GetBoundingBoxMax() - gameEntity.GetBoundingBoxMin();
			_plankVerticalSize = gameEntity.GetLocalScale().y * vec.y;
			_plankHorizontalSize = gameEntity.GetLocalScale().x * vec.x;
			gameEntity.Remove(78);
			_bridgeSwapTimer = new Timer(Mission.Current.CurrentTime, 0f);
			if (!unbreakableJoint && !_hookAttachSoundAlreadyTriggered)
			{
				bool flag = Agent.Main != null && Agent.Main.IsActive() && (AttachmentSource.OwnerShip.GetIsAgentOnShip(Agent.Main) || AttachmentTarget.OwnerShip.GetIsAgentOnShip(Agent.Main));
				SoundManager.StartOneShotEvent("event:/mission/movement/vessel/hook_impact_attach", in attachmentTargetGlobalPosition, "isPlayer", flag ? 1f : 0f);
			}
			_hookAttachSoundAlreadyTriggered = false;
			AttachmentSource.OwnerShip.ShipsLogic.OnSuccessfulHookThrow(AttachmentSource.OwnerShip, AttachmentTarget.OwnerShip);
			_sideBarrierIndicesCached[0] = 0;
			_sideBarrierIndicesCached[1] = 1;
			_sideBarrierIndicesCached[2] = 2;
			_sideBarrierIndicesCached[3] = 0;
			_sideBarrierIndicesCached[4] = 2;
			_sideBarrierIndicesCached[5] = 3;
			_vFoldQuadsIndicesCached[0] = 2;
			_vFoldQuadsIndicesCached[1] = 1;
			_vFoldQuadsIndicesCached[2] = 0;
			_vFoldQuadsIndicesCached[3] = 3;
			_vFoldQuadsIndicesCached[4] = 2;
			_vFoldQuadsIndicesCached[5] = 0;
		}

		private void UpdateRopeThrowingBehavior(float dt)
		{
			_ropeThrownTimer += dt;
			if (_launchFlightData.GlobalPositionError.LengthSquared > 1.0000001E-06f)
			{
				_launchFlightData.GlobalPositionError *= 1f - dt * 8f;
			}
			else
			{
				_launchFlightData.GlobalPositionError = Vec3.Zero;
			}
			Vec3 sourceGlobalPosition = AttachmentSource.RopeVisual.GameEntity.GlobalPosition;
			if (_state == ShipAttachmentState.RopeFailedAndReloading)
			{
				_launchFlightData.GlobalVelocity = _launchFlightData.GlobalVelocity * (1f - dt * (_launchFlightData.IsUnderWater ? 8f : 1f)) + MBGlobals.GravitationalAcceleration * dt;
				Vec3 sourceGlobalPosition2 = _launchFlightData.SourceGlobalPosition;
				_launchFlightData.SourceGlobalPosition += _launchFlightData.GlobalVelocity * dt;
				if (_launchFlightData.SourceGlobalPosition.DistanceSquared(sourceGlobalPosition) > 1600f)
				{
					_launchFlightData.SourceGlobalPosition = sourceGlobalPosition + (_launchFlightData.SourceGlobalPosition - sourceGlobalPosition).NormalizedCopy() * 40f;
					_launchFlightData.GlobalVelocity = (_launchFlightData.SourceGlobalPosition - sourceGlobalPosition2) / dt;
				}
				float waterLevelAtPosition = AttachmentSource.Scene.GetWaterLevelAtPosition(_launchFlightData.SourceGlobalPosition.AsVec2, useWaterRenderer: true, checkWaterBodyEntities: false);
				if (_launchFlightData.IsUnderWater)
				{
					float valueTo = waterLevelAtPosition - 0.15f;
					_launchFlightData.SourceGlobalPosition.z = TaleWorlds.Library.MathF.Lerp(_launchFlightData.SourceGlobalPosition.z, valueTo, TaleWorlds.Library.MathF.Min(1f, dt * 6f));
					_launchFlightData.GlobalVelocity.x *= 1f - dt * 3f;
					_launchFlightData.GlobalVelocity.y *= 1f - dt * 3f;
					_launchFlightData.GlobalVelocity.z = 0f;
				}
				else if (waterLevelAtPosition > _launchFlightData.SourceGlobalPosition.z)
				{
					_launchFlightData.IsUnderWater = true;
				}
				if (_currentRopeLength <= 0f)
				{
					if (!_launchFlightData.IsUnderWater)
					{
						_ropeThrownTimer -= dt * 0.8f;
					}
					float num = TaleWorlds.Library.MathF.Clamp(TaleWorlds.Library.MathF.Pow(_ropeThrownTimer / _launchFlightData.Time, 1.3f), 0f, 1f);
					if (num >= 1f)
					{
						_currentRopeLength = sourceGlobalPosition.Distance(_launchFlightData.SourceGlobalPosition);
						AttachmentSource.RopeVisual.SetRopeState(RopePileBaked.RopeSlackPolicy.Natural, _currentRopeLength * 1.5f);
						AttachmentSource.RopeVisual.ClearMeshUnfurlOverride();
						_hookGlobalFrame.origin = AttachmentSource.RopeVisual.UpdateRopeMeshVisualAccordingToTargetPointLinear(in sourceGlobalPosition, in _launchFlightData.SourceGlobalPosition);
						_hookGlobalFrame.rotation.f = (_launchFlightData.SourceGlobalPosition - sourceGlobalPosition).NormalizedCopy();
						_hookGlobalFrame.rotation.s = _hookGlobalFrame.rotation.f.CrossProductWithUp().NormalizedCopy();
						_hookGlobalFrame.rotation.u = Vec3.CrossProduct(_hookGlobalFrame.rotation.s, _hookGlobalFrame.rotation.f);
						_hookGlobalFrame.rotation.RotateAboutSide(-System.MathF.PI / 2f);
					}
					else
					{
						UpdateRopeMeshVisualAccordingToTargetPoint(in sourceGlobalPosition, in _launchFlightData.SourceGlobalPosition, _launchFlightData.AngleDegree - num * (_launchFlightData.AngleDegree - CalculateDifferenceVectorAngle(in sourceGlobalPosition, in _launchFlightData.SourceGlobalPosition) - 0.1f));
					}
				}
				else
				{
					_currentRopeLength -= dt * 4f;
					_launchFlightData.SourceGlobalPosition = sourceGlobalPosition + (_launchFlightData.SourceGlobalPosition - sourceGlobalPosition).NormalizedCopy() * _currentRopeLength;
					AttachmentSource.RopeVisual.SetRopeState(RopePileBaked.RopeSlackPolicy.Taut, _currentRopeLength);
					AttachmentSource.RopeVisual.ClearMeshUnfurlOverride();
					_hookGlobalFrame.origin = AttachmentSource.RopeVisual.UpdateRopeMeshVisualAccordingToTargetPointLinear(in sourceGlobalPosition, in _launchFlightData.SourceGlobalPosition);
					_hookGlobalFrame.rotation.f = (_launchFlightData.SourceGlobalPosition - sourceGlobalPosition).NormalizedCopy();
					_hookGlobalFrame.rotation.s = _hookGlobalFrame.rotation.f.CrossProductWithUp().NormalizedCopy();
					_hookGlobalFrame.rotation.u = Vec3.CrossProduct(_hookGlobalFrame.rotation.s, _hookGlobalFrame.rotation.f);
					_hookGlobalFrame.rotation.RotateAboutSide(-System.MathF.PI / 2f);
					if (_currentRopeLength <= 0f)
					{
						_currentRopeLength = 0f;
						SetAttachmentState(ShipAttachmentState.BrokenAndWaitingForRemoval);
					}
				}
				return;
			}
			float num2 = _launchFlightData.AngleDegree - _ropeThrownTimer * 5f;
			if (_launchFlightData.Time > 0f)
			{
				Vec3 vec = AttachmentTarget.GameEntity.GetGlobalFrame().TransformToParent(AttachmentTarget.HookAttachLocalPosition);
				Vec3 targetGlobalPosition = vec + _launchFlightData.GlobalPositionError;
				if (_ropeThrownTimer >= _launchFlightData.Time)
				{
					float num3 = TaleWorlds.Library.MathF.Clamp(TaleWorlds.Library.MathF.Pow((_ropeThrownTimer - _launchFlightData.Time) / _launchFlightData.Time, 1.3f), 0f, 1f);
					UpdateRopeMeshVisualAccordingToTargetPoint(in sourceGlobalPosition, in targetGlobalPosition, num2 - num3 * (num2 - CalculateDifferenceVectorAngle(in sourceGlobalPosition, in targetGlobalPosition) - 0.1f));
					if (num3 >= 1f)
					{
						InitializeShipAttachmentJoint(sourceGlobalPosition, vec);
					}
				}
				else
				{
					Vec3 launchProjectileCurrentGlobalPosition = GetLaunchProjectileCurrentGlobalPosition(_ropeThrownTimer);
					UpdateRopeMeshVisualAccordingToTargetPoint(in sourceGlobalPosition, launchProjectileCurrentGlobalPosition + (targetGlobalPosition - _launchFlightData.TargetGlobalPosition), num2);
				}
				return;
			}
			Vec3 targetGlobalPosition2 = GetLaunchProjectileCurrentGlobalPosition(_ropeThrownTimer);
			UpdateRopeMeshVisualAccordingToTargetPoint(in sourceGlobalPosition, in targetGlobalPosition2, num2);
			if (AttachmentSource.Scene.GetWaterLevelAtPosition(targetGlobalPosition2.AsVec2, useWaterRenderer: true, checkWaterBodyEntities: false) > targetGlobalPosition2.z)
			{
				SetAttachmentState(ShipAttachmentState.RopeFailedAndReloading);
				_launchFlightData.SourceGlobalPosition = targetGlobalPosition2;
				_launchFlightData.GlobalVelocity += MBGlobals.GravitationalAcceleration * _ropeThrownTimer;
				_launchFlightData.AngleDegree = num2;
				_launchFlightData.Time = Math.Min(2.5f, _ropeThrownTimer);
				_launchFlightData.IsUnderWater = true;
				_ropeThrownTimer = 0f;
				_currentRopeLength = 0f;
				SoundManager.StartOneShotEvent("event:/mission/movement/vessel/hook_impact_fail_water_splash", in targetGlobalPosition2);
				return;
			}
			if (targetGlobalPosition2.DistanceSquared(sourceGlobalPosition) > 1600f)
			{
				SetAttachmentState(ShipAttachmentState.RopeFailedAndReloading);
				_launchFlightData.SourceGlobalPosition = targetGlobalPosition2;
				_launchFlightData.GlobalVelocity = new Vec3(0f, 0f, _launchFlightData.GlobalVelocity.z - 9.806f * _ropeThrownTimer);
				_launchFlightData.AngleDegree = num2;
				_launchFlightData.Time = Math.Min(2.5f, _ropeThrownTimer);
				_ropeThrownTimer = 0f;
				_currentRopeLength = 0f;
				SoundManager.StartOneShotEvent("event:/mission/movement/vessel/hook_impact_fail_to_attach", in targetGlobalPosition2);
				return;
			}
			WeakGameEntity attachmentSourceHolderEntity = AttachmentSource.GameEntity.Parent;
			IEnumerable<WeakGameEntity> enumerable = from x in Mission.Current.GetActiveEntitiesWithScriptComponentOfType<ShipAttachmentPointMachine>()
				where x.Parent != attachmentSourceHolderEntity
				select x;
			ShipAttachmentPointMachine shipAttachmentPointMachine = null;
			foreach (WeakGameEntity item in enumerable)
			{
				ShipAttachmentPointMachine firstScriptOfType = item.GetFirstScriptOfType<ShipAttachmentPointMachine>();
				if (firstScriptOfType.CurrentAttachment == null && firstScriptOfType.LinkedAttachmentMachine?.CurrentAttachment == null && targetGlobalPosition2.DistanceSquared(item.GetGlobalFrame().TransformToParent(firstScriptOfType.HookAttachLocalPosition)) < 9f && Vec3.DotProduct(firstScriptOfType.GameEntity.GetGlobalFrame().rotation.f, targetGlobalPosition2 - item.GetGlobalFrame().TransformToParent(firstScriptOfType.HookAttachLocalPosition)) < 0f && ComputePotentialAttachmentValue(AttachmentSource, firstScriptOfType, checkInteractionDistance: false, checkConnectionBlock: true, allowWiderAngleBetweenConnections: true) > 0f)
				{
					shipAttachmentPointMachine = firstScriptOfType;
					targetGlobalPosition2.DistanceSquared(item.GetGlobalFrame().TransformToParent(firstScriptOfType.HookAttachLocalPosition));
					break;
				}
			}
			if (shipAttachmentPointMachine != null)
			{
				if (_attachmentInitializedByPlayer && AttachmentSource.OwnerShip != null && AttachmentSource.OwnerShip.Team != null && AttachmentSource.OwnerShip.Team.IsPlayerTeam)
				{
					AttachmentSource.OwnerShip?.ShipOrder?.SetBoardingTargetShip(shipAttachmentPointMachine.OwnerShip);
				}
				shipAttachmentPointMachine.AssignConnection(this);
				AttachmentTarget = shipAttachmentPointMachine;
				UpdateAttachmentMachineEntityVisibilities(_state);
				_launchFlightData.Time = _ropeThrownTimer;
				Vec3 position = shipAttachmentPointMachine.GameEntity.GetGlobalFrame().TransformToParent(shipAttachmentPointMachine.HookAttachLocalPosition);
				_launchFlightData.GlobalPositionError = targetGlobalPosition2 - position;
				if ((AttachmentSource.PilotStandingPoint.UserAgent != null && AttachmentSource.PilotStandingPoint.UserAgent.IsMainAgent) || (AttachmentSource.PilotStandingPoint.UserAgent == null && AttachmentSource.PilotStandingPoint.PreviousUserAgent != null && AttachmentSource.PilotStandingPoint.PreviousUserAgent.IsMainAgent))
				{
					_hookAttachSoundAlreadyTriggered = SoundManager.StartOneShotEvent("event:/mission/movement/vessel/hook_impact_attach", in position, "isPlayer", 1f);
				}
			}
			if (AttachmentTarget != null && CheckIntersectionsBetweenConnectionsWithState(AttachmentSource, AttachmentTarget, ShipAttachmentState.BridgeConnected))
			{
				SetAttachmentState(ShipAttachmentState.RopeFailedAndReloading);
				_launchFlightData.SourceGlobalPosition = targetGlobalPosition2;
				_launchFlightData.GlobalVelocity = new Vec3(0f, 0f, _launchFlightData.GlobalVelocity.z - 9.806f * _ropeThrownTimer);
				_launchFlightData.AngleDegree = num2;
				_launchFlightData.Time = Math.Min(2.5f, _ropeThrownTimer);
				_ropeThrownTimer = 0f;
				_currentRopeLength = 0f;
				SoundManager.StartOneShotEvent("event:/mission/movement/vessel/hook_impact_fail_to_attach", in targetGlobalPosition2);
			}
		}

		public void OnFixedTick(float fixedDt)
		{
			if (_state == ShipAttachmentState.RopesPulling || _state == ShipAttachmentState.BridgeConnected || _state == ShipAttachmentState.BridgeThrown)
			{
				ShipAttachmentJoint.OnFixedTick(fixedDt, this, ref _currentRopeLength);
			}
			if ((_state == ShipAttachmentState.BridgeConnected || _state == ShipAttachmentState.BridgeThrown) && CommittedAgentCount > 0f && CommittedTotalMass > 0f && CommittedWeightedPosition != Vec3.Zero)
			{
				Vec3 v = CommittedWeightedPosition / CommittedTotalMass;
				if (v.DistanceSquared(AttachmentSource.ConnectionClipPlaneEntity.GetGlobalFrameImpreciseForFixedTick().origin) < 25f)
				{
					MatrixFrame globalFrameImpreciseForFixedTick = AttachmentSource.ConnectionClipPlaneEntity.GetGlobalFrameImpreciseForFixedTick();
					Vec3 v2 = AttachmentTarget.ConnectionClipPlaneEntity.GetGlobalFrameImpreciseForFixedTick().origin - globalFrameImpreciseForFixedTick.origin;
					float num = v2.Normalize();
					Vec3 v3 = v - globalFrameImpreciseForFixedTick.origin;
					float num2 = Vec3.DotProduct(v2, v3) / num;
					MissionShip ownerShip = AttachmentSource.OwnerShip;
					MissionShip ownerShip2 = AttachmentSource.OwnerShip;
					Vec3 localPos = ownerShip.GameEntity.GetBodyWorldTransform().TransformToLocal(in v);
					Vec3 localPos2 = ownerShip2.GameEntity.GetBodyWorldTransform().TransformToLocal(in v);
					float stepAgentWeightMultiplier = ownerShip.Physics.PhysicsParameters.StepAgentWeightMultiplier;
					float stepAgentWeightMultiplier2 = ownerShip2.Physics.PhysicsParameters.StepAgentWeightMultiplier;
					Vec3 vec = CommittedTotalMass * MBGlobals.GravitationalAcceleration;
					ownerShip.Physics.ApplyGlobalForceAtLocalPos(in localPos, vec * ((1f - num2) * stepAgentWeightMultiplier));
					ownerShip2.Physics.ApplyGlobalForceAtLocalPos(in localPos2, vec * (num2 * stepAgentWeightMultiplier2));
				}
			}
			ClearCommittedAgentInformation();
		}

		private void ArrangeBarrier(GameEntity barrier, Vec3 startPosition, Vec3 endPosition, float height)
		{
			MatrixFrame frame = default(MatrixFrame);
			frame.origin = Vec3.Zero;
			frame.rotation = Mat3.Identity;
			_sideBarrierQuadsCached[0] = frame.TransformToLocal(startPosition + new Vec3(0f, 0f, height));
			_sideBarrierQuadsCached[1] = frame.TransformToLocal(endPosition + new Vec3(0f, 0f, height));
			_sideBarrierQuadsCached[2] = frame.TransformToLocal(in endPosition);
			_sideBarrierQuadsCached[3] = frame.TransformToLocal(in startPosition);
			barrier.ReplacePhysicsBodyWithQuadPhysicsBody(_sideBarriersQuadPinnedPointer, 4, _woodPhysicsMaterialCached, BodyFlags.Moveable | BodyFlags.AILimiter, _sideBarriersIndicesPinnedPointer, 6);
			barrier.SetGlobalFrame(in frame);
		}

		private void ConnectBridge()
		{
			for (int i = 0; i < 4; i++)
			{
				string prefabName = _shipConnectionPlankVariations[MBRandom.RandomInt(0, _shipConnectionPlankVariations.Count - 1)];
				GameEntity gameEntity = TaleWorlds.Engine.GameEntity.Instantiate(Mission.Current.Scene, prefabName, MatrixFrame.Identity);
				_bridge.AddChild(gameEntity);
				_targetSafetyPlanks.Add(gameEntity);
			}
			for (int j = 0; j < 4; j++)
			{
				string prefabName2 = _shipConnectionPlankVariations[MBRandom.RandomInt(0, _shipConnectionPlankVariations.Count - 1)];
				GameEntity gameEntity2 = TaleWorlds.Engine.GameEntity.Instantiate(Mission.Current.Scene, prefabName2, MatrixFrame.Identity);
				_bridge.AddChild(gameEntity2);
				_sourceSafetyPlanks.Add(gameEntity2);
			}
			_bridgeNavmeshId = Mission.Current.GetNextDynamicNavMeshIdStart();
			_navMeshBridge = TaleWorlds.Engine.GameEntity.Instantiate(Mission.Current.Scene, "ship_connection_nav_mesh_plank", MatrixFrame.Identity);
			_navMeshBridgeNavMeshHolder = _navMeshBridge.GetFirstChildEntityWithTag("navmesh_holder");
			_navMeshBridgeNavMeshHolder.CreateAndAddScriptComponent("ShipBridgeNavmeshHolder", callScriptCallbacks: true);
			_shipBridgeNavmeshHolder = _navMeshBridgeNavMeshHolder.GetFirstScriptOfType<ShipBridgeNavmeshHolder>();
			_shipBridgeNavmeshHolder.Initialize(_bridgeNavmeshId, AttachmentSource);
			SetAttachmentState(ShipAttachmentState.BridgeConnected);
			ArrangePlanksMT();
			ArrangePlanks();
			ArrangeNavmeshBridgeSideBarriersAndVFoldQuads();
			AddRopesToBridge();
			_bridge.CreateAndAddScriptComponent("ShipBridge", callScriptCallbacks: true);
			_shipBridgeNavmeshHolder.GameEntity.UpdateAttachedNavigationMeshFaces();
			_bridgeSwapTimer.Reset(Mission.Current.CurrentTime, 0.05f);
			_faceSwapSideOneDone = false;
			_faceSwapSideTwoDone = false;
			ShipIslandsConnected = false;
			AttachmentSource.OwnerShip.ShipsLogic.OnShipsConnected(AttachmentSource.OwnerShip, AttachmentTarget.OwnerShip);
		}

		private void SetShieldsVisibility(bool visible)
		{
			MBReadOnlyList<ShipShieldComponent> shields = AttachmentSource.OwnerShip.Shields;
			if (shields.Count > 0)
			{
				Vec3 origin = AttachmentSource.ConnectionClipPlaneEntity.ComputePreciseGlobalFrameForFixedTickSlow().origin;
				foreach (ShipShieldComponent item in shields)
				{
					if (item.GameEntity.IsValid)
					{
						if (visible)
						{
							item.DeregisterRampEntityDisablingShield(AttachmentSource.ConnectionClipPlaneEntity);
						}
						else if (item.GameEntity.ComputePreciseGlobalFrameForFixedTickSlow().origin.DistanceSquared(origin) < 3f)
						{
							item.RegisterRampEntityDisablingShield(AttachmentSource.ConnectionClipPlaneEntity);
						}
					}
				}
			}
			if (AttachmentTarget == null)
			{
				return;
			}
			MBReadOnlyList<ShipShieldComponent> shields2 = AttachmentTarget.OwnerShip.Shields;
			if (shields2.Count <= 0)
			{
				return;
			}
			Vec3 origin2 = AttachmentTarget.ConnectionClipPlaneEntity.ComputePreciseGlobalFrameForFixedTickSlow().origin;
			foreach (ShipShieldComponent item2 in shields2)
			{
				if (item2.GameEntity.IsValid)
				{
					if (visible)
					{
						item2.DeregisterRampEntityDisablingShield(AttachmentTarget.ConnectionClipPlaneEntity);
					}
					else if (item2.GameEntity.ComputePreciseGlobalFrameForFixedTickSlow().origin.DistanceSquared(origin2) < 3f)
					{
						item2.RegisterRampEntityDisablingShield(AttachmentTarget.ConnectionClipPlaneEntity);
					}
				}
			}
		}

		private void ArrangeNavmeshBridgeSideBarriersAndVFoldQuads()
		{
			MatrixFrame globalFrame = AttachmentSource.ConnectionClipPlaneEntity.GetGlobalFrame();
			MatrixFrame globalFrame2 = AttachmentTarget.ConnectionClipPlaneEntity.GetGlobalFrame();
			Vec3 s = globalFrame.rotation.s;
			s.Normalize();
			Vec3 s2 = globalFrame2.rotation.s;
			s2.Normalize();
			Vec3 vec = globalFrame2.origin - s2 * _plankHorizontalSize * 0.5f;
			Vec3 vec2 = globalFrame2.origin + s2 * _plankHorizontalSize * 0.5f;
			Vec3 vec3 = globalFrame.origin + s * _plankHorizontalSize * 0.5f;
			Vec3 vec4 = globalFrame.origin - s * _plankHorizontalSize * 0.5f;
			Vec3 vec5 = vec - vec3;
			vec5.Normalize();
			Vec3 vec6 = vec2 - vec4;
			vec6.Normalize();
			vec += vec5 * 0.05f;
			vec2 += vec6 * 0.05f;
			vec3 -= vec5 * 0.05f;
			vec4 -= vec6 * 0.05f;
			ArrangeBarrier(AttachmentSource.BarrierSource, vec2, vec4, 6f);
			ArrangeBarrier(AttachmentSource.BarrierTarget, vec3, vec, 6f);
			ArrangeVFoldQuads(vec3, vec4, vec2, vec);
			ArrangeNavMeshBridge(vec3, vec4, vec, vec2);
		}

		private void ArrangeVFoldQuads(Vec3 leftSource, Vec3 rightSource, Vec3 rightTarget, Vec3 leftTarget)
		{
			Vec3 v = (leftSource + rightSource) * 0.5f - Vec3.Up * 0.5f;
			Vec3 v2 = (leftTarget + rightTarget) * 0.5f - Vec3.Up * 0.5f;
			MatrixFrame frame = default(MatrixFrame);
			frame.origin = (leftSource + leftTarget + rightSource + rightTarget) * 0.25f;
			frame.rotation = Mat3.Identity;
			_vFoldQuadsCached[0] = frame.TransformToLocal(in leftSource);
			_vFoldQuadsCached[1] = frame.TransformToLocal(in leftTarget);
			_vFoldQuadsCached[2] = frame.TransformToLocal(in v2);
			_vFoldQuadsCached[3] = frame.TransformToLocal(in v);
			AttachmentSource.VFoldSource.ReplacePhysicsBodyWithQuadPhysicsBody(_vFoldQuadPinnedPointer, 4, _defaultPhysicsMaterialCached, BodyFlags.Moveable | BodyFlags.AgentOnly, _vFoldIndicesPinnedPointer, 6);
			AttachmentSource.VFoldSource.SetGlobalFrame(in frame);
			_vFoldQuadsCached[0] = frame.TransformToLocal(in rightSource);
			_vFoldQuadsCached[1] = frame.TransformToLocal(in v);
			_vFoldQuadsCached[2] = frame.TransformToLocal(in v2);
			_vFoldQuadsCached[3] = frame.TransformToLocal(in rightTarget);
			AttachmentSource.VFoldTarget.ReplacePhysicsBodyWithQuadPhysicsBody(_vFoldQuadPinnedPointer, 4, _defaultPhysicsMaterialCached, BodyFlags.Moveable | BodyFlags.AgentOnly, _vFoldIndicesPinnedPointer, 6);
			AttachmentSource.VFoldTarget.SetGlobalFrame(in frame);
		}

		private void StartBridgeThrowAnimation()
		{
			_targetSafetyPlanks.Clear();
			_sourceSafetyPlanks.Clear();
			_bridgeFlightData.DtSinceFlightStart = 0f;
			_bridgeFlightData.CurveLerpVelocity = 0f;
			_bridgeFlightData.CurveLerpValue = 0f;
			_bridgeFlightData.ThrowFinishValue = 7f;
			_currentRopeLength = AttachmentSource.ConnectionClipPlaneEntity.ComputePreciseGlobalFrameForFixedTickSlow().origin.Distance(AttachmentTarget.ConnectionClipPlaneEntity.ComputePreciseGlobalFrameForFixedTickSlow().origin);
			SetAttachmentState(ShipAttachmentState.BridgeThrown);
		}

		private void TickThrownBridge(float dt)
		{
			MatrixFrame globalFrame = AttachmentSource.ConnectionClipPlaneEntity.GetGlobalFrame();
			Vec3 initialPosition = globalFrame.origin;
			Vec3 destination = AttachmentTarget.ConnectionClipPlaneEntity.GetGlobalFrame().origin;
			if (MBMath.ApproximatelyEquals(destination.DistanceSquared(initialPosition), 0f))
			{
				destination += globalFrame.rotation.f * 0.1f + globalFrame.rotation.u * 0.1f;
			}
			float launchSpeed = 10.327f;
			float num = CalculateLaunchAngleDegree(initialPosition, destination, launchSpeed);
			if (num == float.MinValue)
			{
				num = TaleWorlds.Library.MathF.Clamp(num, TaleWorlds.Library.MathF.Min(44.9999f, CalculateDifferenceVectorAngle(in initialPosition, in destination) + 0.1f), 45f);
			}
			(_bridgeFlightData.CurrentFrameInitialVelocity, _bridgeFlightData.CurrentFrameTotalLightTime) = CalculateInitialVelocityAndTime(initialPosition, destination, num);
			_bridgeFlightData.DtSinceFlightStart += dt;
			_bridgeFlightData.CurveLerpVelocity += dt * 3f;
			if (_bridgeFlightData.CurrentFrameTotalLightTime <= _bridgeFlightData.DtSinceFlightStart)
			{
				_bridgeFlightData.CurveLerpValue += _bridgeFlightData.CurveLerpVelocity * dt;
				if (_bridgeFlightData.CurveLerpValue > _bridgeFlightData.ThrowFinishValue)
				{
					ConnectBridge();
					return;
				}
			}
			ArrangePlanksMT();
			ArrangePlanks();
		}

		private void SetOarsAvailability(bool value)
		{
			Vec3 origin = AttachmentSource.ConnectionClipPlaneEntity.ComputePreciseGlobalFrameForFixedTickSlow().origin;
			foreach (ShipOarMachine leftSideShipOarMachine in AttachmentSource.OwnerShip.LeftSideShipOarMachines)
			{
				if (value)
				{
					leftSideShipOarMachine.DeregisterRampEntityDisablingOar(AttachmentSource.ConnectionClipPlaneEntity);
				}
				else if (leftSideShipOarMachine.GameEntity.ComputePreciseGlobalFrameForFixedTickSlow().origin.DistanceSquared(origin) < 9f)
				{
					leftSideShipOarMachine.RegisterRampEntityDisablingOar(AttachmentSource.ConnectionClipPlaneEntity);
				}
			}
			foreach (ShipOarMachine rightSideShipOarMachine in AttachmentSource.OwnerShip.RightSideShipOarMachines)
			{
				if (value)
				{
					rightSideShipOarMachine.DeregisterRampEntityDisablingOar(AttachmentSource.ConnectionClipPlaneEntity);
				}
				else if (rightSideShipOarMachine.GameEntity.ComputePreciseGlobalFrameForFixedTickSlow().origin.DistanceSquared(origin) < 9f)
				{
					rightSideShipOarMachine.RegisterRampEntityDisablingOar(AttachmentSource.ConnectionClipPlaneEntity);
				}
			}
			if (AttachmentTarget == null)
			{
				return;
			}
			Vec3 origin2 = AttachmentTarget.ConnectionClipPlaneEntity.ComputePreciseGlobalFrameForFixedTickSlow().origin;
			foreach (ShipOarMachine leftSideShipOarMachine2 in AttachmentTarget.OwnerShip.LeftSideShipOarMachines)
			{
				if (value)
				{
					leftSideShipOarMachine2.DeregisterRampEntityDisablingOar(AttachmentTarget.ConnectionClipPlaneEntity);
				}
				else if (leftSideShipOarMachine2.GameEntity.ComputePreciseGlobalFrameForFixedTickSlow().origin.DistanceSquared(origin2) < 9f)
				{
					leftSideShipOarMachine2.RegisterRampEntityDisablingOar(AttachmentTarget.ConnectionClipPlaneEntity);
				}
			}
			foreach (ShipOarMachine rightSideShipOarMachine2 in AttachmentTarget.OwnerShip.RightSideShipOarMachines)
			{
				if (value)
				{
					rightSideShipOarMachine2.DeregisterRampEntityDisablingOar(AttachmentTarget.ConnectionClipPlaneEntity);
				}
				else if (rightSideShipOarMachine2.GameEntity.ComputePreciseGlobalFrameForFixedTickSlow().origin.DistanceSquared(origin2) < 9f)
				{
					rightSideShipOarMachine2.RegisterRampEntityDisablingOar(AttachmentTarget.ConnectionClipPlaneEntity);
				}
			}
		}

		private void AddRopesToBridge()
		{
			_ = _numberOfPlanksNeeded;
			int num = (int)((float)_numberOfPlanksNeeded * 0.16f + MBRandom.RandomFloat * (float)_numberOfPlanksNeeded * 0.16f);
			for (int i = 0; i < num; i++)
			{
				RopeSegment item = default(RopeSegment);
				int num2 = 1 + MBRandom.RandomInt(3);
				int num3 = _numberOfPlanksNeeded - 5;
				item.StartSegmentIndex = (int)(3f + MBRandom.RandomFloat * (float)(num3 - 3));
				item.EndSegmentIndex = item.StartSegmentIndex + num2;
				item.SideStartShift = MBRandom.RandomFloat - 0.5f;
				item.SideEndShift = MBRandom.RandomFloat - 0.5f;
				if (item.StartSegmentIndex >= item.EndSegmentIndex || item.StartSegmentIndex <= 0 || item.EndSegmentIndex <= 0 || item.StartSegmentIndex >= _numberOfPlanksNeeded || item.EndSegmentIndex >= _numberOfPlanksNeeded)
				{
					continue;
				}
				GameEntity gameEntity = TaleWorlds.Engine.GameEntity.Instantiate(Mission.Current.Scene, "simple_rope_nested", MatrixFrame.Identity);
				_bridge.AddChild(gameEntity);
				item.ParentEntity = gameEntity;
				item.ParentEntity.SetDoNotCheckVisibility(value: true);
				item.RopeStart = gameEntity.GetFirstChildEntityWithTag("simple_rope_start");
				item.RopeEnd = gameEntity.GetFirstChildEntityWithTag("simple_rope_end");
				if (!(item.RopeStart != null) || !(item.RopeEnd != null))
				{
					continue;
				}
				NavalDLC.Missions.Objects.RopeSegment firstScriptOfType = item.RopeStart.GetFirstScriptOfType<NavalDLC.Missions.Objects.RopeSegment>();
				if (firstScriptOfType != null)
				{
					firstScriptOfType.SetAsFixedEntity();
					firstScriptOfType.SetRuntimeLooseMultiplier(2f);
				}
				_ropes.Add(item);
				if (MBRandom.RandomFloat > 0.6f)
				{
					int num4 = MBRandom.RandomInt(1, 2);
					for (int j = 0; j < num4; j++)
					{
						string prefabName = _ropeClothFragmentPrefabList[MBRandom.RandomInt(0, _ropeClothFragmentPrefabList.Count - 1)];
						GameEntity gameEntity2 = TaleWorlds.Engine.GameEntity.Instantiate(Mission.Current.Scene, prefabName, MatrixFrame.Identity);
						item.RopeStart.AddChild(gameEntity2);
					}
				}
			}
		}

		private void ArrangeNavMeshBridge(Vec3 leftSource, Vec3 rightSource, Vec3 leftTarget, Vec3 rightTarget)
		{
			if (!(_navMeshBridge == null) && AttachmentSource != null && AttachmentTarget != null)
			{
				Vec3 p = AttachmentSource.GameEntity.GlobalPosition;
				Vec3 p2 = AttachmentTarget.GameEntity.GlobalPosition;
				p.Distance(p2);
				MatrixFrame frame = MatrixFrame.CenterFrameOfTwoPoints(in p, in p2, Vec3.Up);
				frame.origin.z += 1.1f;
				frame.rotation.Orthonormalize();
				_navMeshBridge.SetFrame(ref frame);
				_shipBridgeNavmeshHolder.SetShipBridgeStartEndPositions(leftSource, rightSource, leftTarget, rightTarget);
				bool flag = IsNavmeshBridgeEntityUpsideDown();
				if (flag != _isNavmeshBridgeDisabled)
				{
					SetAbilityOfNavmeshBridgeFaces(!flag);
					_isNavmeshBridgeDisabled = flag;
				}
			}
		}

		public void Destroy()
		{
			if (_bridgeCreated)
			{
				bool num = _faceSwapSideOneDone || _faceSwapSideTwoDone;
				if (_faceSwapSideOneDone)
				{
					Mission.Current.Scene.SwapFaceConnectionsWithID(_bridgeNavmeshId + 1, AttachmentTarget.RelatedShipNavmeshOffset + AttachmentTarget.OwnerShip.GetDynamicNavmeshIdStart(), _bridgeNavmeshId + 3, canFail: true);
					_faceSwapSideOneDone = false;
				}
				if (_faceSwapSideTwoDone)
				{
					Mission.Current.Scene.SwapFaceConnectionsWithID(_bridgeNavmeshId + 2, AttachmentSource.RelatedShipNavmeshOffset + AttachmentSource.OwnerShip.GetDynamicNavmeshIdStart(), _bridgeNavmeshId + 4, canFail: true);
					_faceSwapSideTwoDone = false;
				}
				if (num)
				{
					AttachmentSource.OwnerShip.SeparateFromShip(AttachmentTarget.OwnerShip);
				}
				MatrixFrame globalFrame = AttachmentSource.PlankBridgePhysicsEntity.GetGlobalFrame();
				SoundManager.StartOneShotEvent("event:/mission/movement/vessel/bridge_fall", in globalFrame.origin);
			}
			AttachmentSource.CurrentAttachment = null;
			AttachmentTarget?.AssignConnection(null);
			if (_planks != null)
			{
				foreach (GameEntity plank in _planks)
				{
					plank.Remove(78);
				}
				_planks = null;
			}
			if (_targetSafetyPlanks != null)
			{
				foreach (GameEntity targetSafetyPlank in _targetSafetyPlanks)
				{
					targetSafetyPlank.Remove(35);
				}
				_targetSafetyPlanks = null;
			}
			if (_sourceSafetyPlanks != null)
			{
				foreach (GameEntity sourceSafetyPlank in _sourceSafetyPlanks)
				{
					sourceSafetyPlank.Remove(35);
				}
				_sourceSafetyPlanks = null;
			}
			if (_navMeshBridge != null)
			{
				_navMeshBridge.Remove(78);
				Mission.Current.Scene.SetAbilityOfFacesWithId(_bridgeNavmeshId, isEnabled: false);
				Mission.Current.Scene.SetAbilityOfFacesWithId(_bridgeNavmeshId + 1, isEnabled: false);
				Mission.Current.Scene.SetAbilityOfFacesWithId(_bridgeNavmeshId + 2, isEnabled: false);
				Mission.Current.Scene.SetAbilityOfFacesWithId(_bridgeNavmeshId + 3, isEnabled: false);
				Mission.Current.Scene.SetAbilityOfFacesWithId(_bridgeNavmeshId + 4, isEnabled: false);
				_navMeshBridge = null;
			}
			AttachmentSource.SetConnectionPhysicsEntitiesVisibility(visible: false);
			AttachmentTarget?.SetPhysicsEntitiesVisibility(isEnabled: false);
			if (_ropes != null)
			{
				foreach (RopeSegment rope in _ropes)
				{
					rope.ParentEntity.Remove(45);
				}
				_ropes = null;
			}
			if (_bridge != null)
			{
				_bridge.Remove(78);
				_bridge = null;
			}
			_bridgeCurveLinearAccessCache = null;
			if (_currentFramePlankPhysicsVerticesPinnedPointer != UIntPtr.Zero)
			{
				_currentFramePlankPhysicsVerticesPinnedGCHandler.Free();
				_currentFramePlankPhysicsVerticesPinnedPointer = UIntPtr.Zero;
			}
			if (_currentFramePlankPhysicsIndicesPinnedPointer != UIntPtr.Zero)
			{
				_currentFramePlankPhysicsIndicesPinnedGCHandler.Free();
				_currentFramePlankPhysicsIndicesPinnedPointer = UIntPtr.Zero;
			}
			if (_sideBarriersQuadPinnedPointer != UIntPtr.Zero)
			{
				_sideBarriersQuadPinnedGCHandler.Free();
				_sideBarriersQuadPinnedPointer = UIntPtr.Zero;
			}
			if (_sideBarriersIndicesPinnedPointer != UIntPtr.Zero)
			{
				_sideBarriersIndicesPinnedGCHandler.Free();
				_sideBarriersIndicesPinnedPointer = UIntPtr.Zero;
			}
			if (_vFoldQuadPinnedPointer != UIntPtr.Zero)
			{
				_vFoldQuadPinnedGCHandler.Free();
				_vFoldQuadPinnedPointer = UIntPtr.Zero;
			}
			if (_vFoldIndicesPinnedPointer != UIntPtr.Zero)
			{
				_vFoldIndicesPinnedGCHandler.Free();
				_vFoldIndicesPinnedPointer = UIntPtr.Zero;
			}
		}

		private Vec3 GetCurvePositionFromLength(float currentLength)
		{
			int num = Array.BinarySearch(_bridgeCurveLinearAccessCache, new KeyValuePair<float, Vec3>(currentLength, Vec3.Zero), _cacheCompareDelegate);
			if (num >= 0)
			{
				return _bridgeCurveLinearAccessCache[num].Value;
			}
			int num2 = ~num;
			int num3 = num2 - 1;
			KeyValuePair<float, Vec3> keyValuePair = _bridgeCurveLinearAccessCache[num3];
			KeyValuePair<float, Vec3> keyValuePair2 = _bridgeCurveLinearAccessCache[num2];
			float alpha = (currentLength - keyValuePair.Key) / (keyValuePair2.Key - keyValuePair.Key);
			return Vec3.Lerp(keyValuePair.Value, keyValuePair2.Value, alpha);
		}

		private void SetRopeMeshParams(Mesh ropeMesh, Vec3 start, Vec3 end, float length)
		{
			if (ropeMesh != null)
			{
				MatrixFrame frame = MatrixFrame.Identity;
				frame.rotation.s = start;
				frame.origin = end;
				ropeMesh.SetAdditionalBoneFrame(0, in frame);
				ropeMesh.SetAdditionalBoneFrame(1, MatrixFrame.Identity);
				Vec3 vectorArgument = ropeMesh.GetVectorArgument();
				vectorArgument.x = length;
				vectorArgument.y = 25.9f;
				vectorArgument.z = 1f;
				ropeMesh.SetVectorArgument(vectorArgument.x, vectorArgument.y, vectorArgument.z, vectorArgument.w);
			}
		}

		private static Vec3 GetPositionAtProjectileCurveProgress(in Vec3 globalVelocity, in Vec3 sourceGlobalPosition, float time, float progressInterval)
		{
			time *= progressInterval;
			return sourceGlobalPosition + globalVelocity * time + 0.5f * MBGlobals.GravitationalAcceleration * time * time;
		}

		private void SetAbilityOfNavmeshBridgeFaces(bool enable)
		{
			Mission.Current.Scene.SetAbilityOfFacesWithId(_bridgeNavmeshId, enable);
			Mission.Current.Scene.SetAbilityOfFacesWithId(_bridgeNavmeshId + 1, enable);
			Mission.Current.Scene.SetAbilityOfFacesWithId(_bridgeNavmeshId + 2, enable);
			Mission.Current.Scene.SetAbilityOfFacesWithId(_bridgeNavmeshId + 3, enable);
			Mission.Current.Scene.SetAbilityOfFacesWithId(_bridgeNavmeshId + 4, enable);
		}

		private bool IsNavmeshBridgeEntityUpsideDown()
		{
			return _navMeshBridge.GetGlobalFrame().rotation.u.z <= 0.35f;
		}

		private void AddNewClipPlaneIntersectionPoint(ref int numberOfValidVertices, in Vec3 currentCorner)
		{
			if (numberOfValidVertices < 5)
			{
				_registeredVerticesAfterPhysicsClipPlaneIntersection[numberOfValidVertices] = currentCorner;
				numberOfValidVertices++;
			}
		}

		private void ArrangePlankPhysicsWithClipPlanes(Vec3[] quadVerticesCCW, MatrixFrame firstClipFrame, MatrixFrame secondClipFrame)
		{
			_alreadyAddedVertexDataForPhysicsClipPlaneIntersection[0] = 0;
			_alreadyAddedVertexDataForPhysicsClipPlaneIntersection[1] = 0;
			_alreadyAddedVertexDataForPhysicsClipPlaneIntersection[2] = 0;
			_alreadyAddedVertexDataForPhysicsClipPlaneIntersection[3] = 0;
			int numberOfValidVertices = 0;
			bool flag = false;
			for (int i = 0; i < 4; i++)
			{
				Vec3 point = quadVerticesCCW[i];
				int num = (i + 1) % 4;
				Vec3 rayOrigin = quadVerticesCCW[num];
				if (MBMath.PointLiesAheadOfPlane(in firstClipFrame.rotation.f, in firstClipFrame.origin, in point))
				{
					Vec3 rayDirection = rayOrigin - point;
					float num2 = rayDirection.Normalize();
					if (MBMath.GetRayPlaneIntersectionPoint(-firstClipFrame.rotation.f, in firstClipFrame.origin, in point, in rayDirection, out var t) && t < num2)
					{
						Vec3 currentCorner = point + rayDirection * t;
						if (_alreadyAddedVertexDataForPhysicsClipPlaneIntersection[i] == 0)
						{
							AddNewClipPlaneIntersectionPoint(ref numberOfValidVertices, in point);
							_alreadyAddedVertexDataForPhysicsClipPlaneIntersection[i] = 1;
						}
						AddNewClipPlaneIntersectionPoint(ref numberOfValidVertices, in currentCorner);
						flag = true;
						continue;
					}
					if (_alreadyAddedVertexDataForPhysicsClipPlaneIntersection[i] == 0)
					{
						AddNewClipPlaneIntersectionPoint(ref numberOfValidVertices, in point);
						_alreadyAddedVertexDataForPhysicsClipPlaneIntersection[i] = 1;
					}
					if (_alreadyAddedVertexDataForPhysicsClipPlaneIntersection[num] == 0)
					{
						AddNewClipPlaneIntersectionPoint(ref numberOfValidVertices, in rayOrigin);
						_alreadyAddedVertexDataForPhysicsClipPlaneIntersection[num] = 1;
					}
					continue;
				}
				flag = true;
				Vec3 rayDirection2 = point - rayOrigin;
				float num3 = rayDirection2.Normalize();
				if (MBMath.GetRayPlaneIntersectionPoint(-firstClipFrame.rotation.f, in firstClipFrame.origin, in rayOrigin, in rayDirection2, out var t2) && t2 < num3)
				{
					AddNewClipPlaneIntersectionPoint(ref numberOfValidVertices, rayOrigin + rayDirection2 * t2);
					if (_alreadyAddedVertexDataForPhysicsClipPlaneIntersection[num] == 0)
					{
						AddNewClipPlaneIntersectionPoint(ref numberOfValidVertices, in rayOrigin);
						_alreadyAddedVertexDataForPhysicsClipPlaneIntersection[num] = 1;
					}
				}
			}
			if (!flag)
			{
				_alreadyAddedVertexDataForPhysicsClipPlaneIntersection[0] = 0;
				_alreadyAddedVertexDataForPhysicsClipPlaneIntersection[1] = 0;
				_alreadyAddedVertexDataForPhysicsClipPlaneIntersection[2] = 0;
				_alreadyAddedVertexDataForPhysicsClipPlaneIntersection[3] = 0;
				numberOfValidVertices = 0;
				for (int j = 0; j < 4; j++)
				{
					Vec3 point2 = quadVerticesCCW[j];
					int num4 = (j + 1) % 4;
					Vec3 rayOrigin2 = quadVerticesCCW[num4];
					if (MBMath.PointLiesAheadOfPlane(in secondClipFrame.rotation.f, in secondClipFrame.origin, in point2))
					{
						Vec3 rayDirection3 = rayOrigin2 - point2;
						float num5 = rayDirection3.Normalize();
						if (MBMath.GetRayPlaneIntersectionPoint(-secondClipFrame.rotation.f, in secondClipFrame.origin, in point2, in rayDirection3, out var t3) && t3 < num5)
						{
							Vec3 currentCorner2 = point2 + rayDirection3 * t3;
							if (_alreadyAddedVertexDataForPhysicsClipPlaneIntersection[j] == 0)
							{
								AddNewClipPlaneIntersectionPoint(ref numberOfValidVertices, in point2);
								_alreadyAddedVertexDataForPhysicsClipPlaneIntersection[j] = 1;
							}
							AddNewClipPlaneIntersectionPoint(ref numberOfValidVertices, in currentCorner2);
							continue;
						}
						if (_alreadyAddedVertexDataForPhysicsClipPlaneIntersection[j] == 0)
						{
							AddNewClipPlaneIntersectionPoint(ref numberOfValidVertices, in point2);
							_alreadyAddedVertexDataForPhysicsClipPlaneIntersection[j] = 1;
						}
						if (_alreadyAddedVertexDataForPhysicsClipPlaneIntersection[num4] == 0)
						{
							AddNewClipPlaneIntersectionPoint(ref numberOfValidVertices, in rayOrigin2);
							_alreadyAddedVertexDataForPhysicsClipPlaneIntersection[num4] = 1;
						}
						continue;
					}
					Vec3 rayDirection4 = point2 - rayOrigin2;
					float num6 = rayDirection4.Normalize();
					if (MBMath.GetRayPlaneIntersectionPoint(-secondClipFrame.rotation.f, in secondClipFrame.origin, in rayOrigin2, in rayDirection4, out var t4) && t4 < num6)
					{
						AddNewClipPlaneIntersectionPoint(ref numberOfValidVertices, rayOrigin2 + rayDirection4 * t4);
						if (_alreadyAddedVertexDataForPhysicsClipPlaneIntersection[num4] == 0)
						{
							AddNewClipPlaneIntersectionPoint(ref numberOfValidVertices, in rayOrigin2);
							_alreadyAddedVertexDataForPhysicsClipPlaneIntersection[num4] = 1;
						}
					}
				}
			}
			if (numberOfValidVertices < 3)
			{
				return;
			}
			bool flag2 = true;
			for (int k = 0; k < numberOfValidVertices; k++)
			{
				Vec3 vec = _registeredVerticesAfterPhysicsClipPlaneIntersection[k];
				Vec3 v = _registeredVerticesAfterPhysicsClipPlaneIntersection[(k + 1) % numberOfValidVertices];
				if (vec.DistanceSquared(v) < 1E-06f)
				{
					flag2 = false;
					break;
				}
			}
			if (!flag2)
			{
				return;
			}
			int num7 = 0;
			for (int l = 0; l < numberOfValidVertices; l++)
			{
				int num8 = AddNewVertexToPlankPhysics(_registeredVerticesAfterPhysicsClipPlaneIntersection[l]);
				if (num8 == -1)
				{
					return;
				}
				if (l == 0)
				{
					num7 = num8;
				}
			}
			int num9 = numberOfValidVertices - 2;
			for (int m = 0; m < num9; m++)
			{
				AddNewIndexToPlankPhysics(num7);
				AddNewIndexToPlankPhysics(num7 + m + 1);
				AddNewIndexToPlankPhysics(num7 + m + 2);
			}
		}

		private int AddNewVertexToPlankPhysics(Vec3 vertex)
		{
			if (_currentFramePlankPhysicsVertices.Length > _currentFramePlankPhysicsVertexCount)
			{
				_currentFramePlankPhysicsVertices[_currentFramePlankPhysicsVertexCount] = vertex;
				int currentFramePlankPhysicsVertexCount = _currentFramePlankPhysicsVertexCount;
				_currentFramePlankPhysicsVertexCount++;
				return currentFramePlankPhysicsVertexCount;
			}
			return -1;
		}

		private void AddNewIndexToPlankPhysics(int index)
		{
			if (_currentFramePlankPhysicsIndices.Length > _currentFramePlankPhysicsIndexCount)
			{
				_currentFramePlankPhysicsIndices[_currentFramePlankPhysicsIndexCount] = index;
				_currentFramePlankPhysicsIndexCount++;
			}
		}

		private void TransformCurrentFramePlankPhysicsVerticesToPhysicsEntityLocal(Vec3 physicsEntityGlobalPosition)
		{
			for (int i = 0; i < _currentFramePlankPhysicsVertices.Length; i++)
			{
				_currentFramePlankPhysicsVertices[i] -= physicsEntityGlobalPosition;
			}
		}

		private void SpawnPlankEntities()
		{
			_bridge = TaleWorlds.Engine.GameEntity.CreateEmpty(Mission.Current.Scene);
			for (int i = _planks.Count; i < 80; i++)
			{
				string prefabName = _shipConnectionPlankVariations[MBRandom.RandomInt(0, _shipConnectionPlankVariations.Count - 1)];
				GameEntity gameEntity = TaleWorlds.Engine.GameEntity.Instantiate(Mission.Current.Scene, prefabName, MatrixFrame.Identity);
				_bridge.AddChild(gameEntity);
				_planks.Add(gameEntity);
				gameEntity.SetupAdditionalBoneBufferForMeshes(1);
			}
		}

		private void FillBridgeCurveAccessData(in Vec3 plankTargetOrigin, in Vec3 plankSourceOrigin, in float curvedLength)
		{
			_bridgeCurveLinearAccessCache[0] = new KeyValuePair<float, Vec3>(0f, plankTargetOrigin);
			Vec3 v = plankTargetOrigin;
			float num = 1f / 15f;
			float num2 = 0f;
			for (int i = 1; i < 15; i++)
			{
				Vec3 vec = NavalDLC.Missions.Objects.RopeSegment.CalculateAutoCurvePosition(plankTargetOrigin, plankSourceOrigin, curvedLength, (float)i * num);
				float num3 = vec.Distance(v);
				num2 += num3;
				_bridgeCurveLinearAccessCache[i] = new KeyValuePair<float, Vec3>(num2, vec);
				v = vec;
			}
			_bridgeCurveLinearAccessCache[15] = new KeyValuePair<float, Vec3>(curvedLength, plankSourceOrigin);
		}

		private void ArrangePlanksMT()
		{
			Vec3 localPosition = (AttachmentSource.GameEntity.GetGlobalFrame().origin + AttachmentTarget.GameEntity.GetGlobalFrame().origin) * 0.5f;
			AttachmentSource.PlankBridgePhysicsEntity.SetLocalPosition(localPosition);
			_currentFramePlankPhysicsIndexCount = 0;
			_currentFramePlankPhysicsVertexCount = 0;
			MatrixFrame globalFrame = AttachmentSource.ConnectionClipPlaneEntity.GetGlobalFrame();
			Vec3 plankSourceOrigin = globalFrame.origin;
			MatrixFrame globalFrame2 = AttachmentTarget.ConnectionClipPlaneEntity.GetGlobalFrame();
			Vec3 plankTargetOrigin = globalFrame2.origin;
			Vec3 f = plankSourceOrigin - plankTargetOrigin;
			f.Normalize();
			MatrixFrame identity = MatrixFrame.Identity;
			identity.rotation.f = f;
			identity.rotation.s = f.CrossProductWithUp();
			identity.rotation.s.Normalize();
			identity.rotation.u = Vec3.CrossProduct(identity.rotation.s, identity.rotation.f);
			identity.rotation.u.Normalize();
			float num = plankSourceOrigin.Distance(plankTargetOrigin);
			float num2 = 1.035f;
			if (_state == ShipAttachmentState.BridgeThrown)
			{
				float num3 = TaleWorlds.Library.MathF.Sin(_bridgeFlightData.CurveLerpVelocity * System.MathF.PI);
				float num4 = (_bridgeFlightData.ThrowFinishValue - _bridgeFlightData.CurveLerpValue) / _bridgeFlightData.ThrowFinishValue;
				float num5 = Math.Min((_bridgeFlightData.CurveLerpValue - 0.5f) * 2f, 1f);
				num2 += num3 * num4 * num5 * 0.028f;
			}
			_previousNumberOfPlanksNeeded = _numberOfPlanksNeeded;
			float curvedLength = num * num2;
			_numberOfPlanksNeeded = TaleWorlds.Library.MathF.Max(TaleWorlds.Library.MathF.Ceiling(curvedLength / _plankVerticalSize), 2);
			_numberOfPlanksNeeded = Math.Min(_numberOfPlanksNeeded, 80);
			FillBridgeCurveAccessData(in plankTargetOrigin, in plankSourceOrigin, in curvedLength);
			Vec3 vb = -globalFrame.rotation.s;
			MatrixFrame identity2 = MatrixFrame.Identity;
			identity2.origin = GetCurvePositionFromLength(0f);
			Vec3 curvePositionFromLength = GetCurvePositionFromLength(TaleWorlds.Library.MathF.Min(_plankVerticalSize, curvedLength));
			identity2.rotation.f = curvePositionFromLength - identity2.origin;
			identity2.rotation.f.Normalize();
			identity2.rotation.f.CrossProductWithUp().Normalize();
			Vec3 s = globalFrame2.rotation.s;
			s.Normalize();
			Vec3 va = Vec3.CrossProduct(globalFrame.rotation.f, vb);
			va.Normalize();
			vb = Vec3.CrossProduct(va, globalFrame.rotation.f);
			vb.Normalize();
			float num6 = (float)Math.Acos(Vec3.DotProduct(vb, s));
			if (Vec3.DotProduct(Vec3.CrossProduct(s, vb), globalFrame.rotation.f) < 0f)
			{
				num6 *= -1f;
			}
			float a = num6 / (float)_numberOfPlanksNeeded;
			Vec3 s2 = s;
			for (int i = 0; i < _numberOfPlanksNeeded; i++)
			{
				bool visibilityExcludeParents = true;
				GameEntity gameEntity = _planks[i];
				MatrixFrame m = MatrixFrame.Identity;
				m.origin = GetCurvePositionFromLength(TaleWorlds.Library.MathF.Min((float)i * _plankVerticalSize, curvedLength));
				Vec3 curvePositionFromLength2 = GetCurvePositionFromLength(TaleWorlds.Library.MathF.Min((float)(i + 1) * _plankVerticalSize, curvedLength));
				m.rotation.f = curvePositionFromLength2 - m.origin;
				if (m.rotation.f.LengthSquared > 0f)
				{
					m.rotation.f.Normalize();
				}
				else
				{
					m.rotation.f = f;
				}
				m.rotation.f *= 1.06f;
				m.rotation.s = s2;
				m.rotation.s.Normalize();
				m.rotation.u = Vec3.CrossProduct(m.rotation.s, m.rotation.f);
				m.rotation.u.Normalize();
				MatrixFrame frame = MatrixFrame.Identity;
				frame.rotation.RotateAboutForward(a);
				gameEntity.SetBoneFrameToAllMeshes(0, in frame);
				gameEntity.SetVectorArgument(1f / _plankVerticalSize, 0f, 0f, 0f);
				s2 = Vec3.Lerp(s, vb, (float)i / (float)_numberOfPlanksNeeded);
				if (_state == ShipAttachmentState.BridgeThrown)
				{
					MatrixFrame m2 = MatrixFrame.Identity;
					float time = TaleWorlds.Library.MathF.Min(_bridgeFlightData.DtSinceFlightStart, _bridgeFlightData.CurrentFrameTotalLightTime);
					int num7 = _numberOfPlanksNeeded - i - 1;
					m2.origin = GetPositionAtProjectileCurveProgress(progressInterval: (float)num7 / (float)(_numberOfPlanksNeeded - 1), globalVelocity: in _bridgeFlightData.CurrentFrameInitialVelocity, sourceGlobalPosition: in plankSourceOrigin, time: time);
					Vec3 vec = GetPositionAtProjectileCurveProgress(progressInterval: (float)(num7 - 1) / (float)(_numberOfPlanksNeeded - 1), globalVelocity: in _bridgeFlightData.CurrentFrameInitialVelocity, sourceGlobalPosition: in plankSourceOrigin, time: time);
					m2.rotation.f = vec - m2.origin;
					if ((double)m2.rotation.f.LengthSquared < 0.1)
					{
						visibilityExcludeParents = false;
					}
					else
					{
						m2.rotation.f.Normalize();
						m2.rotation.s = m2.rotation.f.CrossProductWithUp();
						m2.rotation.s.Normalize();
						m2.rotation.u = Vec3.CrossProduct(m2.rotation.s, m2.rotation.f);
						m2.rotation.u.Normalize();
					}
					float alpha = Math.Min(_bridgeFlightData.CurveLerpValue, 1f);
					m = MatrixFrame.Lerp(in m2, in m, alpha);
				}
				gameEntity.SetGlobalFrame(in m);
				gameEntity.SetVisibilityExcludeParents(visibilityExcludeParents);
				gameEntity.SetCustomClipPlane(Vec3.Zero, Vec3.Zero, setForChildren: true);
				if (_state == ShipAttachmentState.BridgeConnected || _state == ShipAttachmentState.BridgeThrown)
				{
					Vec3 vec2 = m.TransformToParent(new Vec3((0f - _plankHorizontalSize) * 0.5f, -0.2f));
					Vec3 vec3 = m.TransformToParent(new Vec3(_plankHorizontalSize * 0.5f, -0.2f));
					Vec3 vec4 = m.TransformToParent(new Vec3((0f - _plankHorizontalSize) * 0.5f, 0.2f + _plankVerticalSize));
					Vec3 vec5 = m.TransformToParent(new Vec3(_plankHorizontalSize * 0.5f, 0.2f + _plankVerticalSize));
					_quadVerticesCCWCached[0] = vec2;
					_quadVerticesCCWCached[1] = vec3;
					_quadVerticesCCWCached[2] = vec5;
					_quadVerticesCCWCached[3] = vec4;
					ArrangePlankPhysicsWithClipPlanes(_quadVerticesCCWCached, globalFrame, globalFrame2);
				}
			}
			for (int j = _numberOfPlanksNeeded; j < _previousNumberOfPlanksNeeded; j++)
			{
				_planks[j].SetVisibilityExcludeParents(visible: false);
			}
			if ((_state == ShipAttachmentState.BridgeConnected || _state == ShipAttachmentState.BridgeThrown) && _numberOfPlanksNeeded > 0)
			{
				MatrixFrame globalFrame3 = _planks[_numberOfPlanksNeeded - 1].GetGlobalFrame();
				Vec3 vec6 = globalFrame3.origin + globalFrame3.rotation.f * _plankVerticalSize;
				MatrixFrame identity3 = MatrixFrame.Identity;
				identity3.rotation.u = globalFrame3.rotation.u;
				identity3.rotation.u.Normalize();
				identity3.rotation.s = Vec3.CrossProduct(globalFrame3.rotation.f, identity3.rotation.u);
				identity3.rotation.s.Normalize();
				identity3.rotation.f = Vec3.CrossProduct(identity3.rotation.u, identity3.rotation.s);
				identity3.rotation.f.Normalize();
				for (int k = 0; k < _sourceSafetyPlanks.Count; k++)
				{
					GameEntity gameEntity2 = _sourceSafetyPlanks[k];
					gameEntity2.SetVisibilityExcludeParents(visible: false);
					MatrixFrame frame2 = MatrixFrame.Identity;
					frame2.origin = vec6 + identity3.rotation.f * _plankVerticalSize * k;
					frame2.rotation = identity3.rotation;
					gameEntity2.SetGlobalFrame(in frame2);
					gameEntity2.SetCustomClipPlane(plankSourceOrigin, globalFrame.rotation.f, setForChildren: true);
					Vec3 vec7 = frame2.TransformToParent(new Vec3((0f - _plankHorizontalSize) * 0.5f, -0.2f));
					Vec3 vec8 = frame2.TransformToParent(new Vec3(_plankHorizontalSize * 0.5f, -0.2f));
					Vec3 vec9 = frame2.TransformToParent(new Vec3((0f - _plankHorizontalSize) * 0.5f, 0.2f + _plankVerticalSize));
					Vec3 vec10 = frame2.TransformToParent(new Vec3(_plankHorizontalSize * 0.5f, 0.2f + _plankVerticalSize));
					_quadVerticesCCWCached[0] = vec7;
					_quadVerticesCCWCached[1] = vec8;
					_quadVerticesCCWCached[2] = vec10;
					_quadVerticesCCWCached[3] = vec9;
					ArrangePlankPhysicsWithClipPlanes(_quadVerticesCCWCached, globalFrame, globalFrame2);
				}
				MatrixFrame globalFrame4 = _planks[0].GetGlobalFrame();
				for (int l = 0; l < _targetSafetyPlanks.Count; l++)
				{
					GameEntity gameEntity3 = _targetSafetyPlanks[l];
					gameEntity3.SetVisibilityExcludeParents(visible: false);
					MatrixFrame frame3 = MatrixFrame.Identity;
					frame3.origin = globalFrame4.origin - globalFrame4.rotation.f * _plankVerticalSize * (l + 1);
					frame3.rotation = globalFrame4.rotation;
					gameEntity3.SetGlobalFrame(in frame3);
					gameEntity3.SetCustomClipPlane(plankTargetOrigin, globalFrame2.rotation.f, setForChildren: true);
					Vec3 vec11 = frame3.TransformToParent(new Vec3((0f - _plankHorizontalSize) * 0.5f, -0.2f));
					Vec3 vec12 = frame3.TransformToParent(new Vec3(_plankHorizontalSize * 0.5f, -0.2f));
					Vec3 vec13 = frame3.TransformToParent(new Vec3((0f - _plankHorizontalSize) * 0.5f, 0.2f + _plankVerticalSize));
					Vec3 vec14 = frame3.TransformToParent(new Vec3(_plankHorizontalSize * 0.5f, 0.2f + _plankVerticalSize));
					ArrangePlankPhysicsWithClipPlanes(new Vec3[4] { vec11, vec12, vec14, vec13 }, globalFrame, globalFrame2);
				}
			}
			for (int n = 0; n < 3 && n < _planks.Count; n++)
			{
				_planks[n].SetCustomClipPlane(plankTargetOrigin, globalFrame2.rotation.f, setForChildren: true);
			}
			for (int num8 = 0; num8 < 3; num8++)
			{
				int num9 = _numberOfPlanksNeeded - 1 - num8;
				if (num9 >= 0)
				{
					_planks[num9].SetCustomClipPlane(plankSourceOrigin, globalFrame.rotation.f, setForChildren: true);
				}
			}
			foreach (RopeSegment rope in _ropes)
			{
				Vec3 vec15 = rope.SideStartShift * identity.rotation.s * _plankHorizontalSize;
				Vec3 vec16 = rope.SideEndShift * identity.rotation.s * _plankHorizontalSize;
				int startSegmentIndex = rope.StartSegmentIndex;
				int num10 = Math.Min(rope.EndSegmentIndex, _numberOfPlanksNeeded - 1);
				if (startSegmentIndex >= num10)
				{
					rope.ParentEntity.SetVisibilityExcludeParents(visible: false);
					continue;
				}
				MatrixFrame frame4 = rope.RopeStart.GetGlobalFrame();
				frame4.origin = _planks[startSegmentIndex].GetGlobalFrame().origin + vec15;
				rope.RopeStart.SetGlobalFrame(in frame4);
				MatrixFrame frame5 = rope.RopeEnd.GetGlobalFrame();
				frame5.origin = _planks[num10].GetGlobalFrame().origin + vec16;
				rope.RopeEnd.SetGlobalFrame(in frame5);
				rope.ParentEntity.SetVisibilityExcludeParents(visible: true);
			}
			if (_currentFramePlankPhysicsIndexCount > 0)
			{
				TransformCurrentFramePlankPhysicsVerticesToPhysicsEntityLocal(AttachmentSource.PlankBridgePhysicsEntity.GlobalPosition);
			}
		}

		private void ArrangePlanks()
		{
			if (_currentFramePlankPhysicsIndexCount > 0)
			{
				AttachmentSource.PlankBridgePhysicsEntity.ReplacePhysicsBodyWithQuadPhysicsBody(_currentFramePlankPhysicsVerticesPinnedPointer, _currentFramePlankPhysicsVertexCount, _woodPhysicsMaterialCached, BodyFlags.TwoSided | BodyFlags.Moveable | BodyFlags.HasSteps | BodyFlags.AgentOnly, _currentFramePlankPhysicsIndicesPinnedPointer, _currentFramePlankPhysicsIndexCount);
				BodyFlags physicsDescBodyFlag = AttachmentSource.PlankBridgePhysicsEntity.PhysicsDescBodyFlag;
				if (physicsDescBodyFlag.HasAnyFlag(BodyFlags.Disabled))
				{
					AttachmentSource.PlankBridgePhysicsEntity.SetBodyFlags((BodyFlags)((uint)physicsDescBodyFlag & 0xFFFFFFFEu));
				}
			}
			else
			{
				BodyFlags physicsDescBodyFlag2 = AttachmentSource.PlankBridgePhysicsEntity.PhysicsDescBodyFlag;
				if (!physicsDescBodyFlag2.HasAnyFlag(BodyFlags.Disabled))
				{
					AttachmentSource.PlankBridgePhysicsEntity.SetBodyFlags(physicsDescBodyFlag2 | BodyFlags.Disabled);
				}
			}
		}

		public Vec3 GetLaunchProjectileCurrentGlobalPosition(float time)
		{
			return _launchFlightData.SourceGlobalPosition + _launchFlightData.GlobalVelocity * time + 0.5f * MBGlobals.GravitationalAcceleration * time * time;
		}

		private static (Vec3, float) CalculateInitialVelocityAndTime(Vec3 initialPosition, Vec3 destination, float verticalLaunchAngleDegree)
		{
			float num = destination.x - initialPosition.x;
			float num2 = destination.y - initialPosition.y;
			float deltaZ = destination.z - initialPosition.z;
			float num3 = verticalLaunchAngleDegree * System.MathF.PI / 180f;
			float num4 = (float)Math.Sqrt(num * num + num2 * num2);
			float num5 = CalculateInitialVelocityMagnitude(num4, deltaZ, num3);
			float num6 = (float)Math.Atan2(num2, num);
			float x = num5 * (float)Math.Cos(num3) * (float)Math.Cos(num6);
			float y = num5 * (float)Math.Cos(num3) * (float)Math.Sin(num6);
			float z = num5 * (float)Math.Sin(num3);
			Vec3 item = new Vec3(x, y, z);
			float item2 = num4 / (num5 * (float)Math.Cos(num3));
			return (item, item2);
		}

		private static float CalculateLaunchAngleDegree(Vec3 initialPosition, Vec3 targetPosition, float launchSpeed)
		{
			Vec3 vec = targetPosition - initialPosition;
			float num = launchSpeed * launchSpeed;
			float length = vec.AsVec2.Length;
			float z = vec.z;
			float num2 = num * num;
			float num3 = 9.806f * (9.806f * length * length + 2f * z * num);
			if (num2 >= num3)
			{
				float num4 = TaleWorlds.Library.MathF.Sqrt(num2 - num3);
				return TaleWorlds.Library.MathF.Atan((num - num4) / (9.806f * length)) * 180f / System.MathF.PI;
			}
			return float.MinValue;
		}

		private static float CalculateInitialVelocityMagnitude(float distanceXY, float deltaZ, float thetaZ)
		{
			float num = (float)Math.Tan(thetaZ);
			float num2 = (float)Math.Cos(thetaZ);
			float num3 = 9.806f * distanceXY * distanceXY;
			float num4 = 2f * num2 * num2 * (distanceXY * num - deltaZ);
			return (float)Math.Sqrt(num3 / num4);
		}

		private static float CalculateDifferenceVectorAngle(in Vec3 initialPosition, in Vec3 destination)
		{
			Vec3 vec = destination - initialPosition;
			float length = vec.AsVec2.Length;
			return (float)Math.Atan2(vec.z, length) * (180f / System.MathF.PI);
		}
	}

	public const float AgentOarLeaveAttachmentLengthSquared = 64f;

	public const float AgentOarLeaveRelativeSpeedThreshold = 4f;

	public const float MaximumRopeLength = 40f;

	public const float MinimumBridgeDistanceToKeep = 2.2f;

	public const float MaximumRopesPullingDuration = 30f;

	public const float BridgeConnectionRelativeSpeedThreshold = 4f;

	public const float RopesPullingFrequency = 1f;

	public const float RopesPullingRelaxSpeed = 0.05f;

	public const float RopesPullingRelaxThresholdRatio = 0.75f;

	public const float RopesPullingPullSpeed = 0.65f;

	public const float RopesPullingPullAcceleration = 0.25f;

	public const float RopesPullingWaveAmp = 0.6f;

	public const float StiffnessRampTime = 5f;

	public const float MaxDistanceError = 10f;

	public const float MaxDistanceErrorBridge = 5f;

	public const float MaxXYError = 2.75f;

	public const float MaxAlignmentError = 0.95f;

	public const float MaxAccumulatedAlignmentError = 20f;

	public const float InteractionDistance = 40f;

	public const float FatigueRate = 4f;

	public const float RopeBeta = 0.1f;

	public const float StretchLimit = 2f;

	public const float Damping = 0.1f;

	public const float RopeMaxAccelerationLowTension = 1.2f;

	public const float RopeMaxAccelerationHighTension = 5f;

	public const float BridgeDirectionDampingRatio = 0.3f;

	public const float BridgeDirectionTargetPeriod = 2f;

	public const float BridgeDirectionMaxAcceleration = 5f;

	public const float AlignmentDampingRatio = 0.8f;

	private const bool CanConnectToFriends = false;

	public const float AlignmentTargetPeriod = 1.75f;

	public const float AlignmentMaxAcceleration = 5f;

	public const float XYDampingRatio = 0.5f;

	public const float XYTargetPeriod = 0.75f;

	public const float XYMaxAcceleration = 15f;

	public const float MaxInclineAngle = System.MathF.PI * 13f / 36f;

	private const string HookItemID = "hook";

	private const string HookGrabSoundEvent = "event:/mission/movement/vessel/hook_grab";

	public const string ConnectionClipPointTag = "connection_point";

	public const string RampBarrierTag = "connection_barrier";

	public const string RampCapsulePhysicsTag = "step_capsule";

	public const string RampSourceVisualTag = "bridge_source";

	public const string RampTargetVisualTag = "bridge_target";

	public const string PileHangedStaticVisualTag = "pile_hanged_static";

	public const string PileFloorStaticVisualTag = "pile_floor_static";

	[EditableScriptComponentVariable(true, "")]
	public int RelatedShipNavmeshOffset;

	private MissionShip _preferredTargetShip;

	private bool _checkedInitialConnections;

	private readonly RopePileBaked[] _cutRopeSegmentsCached = new RopePileBaked[2];

	private readonly GameEntity[] _cutRopeSegmentEntities = new GameEntity[2];

	private WeakGameEntity _staticRopeVisual;

	private ItemObject _hookItem;

	private GameEntity _focusObject;

	private MatrixFrame _initialHookLocalFrame;

	private MBList<GameEntity> _rampPhysicsList;

	private bool _physicsEntitiesVisibility;

	private Vec3[] _defaultPhysicsQuad;

	private int[] _defaultIndicesCached;

	private NavalShipsLogic _navalShipsLogicCached;

	public float BridgeConnectionLengthSquared { get; private set; }

	public MissionShip OwnerShip { get; private set; }

	public ShipAttachment CurrentAttachment { get; private set; }

	public RopePileBaked RopeVisual { get; private set; }

	public GameEntity RopeVisualEntity { get; private set; }

	public ShipAttachmentPointMachine LinkedAttachmentPointMachine { get; private set; }

	public GameEntity ConnectionClipPlaneEntity { get; private set; }

	public GameEntity RampBarrier { get; private set; }

	public float RopeMinLength { get; private set; }

	internal MBReadOnlyList<GameEntity> RampPhysicsList => _rampPhysicsList;

	internal GameEntity RampVisualEntity { get; private set; }

	public GameEntity BarrierSource { get; private set; }

	public GameEntity BarrierTarget { get; private set; }

	public GameEntity VFoldSource { get; private set; }

	public GameEntity Hook { get; private set; }

	public GameEntity VFoldTarget { get; private set; }

	public GameEntity PlankBridgePhysicsEntity { get; private set; }

	public PlankBridgeSteppedAgentManager SteppedAgentManager { get; private set; }

	public bool IsShipAttachmentJointPhysicsEnabled { get; private set; }

	public NavalShipsLogic NavalShipsLogicCached
	{
		get
		{
			if (_navalShipsLogicCached == null)
			{
				_navalShipsLogicCached = Mission.Current.GetMissionBehavior<NavalShipsLogic>();
			}
			return _navalShipsLogicCached;
		}
	}

	public void ActivateCutRopeSegment(int slot, GameEntity anchorTracker, Vec3 anchorLocalOffset, Vec3 anchorGlobalPosition, Vec3 freeEndGlobalPosition, Vec3 freeEndImpulse, RopePileBaked seedFromRope, float seedStartFraction, float seedEndFraction, GameEntity hullClipPlaneEntity, float tensionRatio = 0f)
	{
		if (slot < 0 || slot >= _cutRopeSegmentsCached.Length)
		{
			return;
		}
		RopePileBaked ropePileBaked = _cutRopeSegmentsCached[slot];
		GameEntity gameEntity = _cutRopeSegmentEntities[slot];
		if (ropePileBaked == null || !(gameEntity != null))
		{
			return;
		}
		float num = 5f * ((tensionRatio > 1f) ? (tensionRatio * tensionRatio) : 0f);
		Vec3 vec = -((freeEndGlobalPosition - anchorGlobalPosition).NormalizedCopy() * num + freeEndImpulse).NormalizedCopy() * num;
		gameEntity.SetGlobalFrame(new MatrixFrame(Mat3.Identity, in anchorGlobalPosition));
		gameEntity.SetVisibilityExcludeParents(visible: true);
		gameEntity.SetFactorColor(uint.MaxValue);
		ropePileBaked.SetEndPinning(sourcePinned: true, targetPinned: false);
		ropePileBaked.SetMeshUnfurlOverride(1f);
		ropePileBaked.ClearAnchorTrackers();
		ropePileBaked.SetSourceAnchorTracker(anchorTracker, anchorLocalOffset);
		ropePileBaked.SetDampingRamp(1.5f, 2f);
		ropePileBaked.ClearDriftAcceleration();
		ropePileBaked.SetClipPlaneEntities(hullClipPlaneEntity, null);
		ropePileBaked.SnapRopeState(RopePileBaked.RopeSlackPolicy.Natural, TaleWorlds.Library.MathF.Max(anchorGlobalPosition.Distance(freeEndGlobalPosition) * 1.3f, 0.5f));
		if (seedFromRope != null)
		{
			ropePileBaked.SeedChainFromSlice(seedFromRope, seedStartFraction, seedEndFraction);
		}
		else
		{
			ropePileBaked.SeedChain(in anchorGlobalPosition, in freeEndGlobalPosition);
		}
		if (freeEndImpulse.LengthSquared > 1E-06f)
		{
			for (int i = 0; i < 3; i++)
			{
				float hitT = 1f - (float)i * 0.15f;
				float num2 = TaleWorlds.Library.MathF.Pow(0.55f, i);
				ropePileBaked.ApplyWobble(vec * num2, vec.Length * num2, 1f, hitT);
			}
		}
		ropePileBaked.StartLifetime(5f, 2f);
	}

	private void InitializeCutRopeSegments()
	{
		if (RopeVisualEntity == null)
		{
			return;
		}
		Scene scene = Mission.Current?.Scene;
		if (scene == null)
		{
			return;
		}
		for (int i = 0; i < _cutRopeSegmentsCached.Length; i++)
		{
			GameEntity gameEntity = TaleWorlds.Engine.GameEntity.CopyFrom(scene, RopeVisualEntity);
			if (!(gameEntity == null))
			{
				gameEntity.SetGlobalFrame(new MatrixFrame(Mat3.Identity, in Vec3.Zero));
				gameEntity.SetVisibilityExcludeParents(visible: false);
				_cutRopeSegmentsCached[i] = gameEntity.GetFirstScriptOfType<RopePileBaked>();
				_cutRopeSegmentEntities[i] = gameEntity;
			}
		}
	}

	public void SetShipAttachmentJointPhysicsEnabled(bool enabled)
	{
		IsShipAttachmentJointPhysicsEnabled = enabled;
	}

	public bool IsShipAttachmentMachineBridged()
	{
		if (CurrentAttachment != null)
		{
			if (CurrentAttachment.State != ShipAttachment.ShipAttachmentState.BridgeConnected)
			{
				return CurrentAttachment.State == ShipAttachment.ShipAttachmentState.BridgeThrown;
			}
			return true;
		}
		return false;
	}

	public bool IsShipAttachmentMachineBridgeWithEnemy()
	{
		if (CurrentAttachment != null)
		{
			Team team = CurrentAttachment?.AttachmentSource?.OwnerShip?.Team;
			Team team2 = CurrentAttachment?.AttachmentTarget?.OwnerShip?.Team;
			if (team != null && team2 != null && team.IsEnemyOf(team2))
			{
				return CurrentAttachment.State == ShipAttachment.ShipAttachmentState.BridgeConnected;
			}
			return false;
		}
		return false;
	}

	public bool IsShipAttachmentMachineConnectedToEnemy()
	{
		if (CurrentAttachment != null && (CurrentAttachment.State == ShipAttachment.ShipAttachmentState.RopesPulling || CurrentAttachment.State == ShipAttachment.ShipAttachmentState.BridgeThrown || CurrentAttachment.State == ShipAttachment.ShipAttachmentState.BridgeConnected) && CurrentAttachment.AttachmentSource.OwnerShip.Team != null && CurrentAttachment.AttachmentTarget.OwnerShip.Team != null)
		{
			return CurrentAttachment.AttachmentSource.OwnerShip.Team.IsEnemyOf(CurrentAttachment.AttachmentTarget.OwnerShip.Team);
		}
		return false;
	}

	public static bool DoesShipAttachmentMachineSatisfyOarsmenGetUpCondition(ShipAttachment currentAttachment)
	{
		if (currentAttachment != null && (currentAttachment.State == ShipAttachment.ShipAttachmentState.RopesPulling || currentAttachment.State == ShipAttachment.ShipAttachmentState.BridgeThrown) && currentAttachment.AttachmentSource.OwnerShip.Team != null && currentAttachment.AttachmentTarget.OwnerShip.Team != null && currentAttachment.AttachmentSource.OwnerShip.Team.IsEnemyOf(currentAttachment.AttachmentTarget.OwnerShip.Team))
		{
			MissionShip ownerShip = currentAttachment.AttachmentSource.OwnerShip;
			MissionShip ownerShip2 = currentAttachment.AttachmentTarget.OwnerShip;
			Vec3 angularVelocity = ownerShip.Physics.AngularVelocity;
			Vec3 angularVelocity2 = ownerShip2.Physics.AngularVelocity;
			Vec3 origin = ownerShip.GameEntity.GetBodyWorldTransform().origin;
			Vec3 origin2 = ownerShip2.GameEntity.GetBodyWorldTransform().origin;
			Vec3 origin3 = currentAttachment.AttachmentSource.GameEntity.GetGlobalFrame().origin;
			Vec3 origin4 = currentAttachment.AttachmentTarget.GameEntity.GetGlobalFrame().origin;
			Vec3 va = (origin3 - origin).NormalizedCopy();
			Vec3 va2 = (origin4 - origin2).NormalizedCopy();
			Vec3 vec = ownerShip.Physics.LinearVelocity + Vec3.CrossProduct(va, angularVelocity);
			Vec3 vec2 = ownerShip2.Physics.LinearVelocity + Vec3.CrossProduct(va2, angularVelocity2) - vec;
			float lengthSquared = (origin4 - origin3).LengthSquared;
			if (vec2.LengthSquared <= 16f && lengthSquared <= 64f)
			{
				foreach (ShipOarMachine leftSideShipOarMachine in ownerShip.LeftSideShipOarMachines)
				{
					if (MBRandom.RandomFloat > 0.6f)
					{
						leftSideShipOarMachine.PilotAgent?.YellAfterDelay(0.25f + MBRandom.RandomFloat);
					}
				}
				foreach (ShipOarMachine rightSideShipOarMachine in ownerShip.RightSideShipOarMachines)
				{
					if (MBRandom.RandomFloat > 0.6f)
					{
						rightSideShipOarMachine.PilotAgent?.YellAfterDelay(0.25f + MBRandom.RandomFloat);
					}
				}
				return true;
			}
		}
		return false;
	}

	public override bool ShouldAutoLeaveDetachmentWhenDisabled(BattleSideEnum sideEnum)
	{
		return false;
	}

	public override void Disable()
	{
		if (CurrentAttachment != null)
		{
			CurrentAttachment.Destroy();
			CurrentAttachment = null;
		}
		RemoveConnectionPhysicsEntities();
		base.Disable();
	}

	public void SetConnectionPhysicsEntitiesVisibility(bool visible)
	{
		if (_physicsEntitiesVisibility != visible)
		{
			BarrierSource.SetVisibilityExcludeParents(visible);
			BarrierTarget.SetVisibilityExcludeParents(visible);
			VFoldSource.SetVisibilityExcludeParents(visible);
			VFoldTarget.SetVisibilityExcludeParents(visible);
			PlankBridgePhysicsEntity.SetVisibilityExcludeParents(visible);
			BarrierSource.SetPhysicsStateOnlyVariable(visible, setChildren: false);
			BarrierTarget.SetPhysicsStateOnlyVariable(visible, setChildren: false);
			VFoldSource.SetPhysicsStateOnlyVariable(visible, setChildren: false);
			VFoldTarget.SetPhysicsStateOnlyVariable(visible, setChildren: false);
			PlankBridgePhysicsEntity.SetPhysicsStateOnlyVariable(visible, setChildren: false);
			_physicsEntitiesVisibility = visible;
		}
	}

	private void RemoveConnectionPhysicsEntities()
	{
		BarrierSource.Remove(78);
		BarrierTarget.Remove(78);
		VFoldSource.Remove(78);
		VFoldTarget.Remove(78);
		PlankBridgePhysicsEntity.Remove(35);
	}

	private void InitializeConnectionPhysicsEntities()
	{
		PhysicsMaterial.GetFromName("wood_nonstick");
		_defaultPhysicsQuad = new Vec3[4];
		_defaultPhysicsQuad[0] = new Vec3(-0.5f, -0.5f);
		_defaultPhysicsQuad[1] = new Vec3(0.5f, -0.5f);
		_defaultPhysicsQuad[2] = new Vec3(0.5f, 0.5f);
		_defaultPhysicsQuad[3] = new Vec3(-0.5f, 0.5f);
		_defaultIndicesCached = new int[6];
		_defaultIndicesCached[0] = 0;
		_defaultIndicesCached[1] = 1;
		_defaultIndicesCached[2] = 2;
		_defaultIndicesCached[3] = 0;
		_defaultIndicesCached[4] = 2;
		_defaultIndicesCached[5] = 3;
		BarrierSource = TaleWorlds.Engine.GameEntity.CreateEmpty(Mission.Current.Scene);
		BarrierSource.Name = "Bridge_barrier_source";
		BarrierTarget = TaleWorlds.Engine.GameEntity.CreateEmpty(Mission.Current.Scene);
		BarrierTarget.Name = "Bridge_barrier_target";
		VFoldSource = TaleWorlds.Engine.GameEntity.CreateEmpty(Mission.Current.Scene);
		VFoldSource.Name = "Bridge_vFold_source";
		VFoldTarget = TaleWorlds.Engine.GameEntity.CreateEmpty(Mission.Current.Scene);
		VFoldTarget.Name = "Bridge_vFold_target";
		PlankBridgePhysicsEntity = TaleWorlds.Engine.GameEntity.CreateEmpty(Mission.Current.Scene, isModifiableFromEditor: false);
		PlankBridgePhysicsEntity.SetGlobalFrame(MatrixFrame.Identity);
		PlankBridgePhysicsEntity.Name = "Plank Bridge Physics";
		PlankBridgePhysicsEntity.CreateAndAddScriptComponent("PlankBridgeSteppedAgentManager", callScriptCallbacks: true);
		SteppedAgentManager = PlankBridgePhysicsEntity.GetFirstScriptOfType<PlankBridgeSteppedAgentManager>();
		SetConnectionPhysicsEntitiesVisibility(visible: false);
	}

	public bool CheckAttachmentMachineFlags(bool editMode)
	{
		IEnumerable<WeakGameEntity> children = base.GameEntity.GetChildren();
		string[] source = new string[3] { "hook", "pilot", "pile" };
		foreach (WeakGameEntity item in children)
		{
			if (!item.EntityFlags.HasAnyFlag(EntityFlags.DontSaveToScene) && source.Contains(item.Name) && !item.EntityFlags.HasAnyFlag(EntityFlags.DoesNotAffectParentsLocalBb))
			{
				string msg = $"Root Entity: {base.GameEntity.Root.Name} {base.GameEntity.Name}'s child {item.Name} must have Does not Affect Parent's Local Bounding Box flag.";
				if (editMode)
				{
					MBEditor.AddEntityWarning(item, msg);
				}
				return false;
			}
		}
		return true;
	}

	protected override void OnRemoved(int removeReason)
	{
		_navalShipsLogicCached = null;
		for (int i = 0; i < _cutRopeSegmentsCached.Length; i++)
		{
			_cutRopeSegmentEntities[i]?.Remove(0);
			_cutRopeSegmentEntities[i] = null;
			_cutRopeSegmentsCached[i] = null;
		}
	}

	protected override void OnInit()
	{
		base.OnInit();
		IsShipAttachmentJointPhysicsEnabled = true;
		BridgeConnectionLengthSquared = 20.25f;
		InitializeConnectionPhysicsEntities();
		WeakGameEntity parent = base.GameEntity.Parent;
		while (OwnerShip == null && parent.IsValid)
		{
			OwnerShip = parent.GetFirstScriptOfType<MissionShip>();
			parent = parent.Parent;
		}
		if (base.GameEntity.Parent.GetScriptCountOfTypeRecursive<ShipAttachmentPointMachine>() == 1)
		{
			LinkedAttachmentPointMachine = base.GameEntity.Parent.GetFirstScriptOfTypeRecursive<ShipAttachmentPointMachine>();
		}
		int childCount = base.GameEntity.ChildCount;
		for (int i = 0; i < childCount; i++)
		{
			WeakGameEntity child = base.GameEntity.GetChild(i);
			if (child.Name == "hook")
			{
				Hook = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(child);
				_initialHookLocalFrame = base.GameEntity.GetGlobalFrame().TransformToLocalNonOrthogonal(child.GetGlobalFrame());
			}
			else if (child.Name == "focus_object")
			{
				_focusObject = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(child);
			}
		}
		_hookItem = Game.Current.ObjectManager.GetObject<ItemObject>("hook");
		SetScriptComponentToTick(GetTickRequirement());
		RopeVisual = base.GameEntity.GetFirstScriptInFamilyDescending<RopePileBaked>();
		RopeVisualEntity = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(RopeVisual.GameEntity);
		InitializeCutRopeSegments();
		_staticRopeVisual = base.GameEntity.GetFirstChildEntityWithTagRecursive("pile_hanged_static");
		if (_staticRopeVisual == null)
		{
			_staticRopeVisual = base.GameEntity.GetFirstChildEntityWithTagRecursive("pile_floor_static");
		}
		EnemyRangeToStopUsing = 5f;
		RampBarrier = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(LinkedAttachmentPointMachine.GameEntity.GetFirstChildEntityWithTag("connection_barrier"));
		ConnectionClipPlaneEntity = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(LinkedAttachmentPointMachine.GameEntity.GetFirstChildEntityWithTagRecursive("connection_point"));
		List<WeakGameEntity> list = new List<WeakGameEntity>();
		LinkedAttachmentPointMachine.GameEntity.GetChildrenWithTagRecursive(list, "step_capsule");
		_rampPhysicsList = new MBList<GameEntity>();
		foreach (WeakGameEntity item in list)
		{
			if (item.GetVisibilityExcludeParents())
			{
				_rampPhysicsList.Add(TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(item));
			}
		}
		RampVisualEntity = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(LinkedAttachmentPointMachine.GameEntity.GetFirstChildEntityWithTagRecursive("bridge_source"));
		RampVisualEntity.SetVisibilityExcludeParents(visible: false);
		IsDisabledForAttackerAIDueToEnemyInRange = new QueryData<bool>(() => OwnerShip?.ShipOrder != null && OwnerShip.ShipOrder.IsEnemyOnShip, 1f);
		IsDisabledForDefenderAIDueToEnemyInRange = new QueryData<bool>(() => OwnerShip?.ShipOrder != null && OwnerShip.ShipOrder.IsEnemyOnShip, 1f);
	}

	public void CheckCurrentAttachmentAndInitializeRopeBoundingBox()
	{
		if (CurrentAttachment == null)
		{
			RopeVisual.SetRopeBoundingBoxToInitialState();
		}
	}

	protected override float GetDetachmentWeightAux(BattleSideEnum side)
	{
		return float.MinValue;
	}

	public override TickRequirement GetTickRequirement()
	{
		return TickRequirement.Tick | TickRequirement.TickParallel | TickRequirement.FixedTick | base.GetTickRequirement();
	}

	public void SetPreferredTargetShip(MissionShip newTarget)
	{
		_preferredTargetShip = newTarget;
	}

	public MissionShip GetPreferredTargetShip()
	{
		return _preferredTargetShip;
	}

	public bool CalculateCanConnectToTargetShip(MissionShip targetShip)
	{
		if ((targetShip != null && targetShip.Physics.NavalSinkingState == NavalDLC.Missions.NavalPhysics.NavalPhysics.SinkingState.Sinking) || (targetShip != null && targetShip.Physics.NavalSinkingState == NavalDLC.Missions.NavalPhysics.NavalPhysics.SinkingState.Sunk))
		{
			return false;
		}
		foreach (ShipAttachmentPointMachine attachmentPointMachine in targetShip.AttachmentPointMachines)
		{
			if (attachmentPointMachine.CurrentAttachment == null && ComputePotentialAttachmentValue(this, attachmentPointMachine, checkInteractionDistance: false, checkConnectionBlock: false, allowWiderAngleBetweenConnections: true) > 0f)
			{
				return true;
			}
		}
		return false;
	}

	public bool IsOnCorrectSide(MissionShip targetShip)
	{
		return OwnerShip.GameEntity.GetFrame().TransformToLocal(targetShip.GameEntity.GlobalPosition).AsVec2.DotProduct(OwnerShip.GameEntity.GetFrame().TransformToLocal(base.GameEntity.GlobalPosition).AsVec2) >= 0f;
	}

	public void SetCanConnectToFriends(bool canConnectToFriends)
	{
		_checkedInitialConnections = false;
	}

	public bool HasCheckedInitialConnections()
	{
		return _checkedInitialConnections;
	}

	public void ConnectWithAttachmentPointMachine(ShipAttachmentPointMachine attachmentPointMachine, bool forceBridge = false, bool unbreakableBridge = false, bool connectionInitializedByPlayer = false)
	{
		Vec3 vec;
		if (base.PilotAgent != null)
		{
			MatrixFrame frame = base.PilotAgent.Frame;
			MatrixFrame boneEntitialFrame = base.PilotAgent.GetBoneEntitialFrame(base.PilotAgent.Monster.MainHandItemBoneIndex, useBoneMapping: false);
			vec = frame.TransformToParent(in boneEntitialFrame.origin);
		}
		else
		{
			vec = base.GameEntity.GlobalPosition;
		}
		ShipAttachment shipAttachment = (CurrentAttachment = new ShipAttachment(this, attachmentPointMachine, vec + (vec - RopeVisual.GameEntity.GlobalPosition).NormalizedCopy() * 0.5f, base.PilotAgent?.LookDirection ?? Vec3.Zero, bridgeConnectionInteractionDistanceCheck: false, connectionInitializedByPlayer));
		attachmentPointMachine?.AssignConnection(shipAttachment);
		if (forceBridge)
		{
			Vec3 globalPosition = RopeVisual.GameEntity.GlobalPosition;
			Vec3 attachmentTargetGlobalPosition = attachmentPointMachine.GameEntity.GetGlobalFrame().TransformToParent(attachmentPointMachine.HookAttachLocalPosition);
			shipAttachment.InitializeShipAttachmentJoint(globalPosition, attachmentTargetGlobalPosition, unbreakableBridge);
			shipAttachment.CheckAndConnectBridge(forceBridge: true);
		}
	}

	public ShipAttachmentPointMachine GetBestEnemyAttachment(bool checkAttachmentAlreadyExists = false, bool checkInteractionDistance = true)
	{
		ShipAttachmentPointMachine shipAttachmentPointMachine = null;
		float num = 0f;
		Vec3 origin = OwnerShip.GlobalFrame.origin;
		if (_preferredTargetShip != null)
		{
			if (_preferredTargetShip.GlobalFrame.origin.DistanceSquared(origin) <= 14400f)
			{
				if (!_preferredTargetShip.IsConnectionBlocked())
				{
					foreach (ShipAttachmentPointMachine attachmentPointMachine in _preferredTargetShip.AttachmentPointMachines)
					{
						if (attachmentPointMachine.CurrentAttachment == null && attachmentPointMachine.LinkedAttachmentMachine?.CurrentAttachment == null)
						{
							float num2 = ComputePotentialAttachmentValue(this, attachmentPointMachine, checkInteractionDistance, checkConnectionBlock: false, allowWiderAngleBetweenConnections: true);
							if (num2 > num && (!checkAttachmentAlreadyExists || attachmentPointMachine.CurrentAttachment == null))
							{
								num = num2;
								shipAttachmentPointMachine = attachmentPointMachine;
							}
						}
					}
				}
				if (shipAttachmentPointMachine == null)
				{
					foreach (MissionShip allShip in OwnerShip.ShipsLogic.AllShips)
					{
						if (allShip == OwnerShip || allShip == _preferredTargetShip || !MissionShip.AreShipsConnected(allShip, _preferredTargetShip) || !(allShip.GlobalFrame.origin.DistanceSquared(origin) <= 14400f) || allShip.IsConnectionBlocked() || OwnerShip.SearchShipConnection(allShip, isDirect: false, findEnemy: false, enforceActive: false, acceptNotBridgedConnections: false))
						{
							continue;
						}
						foreach (ShipAttachmentPointMachine attachmentPointMachine2 in allShip.AttachmentPointMachines)
						{
							if (attachmentPointMachine2.CurrentAttachment == null && attachmentPointMachine2.LinkedAttachmentMachine?.CurrentAttachment == null)
							{
								float num3 = ComputePotentialAttachmentValue(this, attachmentPointMachine2, checkInteractionDistance: true, checkConnectionBlock: false, allowWiderAngleBetweenConnections: true);
								if (num3 > num && (!checkAttachmentAlreadyExists || attachmentPointMachine2.CurrentAttachment == null))
								{
									num = num3;
									shipAttachmentPointMachine = attachmentPointMachine2;
								}
							}
						}
					}
				}
			}
		}
		else
		{
			foreach (MissionShip allShip2 in OwnerShip.ShipsLogic.AllShips)
			{
				if (allShip2 == OwnerShip || allShip2.GlobalFrame.origin.DistanceSquared(origin) > 14400f || allShip2.IsConnectionBlocked() || OwnerShip.SearchShipConnection(allShip2, isDirect: false, findEnemy: false, enforceActive: false, acceptNotBridgedConnections: false))
				{
					continue;
				}
				foreach (ShipAttachmentPointMachine attachmentPointMachine3 in allShip2.AttachmentPointMachines)
				{
					if (attachmentPointMachine3.CurrentAttachment == null && attachmentPointMachine3.LinkedAttachmentMachine?.CurrentAttachment == null && ((base.PilotAgent != null && !base.PilotAgent.IsAIControlled) || allShip2 == _preferredTargetShip || (_preferredTargetShip == null && allShip2.BattleSide != OwnerShip.BattleSide) || (_preferredTargetShip != null && _preferredTargetShip.ShipIslandCombinedID == allShip2.ShipIslandCombinedID)))
					{
						float num4 = ComputePotentialAttachmentValue(this, attachmentPointMachine3, checkInteractionDistance: true, checkConnectionBlock: false, allowWiderAngleBetweenConnections: true);
						if (num4 > num && (!checkAttachmentAlreadyExists || attachmentPointMachine3.CurrentAttachment == null))
						{
							num = num4;
							shipAttachmentPointMachine = attachmentPointMachine3;
						}
					}
				}
			}
		}
		return shipAttachmentPointMachine;
	}

	public override void OnDeploymentFinished()
	{
		base.PilotStandingPoint.AddComponent(new ResetAnimationOnStopUsageComponent(ActionIndexCache.act_none, alwaysResetWithAction: false));
		base.PilotStandingPoint.AddComponent(new RemoveExtraWeaponOnStopUsageComponent());
		base.PilotStandingPoint.LockUserFrames = false;
		base.PilotStandingPoint.LockUserPositions = true;
	}

	protected override void OnTickParallel(float dt)
	{
		if (Mission.Current != null)
		{
			if (CurrentAttachment != null && CurrentAttachment.State == ShipAttachment.ShipAttachmentState.BridgeConnected)
			{
				CurrentAttachment.OnParallelTick(dt);
			}
			if (CurrentAttachment == null && base.PilotAgent == null)
			{
				RopeVisual.UpdateRopeMeshVisualAccordingToTargetPointLinearWithoutBoundingBoxUpdate(RopeVisual.GameEntity.GlobalPosition, RopeVisual.GameEntity.GlobalPosition);
			}
		}
	}

	protected override void OnTick(float dt)
	{
		if (Mission.Current == null || OwnerShip == null)
		{
			return;
		}
		if (!Mission.Current.MissionEnded)
		{
			bool flag = LinkedAttachmentPointMachine?.CurrentAttachment != null || (base.PilotAgent == null && CurrentAttachment != null && (CurrentAttachment.State != ShipAttachment.ShipAttachmentState.BridgeConnected || OwnerShip.IsDisconnectionBlocked()));
			base.PilotStandingPoint.SetIsDeactivatedSynched(flag);
			base.PilotStandingPoint.AutoSheathWeapons = CurrentAttachment != null && CurrentAttachment.State == ShipAttachment.ShipAttachmentState.BridgeConnected;
			if (_focusObject.GetVisibilityExcludeParents() == flag)
			{
				_focusObject.SetVisibilityExcludeParents(!flag);
			}
		}
		if (base.PilotAgent != null)
		{
			if (CurrentAttachment == null)
			{
				if (base.PilotAgent.SetActionChannel(1, in ActionIndexCache.act_usage_hook_ready, ignorePriority: false, (AnimFlags)0uL))
				{
					RopeVisual.GameEntity.SetVisibilityExcludeParents(visible: true);
					Hook.SetVisibilityExcludeParents(visible: false);
					_staticRopeVisual.SetVisibilityExcludeParents(visible: false);
					MatrixFrame frame = base.PilotAgent.Frame;
					MatrixFrame boneEntitialFrame = base.PilotAgent.GetBoneEntitialFrame(base.PilotAgent.Monster.MainHandItemBoneIndex, useBoneMapping: false);
					Vec3 targetGlobalPosition = frame.TransformToParent(in boneEntitialFrame.origin);
					Vec3 sourceGlobalPosition = (_staticRopeVisual.IsValid ? _staticRopeVisual.GetGlobalFrame().origin : base.GameEntity.GetGlobalFrame().origin);
					RopeVisual.SetRopeState(RopePileBaked.RopeSlackPolicy.Natural, TaleWorlds.Library.MathF.Max(sourceGlobalPosition.Distance(targetGlobalPosition) * 1.5f, 2f));
					RopeVisual.ClearMeshUnfurlOverride();
					RopeVisual.UpdateRopeMeshVisualAccordingToTargetPointLinearNoHookOffset(in sourceGlobalPosition, in targetGlobalPosition);
					RopeVisual.SetApplyClipPlanes(applyClipPlanes: false);
					if (base.PilotAgent.WieldedWeapon.Item != _hookItem)
					{
						SoundManager.StartOneShotEvent("event:/mission/movement/vessel/hook_grab", base.PilotAgent.Position);
						MissionWeapon weapon = new MissionWeapon(_hookItem, null, null);
						base.PilotAgent.EquipWeaponToExtraSlotAndWield(ref weapon);
					}
					if (base.PilotAgent.IsAIControlled)
					{
						if (GetBestEnemyAttachment() != null && !base.PilotAgent.SetActionChannel(1, in ActionIndexCache.act_usage_hook_release, ignorePriority: false, (AnimFlags)0uL))
						{
							base.PilotAgent.StopUsingGameObject();
						}
					}
					else if (base.PilotAgent.Mission.InputManager.IsGameKeyReleased(9) && Vec3.DotProduct(base.GameEntity.GetGlobalFrame().rotation.f, base.PilotAgent.LookRotation.f) >= 0f && !base.PilotAgent.SetActionChannel(1, in ActionIndexCache.act_usage_hook_release, ignorePriority: false, (AnimFlags)0uL))
					{
						base.PilotAgent.StopUsingGameObject();
					}
					_checkedInitialConnections = true;
				}
				else if (base.PilotAgent.GetCurrentAction(1) == ActionIndexCache.act_usage_hook_release)
				{
					if (base.PilotAgent.IsAIControlled)
					{
						ShipAttachmentPointMachine bestEnemyAttachment = GetBestEnemyAttachment();
						if (bestEnemyAttachment == null)
						{
							if (!base.PilotAgent.SetActionChannel(1, in ActionIndexCache.act_none, ignorePriority: false, AnimFlags.amf_priority_cancel) || !base.PilotAgent.SetActionChannel(1, in ActionIndexCache.act_usage_hook_ready, ignorePriority: false, (AnimFlags)0uL))
							{
								base.PilotAgent.StopUsingGameObject();
							}
						}
						else if (base.PilotAgent.GetCurrentActionProgress(1) > MBAnimation.GetAnimationParameter1("usage_hook_release"))
						{
							ConnectWithAttachmentPointMachine(bestEnemyAttachment);
							base.PilotAgent.RemoveEquippedWeapon(EquipmentIndex.ExtraWeaponSlot);
							Hook.SetVisibilityExcludeParents(visible: true);
						}
					}
					else if (base.PilotAgent.GetCurrentActionProgress(1) > MBAnimation.GetAnimationParameter1("usage_hook_release"))
					{
						ConnectWithAttachmentPointMachine(null, forceBridge: false, unbreakableBridge: false, connectionInitializedByPlayer: true);
						base.PilotAgent.RemoveEquippedWeapon(EquipmentIndex.ExtraWeaponSlot);
						Hook.SetVisibilityExcludeParents(visible: true);
					}
				}
				else if (!base.PilotAgent.IsInBeingStruckAction)
				{
					base.PilotAgent.StopUsingGameObject();
				}
			}
			else if (base.PilotAgent.GetCurrentAction(1) == ActionIndexCache.act_usage_hook_release)
			{
				if (base.PilotAgent.GetCurrentActionProgress(1) > 0.99f)
				{
					base.PilotAgent.StopUsingGameObject();
				}
			}
			else if (CurrentAttachment.State == ShipAttachment.ShipAttachmentState.BridgeConnected)
			{
				if (base.PilotAgent.SetActionChannel(1, in ActionIndexCache.act_ship_connection_break, ignorePriority: false, (AnimFlags)0uL))
				{
					if (base.PilotAgent == Agent.Main && base.PilotAgent.GetCurrentActionProgress(1) < 0.1f)
					{
						MissionShip ownerShip = OwnerShip;
						if (ownerShip != null && ownerShip.Team?.IsPlayerTeam == true)
						{
							OwnerShip?.ShipOrder?.SetCutLoose(enable: true);
						}
					}
					if (base.PilotAgent.GetCurrentActionProgress(1) > 0.99f)
					{
						DisconnectAttachment();
						base.PilotAgent.StopUsingGameObject();
					}
				}
				else
				{
					base.PilotAgent.StopUsingGameObject();
				}
			}
		}
		else if (CurrentAttachment == null)
		{
			RopeVisual.GameEntity.SetVisibilityExcludeParents(visible: false);
			Hook.SetVisibilityExcludeParents(visible: true);
			_staticRopeVisual.SetVisibilityExcludeParents(visible: true);
		}
		if (CurrentAttachment != null)
		{
			bool num = CurrentAttachment.State == ShipAttachment.ShipAttachmentState.BridgeConnected;
			CurrentAttachment.OnTick(dt);
			if (!num && CurrentAttachment.State == ShipAttachment.ShipAttachmentState.BridgeConnected)
			{
				CurrentAttachment.AttachmentSource.OwnerShip.OnShipConnected(CurrentAttachment);
				CurrentAttachment.AttachmentTarget.OwnerShip.OnShipConnected(CurrentAttachment);
			}
			if (CurrentAttachment.State == ShipAttachment.ShipAttachmentState.BrokenAndWaitingForRemoval)
			{
				CurrentAttachment.Destroy();
				CheckCurrentAttachmentAndInitializeRopeBoundingBox();
			}
		}
		if (Hook.GetVisibilityExcludeParents())
		{
			if (CurrentAttachment != null && (CurrentAttachment.State == ShipAttachment.ShipAttachmentState.RopeThrown || CurrentAttachment.State == ShipAttachment.ShipAttachmentState.RopesPulling || CurrentAttachment.State == ShipAttachment.ShipAttachmentState.BridgeConnected || CurrentAttachment.State == ShipAttachment.ShipAttachmentState.RopeFailedAndReloading))
			{
				RopeVisual.SetApplyClipPlanes(applyClipPlanes: true);
				Hook.SetGlobalFrame(CurrentAttachment.HookGlobalFrame);
			}
			else
			{
				Hook.SetGlobalFrame(base.GameEntity.GetGlobalFrame().TransformToParent(in _initialHookLocalFrame));
			}
		}
		if (base.GameEntity.BodyFlag.HasAllFlags(BodyFlags.Sinking) && base.GameEntity.GetGlobalFrame().origin.z + SinkingReferenceOffset < base.Scene.GetWaterLevelAtPosition(base.GameEntity.GetFrame().origin.AsVec2, useWaterRenderer: true, checkWaterBodyEntities: false))
		{
			Disable();
		}
	}

	public void DisconnectAttachment()
	{
		CurrentAttachment.SetAttachmentState(ShipAttachment.ShipAttachmentState.BrokenAndWaitingForRemoval);
		CurrentAttachment.AttachmentSource.OwnerShip.OnShipDisconnected(CurrentAttachment);
		CurrentAttachment.AttachmentTarget.OwnerShip.OnShipDisconnected(CurrentAttachment);
	}

	private static bool CheckIntersectionsBetweenConnectionsAux(Vec2 attachmentMachineSourcePosition, Vec2 attachmentMachineTargetPosition, ShipAttachment testAttachment)
	{
		return MBMath.CheckLineSegmentToLineSegmentIntersection(attachmentMachineSourcePosition, attachmentMachineTargetPosition, testAttachment.AttachmentSource.GameEntity.GlobalPosition.AsVec2, testAttachment.AttachmentTarget.GameEntity.GlobalPosition.AsVec2);
	}

	private static bool CheckIntersectionsBetweenConnectionsWithState(ShipAttachmentMachine attachmentMachine, ShipAttachmentPointMachine attachmentPointMachine, ShipAttachment.ShipAttachmentState state)
	{
		Vec2 asVec = attachmentMachine.GameEntity.GlobalPosition.AsVec2;
		Vec2 asVec2 = attachmentPointMachine.GameEntity.GlobalPosition.AsVec2;
		MissionShip ownerShip = attachmentMachine.OwnerShip;
		MissionShip ownerShip2 = attachmentPointMachine.OwnerShip;
		foreach (ShipAttachmentMachine attachmentMachine2 in ownerShip.AttachmentMachines)
		{
			if (attachmentMachine2 != attachmentMachine && attachmentMachine2.CurrentAttachment != null && attachmentMachine2.CurrentAttachment.State == state && attachmentMachine2.CurrentAttachment.AttachmentTarget != null && CheckIntersectionsBetweenConnectionsAux(asVec, asVec2, attachmentMachine2.CurrentAttachment))
			{
				return true;
			}
		}
		foreach (ShipAttachmentPointMachine attachmentPointMachine2 in ownerShip.AttachmentPointMachines)
		{
			if (attachmentPointMachine2.CurrentAttachment != null && attachmentPointMachine2.CurrentAttachment.State == state && CheckIntersectionsBetweenConnectionsAux(asVec, asVec2, attachmentPointMachine2.CurrentAttachment))
			{
				return true;
			}
		}
		foreach (ShipAttachmentMachine attachmentMachine3 in ownerShip2.AttachmentMachines)
		{
			if (attachmentMachine3.CurrentAttachment != null && attachmentMachine3.CurrentAttachment.State == state && attachmentMachine3.CurrentAttachment.AttachmentTarget != null && CheckIntersectionsBetweenConnectionsAux(asVec, asVec2, attachmentMachine3.CurrentAttachment))
			{
				return true;
			}
		}
		foreach (ShipAttachmentPointMachine attachmentPointMachine3 in ownerShip2.AttachmentPointMachines)
		{
			if (attachmentPointMachine3 != attachmentPointMachine && attachmentPointMachine3.CurrentAttachment != null && attachmentPointMachine3.CurrentAttachment.State == state && CheckIntersectionsBetweenConnectionsAux(asVec, asVec2, attachmentPointMachine3.CurrentAttachment))
			{
				return true;
			}
		}
		return false;
	}

	private static bool CheckAttachmentsFacingEachOther(ShipAttachmentMachine attachmentMachine, ShipAttachmentPointMachine attachmentPointMachine)
	{
		MatrixFrame globalFrame = attachmentMachine.GameEntity.GetGlobalFrame();
		MatrixFrame globalFrame2 = attachmentPointMachine.GameEntity.GetGlobalFrame();
		Vec2 asVec = globalFrame.rotation.f.AsVec2;
		Vec2 asVec2 = globalFrame2.rotation.f.AsVec2;
		Vec2 va = globalFrame2.origin.AsVec2 - globalFrame.origin.AsVec2;
		if (Vec2.DotProduct(asVec, asVec2) < 0f)
		{
			return Vec2.DotProduct(va, asVec2) < 0f;
		}
		return false;
	}

	private static bool CheckIntersectionsBetweenConnections(ShipAttachmentMachine attachmentMachine, ShipAttachmentPointMachine attachmentPointMachine)
	{
		Vec2 asVec = attachmentMachine.GameEntity.GlobalPosition.AsVec2;
		Vec2 asVec2 = attachmentPointMachine.GameEntity.GlobalPosition.AsVec2;
		MissionShip ownerShip = attachmentMachine.OwnerShip;
		MissionShip ownerShip2 = attachmentPointMachine.OwnerShip;
		foreach (ShipAttachmentMachine attachmentMachine2 in ownerShip.AttachmentMachines)
		{
			if (attachmentMachine2 != attachmentMachine && attachmentMachine2.CurrentAttachment != null && attachmentMachine2.CurrentAttachment.AttachmentTarget != null && attachmentMachine2.CurrentAttachment.AttachmentTarget != attachmentPointMachine && CheckIntersectionsBetweenConnectionsAux(asVec, asVec2, attachmentMachine2.CurrentAttachment))
			{
				return true;
			}
		}
		foreach (ShipAttachmentPointMachine attachmentPointMachine2 in ownerShip.AttachmentPointMachines)
		{
			if (attachmentPointMachine2.CurrentAttachment != null && CheckIntersectionsBetweenConnectionsAux(asVec, asVec2, attachmentPointMachine2.CurrentAttachment))
			{
				return true;
			}
		}
		foreach (ShipAttachmentMachine attachmentMachine3 in ownerShip2.AttachmentMachines)
		{
			if (attachmentMachine3.CurrentAttachment != null && attachmentMachine3.CurrentAttachment.AttachmentTarget != null && CheckIntersectionsBetweenConnectionsAux(asVec, asVec2, attachmentMachine3.CurrentAttachment))
			{
				return true;
			}
		}
		foreach (ShipAttachmentPointMachine attachmentPointMachine3 in ownerShip2.AttachmentPointMachines)
		{
			if (attachmentPointMachine3 != attachmentPointMachine && attachmentPointMachine3.CurrentAttachment != null && attachmentPointMachine3.CurrentAttachment.AttachmentSource != attachmentMachine && CheckIntersectionsBetweenConnectionsAux(asVec, asVec2, attachmentPointMachine3.CurrentAttachment))
			{
				return true;
			}
		}
		return false;
	}

	public static bool IsShipNearAttachmentMachines(MissionShip ship, MatrixFrame shipFrame, Vec2 sourceGlobalPos, Vec2 targetGlobalPos)
	{
		float radius = ship.Physics.PhysicsBoundingBoxWithoutChildren.radius;
		Vec3 v = ship.Physics.PhysicsBoundingBoxWithoutChildren.center;
		Vec2 asVec = shipFrame.TransformToParent(in v).AsVec2;
		Vec2 v2 = (sourceGlobalPos + targetGlobalPos) * 0.5f;
		float num = v2.Distance(sourceGlobalPos) + radius;
		return asVec.DistanceSquared(v2) <= num * num;
	}

	public static bool IsShipBetweenAttachments(ShipAttachmentMachine attachmentMachineSource, ShipAttachmentPointMachine attachmentMachineTarget)
	{
		MissionShip offendingShip;
		return TryFindShipBetweenAttachments(attachmentMachineSource, attachmentMachineTarget, out offendingShip);
	}

	public static bool TryFindShipBetweenAttachments(ShipAttachmentMachine attachmentMachineSource, ShipAttachmentPointMachine attachmentMachineTarget, out MissionShip offendingShip)
	{
		offendingShip = null;
		Vec2 asVec = attachmentMachineSource.GameEntity.GlobalPosition.AsVec2;
		Vec2 asVec2 = attachmentMachineTarget.GameEntity.GlobalPosition.AsVec2;
		foreach (MissionShip allShip in attachmentMachineSource.NavalShipsLogicCached.AllShips)
		{
			if (allShip != attachmentMachineSource.OwnerShip && allShip != attachmentMachineTarget.OwnerShip)
			{
				MatrixFrame shipFrame = allShip.GameEntity.GetGlobalFrame();
				Vec2[] physicsBoundingBoxPointsOfShip = allShip.CalculateBoundingXYGlobalPlaneFromLocal(in shipFrame);
				if (EarlyCrossCheckForShipIntersectingAttachmentMachine(physicsBoundingBoxPointsOfShip, asVec, asVec2) && IsShipNearAttachmentMachines(allShip, shipFrame, asVec, asVec2) && IsLineSegmentIntersectingShipBoundingXYPlane(physicsBoundingBoxPointsOfShip, asVec, asVec2))
				{
					offendingShip = allShip;
					return true;
				}
			}
		}
		return false;
	}

	private static bool EarlyCrossCheckForShipIntersectingAttachmentMachine(Vec2[] physicsBoundingBoxPointsOfShip, Vec2 attachmentSourceGlobalPosition, Vec2 attachmentTargetGlobalPosition)
	{
		Vec2 vb = attachmentSourceGlobalPosition - attachmentTargetGlobalPosition;
		float num = Vec2.CCW(physicsBoundingBoxPointsOfShip[0] - attachmentTargetGlobalPosition, vb);
		for (int i = 1; i < physicsBoundingBoxPointsOfShip.Length; i++)
		{
			float num2 = Vec2.CCW(physicsBoundingBoxPointsOfShip[i] - attachmentTargetGlobalPosition, vb);
			if (num * num2 <= 0f)
			{
				return true;
			}
		}
		return false;
	}

	public static bool IsLineSegmentIntersectingShipBoundingXYPlane(Vec2[] physicsBoundingBoxPointsOfShip, Vec2 attachment0Position, Vec2 attachment1Position)
	{
		if (MBMath.CheckLineSegmentToLineSegmentIntersection(physicsBoundingBoxPointsOfShip[0], physicsBoundingBoxPointsOfShip[1], attachment0Position, attachment1Position))
		{
			return true;
		}
		if (MBMath.CheckLineSegmentToLineSegmentIntersection(physicsBoundingBoxPointsOfShip[1], physicsBoundingBoxPointsOfShip[2], attachment0Position, attachment1Position))
		{
			return true;
		}
		if (MBMath.CheckLineSegmentToLineSegmentIntersection(physicsBoundingBoxPointsOfShip[2], physicsBoundingBoxPointsOfShip[3], attachment0Position, attachment1Position))
		{
			return true;
		}
		if (MBMath.CheckLineSegmentToLineSegmentIntersection(physicsBoundingBoxPointsOfShip[3], physicsBoundingBoxPointsOfShip[0], attachment0Position, attachment1Position))
		{
			return true;
		}
		if (MBMath.CheckPointInsidePolygon(in physicsBoundingBoxPointsOfShip[0], in physicsBoundingBoxPointsOfShip[1], in physicsBoundingBoxPointsOfShip[2], in physicsBoundingBoxPointsOfShip[3], in attachment0Position) || MBMath.CheckPointInsidePolygon(in physicsBoundingBoxPointsOfShip[0], in physicsBoundingBoxPointsOfShip[1], in physicsBoundingBoxPointsOfShip[2], in physicsBoundingBoxPointsOfShip[3], in attachment1Position))
		{
			return true;
		}
		return false;
	}

	public static float ComputePotentialAttachmentValue(ShipAttachmentMachine attachmentSource, ShipAttachmentPointMachine attachmentTarget, bool checkInteractionDistance, bool checkConnectionBlock, bool allowWiderAngleBetweenConnections)
	{
		if (!checkConnectionBlock || !attachmentSource.OwnerShip.IsConnectionBlocked())
		{
			MatrixFrame globalFrame = attachmentSource.GameEntity.GetGlobalFrame();
			Vec3 v = globalFrame.rotation.f.NormalizedCopy();
			MatrixFrame globalFrame2 = attachmentTarget.GameEntity.GetGlobalFrame();
			Vec3 vec = globalFrame2.origin - globalFrame.origin;
			float num = vec.Normalize();
			if (!checkInteractionDistance || num <= 40f)
			{
				float num2 = Vec3.DotProduct(vec, v);
				if (num2 > (allowWiderAngleBetweenConnections ? 0.1736f : 0.4226f))
				{
					if (IsShipBetweenAttachments(attachmentSource, attachmentTarget))
					{
						return -1f;
					}
					if (CheckIntersectionsBetweenConnections(attachmentSource, attachmentTarget))
					{
						return -1f;
					}
					if (!CheckAttachmentsFacingEachOther(attachmentSource, attachmentTarget))
					{
						return -1f;
					}
					Vec3 v2 = globalFrame2.rotation.f.NormalizedCopy();
					float num3 = Vec3.DotProduct(-vec, v2);
					if (num3 > 0.1736f)
					{
						return 10000f * num2 * num3 / num;
					}
				}
			}
		}
		return -1f;
	}

	protected override void OnFixedTick(float fixedDt)
	{
		if (CurrentAttachment != null)
		{
			CurrentAttachment.OnFixedTick(fixedDt);
		}
	}

	public override TextObject GetActionTextForStandingPoint(UsableMissionObject usableGameObject)
	{
		TextObject textObject = (((CurrentAttachment == null || CurrentAttachment.State != ShipAttachment.ShipAttachmentState.BridgeConnected) && (LinkedAttachmentPointMachine?.CurrentAttachment == null || LinkedAttachmentPointMachine.CurrentAttachment.State != ShipAttachment.ShipAttachmentState.BridgeConnected)) ? new TextObject("{=fEQAPJ2e}{KEY} Use") : new TextObject("{=PUbT3s7W}{KEY} Cut Loose"));
		textObject.SetTextVariable("KEY", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("CombatHotKeyCategory", 13)));
		return textObject;
	}

	public override TextObject GetDescriptionText(WeakGameEntity gameEntity)
	{
		if ((CurrentAttachment != null && CurrentAttachment.State == ShipAttachment.ShipAttachmentState.BridgeConnected) || (LinkedAttachmentPointMachine?.CurrentAttachment != null && LinkedAttachmentPointMachine.CurrentAttachment.State == ShipAttachment.ShipAttachmentState.BridgeConnected))
		{
			return new TextObject("{=kCMGJl1W}Bridge");
		}
		return new TextObject("{=7zCPG8TR}Hook");
	}

	public override UsableMachineAIBase CreateAIBehaviorObject()
	{
		return new ShipAttachmentMachineAI(this);
	}
}
