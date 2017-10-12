using Microsoft.Build.Framework;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Packaging;
using NuGet.ProjectModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CBT.NuGet.Internal
{
    /// <inheritdoc />
    /// <summary>
    /// Represents a class that can parse a NuGet project.json file.
    /// </summary>
    internal sealed class NuGetProjectJsonParser : NuGetPackageConfigParserBase
    {
        public NuGetProjectJsonParser(ISettings settings, CBTTaskLogHelper log)
            : base(settings, log)
        {
        }

        public override bool TryGetPackages(string packageConfigPath, PackageRestoreData packageRestoreData, out IEnumerable<PackageIdentityWithPath> packages)
        {
            packages = null;

            string projectJsonPath;

            if (ProjectJsonPathUtilities.IsProjectConfig(packageConfigPath))
            {
                projectJsonPath = packageConfigPath;
            }
            else
            {
                if (!String.Equals("ProjectJson", packageRestoreData?.RestoreProjectStyle, StringComparison.OrdinalIgnoreCase) || String.IsNullOrWhiteSpace(packageRestoreData?.ProjectJsonPath))
                {
                    return false;
                }

                projectJsonPath = packageRestoreData.ProjectJsonPath;
            }

            string lockFilePath = ProjectJsonPathUtilities.GetLockFilePath(projectJsonPath);

            if (!File.Exists(lockFilePath))
            {
                throw new FileNotFoundException($"The lock file '{lockFilePath}' does not exist.  Ensure that the restore succeeded and that the lock file was generated.");
            }

            LockFile lockFile = LockFileUtilities.GetLockFile(lockFilePath, NullLogger.Instance);

            string globalPackagesFolder = SettingsUtility.GetGlobalPackagesFolder(NuGetSettings);

            if (String.IsNullOrWhiteSpace(globalPackagesFolder))
            {
                throw new NuGetConfigurationException(@"Unable to determine the NuGet repository path.  This usually defaults to ""%UserProfile%\.nuget\packages"", ""%NUGET_PACKAGES%"", or the ""globalPackagesFolder"" in your NuGet.config.");
            }

            globalPackagesFolder = Path.GetFullPath(globalPackagesFolder);

            if (!Directory.Exists(globalPackagesFolder))
            {
                throw new DirectoryNotFoundException($"The NuGet repository '{globalPackagesFolder}' does not exist.  Ensure that NuGet is restore packages to the location specified in your NuGet.config.");
            }

            Log.LogMessage(MessageImportance.Low, $"Using repository path: '{globalPackagesFolder}'");

            VersionFolderPathResolver versionFolderPathResolver = new VersionFolderPathResolver(globalPackagesFolder);

            packages = lockFile.Libraries.Select(i =>
            {
                string installPath = versionFolderPathResolver.GetInstallPath(i.Name, i.Version);

                if (!String.IsNullOrWhiteSpace(installPath))
                {
                    installPath = Path.GetFullPath(installPath);
                }
                else
                {
                    Log.LogWarning($"The package '{i.Name}' was not found in the repository.");
                }

                return new PackageIdentityWithPath(i.Name, i.Version, installPath);
            }).Where(i => !String.IsNullOrWhiteSpace(i.FullPath));

            return true;
        }
    }
}