using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Resources;
using System.Text;
using Microsoft.Reflection;

namespace System.Diagnostics.Tracing
{
	// Token: 0x02000431 RID: 1073
	internal class ManifestBuilder
	{
		// Token: 0x06003570 RID: 13680 RVA: 0x000CF228 File Offset: 0x000CD428
		public ManifestBuilder(string providerName, Guid providerGuid, string dllName, ResourceManager resources, EventManifestOptions flags)
		{
			this.providerName = providerName;
			this.flags = flags;
			this.resources = resources;
			this.sb = new StringBuilder();
			this.events = new StringBuilder();
			this.templates = new StringBuilder();
			this.opcodeTab = new Dictionary<int, string>();
			this.stringTab = new Dictionary<string, string>();
			this.errors = new List<string>();
			this.perEventByteArrayArgIndices = new Dictionary<string, List<int>>();
			this.sb.AppendLine("<instrumentationManifest xmlns=\"http://schemas.microsoft.com/win/2004/08/events\">");
			this.sb.AppendLine(" <instrumentation xmlns:xs=\"http://www.w3.org/2001/XMLSchema\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:win=\"http://manifests.microsoft.com/win/2004/08/windows/events\">");
			this.sb.AppendLine("  <events xmlns=\"http://schemas.microsoft.com/win/2004/08/events\">");
			this.sb.Append("<provider name=\"").Append(providerName).Append("\" guid=\"{")
				.Append(providerGuid.ToString())
				.Append("}");
			if (dllName != null)
			{
				this.sb.Append("\" resourceFileName=\"").Append(dllName).Append("\" messageFileName=\"")
					.Append(dllName);
			}
			string value = providerName.Replace("-", "").Replace(".", "_");
			this.sb.Append("\" symbol=\"").Append(value);
			this.sb.Append("\">").AppendLine();
		}

		// Token: 0x06003571 RID: 13681 RVA: 0x000CF398 File Offset: 0x000CD598
		public void AddOpcode(string name, int value)
		{
			if ((this.flags & EventManifestOptions.Strict) != EventManifestOptions.None)
			{
				if (value <= 10 || value >= 239)
				{
					this.ManifestError(Environment.GetResourceString("EventSource_IllegalOpcodeValue", new object[] { name, value }), false);
				}
				string text;
				if (this.opcodeTab.TryGetValue(value, out text) && !name.Equals(text, StringComparison.Ordinal))
				{
					this.ManifestError(Environment.GetResourceString("EventSource_OpcodeCollision", new object[] { name, text, value }), false);
				}
			}
			this.opcodeTab[value] = name;
		}

		// Token: 0x06003572 RID: 13682 RVA: 0x000CF430 File Offset: 0x000CD630
		public void AddTask(string name, int value)
		{
			if ((this.flags & EventManifestOptions.Strict) != EventManifestOptions.None)
			{
				if (value <= 0 || value >= 65535)
				{
					this.ManifestError(Environment.GetResourceString("EventSource_IllegalTaskValue", new object[] { name, value }), false);
				}
				string text;
				if (this.taskTab != null && this.taskTab.TryGetValue(value, out text) && !name.Equals(text, StringComparison.Ordinal))
				{
					this.ManifestError(Environment.GetResourceString("EventSource_TaskCollision", new object[] { name, text, value }), false);
				}
			}
			if (this.taskTab == null)
			{
				this.taskTab = new Dictionary<int, string>();
			}
			this.taskTab[value] = name;
		}

		// Token: 0x06003573 RID: 13683 RVA: 0x000CF4E4 File Offset: 0x000CD6E4
		public void AddKeyword(string name, ulong value)
		{
			if ((value & (value - 1UL)) != 0UL)
			{
				this.ManifestError(Environment.GetResourceString("EventSource_KeywordNeedPowerOfTwo", new object[]
				{
					"0x" + value.ToString("x", CultureInfo.CurrentCulture),
					name
				}), true);
			}
			if ((this.flags & EventManifestOptions.Strict) != EventManifestOptions.None)
			{
				if (value >= 17592186044416UL && !name.StartsWith("Session", StringComparison.Ordinal))
				{
					this.ManifestError(Environment.GetResourceString("EventSource_IllegalKeywordsValue", new object[]
					{
						name,
						"0x" + value.ToString("x", CultureInfo.CurrentCulture)
					}), false);
				}
				string text;
				if (this.keywordTab != null && this.keywordTab.TryGetValue(value, out text) && !name.Equals(text, StringComparison.Ordinal))
				{
					this.ManifestError(Environment.GetResourceString("EventSource_KeywordCollision", new object[]
					{
						name,
						text,
						"0x" + value.ToString("x", CultureInfo.CurrentCulture)
					}), false);
				}
			}
			if (this.keywordTab == null)
			{
				this.keywordTab = new Dictionary<ulong, string>();
			}
			this.keywordTab[value] = name;
		}

