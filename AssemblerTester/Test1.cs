using Obfuscator;

namespace AssemblerTester
{
    [TestClass]
    public sealed class Test1
    {
        [TestMethod]
        public void AddTest()
        {
            string test =
                "PUSH_CONST_INT 1\r\n" +
                "PUSH_CONST_INT 1\r\n" +
                "ADD \r\n" +
                "POP_LOCAL_INT 0 \r\n" +
                "PUSH_CONST_INT 1\r\n" +
                "PUSH_CONST_INT 2\r\n" +
                "ADD \r\n" +
                "POP_LOCAL_INT 1 \r\n";

            byte[] expected =
            [
                0x00, 0x01, 0x00, 0x00, // PUSH_CONST_INT 1
                0x00, 0x01, 0x00, 0x00, // PUSH_CONST_INT 1
                0x10, 0x00, 0x00, 0x00, // ADD
                0x04, 0x00, 0x00, 0x00, // POP_LOCAL_INT 0
                0x00, 0x01, 0x00, 0x00, // PUSH_CONST_INT 1
                0x00, 0x02, 0x00, 0x00, // PUSH_CONST_INT 2
                0x10, 0x00, 0x00, 0x00, // ADD
                0x04, 0x01, 0x00, 0x00, // POP_LOCAL_INT 1
            ];

            var result = Assembler.Assemble(test);


            CollectionAssert.AreEqual(expected, result);
        }

        [TestMethod]
        public void AddVariablesTest()
        {
            string test = 
                "PUSH_CONST_INT 1\r\n" +
                "PUSH_CONST_INT 1\r\n" +
                "ADD \r\n" +
                "POP_LOCAL_INT 0 \r\n" +
                "PUSH_CONST_INT 1\r\n" +
                "PUSH_CONST_INT 2\r\n" +
                "ADD \r\n" +
                "POP_LOCAL_INT 1 \r\n" +
                "PUSH_LOCAL_INT 0 \r\n" +
                "PUSH_LOCAL_INT 1 \r\n" +
                "ADD \r\n" +
                "POP_LOCAL_INT 2 \r\n";

            byte[] expected =
            [
                0x00, 0x01, 0x00, 0x00, // PUSH_CONST_INT 1
                0x00, 0x01, 0x00, 0x00, // PUSH_CONST_INT 1
                0x10, 0x00, 0x00, 0x00, // ADD
                0x04, 0x00, 0x00, 0x00, // POP_LOCAL_INT 0
                0x00, 0x01, 0x00, 0x00, // PUSH_CONST_INT 1
                0x00, 0x02, 0x00, 0x00, // PUSH_CONST_INT 2
                0x10, 0x00, 0x00, 0x00, // ADD
                0x04, 0x01, 0x00, 0x00, // POP_LOCAL_INT 1
                0x0b, 0x00, 0x00, 0x00, // PUSH_LOCAL_INT 0
                0x0b, 0x01, 0x00, 0x00, // PUSH_LOCAL_INT 1
                0x10, 0x00, 0x00, 0x00, // ADD
                0x04, 0x02, 0x00, 0x00, // POP_LOCAL_INT 2
            ];

            var result = Assembler.Assemble(test);


            CollectionAssert.AreEqual(expected, result);
        }

        [TestMethod]
        public void IfTest()
        {
            string test =
                "PUSH_CONST_INT 1\r\n" +
                "PUSH_CONST_INT 1\r\n" +
                "ADD \r\n" +
                "POP_LOCAL_INT 0 \r\n" +
                "PUSH_CONST_INT 1\r\n" +
                "PUSH_CONST_INT 2\r\n" +
                "ADD \r\n" +
                "POP_LOCAL_INT 1 \r\n" +
                "PUSH_LOCAL_INT 0 \r\n" +
                "PUSH_LOCAL_INT 1 \r\n" +
                "ADD \r\n" +
                "POP_LOCAL_INT 2 \r\n" +
                "PUSH_LOCAL_INT 0 \r\n" +
                "PUSH_LOCAL_INT 1 \r\n" +
                "EQ\r\n" +
                "JNZ 72\r\n" +
                "PUSH_CONST_INT 0\r\n" +
                "POP_LOCAL_INT 3 \r\n";

            byte[] expected =
            [
                0x00, 0x01, 0x00, 0x00, // PUSH_CONST_INT 1
                0x00, 0x01, 0x00, 0x00, // PUSH_CONST_INT 1
                0x10, 0x00, 0x00, 0x00, // ADD
                0x04, 0x00, 0x00, 0x00, // POP_LOCAL_INT 0
                0x00, 0x01, 0x00, 0x00, // PUSH_CONST_INT 1
                0x00, 0x02, 0x00, 0x00, // PUSH_CONST_INT 2
                0x10, 0x00, 0x00, 0x00, // ADD
                0x04, 0x01, 0x00, 0x00, // POP_LOCAL_INT 1
                0x0b, 0x00, 0x00, 0x00, // PUSH_LOCAL_INT 0
                0x0b, 0x01, 0x00, 0x00, // PUSH_LOCAL_INT 1
                0x10, 0x00, 0x00, 0x00, // ADD
                0x04, 0x02, 0x00, 0x00, // POP_LOCAL_INT 2
                0x0b, 0x00, 0x00, 0x00, // PUSH_LOCAL_INT 0
                0x0b, 0x01, 0x00, 0x00, // PUSH_LOCAL_INT 1
                0x22, 0x00, 0x00, 0x00, // EQ
                0x82, 0x48, 0x00, 0x00, // JNZ 72
                0x00, 0x00, 0x00, 0x00, // PUSH_CONST_INT 0
                0x04, 0x03, 0x00, 0x00, // POP_LOCAL_INT 3
            ];

            var result = Assembler.Assemble(test);


            CollectionAssert.AreEqual(expected, result);
        }
    }
}