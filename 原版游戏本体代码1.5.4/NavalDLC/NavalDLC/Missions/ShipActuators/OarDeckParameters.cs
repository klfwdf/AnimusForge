using System;

namespace NavalDLC.Missions.ShipActuators;

public class OarDeckParameters
{
	public const float DefaultVerticalBaseAngle = MathF.PI / 12f;

	public const float DefaultLateralBaseAngle = 0f;

	public const float DefaultVerticalRotationAngle = 0.17453292f;

	public const float DefaultLateralRotationAngle = 0.30019665f;

	public const float DefaultOarLength = 4f;

	public const float DefaultRetractionRate = 0.4f;

	public const float DefaultRetractionOffset = 1f;

	public float VerticalBaseAngle { get; private set; }

	public float LateralBaseAngle { get; private set; }

	public float VerticalRotationAngle { get; private set; }

	public float LateralRotationAngle { get; private set; }

	public float OarLength { get; private set; }

	public float RetractionRate { get; private set; }

	public float RetractionOffset { get; private set; }

	public OarDeckParameters(float verticalBaseAngle = MathF.PI / 12f, float lateralBaseAngle = 0f, float verticalRotationAngle = 0.17453292f, float lateralRotationAngle = 0.30019665f, float oarLength = 4f, float retractionRate = 0.4f, float retractionOffset = 1f)
	{
		SetParameters(verticalBaseAngle, lateralBaseAngle, verticalRotationAngle, lateralRotationAngle, oarLength, retractionRate, retractionOffset);
	}

	public OarDeckParameters()
	{
		SetParameters();
	}

	public void SetParameters(float verticalBaseAngle = MathF.PI / 12f, float lateralBaseAngle = 0f, float verticalRotationAngle = 0.17453292f, float lateralRotationAngle = 0.30019665f, float oarLength = 4f, float retractionRate = 0.4f, float retractionOffset = 1f)
	{
		VerticalBaseAngle = verticalBaseAngle;
		LateralBaseAngle = lateralBaseAngle;
		VerticalRotationAngle = verticalRotationAngle;
		LateralRotationAngle = lateralRotationAngle;
		OarLength = oarLength;
		RetractionRate = retractionRate;
		RetractionOffset = retractionOffset;
	}
}
