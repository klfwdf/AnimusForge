using System;
using System.Collections.Generic;
using System.Xml;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Chat;

public class ChatLogItemWidget : Widget
{
	public struct ChatMultiLineElement
	{
		public string Line;

		public int IdentModifier;

		public ChatMultiLineElement(string line, int identModifier)
		{
			Line = line;
			IdentModifier = identModifier;
		}
	}

	private int _defaultMarginLeftPerIndent = 20;

	private string _detailOpeningTag = "<Detail>";

	private string _detailClosingTag = "</Detail>";

	private Action<Widget> _fullyInsideAction;

	private ChatLogWidget _chatLogWidget;

	private string _chatLine;

	private RichTextWidget _oneLineTextWidget;

	private ChatCollapsableListPanel _collapsableWidget;

	[Editor(false)]
	public RichTextWidget OneLineTextWidget
	{
		get
		{
			return _oneLineTextWidget;
		}
		set
		{
			if (_oneLineTextWidget != value)
			{
				_oneLineTextWidget = value;
			}
		}
	}

	[Editor(false)]
	public ChatCollapsableListPanel CollapsableWidget
	{
		get
		{
			return _collapsableWidget;
		}
		set
		{
			if (_collapsableWidget != value)
			{
				_collapsableWidget = value;
			}
		}
	}

	[Editor(false)]
	public string ChatLine
	{
		get
		{
			return _chatLine;
		}
		set
		{
			if (_chatLine != value)
			{
				_chatLine = value;
				PostMessage(value);
			}
		}
	}

	[Editor(false)]
	public ChatLogWidget ChatLogWidget
	{
		get
		{
			return _chatLogWidget;
		}
		set
		{
			if (_chatLogWidget != value)
			{
				_chatLogWidget = value;
			}
		}
	}

	public ChatLogItemWidget(UIContext context)
		: base(context)
	{
		_fullyInsideAction = UpdateWidgetFullyInside;
	}

	private void UpdateWidgetFullyInside(Widget widget)
	{
		widget.DoNotRenderIfNotFullyInsideScissor = false;
	}

	protected override void OnParallelUpdate(float dt)
	{
		base.OnParallelUpdate(dt);
		ApplyActionToAllChildrenRecursive(_fullyInsideAction);
	}

	private void PostMessage(string message)
	{
		if (message.IndexOf(_detailOpeningTag, StringComparison.Ordinal) > 0)
		{
			foreach (ChatMultiLineElement item in GetFormattedLinesFromMessage(message))
			{
				RichTextWidget widget = new RichTextWidget(base.Context)
				{
					Id = "FormattedLineRichTextWidget",
					WidthSizePolicy = SizePolicy.StretchToParent,
					HeightSizePolicy = SizePolicy.CoverChildren,
					Brush = OneLineTextWidget.ReadOnlyBrush,
					MarginTop = -2f,
					MarginBottom = -2f,
					IsEnabled = false,
					Text = item.Line,
					MarginLeft = (float)(item.IdentModifier * _defaultMarginLeftPerIndent) * base._inverseScaleToUse,
					ClipContents = false,
					DoNotRenderIfNotFullyInsideScissor = false
				};
				CollapsableWidget.AddChild(widget);
			}
			CollapsableWidget.IsVisible = true;
			OneLineTextWidget.IsVisible = false;
		}
		else
		{
			OneLineTextWidget.Text = message;
			CollapsableWidget.IsVisible = false;
			OneLineTextWidget.IsVisible = true;
		}
	}

	private List<ChatMultiLineElement> GetFormattedLinesFromMessage(string message)
	{
		List<ChatMultiLineElement> lineList = new List<ChatMultiLineElement>();
		XmlDocument xmlDocument = new XmlDocument();
		int num = message.IndexOf(_detailOpeningTag, StringComparison.Ordinal);
		string line = message.Substring(0, num);
		string text = message.Substring(num, message.Length - num);
		text = _detailOpeningTag + text + _detailClosingTag;
		lineList.Add(new ChatMultiLineElement(line, 0));
		try
		{
			xmlDocument.LoadXml(text);
			AddLinesFromXMLRecur(xmlDocument.FirstChild, ref lineList, 0);
		}
		catch (Exception ex)
		{
			Debug.FailedAssert("Couldn't parse chat log message: " + ex.Message, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.GauntletUI.Widgets\\Chat\\ChatLogItemWidget.cs", "GetFormattedLinesFromMessage", 111);
		}
		return lineList;
	}

	private void AddLinesFromXMLRecur(XmlNode currentNode, ref List<ChatMultiLineElement> lineList, int currentIndentModifier)
	{
		if (currentNode.NodeType == XmlNodeType.Text)
		{
			lineList.Add(new ChatMultiLineElement(currentNode.InnerText, currentIndentModifier));
			for (int i = 0; i < currentNode.ChildNodes.Count; i++)
			{
				AddLinesFromXMLRecur(currentNode.ChildNodes.Item(i), ref lineList, currentIndentModifier + 1);
			}
		}
		else
		{
			for (int j = 0; j < currentNode.ChildNodes.Count; j++)
			{
				AddLinesFromXMLRecur(currentNode.ChildNodes.Item(j), ref lineList, currentIndentModifier + 1);
			}
		}
	}
}
