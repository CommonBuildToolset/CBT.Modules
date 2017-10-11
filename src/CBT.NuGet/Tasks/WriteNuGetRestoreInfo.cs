using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CBT.NuGet.Internal;
using Microsoft.Build.Framework;
using Newtonsoft.Json;

namespace CBT.NuGet.Tasks
{
    public class WriteNuGetRestoreInfo : ITask
    {

        [Required]
        public ITaskItem[] Input { get; set; }

        [Required]
        public string File { get; set; }

        /// <summary>
        /// Gets or sets the build engine associated with the task.
        /// </summary>
        public IBuildEngine BuildEngine { get; set; }

        /// <summary>
        /// Gets or sets any host object that is associated with the task.
        /// </summary>
        public ITaskHost HostObject { get; set; }

        public bool Execute()
        {
            if (!Directory.Exists(Path.GetDirectoryName(File)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(File));
            }

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

            System.IO.File.WriteAllText(File, JsonConvert.SerializeObject(packageRestoreData, Formatting.Indented));

            return System.IO.File.Exists(File);
        }
    }
}
