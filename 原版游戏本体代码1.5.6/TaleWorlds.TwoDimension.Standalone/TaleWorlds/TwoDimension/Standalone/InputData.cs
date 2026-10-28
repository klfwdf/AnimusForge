namespace TaleWorlds.TwoDimension.Standalone;

public class InputData
{
	public bool[] KeyData { get; set; }

	public bool LeftMouse { get; set; }

	public bool RightMouse { get; set; }

	public int CursorX { get; set; }

	public int CursorY { get; set; }

	public bool MouseMove { get; set; }

	public float MouseScrollDelta { get; set; }

	public InputData()
	{
		KeyData = new bool[256];
		CursorX = 0;
		CursorY = 0;
		LeftMouse = false;
		RightMouse = false;
		MouseMove = false;
		MouseScrollDelta = 0f;
		for (int i = 0; i < 256; i++)
		{
			KeyData[i] = false;
		}
	}

	public void Reset()
	{
		MouseScrollDelta = 0f;
	}

	public void FillFrom(InputData inputData)
	{
		CursorX = inputData.CursorX;
		CursorY = inputData.CursorY;
		LeftMouse = inputData.LeftMouse;
		RightMouse = inputData.RightMouse;
		MouseMove = inputData.MouseMove;
		MouseScrollDelta = inputData.MouseScrollDelta;
		for (int i = 0; i < 256; i++)
		{
			KeyData[i] = inputData.KeyData[i];
		}
	}
}
