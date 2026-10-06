using System;
using System.Collections.Generic;
using System.Linq;

namespace TaleWorlds.Library;

public class BindingPath
{
	private readonly string _path;

	public string Path => _path;

	public string[] Nodes { get; private set; }

	public string FirstNode => Nodes[0];

	public string LastNode
	{
		get
		{
			if (Nodes.Length == 0)
			{
				return "";
			}
			return Nodes[Nodes.Length - 1];
		}
	}

	public BindingPath SubPath
	{
		get
		{
			if (Nodes.Length > 1)
			{
				MBStringBuilder mBStringBuilder = default(MBStringBuilder);
				mBStringBuilder.Initialize(16, "SubPath");
				for (int i = 1; i < Nodes.Length; i++)
				{
					mBStringBuilder.Append(Nodes[i]);
					if (i + 1 < Nodes.Length)
					{
						mBStringBuilder.Append('\\');
					}
				}
				return new BindingPath(mBStringBuilder.ToStringAndRelease());
			}
			return null;
		}
	}

	public BindingPath ParentPath
	{
		get
		{
			if (Nodes.Length > 1)
			{
				string text = "";
				for (int i = 0; i < Nodes.Length - 1; i++)
				{
					text += Nodes[i];
					if (i + 1 < Nodes.Length - 1)
					{
						text += "\\";
					}
				}
				return new BindingPath(text);
			}
			return null;
		}
	}

	private BindingPath(string path, string[] nodes)
	{
		_path = path;
		Nodes = nodes;
	}

	public BindingPath(string path)
	{
		_path = path;
		Nodes = path.Split(new char[1] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
	}

	public BindingPath(int path)
	{
		_path = path.ToString();
		Nodes = new string[1] { _path };
	}

	public static BindingPath CreateFromProperty(string propertyName)
	{
		return new BindingPath(propertyName, new string[1] { propertyName });
	}

	public BindingPath(IEnumerable<string> nodes)
	{
		Nodes = nodes.ToArray();
		_path = "";
		MBStringBuilder mBStringBuilder = default(MBStringBuilder);
		mBStringBuilder.Initialize(16, ".ctor");
		for (int i = 0; i < Nodes.Length; i++)
		{
			string value = Nodes[i];
			mBStringBuilder.Append(value);
			if (i + 1 != Nodes.Length)
			{
				mBStringBuilder.Append('\\');
			}
		}
		_path = mBStringBuilder.ToStringAndRelease();
	}

	private BindingPath(string[] firstNodes, string[] secondNodes)
	{
		Nodes = new string[firstNodes.Length + secondNodes.Length];
		_path = "";
		MBStringBuilder mBStringBuilder = default(MBStringBuilder);
		mBStringBuilder.Initialize(16, ".ctor");
		for (int i = 0; i < firstNodes.Length; i++)
		{
			Nodes[i] = firstNodes[i];
		}
		for (int j = 0; j < secondNodes.Length; j++)
		{
			Nodes[j + firstNodes.Length] = secondNodes[j];
		}
		for (int k = 0; k < Nodes.Length; k++)
		{
			string value = Nodes[k];
			mBStringBuilder.Append(value);
			if (k + 1 != Nodes.Length)
			{
				mBStringBuilder.Append('\\');
			}
		}
		_path = mBStringBuilder.ToStringAndRelease();
	}

	public override int GetHashCode()
	{
		return _path.GetHashCode();
	}

	public override bool Equals(object obj)
	{
		BindingPath bindingPath = obj as BindingPath;
		if (bindingPath == null)
		{
			return false;
		}
		return Path == bindingPath.Path;
	}

	public static bool operator ==(BindingPath a, BindingPath b)
	{
		bool flag = (object)a == null;
		bool flag2 = (object)b == null;
		if (flag && flag2)
		{
			return true;
		}
		if (flag || flag2)
		{
			return false;
		}
		return a.Path == b.Path;
	}

	public static bool operator !=(BindingPath a, BindingPath b)
	{
		return !(a == b);
	}

	public static bool IsRelatedWithPathAsString(string path, string referencePath)
	{
		if (referencePath.StartsWith(path))
		{
			return true;
		}
		return false;
	}

	public static bool IsRelatedWithPath(string path, BindingPath referencePath)
	{
		if (referencePath.Path.StartsWith(path))
		{
			return true;
		}
		return false;
	}

	public bool IsRelatedWith(BindingPath referencePath)
	{
		return IsRelatedWithPath(Path, referencePath);
	}

	public void DecrementIfRelatedWith(BindingPath path, int startIndex)
	{
		if (IsRelatedWith(path) && path.Nodes.Length < Nodes.Length && int.TryParse(Nodes[path.Nodes.Length], out var result) && result >= startIndex)
		{
			result--;
			Nodes[path.Nodes.Length] = result.ToString();
		}
	}

	public BindingPath Simplify()
	{
		List<string> list = new List<string>();
		for (int i = 0; i < Nodes.Length; i++)
		{
			string text = Nodes[i];
			if (text == ".." && list.Count > 0 && list[list.Count - 1] != "..")
			{
				list.RemoveAt(list.Count - 1);
			}
			else
			{
				list.Add(text);
			}
		}
		return new BindingPath(list);
	}

	public BindingPath Append(BindingPath bindingPath)
	{
		return new BindingPath(Nodes, bindingPath.Nodes);
	}

	public override string ToString()
	{
		return Path;
	}
}
