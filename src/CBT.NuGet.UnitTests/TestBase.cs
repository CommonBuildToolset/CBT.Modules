using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CBT.NuGet.UnitTests
{
    public abstract class TestBase : IDisposable
    {
        public string TestRootPath { get; } = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        protected string GetTempFileName()
        {
            Directory.CreateDirectory(TestRootPath);

            return Path.Combine(TestRootPath, Path.GetRandomFileName());
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (Directory.Exists(TestRootPath))
                {
                    Directory.Delete(TestRootPath, recursive: true);
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
