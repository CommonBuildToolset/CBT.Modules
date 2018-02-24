using CBT.NuGet.Internal;
using Microsoft.Build.Framework;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;

namespace CBT.NuGet.Tasks
{
    public class WriteNuGetRestoreInfo : SemaphoreTask
    {
        [Required]
        public string File { get; set; }

        [Required]
        public ITaskItem[] Input { get; set; }

        /// <summary>
        /// Use the path to file to be written as the semaphore name to ensure its only written to once.
        /// </summary>
        protected override string SemaphoreName => File;

        public override void Run()
        {
            if (System.IO.File.Exists(File))
            {
                System.IO.File.Delete(File);
            }

            PackageRestoreData packageRestoreData = new PackageRestoreData
            {
                ProjectJsonPath = Input
                    .FirstOrDefault(i => i.ItemSpec.Equals("ProjectJsonPath", StringComparison.OrdinalIgnoreCase))?
                    .GetMetadata("value"),
                RestoreProjectStyle = Input
                    .FirstOrDefault(i => i.ItemSpec.Equals("RestoreProjectStyle", StringComparison.OrdinalIgnoreCase))?
                    .GetMetadata("value"),
                RestoreOutputAbsolutePath = Input
                    .FirstOrDefault(i => i.ItemSpec.Equals("RestoreOutputAbsolutePath", StringComparison.OrdinalIgnoreCase))?
                    .GetMetadata("value"),
                PackageImportOrder = Input
                    .Where(i => i.ItemSpec.Equals("PackageReference", StringComparison.OrdinalIgnoreCase) && !String.IsNullOrWhiteSpace(i.GetMetadata("id")))
                    .Select(i => new RestorePackage(i.GetMetadata("id"), i.GetMetadata("version"))).ToList()
            };

            Directory.CreateDirectory(Path.GetDirectoryName(File));

            System.IO.File.WriteAllText(File, JsonConvert.SerializeObject(packageRestoreData, Formatting.Indented));
        }
    }
}