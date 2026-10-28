using System.Collections.Generic;

namespace TaleWorlds.Engine;

public class Highlights
{
	public enum Significance
	{
		None = 0,
		ExtremelyBad = 1,
		VeryBad = 2,
		Bad = 4,
		Neutral = 0x10,
		Good = 0x100,
		VeryGood = 0x200,
		ExtremelyGoods = 0x400,
		Max = 0x800
	}

	public enum Type
	{
		None = 0,
		Milestone = 1,
		Achievement = 2,
		Incident = 4,
		StateChange = 8,
		Unannounced = 0x10,
		Max = 0x20
	}

	public static void Initialize()
	{
		EngineApplicationInterface.IHighlights.Initialize();
	}

	public static void OpenGroup(string id)
	{
		EngineApplicationInterface.IHighlights.OpenGroup(id);
	}

	public static void CloseGroup(string id, bool destroy = false)
	{
		EngineApplicationInterface.IHighlights.CloseGroup(id, destroy);
	}

	public static void SaveScreenshot(string highlightId, string groupId)
	{
		EngineApplicationInterface.IHighlights.SaveScreenshot(highlightId, groupId);
	}

	public static void SaveVideo(string highlightId, string groupId, int startDelta, int endDelta)
	{
		EngineApplicationInterface.IHighlights.SaveVideo(highlightId, groupId, startDelta, endDelta);
	}

	public static void OpenSummary(List<string> groups)
	{
		string groups2 = string.Join("::", groups);
		EngineApplicationInterface.IHighlights.OpenSummary(groups2);
	}

	public static void AddHighlight(string id, string name)
	{
		EngineApplicationInterface.IHighlights.AddHighlight(id, name);
	}

	public static void RemoveHighlight(string id)
	{
		EngineApplicationInterface.IHighlights.RemoveHighlight(id);
	}
}
