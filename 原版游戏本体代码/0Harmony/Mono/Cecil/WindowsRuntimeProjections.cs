using System;
using System.Collections.Generic;
using System.Threading;
using Mono.Collections.Generic;

namespace Mono.Cecil
{
	// Token: 0x020002B8 RID: 696
	internal sealed class WindowsRuntimeProjections
	{
		// Token: 0x170004C8 RID: 1224
		// (get) Token: 0x060011A8 RID: 4520 RVA: 0x0003526C File Offset: 0x0003346C
		private static Dictionary<string, WindowsRuntimeProjections.ProjectionInfo> Projections
		{
			get
			{
				if (WindowsRuntimeProjections.projections != null)
				{
					return WindowsRuntimeProjections.projections;
				}
				Dictionary<string, WindowsRuntimeProjections.ProjectionInfo> new_projections = new Dictionary<string, WindowsRuntimeProjections.ProjectionInfo>
				{
					{
						"AttributeTargets",
						new WindowsRuntimeProjections.ProjectionInfo("Windows.Foundation.Metadata", "System", "AttributeTargets", "System.Runtime", false)
					},
					{
						"AttributeUsageAttribute",
						new WindowsRuntimeProjections.ProjectionInfo("Windows.Foundation.Metadata", "System", "AttributeUsageAttribute", "System.Runtime", true)
					},
					{
						"Color",
						new WindowsRuntimeProjections.ProjectionInfo("Windows.UI", "Windows.UI", "Color", "System.Runtime.WindowsRuntime", false)
					},
					{
						"CornerRadius",
						new WindowsRuntimeProjections.ProjectionInfo("Windows.UI.Xaml", "Windows.UI.Xaml", "CornerRadius", "System.Runtime.WindowsRuntime.UI.Xaml", false)
					},
					{
						"DateTime",
						new WindowsRuntimeProjections.ProjectionInfo("Windows.Foundation", "System", "DateTimeOffset", "System.Runtime", false)
					},
					{
						"Duration",
						new WindowsRuntimeProjections.ProjectionInfo("Windows.UI.Xaml", "Windows.UI.Xaml", "Duration", "System.Runtime.WindowsRuntime.UI.Xaml", false)
					},
					{
						"DurationType",
						new WindowsRuntimeProjections.ProjectionInfo("Windows.UI.Xaml", "Windows.UI.Xaml", "DurationType", "System.Runtime.WindowsRuntime.UI.Xaml", false)
					},
					{
						"EventHandler`1",
						new WindowsRuntimeProjections.ProjectionInfo("Windows.Foundation", "System", "EventHandler`1", "System.Runtime", false)
					},
					{
						"EventRegistrationToken",
						new WindowsRuntimeProjections.ProjectionInfo("Windows.Foundation", "System.Runtime.InteropServices.WindowsRuntime", "EventRegistrationToken", "System.Runtime.InteropServices.WindowsRuntime", false)
					},
					{
						"GeneratorPosition",
						new WindowsRuntimeProjections.ProjectionInfo("Windows.UI.Xaml.Controls.Primitives", "Windows.UI.Xaml.Controls.Primitives", "GeneratorPosition", "System.Runtime.WindowsRuntime.UI.Xaml", false)
					},
					{
						"GridLength",
						new WindowsRuntimeProjections.ProjectionInfo("Windows.UI.Xaml", "Windows.UI.Xaml", "GridLength", "System.Runtime.WindowsRuntime.UI.Xaml", false)
					},
					{
						"GridUnitType",
						new WindowsRuntimeProjections.ProjectionInfo("Windows.UI.Xaml", "Windows.UI.Xaml", "GridUnitType", "System.Runtime.WindowsRuntime.UI.Xaml", false)
					},
					{
						"HResult",
						new WindowsRuntimeProjections.ProjectionInfo("Windows.Foundation", "System", "Exception", "System.Runtime", false)
					},
					{
						"IBindableIterable",
						new WindowsRuntimeProjections.ProjectionInfo("Windows.UI.Xaml.Interop", "System.Collections", "IEnumerable", "System.Runtime", false)
					},
					{
						"IBindableVector",
						new WindowsRuntimeProjections.ProjectionInfo("Windows.UI.Xaml.Interop", "System.Collections", "IList", "System.Runtime", false)
					},
					{
						"IClosable",
						new WindowsRuntimeProjections.ProjectionInfo("Windows.Foundation", "System", "IDisposable", "System.Runtime", false)
					},
					{
						"ICommand",
						new WindowsRuntimeProjections.ProjectionInfo("Windows.UI.Xaml.Input", "System.Windows.Input", "ICommand", "System.ObjectModel", false)
					},
					{
						"IIterable`1",
						new WindowsRuntimeProjections.ProjectionInfo("Windows.Foundation.Collections", "System.Collections.Generic", "IEnumerable`1", "System.Runtime", false)
					},
					{
						"IKeyValuePair`2",
						new WindowsRuntimeProjections.ProjectionInfo("Windows.Foundation.Collections", "System.Collections.Generic", "KeyValuePair`2", "System.Runtime", false)
					},
					{
						"IMapView`2",
						new WindowsRuntimeProjections.ProjectionInfo("Windows.Foundation.Collections", "System.Collections.Generic", "IReadOnlyDictionary`2", "System.Runtime", false)
					},
					{
						"IMap`2",
						new WindowsRuntimeProjections.ProjectionInfo("Windows.Foundation.Collections", "System.Collections.Generic", "IDictionary`2", "System.Runtime", false)
					},
					{
						"INotifyCollectionChanged",
						new WindowsRuntimeProjections.ProjectionInfo("Windows.UI.Xaml.Interop", "System.Collections.Specialized", "INotifyCollectionChanged", "System.ObjectModel", false)
					},
					{
						"INotifyPropertyChanged",
						new WindowsRuntimeProjections.ProjectionInfo("Windows.UI.Xaml.Data", "System.ComponentModel", "INotifyPropertyChanged", "System.ObjectModel", false)
					},
					{
						"IReference`1",
						new WindowsRuntimeProjections.ProjectionInfo("Windows.Foundation", "System", "Nullable`1", "System.Runtime", false)
					},
					{
						"IVectorView`1",
						new WindowsRuntimeProjections.ProjectionInfo("Windows.Foundation.Collections", "System.Collections.Generic", "IReadOnlyList`1", "System.Runtime", false)
					},
					{
						"IVector`1",
						new WindowsRuntimeProjections.ProjectionInfo("Windows.Foundation.Collections", "System.Collections.Generic", "IList`1", "System.Runtime", false)
					},
					{
						"KeyTime",
						new WindowsRuntimeProjections.ProjectionInfo("Windows.UI.Xaml.Media.Animation", "Windows.UI.Xaml.Media.Animation", "KeyTime", "System.Runtime.WindowsRuntime.UI.Xaml", false)
					},
					{
						"Matrix",
						new WindowsRuntimeProjections.ProjectionInfo("Windows.UI.Xaml.Media", "Windows.UI.Xaml.Media", "Matrix", "System.Runtime.WindowsRuntime.UI.Xaml", false)
					},
					{
						"Matrix3D",
						new WindowsRuntimeProjections.ProjectionInfo("Windows.UI.Xaml.Media.Media3D", "Windows.UI.Xaml.Media.Media3D", "Matrix3D", "System.Runtime.WindowsRuntime.UI.Xaml", false)
					},
					{
						"Matrix3x2",
						new WindowsRuntimeProjections.ProjectionInfo("Windows.Foundation.Numerics", "System.Numerics", "Matrix3x2", "System.Numerics.Vectors", false)
					},
					{
						"Matrix4x4",
						new WindowsRuntimeProjections.ProjectionInfo("Windows.Foundation.Numerics", "System.Numerics", "Matrix4x4", "System.Numerics.Vectors", false)
					},
					{
						"NotifyCollectionChangedAction",
						new WindowsRuntimeProjections.ProjectionInfo("Windows.UI.Xaml.Interop", "System.Collections.Specialized", "NotifyCollectionChangedAction", "System.ObjectModel", false)
					},
					{
						"NotifyCollectionChangedEventArgs",
						new WindowsRuntimeProjections.ProjectionInfo("Windows.UI.Xaml.Interop", "System.Collections.Specialized", "NotifyCollectionChangedEventArgs", "System.ObjectModel", false)
					},
					{
						"NotifyCollectionChangedEventHandler",
						new WindowsRuntimeProjections.ProjectionInfo("Windows.UI.Xaml.Interop", "System.Collections.Specialized", "NotifyCollectionChangedEventHandler", "System.ObjectModel", false)
					},
					{
						"Plane",
						new WindowsRuntimeProjections.ProjectionInfo("Windows.Foundation.Numerics", "System.Numerics", "Plane", "System.Numerics.Vectors", false)
					},
					{
						"Point",
						new WindowsRuntimeProjections.ProjectionInfo("Windows.Foundation", "Windows.Foundation", "Point", "System.Runtime.WindowsRuntime", false)
					},
					{
						"PropertyChangedEventArgs",
						new WindowsRuntimeProjections.ProjectionInfo("Windows.UI.Xaml.Data", "System.ComponentModel", "PropertyChangedEventArgs", "System.ObjectModel", false)
					},
					{
						"PropertyChangedEventHandler",
						new WindowsRuntimeProjections.ProjectionInfo("Windows.UI.Xaml.Data", "System.ComponentModel", "PropertyChangedEventHandler", "System.ObjectModel", false)
					},
					{
						"Quaternion",
						new WindowsRuntimeProjections.ProjectionInfo("Windows.Foundation.Numerics", "System.Numerics", "Quaternion", "System.Numerics.Vectors", false)
					},
					{
						"Rect",
						new WindowsRuntimeProjections.ProjectionInfo("Windows.Foundation", "Windows.Foundation", "Rect", "System.Runtime.WindowsRuntime", false)
					},
					{
						"RepeatBehavior",
						new WindowsRuntimeProjections.ProjectionInfo("Windows.UI.Xaml.Media.Animation", "Windows.UI.Xaml.Media.Animation", "RepeatBehavior", "System.Runtime.WindowsRuntime.UI.Xaml", false)
					},
					{
						"RepeatBehaviorType",
						new WindowsRuntimeProjections.ProjectionInfo("Windows.UI.Xaml.Media.Animation", "Windows.UI.Xaml.Media.Animation", "RepeatBehaviorType", "System.Runtime.WindowsRuntime.UI.Xaml", false)
					},
					{
						"Size",
						new WindowsRuntimeProjections.ProjectionInfo("Windows.Foundation", "Windows.Foundation", "Size", "System.Runtime.WindowsRuntime", false)
					},
					{
						"Thickness",
						new WindowsRuntimeProjections.ProjectionInfo("Windows.UI.Xaml", "Windows.UI.Xaml", "Thickness", "System.Runtime.WindowsRuntime.UI.Xaml", false)
					},
					{
						"TimeSpan",
						new WindowsRuntimeProjections.ProjectionInfo("Windows.Foundation", "System", "TimeSpan", "System.Runtime", false)
					},
					{
						"TypeName",
						new WindowsRuntimeProjections.ProjectionInfo("Windows.UI.Xaml.Interop", "System", "Type", "System.Runtime", false)
					},
					{
						"Uri",
						new WindowsRuntimeProjections.ProjectionInfo("Windows.Foundation", "System", "Uri", "System.Runtime", false)
					},
					{
						"Vector2",
						new WindowsRuntimeProjections.ProjectionInfo("Windows.Foundation.Numerics", "System.Numerics", "Vector2", "System.Numerics.Vectors", false)
					},
					{
						"Vector3",
						new WindowsRuntimeProjections.ProjectionInfo("Windows.Foundation.Numerics", "System.Numerics", "Vector3", "System.Numerics.Vectors", false)
					},
					{
						"Vector4",
						new WindowsRuntimeProjections.ProjectionInfo("Windows.Foundation.Numerics", "System.Numerics", "Vector4", "System.Numerics.Vectors", false)
					}
				};
				Interlocked.CompareExchange<Dictionary<string, WindowsRuntimeProjections.ProjectionInfo>>(ref WindowsRuntimeProjections.projections, new_projections, null);
				return WindowsRuntimeProjections.projections;
			}
		}

