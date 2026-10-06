using System;
using Newtonsoft.Json;

namespace TaleWorlds.Diamond;

[Serializable]
[JsonConverter(typeof(MessageJsonConverter))]
public abstract class Message
{
}
