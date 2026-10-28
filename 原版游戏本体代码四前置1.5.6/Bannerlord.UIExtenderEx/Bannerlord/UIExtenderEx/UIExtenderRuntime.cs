using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Components;
using Bannerlord.UIExtenderEx.Patches;
using Bannerlord.UIExtenderEx.Prefabs;
using Bannerlord.UIExtenderEx.Prefabs2;
using Bannerlord.UIExtenderEx.Utils;

namespace Bannerlord.UIExtenderEx;

internal class UIExtenderRuntime
{
	public readonly string ModuleName;

	public readonly PrefabComponent PrefabComponent;

	public readonly ViewModelComponent ViewModelComponent;

	public UIExtenderRuntime(string moduleName)
	{
		ModuleName = moduleName;
		PrefabComponent = new PrefabComponent(moduleName);
		ViewModelComponent = new ViewModelComponent(moduleName);
	}

	public void Register(IEnumerable<Type> types)
	{
		foreach (Type type in types)
		{
			Attribute[] customAttributes = Attribute.GetCustomAttributes(type, typeof(BaseUIExtenderAttribute));
			foreach (Attribute attribute in customAttributes)
			{
				if (!(attribute is PrefabExtensionAttribute prefabExtensionAttribute))
				{
					if (attribute is ViewModelMixinAttribute viewModelMixinAttribute)
					{
						ViewModelComponent.RegisterViewModelMixin(type, viewModelMixinAttribute.RefreshMethodName, viewModelMixinAttribute.HandleDerived);
					}
					else
					{
						MessageUtils.Fail($"Failed to find appropriate clause for base type {type} with attribute {attribute}!");
					}
					continue;
				}
				ConstructorInfo constructor = type.GetConstructor(Type.EmptyTypes);
				if ((object)constructor == null)
				{
					MessageUtils.Fail("Failed to find appropriate constructor for patch!");
					continue;
				}
				object obj = constructor.Invoke(Array.Empty<object>());
				if (!(obj is Bannerlord.UIExtenderEx.Prefabs.PrefabExtensionSetAttributePatch patch))
				{
					if (!(obj is Bannerlord.UIExtenderEx.Prefabs.PrefabExtensionInsertPatch patch2))
					{
						if (!(obj is PrefabExtensionReplacePatch patch3))
						{
							if (!(obj is PrefabExtensionInsertAsSiblingPatch patch4))
							{
								if (!(obj is CustomPatch<XmlDocument> customPatch))
								{
									if (!(obj is CustomPatch<XmlNode> customPatch2))
									{
										if (!(obj is Bannerlord.UIExtenderEx.Prefabs2.PrefabExtensionSetAttributePatch patch5))
										{
											if (obj is Bannerlord.UIExtenderEx.Prefabs2.PrefabExtensionInsertPatch patch6)
											{
												PrefabComponent.RegisterPatch(prefabExtensionAttribute.Movie, prefabExtensionAttribute.XPath, patch6);
											}
											else
											{
												MessageUtils.Fail($"Patch class is unsupported - {type}!");
											}
										}
										else
										{
											PrefabComponent.RegisterPatch(prefabExtensionAttribute.Movie, prefabExtensionAttribute.XPath, patch5);
										}
									}
									else
									{
										PrefabComponent.RegisterPatch(prefabExtensionAttribute.Movie, prefabExtensionAttribute.XPath, customPatch2.GetType(), customPatch2.Apply);
									}
								}
								else
								{
									PrefabComponent.RegisterPatch(prefabExtensionAttribute.Movie, customPatch.GetType(), customPatch.Apply);
								}
							}
							else
							{
								PrefabComponent.RegisterPatch(prefabExtensionAttribute.Movie, prefabExtensionAttribute.XPath, patch4);
							}
						}
						else
						{
							PrefabComponent.RegisterPatch(prefabExtensionAttribute.Movie, prefabExtensionAttribute.XPath, patch3);
						}
					}
					else
					{
						PrefabComponent.RegisterPatch(prefabExtensionAttribute.Movie, prefabExtensionAttribute.XPath, patch2);
					}
				}
				else
				{
					PrefabComponent.RegisterPatch(prefabExtensionAttribute.Movie, prefabExtensionAttribute.XPath, patch);
				}
				GauntletMoviePatch.Register(this, prefabExtensionAttribute.AutoGenWidgetName);
			}
		}
	}

	public void Deregister()
	{
		PrefabComponent.Deregister();
		ViewModelComponent.Deregister();
		GauntletMoviePatch.Deregister(this);
	}

	public void Enable()
	{
		ViewModelComponent.Enable();
		PrefabComponent.Enable();
	}

	public void Disable()
	{
		ViewModelComponent.Disable();
		PrefabComponent.Disable();
	}

	public void Enable(Type type)
	{
		ViewModelComponent.Enable(type);
		PrefabComponent.Enable(type);
	}

	public void Disable(Type type)
	{
		ViewModelComponent.Disable(type);
		PrefabComponent.Disable(type);
	}
}
