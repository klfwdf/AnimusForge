using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using TaleWorlds.DotNet;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AnimusForge.Bootstrap
{
    internal enum BannerlordApiLine
    {
        V13,
        V14
    }

    internal sealed class BootstrapRuntime : IDisposable
    {
        private const string ImplementationAssemblyName = "AnimusForge";
        private const string ImplementationAssemblyFileName = "AnimusForge.dll";
        private const string ImplementationTypeName = "AnimusForge.SubModule";
        private const string UnifiedModuleId = "AnimusForge";
        private const string BootstrapAssemblyFileName = "AnimusForge.Bootstrap.dll";
        private const string BootstrapSubModuleTypeName = "AnimusForge.Bootstrap.BootstrapSubModule";
        private const string ModuleHelperTypeName = "TaleWorlds.ModuleManager.ModuleHelper, TaleWorlds.ModuleManager";
        private const string OnnxRuntimeAssemblyName = "Microsoft.ML.OnnxRuntime";
        private const string ApiMetadataKey = "AnimusForge.BannerlordApi";

        private static readonly string[] PrivateManagedDependencyLoadOrder =
        {
            "System.Runtime.CompilerServices.Unsafe",
            "System.Buffers",
            "System.Memory",
            "Microsoft.ML.OnnxRuntime"
        };

        private static readonly HashSet<string> PrivateDependencyAssemblyNames =
            new HashSet<string>(PrivateManagedDependencyLoadOrder, StringComparer.OrdinalIgnoreCase);

        private static readonly string[] PrivateNativeDependencyFileNames =
        {
            "onnxruntime.dll",
            "onnxruntime_providers_shared.dll"
        };

        private static int _fatalMessageShown;

        private readonly Dictionary<string, MethodInfo> _lifecycleMethods = new Dictionary<string, MethodInfo>(StringComparer.Ordinal);
        private readonly object _resolverOwnedPathsLock = new object();
        private readonly HashSet<string> _resolverOwnedAssemblyPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private ResolveEventHandler _assemblyResolveHandler;
        private string _selectedImplementationDirectory;
        private string _selectedImplementationPath;
        private string _binDirectory;
        private Assembly _implementationAssembly;
        private MBSubModuleBase _implementation;
        private IList _activeAssemblyRegistrationList;
        private bool _activeAssemblyRegistrationAdded;

        internal string RuntimeVersionText { get; private set; } = "unknown";

        internal string SelectedApiText { get; private set; } = "unknown";

        internal string SelectedImplementationPath => _selectedImplementationPath ?? "not selected";

        internal MBSubModuleBase LoadImplementation()
        {
            if (_implementation != null)
            {
                throw new InvalidOperationException("AnimusForge implementation has already been loaded by this Bootstrap instance.");
            }

            _binDirectory = GetBootstrapBinDirectory();
            BootstrapLog.Initialize(_binDirectory);

            BannerlordApiLine apiLine = DetectRuntimeApiLine();
            SelectedApiText = apiLine == BannerlordApiLine.V13 ? "1.3" : "1.4";
            string versionFolder = apiLine == BannerlordApiLine.V13 ? "1.3" : "1.4";

            _selectedImplementationDirectory = Path.GetFullPath(Path.Combine(_binDirectory, "versions", versionFolder));
            _selectedImplementationPath = Path.GetFullPath(Path.Combine(_selectedImplementationDirectory, ImplementationAssemblyFileName));

            BootstrapLog.Info($"Game version={RuntimeVersionText}; selected API={SelectedApiText}; implementation={_selectedImplementationPath}");

            ValidateImplementationFile(_selectedImplementationPath);
            RejectConflictingLoadedImplementation(_selectedImplementationPath);
            InstallAssemblyResolver();
            PreloadPrivateManagedDependencies();

            Assembly assembly = FindAlreadyLoadedAssemblyAtPath(_selectedImplementationPath)
                ?? Assembly.LoadFrom(_selectedImplementationPath);

            string actualLocation = GetAssemblyLocation(assembly);
            if (!PathsEqual(actualLocation, _selectedImplementationPath))
            {
                throw new InvalidOperationException(
                    $"CLR returned an unexpected AnimusForge assembly. Expected '{_selectedImplementationPath}', actual '{actualLocation}'. " +
                    "Disable and remove every legacy AnimusForge module before starting the game.");
            }

            ValidateApiMetadata(assembly, SelectedApiText);

            Type implementationType = assembly.GetType(ImplementationTypeName, throwOnError: true, ignoreCase: false);
            if (implementationType == null || implementationType.IsAbstract || !typeof(MBSubModuleBase).IsAssignableFrom(implementationType))
            {
                throw new TypeLoadException(
                    $"Type '{ImplementationTypeName}' in '{_selectedImplementationPath}' is not a concrete {typeof(MBSubModuleBase).FullName}.");
            }

            _implementationAssembly = assembly;
            RegisterImplementationAsActiveGameAssembly();
            RegisterImplementationManagedTypes(assembly);

            object instance = Activator.CreateInstance(implementationType);
            _implementation = instance as MBSubModuleBase;
            if (_implementation == null)
            {
                throw new InvalidCastException($"Could not instantiate '{ImplementationTypeName}' as {typeof(MBSubModuleBase).FullName}.");
            }

            CacheLifecycleMethods(implementationType);

            BootstrapLog.Info(
                $"Loaded implementation '{assembly.FullName}' from '{actualLocation}', " +
                $"file version={GetImplementationFileVersion(_selectedImplementationPath)}, " +
                $"{GetBuildMarkerSummary(_selectedImplementationPath)}, " +
                $"SHA-256={ComputeSha256(_selectedImplementationPath)}.");

            return _implementation;
        }

        internal object InvokeLifecycle(string methodName, params object[] arguments)
        {
            if (_implementation == null)
            {
                throw new InvalidOperationException(
                    $"Cannot forward lifecycle '{methodName}': the AnimusForge implementation is not loaded.");
            }

            if (!_lifecycleMethods.TryGetValue(methodName, out MethodInfo method))
            {
                throw new MissingMethodException(_implementation.GetType().FullName, methodName);
            }

            try
            {
                return method.Invoke(_implementation, arguments);
            }
            catch (TargetInvocationException exception) when (exception.InnerException != null)
            {
                Exception inner = exception.InnerException;
                BootstrapLog.Error(
                    $"Implementation lifecycle '{methodName}' failed: {inner.GetType().FullName}: {inner.Message}{Environment.NewLine}{inner}");
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(inner).Throw();
                throw;
            }
        }

        internal void ReportFatal(string stage, Exception exception)
        {
            string message = BuildFatalMessage(stage, exception);
            BootstrapLog.Error(message + Environment.NewLine + exception);

            try
            {
                TaleWorlds.Library.Debug.SetCrashReportCustomString(message + Environment.NewLine + exception);
            }
            catch
            {
                // The standalone UTF-8 log remains the source of truth.
            }

            if (Interlocked.Exchange(ref _fatalMessageShown, 1) != 0)
            {
                return;
            }

            try
            {
                // Some Bannerlord builds pass this native dialog through a legacy
                // code page and turn Chinese UTF-8 text into mojibake.  Keep the
                // authoritative file/crash log bilingual, but make the emergency
                // dialog ASCII-only and include the innermost actionable error.
                TaleWorlds.Library.Debug.ShowMessageBox(
                    BuildFatalDialogMessage(stage, exception),
                    "AnimusForge Bootstrap Error",
                    4u);
            }
            catch
            {
                // Throwing from OnSubModuleLoad still makes the failure visible to Bannerlord.
            }
        }

        public void Dispose()
        {
            if (_activeAssemblyRegistrationAdded && _activeAssemblyRegistrationList != null)
            {
                _activeAssemblyRegistrationList.Remove(ImplementationAssemblyFileName);
                _activeAssemblyRegistrationAdded = false;
                _activeAssemblyRegistrationList = null;
            }

            if (_assemblyResolveHandler != null)
            {
                AppDomain.CurrentDomain.AssemblyResolve -= _assemblyResolveHandler;
                _assemblyResolveHandler = null;
            }
        }

        private BannerlordApiLine DetectRuntimeApiLine()
        {
            bool versionReadSucceeded = false;

            try
            {
                ApplicationVersion version = ApplicationVersion.FromParametersFile();
                RuntimeVersionText = version.ToString();

                if (version.Major >= 0 && version.Minor >= 0)
                {
                    versionReadSucceeded = true;
                    if (version.Major == 1 && version.Minor == 3)
                    {
                        return BannerlordApiLine.V13;
                    }

                    if (version.Major == 1 && version.Minor == 4)
                    {
                        return BannerlordApiLine.V14;
                    }
                }
            }
            catch (Exception exception)
            {
                BootstrapLog.Warning(
                    $"ApplicationVersion.FromParametersFile failed; BuildInfo will be inspected. {exception.GetType().Name}: {exception.Message}");
            }

            if (versionReadSucceeded)
            {
                throw new NotSupportedException(
                    $"Bannerlord version '{RuntimeVersionText}' is unsupported. This package only supports the 1.3.x and 1.4.x game lines.");
            }

            BannerlordApiLine? buildInfoLine = DetectRuntimeApiLineFromBuildInfo();
            if (buildInfoLine.HasValue)
            {
                return buildInfoLine.Value;
            }

            return DetectRuntimeApiLineFromFeature();
        }

        private BannerlordApiLine? DetectRuntimeApiLineFromBuildInfo()
        {
            Type buildInfoType;
            try
            {
                buildInfoType = typeof(ApplicationVersion).Assembly.GetType("BuildInfo", throwOnError: false, ignoreCase: false);
            }
            catch (Exception exception)
            {
                BootstrapLog.Warning($"Could not inspect TaleWorlds.Library BuildInfo: {exception.GetType().Name}: {exception.Message}");
                return null;
            }

            if (buildInfoType == null)
            {
                BootstrapLog.Warning("TaleWorlds.Library BuildInfo type is unavailable.");
                return null;
            }

            FieldInfo gameVersionField = buildInfoType.GetField(
                "GameVersion",
                BindingFlags.Public | BindingFlags.Static);
            if (gameVersionField == null || gameVersionField.FieldType != typeof(string))
            {
                BootstrapLog.Warning("TaleWorlds.Library BuildInfo.GameVersion is unavailable.");
                return null;
            }

            string buildVersionText;
            try
            {
                buildVersionText = (gameVersionField.IsLiteral
                    ? gameVersionField.GetRawConstantValue()
                    : gameVersionField.GetValue(null)) as string;
            }
            catch (Exception exception)
            {
                BootstrapLog.Warning($"Could not read BuildInfo.GameVersion: {exception.GetType().Name}: {exception.Message}");
                return null;
            }

            if (string.IsNullOrWhiteSpace(buildVersionText))
            {
                BootstrapLog.Warning("BuildInfo.GameVersion is empty.");
                return null;
            }

            ApplicationVersion buildVersion;
            try
            {
                buildVersion = ApplicationVersion.FromString(buildVersionText);
            }
            catch (Exception exception)
            {
                throw new NotSupportedException(
                    $"BuildInfo.GameVersion '{buildVersionText}' could not be parsed safely.", exception);
            }

            RuntimeVersionText = buildVersionText + " (BuildInfo)";
            if (buildVersion.Major == 1 && buildVersion.Minor == 3)
            {
                BootstrapLog.Warning("Selected Bannerlord API 1.3 from BuildInfo.GameVersion fallback.");
                return BannerlordApiLine.V13;
            }

            if (buildVersion.Major == 1 && buildVersion.Minor == 4)
            {
                BootstrapLog.Warning("Selected Bannerlord API 1.4 from BuildInfo.GameVersion fallback.");
                return BannerlordApiLine.V14;
            }

            throw new NotSupportedException(
                $"BuildInfo.GameVersion '{buildVersionText}' is unsupported. This package only supports the 1.3.x and 1.4.x game lines.");
        }

        private BannerlordApiLine DetectRuntimeApiLineFromFeature()
        {
            const string typeName =
                "TaleWorlds.CampaignSystem.ComponentInterfaces.MobilePartyAIModel, TaleWorlds.CampaignSystem";

            Type mobilePartyAiModel;
            try
            {
                mobilePartyAiModel = Type.GetType(typeName, throwOnError: false);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(
                    "Could not inspect the Bannerlord CampaignSystem API after the version file was unavailable.", exception);
            }

            if (mobilePartyAiModel == null)
            {
                throw new InvalidOperationException(
                    "Could not determine the Bannerlord API line: the version file was unavailable and MobilePartyAIModel could not be resolved.");
            }

            bool hasOneFourFeature = mobilePartyAiModel.GetProperty(
                "FortificationPortPatrolDistanceAsDays",
                BindingFlags.Instance | BindingFlags.Public) != null;

            if (!hasOneFourFeature)
            {
                throw new NotSupportedException(
                    "Could not determine a supported Bannerlord API line: no authoritative version was available, " +
                    "and the 1.4-only MobilePartyAIModel feature was absent. Bootstrap will not guess that this is 1.3.");
            }

            RuntimeVersionText = "unknown (1.4 API feature detected)";
            BootstrapLog.Warning("Selected Bannerlord API 1.4 by positive feature fallback.");
            return BannerlordApiLine.V14;
        }

        private static string GetBootstrapBinDirectory()
        {
            string assemblyLocation = typeof(BootstrapRuntime).Assembly.Location;
            if (string.IsNullOrWhiteSpace(assemblyLocation))
            {
                throw new InvalidOperationException("AnimusForge.Bootstrap.dll has no physical assembly location.");
            }

            string directory = Path.GetDirectoryName(Path.GetFullPath(assemblyLocation));
            if (string.IsNullOrWhiteSpace(directory))
            {
                throw new InvalidOperationException($"Could not resolve the Bootstrap bin directory from '{assemblyLocation}'.");
            }

            return directory;
        }

        private static void ValidateImplementationFile(string implementationPath)
        {
            if (!File.Exists(implementationPath))
            {
                throw new FileNotFoundException(
                    "The implementation DLL selected for this Bannerlord version is missing. " +
                    "Reinstall the unified AnimusForge package; Bootstrap will not load the other version as a fallback.",
                    implementationPath);
            }

            AssemblyName assemblyName;
            try
            {
                assemblyName = AssemblyName.GetAssemblyName(implementationPath);
            }
            catch (Exception exception)
            {
                throw new BadImageFormatException($"'{implementationPath}' is not a readable managed DLL.", exception);
            }

            if (!string.Equals(assemblyName.Name, ImplementationAssemblyName, StringComparison.Ordinal))
            {
                throw new BadImageFormatException(
                    $"Expected implementation assembly name '{ImplementationAssemblyName}', found '{assemblyName.Name}' in '{implementationPath}'.");
            }
        }

        private static void ValidateApiMetadata(Assembly assembly, string expectedApi)
        {
            List<string> values = new List<string>();
            foreach (CustomAttributeData attribute in assembly.GetCustomAttributesData())
            {
                if (!string.Equals(attribute.AttributeType.FullName, typeof(AssemblyMetadataAttribute).FullName, StringComparison.Ordinal) ||
                    attribute.ConstructorArguments.Count != 2)
                {
                    continue;
                }

                string key = attribute.ConstructorArguments[0].Value as string;
                if (string.Equals(key, ApiMetadataKey, StringComparison.Ordinal))
                {
                    values.Add(attribute.ConstructorArguments[1].Value as string);
                }
            }

            if (values.Count != 1 || !string.Equals(values[0], expectedApi, StringComparison.Ordinal))
            {
                string actual = values.Count == 0 ? "missing" : string.Join(", ", values.Select(value => value ?? "<null>"));
                throw new InvalidOperationException(
                    $"Implementation API marker mismatch. Expected {ApiMetadataKey}='{expectedApi}', actual '{actual}'. " +
                    "The package may contain a swapped or stale DLL; the other implementation will not be attempted.");
            }
        }

        private static void RegisterImplementationManagedTypes(Assembly assembly)
        {
            Type[] assemblyTypes;
            try
            {
                assemblyTypes = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException exception)
            {
                string loaderErrors = string.Join(
                    Environment.NewLine,
                    exception.LoaderExceptions
                        .Where(loaderException => loaderException != null)
                        .Select(loaderException => loaderException.GetType().Name + ": " + loaderException.Message));

                throw new InvalidOperationException(
                    "Failed to enumerate all implementation types before TaleWorlds managed-type registration." +
                    (string.IsNullOrWhiteSpace(loaderErrors)
                        ? string.Empty
                        : Environment.NewLine + "Loader errors:" + Environment.NewLine + loaderErrors),
                    exception);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(
                    "Failed to enumerate implementation types before TaleWorlds managed-type registration.",
                    exception);
            }

            Dictionary<string, Type> managedTypes = new Dictionary<string, Type>();
            foreach (Type type in assemblyTypes)
            {
                if (!typeof(ManagedObject).IsAssignableFrom(type) && !typeof(DotNetObject).IsAssignableFrom(type))
                {
                    continue;
                }

                if (managedTypes.TryGetValue(type.Name, out Type existing))
                {
                    throw new InvalidOperationException(
                        $"Implementation managed-type name collision for '{type.Name}': '{existing.FullName}' and '{type.FullName}'.");
                }

                managedTypes.Add(type.Name, type);
            }

            try
            {
                Managed.AddTypes(managedTypes);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(
                    $"TaleWorlds managed-type registration failed for {managedTypes.Count} implementation type(s).",
                    exception);
            }

            BootstrapLog.Info($"Registered {managedTypes.Count} implementation ManagedObject/DotNetObject type(s) with TaleWorlds.DotNet.Managed.");
        }

        private void RejectConflictingLoadedImplementation(string expectedPath)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                string simpleName;
                try
                {
                    simpleName = assembly.GetName().Name;
                }
                catch
                {
                    continue;
                }

                if (!string.Equals(simpleName, ImplementationAssemblyName, StringComparison.Ordinal))
                {
                    continue;
                }

                string location = GetAssemblyLocation(assembly);
                if (!PathsEqual(location, expectedPath))
                {
                    throw new InvalidOperationException(
                        $"A conflicting AnimusForge implementation is already loaded from '{location}'. " +
                        $"Expected only '{expectedPath}'. Disable and remove all legacy AnimusForge_1_3_x / AnimusForge_1_4_5 modules.");
                }
            }
        }

        private static Assembly FindAlreadyLoadedAssemblyAtPath(string expectedPath)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (PathsEqual(GetAssemblyLocation(assembly), expectedPath))
                {
                    return assembly;
                }
            }

            return null;
        }

        private void PreloadPrivateManagedDependencies()
        {
            foreach (string simpleName in PrivateManagedDependencyLoadOrder)
            {
                string expectedPath = Path.GetFullPath(Path.Combine(_binDirectory, simpleName + ".dll"));
                if (!File.Exists(expectedPath))
                {
                    throw new FileNotFoundException(
                        $"Required AnimusForge private runtime dependency '{simpleName}' is missing from the unified module bin.",
                        expectedPath);
                }

                AssemblyName expectedIdentity;
                try
                {
                    expectedIdentity = AssemblyName.GetAssemblyName(expectedPath);
                }
                catch (Exception exception)
                {
                    throw new BadImageFormatException(
                        $"Private runtime dependency '{expectedPath}' is not a readable managed DLL.", exception);
                }

                if (!string.Equals(expectedIdentity.Name, simpleName, StringComparison.Ordinal))
                {
                    throw new BadImageFormatException(
                        $"Private runtime dependency identity mismatch. Expected '{simpleName}', " +
                        $"found '{expectedIdentity.Name}' in '{expectedPath}'.");
                }

                Assembly alreadyLoaded = FindLoadedAssemblyByIdentity(expectedIdentity);
                if (alreadyLoaded != null)
                {
                    string loadedLocation = GetAssemblyLocation(alreadyLoaded);
                    if (PathsEqual(loadedLocation, expectedPath))
                    {
                        AddResolverOwnedAssemblyPath(loadedLocation);
                        BootstrapLog.Info(
                            $"Reusing private dependency '{alreadyLoaded.FullName}' already loaded from '{loadedLocation}'.");
                    }
                    else
                    {
                        if (string.Equals(simpleName, OnnxRuntimeAssemblyName, StringComparison.OrdinalIgnoreCase))
                        {
                            throw new InvalidOperationException(
                                $"A conflicting '{OnnxRuntimeAssemblyName}' assembly is already loaded from '{loadedLocation}'. " +
                                $"AnimusForge requires the managed/native ONNX pair shipped in '{_binDirectory}'.");
                        }

                        // Strong-named framework compatibility assemblies can be unified by
                        // the CLR to an exact-identity copy in the GAC or another load context.
                        // FindLoadedAssemblyByIdentity already verified name, version, culture,
                        // and public-key token, so rejecting that copy only because its path is
                        // different breaks otherwise compatible player machines.  ONNX remains
                        // path-strict above because its managed/native binaries must stay paired.
                        BootstrapLog.Warning(
                            $"Reusing exact private dependency identity '{alreadyLoaded.FullName}' already loaded outside " +
                            $"the AnimusForge module bin from '{loadedLocation}'.");
                    }
                    continue;
                }

                AddResolverOwnedAssemblyPath(expectedPath);
                try
                {
                    Assembly loaded = Assembly.LoadFrom(expectedPath);
                    AssemblyName actualIdentity = loaded.GetName();
                    string actualLocation = GetAssemblyLocation(loaded);
                    if (!AssemblyIdentityMatches(expectedIdentity, actualIdentity))
                    {
                        throw new FileLoadException(
                            $"CLR returned the wrong identity for private dependency '{simpleName}'. " +
                            $"Expected '{expectedIdentity.FullName}', actual '{actualIdentity.FullName}'.",
                            expectedPath);
                    }

                    if (!PathsEqual(actualLocation, expectedPath))
                    {
                        if (string.Equals(simpleName, OnnxRuntimeAssemblyName, StringComparison.OrdinalIgnoreCase))
                        {
                            throw new FileLoadException(
                                $"CLR returned private dependency '{simpleName}' from an unexpected path. " +
                                $"Expected '{expectedPath}', actual '{actualLocation}'.",
                                expectedPath);
                        }

                        // AssemblyIdentityMatches above is intentionally strict.  When the CLR
                        // unifies an exact strong-name identity to the GAC, use that compatible
                        // assembly.  Do not mark the shared GAC/runtime path as resolver-owned;
                        // implementation requests can still reuse it by exact AppDomain identity
                        // without widening AnimusForge's private resolution scope.
                        RemoveResolverOwnedAssemblyPath(expectedPath);
                        BootstrapLog.Warning(
                            $"CLR unified exact private dependency identity '{loaded.FullName}' from packaged path " +
                            $"'{expectedPath}' to compatible runtime path '{actualLocation}'.");
                    }
                    else
                    {
                        BootstrapLog.Info(
                            $"Preloaded private dependency '{loaded.FullName}' from '{actualLocation}'.");
                    }
                }
                catch (Exception exception)
                {
                    RemoveResolverOwnedAssemblyPath(expectedPath);
                    throw new InvalidOperationException(
                        $"Failed to preload required AnimusForge private dependency '{simpleName}' from '{expectedPath}'.",
                        exception);
                }
            }

            foreach (string fileName in PrivateNativeDependencyFileNames)
            {
                string expectedPath = Path.GetFullPath(Path.Combine(_binDirectory, fileName));
                if (!File.Exists(expectedPath))
                {
                    throw new FileNotFoundException(
                        $"Required AnimusForge native runtime dependency '{fileName}' is missing from the unified module bin.",
                        expectedPath);
                }
            }

            BootstrapLog.Info(
                $"Private dependency check completed: preloaded/reused " +
                $"{PrivateManagedDependencyLoadOrder.Length} managed assemblies and verified " +
                $"{PrivateNativeDependencyFileNames.Length} native files.");
        }

        private void RegisterImplementationAsActiveGameAssembly()
        {
            if (_implementationAssembly == null)
            {
                throw new InvalidOperationException(
                    "Cannot register the selected implementation as active before its assembly has loaded.");
            }

            Type moduleHelperType = Type.GetType(ModuleHelperTypeName, throwOnError: false);
            if (moduleHelperType == null)
            {
                throw new TypeLoadException($"Required runtime type '{ModuleHelperTypeName}' is unavailable.");
            }

            MethodInfo getModuleInfo = moduleHelperType.GetMethod(
                "GetModuleInfo",
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types: new[] { typeof(string) },
                modifiers: null);
            if (getModuleInfo == null)
            {
                throw new MissingMethodException(moduleHelperType.FullName, "GetModuleInfo(string)");
            }

            object moduleInfo = InvokeRequiredStaticMethod(getModuleInfo, UnifiedModuleId);
            if (moduleInfo == null)
            {
                throw new InvalidOperationException(
                    $"The active module registry does not contain the unified module id '{UnifiedModuleId}'.");
            }

            object isActiveValue = GetRequiredPublicMemberValue(moduleInfo, "IsActive");
            if (!(isActiveValue is bool isActive) || !isActive)
            {
                throw new InvalidOperationException(
                    $"The unified module '{UnifiedModuleId}' is not active while its Bootstrap is loading.");
            }

            IEnumerable subModules = GetRequiredPublicMemberValue(moduleInfo, "SubModules") as IEnumerable;
            if (subModules == null)
            {
                throw new InvalidOperationException(
                    $"Module '{UnifiedModuleId}' did not expose an enumerable SubModules collection.");
            }

            object bootstrapSubModuleInfo = null;
            foreach (object subModuleInfo in subModules)
            {
                if (subModuleInfo == null)
                {
                    continue;
                }

                string dllName = GetRequiredPublicMemberValue(subModuleInfo, "DLLName") as string;
                string classTypeName = GetRequiredPublicMemberValue(subModuleInfo, "SubModuleClassTypeName") as string;
                if (string.Equals(dllName, BootstrapAssemblyFileName, StringComparison.Ordinal) &&
                    string.Equals(classTypeName, BootstrapSubModuleTypeName, StringComparison.Ordinal))
                {
                    bootstrapSubModuleInfo = subModuleInfo;
                    break;
                }
            }

            if (bootstrapSubModuleInfo == null)
            {
                throw new InvalidOperationException(
                    $"Could not find the '{BootstrapSubModuleTypeName}' entry in module '{UnifiedModuleId}'.");
            }

            IList assemblies = GetRequiredPublicMemberValue(bootstrapSubModuleInfo, "Assemblies") as IList;
            if (assemblies == null || assemblies.IsReadOnly || assemblies.IsFixedSize)
            {
                throw new InvalidOperationException(
                    "The Bootstrap SubModuleInfo.Assemblies collection is unavailable or cannot be updated in memory.");
            }

            bool added = false;
            try
            {
                if (!assemblies.Contains(ImplementationAssemblyFileName))
                {
                    assemblies.Add(ImplementationAssemblyFileName);
                    added = true;
                    _activeAssemblyRegistrationList = assemblies;
                    _activeAssemblyRegistrationAdded = true;
                }

                MethodInfo getActiveGameAssemblies = moduleHelperType.GetMethod(
                    "GetActiveGameAssemblies",
                    BindingFlags.Public | BindingFlags.Static,
                    binder: null,
                    types: Type.EmptyTypes,
                    modifiers: null);
                if (getActiveGameAssemblies == null)
                {
                    throw new MissingMethodException(moduleHelperType.FullName, "GetActiveGameAssemblies()");
                }

                IEnumerable activeAssemblies = InvokeRequiredStaticMethod(getActiveGameAssemblies) as IEnumerable;
                if (activeAssemblies == null)
                {
                    throw new InvalidOperationException("ModuleHelper.GetActiveGameAssemblies returned no enumerable result.");
                }

                bool implementationIsActive = false;
                foreach (object item in activeAssemblies)
                {
                    if (item is Assembly activeAssembly &&
                        (ReferenceEquals(activeAssembly, _implementationAssembly) ||
                         PathsEqual(GetAssemblyLocation(activeAssembly), _selectedImplementationPath)))
                    {
                        implementationIsActive = true;
                        break;
                    }
                }

                if (!implementationIsActive)
                {
                    throw new InvalidOperationException(
                        "The selected AnimusForge implementation was not returned by ModuleHelper.GetActiveGameAssemblies " +
                        "after its in-memory registration.");
                }

                BootstrapLog.Info(
                    "Registered the selected implementation in the in-memory active-game assembly list; " +
                    "SubModule.xml remains Bootstrap-only.");
            }
            catch
            {
                if (added)
                {
                    assemblies.Remove(ImplementationAssemblyFileName);
                    _activeAssemblyRegistrationList = null;
                    _activeAssemblyRegistrationAdded = false;
                }

                throw;
            }
        }

        private static Assembly FindLoadedAssemblyByIdentity(AssemblyName expectedIdentity)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    if (AssemblyIdentityMatches(expectedIdentity, assembly.GetName()))
                    {
                        return assembly;
                    }
                }
                catch
                {
                    // Continue past dynamic or partially initialized assemblies.
                }
            }

            return null;
        }

        private void AddResolverOwnedAssemblyPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || path.StartsWith("<", StringComparison.Ordinal))
            {
                return;
            }

            lock (_resolverOwnedPathsLock)
            {
                _resolverOwnedAssemblyPaths.Add(Path.GetFullPath(path));
            }
        }

        private void RemoveResolverOwnedAssemblyPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || path.StartsWith("<", StringComparison.Ordinal))
            {
                return;
            }

            lock (_resolverOwnedPathsLock)
            {
                _resolverOwnedAssemblyPaths.Remove(Path.GetFullPath(path));
            }
        }

        private static object GetRequiredPublicMemberValue(object instance, string memberName)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            PropertyInfo property = instance.GetType().GetProperty(
                memberName,
                BindingFlags.Instance | BindingFlags.Public);
            if (property != null && property.CanRead)
            {
                return property.GetValue(instance, null);
            }

            FieldInfo field = instance.GetType().GetField(
                memberName,
                BindingFlags.Instance | BindingFlags.Public);
            if (field != null)
            {
                return field.GetValue(instance);
            }

            throw new MissingMemberException(instance.GetType().FullName, memberName);
        }

        private static object InvokeRequiredStaticMethod(MethodInfo method, params object[] arguments)
        {
            try
            {
                return method.Invoke(null, arguments);
            }
            catch (TargetInvocationException exception) when (exception.InnerException != null)
            {
                throw new InvalidOperationException(
                    $"Runtime method '{method.DeclaringType?.FullName}.{method.Name}' failed.",
                    exception.InnerException);
            }
        }

        private void InstallAssemblyResolver()
        {
            if (_assemblyResolveHandler != null)
            {
                return;
            }

            _assemblyResolveHandler = ResolveImplementationDependency;
            AppDomain.CurrentDomain.AssemblyResolve += _assemblyResolveHandler;
        }

        private Assembly ResolveImplementationDependency(object sender, ResolveEventArgs eventArgs)
        {
            AssemblyName requested;
            try
            {
                requested = new AssemblyName(eventArgs.Name);
            }
            catch
            {
                return null;
            }

            string simpleName = requested.Name;
            if (string.IsNullOrWhiteSpace(simpleName) || simpleName.EndsWith(".resources", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (!IsResolverOwnedRequester(eventArgs.RequestingAssembly))
            {
                return null;
            }

            if (IsGameManagedAssembly(simpleName))
            {
                return null;
            }

            bool isImplementationRequest = string.Equals(simpleName, ImplementationAssemblyName, StringComparison.OrdinalIgnoreCase);
            if (!isImplementationRequest && !PrivateDependencyAssemblyNames.Contains(simpleName))
            {
                return null;
            }

            foreach (Assembly loaded in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    if (AssemblyIdentityMatches(requested, loaded.GetName()))
                    {
                        if (!isImplementationRequest ||
                            PathsEqual(GetAssemblyLocation(loaded), _selectedImplementationPath))
                        {
                            return loaded;
                        }
                    }
                }
                catch
                {
                    // Keep looking for a physical dependency.
                }
            }

            if (isImplementationRequest)
            {
                return _implementationAssembly != null && AssemblyIdentityMatches(requested, _implementationAssembly.GetName())
                    ? _implementationAssembly
                    : null;
            }

            foreach (string directory in new[] { _selectedImplementationDirectory, _binDirectory })
            {
                string candidate = Path.GetFullPath(Path.Combine(directory, simpleName + ".dll"));
                if (!File.Exists(candidate))
                {
                    continue;
                }

                try
                {
                    AssemblyName candidateName = AssemblyName.GetAssemblyName(candidate);
                    if (!AssemblyIdentityMatches(requested, candidateName))
                    {
                        continue;
                    }

                    BootstrapLog.Info($"Resolving implementation dependency '{eventArgs.Name}' from '{candidate}'.");
                    Assembly resolved = Assembly.LoadFrom(candidate);
                    string resolvedLocation = GetAssemblyLocation(resolved);
                    if (PathsEqual(resolvedLocation, candidate))
                    {
                        AddResolverOwnedAssemblyPath(resolvedLocation);
                    }
                    return resolved;
                }
                catch (Exception exception)
                {
                    BootstrapLog.Warning(
                        $"Failed to resolve dependency '{eventArgs.Name}' from '{candidate}': {exception.GetType().Name}: {exception.Message}");
                }
            }

            return null;
        }

        private bool IsResolverOwnedRequester(Assembly requestingAssembly)
        {
            if (requestingAssembly == null)
            {
                return false;
            }

            string requestingPath = GetAssemblyLocation(requestingAssembly);
            if (PathsEqual(requestingPath, _selectedImplementationPath))
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(requestingPath) || requestingPath.StartsWith("<", StringComparison.Ordinal))
            {
                return false;
            }

            try
            {
                lock (_resolverOwnedPathsLock)
                {
                    return _resolverOwnedAssemblyPaths.Contains(Path.GetFullPath(requestingPath));
                }
            }
            catch
            {
                return false;
            }
        }

        private static bool AssemblyIdentityMatches(AssemblyName requested, AssemblyName candidate)
        {
            if (requested == null || candidate == null ||
                !string.Equals(requested.Name, candidate.Name, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (requested.Version != null && candidate.Version != null && requested.Version != candidate.Version)
            {
                return false;
            }

            string requestedCulture = requested.CultureName ?? string.Empty;
            string candidateCulture = candidate.CultureName ?? string.Empty;
            if (!string.Equals(requestedCulture, candidateCulture, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            byte[] requestedToken = requested.GetPublicKeyToken() ?? Array.Empty<byte>();
            byte[] candidateToken = candidate.GetPublicKeyToken() ?? Array.Empty<byte>();
            return requestedToken.SequenceEqual(candidateToken);
        }

        private static bool IsGameManagedAssembly(string simpleName)
        {
            // These assemblies are private AnimusForge runtime dependencies kept beside Bootstrap.
            // They intentionally override the broad System.* guard below so an implementation
            // loaded from versions/1.3 or versions/1.4 can still resolve them from the module bin.
            if (PrivateDependencyAssemblyNames.Contains(simpleName))
            {
                return false;
            }

            return simpleName.Equals("mscorlib", StringComparison.OrdinalIgnoreCase) ||
                   simpleName.Equals("netstandard", StringComparison.OrdinalIgnoreCase) ||
                   simpleName.Equals("Native", StringComparison.OrdinalIgnoreCase) ||
                   simpleName.StartsWith("System", StringComparison.OrdinalIgnoreCase) ||
                   simpleName.StartsWith("TaleWorlds.", StringComparison.OrdinalIgnoreCase) ||
                   simpleName.Equals("SandBox", StringComparison.OrdinalIgnoreCase) ||
                   simpleName.StartsWith("SandBox.", StringComparison.OrdinalIgnoreCase) ||
                   simpleName.Equals("StoryMode", StringComparison.OrdinalIgnoreCase) ||
                   simpleName.StartsWith("StoryMode.", StringComparison.OrdinalIgnoreCase) ||
                   simpleName.Equals("CustomBattle", StringComparison.OrdinalIgnoreCase) ||
                   simpleName.StartsWith("CustomBattle.", StringComparison.OrdinalIgnoreCase);
        }

        private void CacheLifecycleMethods(Type implementationType)
        {
            Type baseType = typeof(MBSubModuleBase);
            foreach (MethodInfo baseMethod in baseType.GetMethods(
                         BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                if (!baseMethod.IsVirtual)
                {
                    continue;
                }

                Type[] parameterTypes = baseMethod.GetParameters().Select(parameter => parameter.ParameterType).ToArray();
                MethodInfo method = FindLifecycleMethod(implementationType, baseMethod.Name, parameterTypes);
                if (method == null)
                {
                    throw new MissingMethodException(implementationType.FullName, baseMethod.Name);
                }

                _lifecycleMethods.Add(baseMethod.Name, method);
            }
        }

        private static MethodInfo FindLifecycleMethod(Type implementationType, string name, Type[] parameterTypes)
        {
            for (Type current = implementationType; current != null; current = current.BaseType)
            {
                MethodInfo method = current.GetMethod(
                    name,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly,
                    binder: null,
                    types: parameterTypes,
                    modifiers: null);

                if (method != null)
                {
                    return method;
                }
            }

            return null;
        }

        private string BuildFatalMessage(string stage, Exception exception)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("AnimusForge 无法启动，Bootstrap 已停止加载。不会尝试加载另一游戏版本的 DLL。");
            builder.AppendLine($"阶段 / Stage: {stage}");
            builder.AppendLine($"游戏版本 / Game: {RuntimeVersionText}");
            builder.AppendLine($"目标 API / API: {SelectedApiText}");
            builder.AppendLine($"目标 DLL / DLL: {SelectedImplementationPath}");
            builder.AppendLine($"错误 / Error: {exception.GetType().Name}: {exception.Message}");
            builder.Append($"UTF-8 日志 / Log: {BootstrapLog.LogPath}");
            return builder.ToString();
        }

        private string BuildFatalDialogMessage(string stage, Exception exception)
        {
            Exception rootCause = GetInnermostException(exception);
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("AnimusForge could not start. Bootstrap stopped loading and will not try the other game-version DLL.");
            builder.AppendLine($"Stage: {stage}");
            builder.AppendLine($"Game: {RuntimeVersionText}");
            builder.AppendLine($"API: {SelectedApiText}");
            builder.AppendLine($"DLL: {SelectedImplementationPath}");
            builder.AppendLine($"Error: {exception.GetType().Name}: {exception.Message}");
            if (!ReferenceEquals(rootCause, exception))
            {
                builder.AppendLine($"Root cause: {rootCause.GetType().Name}: {rootCause.Message}");
            }
            builder.Append($"UTF-8 log: {BootstrapLog.LogPath}");
            return builder.ToString();
        }

        private static Exception GetInnermostException(Exception exception)
        {
            Exception current = exception ?? new InvalidOperationException("Unknown Bootstrap error.");
            for (int depth = 0; depth < 16 && current.InnerException != null; depth++)
            {
                current = current.InnerException;
            }
            return current;
        }

        private static string GetAssemblyLocation(Assembly assembly)
        {
            if (assembly == null)
            {
                return "<not loaded>";
            }

            try
            {
                string location = assembly.Location;
                return string.IsNullOrWhiteSpace(location) ? "<dynamic or unknown>" : Path.GetFullPath(location);
            }
            catch
            {
                return "<unavailable>";
            }
        }

        private static bool PathsEqual(string left, string right)
        {
            if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right) ||
                left.StartsWith("<", StringComparison.Ordinal) || right.StartsWith("<", StringComparison.Ordinal))
            {
                return false;
            }

            try
            {
                return string.Equals(Path.GetFullPath(left), Path.GetFullPath(right), StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private static string ComputeSha256(string path)
        {
            try
            {
                using (SHA256 sha256 = SHA256.Create())
                using (FileStream stream = File.OpenRead(path))
                {
                    return BitConverter.ToString(sha256.ComputeHash(stream)).Replace("-", string.Empty);
                }
            }
            catch (Exception exception)
            {
                return "unavailable:" + exception.GetType().Name;
            }
        }

        private static string GetImplementationFileVersion(string path)
        {
            try
            {
                FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(path);
                string fileVersion = string.IsNullOrWhiteSpace(versionInfo.FileVersion) ? "unknown" : versionInfo.FileVersion;
                string productVersion = string.IsNullOrWhiteSpace(versionInfo.ProductVersion) ? "unknown" : versionInfo.ProductVersion;
                return $"{fileVersion} (product={productVersion})";
            }
            catch (Exception ex)
            {
                return $"unavailable ({ex.GetType().Name})";
            }
        }

        private static string GetBuildMarkerSummary(string implementationPath)
        {
            string markerPath = Path.ChangeExtension(implementationPath, ".build.json");
            try
            {
                if (!File.Exists(markerPath))
                {
                    return "build marker=missing";
                }

                string json = File.ReadAllText(markerPath, Encoding.UTF8);
                string referenceVersion = ReadBuildMarkerString(json, "ReferenceGameVersion");
                string createdUtc = ReadBuildMarkerString(json, "CreatedUtc");
                return $"reference game={referenceVersion}, built UTC={createdUtc}";
            }
            catch (Exception ex)
            {
                return $"build marker=unavailable ({ex.GetType().Name})";
            }
        }

        private static string ReadBuildMarkerString(string json, string key)
        {
            Match match = Regex.Match(
                json ?? string.Empty,
                $"\"{Regex.Escape(key)}\"\\s*:\\s*\"(?<value>[^\"]*)\"",
                RegexOptions.CultureInvariant);
            return match.Success && !string.IsNullOrWhiteSpace(match.Groups["value"].Value)
                ? match.Groups["value"].Value
                : "unknown";
        }
    }
}