		// Token: 0x170004C9 RID: 1225
		// (get) Token: 0x060011A9 RID: 4521 RVA: 0x000359D8 File Offset: 0x00033BD8
		private AssemblyNameReference[] VirtualReferences
		{
			get
			{
				if (this.virtual_references == null)
				{
					Mixin.Read(this.module.AssemblyReferences);
				}
				return this.virtual_references;
			}
		}

		// Token: 0x060011AA RID: 4522 RVA: 0x000359F8 File Offset: 0x00033BF8
		public WindowsRuntimeProjections(ModuleDefinition module)
		{
			this.module = module;
		}

		// Token: 0x060011AB RID: 4523 RVA: 0x00035A28 File Offset: 0x00033C28
		public static void Project(TypeDefinition type)
		{
			TypeDefinitionTreatment treatment = TypeDefinitionTreatment.None;
			MetadataKind metadata_kind = type.Module.MetadataKind;
			Collection<MethodDefinition> redirectedMethods = null;
			Collection<KeyValuePair<InterfaceImplementation, InterfaceImplementation>> redirectedInterfaces = null;
			if (type.IsWindowsRuntime)
			{
				if (metadata_kind == MetadataKind.WindowsMetadata)
				{
					treatment = WindowsRuntimeProjections.GetWellKnownTypeDefinitionTreatment(type);
					if (treatment != TypeDefinitionTreatment.None)
					{
						WindowsRuntimeProjections.ApplyProjection(type, new TypeDefinitionProjection(type, treatment, redirectedMethods, redirectedInterfaces));
						return;
					}
					TypeReference base_type = type.BaseType;
					if (base_type != null && WindowsRuntimeProjections.IsAttribute(base_type))
					{
						treatment = TypeDefinitionTreatment.NormalAttribute;
					}
					else
					{
						treatment = WindowsRuntimeProjections.GenerateRedirectionInformation(type, out redirectedMethods, out redirectedInterfaces);
					}
				}
				else if (metadata_kind == MetadataKind.ManagedWindowsMetadata && WindowsRuntimeProjections.NeedsWindowsRuntimePrefix(type))
				{
					treatment = TypeDefinitionTreatment.PrefixWindowsRuntimeName;
				}
				if ((treatment == TypeDefinitionTreatment.PrefixWindowsRuntimeName || treatment == TypeDefinitionTreatment.NormalType) && !type.IsInterface && WindowsRuntimeProjections.HasAttribute(type.CustomAttributes, "Windows.UI.Xaml", "TreatAsAbstractComposableClassAttribute"))
				{
					treatment |= TypeDefinitionTreatment.Abstract;
				}
			}
			else if (metadata_kind == MetadataKind.ManagedWindowsMetadata && WindowsRuntimeProjections.IsClrImplementationType(type))
			{
				treatment = TypeDefinitionTreatment.UnmangleWindowsRuntimeName;
			}
			if (treatment != TypeDefinitionTreatment.None)
			{
				WindowsRuntimeProjections.ApplyProjection(type, new TypeDefinitionProjection(type, treatment, redirectedMethods, redirectedInterfaces));
			}
		}