		// Token: 0x06003574 RID: 13684 RVA: 0x000CF614 File Offset: 0x000CD814
		public void AddChannel(string name, int value, EventChannelAttribute channelAttribute)
		{
			EventChannel eventChannel = (EventChannel)value;
			if (value < 16 || value > 255)
			{
				this.ManifestError(Environment.GetResourceString("EventSource_EventChannelOutOfRange", new object[] { name, value }), false);
			}
			else if (eventChannel >= EventChannel.Admin && eventChannel <= EventChannel.Debug && channelAttribute != null && this.EventChannelToChannelType(eventChannel) != channelAttribute.EventChannelType)
			{
				this.ManifestError(Environment.GetResourceString("EventSource_ChannelTypeDoesNotMatchEventChannelValue", new object[]
				{
					name,
					((EventChannel)value).ToString()
				}), false);
			}
			ulong channelKeyword = this.GetChannelKeyword(eventChannel);
			if (this.channelTab == null)
			{
				this.channelTab = new Dictionary<int, ManifestBuilder.ChannelInfo>(4);
			}
			this.channelTab[value] = new ManifestBuilder.ChannelInfo
			{
				Name = name,
				Keywords = channelKeyword,
				Attribs = channelAttribute
			};
		}

		// Token: 0x06003575 RID: 13685 RVA: 0x000CF6E3 File Offset: 0x000CD8E3
		private EventChannelType EventChannelToChannelType(EventChannel channel)
		{
			return (EventChannelType)(channel - 16 + 1);
		}

		// Token: 0x06003576 RID: 13686 RVA: 0x000CF6EC File Offset: 0x000CD8EC
		private EventChannelAttribute GetDefaultChannelAttribute(EventChannel channel)
		{
			EventChannelAttribute eventChannelAttribute = new EventChannelAttribute();
			eventChannelAttribute.EventChannelType = this.EventChannelToChannelType(channel);
			if (eventChannelAttribute.EventChannelType <= EventChannelType.Operational)
			{
				eventChannelAttribute.Enabled = true;
			}
			return eventChannelAttribute;
		}

		// Token: 0x06003577 RID: 13687 RVA: 0x000CF720 File Offset: 0x000CD920
		public ulong[] GetChannelData()
		{
			if (this.channelTab == null)
			{
				return new ulong[0];
			}
			int num = -1;
			foreach (int num2 in this.channelTab.Keys)
			{
				if (num2 > num)
				{
					num = num2;
				}
			}
			ulong[] array = new ulong[num + 1];
			foreach (KeyValuePair<int, ManifestBuilder.ChannelInfo> keyValuePair in this.channelTab)
			{
				array[keyValuePair.Key] = keyValuePair.Value.Keywords;
			}
			return array;
		}

		// Token: 0x06003578 RID: 13688 RVA: 0x000CF7E4 File Offset: 0x000CD9E4
		public void StartEvent(string eventName, EventAttribute eventAttribute)
		{
			this.eventName = eventName;
			this.numParams = 0;
			this.byteArrArgIndices = null;
			this.events.Append("  <event").Append(" value=\"").Append(eventAttribute.EventId)
				.Append("\"")
				.Append(" version=\"")
				.Append(eventAttribute.Version)
				.Append("\"")
				.Append(" level=\"")
				.Append(ManifestBuilder.GetLevelName(eventAttribute.Level))
				.Append("\"")
				.Append(" symbol=\"")
				.Append(eventName)
				.Append("\"");
			this.WriteMessageAttrib(this.events, "event", eventName, eventAttribute.Message);
			if (eventAttribute.Keywords != EventKeywords.None)
			{
				this.events.Append(" keywords=\"").Append(this.GetKeywords((ulong)eventAttribute.Keywords, eventName)).Append("\"");
			}
			if (eventAttribute.Opcode != EventOpcode.Info)
			{
				this.events.Append(" opcode=\"").Append(this.GetOpcodeName(eventAttribute.Opcode, eventName)).Append("\"");
			}
			if (eventAttribute.Task != EventTask.None)
			{
				this.events.Append(" task=\"").Append(this.GetTaskName(eventAttribute.Task, eventName)).Append("\"");
			}
			if (eventAttribute.Channel != EventChannel.None)
			{
				this.events.Append(" channel=\"").Append(this.GetChannelName(eventAttribute.Channel, eventName, eventAttribute.Message)).Append("\"");
			}
		}

