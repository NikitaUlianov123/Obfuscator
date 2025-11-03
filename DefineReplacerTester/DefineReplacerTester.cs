using Obfuscator;

namespace DefineReplacerTester
{
    public class DefineReplacerTester
    {
        [Fact]
        public void TestBasicUsage()
        {
            string code = """
                #define MAX 100
                #define MIN 0

                MAX MIN
                MAX MIN
                MAXIMUM MINIMUM

                MAXMIN
                MINMAX
                """;

            string expectedTransformedCode = """
            #define MAX 100
            #define MIN 0

            100 0
            100 0
            MAXIMUM MINIMUM

            MAXMIN
            MINMAX
            """;

            Assert.Equal(expectedTransformedCode, DefineReplacer.TransformDefinedCode(code));

        }
    }
}