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
        public void GetHashCaseInsensitive(string input)
        {
            input.GetHash().ShouldBe("RqVWg4YHc/mLuuEhWQmeClqjVrrZFDD+TK2Ubahsq/Q=");
        }

        [Fact]
        public void GetHashTest()
        {
            "Hello World".GetHash().ShouldBe("eH7Hbcr9IMGQjrCTahL5Ht0QWrXNfswrGuIDJkg0Xf8=");
        }

        [Fact]
        public void GetHashTestMaxPath()
        {
            new string('s', 1024).GetHash().ShouldBe("UpoU1WutH2vtQTWhm6u11gVJLaSIf7uZHtds2shv1TU=");
        }
    }
}