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
			var prog = Regex.CompileLexer(@"[ \t\r\n\v\f]+",@"[A-Z_a-z][A-Z_a-z0-9]*",@"0|(\-?[1-9][0-9]*)");
			Console.WriteLine(Regex.ProgramToString(prog));
			//Console.WriteLine("Accepts \"foo\": " + Regex.Accepts(prog, LexContext.Create("foo")));
			var text = "fubar bar 123 1foo bar -243 0";
			Console.WriteLine("Lex: " + text);
			var lc = LexContext.Create(text);
			while(LexContext.EndOfInput!=lc.Current)
			{
				lc.ClearCapture();
				Console.WriteLine("{0}: \"{1}\"", Regex.Lex(prog, lc), lc.GetCapture());
			}
		}
	}
}
