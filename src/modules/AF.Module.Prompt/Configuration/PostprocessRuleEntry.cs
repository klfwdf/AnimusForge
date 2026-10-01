namespace AnimusForge;

public class PostprocessRuleEntry
{
	public string Tag { get; set; } = "";

	public string Description { get; set; } = "";

	public string SingleFramedNpcDescription { get; set; } = "";

	[Newtonsoft.Json.JsonIgnore]
	public System.Collections.Generic.HashSet<string> RuntimeAllowedParameterValues { get; set; }
}