		// Token: 0x06003579 RID: 13689 RVA: 0x000CF988 File Offset: 0x000CDB88
		public void AddEventParameter(Type type, string name)
		{
			if (this.numParams == 0)
			{
				this.templates.Append("  <template tid=\"").Append(this.eventName).Append("Args\">")
					.AppendLine();
			}
			if (type == typeof(byte[]))
			{
				if (this.byteArrArgIndices == null)
				{
					this.byteArrArgIndices = new List<int>(4);
				}
				this.byteArrArgIndices.Add(this.numParams);
				this.numParams++;
				this.templates.Append("   <data name=\"").Append(name).Append("Size\" inType=\"win:UInt32\"/>")
					.AppendLine();
			}
			this.numParams++;
			this.templates.Append("   <data name=\"").Append(name).Append("\" inType=\"")
				.Append(this.GetTypeName(type))
				.Append("\"");
			if ((type.IsArray || type.IsPointer) && type.GetElementType() == typeof(byte))
			{
				this.templates.Append(" length=\"").Append(name).Append("Size\"");
			}
			if (type.IsEnum() && Enum.GetUnderlyingType(type) != typeof(ulong) && Enum.GetUnderlyingType(type) != typeof(long))
			{
				this.templates.Append(" map=\"").Append(type.Name).Append("\"");
				if (this.mapsTab == null)
				{
					this.mapsTab = new Dictionary<string, Type>();
				}
				if (!this.mapsTab.ContainsKey(type.Name))
				{
					this.mapsTab.Add(type.Name, type);
				}
			}
			this.templates.Append("/>").AppendLine();
		}

		// Token: 0x0600357A RID: 13690 RVA: 0x000CFB70 File Offset: 0x000CDD70
		public void EndEvent()
		{
			if (this.numParams > 0)
			{
				this.templates.Append("  </template>").AppendLine();
				this.events.Append(" template=\"").Append(this.eventName).Append("Args\"");
			}
			this.events.Append("/>").AppendLine();
			if (this.byteArrArgIndices != null)
			{
				this.perEventByteArrayArgIndices[this.eventName] = this.byteArrArgIndices;
			}
			string text;
			if (this.stringTab.TryGetValue("event_" + this.eventName, out text))
			{
				text = this.TranslateToManifestConvention(text, this.eventName);
				this.stringTab["event_" + this.eventName] = text;
			}
			this.eventName = null;
			this.numParams = 0;
			this.byteArrArgIndices = null;
		}

		// Token: 0x0600357B RID: 13691 RVA: 0x000CFC58 File Offset: 0x000CDE58
		public ulong GetChannelKeyword(EventChannel channel)
		{
			if (this.channelTab == null)
			{
				this.channelTab = new Dictionary<int, ManifestBuilder.ChannelInfo>(4);
			}
			if (this.channelTab.Count == 8)
			{
				this.ManifestError(Environment.GetResourceString("EventSource_MaxChannelExceeded"), false);
			}
			ManifestBuilder.ChannelInfo channelInfo;
			ulong keywords;
			if (!this.channelTab.TryGetValue((int)channel, out channelInfo))
			{
				keywords = this.nextChannelKeywordBit;
				this.nextChannelKeywordBit >>= 1;
			}
			else
			{
				keywords = channelInfo.Keywords;
			}
			return keywords;
		}

		// Token: 0x0600357C RID: 13692 RVA: 0x000CFCC8 File Offset: 0x000CDEC8
		public byte[] CreateManifest()
		{
			string s = this.CreateManifestString();
			return Encoding.UTF8.GetBytes(s);
		}

		// Token: 0x170007F4 RID: 2036
		// (get) Token: 0x0600357D RID: 13693 RVA: 0x000CFCE7 File Offset: 0x000CDEE7
		public IList<string> Errors
		{
			get
			{
				return this.errors;
			}
		}

		// Token: 0x0600357E RID: 13694 RVA: 0x000CFCEF File Offset: 0x000CDEEF
		public void ManifestError(string msg, bool runtimeCritical = false)
		{
			if ((this.flags & EventManifestOptions.Strict) != EventManifestOptions.None)
			{
				this.errors.Add(msg);
				return;
			}
			if (runtimeCritical)
			{
				throw new ArgumentException(msg);
			}
		}

