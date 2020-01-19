using LC;
using System.Collections.Generic;
using System.Text;

namespace L
{
	public static class Regex
	{
		public static int[][] Compile(string expression)
		{
			var ast = Ast.Parse(LexContext.Create(expression));
			var prog = new List<int[]>();
			return Compiler.Emit(ast, 0).ToArray();
		}
		
		public static int[][] CompileLexer(params string[] expressions)
		{
			var asts = new Ast[expressions.Length];
			for(var i = 0;i<expressions.Length;++i)
				asts[i] = Ast.Parse(LexContext.Create(expressions[i]));
			return Compiler.EmitLexer(asts);
		}
		public static string ProgramToString(int[][] program)
		{
			return Compiler.ToString(program);
		}
		
		public static bool Accepts(int[][] prog,LexContext input)
		{
			return -1 != Lex(prog, input) && input.Current==LexContext.EndOfInput;
		}
		public static int Lex(int[][] prog,LexContext input)
		{
			input.EnsureStarted();
			int i,match=-1;
			_Fiber[] currentFibers, nextFibers, tmp;
			int currentFiberCount=0, nextFiberCount=0;
			int[] pc;
			int sp=0;
			var sb = new StringBuilder(64);
			int[] saved, matched;
			saved = new int[2];
			currentFibers = new _Fiber[prog.Length];
			nextFibers = new _Fiber[prog.Length];
			_EnqueueFiber(ref currentFiberCount, currentFibers, new _Fiber(prog,0, saved), 0);
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
							_EnqueueFiber(ref nextFiberCount, nextFibers, new _Fiber(t, t.Index+1, saved), sp+1);

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
		static void _EnqueueFiber(ref int lcount,_Fiber[] l, _Fiber t, int sp)
		{
			l[lcount] = t;
			++lcount;
			var pc = t.Program[t.Index];
			switch (pc[0])
			{
				case Compiler.Jmp:
					_EnqueueFiber(ref lcount,l, new _Fiber(t, pc[1],t.Saved),sp);
					break;
				case Compiler.Split:
					for (var j = 1; j < pc.Length; j++)
						_EnqueueFiber(ref lcount,l, new _Fiber(t.Program, pc[j],t.Saved),sp);
					break;
				case Compiler.Save:
					var slot = pc[1];
					var max = slot > t.Saved.Length ? slot : t.Saved.Length;
					var saved = new int[max];
					for (var i = 0;i<t.Saved.Length;++i)
						saved[i]=t.Saved[i];
					saved[slot] = sp;
					_EnqueueFiber(ref lcount,l, new _Fiber(t,t.Index+1, saved), sp);
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
