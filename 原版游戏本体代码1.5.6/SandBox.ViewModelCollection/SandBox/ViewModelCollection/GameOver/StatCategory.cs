using System.Collections.Generic;

namespace SandBox.ViewModelCollection.GameOver;

public class StatCategory
{
	public readonly IEnumerable<StatItem> Items;

	public readonly string ID;

	public StatCategory(string id, IEnumerable<StatItem> items)
	{
		ID = id;
		Items = items;
	}
}
