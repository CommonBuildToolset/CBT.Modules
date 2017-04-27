using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace CBT.NuGet.Internal
{
    internal sealed class AggregatePackage
    {
        public enum AggregateOption
        {
            Add,
            Remove,
        }

        public AggregatePackage(string outPropertyId, List<KeyValuePair<AggregateOption, string>> packagesToAggregate, string destinationRoot)
        {
            OutPropertyId = outPropertyId;
            PackagesToAggregate = packagesToAggregate;
            OutPropertyValue = Path.Combine(destinationRoot, string.Format("{0}.{1}", outPropertyId, GetOutputPropertyHash()));
        }

        public string OutPropertyId { get; private set; }

        public string OutPropertyValue { get; private set; }

        public List<KeyValuePair<AggregateOption, string>> PackagesToAggregate { get; private set; }
        private string GetOutputPropertyHash()
        {
            StringBuilder valueToHash = new StringBuilder();
            foreach (var pkg in PackagesToAggregate)
            {
                valueToHash.Append(pkg.Value.ToLower());
            }
            return string.Format("{0:X}", Convert.ToBase64String(Encoding.UTF8.GetBytes(valueToHash.ToString())).GetHashCode());
        }

        public static AggregatePackage ParseIntoPackage(string input, string pkgDestRoot)
        {
            try
            {
                // Review split function for something more robust and proper.
                List<KeyValuePair<AggregateOption, string>> packagesToAggregate = new List<KeyValuePair<AggregateOption, string>>();
                int aggIndex = 0;
                var aggPkg = input.Split('=');
                var pkgID = aggPkg[0];
                string aggPkgs = aggPkg[1];
                char[] splitters = new char[] { '*', '|' };
                aggIndex = aggPkgs.IndexOfAny(splitters);
                // add base package
                if (aggIndex < 0)
                {
                    aggIndex = aggPkgs.Length;
                }
                packagesToAggregate.Add(new KeyValuePair<AggregateOption, string>(AggregateOption.Add, aggPkgs.Substring(0, aggIndex)));
                aggPkgs = aggPkgs.Substring(aggIndex);
                aggIndex = aggPkgs.IndexOfAny(splitters);
                while (aggPkgs.Length > 0)
                {
                    char indexChar = aggPkgs[0];
                    aggPkgs = aggPkgs.Substring(1);
                    aggIndex = aggPkgs.IndexOfAny(splitters);
                    if (aggIndex < 0)
                    {
                        aggIndex = aggPkgs.Length;
                    }
                    if (indexChar.Equals('*'))
                    {
                        packagesToAggregate.Add(new KeyValuePair<AggregateOption, string>(AggregateOption.Add, aggPkgs.Substring(0, aggIndex)));
                    }
                    if (indexChar.Equals('|'))
                    {
                        packagesToAggregate.Add(new KeyValuePair<AggregateOption, string>(AggregateOption.Remove, aggPkgs.Substring(0, aggIndex)));
                    }
                    aggPkgs = aggPkgs.Substring(aggIndex);
                }

                return new AggregatePackage(pkgID, packagesToAggregate, pkgDestRoot);
            }
            catch
            {
                return null;
            }
        }

        public bool CreateAggregatePackage()
        {
            // The assumption is that in order for the source of an aggregate to change the package source folder must of changed for a new version.
            // And therefor it is assumed this never needs to be regenerated (assumped corruption would be cleaned up manually).
            // This is a flawed assumption if someone passes a non nuget package folder to aggregate.  Must consider what to do.
            if (Directory.Exists(OutPropertyValue))
            {
                return true;
            }

            using (var mutex = new Mutex(false, FileUtilities.ComputeMutexName(OutPropertyValue)))
            {
                bool owner = false;
                try
                {
                    var outTmpDir = OutPropertyValue + ".tmp";
                    FileUtilities.AcquireMutex(mutex);
                    owner = true;
                    // check again to see if aggregate package is already created while waiting.
                    if (Directory.Exists(OutPropertyValue))
                    {
                        return true;
                    }

                    if (Directory.Exists(outTmpDir))
                    {
                        Directory.Delete(outTmpDir, true);
                    }
                    foreach (var srcPkg in PackagesToAggregate)
                    {
                        if (srcPkg.Key.Equals(AggregatePackage.AggregateOption.Add))
                        {
                            FileUtilities.DirectoryCopy(srcPkg.Value, outTmpDir, true, true);
                        }
                        if (srcPkg.Key.Equals(AggregatePackage.AggregateOption.Remove))
                        {
                            FileUtilities.DirectoryRemove(srcPkg.Value, outTmpDir, true);
                        }
                    }
                    Directory.Move(outTmpDir, OutPropertyValue);
                }
                finally
                {
                    if (owner)
                    {
                        mutex.ReleaseMutex();
                    }
                }
            }
            return Directory.Exists(OutPropertyValue);
        }

    }
}