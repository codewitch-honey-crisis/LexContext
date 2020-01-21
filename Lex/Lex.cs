using LC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace L
{
	/// <summary>
	/// Provides services for assembling and disassembling lexers, and for compiling regular expressions into lexers
	/// </summary>
#if LLIB
	public
#endif
	static class Lex
	{
		public static int[] GetCharacterClass(string name)
		{
			if (null == name)
				throw new ArgumentNullException(nameof(name));
			if (0 == name.Length)
				throw new ArgumentException("The character class name must not be empty.", nameof(name));
			int[] result;
			if (!CharCls.CharacterClasses.TryGetValue(name, out result))
				throw new ArgumentException("The character class " + name + " was not found", nameof(name));
			return result;
		}
		/// <summary>
		/// Assembles the assembly code into a program
		/// </summary>
		/// <param name="asmCode">The code to assemble</param>
		/// <returns>A program</returns>
		public static int[][] Assemble(LexContext asmCode)
		{
			return Assembler.Emit(Assembler.Parse(asmCode)).ToArray();
		}
		/// <summary>
		/// Assembles the assembly code into a program
		/// </summary>
		/// <param name="asmCode">The code to assemble</param>
		/// <returns>A program</returns>
		public static int[][] Assemble(string asmCode)
		{
			var lc = LexContext.Create(asmCode);
			return Assembler.Emit(Assembler.Parse(lc)).ToArray();
		}
		/// <summary>
		/// Assembles the assembly code from the <see cref="TextReader"/>
		/// </summary>
		/// <param name="asmCodeReader">A reader that will read the assembly code</param>
		/// <returns>A program</returns>
		public static int[][] AssembleFrom(TextReader asmCodeReader)
		{
			var lc = LexContext.CreateFrom(asmCodeReader);
			return Assembler.Emit(Assembler.Parse(lc)).ToArray();
		}
		/// <summary>
		/// Assembles the assembly code from the specified file
		/// </summary>
		/// <param name="asmFile">A file containing the assembly code</param>
		/// <returns>A program</returns>
		public static int[][] AssembleFrom(string asmFile)
		{
			var lc = LexContext.CreateFrom(asmFile);
			return Assembler.Emit(Assembler.Parse(lc)).ToArray();
		}
		/// <summary>
		/// Assembles the assembly code from the specified url
		/// </summary>
		/// <param name="asmUrl">An URL that points to the assembly code</param>
		/// <returns>A program</returns>
		public static int[][] AssembleFromUrl(string asmUrl)
		{
			var lc = LexContext.CreateFromUrl(asmUrl);
			return Assembler.Emit(Assembler.Parse(lc)).ToArray();
		}
		/// <summary>
		/// Compiles a single regular expression into a program segment
		/// </summary>
		/// <param name="input">The expression to compile</param>
		/// <returns>A part of a program</returns>
		public static int[][] CompileRegexPart(LexContext input)
		{
			var ast = Ast.Parse(input);
			var prog = new List<int[]>();
			Compiler.EmitPart(ast, prog);
			return prog.ToArray();
		}
		/// <summary>
		/// Compiles a single regular expression into a program segment
		/// </summary>
		/// <param name="expression">The expression to compile</param>
		/// <returns>A part of a program</returns>
		public static int[][] CompileRegexPart(string expression)
		{
			return CompileRegexPart(LexContext.Create(expression));
		}
		/// <summary>
		/// Compiles a single literal expression into a program segment
		/// </summary>
		/// <param name="input">The expression to compile</param>
		/// <returns>A part of a program</returns>
		public static int[][] CompileLiteralPart(LexContext input)
		{
			var ll = input.CaptureBuffer.Length;
			while (-1 != input.Current)
				input.Capture();
			return CompileLiteralPart(input.GetCapture(ll));
		}
		/// <summary>
		/// Compiles a single literal expression into a program segment
		/// </summary>
		/// <param name="expression">The expression to compile</param>
		/// <returns>A part of a program</returns>
		public static int[][] CompileLiteralPart(string expression)
		{
			var prog = new List<int[]>();
			Compiler.EmitPart(expression, prog);
			return prog.ToArray();
		}

		/// <summary>
		/// Compiles a series of regular expressions into a program
		/// </summary>
		/// <param name="expressions">The expressions</param>
		/// <returns>A program</returns>
		public static int[][] CompileLexerRegex(params string[] expressions)
		{
			var asts = new Ast[expressions.Length];
			for(var i = 0;i<expressions.Length;++i)
				asts[i] = Ast.Parse(LexContext.Create(expressions[i]));
			return Compiler.EmitLexer(asts);
		}
		/// <summary>
		/// Links a series of partial programs together into single lexer program
		/// </summary>
		/// <param name="parts">The parts</param>
		/// <returns>A program</returns>
		public static int[][] LinkLexerParts(IEnumerable<KeyValuePair<int,int[][]>> parts)
		{
			return Compiler.EmitLexer(parts);
		}
		/// <summary>
		/// Disassembles the specified program
		/// </summary>
		/// <param name="program">The program</param>
		/// <returns>A string containing the assembly code for the program</returns>
		public static string Disassemble(int[][] program)
		{
			return Compiler.ToString(program);
		}
		/// <summary>
		/// Indicates whether or not the program matches the entire input specified
		/// </summary>
		/// <param name="prog">The program</param>
		/// <param name="input">The input to check</param>
		/// <returns>True if the input was matched, otherwise false</returns>
		public static bool IsMatch(int[][] prog,LexContext input)
		{
			return -1 != Run(prog, input) && input.Current==LexContext.EndOfInput;
		}
		/// <summary>
		/// Runs the specified program over the specified input
		/// </summary>
		/// <param name="prog">The program to run</param>
		/// <param name="input">The input to match</param>
		/// <returns>The id of the match, or -1 for an error. <see cref="LexContext.CaptureBuffer"/> contains the captured value.</returns>
		public static int Run(int[][] prog,LexContext input)
		{
			input.EnsureStarted();
			int i,match=-1;
			_Fiber[] currentFibers, nextFibers, tmp;
			int currentFiberCount=0, nextFiberCount=0;
			int[] pc;
			// position in input
			int sp=0;
			// stores our captured input
			var sb = new StringBuilder(64);
			int[] saved, matched;
			saved = new int[2];
			currentFibers = new _Fiber[prog.Length];
			nextFibers = new _Fiber[prog.Length];
			_EnqueueFiber(ref currentFiberCount, ref currentFibers, new _Fiber(prog,0, saved), 0);
			matched = null;
			var cur = -1;
			if(LexContext.EndOfInput!=input.Current)
			{
				var ch1 = unchecked((char)input.Current);
				if (char.IsHighSurrogate(ch1))
				{
					if (-1 == input.Advance())
						throw new ExpectingException("Expecting low surrogate in unicode stream. The input source is corrupt or not valid Unicode",input.Line,input.Column,input.Position,input.FileOrUrl) ;
					var ch2 = unchecked((char)input.Current);
					cur = char.ConvertToUtf32(ch1, ch2);
				}
				else
					cur = ch1;
				
			}
			
			while(0<currentFiberCount)
			{
				bool passed = false;
				for (i = 0; i < currentFiberCount; ++i)
				{
					var t = currentFibers[i];
					pc = t.Program[t.Index];
					saved = t.Saved;
					switch (pc[0])
					{
						case Compiler.Char:
							if (cur!= pc[1])
							{
								break;
							}
							goto case Compiler.Any;
						case Compiler.Set:
							if (!_InRanges(pc, cur))
							{
								break;
							}
							goto case Compiler.Any;
						case Compiler.NSet:
							if (_InRanges(pc, cur))
							{
								break;
							}
							goto case Compiler.Any;
						case Compiler.UCode:
							var str = char.ConvertFromUtf32(cur);
							if (unchecked((int)char.GetUnicodeCategory(str,0) != pc[1]))
							{
								break;
							}
							goto case Compiler.Any;
						case Compiler.NUCode:
							str = char.ConvertFromUtf32(cur);
							if (unchecked((int)char.GetUnicodeCategory(str,0)) == pc[1])
							{
								break;
							}
							goto case Compiler.Any;

						case Compiler.Any:
							if (LexContext.EndOfInput==input.Current)
							{
								break;
							}
							passed = true;
							_EnqueueFiber(ref nextFiberCount, ref nextFibers, new _Fiber(t, t.Index+1, saved), sp+1);

							break;
						case Compiler.Match:
							matched = saved;
							match = pc[1];
							
							// break the for loop:
							i = currentFiberCount;
							break;
							
					}
				}
				if (passed)
				{
					sb.Append(char.ConvertFromUtf32(cur));
					input.Advance();
					if (LexContext.EndOfInput != input.Current)
					{
						var ch1 = unchecked((char)input.Current);
						if (char.IsHighSurrogate(ch1))
						{
							input.Advance();
							if (-1 == input.Advance())
								throw new ExpectingException("Expecting low surrogate in unicode stream. The input source is corrupt or not valid Unicode", input.Line, input.Column, input.Position, input.FileOrUrl);
							++sp;
							var ch2 = unchecked((char)input.Current);
							cur = char.ConvertToUtf32(ch1, ch2);
						}
						else
							cur = ch1;
						
					}
					++sp;
				}
				tmp = currentFibers;
				currentFibers = nextFibers;
				nextFibers = tmp;
				currentFiberCount = nextFiberCount;
				nextFiberCount = 0;
				
			}

			if (null!=matched)
			{
				var start = matched[0];
				// this is actually the point just past the end
				// of the match, but we can treat it as the length
				var len = matched[1];
				input.CaptureBuffer.Append(sb.ToString(start, len - start));
				return match;
			};
			return -1; // error symbol
		}
		
		static bool _InRanges(int[] pc,int ch)
		{
			var found = false;
			// go through all the ranges to see if we matched anything.
			for (var j = 1; j < pc.Length; ++j)
			{
				// grab our range from the packed ranges into first and last
				var first = pc[j];
				++j;
				var last = pc[j];
				// do a quick search through our ranges
				if (ch <= last)
				{
					if (first <= ch)
						found = true;
					break;
				}
			}
			return found;
		}
		static void _EnqueueFiber(ref int lcount,ref _Fiber[] l, _Fiber t, int sp)
		{
			// really shouldn't happen, but maybe it might
			if(l.Length<=lcount)
			{
				var newarr = new _Fiber[l.Length * 2];
				Array.Copy(l, 0, newarr, 0, l.Length);
				l = newarr;
			}
			l[lcount] = t;
			++lcount;
			var pc = t.Program[t.Index];
			switch (pc[0])
			{
				case Compiler.Jmp:
					_EnqueueFiber(ref lcount,ref l, new _Fiber(t, pc[1],t.Saved),sp);
					break;
				case Compiler.Split:
					for (var j = 1; j < pc.Length; j++)
						_EnqueueFiber(ref lcount,ref l, new _Fiber(t.Program, pc[j],t.Saved),sp);
					break;
				case Compiler.Save:
					var slot = pc[1];
					var max = slot > t.Saved.Length ? slot : t.Saved.Length;
					var saved = new int[max];
					for (var i = 0;i<t.Saved.Length;++i)
						saved[i]=t.Saved[i];
					saved[slot] = sp;
					_EnqueueFiber(ref lcount,ref l, new _Fiber(t,t.Index+1, saved), sp);
					break;
			}
		}
		
		private struct _Fiber
		{
			public readonly int[][] Program;
			public readonly int Index;
			public int[] Saved;
			public _Fiber(int[][] program, int index,int[] saved)
			{
				Program = program;
				Index = index;
				Saved = saved;
			}
			public _Fiber(_Fiber fiber, int index,int[] saved)
			{
				Program = fiber.Program;
				Index = index;
				Saved = saved;
			}
		}
	}
}