		// Token: 0x060011AC RID: 4524 RVA: 0x00035AF4 File Offset: 0x00033CF4
		private static TypeDefinitionTreatment GetWellKnownTypeDefinitionTreatment(TypeDefinition type)
		{
			WindowsRuntimeProjections.ProjectionInfo info;
			if (!WindowsRuntimeProjections.Projections.TryGetValue(type.Name, out info))
			{
				return TypeDefinitionTreatment.None;
			}
			TypeDefinitionTreatment treatment = (info.Attribute ? TypeDefinitionTreatment.RedirectToClrAttribute : TypeDefinitionTreatment.RedirectToClrType);
			if (type.Namespace == info.ClrNamespace)
			{
				return treatment;
			}
			if (type.Namespace == info.WinRTNamespace)
			{
				return treatment | TypeDefinitionTreatment.Internal;
			}
			return TypeDefinitionTreatment.None;
		}

		// Token: 0x060011AD RID: 4525 RVA: 0x00035B54 File Offset: 0x00033D54
		private static TypeDefinitionTreatment GenerateRedirectionInformation(TypeDefinition type, out Collection<MethodDefinition> redirectedMethods, out Collection<KeyValuePair<InterfaceImplementation, InterfaceImplementation>> redirectedInterfaces)
		{
			bool implementsProjectedInterface = false;
			redirectedMethods = null;
			redirectedInterfaces = null;
			using (Collection<InterfaceImplementation>.Enumerator enumerator = type.Interfaces.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					if (WindowsRuntimeProjections.IsRedirectedType(enumerator.Current.InterfaceType))
					{
						implementsProjectedInterface = true;
						break;
					}
				}
			}
			if (!implementsProjectedInterface)
			{
				return TypeDefinitionTreatment.NormalType;
			}
			HashSet<TypeReference> allImplementedInterfaces = new HashSet<TypeReference>(new TypeReferenceEqualityComparer());
			redirectedMethods = new Collection<MethodDefinition>();
			redirectedInterfaces = new Collection<KeyValuePair<InterfaceImplementation, InterfaceImplementation>>();
			foreach (InterfaceImplementation interfaceImplementation in type.Interfaces)
			{
				TypeReference interfaceType = interfaceImplementation.InterfaceType;
				if (WindowsRuntimeProjections.IsRedirectedType(interfaceType))
				{
					allImplementedInterfaces.Add(interfaceType);
					WindowsRuntimeProjections.CollectImplementedInterfaces(interfaceType, allImplementedInterfaces);
				}
			}
			foreach (InterfaceImplementation implementedInterface in type.Interfaces)
			{
				TypeReference interfaceType2 = implementedInterface.InterfaceType;
				if (WindowsRuntimeProjections.IsRedirectedType(implementedInterface.InterfaceType))
				{
					TypeReference etype = interfaceType2.GetElementType();
					TypeReference unprojectedType = new TypeReference(etype.Namespace, etype.Name, etype.Module, etype.Scope)
					{
						DeclaringType = etype.DeclaringType,
						projection = etype.projection
					};
					WindowsRuntimeProjections.RemoveProjection(unprojectedType);
					GenericInstanceType genericInstanceType = interfaceType2 as GenericInstanceType;
					if (genericInstanceType != null)
					{
						GenericInstanceType genericUnprojectedType = new GenericInstanceType(unprojectedType);
						foreach (TypeReference genericArgument in genericInstanceType.GenericArguments)
						{
							genericUnprojectedType.GenericArguments.Add(genericArgument);
						}
						unprojectedType = genericUnprojectedType;
					}
					InterfaceImplementation unprojectedInterface = new InterfaceImplementation(unprojectedType);
					redirectedInterfaces.Add(new KeyValuePair<InterfaceImplementation, InterfaceImplementation>(implementedInterface, unprojectedInterface));
				}
			}
			if (!type.IsInterface)
			{
				foreach (TypeReference interfaceType3 in allImplementedInterfaces)
				{
					WindowsRuntimeProjections.RedirectInterfaceMethods(interfaceType3, redirectedMethods);
				}
			}
			return TypeDefinitionTreatment.RedirectImplementedMethods;
		}

