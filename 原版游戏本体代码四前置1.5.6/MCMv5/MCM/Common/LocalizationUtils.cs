using System;
using System.Collections.Generic;
using HarmonyLib.BUTR.Extensions;

namespace MCM.Common;

public static class LocalizationUtils
{
	private readonly ref struct TextObjectWrapper
	{
		private delegate object TextObjectCtorDelegate(string rawText, Dictionary<string, object>? attributes = null);

		private delegate string? ToStringDelegate(object instance);

		private static readonly TextObjectCtorDelegate? _textObjectCtor = AccessTools2.GetDeclaredConstructorDelegate<TextObjectCtorDelegate>("TaleWorlds.Localization.TextObject", new Type[2]
		{
			typeof(string),
			typeof(Dictionary<string, object>)
		});

		private static readonly ToStringDelegate? _toString = AccessTools2.GetDeclaredDelegate<ToStringDelegate>("TaleWorlds.Localization.TextObject:ToString", Type.EmptyTypes);

		private readonly object? _object;

		public static TextObjectWrapper Create(string rawText, Dictionary<string, object>? attributes = null)
		{
			return new TextObjectWrapper(_textObjectCtor?.Invoke(rawText, attributes));
		}

		private TextObjectWrapper(object? @object)
		{
			_object = @object;
		}

		public string Localize()
		{
			return (_object != null && _toString != null) ? (_toString(_object) ?? string.Empty) : string.Empty;
		}
	}

	public static string Localize(string rawText, Dictionary<string, object>? attributes = null)
	{
		return TextObjectWrapper.Create(rawText, attributes).Localize();
	}
}
