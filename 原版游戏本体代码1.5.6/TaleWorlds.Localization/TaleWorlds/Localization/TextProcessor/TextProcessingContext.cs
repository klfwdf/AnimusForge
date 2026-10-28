using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TaleWorlds.Library;
using TaleWorlds.Localization.Expressions;

namespace TaleWorlds.Localization.TextProcessor;

public class TextProcessingContext
{
	private readonly Dictionary<string, TextObject> _variables = new Dictionary<string, TextObject>(new CaseInsensitiveComparer());

	private readonly Dictionary<string, MBTextModel> _functions = new Dictionary<string, MBTextModel>();

	private readonly Stack<TextObject[]> _curParams = new Stack<TextObject[]>();

	private readonly Stack<TextObject[]> _curParamsWithoutEvaluate = new Stack<TextObject[]>();

	internal void SetTextVariable(string variableName, TextObject data)
	{
		_variables[variableName] = data;
	}

	internal TextObject GetRawTextVariable(string variableName, TextObject parent)
	{
		TextObject variable = null;
		if (parent != null && parent.GetVariableValue(variableName, out variable))
		{
			return variable;
		}
		if (!_variables.ContainsKey(variableName))
		{
			return TextObject.GetEmpty();
		}
		return _variables[variableName];
	}

	internal MultiStatement GetVariableValue(string variableName, TextObject parent)
	{
		TextObject value = null;
		MBTextModel mBTextModel = null;
		if (!(parent != null) || !parent.GetVariableValue(variableName, out value))
		{
			_variables.TryGetValue(variableName, out value);
		}
		if (value != null)
		{
			mBTextModel = MBTextParser.Parse(MBTextManager.Tokenizer.Tokenize(value.ToStringWithoutClear()));
		}
		if (mBTextModel != null)
		{
			if (mBTextModel.RootExpressions.Count == 1 && mBTextModel.RootExpressions[0] is MultiStatement)
			{
				return new MultiStatement((mBTextModel.RootExpressions[0] as MultiStatement).SubStatements);
			}
			return new MultiStatement(mBTextModel.RootExpressions);
		}
		return null;
	}

	internal (TextObject, bool) GetVariableValueAsTextObject(string variableName, TextObject parent)
	{
		TextObject variable = null;
		if (!TextObject.IsNullOrEmpty(parent))
		{
			if (parent.GetVariableValue(variableName, out variable))
			{
				return (variable, true);
			}
			variable = FindNestedFieldValue(MBTextManager.GetLocalizedText(parent.Value), variableName, parent);
			if ((object)variable != null && variable.Length > 0)
			{
				return (variable, true);
			}
		}
		if (!(variable == null) && variable.Length != 0)
		{
			return (variable, true);
		}
		return (new TextObject("{=!}ERROR: " + variableName + " variable has not been set before."), false);
	}

	internal MultiStatement GetArrayAccess(string variableName, int index)
	{
		string key = variableName + ":" + index;
		if (_variables.TryGetValue(key, out var value))
		{
			return new MultiStatement(MBTextParser.Parse(MBTextManager.Tokenizer.Tokenize(value.ToStringWithoutClear())).RootExpressions);
		}
		return null;
	}

	private int CountMarkerOccurancesInString(string searchedIdentifier, TextObject parent)
	{
		Regex regex = new Regex("{." + searchedIdentifier + "}");
		TextObject variable = parent;
		if (parent.IsLink)
		{
			string key = parent.Attributes.First((KeyValuePair<string, object> x) => !x.Key.Equals("LINK")).Key;
			parent.GetVariableValue(key, out variable);
		}
		string input = MBTextManager.ProcessWithoutLanguageProcessor(variable);
		if (regex.IsMatch(input))
		{
			return 1;
		}
		return 0;
	}

	internal string GetParameterWithMarkerOccurance(string token, TextObject parent)
	{
		int num = token.IndexOf('!');
		if (num == -1)
		{
			return "";
		}
		string rawValue = token.Substring(0, num);
		string searchedIdentifier = token.Substring(num + 2, token.Length - num - 2);
		TextObject functionParamWithoutEvaluate = GetFunctionParamWithoutEvaluate(rawValue);
		if (((parent?.Attributes != null && parent.TryGetAttributesValue(functionParamWithoutEvaluate.ToStringWithoutClear(), out var value)) || _variables.TryGetValue(functionParamWithoutEvaluate.ToStringWithoutClear(), out value)) && value.Length > 0)
		{
			return CountMarkerOccurancesInString(searchedIdentifier, value).ToString();
		}
		return "";
	}

