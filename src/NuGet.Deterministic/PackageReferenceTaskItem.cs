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

            _pathLazy = new Lazy<string>(() => taskItem.GetMetadata(GenerateLockedPackageReferencesFile.PackagePathMetadataName)?.Replace('/', System.IO.Path.DirectorySeparatorChar));
        }

        public string Sha512 => _taskItem.GetMetadataNoThrow(GenerateLockedPackageReferencesFile.Sha512MetadataName);

        public string Name => _taskItem.ItemSpec;

        public string Hashfile => _taskItem.GetMetadataNoThrow(GenerateLockedPackageReferencesFile.HashfileMetadataName);

        public string PackagePath => _pathLazy.Value;
    }
}