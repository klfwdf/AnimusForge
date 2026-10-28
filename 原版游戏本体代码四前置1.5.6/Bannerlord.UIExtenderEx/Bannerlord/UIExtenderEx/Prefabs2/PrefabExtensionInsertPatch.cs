using System;

namespace Bannerlord.UIExtenderEx.Prefabs2;

public abstract class PrefabExtensionInsertPatch
{
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
	protected internal abstract class PrefabExtensionContentAttribute : Attribute
	{
	}

	protected internal abstract class PrefabExtensionSingleContentAttribute : PrefabExtensionContentAttribute
	{
		public bool RemoveRootNode { get; }

		protected PrefabExtensionSingleContentAttribute(bool removeRootNode)
		{
			RemoveRootNode = removeRootNode;
		}
	}

	protected internal sealed class PrefabExtensionFileNameAttribute : PrefabExtensionSingleContentAttribute
	{
		public PrefabExtensionFileNameAttribute(bool removeRootNode = false)
			: base(removeRootNode)
		{
		}
	}

	protected internal sealed class PrefabExtensionTextAttribute : PrefabExtensionSingleContentAttribute
	{
		public PrefabExtensionTextAttribute(bool removeRootNode = false)
			: base(removeRootNode)
		{
		}
	}

	protected internal sealed class PrefabExtensionXmlNodeAttribute : PrefabExtensionSingleContentAttribute
	{
		public PrefabExtensionXmlNodeAttribute(bool removeRootNode = false)
			: base(removeRootNode)
		{
		}
	}

	protected internal sealed class PrefabExtensionXmlNodesAttribute : PrefabExtensionContentAttribute
	{
	}

	protected internal sealed class PrefabExtensionXmlDocumentAttribute : PrefabExtensionSingleContentAttribute
	{
		public PrefabExtensionXmlDocumentAttribute(bool removeRootNode = false)
			: base(removeRootNode)
		{
		}
	}

	public abstract InsertType Type { get; }

	public virtual int Index { get; }
}
