using System.Collections.Generic;
using System.Threading.Tasks;
using BUTR.CrashReport.Models;

namespace Bannerlord.ButterLib.CrashUploader;

internal interface ICrashUploader
{
	Task<CrashUploaderResult> UploadAsync(CrashReportModel crashReportModel, IEnumerable<LogSource> logSources);
}