		// Token: 0x060011AE RID: 4526 RVA: 0x00035D9C File Offset: 0x00033F9C
		private static void CollectImplementedInterfaces(TypeReference type, HashSet<TypeReference> results)
		{
			TypeResolver typeResolver = TypeResolver.For(type);
			foreach (InterfaceImplementation implementedInterface in type.Resolve().Interfaces)
			{
				TypeReference interfaceType = typeResolver.Resolve(implementedInterface.InterfaceType);
				results.Add(interfaceType);
				WindowsRuntimeProjections.CollectImplementedInterfaces(interfaceType, results);
			}
		}

		// Token: 0x060011AF RID: 4527 RVA: 0x00035E10 File Offset: 0x00034010
		private static void RedirectInterfaceMethods(TypeReference interfaceType, Collection<MethodDefinition> redirectedMethods)
		{
			TypeResolver typeResolver = TypeResolver.For(interfaceType);
			foreach (MethodDefinition method in interfaceType.Resolve().Methods)
			{
				MethodDefinition redirectedMethod = new MethodDefinition(method.Name, MethodAttributes.FamANDAssem | MethodAttributes.Family | MethodAttributes.Final | MethodAttributes.Virtual | MethodAttributes.VtableLayoutMask, typeResolver.Resolve(method.ReturnType));
				redirectedMethod.ImplAttributes = MethodImplAttributes.CodeTypeMask;
				foreach (ParameterDefinition parameter in method.Parameters)
				{
					redirectedMethod.Parameters.Add(new ParameterDefinition(parameter.Name, parameter.Attributes, typeResolver.Resolve(parameter.ParameterType)));
				}
				redirectedMethod.Overrides.Add(typeResolver.Resolve(method));
				redirectedMethods.Add(redirectedMethod);
			}
		}

		// Token: 0x060011B0 RID: 4528 RVA: 0x00035F14 File Offset: 0x00034114
		private static bool IsRedirectedType(TypeReference type)
		{
			TypeReferenceProjection typeRefProjection = type.GetElementType().projection as TypeReferenceProjection;
			return typeRefProjection != null && typeRefProjection.Treatment == TypeReferenceTreatment.UseProjectionInfo;
		}

		// Token: 0x060011B1 RID: 4529 RVA: 0x00035F40 File Offset: 0x00034140
		private static bool NeedsWindowsRuntimePrefix(TypeDefinition type)
		{
			if ((type.Attributes & (TypeAttributes.VisibilityMask | TypeAttributes.ClassSemanticMask)) != TypeAttributes.Public)
			{
				return false;
			}
			TypeReference base_type = type.BaseType;
			if (base_type == null || base_type.MetadataToken.TokenType != TokenType.TypeRef)
			{
				return false;
			}
			if (base_type.Namespace == "System")
			{
				string name = base_type.Name;
				if (name == "Attribute" || name == "MulticastDelegate" || name == "ValueType")
				{
					return false;
				}
			}
			return true;
		}

		// Token: 0x060011B2 RID: 4530 RVA: 0x00035FBF File Offset: 0x000341BF
		public static bool IsClrImplementationType(TypeDefinition type)
		{
			return (type.Attributes & (TypeAttributes.VisibilityMask | TypeAttributes.SpecialName)) == TypeAttributes.SpecialName && type.Name.StartsWith("<CLR>");
		}

		// Token: 0x060011B3 RID: 4531 RVA: 0x00035FE8 File Offset: 0x000341E8
		public static void ApplyProjection(TypeDefinition type, TypeDefinitionProjection projection)
		{
			if (projection == null)
			{
				return;
			}
			TypeDefinitionTreatment treatment = projection.Treatment;
			switch (treatment & TypeDefinitionTreatment.KindMask)
			{
			case TypeDefinitionTreatment.NormalType:
				type.Attributes |= TypeAttributes.Import | TypeAttributes.WindowsRuntime;
				break;
			case TypeDefinitionTreatment.NormalAttribute:
				type.Attributes |= TypeAttributes.Sealed | TypeAttributes.WindowsRuntime;
				break;
			case TypeDefinitionTreatment.UnmangleWindowsRuntimeName:
				type.Attributes = (type.Attributes & ~TypeAttributes.SpecialName) | TypeAttributes.Public;
				type.Name = type.Name.Substring("<CLR>".Length);
				break;
			case TypeDefinitionTreatment.PrefixWindowsRuntimeName:
				type.Attributes = (type.Attributes & ~TypeAttributes.Public) | TypeAttributes.Import;
				type.Name = "<WinRT>" + type.Name;
				break;
			case TypeDefinitionTreatment.RedirectToClrType:
				type.Attributes = (type.Attributes & ~TypeAttributes.Public) | TypeAttributes.Import;
				break;
			case TypeDefinitionTreatment.RedirectToClrAttribute:
				type.Attributes &= ~TypeAttributes.Public;
				break;
			case TypeDefinitionTreatment.RedirectImplementedMethods:
				type.Attributes |= TypeAttributes.Import | TypeAttributes.WindowsRuntime;
				foreach (KeyValuePair<InterfaceImplementation, InterfaceImplementation> redirectedInterfacePair in projection.RedirectedInterfaces)
				{
					type.Interfaces.Add(redirectedInterfacePair.Value);
					foreach (CustomAttribute customAttribute in redirectedInterfacePair.Key.CustomAttributes)
					{
						redirectedInterfacePair.Value.CustomAttributes.Add(customAttribute);
					}
					redirectedInterfacePair.Key.CustomAttributes.Clear();
					foreach (MethodDefinition methodDefinition in type.Methods)
					{
						foreach (MethodReference @override in methodDefinition.Overrides)
						{
							if (TypeReferenceEqualityComparer.AreEqual(@override.DeclaringType, redirectedInterfacePair.Key.InterfaceType, TypeComparisonMode.Exact))
							{
								@override.DeclaringType = redirectedInterfacePair.Value.InterfaceType;
							}
						}
					}
				}
				foreach (MethodDefinition method in projection.RedirectedMethods)
				{
					type.Methods.Add(method);
				}
				break;
			}
			if ((treatment & TypeDefinitionTreatment.Abstract) != TypeDefinitionTreatment.None)
			{
				type.Attributes |= TypeAttributes.Abstract;
			}
			if ((treatment & TypeDefinitionTreatment.Internal) != TypeDefinitionTreatment.None)
			{
				type.Attributes &= ~TypeAttributes.Public;
			}
			type.WindowsRuntimeProjection = projection;
		}

