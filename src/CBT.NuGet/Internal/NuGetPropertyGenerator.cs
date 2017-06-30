using Microsoft.Build.Construction;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace CBT.NuGet.Internal
{
    internal sealed class NuGetPropertyGenerator
    {
        /// <summary>
        /// The name of the 'ID' attribute in the NuGet packages.config.
        /// </summary>
        private const string NuGetPackagesConfigIdAttributeName = "id";

        /// <summary>
        /// The name of the &lt;package /&gt; element in th NuGet packages.config.
        /// </summary>
        private const string NuGetPackagesConfigPackageElementName = "package";

        /// <summary>
        /// The name of the 'Version' attribute in the NuGet packages.config.
        /// </summary>
        private const string NuGetPackagesConfigVersionAttributeName = "version";

        private readonly string[] _packageConfigPaths;

        private readonly CBTTaskLogHelper _logger;

        public NuGetPropertyGenerator(CBTTaskLogHelper logger, params string[] packageConfigPaths)
        {

            _packageConfigPaths = packageConfigPaths ?? throw new ArgumentNullException(nameof(packageConfigPaths));
            _logger = logger;
        }

        public bool Generate(string outputPath, string propertyVersionNamePrefix, string propertyPathNamePrefix, string propertyPathValuePrefix, string nuGetPackagesPath, PackageRestoreData restoreData)
        {
            ProjectRootElement project = ProjectRootElement.Create();
            ProjectPropertyGroupElement propertyGroup = project.AddPropertyGroup();
            propertyGroup.SetProperty("MSBuildAllProjects", "$(MSBuildAllProjects);$(MSBuildThisFileFullPath)");

            ProjectItemGroupElement itemGroup = project.AddItemGroup();
            foreach (PackageIdentityWithPath packageInfo in ParsePackages(nuGetPackagesPath, restoreData))
            {
                propertyGroup.SetProperty(
                    String.Format(CultureInfo.CurrentCulture, "{0}{1}", propertyPathNamePrefix,
                        packageInfo.Id.Replace(".", "_")),
                    String.Format(CultureInfo.CurrentCulture, "{0}", packageInfo.FullPath));
                propertyGroup.SetProperty(
                    String.Format(CultureInfo.CurrentCulture, "{0}{1}", propertyVersionNamePrefix,
                        packageInfo.Id.Replace(".", "_")),
                    String.Format(CultureInfo.CurrentCulture, "{0}", packageInfo.Version.ToString()));
                // Consider adding item metadata of packageid and version for ease of consumption of this property.
                itemGroup.AddItem("CBTNuGetPackageDir",
                    String.Format(CultureInfo.CurrentCulture, "{0}", packageInfo.FullPath));
            }
            project.Save(outputPath);

            return true;
        }

        private IEnumerable<PackageIdentityWithPath> ParsePackages(string packagesPath, PackageRestoreData restoreData)
        {
            ModulePropertyGenerator modulePropertyGenerator = new ModulePropertyGenerator(_logger, packagesPath, restoreData, _packageConfigPaths);
            return modulePropertyGenerator._packages.Values;
        }
    }
}