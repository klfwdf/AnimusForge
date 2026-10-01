using System;
using System.Collections.Generic;
using System.Text;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace AnimusForge;

// Native font measurement keeps the first three rendered lines beside the drop cap.
// The remaining text uses the full column; both native RichTextWidgets retain link input.
public sealed class WorldBulletinBodyWidget : Widget
{
	private readonly RichText _measure;
	private string _lastText;
	private string _lastCap;
	private string _lastLanguage;
	private Brush _lastBrush;
	private float _lastWidth;
	private float _lastScale;

	[Editor(false)] public string BodyText { get; set; } = "";
	[Editor(false)] public string DropCapText { get; set; } = "";
	[Editor(false)] public RichTextWidget LeadText { get; set; }
	[Editor(false)] public RichTextWidget TailText { get; set; }
	[Editor(false)] public TextWidget DropCap { get; set; }

	public WorldBulletinBodyWidget(UIContext context) : base(context)
	{
		_measure = new RichText(1, 1, context.FontFactory.DefaultFont, context.FontFactory.GetUsableFontForCharacter);
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (LeadText == null || TailText == null || DropCap == null || Size.X <= 0 || _scaleToUse <= 0) return;
		string language = Context.FontFactory.CurrentLanguage.LanguageID;
		Brush brush = LeadText.ReadOnlyBrush;
		if (_lastText == BodyText && _lastCap == DropCapText && _lastBrush == brush &&
			_lastWidth == Size.X && _lastScale == _scaleToUse && _lastLanguage == language) return;

		_lastText = BodyText;
		_lastCap = DropCapText;
		_lastBrush = brush;
		_lastWidth = Size.X;
		_lastScale = _scaleToUse;
		_lastLanguage = language;
		_measure.CurrentLanguage = Context.FontFactory.CurrentLanguage;
		_measure.CanBreakWords = true;
		_measure.StyleFontContainer.ClearFonts();
		foreach (Style style in brush.Styles)
		{
			Font font = style.Font ?? brush.Font ?? Context.FontFactory.DefaultFont;
			_measure.StyleFontContainer.Add(style.Name, Context.FontFactory.GetMappedFontForLocalization(font.Name), style.FontSize * _scaleToUse);
		}

		float width = Math.Max(1, Size.X - 6f * _scaleToUse);
		bool hasCap = !string.IsNullOrEmpty(DropCapText);
		float indent = hasCap ? 62f : 0f;
		float leadWidth = Math.Max(1, width - indent * _scaleToUse);
		string head = "";
		string tail = BodyText ?? "";
		float capHeight = 0;
		if (hasCap)
		{
			float threeLines = MeasureHeight("国\n国\n国", leadWidth);
			WorldBulletinTextFlow.SplitToHeight(tail, threeLines, text => MeasureHeight(text, leadWidth), out head, out tail);
			capHeight = threeLines / _scaleToUse;
		}
		LeadText.Text = head;
		LeadText.MarginLeft = indent;
		LeadText.IsVisible = head.Length > 0;
		float leadHeight = Math.Max(capHeight, MeasureHeight(head, leadWidth) / _scaleToUse);
		LeadText.SuggestedHeight = leadHeight;
		TailText.Text = tail;
		TailText.MarginTop = leadHeight;
		TailText.IsVisible = tail.Length > 0;
		TailText.SuggestedHeight = MeasureHeight(tail, width) / _scaleToUse;
		DropCap.Text = DropCapText ?? "";
		DropCap.IsVisible = hasCap;
		DropCap.SuggestedHeight = capHeight;
		DropCap.Brush.FontSize = Math.Max(14, (int)(capHeight * 0.85f));
		SuggestedHeight = leadHeight + TailText.SuggestedHeight + 10f;
	}

	private float MeasureHeight(string text, float width)
	{
		if (string.IsNullOrEmpty(text)) return 0;
		_measure.Value = text;
		_measure.SetAllDirty();
		return _measure.GetPreferredSize(true, width, false, 0, Context.SpriteData, _scaleToUse).Y;
	}
}

// Splitting rendered text must close/reopen links and styles, never cut a markup tag.
internal static class WorldBulletinTextFlow
{
	internal static void SplitToHeight(string text, float height, Func<string, float> measure, out string head, out string tail)
	{
		text ??= "";
		// Bounded prefix and at most ten native measurements; never remeasure the whole article.
		int low = 0, high = Math.Min(512, text.Length);
		while (low < high)
		{
			int count = (low + high + 1) / 2;
			Split(text, count, out string candidate, out _);
			if (measure(candidate) <= height) low = count;
			else high = count - 1;
		}
		Split(text, low, out head, out tail);
	}

	internal static void Split(string text, int visibleCount, out string head, out string tail)
	{
		text ??= "";
		var openTags = new List<KeyValuePair<string, string>>();
		int index = 0, visible = 0;
		while (index < text.Length && visible < visibleCount)
		{
			if (text[index] == '<')
			{
				int end = text.IndexOf('>', index);
				if (end >= 0)
				{
					string tag = text.Substring(index, end - index + 1);
					string name = tag.Substring(1, tag.Length - 2).Trim();
					bool closing = name.StartsWith("/", StringComparison.Ordinal);
					name = name.TrimStart('/').Split(' ', '\t', '/')[0];
					if (closing && openTags.Count > 0 && openTags[openTags.Count - 1].Key == name)
						openTags.RemoveAt(openTags.Count - 1);
					else if (!closing && !tag.EndsWith("/>", StringComparison.Ordinal))
						openTags.Add(new KeyValuePair<string, string>(name, tag));
					index = end + 1;
					continue;
				}
			}
			index += char.IsHighSurrogate(text[index]) && index + 1 < text.Length && char.IsLowSurrogate(text[index + 1]) ? 2 : 1;
			visible++;
		}
		if (index == text.Length) { head = text; tail = ""; return; }
		var prefix = new StringBuilder(text.Substring(0, index));
		for (int i = openTags.Count - 1; i >= 0; i--) prefix.Append("</").Append(openTags[i].Key).Append('>');
		var suffix = new StringBuilder();
		foreach (var tag in openTags) suffix.Append(tag.Value);
		suffix.Append(text, index, text.Length - index);
		head = prefix.ToString();
		tail = suffix.ToString();
	}
}
