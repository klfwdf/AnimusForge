namespace TaleWorlds.Library;

public class InformationMessage
{
	public string Information { get; set; }

	public string Detail { get; set; }

	public Color Color { get; set; }

	public string SoundEventPath { get; set; }

	public string Category { get; set; }

	public InformationMessage(string information)
	{
		Information = information;
		Color = Color.White;
	}

	public InformationMessage(string information, Color color)
	{
		Information = information;
		Color = color;
	}

	public InformationMessage(string information, Color color, string category)
	{
		Information = information;
		Color = color;
		Category = category;
	}

	public InformationMessage(string information, string soundEventPath)
	{
		Information = information;
		SoundEventPath = soundEventPath;
		Color = Color.White;
	}

	public InformationMessage()
	{
		Information = "";
	}
}
