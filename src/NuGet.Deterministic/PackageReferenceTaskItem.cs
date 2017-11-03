using System;
using Microsoft.Build.Framework;

namespace NuGet.Tasks.Deterministic
{
    internal sealed class PackageReferenceTaskItem
    {
        private readonly ITaskItem _taskItem;
        private readonly Lazy<string> _pathLazy;

        public PackageReferenceTaskItem(ITaskItem taskItem)
        {
            _taskItem = taskItem;

            _pathLazy = new Lazy<string>(() => taskItem.GetMetadata(GenerateLockedPackageConfigurationFile.PathMetadataName)?.Replace('/', System.IO.Path.DirectorySeparatorChar));
        }

        public string Sha512 => _taskItem.GetMetadata(GenerateLockedPackageConfigurationFile.Sha512MetadataName);

        public string Name => _taskItem.ItemSpec;

        public string Hashfile => _taskItem.GetMetadata(GenerateLockedPackageConfigurationFile.HashfileMetadataName);

        public string Path => _pathLazy.Value;
    }
}