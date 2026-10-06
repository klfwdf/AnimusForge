using System;
using NavalDLC.Missions.NavalPhysics;
using NavalDLC.Missions.Objects;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.Missions.ShipActuators;

public class OarSidePhaseController
{
	public enum OarSide
	{
		Left,
		Right
	}

	public const float RaisedPhase = System.MathF.PI;

	public const float LoweredPhase = 0f;

	private OarDeckParameters _averageDeckParameters;

	private float _lastPhase;

	private readonly MissionShip _ownerShip;

	public float Phase { get; private set; }

	public float CycleArcSizeMult { get; private set; }

	public float LastSlowDownFactor { get; private set; }

	public float VisualPhase { get; private set; }

	public float PhaseRate { get; private set; }

	public float NeededRevolutionRate { get; private set; }

	public float VisualVerticalBaseAngleOffsetFromShipRoll { get; private set; }

	public OarSide Side { get; }

	public OarSidePhaseController(MissionShip ownerShip, OarSide side)
	{
		Phase = System.MathF.PI;
		_lastPhase = System.MathF.PI;
		VisualPhase = System.MathF.PI;
		PhaseRate = 0f;
		NeededRevolutionRate = 0f;
		_ownerShip = ownerShip;
		Side = side;
	}

	public void SetAverageOarDeckParameters(OarDeckParameters averageDeckParameters)
	{
		_averageDeckParameters = averageDeckParameters;
	}

	public (float, float) ComputeForceAndSlowDownFactor(float rowerNeededPhaseRate, float shipForwardSpeed, float syncPhase, float targetPhaseRate, float oarsmenForceMultiplier, float oarFrictionMultiplier, float maxTipSpeed)
	{
		float num = 0f;
		float num2 = 1f;
		if (rowerNeededPhaseRate != 0f)
		{
			Vec3 vec = MissionOar.ComputeBladeContactVelocityAux(_averageDeckParameters, syncPhase, targetPhaseRate);
			if (vec.y <= 0f)
			{
				if (vec.y < 0f - maxTipSpeed)
				{
					num2 = TaleWorlds.Library.MathF.Abs(maxTipSpeed / vec.y);
					vec.y = 0f - maxTipSpeed;
				}
				float num3 = vec.y * (float)TaleWorlds.Library.MathF.Sign(rowerNeededPhaseRate);
				float num4 = num3 + shipForwardSpeed;
				if (num4 * rowerNeededPhaseRate <= 0f)
				{
					float num5 = TaleWorlds.Library.MathF.Abs(TaleWorlds.Library.MathF.Cos(syncPhase));
					float num6 = 1000f * oarsmenForceMultiplier;
					float num7 = 1.2f * oarFrictionMultiplier * 0.5f * NavalDLC.Missions.NavalPhysics.NavalPhysics.GetWaterDensity() * (0.45f * num5);
					num = num7 * num4 * num4 * (float)TaleWorlds.Library.MathF.Sign(rowerNeededPhaseRate);
					if (TaleWorlds.Library.MathF.Abs(num) > num6)
					{
						float num8 = TaleWorlds.Library.MathF.Sqrt(num6 / num7);
						float num9 = (float)TaleWorlds.Library.MathF.Sign(num4) * num8 - shipForwardSpeed;
						if (num9 * num3 < 0f || TaleWorlds.Library.MathF.Abs(num9) < 0.8f)
						{
							num9 = (float)TaleWorlds.Library.MathF.Sign(num9) * 0.8f;
						}
						num2 *= TaleWorlds.Library.MathF.Abs(num9 / num3);
						if (num2 > 1f)
						{
							num2 = 1f;
						}
						num = (float)TaleWorlds.Library.MathF.Sign(num) * num6;
					}
				}
			}
		}
		LastSlowDownFactor = num2;
		return (num, num2);
	}

	public void SetPhaseData(float phase, float phaseRate, float cycleArcSizeMult, float neededRevolutionRate)
	{
		PhaseRate = phaseRate;
		_lastPhase = Phase;
		Phase = phase;
		CycleArcSizeMult = cycleArcSizeMult;
		NeededRevolutionRate = neededRevolutionRate;
	}

	public void OnParallelTick(float dt)
	{
		Mission.Current.Scene.GetInterpolationFactorForBodyWorldTransformSmoothing(out var interpolationFactor, out var _);
		float num = TaleWorlds.Library.MathF.Abs(_lastPhase - Phase);
		float num2 = TaleWorlds.Library.MathF.Abs(_lastPhase - System.MathF.PI * 2f - Phase);
		float num3 = TaleWorlds.Library.MathF.Abs(_lastPhase + System.MathF.PI * 2f - Phase);
		if (num < num2)
		{
			if (num3 < num)
			{
				VisualPhase = TaleWorlds.Library.MathF.Lerp(_lastPhase + System.MathF.PI * 2f, Phase, interpolationFactor);
			}
			else
			{
				VisualPhase = TaleWorlds.Library.MathF.Lerp(_lastPhase, Phase, interpolationFactor);
			}
		}
		else if (num3 < num)
		{
			VisualPhase = TaleWorlds.Library.MathF.Lerp(_lastPhase + System.MathF.PI * 2f, Phase, interpolationFactor);
		}
		else
		{
			VisualPhase = TaleWorlds.Library.MathF.Lerp(_lastPhase - System.MathF.PI * 2f, Phase, interpolationFactor);
		}
		VisualPhase = MBMath.WrapAngleSafe(VisualPhase);
		float num4 = 0f;
		if (PhaseRate != 0f)
		{
			num4 = 0f - _ownerShip.GameEntity.GetLocalFrame().rotation.GetEulerAngles().y;
			if (Side == OarSide.Left)
			{
				num4 = 0f - num4;
			}
			if (num4 < 0f)
			{
				num4 = 0f;
			}
		}
		VisualVerticalBaseAngleOffsetFromShipRoll = MBMath.Lerp(VisualVerticalBaseAngleOffsetFromShipRoll, num4, dt * 3f);
	}

	public void Stop()
	{
		PhaseRate = 0f;
		NeededRevolutionRate = 0f;
	}

	public bool IsInRowingMotion()
	{
		return PhaseRate != 0f || (!Phase.ApproximatelyEqualsTo(System.MathF.PI) && !MBMath.WrapAngleSafe(Phase).ApproximatelyEqualsTo(System.MathF.PI)) || (!VisualPhase.ApproximatelyEqualsTo(System.MathF.PI) && !MBMath.WrapAngleSafe(VisualPhase).ApproximatelyEqualsTo(System.MathF.PI));
	}

	public float GetLastSubmergedHeightFactorForActuators()
	{
		return TaleWorlds.Library.MathF.Clamp(_ownerShip.Physics.LastSubmergedHeightFactorForActuators, 0f, 1f);
	}
}
