using TaleWorlds.Library;

namespace TaleWorlds.Engine;

[ApplicationInterfaceBase]
internal interface IImgui
{
	[EngineMethod("begin_main_thread_scope", false, null, false)]
	void BeginMainThreadScope();

	[EngineMethod("end_main_thread_scope", false, null, false)]
	void EndMainThreadScope();

	[EngineMethod("push_style_color", false, null, false)]
	void PushStyleColor(int style, ref Vec3 color);

	[EngineMethod("pop_style_color", false, null, false)]
	void PopStyleColor();

	[EngineMethod("new_frame", false, null, false)]
	void NewFrame();

	[EngineMethod("render", false, null, false)]
	void Render();

	[EngineMethod("begin", false, null, false)]
	void Begin(string text);

	[EngineMethod("begin_with_close_button", false, null, false)]
	void BeginWithCloseButton(string text, ref bool is_open);

	[EngineMethod("end", false, null, false)]
	void End();

	[EngineMethod("text", false, null, false)]
	void Text(string text);

	[EngineMethod("checkbox", false, null, false)]
	bool Checkbox(string text, ref bool is_checked);

	[EngineMethod("tree_node", false, null, false)]
	bool TreeNode(string name);

	[EngineMethod("tree_pop", false, null, false)]
	void TreePop();

	[EngineMethod("separator", false, null, false)]
	void Separator();

	[EngineMethod("button", false, null, false)]
	bool Button(string text);

	[EngineMethod("plot_lines", false, null, false)]
	void PlotLines(string name, float[] values, int valuesCount, int valuesOffset, string overlayText, float minScale, float maxScale, float graphWidth, float graphHeight, int stride);

	[EngineMethod("progress_bar", false, null, false)]
	void ProgressBar(float value);

	[EngineMethod("new_line", false, null, false)]
	void NewLine();

	[EngineMethod("same_line", false, null, false)]
	void SameLine(float posX, float spacingWidth);

	[EngineMethod("combo", false, null, false)]
	bool Combo(string label, ref int selectedIndex, string items);

	[EngineMethod("combo_custom_seperator", false, null, false)]
	bool ComboCustomSeperator(string label, ref int selectedIndex, string items, string seperator);

	[EngineMethod("input_int", false, null, false)]
	bool InputInt(string label, ref int value);

	[EngineMethod("slider_float", false, null, false)]
	bool SliderFloat(string label, ref float value, float min, float max);

	[EngineMethod("columns", false, null, false)]
	void Columns(int count = 1, string id = "", bool border = true);

	[EngineMethod("next_column", false, null, false)]
	void NextColumn();

	[EngineMethod("radio_button", false, null, false)]
	bool RadioButton(string label, bool active);

	[EngineMethod("collapsing_header", false, null, false)]
	bool CollapsingHeader(string label);

	[EngineMethod("is_item_hovered", false, null, false)]
	bool IsItemHovered();

	[EngineMethod("set_tool_tip", false, null, false)]
	void SetTooltip(string label);

	[EngineMethod("small_button", false, null, false)]
	bool SmallButton(string label);

	[EngineMethod("input_float", false, null, false)]
	bool InputFloat(string label, ref float val, float step, float stepFast, int decimalPrecision = -1);

	[EngineMethod("input_float2", false, null, false)]
	bool InputFloat2(string label, ref float val0, ref float val1, int decimalPrecision = -1);

	[EngineMethod("input_float3", false, null, false)]
	bool InputFloat3(string label, ref float val0, ref float val1, ref float val2, int decimalPrecision = -1);

	[EngineMethod("input_float4", false, null, false)]
	bool InputFloat4(string label, ref float val0, ref float val1, ref float val2, ref float val3, int decimalPrecision = -1);

	[EngineMethod("input_text", false, null, false)]
	string InputText(string label, string inputTest, ref bool changed);

	[EngineMethod("input_text_multiline_copy_paste", false, null, false)]
	string InputTextMultilineCopyPaste(string label, string inputTest, int textBoxHeight, ref bool changed);
}
