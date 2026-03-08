using System;
using System.Security;
using System.Security.Permissions;

namespace Mono.Cecil.Rocks
{
	// Token: 0x0200045A RID: 1114
	internal static class SecurityDeclarationRocks
	{
		// Token: 0x0600182E RID: 6190 RVA: 0x0004CA34 File Offset: 0x0004AC34
		public static PermissionSet ToPermissionSet(this SecurityDeclaration self)
		{
			if (self == null)
			{
				throw new ArgumentNullException("self");
			}
			PermissionSet set;
			if (SecurityDeclarationRocks.TryProcessPermissionSetAttribute(self, out set))
			{
				return set;
			}
			return SecurityDeclarationRocks.CreatePermissionSet(self);
		}

		// Token: 0x0600182F RID: 6191 RVA: 0x0004CA64 File Offset: 0x0004AC64
		private static bool TryProcessPermissionSetAttribute(SecurityDeclaration declaration, out PermissionSet set)
		{
			set = null;
			if (!declaration.HasSecurityAttributes && declaration.SecurityAttributes.Count != 1)
			{
				return false;
			}
			SecurityAttribute security_attribute = declaration.SecurityAttributes[0];
			if (!security_attribute.AttributeType.IsTypeOf("System.Security.Permissions", "PermissionSetAttribute"))
			{
				return false;
			}
			PermissionSetAttribute attribute = new PermissionSetAttribute((SecurityAction)declaration.Action);
			CustomAttributeNamedArgument named_argument = security_attribute.Properties[0];
			string value = (string)named_argument.Argument.Value;
			string name = named_argument.Name;
			if (!(name == "XML"))
			{
				if (!(name == "Name"))
				{
					throw new NotImplementedException(named_argument.Name);
				}
				attribute.Name = value;
			}
			else
			{
				attribute.XML = value;
			}
			set = attribute.CreatePermissionSet();
			return true;
		}

		// Token: 0x06001830 RID: 6192 RVA: 0x0004CB30 File Offset: 0x0004AD30
		private static PermissionSet CreatePermissionSet(SecurityDeclaration declaration)
		{
			PermissionSet set = new PermissionSet(PermissionState.None);
			foreach (SecurityAttribute attribute in declaration.SecurityAttributes)
			{
				IPermission permission = SecurityDeclarationRocks.CreatePermission(declaration, attribute);
				set.AddPermission(permission);
			}
			return set;
		}

		// Token: 0x06001831 RID: 6193 RVA: 0x0004CB94 File Offset: 0x0004AD94
		private static IPermission CreatePermission(SecurityDeclaration declaration, SecurityAttribute attribute)
		{
			Type type = Type.GetType(attribute.AttributeType.FullName);
			if (type == null)
			{
				throw new ArgumentException("attribute");
			}
			SecurityAttribute securityAttribute = SecurityDeclarationRocks.CreateSecurityAttribute(type, declaration);
			if (securityAttribute == null)
			{
				throw new InvalidOperationException();
			}
			SecurityDeclarationRocks.CompleteSecurityAttribute(securityAttribute, attribute);
			return securityAttribute.CreatePermission();
		}

		// Token: 0x06001832 RID: 6194 RVA: 0x0004CBE0 File Offset: 0x0004ADE0
		private static void CompleteSecurityAttribute(SecurityAttribute security_attribute, SecurityAttribute attribute)
		{
			if (attribute.HasFields)
			{
				SecurityDeclarationRocks.CompleteSecurityAttributeFields(security_attribute, attribute);
			}
			if (attribute.HasProperties)
			{
				SecurityDeclarationRocks.CompleteSecurityAttributeProperties(security_attribute, attribute);
			}
		}

		// Token: 0x06001833 RID: 6195 RVA: 0x0004CC00 File Offset: 0x0004AE00
		private static void CompleteSecurityAttributeFields(SecurityAttribute security_attribute, SecurityAttribute attribute)
		{
			Type type = security_attribute.GetType();
			foreach (CustomAttributeNamedArgument named_argument in attribute.Fields)
			{
				type.GetField(named_argument.Name).SetValue(security_attribute, named_argument.Argument.Value);
			}
		}

		// Token: 0x06001834 RID: 6196 RVA: 0x0004CC78 File Offset: 0x0004AE78
		private static void CompleteSecurityAttributeProperties(SecurityAttribute security_attribute, SecurityAttribute attribute)
		{
			Type type = security_attribute.GetType();
			foreach (CustomAttributeNamedArgument named_argument in attribute.Properties)
			{
				type.GetProperty(named_argument.Name).SetValue(security_attribute, named_argument.Argument.Value, null);
			}
		}

		// Token: 0x06001835 RID: 6197 RVA: 0x0004CCF0 File Offset: 0x0004AEF0
		private static SecurityAttribute CreateSecurityAttribute(Type attribute_type, SecurityDeclaration declaration)
		{
			SecurityAttribute security_attribute;
			try
			{
				security_attribute = (SecurityAttribute)Activator.CreateInstance(attribute_type, new object[] { (SecurityAction)declaration.Action });
			}
			catch (MissingMethodException)
			{
				security_attribute = (SecurityAttribute)Activator.CreateInstance(attribute_type, new object[0]);
			}
			return security_attribute;
		}

		// Token: 0x06001836 RID: 6198 RVA: 0x0004CD48 File Offset: 0x0004AF48
		public static SecurityDeclaration ToSecurityDeclaration(this PermissionSet self, SecurityAction action, ModuleDefinition module)
		{
			if (self == null)
			{
				throw new ArgumentNullException("self");
			}
			if (module == null)
			{
				throw new ArgumentNullException("module");
			}
			SecurityDeclaration securityDeclaration = new SecurityDeclaration(action);
			SecurityAttribute attribute = new SecurityAttribute(module.TypeSystem.LookupType("System.Security.Permissions", "PermissionSetAttribute"));
			attribute.Properties.Add(new CustomAttributeNamedArgument("XML", new CustomAttributeArgument(module.TypeSystem.String, self.ToXml().ToString())));
			securityDeclaration.SecurityAttributes.Add(attribute);
			return securityDeclaration;
		}
	}
}
