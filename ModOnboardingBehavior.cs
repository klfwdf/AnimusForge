using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Voxforge;

public class ModOnboardingBehavior : CampaignBehaviorBase
{
	private const string SetupDoneKey = "_Voxforge_setup_done_v1";

	private bool _setupDone;

	private bool _welcomeShownThisSession;

	private bool _welcomeInProgress;

	private long _suppressWelcomeUntilUtcTicks;

	private bool _pendingWelcome;

	private long _pendingWelcomeAfterUtcTicks;

	public static ModOnboardingBehavior Instance { get; private set; }

	public ModOnboardingBehavior()
	{
		Instance = this;
	}

	public override void RegisterEvents()
	{
		CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnGameStarted);
		CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameStarted);
		CampaignEvents.TickEvent.AddNonSerializedListener(this, OnTick);
	}

	public override void SyncData(IDataStore dataStore)
	{
		dataStore.SyncData("_Voxforge_setup_done_v1", ref _setupDone);
		if (!_setupDone)
		{
			_welcomeShownThisSession = false;
		}
	}

	private void OnGameStarted(CampaignGameStarter starter)
	{
		if (!_setupDone)
		{
			MarkPendingWelcome();
		}
	}

	private void MarkPendingWelcome()
	{
		try
		{
			_pendingWelcome = true;
			_pendingWelcomeAfterUtcTicks = DateTime.UtcNow.Ticks + TimeSpan.FromSeconds(2.0).Ticks;
		}
		catch
		{
		}
	}

	private void OnTick(float dt)
	{
		try
		{
			if (!_setupDone && _pendingWelcome && !_welcomeShownThisSession && DateTime.UtcNow.Ticks >= _pendingWelcomeAfterUtcTicks && Campaign.Current != null && Campaign.Current.GameStarted)
			{
				_pendingWelcome = false;
				_welcomeShownThisSession = true;
				ShowWelcomePopup(fromGate: false);
			}
		}
		catch
		{
		}
	}

	public static bool EnsureSetupReady()
	{
		ModOnboardingBehavior modOnboardingBehavior = Instance ?? Campaign.Current?.GetCampaignBehavior<ModOnboardingBehavior>();
		if (modOnboardingBehavior == null)
		{
			return true;
		}
		if (modOnboardingBehavior._setupDone)
		{
			return true;
		}
		modOnboardingBehavior.ShowWelcomePopup(fromGate: true);
		return false;
	}

	private void ShowWelcomePopup(bool fromGate)
	{
		try
		{
			if (_setupDone || _welcomeInProgress)
			{
				return;
			}
			long ticks = DateTime.UtcNow.Ticks;
			if (_suppressWelcomeUntilUtcTicks > ticks)
			{
				return;
			}
			_suppressWelcomeUntilUtcTicks = ticks + TimeSpan.FromMilliseconds(fromGate ? 800 : 200).Ticks;
			string playerExportsRootPath = GetPlayerExportsRootPath();
			string text = "首次在此存档中使用 Voxforge，必须导入编辑器导出的 JSON 数据，否则本 MOD 的对话/场景喊话将不可用。\n\n需要导入（缺一不可）：\n1) Hero：个性与背景（personality_background/*.json）\n2) 非Hero：描述（unnamed_persona/*.json）\n3) 知识：knowledge/rules/*.json（兼容旧版：knowledge/KnowledgeRules.json）\n\n导入目录（默认）：\n" + playerExportsRootPath;
			_welcomeInProgress = true;
			InformationManager.ShowInquiry(new InquiryData("Voxforge - 首次使用", text, isAffirmativeOptionShown: true, isNegativeOptionShown: true, "一键导入", "退出（不导入不可用）", delegate
			{
				_welcomeInProgress = false;
				OpenImportFolderPicker(delegate
				{
					ShowWelcomePopup(fromGate: true);
				});
			}, delegate
			{
				_welcomeInProgress = false;
				InformationManager.DisplayMessage(new InformationMessage("未完成首次导入：本 MOD 的对话/场景喊话将被阻止。"));
				ShowWelcomePopup(fromGate: true);
			}), pauseGameActiveState: true);
		}
		catch
		{
			_welcomeInProgress = false;
		}
	}

	private void OpenImportFolderPicker(Action onReturn)
	{
		try
		{
			if (onReturn == null)
			{
				onReturn = delegate
				{
				};
			}
			string playerExportsRootPath = GetPlayerExportsRootPath();
			if (!Directory.Exists(playerExportsRootPath))
			{
				InformationManager.DisplayMessage(new InformationMessage("找不到导出目录：" + playerExportsRootPath));
				onReturn();
				return;
			}
			List<string> list = (from d in new DirectoryInfo(playerExportsRootPath).GetDirectories()
				orderby d.LastWriteTimeUtc descending
				select d.Name).ToList();
			List<InquiryElement> list2 = new List<InquiryElement>();
			list2.Add(new InquiryElement("__latest__", "自动选择最新导出（推荐）", null));
			list2.Add(new InquiryElement("__manual__", "手动输入文件夹名", null));
			foreach (string item in list)
			{
				if (!string.IsNullOrWhiteSpace(item))
				{
					list2.Add(new InquiryElement(item, item, null));
				}
			}
			MultiSelectionInquiryData data = new MultiSelectionInquiryData("选择导入文件夹", "请选择 PlayerExports 下的导出文件夹：", list2, isExitShown: true, 0, 1, "导入", "返回", delegate(List<InquiryElement> selected)
			{
				if (selected == null || selected.Count == 0)
				{
					onReturn();
				}
				else
				{
					string text = selected[0].Identifier as string;
					if (text == "__manual__")
					{
						InformationManager.ShowTextInquiry(new TextInquiryData("手动输入文件夹名", "请输入 PlayerExports 下的文件夹名（留空=最新）：", isAffirmativeOptionShown: true, isNegativeOptionShown: true, "确定", "取消", delegate(string input)
						{
							string folderName2 = (input ?? "").Trim();
							TryImportRequiredSetAndUnlock(folderName2, onReturn);
						}, delegate
						{
							OpenImportFolderPicker(onReturn);
						}));
					}
					else
					{
						string folderName = ((text == "__latest__") ? "" : (text ?? ""));
						TryImportRequiredSetAndUnlock(folderName, onReturn);
					}
				}
			}, delegate
			{
				onReturn();
			});
			MBInformationManager.ShowMultiSelectionInquiry(data);
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("打开导入选择失败：" + ex.Message));
			onReturn?.Invoke();
		}
	}

	private void TryImportRequiredSetAndUnlock(string folderName, Action onReturn)
	{
		try
		{
			string text = ResolveImportFolderPath(folderName);
			if (string.IsNullOrWhiteSpace(text) || !Directory.Exists(text))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到导出目录。"));
				OpenImportFolderPicker(onReturn);
				return;
			}
			string path = Path.Combine(text, "personality_background");
			if (!Directory.Exists(path) || Directory.GetFiles(path, "*.json").Length == 0)
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：缺少 personality_background\\*.json"));
				OpenImportFolderPicker(onReturn);
				return;
			}
			string path2 = Path.Combine(text, "unnamed_persona");
			if (!Directory.Exists(path2) || Directory.GetFiles(path2, "*.json").Length == 0)
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：缺少 unnamed_persona\\*.json"));
				OpenImportFolderPicker(onReturn);
				return;
			}
			bool flag = false;
			try
			{
				string path3 = Path.Combine(text, "knowledge", "rules");
				if (Directory.Exists(path3) && Directory.GetFiles(path3, "*.json").Length != 0)
				{
					flag = true;
				}
			}
			catch
			{
				flag = false;
			}
			if (!flag)
			{
				string path4 = Path.Combine(text, "knowledge", "KnowledgeRules.json");
				if (File.Exists(path4))
				{
					flag = true;
				}
			}
			if (!flag)
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：缺少 knowledge\\rules\\*.json（或 knowledge\\KnowledgeRules.json）"));
				OpenImportFolderPicker(onReturn);
				return;
			}
			MyBehavior myBehavior = Campaign.Current?.GetCampaignBehavior<MyBehavior>();
			if (myBehavior == null)
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：MyBehavior 未初始化。"));
				OpenImportFolderPicker(onReturn);
			}
			else if (!InvokePrivateImport(myBehavior, "ImportPersonaData", folderName))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：无法执行 Hero 个性/背景导入。"));
				OpenImportFolderPicker(onReturn);
			}
			else if (!InvokePrivateImport(myBehavior, "ImportUnnamedPersonaData", folderName))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：无法执行 非Hero 描述导入。"));
				OpenImportFolderPicker(onReturn);
			}
			else if (!InvokePrivateImport(myBehavior, "ImportKnowledgeData", folderName))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：无法执行 知识导入。"));
				OpenImportFolderPicker(onReturn);
			}
			else
			{
				_setupDone = true;
				InformationManager.DisplayMessage(new InformationMessage("首次导入完成：已解锁 Voxforge 对话/场景喊话。"));
				onReturn?.Invoke();
			}
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导入失败：" + ex.Message));
			OpenImportFolderPicker(onReturn);
		}
	}

	private static bool InvokePrivateImport(MyBehavior my, string methodName, string folderName)
	{
		try
		{
			if (my == null)
			{
				return false;
			}
			MethodInfo method = typeof(MyBehavior).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
			if (method == null)
			{
				return false;
			}
			method.Invoke(my, new object[1] { folderName ?? "" });
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static string GetModuleRootPath()
	{
		try
		{
			string location = typeof(SubModule).Assembly.Location;
			string text = (string.IsNullOrEmpty(location) ? "" : Path.GetDirectoryName(Path.GetFullPath(location)));
			DirectoryInfo directoryInfo = (string.IsNullOrEmpty(text) ? null : new DirectoryInfo(text));
			while (directoryInfo != null && directoryInfo.Exists)
			{
				if (File.Exists(Path.Combine(directoryInfo.FullName, "SubModule.xml")))
				{
					return directoryInfo.FullName;
				}
				directoryInfo = directoryInfo.Parent;
			}
		}
		catch
		{
		}
		try
		{
			return Path.GetFullPath(Directory.GetCurrentDirectory());
		}
		catch
		{
			return "";
		}
	}

	private static string GetPlayerExportsRootPath()
	{
		string moduleRootPath = GetModuleRootPath();
		return Path.Combine(moduleRootPath, "PlayerExports");
	}

	private static string SanitizeFolderName(string input)
	{
		string text = (input ?? "").Trim();
		if (string.IsNullOrEmpty(text))
		{
			return "";
		}
		char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
		foreach (char oldChar in invalidFileNameChars)
		{
			text = text.Replace(oldChar, '_');
		}
		return text.Trim();
	}

	private static string FindLatestExportFolder(string root)
	{
		try
		{
			if (!Directory.Exists(root))
			{
				return null;
			}
			DirectoryInfo directoryInfo = new DirectoryInfo(root);
			return (from d in directoryInfo.GetDirectories()
				orderby d.LastWriteTimeUtc descending
				select d).FirstOrDefault()?.FullName;
		}
		catch
		{
			return null;
		}
	}

	private static string ResolveImportFolderPath(string folderName)
	{
		string playerExportsRootPath = GetPlayerExportsRootPath();
		string text = SanitizeFolderName(folderName);
		if (string.IsNullOrEmpty(text))
		{
			return FindLatestExportFolder(playerExportsRootPath);
		}
		return Path.Combine(playerExportsRootPath, text);
	}
}