		// Token: 0x0600357F RID: 13695 RVA: 0x000CFD14 File Offset: 0x000CDF14
		private string CreateManifestString()
		{
			if (this.channelTab != null)
			{
				this.sb.Append(" <channels>").AppendLine();
				List<KeyValuePair<int, ManifestBuilder.ChannelInfo>> list = new List<KeyValuePair<int, ManifestBuilder.ChannelInfo>>();
				foreach (KeyValuePair<int, ManifestBuilder.ChannelInfo> item in this.channelTab)
				{
					list.Add(item);
				}
				list.Sort((KeyValuePair<int, ManifestBuilder.ChannelInfo> p1, KeyValuePair<int, ManifestBuilder.ChannelInfo> p2) => -Comparer<ulong>.Default.Compare(p1.Value.Keywords, p2.Value.Keywords));
				foreach (KeyValuePair<int, ManifestBuilder.ChannelInfo> keyValuePair in list)
				{
					int key = keyValuePair.Key;
					ManifestBuilder.ChannelInfo value = keyValuePair.Value;
					string text = null;
					string text2 = "channel";
					bool flag = false;
					string text3 = null;
					if (value.Attribs != null)
					{
						EventChannelAttribute attribs = value.Attribs;
						if (Enum.IsDefined(typeof(EventChannelType), attribs.EventChannelType))
						{
							text = attribs.EventChannelType.ToString();
						}
						flag = attribs.Enabled;
					}
					if (text3 == null)
					{
						text3 = this.providerName + "/" + value.Name;
					}
					this.sb.Append("  <").Append(text2);
					this.sb.Append(" chid=\"").Append(value.Name).Append("\"");
					this.sb.Append(" name=\"").Append(text3).Append("\"");
					if (text2 == "channel")
					{
						this.WriteMessageAttrib(this.sb, "channel", value.Name, null);
						this.sb.Append(" value=\"").Append(key).Append("\"");
						if (text != null)
						{
							this.sb.Append(" type=\"").Append(text).Append("\"");
						}
						this.sb.Append(" enabled=\"").Append(flag.ToString().ToLower()).Append("\"");
					}
					this.sb.Append("/>").AppendLine();
				}
				this.sb.Append(" </channels>").AppendLine();
			}
			if (this.taskTab != null)
			{
				this.sb.Append(" <tasks>").AppendLine();
				List<int> list2 = new List<int>(this.taskTab.Keys);
				list2.Sort();
				foreach (int num in list2)
				{
					this.sb.Append("  <task");
					this.WriteNameAndMessageAttribs(this.sb, "task", this.taskTab[num]);
					this.sb.Append(" value=\"").Append(num).Append("\"/>")
						.AppendLine();
				}
				this.sb.Append(" </tasks>").AppendLine();
			}
			if (this.mapsTab != null)
			{
				this.sb.Append(" <maps>").AppendLine();
				foreach (Type type in this.mapsTab.Values)
				{
					bool flag2 = EventSource.GetCustomAttributeHelper(type, typeof(FlagsAttribute), this.flags) != null;
					string value2 = (flag2 ? "bitMap" : "valueMap");
					this.sb.Append("  <").Append(value2).Append(" name=\"")
						.Append(type.Name)
						.Append("\">")
						.AppendLine();
					FieldInfo[] fields = type.GetFields(BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public);
					foreach (FieldInfo fieldInfo in fields)
					{
						object rawConstantValue = fieldInfo.GetRawConstantValue();
						if (rawConstantValue != null)
						{
							long num2;
							if (rawConstantValue is int)
							{
								num2 = (long)((int)rawConstantValue);
							}
							else
							{
								if (!(rawConstantValue is long))
								{
									goto IL_4D1;
								}
								num2 = (long)rawConstantValue;
							}
							if (!flag2 || ((num2 & (num2 - 1L)) == 0L && num2 != 0L))
							{
								this.sb.Append("   <map value=\"0x").Append(num2.ToString("x", CultureInfo.InvariantCulture)).Append("\"");
								this.WriteMessageAttrib(this.sb, "map", type.Name + "." + fieldInfo.Name, fieldInfo.Name);
								this.sb.Append("/>").AppendLine();
							}
						}
						IL_4D1:;
					}
					this.sb.Append("  </").Append(value2).Append(">")
						.AppendLine();
				}
				this.sb.Append(" </maps>").AppendLine();
			}
			this.sb.Append(" <opcodes>").AppendLine();
			List<int> list3 = new List<int>(this.opcodeTab.Keys);
			list3.Sort();
			foreach (int num3 in list3)
			{
				this.sb.Append("  <opcode");
				this.WriteNameAndMessageAttribs(this.sb, "opcode", this.opcodeTab[num3]);
				this.sb.Append(" value=\"").Append(num3).Append("\"/>")
					.AppendLine();
			}
			this.sb.Append(" </opcodes>").AppendLine();
			if (this.keywordTab != null)
			{
				this.sb.Append(" <keywords>").AppendLine();
				List<ulong> list4 = new List<ulong>(this.keywordTab.Keys);
				list4.Sort();
				foreach (ulong key2 in list4)
				{
					this.sb.Append("  <keyword");
					this.WriteNameAndMessageAttribs(this.sb, "keyword", this.keywordTab[key2]);
					this.sb.Append(" mask=\"0x").Append(key2.ToString("x", CultureInfo.InvariantCulture)).Append("\"/>")
						.AppendLine();
				}
				this.sb.Append(" </keywords>").AppendLine();
			}
			this.sb.Append(" <events>").AppendLine();
			this.sb.Append(this.events);
			this.sb.Append(" </events>").AppendLine();
			this.sb.Append(" <templates>").AppendLine();
			if (this.templates.Length > 0)
			{
				this.sb.Append(this.templates);
			}
			else
			{
				this.sb.Append("    <template tid=\"_empty\"></template>").AppendLine();
			}
			this.sb.Append(" </templates>").AppendLine();
			this.sb.Append("</provider>").AppendLine();
			this.sb.Append("</events>").AppendLine();
			this.sb.Append("</instrumentation>").AppendLine();
			this.sb.Append("<localization>").AppendLine();
			List<CultureInfo> list5;
			if (this.resources != null && (this.flags & EventManifestOptions.AllCultures) != EventManifestOptions.None)
			{
				list5 = ManifestBuilder.GetSupportedCultures(this.resources);
			}
			else
			{
				list5 = new List<CultureInfo>();
				list5.Add(CultureInfo.CurrentUICulture);
			}
			string[] array2 = new string[this.stringTab.Keys.Count];
			this.stringTab.Keys.CopyTo(array2, 0);
			ArraySortHelper<string>.IntrospectiveSort(array2, 0, array2.Length, Comparer<string>.Default);
			foreach (CultureInfo cultureInfo in list5)
			{
				this.sb.Append(" <resources culture=\"").Append(cultureInfo.Name).Append("\">")
					.AppendLine();
				this.sb.Append("  <stringTable>").AppendLine();
				foreach (string text4 in array2)
				{
					string localizedMessage = this.GetLocalizedMessage(text4, cultureInfo, true);
					this.sb.Append("   <string id=\"").Append(text4).Append("\" value=\"")
						.Append(localizedMessage)
						.Append("\"/>")
						.AppendLine();
				}
				this.sb.Append("  </stringTable>").AppendLine();
				this.sb.Append(" </resources>").AppendLine();
			}
			this.sb.Append("</localization>").AppendLine();
			this.sb.AppendLine("</instrumentationManifest>");
			return this.sb.ToString();
		}

