using System;
using TaleWorlds.Library;

namespace TaleWorlds.Engine
{
	// Token: 0x02000052 RID: 82
	public class Imgui
	{
		// Token: 0x0600086E RID: 2158 RVA: 0x000069AF File Offset: 0x00004BAF
		public static void BeginMainThreadScope()
		{
			EngineApplicationInterface.IImgui.BeginMainThreadScope();
		}

		// Token: 0x0600086F RID: 2159 RVA: 0x000069BB File Offset: 0x00004BBB
		public static void EndMainThreadScope()
		{
			EngineApplicationInterface.IImgui.EndMainThreadScope();
		}

		// Token: 0x06000870 RID: 2160 RVA: 0x000069C7 File Offset: 0x00004BC7
		public static void PushStyleColor(Imgui.ColorStyle style, ref Vec3 color)
		{
			EngineApplicationInterface.IImgui.PushStyleColor((int)style, ref color);
		}

		// Token: 0x06000871 RID: 2161 RVA: 0x000069D5 File Offset: 0x00004BD5
		public static void PopStyleColor()
		{
			EngineApplicationInterface.IImgui.PopStyleColor();
		}

		// Token: 0x06000872 RID: 2162 RVA: 0x000069E1 File Offset: 0x00004BE1
		public static void NewFrame()
		{
			EngineApplicationInterface.IImgui.NewFrame();
		}

		// Token: 0x06000873 RID: 2163 RVA: 0x000069ED File Offset: 0x00004BED
		public static void Render()
		{
			EngineApplicationInterface.IImgui.Render();
		}

		// Token: 0x06000874 RID: 2164 RVA: 0x000069F9 File Offset: 0x00004BF9
		public static void Begin(string text)
		{
			EngineApplicationInterface.IImgui.Begin(text);
		}

		// Token: 0x06000875 RID: 2165 RVA: 0x00006A06 File Offset: 0x00004C06
		public static void Begin(string text, ref bool is_open)
		{
			EngineApplicationInterface.IImgui.BeginWithCloseButton(text, ref is_open);
		}

		// Token: 0x06000876 RID: 2166 RVA: 0x00006A14 File Offset: 0x00004C14
		public static void End()
		{
			EngineApplicationInterface.IImgui.End();
		}

		// Token: 0x06000877 RID: 2167 RVA: 0x00006A20 File Offset: 0x00004C20
		public static void Text(string text)
		{
			EngineApplicationInterface.IImgui.Text(text);
		}

		// Token: 0x06000878 RID: 2168 RVA: 0x00006A2D File Offset: 0x00004C2D
		public static bool Checkbox(string text, ref bool is_checked)
		{
			return EngineApplicationInterface.IImgui.Checkbox(text, ref is_checked);
		}

		// Token: 0x06000879 RID: 2169 RVA: 0x00006A3B File Offset: 0x00004C3B
		public static bool TreeNode(string name)
		{
			return EngineApplicationInterface.IImgui.TreeNode(name);
		}

		// Token: 0x0600087A RID: 2170 RVA: 0x00006A48 File Offset: 0x00004C48
		public static void TreePop()
		{
			EngineApplicationInterface.IImgui.TreePop();
		}

		// Token: 0x0600087B RID: 2171 RVA: 0x00006A54 File Offset: 0x00004C54
		public static void Separator()
		{
			EngineApplicationInterface.IImgui.Separator();
		}

		// Token: 0x0600087C RID: 2172 RVA: 0x00006A60 File Offset: 0x00004C60
		public static bool Button(string text)
		{
			return EngineApplicationInterface.IImgui.Button(text);
		}

		// Token: 0x0600087D RID: 2173 RVA: 0x00006A70 File Offset: 0x00004C70
		public static void PlotLines(string name, float[] values, int valuesCount, int valuesOffset, string overlayText, float minScale, float maxScale, float graphWidth, float graphHeight, int stride)
		{
			EngineApplicationInterface.IImgui.PlotLines(name, values, valuesCount, valuesOffset, overlayText, minScale, maxScale, graphWidth, graphHeight, stride);
		}

		// Token: 0x0600087E RID: 2174 RVA: 0x00006A97 File Offset: 0x00004C97
		public static void ProgressBar(float progress)
		{
			EngineApplicationInterface.IImgui.ProgressBar(progress);
		}

