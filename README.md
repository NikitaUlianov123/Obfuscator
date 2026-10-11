# Obfuscator

A team project (5 developers, 2025 to 2026) to obfuscate C# and RobotC source code, written in C# with the Roslyn API.

## What's here

- **C#Obfuscator**: Roslyn rewriters that rename identifiers, convert loops to labels and `goto`s, rewrite `break` and `continue`, apply De Morgan's laws to conditions, and encode labels.
- **C#DeObfuscator**: reverses those changes, including separate passes that rebuild `for`, `foreach`, `while` and `do-while` loops.
- **DonutMaker**: reshapes obfuscated source text into ASCII art, such as a donut, from a 0/1 mask, so the code still compiles but shows a picture.
- **ObfuscatingTool** and **DeObfuscationTool**: command-line tools that run the full process.
- **TestRunner**, **TestCodeCSharp** and **DonutMaker/TestProject**: sample inputs (BST, FizzBuzz, linked list) and tests.

The RobotC part (a parser, code generation and an assembler for RobotC) is on the `c-team`, `c-team-CodeGen` and `ILGen` branches. It is not merged into `main`.