		// Token: 0x06003580 RID: 13696 RVA: 0x000D0748 File Offset: 0x000CE948
		private void WriteNameAndMessageAttribs(StringBuilder stringBuilder, string elementName, string name)
		{
			stringBuilder.Append(" name=\"").Append(name).Append("\"");
			this.WriteMessageAttrib(this.sb, elementName, name, name);
		}

		// Token: 0x06003581 RID: 13697 RVA: 0x000D0778 File Offset: 0x000CE978
		private void WriteMessageAttrib(StringBuilder stringBuilder, string elementName, string name, string value)
		{
			string text = elementName + "_" + name;
			if (this.resources != null)
			{
				string @string = this.resources.GetString(text, CultureInfo.InvariantCulture);
				if (@string != null)
				{
					value = @string;
				}
			}
			if (value == null)
			{
				return;
			}
			stringBuilder.Append(" message=\"$(string.").Append(text).Append(")\"");
			string text2;
			if (this.stringTab.TryGetValue(text, out text2) && !text2.Equals(value))
			{
				this.ManifestError(Environment.GetResourceString("EventSource_DuplicateStringKey", new object[] { text }), true);
				return;
			}
			this.stringTab[text] = value;
		}

		// Token: 0x06003582 RID: 13698 RVA: 0x000D0818 File Offset: 0x000CEA18
		internal string GetLocalizedMessage(string key, CultureInfo ci, bool etwFormat)
		{
			string text = null;
			if (this.resources != null)
			{
				string @string = this.resources.GetString(key, ci);
				if (@string != null)
				{
					text = @string;
					if (etwFormat && key.StartsWith("event_"))
					{
						string evtName = key.Substring("event_".Length);
						text = this.TranslateToManifestConvention(text, evtName);
					}
				}
			}
			if (etwFormat && text == null)
			{
				this.stringTab.TryGetValue(key, out text);
			}
			return text;
		}