		// Token: 0x0600087F RID: 2175 RVA: 0x00006AA4 File Offset: 0x00004CA4
		public static void NewLine()
		{
			EngineApplicationInterface.IImgui.NewLine();
		}

		// Token: 0x06000880 RID: 2176 RVA: 0x00006AB0 File Offset: 0x00004CB0
		public static void SameLine(float posX = 0f, float spacingWidth = 0f)
		{
			EngineApplicationInterface.IImgui.SameLine(posX, spacingWidth);
		}

		// Token: 0x06000881 RID: 2177 RVA: 0x00006ABE File Offset: 0x00004CBE
		public static bool Combo(string label, ref int selectedIndex, string items)
		{
			return EngineApplicationInterface.IImgui.Combo(label, ref selectedIndex, items);
		}

		// Token: 0x06000882 RID: 2178 RVA: 0x00006ACD File Offset: 0x00004CCD
		public static bool ComboCustomSeperator(string label, ref int selectedIndex, string items, char seperator)
		{
			return EngineApplicationInterface.IImgui.ComboCustomSeperator(label, ref selectedIndex, items, seperator.ToString());
		}

		// Token: 0x06000883 RID: 2179 RVA: 0x00006AE3 File Offset: 0x00004CE3
		public static bool InputInt(string label, ref int value)
		{
			return EngineApplicationInterface.IImgui.InputInt(label, ref value);
		}

		// Token: 0x06000884 RID: 2180 RVA: 0x00006AF1 File Offset: 0x00004CF1
		public static bool SliderFloat(string label, ref float value, float min, float max)
		{
			return EngineApplicationInterface.IImgui.SliderFloat(label, ref value, min, max);
		}

		// Token: 0x06000885 RID: 2181 RVA: 0x00006B01 File Offset: 0x00004D01
		public static void Columns(int count = 1, string id = "", bool border = true)
		{
			EngineApplicationInterface.IImgui.Columns(count, id, border);
		}

		// Token: 0x06000886 RID: 2182 RVA: 0x00006B10 File Offset: 0x00004D10
		public static void NextColumn()
		{
			EngineApplicationInterface.IImgui.NextColumn();
		}

		// Token: 0x06000887 RID: 2183 RVA: 0x00006B1C File Offset: 0x00004D1C
		public static bool RadioButton(string label, bool active)
		{
			return EngineApplicationInterface.IImgui.RadioButton(label, active);
		}

		// Token: 0x06000888 RID: 2184 RVA: 0x00006B2A File Offset: 0x00004D2A
		public static bool CollapsingHeader(string label)
		{
			return EngineApplicationInterface.IImgui.CollapsingHeader(label);
		}

		// Token: 0x06000889 RID: 2185 RVA: 0x00006B37 File Offset: 0x00004D37
		public static bool IsItemHovered()
		{
			return EngineApplicationInterface.IImgui.IsItemHovered();
		}

		// Token: 0x0600088A RID: 2186 RVA: 0x00006B43 File Offset: 0x00004D43
		public static void SetTooltip(string label)
		{
			EngineApplicationInterface.IImgui.SetTooltip(label);
		}

		// Token: 0x0600088B RID: 2187 RVA: 0x00006B50 File Offset: 0x00004D50
		public static bool SmallButton(string label)
		{
			return EngineApplicationInterface.IImgui.SmallButton(label);
		}

		// Token: 0x0600088C RID: 2188 RVA: 0x00006B5D File Offset: 0x00004D5D
		public static bool InputFloat(string label, ref float val, float step, float stepFast, int decimalPrecision = -1)
		{
			return EngineApplicationInterface.IImgui.InputFloat(label, ref val, step, stepFast, decimalPrecision);
		}

		// Token: 0x0600088D RID: 2189 RVA: 0x00006B70 File Offset: 0x00004D70
		public static bool InputText(string label, ref string text)
		{
			bool result = false;
			text = EngineApplicationInterface.IImgui.InputText(label, text, ref result);
			return result;
		}

		// Token: 0x0600088E RID: 2190 RVA: 0x00006B94 File Offset: 0x00004D94
		public static bool InputTextMultilineCopyPaste(string label, int textBoxHeight, ref string text)
		{
			bool result = false;
			text = EngineApplicationInterface.IImgui.InputTextMultilineCopyPaste(label, text, textBoxHeight, ref result);
			return result;
		}

