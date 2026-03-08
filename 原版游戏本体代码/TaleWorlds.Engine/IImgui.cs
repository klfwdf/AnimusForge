using System;
using TaleWorlds.Library;

namespace TaleWorlds.Engine
{
	// Token: 0x0200003F RID: 63
	[ApplicationInterfaceBase]
	internal interface IImgui
	{
		// Token: 0x0600064A RID: 1610
		[EngineMethod("begin_main_thread_scope", false, null, false)]
		void BeginMainThreadScope();

		// Token: 0x0600064B RID: 1611
		[EngineMethod("end_main_thread_scope", false, null, false)]
		void EndMainThreadScope();

		// Token: 0x0600064C RID: 1612
		[EngineMethod("push_style_color", false, null, false)]
		void PushStyleColor(int style, ref Vec3 color);

		// Token: 0x0600064D RID: 1613
		[EngineMethod("pop_style_color", false, null, false)]
		void PopStyleColor();

		// Token: 0x0600064E RID: 1614
		[EngineMethod("new_frame", false, null, false)]
		void NewFrame();

		// Token: 0x0600064F RID: 1615
		[EngineMethod("render", false, null, false)]
		void Render();

		// Token: 0x06000650 RID: 1616
		[EngineMethod("begin", false, null, false)]
		void Begin(string text);

		// Token: 0x06000651 RID: 1617
		[EngineMethod("begin_with_close_button", false, null, false)]
		void BeginWithCloseButton(string text, ref bool is_open);

		// Token: 0x06000652 RID: 1618
		[EngineMethod("end", false, null, false)]
		void End();

		// Token: 0x06000653 RID: 1619
		[EngineMethod("text", false, null, false)]
		void Text(string text);

		// Token: 0x06000654 RID: 1620
		[EngineMethod("checkbox", false, null, false)]
		bool Checkbox(string text, ref bool is_checked);

		// Token: 0x06000655 RID: 1621
		[EngineMethod("tree_node", false, null, false)]
		bool TreeNode(string name);

		// Token: 0x06000656 RID: 1622
		[EngineMethod("tree_pop", false, null, false)]
		void TreePop();

		// Token: 0x06000657 RID: 1623
		[EngineMethod("separator", false, null, false)]
		void Separator();

		// Token: 0x06000658 RID: 1624
		[EngineMethod("button", false, null, false)]
		bool Button(string text);

		// Token: 0x06000659 RID: 1625
		[EngineMethod("plot_lines", false, null, false)]
		void PlotLines(string name, float[] values, int valuesCount, int valuesOffset, string overlayText, float minScale, float maxScale, float graphWidth, float graphHeight, int stride);

		// Token: 0x0600065A RID: 1626
		[EngineMethod("progress_bar", false, null, false)]
		void ProgressBar(float value);

		// Token: 0x0600065B RID: 1627
		[EngineMethod("new_line", false, null, false)]
		void NewLine();

		// Token: 0x0600065C RID: 1628
		[EngineMethod("same_line", false, null, false)]
		void SameLine(float posX, float spacingWidth);

		// Token: 0x0600065D RID: 1629
		[EngineMethod("combo", false, null, false)]
		bool Combo(string label, ref int selectedIndex, string items);

		// Token: 0x0600065E RID: 1630
		[EngineMethod("combo_custom_seperator", false, null, false)]
		bool ComboCustomSeperator(string label, ref int selectedIndex, string items, string seperator);

		// Token: 0x0600065F RID: 1631
		[EngineMethod("input_int", false, null, false)]
		bool InputInt(string label, ref int value);

		// Token: 0x06000660 RID: 1632
		[EngineMethod("slider_float", false, null, false)]
		bool SliderFloat(string label, ref float value, float min, float max);

		// Token: 0x06000661 RID: 1633
		[EngineMethod("columns", false, null, false)]
		void Columns(int count = 1, string id = "", bool border = true);

		// Token: 0x06000662 RID: 1634
		[EngineMethod("next_column", false, null, false)]
		void NextColumn();

		// Token: 0x06000663 RID: 1635
		[EngineMethod("radio_button", false, null, false)]
		bool RadioButton(string label, bool active);

		// Token: 0x06000664 RID: 1636
		[EngineMethod("collapsing_header", false, null, false)]
		bool CollapsingHeader(string label);

		// Token: 0x06000665 RID: 1637
		[EngineMethod("is_item_hovered", false, null, false)]
		bool IsItemHovered();

		// Token: 0x06000666 RID: 1638
		[EngineMethod("set_tool_tip", false, null, false)]
		void SetTooltip(string label);

		// Token: 0x06000667 RID: 1639
		[EngineMethod("small_button", false, null, false)]
		bool SmallButton(string label);

		// Token: 0x06000668 RID: 1640
		[EngineMethod("input_float", false, null, false)]
		bool InputFloat(string label, ref float val, float step, float stepFast, int decimalPrecision = -1);

		// Token: 0x06000669 RID: 1641
		[EngineMethod("input_float2", false, null, false)]
		bool InputFloat2(string label, ref float val0, ref float val1, int decimalPrecision = -1);

		// Token: 0x0600066A RID: 1642
		[EngineMethod("input_float3", false, null, false)]
		bool InputFloat3(string label, ref float val0, ref float val1, ref float val2, int decimalPrecision = -1);

		// Token: 0x0600066B RID: 1643
		[EngineMethod("input_float4", false, null, false)]
		bool InputFloat4(string label, ref float val0, ref float val1, ref float val2, ref float val3, int decimalPrecision = -1);

		// Token: 0x0600066C RID: 1644
		[EngineMethod("input_text", false, null, false)]
		string InputText(string label, string inputTest, ref bool changed);

		// Token: 0x0600066D RID: 1645
		[EngineMethod("input_text_multiline_copy_paste", false, null, false)]
		string InputTextMultilineCopyPaste(string label, string inputTest, int textBoxHeight, ref bool changed);
	}
}