		// Token: 0x06003583 RID: 13699 RVA: 0x000D0884 File Offset: 0x000CEA84
		private static List<CultureInfo> GetSupportedCultures(ResourceManager resources)
		{
			List<CultureInfo> list = new List<CultureInfo>();
			foreach (CultureInfo cultureInfo in CultureInfo.GetCultures(CultureTypes.SpecificCultures))
			{
				if (resources.GetResourceSet(cultureInfo, true, false) != null)
				{
					list.Add(cultureInfo);
				}
			}
			if (!list.Contains(CultureInfo.CurrentUICulture))
			{
				list.Insert(0, CultureInfo.CurrentUICulture);
			}
			return list;
		}

		// Token: 0x06003584 RID: 13700 RVA: 0x000D08DC File Offset: 0x000CEADC
		private static string GetLevelName(EventLevel level)
		{
			return ((level >= (EventLevel)16) ? "" : "win:") + level.ToString();
		}

		// Token: 0x06003585 RID: 13701 RVA: 0x000D0904 File Offset: 0x000CEB04
		private string GetChannelName(EventChannel channel, string eventName, string eventMessage)
		{
			ManifestBuilder.ChannelInfo channelInfo = null;
			if (this.channelTab == null || !this.channelTab.TryGetValue((int)channel, out channelInfo))
			{
				if (channel < EventChannel.Admin)
				{
					this.ManifestError(Environment.GetResourceString("EventSource_UndefinedChannel", new object[] { channel, eventName }), false);
				}
				if (this.channelTab == null)
				{
					this.channelTab = new Dictionary<int, ManifestBuilder.ChannelInfo>(4);
				}
				string text = channel.ToString();
				if (EventChannel.Debug < channel)
				{
					text = "Channel" + text;
				}
				this.AddChannel(text, (int)channel, this.GetDefaultChannelAttribute(channel));
				if (!this.channelTab.TryGetValue((int)channel, out channelInfo))
				{
					this.ManifestError(Environment.GetResourceString("EventSource_UndefinedChannel", new object[] { channel, eventName }), false);
				}
			}
			if (this.resources != null && eventMessage == null)
			{
				eventMessage = this.resources.GetString("event_" + eventName, CultureInfo.InvariantCulture);
			}
			if (channelInfo.Attribs.EventChannelType == EventChannelType.Admin && eventMessage == null)
			{
				this.ManifestError(Environment.GetResourceString("EventSource_EventWithAdminChannelMustHaveMessage", new object[] { eventName, channelInfo.Name }), false);
			}
			return channelInfo.Name;
		}

		// Token: 0x06003586 RID: 13702 RVA: 0x000D0A30 File Offset: 0x000CEC30
		private string GetTaskName(EventTask task, string eventName)
		{
			if (task == EventTask.None)
			{
				return "";
			}
			if (this.taskTab == null)
			{
				this.taskTab = new Dictionary<int, string>();
			}
			string result;
			if (!this.taskTab.TryGetValue((int)task, out result))
			{
				this.taskTab[(int)task] = eventName;
				result = eventName;
			}
			return result;
		}

		// Token: 0x06003587 RID: 13703 RVA: 0x000D0A7C File Offset: 0x000CEC7C
		private string GetOpcodeName(EventOpcode opcode, string eventName)
		{
			switch (opcode)
			{
			case EventOpcode.Info:
				return "win:Info";
			case EventOpcode.Start:
				return "win:Start";
			case EventOpcode.Stop:
				return "win:Stop";
			case EventOpcode.DataCollectionStart:
				return "win:DC_Start";
			case EventOpcode.DataCollectionStop:
				return "win:DC_Stop";
			case EventOpcode.Extension:
				return "win:Extension";
			case EventOpcode.Reply:
				return "win:Reply";
			case EventOpcode.Resume:
				return "win:Resume";
			case EventOpcode.Suspend:
				return "win:Suspend";
			case EventOpcode.Send:
				return "win:Send";
			default:
				if (opcode != EventOpcode.Receive)
				{
					string result;
					if (this.opcodeTab == null || !this.opcodeTab.TryGetValue((int)opcode, out result))
					{
						this.ManifestError(Environment.GetResourceString("EventSource_UndefinedOpcode", new object[] { opcode, eventName }), true);
						result = null;
					}
					return result;
				}
				return "win:Receive";
			}
		}