	internal string GetParameterWithMarkerOccurances(string token, TextObject parent)
	{
		int num = token.IndexOf('!');
		if (num == -1)
		{
			return "";
		}
		string rawValue = token.Substring(0, num);
		int num2 = token.IndexOf('[') + 1;
		int num3 = token.IndexOf(']');
		string[] array = token.Substring(num2, num3 - num2).Split(new char[1] { ',' });
		TextObject functionParamWithoutEvaluate = GetFunctionParamWithoutEvaluate(rawValue);
		if (((parent?.Attributes != null && parent.TryGetAttributesValue(functionParamWithoutEvaluate.ToStringWithoutClear(), out var value)) || _variables.TryGetValue(functionParamWithoutEvaluate.ToStringWithoutClear(), out value)) && value.Length > 0)
		{
			string[] array2 = array;
			foreach (string searchedIdentifier in array2)
			{
				int num4 = CountMarkerOccurancesInString(searchedIdentifier, value);
				if (num4 > 0)
				{
					return num4.ToString();
				}
			}
			return "0";
		}
		return "";
	}

	internal static bool IsDeclaration(string token)
	{
		if (token.Length > 1)
		{
			return token[0] == '@';
		}
		return false;
	}

	internal static bool IsLinkToken(string token)
	{
		if (!(token == ".link"))
		{
			return token == "LINK";
		}
		return true;
	}

	internal static bool IsDeclarationFinalizer(string token)
	{
		if (token.Length == 2 && (token[0] == '\\' || token[0] == '/'))
		{
			return token[1] == '@';
		}
		return false;
	}

	private static TextObject FindNestedFieldValue(string text, string identifier, TextObject parent)
	{
		string[] fieldNames = identifier.Split(new char[1] { '.' }, StringSplitOptions.RemoveEmptyEntries);
		return new TextObject(GetFieldValue(text, fieldNames, parent));
	}

	internal (TextObject value, bool doesValueExist) GetQualifiedVariableValue(string token, TextObject parent)
	{
		int num = token.IndexOf('.');
		if (num == -1)
		{
			return GetVariableValueAsTextObject(token, parent);
		}
		string text = token.Substring(0, num);
		string text2 = token.Substring(num + 1, token.Length - (num + 1));
		if (parent?.Attributes != null && parent.TryGetAttributesValue(text, out var value))
		{
			var (textObject, item) = GetQualifiedVariableValue(text2, value);
			if (!textObject.IsEmpty())
			{
				return (value: textObject, doesValueExist: item);
			}
		}
		else
		{
			if (_variables.TryGetValue(text, out var value2) && value2.Length > 0)
			{
				return (value: FindNestedFieldValue(MBTextManager.GetLocalizedText(value2.Value), text2, value2), doesValueExist: true);
			}
			foreach (KeyValuePair<string, TextObject> variable in _variables)
			{
				if (!(variable.Key == text) || variable.Value.Attributes == null)
				{
					continue;
				}
				foreach (KeyValuePair<string, object> attribute in variable.Value.Attributes)
				{
					if (attribute.Key == text2)
					{
						return (value: TextObject.TryGetOrCreateFromObject(attribute.Value), doesValueExist: true);
					}
				}
			}
		}
		return (value: TextObject.GetEmpty(), doesValueExist: false);
	}