		// Token: 0x0600088F RID: 2191 RVA: 0x00006BB6 File Offset: 0x00004DB6
		public static bool InputFloat2(string label, ref float val0, ref float val1, int decimalPrecision = -1)
		{
			return EngineApplicationInterface.IImgui.InputFloat2(label, ref val0, ref val1, decimalPrecision);
		}

		// Token: 0x06000890 RID: 2192 RVA: 0x00006BC6 File Offset: 0x00004DC6
		public static bool InputFloat3(string label, ref float val0, ref float val1, ref float val2, int decimalPrecision = -1)
		{
			return EngineApplicationInterface.IImgui.InputFloat3(label, ref val0, ref val1, ref val2, decimalPrecision);
		}

		// Token: 0x06000891 RID: 2193 RVA: 0x00006BD8 File Offset: 0x00004DD8
		public static bool InputFloat4(string label, ref float val0, ref float val1, ref float val2, ref float val3, int decimalPrecision = -1)
		{
			return EngineApplicationInterface.IImgui.InputFloat4(label, ref val0, ref val1, ref val2, ref val3, decimalPrecision);
		}

		// Token: 0x020000C1 RID: 193
		public enum ColorStyle
		{
			// Token: 0x040003B9 RID: 953
			Text,
			// Token: 0x040003BA RID: 954
			TextDisabled,
			// Token: 0x040003BB RID: 955
			WindowBg,
			// Token: 0x040003BC RID: 956
			ChildWindowBg,
			// Token: 0x040003BD RID: 957
			PopupBg,
			// Token: 0x040003BE RID: 958
			Border,
			// Token: 0x040003BF RID: 959
			BorderShadow,
			// Token: 0x040003C0 RID: 960
			FrameBg,
			// Token: 0x040003C1 RID: 961
			FrameBgHovered,
			// Token: 0x040003C2 RID: 962
			FrameBgActive,
			// Token: 0x040003C3 RID: 963
			TitleBg,
			// Token: 0x040003C4 RID: 964
			TitleBgCollapsed,
			// Token: 0x040003C5 RID: 965
			TitleBgActive,
			// Token: 0x040003C6 RID: 966
			MenuBarBg,
			// Token: 0x040003C7 RID: 967
			ScrollbarBg,
			// Token: 0x040003C8 RID: 968
			ScrollbarGrab,
			// Token: 0x040003C9 RID: 969
			ScrollbarGrabHovered,
			// Token: 0x040003CA RID: 970
			ScrollbarGrabActive,
			// Token: 0x040003CB RID: 971
			ComboBg,
			// Token: 0x040003CC RID: 972
			CheckMark,
			// Token: 0x040003CD RID: 973
			SliderGrab,
			// Token: 0x040003CE RID: 974
			SliderGrabActive,
			// Token: 0x040003CF RID: 975
			Button,
			// Token: 0x040003D0 RID: 976
			ButtonHovered,
			// Token: 0x040003D1 RID: 977
			ButtonActive,
			// Token: 0x040003D2 RID: 978
			Header,
			// Token: 0x040003D3 RID: 979
			HeaderHovered,
			// Token: 0x040003D4 RID: 980
			HeaderActive,
			// Token: 0x040003D5 RID: 981
			Column,
			// Token: 0x040003D6 RID: 982
			ColumnHovered,
			// Token: 0x040003D7 RID: 983
			ColumnActive,
			// Token: 0x040003D8 RID: 984
			ResizeGrip,
			// Token: 0x040003D9 RID: 985
			ResizeGripHovered,
			// Token: 0x040003DA RID: 986
			ResizeGripActive,
			// Token: 0x040003DB RID: 987
			CloseButton,
			// Token: 0x040003DC RID: 988
			CloseButtonHovered,
			// Token: 0x040003DD RID: 989
			CloseButtonActive,
			// Token: 0x040003DE RID: 990
			PlotLines,
			// Token: 0x040003DF RID: 991
			PlotLinesHovered,
			// Token: 0x040003E0 RID: 992
			PlotHistogram,
			// Token: 0x040003E1 RID: 993
			PlotHistogramHovered,
			// Token: 0x040003E2 RID: 994
			TextSelectedBg,
			// Token: 0x040003E3 RID: 995
			ModalWindowDarkening
		}
	}
}