		// Token: 0x060011B4 RID: 4532 RVA: 0x00036320 File Offset: 0x00034520
		public static TypeDefinitionProjection RemoveProjection(TypeDefinition type)
		{
			if (!type.IsWindowsRuntimeProjection)
			{
				return null;
			}
			TypeDefinitionProjection projection = type.WindowsRuntimeProjection;
			type.WindowsRuntimeProjection = null;
			type.Attributes = projection.Attributes;
			type.Name = projection.Name;
			if (projection.Treatment == TypeDefinitionTreatment.RedirectImplementedMethods)
			{
				foreach (MethodDefinition method in projection.RedirectedMethods)
				{
					type.Methods.Remove(method);
				}
				foreach (KeyValuePair<InterfaceImplementation, InterfaceImplementation> redirectedInterfacePair in projection.RedirectedInterfaces)
				{
					foreach (MethodDefinition methodDefinition in type.Methods)
					{
						foreach (MethodReference @override in methodDefinition.Overrides)
						{
							if (TypeReferenceEqualityComparer.AreEqual(@override.DeclaringType, redirectedInterfacePair.Value.InterfaceType, TypeComparisonMode.Exact))
							{
								@override.DeclaringType = redirectedInterfacePair.Key.InterfaceType;
							}
						}
					}
					foreach (CustomAttribute customAttribute in redirectedInterfacePair.Value.CustomAttributes)
					{
						redirectedInterfacePair.Key.CustomAttributes.Add(customAttribute);
					}
					redirectedInterfacePair.Value.CustomAttributes.Clear();
					type.Interfaces.Remove(redirectedInterfacePair.Value);
				}
			}
			return projection;
		}

		// Token: 0x060011B5 RID: 4533 RVA: 0x00036554 File Offset: 0x00034754
		public static void Project(TypeReference type)
		{
			WindowsRuntimeProjections.ProjectionInfo info;
			TypeReferenceTreatment treatment;
			if (WindowsRuntimeProjections.Projections.TryGetValue(type.Name, out info) && info.WinRTNamespace == type.Namespace)
			{
				treatment = TypeReferenceTreatment.UseProjectionInfo;
			}
			else
			{
				treatment = WindowsRuntimeProjections.GetSpecialTypeReferenceTreatment(type);
			}
			if (treatment != TypeReferenceTreatment.None)
			{
				WindowsRuntimeProjections.ApplyProjection(type, new TypeReferenceProjection(type, treatment));
			}
		}

		// Token: 0x060011B6 RID: 4534 RVA: 0x000365A3 File Offset: 0x000347A3
		private static TypeReferenceTreatment GetSpecialTypeReferenceTreatment(TypeReference type)
		{
			if (type.Namespace == "System")
			{
				if (type.Name == "MulticastDelegate")
				{
					return TypeReferenceTreatment.SystemDelegate;
				}
				if (type.Name == "Attribute")
				{
					return TypeReferenceTreatment.SystemAttribute;
				}
			}
			return TypeReferenceTreatment.None;
		}

		// Token: 0x060011B7 RID: 4535 RVA: 0x000365E0 File Offset: 0x000347E0
		private static bool IsAttribute(TypeReference type)
		{
			return type.MetadataToken.TokenType == TokenType.TypeRef && type.Name == "Attribute" && type.Namespace == "System";
		}

		// Token: 0x060011B8 RID: 4536 RVA: 0x00036628 File Offset: 0x00034828
		private static bool IsEnum(TypeReference type)
		{
			return type.MetadataToken.TokenType == TokenType.TypeRef && type.Name == "Enum" && type.Namespace == "System";
		}

		// Token: 0x060011B9 RID: 4537 RVA: 0x00036670 File Offset: 0x00034870
		public static void ApplyProjection(TypeReference type, TypeReferenceProjection projection)
		{
			if (projection == null)
			{
				return;
			}
			TypeReferenceTreatment treatment = projection.Treatment;
			if (treatment - TypeReferenceTreatment.SystemDelegate > 1)
			{
				if (treatment == TypeReferenceTreatment.UseProjectionInfo)
				{
					WindowsRuntimeProjections.ProjectionInfo info = WindowsRuntimeProjections.Projections[type.Name];
					type.Name = info.ClrName;
					type.Namespace = info.ClrNamespace;
					type.Scope = type.Module.Projections.GetAssemblyReference(info.ClrAssembly);
				}
			}
			else
			{
				type.Scope = type.Module.Projections.GetAssemblyReference("System.Runtime");
			}
			type.WindowsRuntimeProjection = projection;
		}

		// Token: 0x060011BA RID: 4538 RVA: 0x00036700 File Offset: 0x00034900
		public static TypeReferenceProjection RemoveProjection(TypeReference type)
		{
			if (!type.IsWindowsRuntimeProjection)
			{
				return null;
			}
			TypeReferenceProjection projection = type.WindowsRuntimeProjection;
			type.WindowsRuntimeProjection = null;
			type.Name = projection.Name;
			type.Namespace = projection.Namespace;
			type.Scope = projection.Scope;
			return projection;
		}

