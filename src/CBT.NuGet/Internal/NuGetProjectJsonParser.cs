using System;
using NuGet.Common;
using NuGet.Packaging;
using NuGet.ProjectModel;
using System.Collections.Generic;
using System.IO;

namespace CBT.NuGet.Internal
{
    /// <summary>
    /// Represents a class that can parse a NuGet project.json file.
    /// </summary>
    internal sealed class NuGetProjectJsonParser : INuGetPackageConfigParser
    {
        public IEnumerable<PackageIdentityWithPath> GetPackages(string packagesPath, string packageConfigPath, PackageRestoreData packageRestoreData)
        {
            if (!ProjectJsonPathUtilities.IsProjectConfig(packageConfigPath))
            {
                if (packageRestoreData?.RestoreProjectStyle != null && !packageRestoreData.RestoreProjectStyle.Equals("ProjectJson", StringComparison.InvariantCultureIgnoreCase))
                {
                    yield break;
                }

                // If a *proj was passed in but it is really a json project then lookup the json file.
                if (packageRestoreData?.RestoreProjectStyle != null && packageRestoreData.RestoreProjectStyle.Equals("ProjectJson", StringComparison.OrdinalIgnoreCase))
                {
                    packageConfigPath = packageRestoreData.ProjectJsonPath;
                }
            }

            if (!ProjectJsonPathUtilities.IsProjectConfig(packageConfigPath))
            {
                yield break;
            }

            string lockFilePath = ProjectJsonPathUtilities.GetLockFilePath(packageConfigPath);

            if (!File.Exists(lockFilePath))
            {
                yield break;
            }

            LockFile lockFile = LockFileUtilities.GetLockFile(lockFilePath, NullLogger.Instance);

            VersionFolderPathResolver versionFolderPathResolver = new VersionFolderPathResolver(packagesPath);

            foreach (LockFileLibrary library in lockFile.Libraries)
            {
                yield return new PackageIdentityWithPath(library.Name, library.Version, versionFolderPathResolver.GetPackageDirectory(library.Name, library.Version), versionFolderPathResolver.GetInstallPath(library.Name, library.Version));
            }
        }
    }
}