using TaleWorlds.Library;

namespace TaleWorlds.Engine;

public class Imgui
{
	public enum ColorStyle
	{
		Text,
		TextDisabled,
		WindowBg,
		ChildWindowBg,
		PopupBg,
		Border,
		BorderShadow,
		FrameBg,
		FrameBgHovered,
		FrameBgActive,
		TitleBg,
		TitleBgCollapsed,
		TitleBgActive,
		MenuBarBg,
		ScrollbarBg,
		ScrollbarGrab,
		ScrollbarGrabHovered,
		ScrollbarGrabActive,
		ComboBg,
		CheckMark,
		SliderGrab,
		SliderGrabActive,
		Button,
		ButtonHovered,
		ButtonActive,
		Header,
		HeaderHovered,
		HeaderActive,
		Column,
		ColumnHovered,
		ColumnActive,
		ResizeGrip,
		ResizeGripHovered,
		ResizeGripActive,
		CloseButton,
		CloseButtonHovered,
		CloseButtonActive,
		PlotLines,
		PlotLinesHovered,
		PlotHistogram,
		PlotHistogramHovered,
		TextSelectedBg,
		ModalWindowDarkening
	}

	public static void BeginMainThreadScope()
	{
		EngineApplicationInterface.IImgui.BeginMainThreadScope();
	}

	public static void EndMainThreadScope()
	{
		EngineApplicationInterface.IImgui.EndMainThreadScope();
	}

	public static void PushStyleColor(ColorStyle style, ref Vec3 color)
	{
		EngineApplicationInterface.IImgui.PushStyleColor((int)style, ref color);
	}

	public static void PopStyleColor()
	{
		EngineApplicationInterface.IImgui.PopStyleColor();
	}

	public static void NewFrame()
	{
		EngineApplicationInterface.IImgui.NewFrame();
	}

	public static void Render()
	{
		EngineApplicationInterface.IImgui.Render();
	}

	public static void Begin(string text)
	{
		EngineApplicationInterface.IImgui.Begin(text);
	}

	public static void Begin(string text, ref bool is_open)
	{
		EngineApplicationInterface.IImgui.BeginWithCloseButton(text, ref is_open);
	}

	public static void End()
	{
		EngineApplicationInterface.IImgui.End();
	}

	public static void Text(string text)
	{
		EngineApplicationInterface.IImgui.Text(text);
	}

	public static bool Checkbox(string text, ref bool is_checked)
	{
		return EngineApplicationInterface.IImgui.Checkbox(text, ref is_checked);
	}

	public static bool TreeNode(string name)
	{
		return EngineApplicationInterface.IImgui.TreeNode(name);
	}

	public static void TreePop()
	{
		EngineApplicationInterface.IImgui.TreePop();
	}

	public static void Separator()
	{
		EngineApplicationInterface.IImgui.Separator();
	}

	public static bool Button(string text)
	{
		return EngineApplicationInterface.IImgui.Button(text);
	}

	public static void PlotLines(string name, float[] values, int valuesCount, int valuesOffset, string overlayText, float minScale, float maxScale, float graphWidth, float graphHeight, int stride)
	{
		EngineApplicationInterface.IImgui.PlotLines(name, values, valuesCount, valuesOffset, overlayText, minScale, maxScale, graphWidth, graphHeight, stride);
	}

	public static void ProgressBar(float progress)
	{
		EngineApplicationInterface.IImgui.ProgressBar(progress);
	}

	public static void NewLine()
	{
		EngineApplicationInterface.IImgui.NewLine();
	}

	public static void SameLine(float posX = 0f, float spacingWidth = 0f)
	{
		EngineApplicationInterface.IImgui.SameLine(posX, spacingWidth);
	}

	public static bool Combo(string label, ref int selectedIndex, string items)
	{
		return EngineApplicationInterface.IImgui.Combo(label, ref selectedIndex, items);
	}

	public static bool ComboCustomSeperator(string label, ref int selectedIndex, string items, char seperator)
	{
		return EngineApplicationInterface.IImgui.ComboCustomSeperator(label, ref selectedIndex, items, seperator.ToString());
	}

	public static bool InputInt(string label, ref int value)
	{
		return EngineApplicationInterface.IImgui.InputInt(label, ref value);
	}

	public static bool SliderFloat(string label, ref float value, float min, float max)
	{
		return EngineApplicationInterface.IImgui.SliderFloat(label, ref value, min, max);
	}

	public static void Columns(int count = 1, string id = "", bool border = true)
	{
		EngineApplicationInterface.IImgui.Columns(count, id, border);
	}

	public static void NextColumn()
	{
		EngineApplicationInterface.IImgui.NextColumn();
	}

	public static bool RadioButton(string label, bool active)
	{
		return EngineApplicationInterface.IImgui.RadioButton(label, active);
	}

	public static bool CollapsingHeader(string label)
	{
		return EngineApplicationInterface.IImgui.CollapsingHeader(label);
	}

	public static bool IsItemHovered()
	{
		return EngineApplicationInterface.IImgui.IsItemHovered();
	}

	public static void SetTooltip(string label)
	{
		EngineApplicationInterface.IImgui.SetTooltip(label);
	}

	public static bool SmallButton(string label)
	{
		return EngineApplicationInterface.IImgui.SmallButton(label);
	}

	public static bool InputFloat(string label, ref float val, float step, float stepFast, int decimalPrecision = -1)
	{
		return EngineApplicationInterface.IImgui.InputFloat(label, ref val, step, stepFast, decimalPrecision);
	}

	public static bool InputText(string label, ref string text)
	{
		bool changed = false;
		text = EngineApplicationInterface.IImgui.InputText(label, text, ref changed);
		return changed;
	}

	public static bool InputTextMultilineCopyPaste(string label, int textBoxHeight, ref string text)
	{
		bool changed = false;
		text = EngineApplicationInterface.IImgui.InputTextMultilineCopyPaste(label, text, textBoxHeight, ref changed);
		return changed;
	}

	public static bool InputFloat2(string label, ref float val0, ref float val1, int decimalPrecision = -1)
	{
		return EngineApplicationInterface.IImgui.InputFloat2(label, ref val0, ref val1, decimalPrecision);
	}

	public static bool InputFloat3(string label, ref float val0, ref float val1, ref float val2, int decimalPrecision = -1)
	{
		return EngineApplicationInterface.IImgui.InputFloat3(label, ref val0, ref val1, ref val2, decimalPrecision);
	}

	public static bool InputFloat4(string label, ref float val0, ref float val1, ref float val2, ref float val3, int decimalPrecision = -1)
	{
		return EngineApplicationInterface.IImgui.InputFloat4(label, ref val0, ref val1, ref val2, ref val3, decimalPrecision);
	}
}
