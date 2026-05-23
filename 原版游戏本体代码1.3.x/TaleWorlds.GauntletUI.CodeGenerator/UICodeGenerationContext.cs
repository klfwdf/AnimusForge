using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TaleWorlds.GauntletUI.PrefabSystem;
using TaleWorlds.Library;
using TaleWorlds.Library.CodeGeneration;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI.CodeGenerator
{
	// Token: 0x0200000E RID: 14
	public class UICodeGenerationContext
	{
		// Token: 0x17000039 RID: 57
		// (get) Token: 0x060000B0 RID: 176 RVA: 0x000044B1 File Offset: 0x000026B1
		// (set) Token: 0x060000B1 RID: 177 RVA: 0x000044B9 File Offset: 0x000026B9
		public ResourceDepot ResourceDepot { get; private set; }

		// Token: 0x1700003A RID: 58
		// (get) Token: 0x060000B2 RID: 178 RVA: 0x000044C2 File Offset: 0x000026C2
		// (set) Token: 0x060000B3 RID: 179 RVA: 0x000044CA File Offset: 0x000026CA
		public WidgetFactory WidgetFactory { get; private set; }

		// Token: 0x1700003B RID: 59
		// (get) Token: 0x060000B4 RID: 180 RVA: 0x000044D3 File Offset: 0x000026D3
		// (set) Token: 0x060000B5 RID: 181 RVA: 0x000044DB File Offset: 0x000026DB
		public FontFactory FontFactory { get; private set; }

		// Token: 0x1700003C RID: 60
		// (get) Token: 0x060000B6 RID: 182 RVA: 0x000044E4 File Offset: 0x000026E4
		// (set) Token: 0x060000B7 RID: 183 RVA: 0x000044EC File Offset: 0x000026EC
		public BrushFactory BrushFactory { get; private set; }

		// Token: 0x1700003D RID: 61
		// (get) Token: 0x060000B8 RID: 184 RVA: 0x000044F5 File Offset: 0x000026F5
		// (set) Token: 0x060000B9 RID: 185 RVA: 0x000044FD File Offset: 0x000026FD
		public SpriteData SpriteData { get; private set; }

		// Token: 0x060000BA RID: 186 RVA: 0x00004506 File Offset: 0x00002706
		public UICodeGenerationContext(string nameSpace, string outputFolder)
		{
			this._nameSpace = nameSpace;
			this._outputFolder = outputFolder;
			this._widgetTemplateGenerateContexts = new List<WidgetTemplateGenerateContext>();
		}

		// Token: 0x060000BB RID: 187 RVA: 0x00004528 File Offset: 0x00002728
		public void Prepare(IEnumerable<string> resourceLocations, IEnumerable<PrefabExtension> prefabExtensions)
		{
			this.ResourceDepot = new ResourceDepot();
			foreach (string location in resourceLocations)
			{
				this.ResourceDepot.AddLocation(BasePath.Name, location);
			}
			this.ResourceDepot.CollectResources();
			this.WidgetFactory = new WidgetFactory(this.ResourceDepot, "Prefabs");
			foreach (PrefabExtension prefabExtension in prefabExtensions)
			{
				this.WidgetFactory.PrefabExtensionContext.AddExtension(prefabExtension);
			}
			this.WidgetFactory.Initialize(null);
			this.SpriteData = new SpriteData("SpriteData");
			this.SpriteData.Load(this.ResourceDepot);
			this.FontFactory = new FontFactory(this.ResourceDepot);
			this.FontFactory.LoadAllFonts(this.SpriteData);
			this.BrushFactory = new BrushFactory(this.ResourceDepot, "Brushes", this.SpriteData, this.FontFactory);
			this.BrushFactory.Initialize();
		}

		// Token: 0x060000BC RID: 188 RVA: 0x00004664 File Offset: 0x00002864
		public void AddPrefabVariant(string prefabName, string variantName, UICodeGenerationVariantExtension variantExtension, Dictionary<string, object> data)
		{
			WidgetTemplateGenerateContext item = WidgetTemplateGenerateContext.CreateAsRoot(this, prefabName, variantName, variantExtension, data);
			this._widgetTemplateGenerateContexts.Add(item);
		}

		// Token: 0x060000BD RID: 189 RVA: 0x0000468C File Offset: 0x0000288C
		private static void ClearFolder(string folderName)
		{
			DirectoryInfo directoryInfo = new DirectoryInfo(folderName);
			FileInfo[] files = directoryInfo.GetFiles();
			for (int i = 0; i < files.Length; i++)
			{
				files[i].Delete();
			}
			foreach (DirectoryInfo directoryInfo2 in directoryInfo.GetDirectories())
			{
				UICodeGenerationContext.ClearFolder(directoryInfo2.FullName);
				directoryInfo2.Delete();
			}
		}

		// Token: 0x060000BE RID: 190 RVA: 0x000046E8 File Offset: 0x000028E8
		public void Generate()
		{
			Dictionary<string, CodeGenerationContext> dictionary = new Dictionary<string, CodeGenerationContext>();
			foreach (WidgetTemplateGenerateContext widgetTemplateGenerateContext in this._widgetTemplateGenerateContexts)
			{
				string key = widgetTemplateGenerateContext.PrefabName + ".gen.cs";
				if (!dictionary.ContainsKey(key))
				{
					dictionary.Add(key, new CodeGenerationContext());
				}
				NamespaceCode namespaceCode = dictionary[key].FindOrCreateNamespace(this._nameSpace);
				widgetTemplateGenerateContext.GenerateInto(namespaceCode);
			}
			string text = Directory.GetCurrentDirectory() + "\\..\\..\\..\\Source\\" + this._outputFolder;
			UICodeGenerationContext.ClearFolder(text);
			List<string> usingDefinitions = new List<string> { "System.Numerics", "TaleWorlds.Library" };
			foreach (KeyValuePair<string, CodeGenerationContext> keyValuePair in dictionary)
			{
				string key2 = keyValuePair.Key;
				CodeGenerationContext codeGenerationContext = dictionary[key2];
				CodeGenerationFile codeGenerationFile = new CodeGenerationFile(usingDefinitions);
				codeGenerationContext.GenerateInto(codeGenerationFile);
				string contents = codeGenerationFile.GenerateText();
				File.WriteAllText(text + "\\" + key2, contents, Encoding.UTF8);
			}
			CodeGenerationContext codeGenerationContext2 = new CodeGenerationContext();
			NamespaceCode namespaceCode2 = codeGenerationContext2.FindOrCreateNamespace(this._nameSpace);
			ClassCode classCode = new ClassCode();
			classCode.Name = "GeneratedUIPrefabCreator";
			classCode.AccessModifier = ClassCodeAccessModifier.Public;
			classCode.InheritedInterfaces.Add("TaleWorlds.GauntletUI.PrefabSystem.IGeneratedUIPrefabCreator");
			MethodCode methodCode = new MethodCode();
			methodCode.Name = "CollectGeneratedPrefabDefinitions";
			methodCode.MethodSignature = "(TaleWorlds.GauntletUI.PrefabSystem.GeneratedPrefabContext generatedPrefabContext)";
			foreach (WidgetTemplateGenerateContext widgetTemplateGenerateContext2 in this._widgetTemplateGenerateContexts)
			{
				MethodCode methodCode2 = widgetTemplateGenerateContext2.GenerateCreatorMethod();
				classCode.AddMethod(methodCode2);
				methodCode.AddLine(string.Concat(new string[] { "generatedPrefabContext.AddGeneratedPrefab(\"", widgetTemplateGenerateContext2.PrefabName, "\", \"", widgetTemplateGenerateContext2.VariantName, "\", ", methodCode2.Name, ");" }));
			}
			classCode.AddMethod(methodCode);
			namespaceCode2.AddClass(classCode);
			CodeGenerationFile codeGenerationFile2 = new CodeGenerationFile(null);
			codeGenerationContext2.GenerateInto(codeGenerationFile2);
			string contents2 = codeGenerationFile2.GenerateText();
			File.WriteAllText(text + "\\PrefabCodes.gen.cs", contents2, Encoding.UTF8);
		}

		// Token: 0x0400004C RID: 76
		private List<WidgetTemplateGenerateContext> _widgetTemplateGenerateContexts;

		// Token: 0x0400004D RID: 77
		private readonly string _nameSpace;

		// Token: 0x0400004E RID: 78
		private readonly string _outputFolder;
	}
}
