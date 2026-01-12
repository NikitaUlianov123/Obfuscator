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


            CollectionAssert.AreEqual(expected, result.code);
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


            CollectionAssert.AreEqual(expected, result.code);
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


            CollectionAssert.AreEqual(expected, result.code);
        }

        [TestMethod]
        public void FunctionTest()
        {
            string test = 
                "motorSet: \r\n" +
                "PUSH_CONST_CHAR 1 \r\n" +
                "MOTOR_SET \r\n" +
                "PUSH_CONST_CHAR 2 \r\n" +
                "MOTOR_SET \r\n" +
                "RET \r\n" +
                "Entry_Point* \r\n" +
                "PUSH_CONST_INT 100\r\n" +
                "PUSH_CONST_INT 100\r\n" +
                "POP_LOCAL_INT 0 \r\n" +
                "POP_LOCAL_INT 1 \r\n" +
                "CALL 0 \r\n";

            byte[] expectedHeap =
            [
                0x01, 0x01, 0x00, 0x00, // PUSH_CONST_CHAR 1
                0x50, 0x00, 0x00, 0x00, // MOTOR_SET
                0x01, 0x02, 0x00, 0x00, // PUSH_CONST_CHAR 2
                0x50, 0x00, 0x00, 0x00, // MOTOR_SET
                0x71, 0x00, 0x00, 0x00, // RET
            ];

            byte[] expected =
            [
                0x00, 0x64, 0x00, 0x00, // PUSH_CONST_INT 100
                0x00, 0x64, 0x00, 0x00, // PUSH_CONST_INT 100
                0x04, 0x00, 0x00, 0x00, // POP_LOCAL_INT 0
                0x04, 0x01, 0x00, 0x00, // POP_LOCAL_INT 1
                0x70, 0x00, 0x00, 0x00, // CALL 0
            ];

            var result = Assembler.Assemble(test);


            CollectionAssert.AreEqual(expected, result.code);
            CollectionAssert.AreEqual(expectedHeap, result.heap);
        }

        [TestMethod]
        public void LoganTest()
        {
            string test =
                "moveForDegrees: \r\n" +
                "PUSH_CONST_CHAR 0 \r\n" +
                "MOTOR_SET \r\n" +
                "PUSH_CONST_CHAR 1 \r\n" +
                "MOTOR_SET \r\n" +
                "PUSH_CONST_CHAR 0 \r\n" +
                "ENCODER_GET \r\n" +
                "LT \r\n" +
                "JNZ 11 \r\n" +
                "JMP 6 \r\n" +
                "PUSH_CONST_INT 0\r\n" +
                "PUSH_CONST_CHAR 0 \r\n" +
                "MOTOR_SET \r\n" +
                "PUSH_CONST_INT 0\r\n" +
                "PUSH_CONST_CHAR 1 \r\n" +
                "MOTOR_SET \r\n" +
                "moveUntillRed: \r\n" +
                "PUSH_CONST_CHAR 0 \r\n" +
                "MOTOR_SET \r\n" +
                "PUSH_CONST_CHAR 1 \r\n" +
                "MOTOR_SET \r\n" +
                "PUSH_LOCAL_CHAR 0 \r\n" +
                "PUSH_CONST_CHAR 5 \r\n" +
                "EQ \r\n" +
                "JNZ 37 \r\n" +
                "JMP 32 \r\n" +
                "PUSH_CONST_INT 0\r\n" +
                "PUSH_CONST_CHAR 0 \r\n" +
                "MOTOR_SET \r\n" +
                "PUSH_CONST_INT 0\r\n" +
                "PUSH_CONST_CHAR 1 \r\n" +
                "MOTOR_SET \r\n" +
                "RET \r\n" +
                "Entry_Point* \r\n" +
                "PUSH_CONST_INT 100\r\n" +
                "PUSH_CONST_INT 100\r\n" +
                "PUSH_CONST_INT 5000\r\n" +
                "CALL 0 \r\n";

            byte[] expectedHeap =
            [
                0x01, 0x00, 0x00, 0x00, // PUSH_CONST_CHAR 0
                0x50, 0x00, 0x00, 0x00, // MOTOR_SET
                0x01, 0x01, 0x00, 0x00, // PUSH_CONST_CHAR 1
                0x50, 0x00, 0x00, 0x00, // MOTOR_SET
                0x01, 0x00, 0x00, 0x00, // PUSH_CONST_CHAR 0
                0x53, 0x00, 0x00, 0x00, // ENCODER_GET
                0x20, 0x00, 0x00, 0x00, // LT
                0x82, 0x0b, 0x00, 0x00, // JNZ 11
                0x80, 0x06, 0x00, 0x00, // JMP 6
                0x00, 0x00, 0x00, 0x00, // PUSH_CONST_INT 0
                0x01, 0x00, 0x00, 0x00, // PUSH_CONST_CHAR 0
                0x50, 0x00, 0x00, 0x00, // MOTOR_SET
                0x00, 0x00, 0x00, 0x00, // PUSH_CONST_INT 0
                0x01, 0x01, 0x00, 0x00, // PUSH_CONST_CHAR 1
                0x50, 0x00, 0x00, 0x00, // MOTOR_SET
                0x01, 0x00, 0x00, 0x00, // PUSH_CONST_CHAR 0
                0x50, 0x00, 0x00, 0x00, // MOTOR_SET
                0x01, 0x01, 0x00, 0x00, // PUSH_CONST_CHAR 1
                0x50, 0x00, 0x00, 0x00, // MOTOR_SET
                0x0C, 0x00, 0x00, 0x00, // PUSH_LOCAL_CHAR 0
                0x01, 0x05, 0x00, 0x00, // PUSH_CONST_CHAR 5
                0x22, 0x00, 0x00, 0x00, // EQ
                0x82, 0x25, 0x00, 0x00, // JNZ 37
                0x80, 0x20, 0x00, 0x00, // JMP 32
                0x00, 0x00, 0x00, 0x00, // PUSH_CONST_INT 0
                0x01, 0x00, 0x00, 0x00, // PUSH_CONST_CHAR 0
                0x50, 0x00, 0x00, 0x00, // MOTOR_SET
                0x00, 0x00, 0x00, 0x00, // PUSH_CONST_INT 0
                0x01, 0x01, 0x00, 0x00, // PUSH_CONST_CHAR 1
                0x50, 0x00, 0x00, 0x00, // MOTOR_SET
                0x71, 0x00, 0x00, 0x00, // RET
            ];

            byte[] expected =
            [
                0x00, 0x64, 0x00, 0x00, // PUSH_CONST_INT 100
                0x00, 0x64, 0x00, 0x00, // PUSH_CONST_INT 100
                0x00, 0x13, 0x88, 0x00, // PUSH_CONST_INT 5000
                0x70, 0x00, 0x00, 0x00, // CALL 0
            ];

            var result = Assembler.Assemble(test);


            CollectionAssert.AreEqual(expected, result.code);
            CollectionAssert.AreEqual(expectedHeap, result.heap);
        }

        [TestMethod]
        public void PackageTest()
        {
            string test =
                "motorSet: \r\n" +
                "PUSH_CONST_CHAR 1 \r\n" +
                "MOTOR_SET \r\n" +
                "PUSH_CONST_CHAR 2 \r\n" +
                "MOTOR_SET \r\n" +
                "RET \r\n" +
                "Entry_Point* \r\n" +
                "PUSH_CONST_INT 100\r\n" +
                "PUSH_CONST_INT 100\r\n" +
                "POP_LOCAL_INT 0 \r\n" +
                "POP_LOCAL_INT 1 \r\n" +
                "CALL 0 \r\n";

            byte[] expectedHeap =
            [
                0x01, 0x01, 0x00, 0x00, // PUSH_CONST_CHAR 1
                0x50, 0x00, 0x00, 0x00, // MOTOR_SET
                0x01, 0x02, 0x00, 0x00, // PUSH_CONST_CHAR 2
                0x50, 0x00, 0x00, 0x00, // MOTOR_SET
                0x71, 0x00, 0x00, 0x00, // RET
            ];

            byte[] expected =
            [
                0x00, 0x64, 0x00, 0x00, // PUSH_CONST_INT 100
                0x00, 0x64, 0x00, 0x00, // PUSH_CONST_INT 100
                0x04, 0x00, 0x00, 0x00, // POP_LOCAL_INT 0
                0x04, 0x01, 0x00, 0x00, // POP_LOCAL_INT 1
                0x70, 0x00, 0x00, 0x00, // CALL 0
            ];

            Assembler.PackageFile(test, "./output.c");
            var result = Assembler.UnpackageFile("./output.c");

            CollectionAssert.AreEqual(expected, result.code);
            CollectionAssert.AreEqual(expectedHeap, result.heap);
            Assert.AreEqual(test, result.original);
        }

        [TestMethod]
        public void PackageTest2()
        {
            string test =
                "moveForDegrees: \r\n" +
                "PUSH_CONST_CHAR 0 \r\n" +
                "MOTOR_SET \r\n" +
                "PUSH_CONST_CHAR 1 \r\n" +
                "MOTOR_SET \r\n" +
                "PUSH_CONST_CHAR 0 \r\n" +
                "ENCODER_GET \r\n" +
                "LT \r\n" +
                "JNZ 11 \r\n" +
                "JMP 6 \r\n" +
                "PUSH_CONST_INT 0\r\n" +
                "PUSH_CONST_CHAR 0 \r\n" +
                "MOTOR_SET \r\n" +
                "PUSH_CONST_INT 0\r\n" +
                "PUSH_CONST_CHAR 1 \r\n" +
                "MOTOR_SET \r\n" +
                "moveUntillRed: \r\n" +
                "PUSH_CONST_CHAR 0 \r\n" +
                "MOTOR_SET \r\n" +
                "PUSH_CONST_CHAR 1 \r\n" +
                "MOTOR_SET \r\n" +
                "PUSH_LOCAL_CHAR 0 \r\n" +
                "PUSH_CONST_CHAR 5 \r\n" +
                "EQ \r\n" +
                "JNZ 37 \r\n" +
                "JMP 32 \r\n" +
                "PUSH_CONST_INT 0\r\n" +
                "PUSH_CONST_CHAR 0 \r\n" +
                "MOTOR_SET \r\n" +
                "PUSH_CONST_INT 0\r\n" +
                "PUSH_CONST_CHAR 1 \r\n" +
                "MOTOR_SET \r\n" +
                "RET \r\n" +
                "Entry_Point* \r\n" +
                "PUSH_CONST_INT 100\r\n" +
                "PUSH_CONST_INT 100\r\n" +
                "PUSH_CONST_INT 5000\r\n" +
                "CALL 0 \r\n";

            byte[] expectedHeap =
            [
                0x01, 0x00, 0x00, 0x00, // PUSH_CONST_CHAR 0
                0x50, 0x00, 0x00, 0x00, // MOTOR_SET
                0x01, 0x01, 0x00, 0x00, // PUSH_CONST_CHAR 1
                0x50, 0x00, 0x00, 0x00, // MOTOR_SET
                0x01, 0x00, 0x00, 0x00, // PUSH_CONST_CHAR 0
                0x53, 0x00, 0x00, 0x00, // ENCODER_GET
                0x20, 0x00, 0x00, 0x00, // LT
                0x82, 0x0b, 0x00, 0x00, // JNZ 11
                0x80, 0x06, 0x00, 0x00, // JMP 6
                0x00, 0x00, 0x00, 0x00, // PUSH_CONST_INT 0
                0x01, 0x00, 0x00, 0x00, // PUSH_CONST_CHAR 0
                0x50, 0x00, 0x00, 0x00, // MOTOR_SET
                0x00, 0x00, 0x00, 0x00, // PUSH_CONST_INT 0
                0x01, 0x01, 0x00, 0x00, // PUSH_CONST_CHAR 1
                0x50, 0x00, 0x00, 0x00, // MOTOR_SET
                0x01, 0x00, 0x00, 0x00, // PUSH_CONST_CHAR 0
                0x50, 0x00, 0x00, 0x00, // MOTOR_SET
                0x01, 0x01, 0x00, 0x00, // PUSH_CONST_CHAR 1
                0x50, 0x00, 0x00, 0x00, // MOTOR_SET
                0x0C, 0x00, 0x00, 0x00, // PUSH_LOCAL_CHAR 0
                0x01, 0x05, 0x00, 0x00, // PUSH_CONST_CHAR 5
                0x22, 0x00, 0x00, 0x00, // EQ
                0x82, 0x25, 0x00, 0x00, // JNZ 37
                0x80, 0x20, 0x00, 0x00, // JMP 32
                0x00, 0x00, 0x00, 0x00, // PUSH_CONST_INT 0
                0x01, 0x00, 0x00, 0x00, // PUSH_CONST_CHAR 0
                0x50, 0x00, 0x00, 0x00, // MOTOR_SET
                0x00, 0x00, 0x00, 0x00, // PUSH_CONST_INT 0
                0x01, 0x01, 0x00, 0x00, // PUSH_CONST_CHAR 1
                0x50, 0x00, 0x00, 0x00, // MOTOR_SET
                0x71, 0x00, 0x00, 0x00, // RET
            ];

            byte[] expected =
            [
                0x00, 0x64, 0x00, 0x00, // PUSH_CONST_INT 100
                0x00, 0x64, 0x00, 0x00, // PUSH_CONST_INT 100
                0x00, 0x13, 0x88, 0x00, // PUSH_CONST_INT 5000
                0x70, 0x00, 0x00, 0x00, // CALL 0
            ];

            Assembler.PackageFile(test, "./output.c");
            var result = Assembler.UnpackageFile("./output.c");

            CollectionAssert.AreEqual(expected, result.code);
            CollectionAssert.AreEqual(expectedHeap, result.heap);
            Assert.AreEqual(test, result.original);
        }
    }
}