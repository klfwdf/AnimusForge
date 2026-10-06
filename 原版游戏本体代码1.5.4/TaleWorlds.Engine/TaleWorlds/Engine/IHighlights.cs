using TaleWorlds.Library;

namespace TaleWorlds.Engine;

[ApplicationInterfaceBase]
internal interface IHighlights
{
	[EngineMethod("initialize", false, null, false)]
	void Initialize();

	[EngineMethod("open_group", false, null, false)]
	void OpenGroup(string id);

	[EngineMethod("close_group", false, null, false)]
	void CloseGroup(string id, bool destroy = false);

	[EngineMethod("save_screenshot", false, null, false)]
	void SaveScreenshot(string highlightId, string groupId);

	[EngineMethod("save_video", false, null, false)]
	void SaveVideo(string highlightId, string groupId, int startDelta, int endDelta);

	[EngineMethod("open_summary", false, null, false)]
	void OpenSummary(string groups);

	[EngineMethod("add_highlight", false, null, false)]
	void AddHighlight(string id, string name);

	[EngineMethod("remove_highlight", false, null, false)]
	void RemoveHighlight(string id);
}
