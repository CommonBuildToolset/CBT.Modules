using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CBT.NuGet.Internal
{
    internal sealed class AggregatePackage
    {
        public enum AggregateOperation
        {
            Add = '*',
            Remove = '|',
        }

        internal struct PackageOperations
        {
            internal AggregateOperation Operation { get; set; }
            internal string Folder { get; set; }
        }

        public AggregatePackage(string outPropertyId, ICollection<PackageOperations> packagesToAggregate, string destinationRoot)
        {
            OutPropertyId = outPropertyId;
            PackagesToAggregate = packagesToAggregate;
            OutPropertyValue = Path.Combine(destinationRoot, $"{outPropertyId}.{GetOutputPropertyHash()}");
        }

        public string OutPropertyId { get; private set; }

        public string OutPropertyValue { get; private set; }

        public ICollection<PackageOperations> PackagesToAggregate { get; private set; }

        private string GetOutputPropertyHash()
        {
            StringBuilder valueToHash = new StringBuilder();
            foreach (var pkg in PackagesToAggregate)
            {
                valueToHash.Append(pkg.Folder.ToLower());
            }
            return string.Format("{0:X}", Convert.ToBase64String(Encoding.UTF8.GetBytes(valueToHash.ToString())).GetHashCode());
        }

    }
}