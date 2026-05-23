using System;
using System.Collections.Generic;
using System.Globalization;

namespace TaleWorlds.Library
{
	// Token: 0x02000076 RID: 118
	public class ParameterContainer
	{
		// Token: 0x06000432 RID: 1074 RVA: 0x0000ED4E File Offset: 0x0000CF4E
		public ParameterContainer()
		{
			this._parameters = new Dictionary<string, string>();
		}

		// Token: 0x06000433 RID: 1075 RVA: 0x0000ED61 File Offset: 0x0000CF61
		public void AddParameter(string key, string value, bool overwriteIfExists)
		{
			if (this._parameters.ContainsKey(key))
			{
				if (overwriteIfExists)
				{
					this._parameters[key] = value;
					return;
				}
			}
			else
			{
				this._parameters.Add(key, value);
			}
		}

		// Token: 0x06000434 RID: 1076 RVA: 0x0000ED90 File Offset: 0x0000CF90
		public void AddParameterConcurrent(string key, string value, bool overwriteIfExists)
		{
			Dictionary<string, string> dictionary = new Dictionary<string, string>(this._parameters);
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
			this._parameters = dictionary;
		}

		// Token: 0x06000435 RID: 1077 RVA: 0x0000EDD0 File Offset: 0x0000CFD0
		public void AddParametersConcurrent(IEnumerable<KeyValuePair<string, string>> parameters, bool overwriteIfExists)
		{
			Dictionary<string, string> dictionary = new Dictionary<string, string>(this._parameters);
			foreach (KeyValuePair<string, string> keyValuePair in parameters)
			{
				if (dictionary.ContainsKey(keyValuePair.Key))
				{
					if (overwriteIfExists)
					{
						dictionary[keyValuePair.Key] = keyValuePair.Value;
					}
				}
				else
				{
					dictionary.Add(keyValuePair.Key, keyValuePair.Value);
				}
			}
			this._parameters = dictionary;
		}

		// Token: 0x06000436 RID: 1078 RVA: 0x0000EE60 File Offset: 0x0000D060
		public void ClearParameters()
		{
			this._parameters = new Dictionary<string, string>();
		}

		// Token: 0x06000437 RID: 1079 RVA: 0x0000EE6D File Offset: 0x0000D06D
		public bool TryGetParameter(string key, out string outValue)
		{
			return this._parameters.TryGetValue(key, out outValue);
		}

		// Token: 0x06000438 RID: 1080 RVA: 0x0000EE7C File Offset: 0x0000D07C
		public bool TryGetParameterAsBool(string key, out bool outValue)
		{
			outValue = false;
			string a;
			if (this.TryGetParameter(key, out a))
			{
				outValue = a == "true" || a == "True";
				return true;
			}
			return false;
		}

		// Token: 0x06000439 RID: 1081 RVA: 0x0000EEB8 File Offset: 0x0000D0B8
		public bool TryGetParameterAsInt(string key, out int outValue)
		{
			outValue = 0;
			string value;
			if (this.TryGetParameter(key, out value))
			{
				outValue = Convert.ToInt32(value);
				return true;
			}
			return false;
		}

		// Token: 0x0600043A RID: 1082 RVA: 0x0000EEE0 File Offset: 0x0000D0E0
		public bool TryGetParameterAsUInt16(string key, out ushort outValue)
		{
			outValue = 0;
			string value;
			if (this.TryGetParameter(key, out value))
			{
				outValue = Convert.ToUInt16(value);
				return true;
			}
			return false;
		}

		// Token: 0x0600043B RID: 1083 RVA: 0x0000EF08 File Offset: 0x0000D108
		public bool TryGetParameterAsFloat(string key, out float outValue)
		{
			outValue = 0f;
			string value;
			if (this.TryGetParameter(key, out value))
			{
				outValue = Convert.ToSingle(value, CultureInfo.InvariantCulture);
				return true;
			}
			return false;
		}

		// Token: 0x0600043C RID: 1084 RVA: 0x0000EF38 File Offset: 0x0000D138
		public bool TryGetParameterAsByte(string key, out byte outValue)
		{
			outValue = 0;
			string value;
			if (this.TryGetParameter(key, out value))
			{
				outValue = Convert.ToByte(value);
				return true;
			}
			return false;
		}

		// Token: 0x0600043D RID: 1085 RVA: 0x0000EF60 File Offset: 0x0000D160
		public bool TryGetParameterAsSByte(string key, out sbyte outValue)
		{
			outValue = 0;
			string value;
			if (this.TryGetParameter(key, out value))
			{
				outValue = Convert.ToSByte(value);
				return true;
			}
			return false;
		}

		// Token: 0x0600043E RID: 1086 RVA: 0x0000EF88 File Offset: 0x0000D188
		public bool TryGetParameterAsVec3(string key, out Vec3 outValue)
		{
			outValue = default(Vec3);
			string text;
			if (this.TryGetParameter(key, out text))
			{
				string[] array = text.Split(new char[] { ';' });
				float x = Convert.ToSingle(array[0], CultureInfo.InvariantCulture);
				float y = Convert.ToSingle(array[1], CultureInfo.InvariantCulture);
				float z = Convert.ToSingle(array[2], CultureInfo.InvariantCulture);
				outValue = new Vec3(x, y, z, -1f);
				return true;
			}
			return false;
		}

		// Token: 0x0600043F RID: 1087 RVA: 0x0000EFF8 File Offset: 0x0000D1F8
		public bool TryGetParameterAsVec2(string key, out Vec2 outValue)
		{
			outValue = default(Vec2);
			string text;
			if (this.TryGetParameter(key, out text))
			{
				string[] array = text.Split(new char[] { ';' });
				float a = Convert.ToSingle(array[0], CultureInfo.InvariantCulture);
				float b = Convert.ToSingle(array[1], CultureInfo.InvariantCulture);
				outValue = new Vec2(a, b);
				return true;
			}
			return false;
		}

		// Token: 0x06000440 RID: 1088 RVA: 0x0000F053 File Offset: 0x0000D253
		public string GetParameter(string key)
		{
			return this._parameters[key];
		}

		// Token: 0x1700006B RID: 107
		// (get) Token: 0x06000441 RID: 1089 RVA: 0x0000F061 File Offset: 0x0000D261
		public IEnumerable<KeyValuePair<string, string>> Iterator
		{
			get
			{
				return this._parameters;
			}
		}

		// Token: 0x06000442 RID: 1090 RVA: 0x0000F06C File Offset: 0x0000D26C
		public ParameterContainer Clone()
		{
			ParameterContainer parameterContainer = new ParameterContainer();
			foreach (KeyValuePair<string, string> keyValuePair in this._parameters)
			{
				parameterContainer._parameters.Add(keyValuePair.Key, keyValuePair.Value);
			}
			return parameterContainer;
		}

		// Token: 0x0400014D RID: 333
		private Dictionary<string, string> _parameters;
	}
}
