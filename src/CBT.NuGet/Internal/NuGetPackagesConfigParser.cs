using NuGet.Packaging;
using NuGet.Packaging.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using NuGet.ProjectManagement;


namespace CBT.NuGet.Internal
{
    /// <summary>
    /// Represents a class that can parse a NuGet packages.config file.
    /// </summary>
    internal sealed class NuGetPackagesConfigParser : INuGetPackageConfigParser
    {
        public IEnumerable<PackageIdentityWithPath> GetPackages(string packagesPath, string packageConfigPath, PackageRestoreData packageRestoreData)
        {
            if (packageConfigPath != null && !packageConfigPath.EndsWith(Constants.PackageReferenceFile, StringComparison.OrdinalIgnoreCase))
            {
                if (packageRestoreData?.RestoreProjectStyle != null && !packageRestoreData.RestoreProjectStyle.Equals("Unknown", StringComparison.InvariantCultureIgnoreCase))
                {
                    yield break;
                }

                // If a *proj was passed in but it is really a packages.config project then set the packages.config file.
                if (packageRestoreData?.RestoreProjectStyle != null && packageRestoreData.RestoreProjectStyle.Equals("Unknown", StringComparison.OrdinalIgnoreCase))
                {
                    var tmpPackageConfigPath = Path.Combine(Path.GetDirectoryName(packageConfigPath ?? string.Empty) ?? string.Empty, "packages.config");
                    if (File.Exists(tmpPackageConfigPath))
                    {
                        packageConfigPath = tmpPackageConfigPath;
                    }
                }
            }

            if (packageConfigPath == null || !packageConfigPath.EndsWith(Constants.PackageReferenceFile, StringComparison.OrdinalIgnoreCase))
            {
                yield break;
            }

            XDocument document = XDocument.Load(packageConfigPath);

            PackagePathResolver packagePathResolver = new PackagePathResolver(packagesPath);

            PackagesConfigReader packagesConfigReader = new PackagesConfigReader(document);
            
            foreach (PackageIdentity item in packagesConfigReader.GetPackages(allowDuplicatePackageIds: true).Select(i => i.PackageIdentity))
            {
                yield return new PackageIdentityWithPath(item, packagePathResolver.GetPackageDirectoryName(item), packagePathResolver.GetInstallPath(item));
            }
        }
    }
}