using System;
using System.Collections.Generic;
using L;
namespace LexlyDemo
{
	using ET = ExampleTokenizer;
	class Program
	{
		
		static void Main(string[] args)
		{
			var text = "foo 123 bar";
			var tokenizer = new ExampleTokenizer(text); // generated from Example.lx

			Console.WriteLine("Disassembly:");
			Console.WriteLine(Lex.Disassemble(ExampleTokenizer.Program));
			Console.WriteLine();
			Console.WriteLine("Lex: " + text);
			foreach (var tok in tokenizer)
			{
				switch(tok.SymbolId)
				{
					case ET.id:
						Console.WriteLine("Input was id: " + tok.Value);
						break;
					case ET.@int:
						Console.WriteLine("Input was int: " + tok.Value);
						break;
					case ET.space:
						Console.WriteLine("Input was space: " + tok.Value);
						break;
					default:
						Console.WriteLine("Error in input: " + tok.Value);
						break;
				}
			}
		}
	}
}
