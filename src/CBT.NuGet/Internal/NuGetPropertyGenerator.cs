using Microsoft.Build.Construction;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;

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

        public NuGetPropertyGenerator(params string[] packageConfigPaths)
        {
            if (packageConfigPaths == null)
            {
                throw new ArgumentNullException(nameof(packageConfigPaths));
            }

            _packageConfigPaths = packageConfigPaths;
        }

        public bool Generate(string outputPath, string propertyVersionNamePrefix, string propertyPathNamePrefix, string propertyPathValuePrefix)
        {
            ProjectRootElement project = ProjectRootElement.Create();

            ProjectPropertyGroupElement propertyGroup = project.AddPropertyGroup();

            propertyGroup.SetProperty("MSBuildAllProjects", "$(MSBuildAllProjects);$(MSBuildThisFileFullPath)");

            ProjectItemGroupElement itemGroup = project.AddItemGroup();

            foreach (PackageInfo packageInfo in ParsePackages())
            {
                propertyGroup.SetProperty(String.Format(CultureInfo.CurrentCulture, "{0}{1}", propertyPathNamePrefix, packageInfo.Id.Replace(".", "_")), String.Format(CultureInfo.CurrentCulture, "{0}{1}.{2}", propertyPathValuePrefix, packageInfo.Id, packageInfo.VersionString));
                propertyGroup.SetProperty(String.Format(CultureInfo.CurrentCulture, "{0}{1}", propertyVersionNamePrefix, packageInfo.Id.Replace(".", "_")), String.Format(CultureInfo.CurrentCulture, "{0}", packageInfo.VersionString));
                itemGroup.AddItem("CBTNuGetPackageDir", String.Format(CultureInfo.CurrentCulture, "{0}{1}.{2}", propertyPathValuePrefix, packageInfo.Id, packageInfo.VersionString));
            }

            project.Save(outputPath);

            return true;
        }

        private IEnumerable<PackageInfo> ParsePackages()
        {
            foreach (string packageConfigPath in _packageConfigPaths.Where(i => !String.IsNullOrWhiteSpace(i) && File.Exists(i)))
            {
                XDocument document = XDocument.Load(packageConfigPath);

                if (document.Root != null)
                {
                    foreach (var item in document.Root.Elements(NuGetPackagesConfigPackageElementName).Select(i => new
                    {
                        Id = i.Attribute(NuGetPackagesConfigIdAttributeName) == null ? null : i.Attribute(NuGetPackagesConfigIdAttributeName).Value,
                        Version = i.Attribute(NuGetPackagesConfigVersionAttributeName) == null ? null : i.Attribute(NuGetPackagesConfigVersionAttributeName).Value,
                    }))
                    {
                        // Skip packages that are missing an 'id' or 'version' attribute or if they specified value is an empty string
                        //
                        if (item.Id == null || item.Version == null ||
                            String.IsNullOrWhiteSpace(item.Id) ||
                            String.IsNullOrWhiteSpace(item.Version))
                        {
                            continue;
                        }

                        yield return new PackageInfo(item.Id, item.Version);
                    }
                }
            }
        }
    }
}