	private static string GetFieldValue(string text, string[] fieldNames, TextObject parent)
	{
		int i = 0;
		int num = 0;
		int num2 = 0;
		MBStringBuilder targetString = default(MBStringBuilder);
		targetString.Initialize(16, "GetFieldValue");
		bool flag = false;
		for (; i < text.Length; i++)
		{
			if (text[i] != '{')
			{
				if (num == fieldNames.Length && num2 == num)
				{
					targetString.Append(text[i]);
				}
				continue;
			}
			string text2 = ReadFirstToken(text, ref i);
			if (IsLinkToken(text2))
			{
				flag = true;
			}
			else if (IsDeclarationFinalizer(text2))
			{
				num--;
				if (num2 > num)
				{
					num2 = num;
				}
			}
			else if (IsDeclaration(text2))
			{
				string strB = text2.Substring(1);
				bool num3 = num2 == num && num < fieldNames.Length && string.Compare(fieldNames[num], strB, StringComparison.InvariantCultureIgnoreCase) == 0;
				num++;
				if (num3)
				{
					num2 = num;
				}
			}
			else if (flag)
			{
				if (parent.Attributes != null && parent.TryGetAttributesValue(text2, out var value))
				{
					return GetFieldValuesFromLinks(fieldNames, value, ref targetString);
				}
			}
			else if (num == fieldNames.Length && num2 == num)
			{
				targetString.Append("{" + text2 + "}");
			}
		}
		return targetString.ToStringAndRelease();
	}

	private static string GetFieldValuesFromLinks(string[] fieldNames, TextObject value, ref MBStringBuilder targetString)
	{
		if (fieldNames.Length == 1 && value.TryGetAttributesValue(fieldNames[0], out var value2))
		{
			targetString.Append(value2);
			return targetString.ToStringAndRelease();
		}
		targetString.Append(GetFieldValue(MBTextManager.GetLocalizedText(value.Value), fieldNames, null));
		return targetString.ToStringAndRelease();
	}

	internal static string ReadFirstToken(string text, ref int i)
	{
		int num = i;
		while (i < text.Length && text[i] != '}')
		{
			i++;
		}
		int num2 = i - num;
		return text.Substring(num + 1, num2 - 1);
	}

	internal TextObject CallFunction(string functionName, List<TextExpression> functionParams, TextObject parent)
	{
		TextObject[] array = new TextObject[functionParams.Count];
		TextObject[] array2 = new TextObject[functionParams.Count];
		for (int i = 0; i < functionParams.Count; i++)
		{
			array[i] = new TextObject(functionParams[i].EvaluateString(this, parent));
			array2[i] = new TextObject(functionParams[i].RawValue);
		}
		_curParams.Push(array);
		_curParamsWithoutEvaluate.Push(array2);
		MBStringBuilder mBStringBuilder = default(MBStringBuilder);
		MBTextModel functionBody = GetFunctionBody(functionName);
		mBStringBuilder.Initialize(16, "CallFunction");
		if (functionBody != null)
		{
			foreach (TextExpression rootExpression in functionBody.RootExpressions)
			{
				mBStringBuilder.Append(rootExpression.EvaluateString(this, parent));
			}
		}
		else if (array.Length != 0)
		{
			mBStringBuilder.Append(array[0]);
		}
		string value = mBStringBuilder.ToStringAndRelease();
		_curParams.Pop();
		_curParamsWithoutEvaluate.Pop();
		return new TextObject(value);
	}

	public void SetFunction(string functionName, MBTextModel functionBody)
	{
		_functions[functionName] = functionBody;
	}

	public void ResetFunctions()
	{
		_functions.Clear();
	}

	public MBTextModel GetFunctionBody(string functionName)
	{
		_functions.TryGetValue(functionName, out var value);
		return value;
	}

	public TextObject GetFunctionParam(string rawValue)
	{
		if (int.TryParse(rawValue.Substring(1), out var result))
		{
			if (_curParams.Count > 0 && _curParams.Peek().Length > result)
			{
				return _curParams.Peek()[result];
			}
			return new TextObject("Can't find parameter:" + rawValue);
		}
		return TextObject.GetEmpty();
	}

	public TextObject GetFunctionParamWithoutEvaluate(string rawValue)
	{
		if (int.TryParse(rawValue.Substring(1), out var result))
		{
			if (_curParamsWithoutEvaluate.Count > 0 && _curParamsWithoutEvaluate.Peek().Length > result)
			{
				return _curParamsWithoutEvaluate.Peek()[result];
			}
			return new TextObject("Can't find parameter:" + rawValue);
		}
		return TextObject.GetEmpty();
	}

	internal void ClearAll()
	{
		_variables.Clear();
	}
}