		// Token: 0x06003588 RID: 13704 RVA: 0x000D0B44 File Offset: 0x000CED44
		private string GetKeywords(ulong keywords, string eventName)
		{
			string text = "";
			for (ulong num = 1UL; num != 0UL; num <<= 1)
			{
				if ((keywords & num) != 0UL)
				{
					string text2 = null;
					if ((this.keywordTab == null || !this.keywordTab.TryGetValue(num, out text2)) && num >= 281474976710656UL)
					{
						text2 = string.Empty;
					}
					if (text2 == null)
					{
						this.ManifestError(Environment.GetResourceString("EventSource_UndefinedKeyword", new object[]
						{
							"0x" + num.ToString("x", CultureInfo.CurrentCulture),
							eventName
						}), true);
						text2 = string.Empty;
					}
					if (text.Length != 0 && text2.Length != 0)
					{
						text += " ";
					}
					text += text2;
				}
			}
			return text;
		}

		// Token: 0x06003589 RID: 13705 RVA: 0x000D0C04 File Offset: 0x000CEE04
		private string GetTypeName(Type type)
		{
			if (type.IsEnum())
			{
				FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				string typeName = this.GetTypeName(fields[0].FieldType);
				return typeName.Replace("win:Int", "win:UInt");
			}
			switch (type.GetTypeCode())
			{
			case TypeCode.Boolean:
				return "win:Boolean";
			case TypeCode.Char:
			case TypeCode.UInt16:
				return "win:UInt16";
			case TypeCode.SByte:
				return "win:Int8";
			case TypeCode.Byte:
				return "win:UInt8";
			case TypeCode.Int16:
				return "win:Int16";
			case TypeCode.Int32:
				return "win:Int32";
			case TypeCode.UInt32:
				return "win:UInt32";
			case TypeCode.Int64:
				return "win:Int64";
			case TypeCode.UInt64:
				return "win:UInt64";
			case TypeCode.Single:
				return "win:Float";
			case TypeCode.Double:
				return "win:Double";
			case TypeCode.DateTime:
				return "win:FILETIME";
			case TypeCode.String:
				return "win:UnicodeString";
			}
			if (type == typeof(Guid))
			{
				return "win:GUID";
			}
			if (type == typeof(IntPtr))
			{
				return "win:Pointer";
			}
			if ((type.IsArray || type.IsPointer) && type.GetElementType() == typeof(byte))
			{
				return "win:Binary";
			}
			this.ManifestError(Environment.GetResourceString("EventSource_UnsupportedEventTypeInManifest", new object[] { type.Name }), true);
			return string.Empty;
		}

		// Token: 0x0600358A RID: 13706 RVA: 0x000D0D63 File Offset: 0x000CEF63
		private static void UpdateStringBuilder(ref StringBuilder stringBuilder, string eventMessage, int startIndex, int count)
		{
			if (stringBuilder == null)
			{
				stringBuilder = new StringBuilder();
			}
			stringBuilder.Append(eventMessage, startIndex, count);
		}

