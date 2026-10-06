using System;
using System.Collections.Generic;
using TaleWorlds.Localization;

namespace TaleWorlds.Core;

public class GameText
{
	public struct GameTextVariation
	{
		public readonly string Id;

		public readonly TextObject Text;

		public readonly GameTextManager.ChoiceTag[] Tags;

		internal GameTextVariation(string id, TextObject text, List<GameTextManager.ChoiceTag> choiceTags)
		{
			Id = id;
			Text = text;
			Tags = choiceTags.ToArray();
		}
	}

	private readonly List<GameTextVariation> _variationList;

	public string Id { get; private set; }

	public IEnumerable<GameTextVariation> Variations => _variationList;

	public TextObject DefaultText
	{
		get
		{
			if (_variationList != null && _variationList.Count > 0)
			{
				return _variationList[0].Text;
			}
			return null;
		}
	}

	internal GameText()
	{
		_variationList = new List<GameTextVariation>();
	}

	internal GameText(string id)
	{
		Id = id;
		_variationList = new List<GameTextVariation>();
	}

	internal TextObject GetVariation(string variationId)
	{
		foreach (GameTextVariation variation in _variationList)
		{
			if (variation.Id.Equals(variationId))
			{
				return variation.Text;
			}
		}
		return null;
	}

	public void AddVariationWithId(string variationId, TextObject text, List<GameTextManager.ChoiceTag> choiceTags)
	{
		foreach (GameTextVariation variation in _variationList)
		{
			if (variation.Id.Equals(variationId) && variation.Text.ToString().Equals(text.ToString()))
			{
				return;
			}
		}
		_variationList.Add(new GameTextVariation(variationId, text, choiceTags));
	}

	public void SetVariationWithId(string variationId, TextObject text, List<GameTextManager.ChoiceTag> choiceTags)
	{
		for (int i = 0; i < _variationList.Count; i++)
		{
			if (_variationList[i].Id.Equals(variationId))
			{
				_variationList[i] = new GameTextVariation(variationId, text, choiceTags);
				return;
			}
		}
		_variationList.Add(new GameTextVariation(variationId, text, choiceTags));
	}

	public void AddVariation(string text, params object[] propertiesAndWeights)
	{
		List<GameTextManager.ChoiceTag> list = new List<GameTextManager.ChoiceTag>();
		for (int i = 0; i < propertiesAndWeights.Length; i += 2)
		{
			string tagName = (string)propertiesAndWeights[i];
			int weight = Convert.ToInt32(propertiesAndWeights[i + 1]);
			list.Add(new GameTextManager.ChoiceTag(tagName, weight));
		}
		AddVariationWithId("", new TextObject(text), list);
	}
}
