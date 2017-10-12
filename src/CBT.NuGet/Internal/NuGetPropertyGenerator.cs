using Microsoft.Build.Construction;
using Microsoft.Build.Framework;
using NuGet.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace CBT.NuGet.Internal
{
    internal sealed class NuGetPropertyGenerator
    {
        private readonly Lazy<List<INuGetPackageConfigParser>> _configParsersLazy;
        private readonly CBTTaskLogHelper _logger;
        private readonly string[] _packageConfigPaths;

        public NuGetPropertyGenerator(CBTTaskLogHelper logger, params string[] packageConfigPaths)
            : this(logger, null, packageConfigPaths)
        {
            
        }

        public NuGetPropertyGenerator(CBTTaskLogHelper logger, ISettings settings, params string[] packageConfigPaths)
        {
            _packageConfigPaths = packageConfigPaths ?? throw new ArgumentNullException(nameof(packageConfigPaths));
            _logger = logger;

            _configParsersLazy = new Lazy<List<INuGetPackageConfigParser>>(() =>
            {
                settings = settings ?? Settings.LoadDefaultSettings(Path.GetDirectoryName(_packageConfigPaths[0]), configFileName: null, machineWideSettings: new XPlatMachineWideSetting());

                return new List<INuGetPackageConfigParser>
                {
                    new NuGetPackagesConfigParser(settings, _logger),
                    new NuGetProjectJsonParser(settings, _logger),
                    new NuGetPackageReferenceProjectParser(settings, _logger)
                };
            });
        }

        public bool Generate(string outputPath, string propertyVersionNamePrefix, string propertyPathNamePrefix, PackageRestoreData restoreData)
        {
            ProjectRootElement project = ProjectRootElement.Create();
            ProjectPropertyGroupElement propertyGroup = project.AddPropertyGroup();
            propertyGroup.SetProperty("MSBuildAllProjects", "$(MSBuildAllProjects);$(MSBuildThisFileFullPath)");

            ProjectItemGroupElement itemGroup = project.AddItemGroup();

            foreach (string packageConfigPath in _packageConfigPaths)
            {
                _logger.LogMessage(MessageImportance.Low, $"Parsing '{packageConfigPath}'");

                IEnumerable<PackageIdentityWithPath> parsedPackages = null;

                INuGetPackageConfigParser configParser = _configParsersLazy.Value.FirstOrDefault(i => i.TryGetPackages(packageConfigPath, restoreData, out parsedPackages));

                if (configParser != null && parsedPackages != null)
                {
                    foreach (PackageIdentityWithPath packageInfo in parsedPackages)
                    {
                        propertyGroup.SetProperty($"{propertyPathNamePrefix}{packageInfo.Id.Replace(".", "_")}", $"{packageInfo.FullPath}");

                        propertyGroup.SetProperty($"{propertyVersionNamePrefix}{packageInfo.Id.Replace(".", "_")}", $"{packageInfo.Version.ToString()}");

                        // Consider adding item metadata of packageid and version for ease of consumption of this property.
                        itemGroup.AddItem("CBTNuGetPackageDir", packageInfo.FullPath);
                    }
                }
            }

            if (propertyGroup.Properties.Count > 1)
            {
                project.Save(outputPath);
            }

            return true;
        }
    }
}