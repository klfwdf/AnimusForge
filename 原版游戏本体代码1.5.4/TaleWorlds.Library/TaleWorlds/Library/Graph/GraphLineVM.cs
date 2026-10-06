namespace TaleWorlds.Library.Graph;

public class GraphLineVM : ViewModel
{
	private MBBindingList<GraphLinePointVM> _points;

	private string _name;

	private string _ID;

	[DataSourceProperty]
	public MBBindingList<GraphLinePointVM> Points
	{
		get
		{
			return _points;
		}
		set
		{
			if (value != _points)
			{
				_points = value;
				OnPropertyChangedWithValue(value, "Points");
			}
		}
	}

	[DataSourceProperty]
	public string Name
	{
		get
		{
			return _name;
		}
		set
		{
			if (value != _name)
			{
				_name = value;
				OnPropertyChangedWithValue(value, "Name");
			}
		}
	}

	[DataSourceProperty]
	public string ID
	{
		get
		{
			return _ID;
		}
		set
		{
			if (value != _ID)
			{
				_ID = value;
				OnPropertyChangedWithValue(value, "ID");
			}
		}
	}

	public GraphLineVM(string ID, string name)
	{
		Points = new MBBindingList<GraphLinePointVM>();
		Name = name;
		this.ID = ID;
	}
}
