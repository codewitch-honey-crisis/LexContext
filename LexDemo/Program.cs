using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using L;
using LC;
namespace LexDemo
{
	class Program
	{
		static void Main()
		{
			//Console.WriteLine(Lex.Disassemble(Lex.CompileRegexPart("[_[:IsLetter:]][_[:IsLetterOrDigit:]]*")));
			_RunLexer();
		}
		
		static void _RunLexer()
		{

			// compile a lexer
			var prog = Lex.CompileLexerRegex(
				@"[A-Z_a-z][A-Z_a-z0-9]*", // id
				@"0|(\-?[1-9][0-9]*)", // int
				@"( |\t|\r|\n|\v|\f)" // space
			);
			
			// dump the program to the console
			Console.WriteLine(Lex.Disassemble(prog));

			// our test data
			var text = "foo 123 bar";//"fubar bar 123 1foo bar -243 @#*! 0";
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
				Console.WriteLine("{0}: \"{1}\"", Lex.Run(prog, lc), lc.GetCapture());
			}
			var sw = new Stopwatch();
			const int ITER = 1000;
			for(var i = 0;i<ITER;++i)
			{
				lc = LexContext.Create(text);
				while (LexContext.EndOfInput != lc.Current)
				{
					lc.ClearCapture();
					sw.Start();
					var acc = Lex.Run(prog, lc);
					sw.Stop();
				}
			}
			Console.WriteLine("Lexed in " + sw.ElapsedMilliseconds / (float)ITER + " msec");
		}
	}
}
