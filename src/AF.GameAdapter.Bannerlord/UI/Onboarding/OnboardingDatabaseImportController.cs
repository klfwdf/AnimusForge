using System;
using System.IO;
using System.Threading;
using TaleWorlds.Library;

namespace AnimusForge;

internal enum OnboardingImportResult { PendingConfirmation, Completed, Failed, Cancelled, Retired }

// A scoped completion is an event result, not dispatch acknowledgement or persisted-data presence.
internal sealed class OnboardingImportStep
{
    private readonly Func<bool> _isCurrent;
    private readonly Action<OnboardingImportResult> _receive;
    private bool _terminal;
    private readonly int _thread = Thread.CurrentThread.ManagedThreadId;
    internal OnboardingImportStep(Func<bool> isCurrent, Action<OnboardingImportResult> receive)
    { _isCurrent = isCurrent; _receive = receive; }
    internal bool CanApply()
    {
        if (_terminal || Thread.CurrentThread.ManagedThreadId != _thread) return false;
        if (_isCurrent()) return true;
        Finish(OnboardingImportResult.Retired);
        return false;
    }
    internal void Pending() { if (CanApply()) _receive(OnboardingImportResult.PendingConfirmation); }
    internal void Finish(OnboardingImportResult result)
    {
        if (_terminal || Thread.CurrentThread.ManagedThreadId != _thread) return;
        if (result != OnboardingImportResult.Retired && !_isCurrent()) result = OnboardingImportResult.Retired;
        _terminal = true;
        _receive(result);
    }
    internal Action Guard(Action apply) => () =>
    {
        if (!CanApply()) return;
        try { apply(); }
        catch { Finish(OnboardingImportResult.Failed); }
    };
}

internal sealed class OnboardingDatabaseImportController
{
    internal sealed class KingdomInspection
    {
        internal bool Available;
        internal bool Valid;
        internal int Total;
        internal int Skipped;
        internal string Error;
        internal Func<bool> Import;
    }

    internal void Begin(string folderName, Func<bool> isCurrent,
        Func<string, KingdomInspection> inspectKingdom,
        Func<string, Action<OnboardingImportStep>[]> captureImports,
        Func<bool> hasLoadedVoice, Action completed, Action onReturn)
    {

        Retire();
		try
		{
			string text = PlayerExportsStore.ResolveImportFolderPath(folderName);
			if (string.IsNullOrWhiteSpace(text) || !Directory.Exists(text))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到导出目录。"));
				onReturn();
				return;
			}
			string path = Path.Combine(text, "personality_background");
			if (!Directory.Exists(path) || Directory.GetFiles(path, "*.json").Length == 0)
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：缺少 personality_background\\*.json"));
				onReturn();
				return;
			}
			string path2 = Path.Combine(text, "unnamed_persona");
			if (!Directory.Exists(path2) || Directory.GetFiles(path2, "*.json").Length == 0)
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：缺少 unnamed_persona\\*.json"));
				onReturn();
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
				onReturn();
				return;
			}
			string path5 = Path.Combine(text, "voice_mapping", "VoiceMapping.json");
			string path6 = Path.Combine(text, "VoiceMapping.json");
			if (!File.Exists(path5) && !File.Exists(path6))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：缺少 voice_mapping\\VoiceMapping.json。"));
				onReturn();
				return;
			}
			string path7 = Path.Combine(text, "event_data", "WorldOpeningSummary.json");
			string path8 = Path.Combine(text, "event_data", "KingdomOpeningSummaries.json");
			if (!File.Exists(path7) || !File.Exists(path8))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：缺少 event_data\\WorldOpeningSummary.json 或 event_data\\KingdomOpeningSummaries.json。"));
				onReturn();
				return;
			}
			string kingdomProfilesPath = Path.Combine(text, "kingdom_profiles", "KingdomProfiles.json");
			if (!File.Exists(kingdomProfilesPath))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：缺少 kingdom_profiles\\KingdomProfiles.json。"));
				onReturn();
				return;
			}
            if (!isCurrent()) return;
            KingdomInspection kingdom = inspectKingdom(text);
            if (kingdom == null || !kingdom.Available)
            {
                InformationManager.DisplayMessage(new InformationMessage("导入失败：国家战略与性格数据行为未初始化。"));
                onReturn(); return;
            }
            if (!kingdom.Valid || kingdom.Total <= 0)
            {
                string reason = string.IsNullOrWhiteSpace(kingdom.Error)
					? "资料包中没有与当前世界安全匹配的国家卡。"
					: kingdom.Error;
				InformationManager.DisplayMessage(new InformationMessage("导入失败：国家战略与性格资料无效。原因：" + reason));
                onReturn(); return;
            }
            Action<OnboardingImportStep>[] imports = captureImports(text);
            if (imports == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("导入失败：MyBehavior 未初始化。"));
                onReturn();
                return;
            }
            Run(isCurrent, imports, () => kingdom.Import() && hasLoadedVoice(), () =>
            {
                InformationManager.DisplayMessage(new InformationMessage("国家战略与性格已从资料包导入：匹配 " + kingdom.Total + " 条；无法匹配 " + kingdom.Skipped + " 条。"));
                completed();
            }, result =>
            {
                if (result == OnboardingImportResult.Retired || !isCurrent()) return;
                InformationManager.DisplayMessage(new InformationMessage(result == OnboardingImportResult.Cancelled ? "已取消导入。" : "导入失败：所需资料未完成导入。"));
                onReturn();
            });
        }
        catch (Exception ex)
        {
            if (!isCurrent()) return;
            InformationManager.DisplayMessage(new InformationMessage("导入失败：" + ex.Message));
            onReturn();
        }
    }

    private long _operation;
    internal void Retire() { ++_operation; }
    // Main-thread event continuation. Previously committed domains deliberately remain committed.
    internal void Run(Func<bool> isCurrent, Action<OnboardingImportStep>[] imports,
        Func<bool> finishKingdomAndCheckVoice, Action completed, Action<OnboardingImportResult> rejected)
    {
        long operation = ++_operation;
        bool terminal = false;
        Func<bool> current = () => operation == _operation && isCurrent();
        Action<OnboardingImportResult> reject = result =>
        {
            if (terminal) return;
            terminal = true;
            rejected(result);
        };
        Action<int> advance = null;
        advance = index =>
        {
            if (terminal) return;
            if (!current()) { reject(OnboardingImportResult.Retired); return; }
            try
            {
                if (index == imports.Length)
                {
                    if (!finishKingdomAndCheckVoice()) { reject(OnboardingImportResult.Failed); return; }
                    if (!current()) { reject(OnboardingImportResult.Retired); return; }
                    terminal = true;
                    completed();
                    return;
                }
                var step = new OnboardingImportStep(current, result =>
                {
                    if (result == OnboardingImportResult.PendingConfirmation) return;
                    if (result == OnboardingImportResult.Completed) advance(index + 1);
                    else reject(result);
                });
                imports[index](step);
            }
            catch { reject(OnboardingImportResult.Failed); }
        };
        advance(0);
    }
}
