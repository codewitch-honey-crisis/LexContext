using System;
using System.IO;
using System.Reflection;
using L;
using LC;
namespace Lexly
{
	class Program
	{
		static void Main(string[] args)
		{
			var prog = Regex.CompileLexer(
				@"[A-Z_a-z][A-Z_a-z0-9]*", // id
				@"0|(\-?[1-9][0-9]*)", // int
				@"[ \t\r\n\v\f]" // space
			);

			// dump the program to the console
			Console.WriteLine(Regex.ProgramToString(prog));

			// our test data
			var text = "fubar bar 123 1foo bar -243 @#*! 0";
			Console.WriteLine("Lex: " + text);

			var tokenizer = new Tokenizer(prog,new string[3],new int[3], text);
			foreach(var tok in tokenizer)
			{
				Console.WriteLine("{0}: {1}", tok.SymbolId, tok.Value);
			}
		}
	}
}
