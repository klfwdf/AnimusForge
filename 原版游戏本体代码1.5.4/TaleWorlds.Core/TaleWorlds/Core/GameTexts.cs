using System.Collections.Generic;
using TaleWorlds.Localization;

namespace TaleWorlds.Core;

public static class GameTexts
{
	public class GameTextHelper
	{
		private string _id;

		public GameTextHelper(string id)
		{
			_id = id;
		}

		public GameTextHelper Variation(string text, params object[] propertiesAndWeights)
		{
			_gameTextManager.AddGameText(_id).AddVariation(text, propertiesAndWeights);
			return this;
		}

		public static TextObject MergeTextObjectsWithComma(List<TextObject> textObjects, bool includeAnd)
		{
			return MergeTextObjectsWithSymbol(textObjects, new TextObject("{=kfdxjIad}, "), includeAnd ? new TextObject("{=eob9goyW} and ") : null);
		}

		public static TextObject MergeTextObjectsWithSymbol(List<TextObject> textObjects, TextObject symbol, TextObject lastSymbol = null)
		{
			TextObject textObject;
			switch (textObjects.Count)
			{
			case 0:
				textObject = TextObject.GetEmpty();
				break;
			case 1:
				textObject = textObjects[0];
				break;
			default:
			{
				string text = "{=!}";
				for (int i = 0; i < textObjects.Count - 2; i++)
				{
					text = text + "{VAR_" + i + "}{SYMBOL}";
				}
				text = text + "{VAR_" + (textObjects.Count - 2) + "}{LAST_SYMBOL}{VAR_" + (textObjects.Count - 1) + "}";
				textObject = new TextObject(text);
				for (int j = 0; j < textObjects.Count; j++)
				{
					textObject.SetTextVariable("VAR_" + j, textObjects[j]);
				}
				textObject.SetTextVariable("SYMBOL", symbol);
				textObject.SetTextVariable("LAST_SYMBOL", lastSymbol ?? symbol);
				break;
			}
			}
			return textObject;
		}
	}

	private static GameTextManager _gameTextManager;

	public static void Initialize(GameTextManager gameTextManager)
	{
		_gameTextManager = gameTextManager;
		InitializeGlobalTags();
	}

	public static TextObject FindText(string id, string variation = null)
	{
		return _gameTextManager.FindText(id, variation);
	}

	public static bool TryGetText(string id, out TextObject textObject, string variation = null)
	{
		return _gameTextManager.TryGetText(id, variation, out textObject);
	}

	public static IEnumerable<TextObject> FindAllTextVariations(string id)
	{
		return _gameTextManager.FindAllTextVariations(id);
	}

	public static void SetVariable(string variableName, string content)
	{
		MBTextManager.SetTextVariable(variableName, content);
	}

	public static void SetVariable(string variableName, float content)
	{
		MBTextManager.SetTextVariable(variableName, content);
	}

	public static void SetVariable(string variableName, int content)
	{
		MBTextManager.SetTextVariable(variableName, content);
	}

	public static void SetVariable(string variableName, TextObject content)
	{
		MBTextManager.SetTextVariable(variableName, content);
	}

	public static void ClearInstance()
	{
		_gameTextManager = null;
	}

	public static GameTextHelper AddGameTextWithVariation(string id)
	{
		return new GameTextHelper(id);
	}

	private static void InitializeGlobalTags()
	{
		SetVariable("newline", "\n");
	}
}