		// Token: 0x060011BB RID: 4539 RVA: 0x0003674C File Offset: 0x0003494C
		public static void Project(MethodDefinition method)
		{
			MethodDefinitionTreatment treatment = MethodDefinitionTreatment.None;
			bool other = false;
			TypeDefinition declaring_type = method.DeclaringType;
			if (declaring_type.IsWindowsRuntime)
			{
				if (WindowsRuntimeProjections.IsClrImplementationType(declaring_type))
				{
					treatment = MethodDefinitionTreatment.None;
				}
				else if (declaring_type.IsNested)
				{
					treatment = MethodDefinitionTreatment.None;
				}
				else if (declaring_type.IsInterface)
				{
					treatment = MethodDefinitionTreatment.Runtime | MethodDefinitionTreatment.InternalCall;
				}
				else if (declaring_type.Module.MetadataKind == MetadataKind.ManagedWindowsMetadata && !method.IsPublic)
				{
					treatment = MethodDefinitionTreatment.None;
				}
				else
				{
					other = true;
					TypeReference base_type = declaring_type.BaseType;
					if (base_type != null && base_type.MetadataToken.TokenType == TokenType.TypeRef)
					{
						TypeReferenceTreatment specialTypeReferenceTreatment = WindowsRuntimeProjections.GetSpecialTypeReferenceTreatment(base_type);
						if (specialTypeReferenceTreatment != TypeReferenceTreatment.SystemDelegate)
						{
							if (specialTypeReferenceTreatment == TypeReferenceTreatment.SystemAttribute)
							{
								treatment = MethodDefinitionTreatment.Runtime | MethodDefinitionTreatment.InternalCall;
								other = false;
							}
						}
						else
						{
							treatment = MethodDefinitionTreatment.Public | MethodDefinitionTreatment.Runtime;
							other = false;
						}
					}
				}
			}
			if (other)
			{
				bool seen_redirected = false;
				bool seen_non_redirected = false;
				foreach (MethodReference @override in method.Overrides)
				{
					if (@override.MetadataToken.TokenType == TokenType.MemberRef && WindowsRuntimeProjections.ImplementsRedirectedInterface(@override))
					{
						seen_redirected = true;
					}
					else
					{
						seen_non_redirected = true;
					}
				}
				if (seen_redirected && !seen_non_redirected)
				{
					treatment = MethodDefinitionTreatment.Private | MethodDefinitionTreatment.Runtime | MethodDefinitionTreatment.InternalCall;
					other = false;
				}
			}
			if (other)
			{
				treatment |= WindowsRuntimeProjections.GetMethodDefinitionTreatmentFromCustomAttributes(method);
			}
			if (treatment != MethodDefinitionTreatment.None)
			{
				WindowsRuntimeProjections.ApplyProjection(method, new MethodDefinitionProjection(method, treatment));
			}
		}

		// Token: 0x060011BC RID: 4540 RVA: 0x0003688C File Offset: 0x00034A8C
		private static MethodDefinitionTreatment GetMethodDefinitionTreatmentFromCustomAttributes(MethodDefinition method)
		{
			MethodDefinitionTreatment treatment = MethodDefinitionTreatment.None;
			foreach (CustomAttribute customAttribute in method.CustomAttributes)
			{
				TypeReference type = customAttribute.AttributeType;
				if (!(type.Namespace != "Windows.UI.Xaml"))
				{
					if (type.Name == "TreatAsPublicMethodAttribute")
					{
						treatment |= MethodDefinitionTreatment.Public;
					}
					else if (type.Name == "TreatAsAbstractMethodAttribute")
					{
						treatment |= MethodDefinitionTreatment.Abstract;
					}
				}
			}
			return treatment;
		}

		// Token: 0x060011BD RID: 4541 RVA: 0x00036920 File Offset: 0x00034B20
		public static void ApplyProjection(MethodDefinition method, MethodDefinitionProjection projection)
		{
			if (projection == null)
			{
				return;
			}
			MethodDefinitionTreatment treatment = projection.Treatment;
			if ((treatment & MethodDefinitionTreatment.Abstract) != MethodDefinitionTreatment.None)
			{
				method.Attributes |= MethodAttributes.Abstract;
			}
			if ((treatment & MethodDefinitionTreatment.Private) != MethodDefinitionTreatment.None)
			{
				method.Attributes = (method.Attributes & ~MethodAttributes.MemberAccessMask) | MethodAttributes.Private;
			}
			if ((treatment & MethodDefinitionTreatment.Public) != MethodDefinitionTreatment.None)
			{
				method.Attributes = (method.Attributes & ~MethodAttributes.MemberAccessMask) | MethodAttributes.Public;
			}
			if ((treatment & MethodDefinitionTreatment.Runtime) != MethodDefinitionTreatment.None)
			{
				method.ImplAttributes |= MethodImplAttributes.CodeTypeMask;
			}
			if ((treatment & MethodDefinitionTreatment.InternalCall) != MethodDefinitionTreatment.None)
			{
				method.ImplAttributes |= MethodImplAttributes.InternalCall;
			}
			method.WindowsRuntimeProjection = projection;
		}

		// Token: 0x060011BE RID: 4542 RVA: 0x000369B4 File Offset: 0x00034BB4
		public static MethodDefinitionProjection RemoveProjection(MethodDefinition method)
		{
			if (!method.IsWindowsRuntimeProjection)
			{
				return null;
			}
			MethodDefinitionProjection projection = method.WindowsRuntimeProjection;
			method.WindowsRuntimeProjection = null;
			method.Attributes = projection.Attributes;
			method.ImplAttributes = projection.ImplAttributes;
			method.Name = projection.Name;
			return projection;
		}

		// Token: 0x060011BF RID: 4543 RVA: 0x00036A00 File Offset: 0x00034C00
		public static void Project(FieldDefinition field)
		{
			FieldDefinitionTreatment treatment = FieldDefinitionTreatment.None;
			TypeDefinition declaring_type = field.DeclaringType;
			if (declaring_type.Module.MetadataKind == MetadataKind.WindowsMetadata && field.IsRuntimeSpecialName && field.Name == "value__")
			{
				TypeReference base_type = declaring_type.BaseType;
				if (base_type != null && WindowsRuntimeProjections.IsEnum(base_type))
				{
					treatment = FieldDefinitionTreatment.Public;
				}
			}
			if (treatment != FieldDefinitionTreatment.None)
			{
				WindowsRuntimeProjections.ApplyProjection(field, new FieldDefinitionProjection(field, treatment));
			}
		}

		// Token: 0x060011C0 RID: 4544 RVA: 0x00036A62 File Offset: 0x00034C62
		public static void ApplyProjection(FieldDefinition field, FieldDefinitionProjection projection)
		{
			if (projection == null)
			{
				return;
			}
			if (projection.Treatment == FieldDefinitionTreatment.Public)
			{
				field.Attributes = (field.Attributes & ~FieldAttributes.FieldAccessMask) | FieldAttributes.Public;
			}
			field.WindowsRuntimeProjection = projection;
		}

		// Token: 0x060011C1 RID: 4545 RVA: 0x00036A8C File Offset: 0x00034C8C
		public static FieldDefinitionProjection RemoveProjection(FieldDefinition field)
		{
			if (!field.IsWindowsRuntimeProjection)
			{
				return null;
			}
			FieldDefinitionProjection projection = field.WindowsRuntimeProjection;
			field.WindowsRuntimeProjection = null;
			field.Attributes = projection.Attributes;
			return projection;
		}

