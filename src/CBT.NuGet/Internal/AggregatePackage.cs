using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace CBT.NuGet.Internal
{
    internal sealed class AggregatePackage
    {
        public enum AggregateOperation
        {
            Add = '*',
            Remove = '!',
        }

        internal struct PackageOperation
        {
            internal AggregateOperation Operation { get; set; }

            internal string Folder { get; set; }
        }

        public AggregatePackage(string outPropertyId, ICollection<PackageOperation> packagesToAggregate, string destinationRoot)
        {
            OutPropertyId = outPropertyId;
            PackagesToAggregate = packagesToAggregate;
            OutPropertyValue = Path.Combine(destinationRoot, $"{outPropertyId}.{GetOutputPropertyHash()}");
        }

        public string OutPropertyId { get; private set; }

        public string OutPropertyValue { get; private set; }

        public ICollection<PackageOperation> PackagesToAggregate { get; private set; }

        private string GetOutputPropertyHash()
        {
            StringBuilder valueToHash = new StringBuilder();
            foreach (var pkg in PackagesToAggregate)
            {
                valueToHash.Append(pkg.Folder.ToLower());
                // Include last write time on all files to ensure that if the source folder was updated a new aggregate package will be created.
                // if a file was removed then it will not be in the value to hash so the hash will still change to trigger a new aggregation as desired.
                // We don't care about directories in aggregation since removing directories is not possible given current implementation.
                // This is in the scenario where the user aggregates against a nuget package and a checked in folder under source control that contains a config file or whatever.
                // Concern on aggregating very large folders as this is disk IO.
                foreach (var f in Directory.GetFiles(pkg.Folder, "*.*", SearchOption.AllDirectories))
                {
                    valueToHash.Append(File.GetLastWriteTime(f).ToString());
                }

            }
            string hashValue = Convert.ToBase64String((new SHA1CryptoServiceProvider()).ComputeHash(Encoding.UTF8.GetBytes(valueToHash.ToString())));
            Regex pattern = new Regex("[/=+]");
            hashValue = pattern.Replace(hashValue, "x");
            return string.Format("{0:X}", hashValue);
        }

    }
}