		// Token: 0x0600358B RID: 13707 RVA: 0x000D0D7C File Offset: 0x000CEF7C
		private string TranslateToManifestConvention(string eventMessage, string evtName)
		{
			StringBuilder stringBuilder = null;
			int writtenSoFar = 0;
			int i = 0;
			Action<char, string> <>9__0;
			while (i < eventMessage.Length)
			{
				int num2;
				if (eventMessage[i] == '%')
				{
					ManifestBuilder.UpdateStringBuilder(ref stringBuilder, eventMessage, writtenSoFar, i - writtenSoFar);
					stringBuilder.Append("%%");
					int j = i;
					i = j + 1;
					writtenSoFar = i;
				}
				else if (i < eventMessage.Length - 1 && ((eventMessage[i] == '{' && eventMessage[i + 1] == '{') || (eventMessage[i] == '}' && eventMessage[i + 1] == '}')))
				{
					ManifestBuilder.UpdateStringBuilder(ref stringBuilder, eventMessage, writtenSoFar, i - writtenSoFar);
					stringBuilder.Append(eventMessage[i]);
					int j = i;
					i = j + 1;
					j = i;
					i = j + 1;
					writtenSoFar = i;
				}
				else if (eventMessage[i] == '{')
				{
					int i2 = i;
					int j = i;
					i = j + 1;
					int num = 0;
					while (i < eventMessage.Length && char.IsDigit(eventMessage[i]))
					{
						num = num * 10 + (int)eventMessage[i] - 48;
						j = i;
						i = j + 1;
					}
					if (i < eventMessage.Length && eventMessage[i] == '}')
					{
						j = i;
						i = j + 1;
						ManifestBuilder.UpdateStringBuilder(ref stringBuilder, eventMessage, writtenSoFar, i2 - writtenSoFar);
						int value = this.TranslateIndexToManifestConvention(num, evtName);
						stringBuilder.Append('%').Append(value);
						if (i < eventMessage.Length && eventMessage[i] == '!')
						{
							j = i;
							i = j + 1;
							stringBuilder.Append("%!");
						}
						writtenSoFar = i;
					}
					else
					{
						this.ManifestError(Environment.GetResourceString("EventSource_UnsupportedMessageProperty", new object[] { evtName, eventMessage }), false);
					}
				}
				else if ((num2 = "&<>'\"\r\n\t".IndexOf(eventMessage[i])) >= 0)
				{
					string[] array = new string[] { "&amp;", "&lt;", "&gt;", "&apos;", "&quot;", "%r", "%n", "%t" };
					Action<char, string> action;
					if ((action = <>9__0) == null)
					{
						action = (<>9__0 = delegate(char ch, string escape)
						{
							ManifestBuilder.UpdateStringBuilder(ref stringBuilder, eventMessage, writtenSoFar, i - writtenSoFar);
							int i3 = i;
							i = i3 + 1;
							stringBuilder.Append(escape);
							writtenSoFar = i;
						});
					}
					Action<char, string> action2 = action;
					action2(eventMessage[i], array[num2]);
				}
				else
				{
					int j = i;
					i = j + 1;
				}
			}
			if (stringBuilder == null)
			{
				return eventMessage;
			}
			ManifestBuilder.UpdateStringBuilder(ref stringBuilder, eventMessage, writtenSoFar, i - writtenSoFar);
			return stringBuilder.ToString();
		}

		// Token: 0x0600358C RID: 13708 RVA: 0x000D11A0 File Offset: 0x000CF3A0
		private int TranslateIndexToManifestConvention(int idx, string evtName)
		{
			List<int> list;
			if (this.perEventByteArrayArgIndices.TryGetValue(evtName, out list))
			{
				foreach (int num in list)
				{
					if (idx < num)
					{
						break;
					}
					idx++;
				}
			}
			return idx + 1;
		}

		// Token: 0x040017CD RID: 6093
		private Dictionary<int, string> opcodeTab;

		// Token: 0x040017CE RID: 6094
		private Dictionary<int, string> taskTab;

		// Token: 0x040017CF RID: 6095
		private Dictionary<int, ManifestBuilder.ChannelInfo> channelTab;

		// Token: 0x040017D0 RID: 6096
		private Dictionary<ulong, string> keywordTab;

		// Token: 0x040017D1 RID: 6097
		private Dictionary<string, Type> mapsTab;

		// Token: 0x040017D2 RID: 6098
		private Dictionary<string, string> stringTab;

		// Token: 0x040017D3 RID: 6099
		private ulong nextChannelKeywordBit = 9223372036854775808UL;

		// Token: 0x040017D4 RID: 6100
		private const int MaxCountChannels = 8;

		// Token: 0x040017D5 RID: 6101
		private StringBuilder sb;

		// Token: 0x040017D6 RID: 6102
		private StringBuilder events;

		// Token: 0x040017D7 RID: 6103
		private StringBuilder templates;

		// Token: 0x040017D8 RID: 6104
		private string providerName;

		// Token: 0x040017D9 RID: 6105
		private ResourceManager resources;

		// Token: 0x040017DA RID: 6106
		private EventManifestOptions flags;

		// Token: 0x040017DB RID: 6107
		private IList<string> errors;

		// Token: 0x040017DC RID: 6108
		private Dictionary<string, List<int>> perEventByteArrayArgIndices;

		// Token: 0x040017DD RID: 6109
		private string eventName;

		// Token: 0x040017DE RID: 6110
		private int numParams;

		// Token: 0x040017DF RID: 6111
		private List<int> byteArrArgIndices;

		// Token: 0x02000B93 RID: 2963
		private class ChannelInfo
		{
			// Token: 0x04003519 RID: 13593
			public string Name;

			// Token: 0x0400351A RID: 13594
			public ulong Keywords;

			// Token: 0x0400351B RID: 13595
			public EventChannelAttribute Attribs;
		}
	}
}
