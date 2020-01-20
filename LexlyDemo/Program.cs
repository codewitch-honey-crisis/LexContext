using System;
using System.Collections.Generic;
using System.IO;
using L;
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
			var tokenizer = new SlangTokenizer(text); // generated from Example.lx

			Console.WriteLine("Disassembly:");
			Console.WriteLine(Lex.Disassemble(SlangTokenizer.Program));
			Console.WriteLine();
			Console.WriteLine("Lex: " + text);
			foreach (var tok in tokenizer)
			{
				Console.WriteLine("{0}: {1}", tok.SymbolId, tok.Value);
			}
		}
	}
}
