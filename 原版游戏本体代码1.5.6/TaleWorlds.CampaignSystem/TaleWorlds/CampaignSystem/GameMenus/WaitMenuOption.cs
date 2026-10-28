using System;
using System.Reflection;
using System.Xml;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.GameMenus;

public class WaitMenuOption
{
	public delegate bool OnConditionDelegate(MenuCallbackArgs args);

	public delegate void OnConsequenceDelegate(MenuCallbackArgs args);

	private string _idString;

	private TextObject _text;

	private string _tooltip;

	private MethodInfo _methodOnCondition;

	public OnConditionDelegate OnCondition;

	private MethodInfo _methodOnConsequence;

	public OnConsequenceDelegate OnConsequence;

	private bool _isLeave;

	public int Priority { get; private set; }

	public TextObject Text => _text;

	public string IdString => _idString;

	public string Tooltip => _tooltip;

	public bool IsLeave => _isLeave;

	internal WaitMenuOption()
	{
		Priority = 100;
		_text = null;
		_tooltip = "";
	}

	internal WaitMenuOption(string idString, TextObject text, OnConditionDelegate condition, OnConsequenceDelegate consequence, int priority = 100, string tooltip = "")
	{
		_idString = idString;
		_text = text;
		OnCondition = condition;
		OnConsequence = consequence;
		Priority = priority;
		_tooltip = tooltip;
	}

	public bool GetConditionsHold(Game game, MapState mapState)
	{
		if (OnCondition != null)
		{
			MenuCallbackArgs args = new MenuCallbackArgs(mapState, Text);
			return OnCondition(args);
		}
		return true;
	}

	public void RunConsequence(Game game, MapState mapState)
	{
		if (OnConsequence != null)
		{
			MenuCallbackArgs args = new MenuCallbackArgs(mapState, Text);
			OnConsequence(args);
		}
	}

	public void Deserialize(XmlNode node, Type typeOfWaitMenusCallbacks)
	{
		if (node.Attributes == null)
		{
			throw new TWXmlLoadException("node.Attributes != null");
		}
		_idString = node.Attributes["id"].Value;
		XmlNode xmlNode = node.Attributes["text"];
		if (xmlNode != null)
		{
			_text = new TextObject(xmlNode.InnerText);
		}
		if (node.Attributes["is_leave"] != null)
		{
			_isLeave = true;
		}
		XmlNode xmlNode2 = node.Attributes["on_condition"];
		if (xmlNode2 != null)
		{
			string innerText = xmlNode2.InnerText;
			_methodOnCondition = typeOfWaitMenusCallbacks.GetMethod(innerText);
			if (_methodOnCondition == null)
			{
				throw new MBNotFoundException("Can not find WaitMenuOption condition:" + innerText);
			}
			OnCondition = (OnConditionDelegate)Delegate.CreateDelegate(typeof(OnConditionDelegate), null, _methodOnCondition);
		}
		XmlNode xmlNode3 = node.Attributes["on_consequence"];
		if (xmlNode3 != null)
		{
			string innerText2 = xmlNode3.InnerText;
			_methodOnConsequence = typeOfWaitMenusCallbacks.GetMethod(innerText2);
			if (_methodOnConsequence == null)
			{
				throw new MBNotFoundException("Can not find WaitMenuOption consequence:" + innerText2);
			}
			OnConsequence = (OnConsequenceDelegate)Delegate.CreateDelegate(typeof(OnConsequenceDelegate), null, _methodOnConsequence);
		}
	}
}
