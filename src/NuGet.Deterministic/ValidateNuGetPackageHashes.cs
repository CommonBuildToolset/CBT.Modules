using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace NuGet.Tasks.Deterministic
{
    public class ValidateNuGetPackageHashes : Task
    {
        [Required]
        public string NuGetPackageRoot { get; set; }

        [Required]
        public ITaskItem[] PackageReferences { get; set; }

        public override bool Execute()
        {
            foreach (PackageReferenceTaskItem packageReference in PackageReferences.Select(i => new PackageReferenceTaskItem(i)))
            {
                if (String.IsNullOrWhiteSpace(packageReference.Sha512))
                {
                    Log.LogWarning(subcategory: null, warningCode: "ND1000", helpKeyword: null, file: null, lineNumber: 0, columnNumber: 0, endColumnNumber: 0, endLineNumber: 0, message: $"The package '{packageReference.Name}' does not have a SHA512 hash specified.");
                    continue;
                }

                string path = Path.Combine(NuGetPackageRoot, packageReference.Path, packageReference.Hashfile);

                if (!File.Exists(path))
                {
                    
                    Log.LogError(sub"ND1001", );
                }
            }

            return !Log.HasLoggedErrors;
        }
    }
}
