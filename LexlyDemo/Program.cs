#region Usings
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using L;
using LC;
#endregion

// Pre-build steps
// "$(SolutionDir)Lexly\bin\Release\Lexly.exe" "$(ProjectDir)Example.lx" /output "$(ProjectDir)ExampleTokenizer.cs" /namespace LexlyDemo
//"$(SolutionDir)Lexly\bin\Release\Lexly.exe" "$(ProjectDir)Slang.lx" /output "$(ProjectDir)SlangTokenizer.cs" /namespace LexlyDemo /noshared
namespace LexlyDemo
{
	using ET = ExampleTokenizer;
	class Program
	{
		
		static void Main(string[] args)
		{
			var text = "foo 123 bar";

			using (var sr = new StreamReader(@"..\..\Program.cs"))
				text = sr.ReadToEnd();
			// our test data - 14 tokens. 29 length
			text = "fubar bar 123 1foo bar 1243 0";
			Console.WriteLine("Lex: " + text);

			var tokenizer = new SlangTokenizer(text); // generated from Example.lx

			Console.WriteLine("Disassembly:");
			Console.WriteLine(Lex.Disassemble(SlangTokenizer.Program));
			Console.WriteLine();
			
			foreach (var tok in tokenizer)
			{
				Console.WriteLine("{0}: {1}", tok.SymbolId, tok.Value);
			}

			var sw = new Stopwatch();
			const int ITER = 1000;
			for (var i = 0; i < ITER; ++i)
			{
				var lc = LexContext.Create(text);
				while (LexContext.EndOfInput != lc.Current)
				{
					lc.ClearCapture();
					sw.Start();
					var acc = Lex.Run(SlangTokenizer.Program, lc);
					sw.Stop();
				}
			}
			Console.WriteLine("Lexed in " + sw.ElapsedMilliseconds / (float)ITER + " msec");
		}
	}
}
