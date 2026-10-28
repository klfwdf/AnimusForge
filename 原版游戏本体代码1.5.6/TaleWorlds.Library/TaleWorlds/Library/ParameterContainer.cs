using System;
using System.Collections.Generic;
using System.Globalization;

namespace TaleWorlds.Library;

public class ParameterContainer
{
	private Dictionary<string, string> _parameters;

	public IEnumerable<KeyValuePair<string, string>> Iterator => _parameters;

	public ParameterContainer()
	{
		_parameters = new Dictionary<string, string>();
	}

	public void AddParameter(string key, string value, bool overwriteIfExists)
	{
		if (_parameters.ContainsKey(key))
		{
			if (overwriteIfExists)
			{
				_parameters[key] = value;
			}
		}
		else
		{
			_parameters.Add(key, value);
		}
	}

	public void AddParameterConcurrent(string key, string value, bool overwriteIfExists)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>(_parameters);
		if (dictionary.ContainsKey(key))
		{
			if (overwriteIfExists)
			{
				dictionary[key] = value;
			}
		}
		else
		{
			dictionary.Add(key, value);
		}
		_parameters = dictionary;
	}

	public void AddParametersConcurrent(IEnumerable<KeyValuePair<string, string>> parameters, bool overwriteIfExists)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>(_parameters);
		foreach (KeyValuePair<string, string> parameter in parameters)
		{
			if (dictionary.ContainsKey(parameter.Key))
			{
				if (overwriteIfExists)
				{
					dictionary[parameter.Key] = parameter.Value;
				}
			}
			else
			{
				dictionary.Add(parameter.Key, parameter.Value);
			}
		}
		_parameters = dictionary;
	}

	public void ClearParameters()
	{
		_parameters = new Dictionary<string, string>();
	}

	public bool TryGetParameter(string key, out string outValue)
	{
		return _parameters.TryGetValue(key, out outValue);
	}

	public bool TryGetParameterAsBool(string key, out bool outValue)
	{
		outValue = false;
		if (TryGetParameter(key, out var outValue2))
		{
			outValue = outValue2 == "true" || outValue2 == "True";
			return true;
		}
		return false;
	}

	public bool TryGetParameterAsInt(string key, out int outValue)
	{
		outValue = 0;
		if (TryGetParameter(key, out var outValue2))
		{
			outValue = Convert.ToInt32(outValue2);
			return true;
		}
		return false;
	}

	public bool TryGetParameterAsUInt16(string key, out ushort outValue)
	{
		outValue = 0;
		if (TryGetParameter(key, out var outValue2))
		{
			outValue = Convert.ToUInt16(outValue2);
			return true;
		}
		return false;
	}

	public bool TryGetParameterAsFloat(string key, out float outValue)
	{
		outValue = 0f;
		if (TryGetParameter(key, out var outValue2))
		{
			outValue = Convert.ToSingle(outValue2, CultureInfo.InvariantCulture);
			return true;
		}
		return false;
	}

	public bool TryGetParameterAsByte(string key, out byte outValue)
	{
		outValue = 0;
		if (TryGetParameter(key, out var outValue2))
		{
			outValue = Convert.ToByte(outValue2);
			return true;
		}
		return false;
	}

	public bool TryGetParameterAsSByte(string key, out sbyte outValue)
	{
		outValue = 0;
		if (TryGetParameter(key, out var outValue2))
		{
			outValue = Convert.ToSByte(outValue2);
			return true;
		}
		return false;
	}

	public bool TryGetParameterAsVec3(string key, out Vec3 outValue)
	{
		outValue = default(Vec3);
		if (TryGetParameter(key, out var outValue2))
		{
			string[] array = outValue2.Split(new char[1] { ';' });
			float x = Convert.ToSingle(array[0], CultureInfo.InvariantCulture);
			float y = Convert.ToSingle(array[1], CultureInfo.InvariantCulture);
			float z = Convert.ToSingle(array[2], CultureInfo.InvariantCulture);
			outValue = new Vec3(x, y, z);
			return true;
		}
		return false;
	}

	public bool TryGetParameterAsVec2(string key, out Vec2 outValue)
	{
		outValue = default(Vec2);
		if (TryGetParameter(key, out var outValue2))
		{
			string[] array = outValue2.Split(new char[1] { ';' });
			float a = Convert.ToSingle(array[0], CultureInfo.InvariantCulture);
			float b = Convert.ToSingle(array[1], CultureInfo.InvariantCulture);
			outValue = new Vec2(a, b);
			return true;
		}
		return false;
	}

	public string GetParameter(string key)
	{
		return _parameters[key];
	}

	public ParameterContainer Clone()
	{
		ParameterContainer parameterContainer = new ParameterContainer();
		foreach (KeyValuePair<string, string> parameter in _parameters)
		{
			parameterContainer._parameters.Add(parameter.Key, parameter.Value);
		}
		return parameterContainer;
	}
}
