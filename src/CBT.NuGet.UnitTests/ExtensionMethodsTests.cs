using CBT.NuGet.Internal;
using Shouldly;
using Xunit;

namespace CBT.NuGet.UnitTests
{
    public class ExtensionMethodsTests
    {
        [Theory]
        [InlineData("zxcvbnm,./asdfghjkl;'qwertyuiop[]\\`1234567890-=~!@#$%^&*()_+{}|:\"<>?")]
        [InlineData("ZXCVBNM,./ASDFGHJKL;'QWERTYUIOP[]\\`1234567890-=~!@#$%^&*()_+{}|:\"<>?")]
        [InlineData("Zxcvbnm,./asdfghjkl;'qwertyuiop[]\\`1234567890-=~!@#$%^&*()_+{}|:\"<>?")]
        public void GetMD5HashCaseInsensitive(string input)
        {
            input.GetMd5Hash().ShouldBe("D79C4C79DD4DC91869D3DD98AA70E3F9");
        }

        [Fact]
        public void GetMD5HashTest()
        {
            "Hello World".GetMd5Hash().ShouldBe("361FADF1C712E812D198C4CAB5712A79");
        }

        [Fact]
        public void GetMD5HashTestMaxPath()
        {
            new string('s', 1024).GetMd5Hash().ShouldBe("3677C1915A53814950925D816AE380B2");
        }
    }
}