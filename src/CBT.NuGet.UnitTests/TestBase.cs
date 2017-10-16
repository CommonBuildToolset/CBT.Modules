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
        private readonly string _testRootPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        public string TestRootPath
        {
            get
            {
                Directory.CreateDirectory(_testRootPath);
                return _testRootPath;
            }
        }

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