		// Token: 0x060011C2 RID: 4546 RVA: 0x00036AC0 File Offset: 0x00034CC0
		private static bool ImplementsRedirectedInterface(MemberReference member)
		{
			TypeReference declaring_type = member.DeclaringType;
			TokenType tokenType = declaring_type.MetadataToken.TokenType;
			TypeReference type;
			if (tokenType != TokenType.TypeRef)
			{
				if (tokenType != TokenType.TypeSpec)
				{
					return false;
				}
				if (!declaring_type.IsGenericInstance)
				{
					return false;
				}
				type = ((TypeSpecification)declaring_type).ElementType;
				if (type.MetadataType != MetadataType.Class || type.MetadataToken.TokenType != TokenType.TypeRef)
				{
					return false;
				}
			}
			else
			{
				type = declaring_type;
			}
			TypeReferenceProjection projection = WindowsRuntimeProjections.RemoveProjection(type);
			bool found = false;
			WindowsRuntimeProjections.ProjectionInfo info;
			if (WindowsRuntimeProjections.Projections.TryGetValue(type.Name, out info) && type.Namespace == info.WinRTNamespace)
			{
				found = true;
			}
			WindowsRuntimeProjections.ApplyProjection(type, projection);
			return found;
		}

		// Token: 0x060011C3 RID: 4547 RVA: 0x00036B74 File Offset: 0x00034D74
		public void AddVirtualReferences(Collection<AssemblyNameReference> references)
		{
			AssemblyNameReference corlib = WindowsRuntimeProjections.GetCoreLibrary(references);
			this.corlib_version = corlib.Version;
			corlib.Version = WindowsRuntimeProjections.version;
			if (this.virtual_references == null)
			{
				AssemblyNameReference[] winrt_references = WindowsRuntimeProjections.GetAssemblyReferences(corlib);
				Interlocked.CompareExchange<AssemblyNameReference[]>(ref this.virtual_references, winrt_references, null);
			}
			foreach (AssemblyNameReference reference in this.virtual_references)
			{
				references.Add(reference);
			}
		}

		// Token: 0x060011C4 RID: 4548 RVA: 0x00036BE0 File Offset: 0x00034DE0
		public void RemoveVirtualReferences(Collection<AssemblyNameReference> references)
		{
			WindowsRuntimeProjections.GetCoreLibrary(references).Version = this.corlib_version;
			foreach (AssemblyNameReference reference in this.VirtualReferences)
			{
				references.Remove(reference);
			}
		}

		// Token: 0x060011C5 RID: 4549 RVA: 0x00036C20 File Offset: 0x00034E20
		private static AssemblyNameReference[] GetAssemblyReferences(AssemblyNameReference corlib)
		{
			AssemblyNameReference system_runtime = new AssemblyNameReference("System.Runtime", WindowsRuntimeProjections.version);
			AssemblyNameReference system_runtime_interopservices_windowsruntime = new AssemblyNameReference("System.Runtime.InteropServices.WindowsRuntime", WindowsRuntimeProjections.version);
			AssemblyNameReference system_objectmodel = new AssemblyNameReference("System.ObjectModel", WindowsRuntimeProjections.version);
			AssemblyNameReference system_runtime_windowsruntime = new AssemblyNameReference("System.Runtime.WindowsRuntime", WindowsRuntimeProjections.version);
			AssemblyNameReference system_runtime_windowsruntime_ui_xaml = new AssemblyNameReference("System.Runtime.WindowsRuntime.UI.Xaml", WindowsRuntimeProjections.version);
			AssemblyNameReference system_numerics_vectors = new AssemblyNameReference("System.Numerics.Vectors", WindowsRuntimeProjections.version);
			if (corlib.HasPublicKey)
			{
				system_runtime_windowsruntime.PublicKey = (system_runtime_windowsruntime_ui_xaml.PublicKey = corlib.PublicKey);
				system_runtime.PublicKey = (system_runtime_interopservices_windowsruntime.PublicKey = (system_objectmodel.PublicKey = (system_numerics_vectors.PublicKey = WindowsRuntimeProjections.contract_pk)));
			}
			else
			{
				system_runtime_windowsruntime.PublicKeyToken = (system_runtime_windowsruntime_ui_xaml.PublicKeyToken = corlib.PublicKeyToken);
				system_runtime.PublicKeyToken = (system_runtime_interopservices_windowsruntime.PublicKeyToken = (system_objectmodel.PublicKeyToken = (system_numerics_vectors.PublicKeyToken = WindowsRuntimeProjections.contract_pk_token)));
			}
			return new AssemblyNameReference[] { system_runtime, system_runtime_interopservices_windowsruntime, system_objectmodel, system_runtime_windowsruntime, system_runtime_windowsruntime_ui_xaml, system_numerics_vectors };
		}

		// Token: 0x060011C6 RID: 4550 RVA: 0x00036D44 File Offset: 0x00034F44
		private static AssemblyNameReference GetCoreLibrary(Collection<AssemblyNameReference> references)
		{
			foreach (AssemblyNameReference reference in references)
			{
				if (reference.Name == "mscorlib")
				{
					return reference;
				}
			}
			throw new BadImageFormatException("Missing mscorlib reference in AssemblyRef table.");
		}

		// Token: 0x060011C7 RID: 4551 RVA: 0x00036DB0 File Offset: 0x00034FB0
		private AssemblyNameReference GetAssemblyReference(string name)
		{
			foreach (AssemblyNameReference assembly in this.VirtualReferences)
			{
				if (assembly.Name == name)
				{
					return assembly;
				}
			}
			throw new Exception();
		}

		// Token: 0x060011C8 RID: 4552 RVA: 0x00036DEC File Offset: 0x00034FEC
		public static void Project(ICustomAttributeProvider owner, Collection<CustomAttribute> owner_attributes, CustomAttribute attribute)
		{
			if (!WindowsRuntimeProjections.IsWindowsAttributeUsageAttribute(owner, attribute))
			{
				return;
			}
			CustomAttributeValueTreatment treatment = CustomAttributeValueTreatment.None;
			TypeDefinition type = (TypeDefinition)owner;
			if (type.Namespace == "Windows.Foundation.Metadata")
			{
				if (type.Name == "VersionAttribute")
				{
					treatment = CustomAttributeValueTreatment.VersionAttribute;
				}
				else if (type.Name == "DeprecatedAttribute")
				{
					treatment = CustomAttributeValueTreatment.DeprecatedAttribute;
				}
			}
			if (treatment == CustomAttributeValueTreatment.None)
			{
				treatment = (WindowsRuntimeProjections.HasAttribute(owner_attributes, "Windows.Foundation.Metadata", "AllowMultipleAttribute") ? CustomAttributeValueTreatment.AllowMultiple : CustomAttributeValueTreatment.AllowSingle);
			}
			if (treatment != CustomAttributeValueTreatment.None)
			{
				AttributeTargets attribute_targets = (AttributeTargets)attribute.ConstructorArguments[0].Value;
				WindowsRuntimeProjections.ApplyProjection(attribute, new CustomAttributeValueProjection(attribute_targets, treatment));
			}
		}

