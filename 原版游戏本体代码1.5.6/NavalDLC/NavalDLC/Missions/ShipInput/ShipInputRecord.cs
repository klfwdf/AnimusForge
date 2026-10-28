namespace NavalDLC.Missions.ShipInput;

public struct ShipInputRecord
{
	public RowerLateralInput RowerLateral { get; private set; }

	public RowerLongitudinalInput RowerLongitudinal { get; private set; }

	public RowerLongitudinalInput RowerLongitudinalDoubleTap { get; private set; }

	public float RudderLateral { get; private set; }

	public SailInput Sail { get; private set; }

	public ShipInputRecord(RowerLateralInput rowerLateral, RowerLongitudinalInput rowerLongitudinal, RowerLongitudinalInput rowerLongitudinalDoubleTap, float rudderLateral, SailInput sail)
	{
		RowerLateral = rowerLateral;
		RowerLongitudinal = rowerLongitudinal;
		RowerLongitudinalDoubleTap = rowerLongitudinalDoubleTap;
		RudderLateral = rudderLateral;
		Sail = sail;
	}

	public void SetRowerLateral(RowerLateralInput value)
	{
		RowerLateral = value;
	}

	public void SetRowerLongitudinal(RowerLongitudinalInput value)
	{
		RowerLongitudinal = value;
	}

	public void SetRowerLongitudinalDoupleTap(RowerLongitudinalInput value)
	{
		RowerLongitudinalDoubleTap = value;
	}

	public void SetRudderLateral(float value)
	{
		RudderLateral = value;
	}

	public void SetSail(SailInput value)
	{
		Sail = value;
	}

	public static ShipInputRecord None()
	{
		return new ShipInputRecord(RowerLateralInput.None, RowerLongitudinalInput.None, RowerLongitudinalInput.None, 0f, SailInput.Raised);
	}

	public static ShipInputRecord Stop()
	{
		return new ShipInputRecord(RowerLateralInput.Stop, RowerLongitudinalInput.Stop, RowerLongitudinalInput.Stop, 0f, SailInput.Raised);
	}
}
