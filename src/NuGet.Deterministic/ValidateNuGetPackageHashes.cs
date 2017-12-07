using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.IO;
using System.Linq;

namespace NuGet.Tasks.Deterministic
{
    public class ValidateNuGetPackageHashes : Task
    {
        [Required]
        public ITaskItem[] PackageReferences { get; set; }

        [Required]
        public string PackageFolders { get; set; }

        public override bool Execute()
        {
            string[] packageFolders = PackageFolders.Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries).Select(i => i.Trim()).Where(i => !String.IsNullOrWhiteSpace(i)).ToArray();

            foreach (PackageReferenceTaskItem packageReference in PackageReferences.Select(i => new PackageReferenceTaskItem(i)))
            {
                if (String.IsNullOrWhiteSpace(packageReference.Sha512))
                {
                    Log.LogMessageFromText($"Cannot validate package reference '{packageReference.Name}' because it does not have a SHA512 associated with it.", MessageImportance.Low);
                    continue;
                }

                string packageDirectory = packageFolders.Select(i => Path.Combine(i, packageReference.PackagePath)).FirstOrDefault(Directory.Exists);

                if (packageDirectory == null)
                {
                    Log.LogErrorFromText("ND1001", $"The package '{packageReference.Name}' does not exist in any of the specified package folders '{PackageFolders}'.  Ensure that the packages for this project have been restored and were not deleted.");
                    continue;
                }

                string hashFilePath = Path.Combine(packageDirectory, packageReference.Hashfile);

                if (!File.Exists(hashFilePath))
                {
                    Log.LogErrorFromText("ND1002", $"The package '{packageReference.Name}' does not have a hash file at '{hashFilePath}'.  Ensure that the package was properly restored and that it has not been deleted.");
                    continue;
                }

                string actualHash = File.ReadAllText(hashFilePath).Trim();

                if (!String.Equals(packageReference.Sha512, actualHash))
                {
                    Log.LogErrorFromText("ND1003", $"The package reference '{packageReference.Name}' has an expected hash of '{packageReference.Sha512}' which does not match the hash of the package '{actualHash}' according to NuGet's hash file '{hashFilePath}'.  This can occur if you are downloading a different package then expected.  Verify that the feeds you are using contain the correct package.  In some cases, users modify packages and upload them with the same ID and version of an existing package.");
                }
            }

            return !Log.HasLoggedErrors;
        }
    }
}