		// Token: 0x060011C9 RID: 4553 RVA: 0x00036E8C File Offset: 0x0003508C
		private static bool IsWindowsAttributeUsageAttribute(ICustomAttributeProvider owner, CustomAttribute attribute)
		{
			if (owner.MetadataToken.TokenType != TokenType.TypeDef)
			{
				return false;
			}
			MethodReference constructor = attribute.Constructor;
			if (constructor.MetadataToken.TokenType != TokenType.MemberRef)
			{
				return false;
			}
			TypeReference declaring_type = constructor.DeclaringType;
			return declaring_type.MetadataToken.TokenType == TokenType.TypeRef && declaring_type.Name == "AttributeUsageAttribute" && declaring_type.Namespace == "System";
		}

		// Token: 0x060011CA RID: 4554 RVA: 0x00036F10 File Offset: 0x00035110
		private static bool HasAttribute(Collection<CustomAttribute> attributes, string @namespace, string name)
		{
			foreach (CustomAttribute customAttribute in attributes)
			{
				TypeReference attribute_type = customAttribute.AttributeType;
				if (attribute_type.Name == name && attribute_type.Namespace == @namespace)
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x060011CB RID: 4555 RVA: 0x00036F80 File Offset: 0x00035180
		public static void ApplyProjection(CustomAttribute attribute, CustomAttributeValueProjection projection)
		{
			if (projection == null)
			{
				return;
			}
			bool version_or_deprecated;
			bool multiple;
			switch (projection.Treatment)
			{
			case CustomAttributeValueTreatment.AllowSingle:
				version_or_deprecated = false;
				multiple = false;
				break;
			case CustomAttributeValueTreatment.AllowMultiple:
				version_or_deprecated = false;
				multiple = true;
				break;
			case CustomAttributeValueTreatment.VersionAttribute:
			case CustomAttributeValueTreatment.DeprecatedAttribute:
				version_or_deprecated = true;
				multiple = true;
				break;
			default:
				throw new ArgumentException();
			}
			AttributeTargets attribute_targets = (AttributeTargets)attribute.ConstructorArguments[0].Value;
			if (version_or_deprecated)
			{
				attribute_targets |= AttributeTargets.Constructor | AttributeTargets.Property;
			}
			attribute.ConstructorArguments[0] = new CustomAttributeArgument(attribute.ConstructorArguments[0].Type, attribute_targets);
			attribute.Properties.Add(new CustomAttributeNamedArgument("AllowMultiple", new CustomAttributeArgument(attribute.Module.TypeSystem.Boolean, multiple)));
			attribute.projection = projection;
		}

		// Token: 0x060011CC RID: 4556 RVA: 0x00037054 File Offset: 0x00035254
		public static CustomAttributeValueProjection RemoveProjection(CustomAttribute attribute)
		{
			if (attribute.projection == null)
			{
				return null;
			}
			CustomAttributeValueProjection projection = attribute.projection;
			attribute.projection = null;
			attribute.ConstructorArguments[0] = new CustomAttributeArgument(attribute.ConstructorArguments[0].Type, projection.Targets);
			attribute.Properties.Clear();
			return projection;
		}

		// Token: 0x04000671 RID: 1649
		private static readonly Version version = new Version(4, 0, 0, 0);

		// Token: 0x04000672 RID: 1650
		private static readonly byte[] contract_pk_token = new byte[] { 176, 63, 95, 127, 17, 213, 10, 58 };

		// Token: 0x04000673 RID: 1651
		private static readonly byte[] contract_pk = new byte[]
		{
			0, 36, 0, 0, 4, 128, 0, 0, 148, 0,
			0, 0, 6, 2, 0, 0, 0, 36, 0, 0,
			82, 83, 65, 49, 0, 4, 0, 0, 1, 0,
			1, 0, 7, 209, 250, 87, 196, 174, 217, 240,
			163, 46, 132, 170, 15, 174, 253, 13, 233, 232,
			253, 106, 236, 143, 135, 251, 3, 118, 108, 131,
			76, 153, 146, 30, 178, 59, 231, 154, 217, 213,
			220, 193, 221, 154, 210, 54, 19, 33, 2, 144,
			11, 114, 60, 249, 128, 149, 127, 196, 225, 119,
			16, 143, 198, 7, 119, 79, 41, 232, 50, 14,
			146, 234, 5, 236, 228, 232, 33, 192, 165, 239,
			232, 241, 100, 92, 76, 12, 147, 193, 171, 153,
			40, 93, 98, 44, 170, 101, 44, 29, 250, 214,
			61, 116, 93, 111, 45, 229, 241, 126, 94, 175,
			15, 196, 150, 61, 38, 28, 138, 18, 67, 101,
			24, 32, 109, 192, 147, 52, 77, 90, 210, 147
		};

		// Token: 0x04000674 RID: 1652
		private static Dictionary<string, WindowsRuntimeProjections.ProjectionInfo> projections;

		// Token: 0x04000675 RID: 1653
		private readonly ModuleDefinition module;

		// Token: 0x04000676 RID: 1654
		private Version corlib_version = new Version(255, 255, 255, 255);

		// Token: 0x04000677 RID: 1655
		private AssemblyNameReference[] virtual_references;

		// Token: 0x020002B9 RID: 697
		private struct ProjectionInfo
		{
			// Token: 0x060011CE RID: 4558 RVA: 0x000370F5 File Offset: 0x000352F5
			public ProjectionInfo(string winrt_namespace, string clr_namespace, string clr_name, string clr_assembly, bool attribute = false)
			{
				this.WinRTNamespace = winrt_namespace;
				this.ClrNamespace = clr_namespace;
				this.ClrName = clr_name;
				this.ClrAssembly = clr_assembly;
				this.Attribute = attribute;
			}

			// Token: 0x04000678 RID: 1656
			public readonly string WinRTNamespace;

			// Token: 0x04000679 RID: 1657
			public readonly string ClrNamespace;

			// Token: 0x0400067A RID: 1658
			public readonly string ClrName;

			// Token: 0x0400067B RID: 1659
			public readonly string ClrAssembly;

			// Token: 0x0400067C RID: 1660
			public readonly bool Attribute;
		}
	}
}
