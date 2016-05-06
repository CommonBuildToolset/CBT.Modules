namespace CBT.NuGet.Internal
{
    internal sealed class PackageInfo
    {
        public PackageInfo(string id, string version)
        {
            Id = id;
            VersionString = version;
        }

        public string Id { get; private set; }

        public string VersionString { get; private set; }
    }
}