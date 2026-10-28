using System.Collections.Generic;
using System.IO;
using System.Text;
using TaleWorlds.GauntletUI.PrefabSystem;
using TaleWorlds.Library;
using TaleWorlds.Library.CodeGeneration;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI.CodeGenerator;

public class UICodeGenerationContext
{
	private List<WidgetTemplateGenerateContext> _widgetTemplateGenerateContexts;

	private readonly string _nameSpace;

	private readonly string _outputFolder;

	public ResourceDepot ResourceDepot { get; private set; }

	public WidgetFactory WidgetFactory { get; private set; }

	public FontFactory FontFactory { get; private set; }

	public BrushFactory BrushFactory { get; private set; }

	public SpriteData SpriteData { get; private set; }

	public UICodeGenerationContext(string nameSpace, string outputFolder)
	{
		_nameSpace = nameSpace;
		_outputFolder = outputFolder;
		_widgetTemplateGenerateContexts = new List<WidgetTemplateGenerateContext>();
	}

	public void Prepare(IEnumerable<string> resourceLocations, IEnumerable<PrefabExtension> prefabExtensions)
	{
		ResourceDepot = new ResourceDepot();
		foreach (string resourceLocation in resourceLocations)
		{
			ResourceDepot.AddLocation(BasePath.Name, resourceLocation);
		}
		ResourceDepot.CollectResources();
		WidgetFactory = new WidgetFactory(ResourceDepot, "Prefabs");
		foreach (PrefabExtension prefabExtension in prefabExtensions)
		{
			WidgetFactory.PrefabExtensionContext.AddExtension(prefabExtension);
		}
		WidgetFactory.Initialize();
		SpriteData = new SpriteData("SpriteData");
		SpriteData.Load(ResourceDepot);
		FontFactory = new FontFactory(ResourceDepot);
		FontFactory.LoadAllFonts(SpriteData);
		BrushFactory = new BrushFactory(ResourceDepot, "Brushes", SpriteData, FontFactory);
		BrushFactory.Initialize();
	}

	public void AddPrefabVariant(string prefabName, string variantName, UICodeGenerationVariantExtension variantExtension, Dictionary<string, object> data)
	{
		WidgetTemplateGenerateContext item = WidgetTemplateGenerateContext.CreateAsRoot(this, prefabName, variantName, variantExtension, data);
		_widgetTemplateGenerateContexts.Add(item);
	}

	private static void ClearFolder(string folderName)
	{
		DirectoryInfo directoryInfo = new DirectoryInfo(folderName);
		FileInfo[] files = directoryInfo.GetFiles();
		for (int i = 0; i < files.Length; i++)
		{
			files[i].Delete();
		}
		DirectoryInfo[] directories = directoryInfo.GetDirectories();
		foreach (DirectoryInfo obj in directories)
		{
			ClearFolder(obj.FullName);
			obj.Delete();
		}
	}

	public void Generate()
	{
		Dictionary<string, CodeGenerationContext> dictionary = new Dictionary<string, CodeGenerationContext>();
		foreach (WidgetTemplateGenerateContext widgetTemplateGenerateContext in _widgetTemplateGenerateContexts)
		{
			string key = widgetTemplateGenerateContext.PrefabName + ".gen.cs";
			if (!dictionary.ContainsKey(key))
			{
				dictionary.Add(key, new CodeGenerationContext());
			}
			NamespaceCode namespaceCode = dictionary[key].FindOrCreateNamespace(_nameSpace);
			widgetTemplateGenerateContext.GenerateInto(namespaceCode);
		}
		string text = string.Concat(Directory.GetCurrentDirectory() + "\\..\\..\\..\\Source\\", _outputFolder);
		ClearFolder(text);
		List<string> usingDefinitions = new List<string> { "System.Numerics", "TaleWorlds.Library" };
		foreach (KeyValuePair<string, CodeGenerationContext> item in dictionary)
		{
			string key2 = item.Key;
			CodeGenerationContext codeGenerationContext = dictionary[key2];
			CodeGenerationFile codeGenerationFile = new CodeGenerationFile(usingDefinitions);
			codeGenerationContext.GenerateInto(codeGenerationFile);
			string contents = codeGenerationFile.GenerateText();
			File.WriteAllText(text + "\\" + key2, contents, Encoding.UTF8);
		}
		CodeGenerationContext codeGenerationContext2 = new CodeGenerationContext();
		NamespaceCode namespaceCode2 = codeGenerationContext2.FindOrCreateNamespace(_nameSpace);
		ClassCode classCode = new ClassCode();
		classCode.Name = "GeneratedUIPrefabCreator";
		classCode.AccessModifier = ClassCodeAccessModifier.Public;
		classCode.InheritedInterfaces.Add("TaleWorlds.GauntletUI.PrefabSystem.IGeneratedUIPrefabCreator");
		MethodCode methodCode = new MethodCode();
		methodCode.Name = "CollectGeneratedPrefabDefinitions";
		methodCode.MethodSignature = "(TaleWorlds.GauntletUI.PrefabSystem.GeneratedPrefabContext generatedPrefabContext)";
		foreach (WidgetTemplateGenerateContext widgetTemplateGenerateContext2 in _widgetTemplateGenerateContexts)
		{
			MethodCode methodCode2 = widgetTemplateGenerateContext2.GenerateCreatorMethod();
			classCode.AddMethod(methodCode2);
			methodCode.AddLine("generatedPrefabContext.AddGeneratedPrefab(\"" + widgetTemplateGenerateContext2.PrefabName + "\", \"" + widgetTemplateGenerateContext2.VariantName + "\", " + methodCode2.Name + ");");
		}
		classCode.AddMethod(methodCode);
		namespaceCode2.AddClass(classCode);
		CodeGenerationFile codeGenerationFile2 = new CodeGenerationFile();
		codeGenerationContext2.GenerateInto(codeGenerationFile2);
		string contents2 = codeGenerationFile2.GenerateText();
		File.WriteAllText(text + "\\PrefabCodes.gen.cs", contents2, Encoding.UTF8);
	}
}
