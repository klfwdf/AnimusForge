using System;

namespace NavalDLC.Missions.ShipInput;

public static class ShipInputExtensions
{
	public static RowerLateralInput OppositeDirection(this RowerLateralInput input)
	{
		return input switch
		{
			RowerLateralInput.Left => RowerLateralInput.Right, 
			RowerLateralInput.Right => RowerLateralInput.Left, 
			_ => input, 
		};
	}

	public static float RudderLateralInputOppositeDirection(float input)
	{
		return 0f - input;
	}

	public static float ToRudderInput(this RowerLateralInput input)
	{
		return input switch
		{
			RowerLateralInput.Left => -1f, 
			RowerLateralInput.Right => 1f, 
			RowerLateralInput.Stop => 0f, 
			_ => 0f, 
		};
	}

	public static SailInput Lower(this SailInput input, bool hasHybridSails = false)
	{
		int num = Math.Min((int)(input + 1), 2);
		if (num == 1 && !hasHybridSails)
		{
			num = Math.Min(num + 1, 2);
		}
		return (SailInput)num;
	}

	public static SailInput Raise(this SailInput input, bool hasHybridSails = false)
	{
		int num = Math.Max((int)(input - 1), 0);
		if (num == 1 && !hasHybridSails)
		{
			num = Math.Max(num - 1, 0);
		}
		return (SailInput)num;
	}

	public static SailInput Min(this SailInput input, bool hasHybridSails = false)
	{
		while (true)
		{
			SailInput sailInput = input.Lower(hasHybridSails);
			if (sailInput == input)
			{
				break;
			}
			input = sailInput;
		}
		return input;
	}

	public static SailInput Max(this SailInput input, bool hasHybridSails = false)
	{
		while (true)
		{
			SailInput sailInput = input.Raise(hasHybridSails);
			if (sailInput == input)
			{
				break;
			}
			input = sailInput;
		}
		return input;
	}

	public static bool IsMin(this SailInput input)
	{
		return input == input.Lower();
	}

	public static bool IsMax(this SailInput input)
	{
		return input == input.Raise();
	}

	public static string ToShortText(this RowerLongitudinalInput input)
	{
		if (input != RowerLongitudinalInput.None)
		{
			return input.ToString()[0].ToString() ?? "";
		}
		return "-";
	}

	public static string ToShortText(this RowerLateralInput input)
	{
		if (input != RowerLateralInput.None)
		{
			return input.ToString()[0].ToString() ?? "";
		}
		return "-";
	}

	public static string RudderLateralInputToShortText(float input)
	{
		return input.ToString();
	}

	public static string ToShortText(this SailInput input)
	{
		return input.ToString()[0].ToString() ?? "";
	}
}
