using Microsoft.Build.Construction;
using Microsoft.Build.Framework;
using NuGet.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;

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

                // Ordering here is based on the most likely scenario.  As PackageReference becomes more popular, we should move it up
                //
                return new List<INuGetPackageConfigParser>
                {
                    new NuGetPackagesConfigParser(settings, _logger),
                    new NuGetPackageReferenceProjectParser(settings, _logger),
                    new NuGetProjectJsonParser(settings, _logger),
                };
            });
        }

        public void Generate(string outputPath, string propertyVersionNamePrefix, string propertyPathNamePrefix, PackageRestoreData restoreData)
        {
            // Delete an existing file in case there are no properties generated and we don't end up saving the file
            //
            if (File.Exists(outputPath))
            {
                Retry(() => File.Delete(outputPath), TimeSpan.FromMilliseconds(500));
            }

            ProjectRootElement project = ProjectRootElement.Create();
            ProjectPropertyGroupElement propertyGroup = project.AddPropertyGroup();
            propertyGroup.SetProperty("MSBuildAllProjects", "$(MSBuildAllProjects);$(MSBuildThisFileFullPath)");

            ProjectItemGroupElement itemGroup = project.AddItemGroup();

            bool anyPropertiesCreated = false;

            foreach (string packageConfigPath in _packageConfigPaths)
            {
                _logger.LogMessage(MessageImportance.Low, $"Parsing '{packageConfigPath}'");

                IEnumerable<PackageIdentityWithPath> parsedPackages = null;

                INuGetPackageConfigParser configParser = null;

                // A bug in nuget sometimes causes "NuGet.Configuration.NuGetConfigurationException: Unexpected failure reading NuGet.Config." when multiple instances are running in parrallel such as in the quickbuild scenario.
                Retry(() => configParser = _configParsersLazy.Value.FirstOrDefault(i => i.TryGetPackages(packageConfigPath, restoreData, out parsedPackages)), TimeSpan.FromMilliseconds(1000));

                if (configParser != null && parsedPackages != null)
                {
                    anyPropertiesCreated = true;

                    foreach (PackageIdentityWithPath packageInfo in parsedPackages)
                    {
                        propertyGroup.SetProperty($"{propertyPathNamePrefix}{packageInfo.Id.Replace(".", "_")}", $"{packageInfo.FullPath}");

                        propertyGroup.SetProperty($"{propertyVersionNamePrefix}{packageInfo.Id.Replace(".", "_")}", $"{packageInfo.Version.ToString()}");

                        // Consider adding item metadata of packageid and version for ease of consumption of this property.
                        itemGroup.AddItem("CBTNuGetPackageDir", packageInfo.FullPath);
                    }
                }
            }

            // Don't save the file if no properties were created.  In Visual Studio design time builds, this can be called multiple times until there are finally
            // properties that can be created.  If we generate an empty file, it won't get regenerated once there are properties to create.
            //
            if (anyPropertiesCreated)
            {
                Retry(() => project.Save(outputPath), TimeSpan.FromMilliseconds(500));
            }
        }

        private static void Retry(Action action, TimeSpan retryInterval, int retryCount = 3)
        {
            List<Exception> exceptions = new List<Exception>();

            for (int retry = 0; retry < retryCount; retry++)
            {
                try
                {
                    if (retry > 0)
                    {
                        Thread.Sleep(retryInterval);
                    }

                    action();

                    return;
                }
                catch (Exception e)
                {
                    exceptions.Add(e);
                }
            }

            throw new AggregateException(exceptions);
        }
    }
}