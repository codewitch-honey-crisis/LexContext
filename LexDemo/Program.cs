using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using L;
using LC;
namespace LexDemo
{
	class Program
	{
		static void Main(string[] args)
		{
			// compile a lexer
			var prog = Regex.CompileLexer(
				@"[A-Z_a-z][A-Z_a-z0-9]*", // id
				@"0|(\-?[1-9][0-9]*)", // int
				@"[ \t\r\n\v\f]" // space
			);
			
			// dump the program to the console
			Console.WriteLine(Regex.ProgramToString(prog));
			
			// our test data
			var text = "fubar bar 123 1foo bar -243 0";
			Console.WriteLine("Lex: " + text);

			// spin up a lexer context
			// see: https://www.codeproject.com/Articles/5256794/LexContext-A-streamlined-cursor-over-a-text-input
			var lc = LexContext.Create(text);
			
			// while more input to be read
			while(LexContext.EndOfInput!=lc.Current)
			{
				// clear any current captured data
				lc.ClearCapture();
				// lex our next input and dump it
				Console.WriteLine("{0}: \"{1}\"", Regex.Lex(prog, lc), lc.GetCapture());
			}
		}
	}
}
