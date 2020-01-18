using LC;
using System;
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
			List<_Fiber> clist, nlist, tmp;
			int[] pc;
			int sp=0;
			var sb = new StringBuilder();
			IList<int> saved, matched;

			matched = null;
			saved = new List<int>();
			clist = new List<_Fiber>(prog.Length);
			nlist = new List<_Fiber>(prog.Length);
			_EnqueueFiber(clist, new _Fiber(prog,0, saved), 0);
			matched = null;
			while(0<clist.Count)
			{
				bool passed = false;
				for (i = 0; i < clist.Count; ++i)
				{
					var t = clist[i];
					pc = t.Instruction;
					saved = t.Saved;
					switch (pc[0])
					{
						case Compiler.Char:
							if (input.Current != pc[1])
							{
								break;
							}
							goto case Compiler.Any;
						case Compiler.Set:
							if (!_InRanges(pc, input.Current))
							{
								break;
							}
							goto case Compiler.Any;
						case Compiler.NSet:
							if (_InRanges(pc, input.Current))
							{
								break;
							}
							goto case Compiler.Any;
						case Compiler.UCode:
							if (unchecked((int)char.GetUnicodeCategory(unchecked((char)input.Current))) != pc[1])
							{
								break;
							}
							goto case Compiler.Any;
						case Compiler.NUCode:
							if (unchecked((int)char.GetUnicodeCategory(unchecked((char)input.Current))) == pc[1])
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
							_EnqueueFiber(nlist, new _Fiber(t, t.Index+1, saved), sp+1);
							++sp;
							break;
						case Compiler.Match:
							matched = saved;
							match = pc[1];
							
							// break the for loop:
							i = clist.Count;
							break;
							
					}
				}
				if (passed)
				{
					sb.Append(unchecked((char)input.Current));
					input.Advance();
				}
				tmp = clist;
				clist = nlist;
				nlist = tmp;
				nlist.Clear();
				
			}
			if (null!=matched)
			{
				var start = matched[0];
				var end = matched[1];
				input.CaptureBuffer.Append(sb.ToString(start, end - start));
				return match;
			};
			return -1;
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
		static void _EnqueueFiber(IList<_Fiber> l, _Fiber t, int sp)
		{
			l.Add(t);
			switch (t.Instruction[0])
			{
				case Compiler.Jmp:
					_EnqueueFiber(l, new _Fiber(t, t.Instruction[1],t.Saved),sp);
					break;
				case Compiler.Split:
					for (var j = 1; j < t.Instruction.Length; j++)
						_EnqueueFiber(l, new _Fiber(t.Program, t.Instruction[j],t.Saved),sp);
					break;
				case Compiler.Save:
					var saved = new List<int>(t.Saved.Count);
					for (int ic = t.Saved.Count, i = 0; i < ic; ++i)
						saved.Add(t.Saved[i]);
					var slot = t.Instruction[1];
					while (saved.Count < (slot + 1))
						saved.Add(0);
					saved[slot] = sp;
					_EnqueueFiber(l, new _Fiber(t,t.Index+1, saved), sp);
					break;
			}
		}
		private struct _Fiber
		{
			public readonly int[][] Program;
			public readonly int Index;
			public IList<int> Saved;
			public _Fiber(int[][] program, int index,IList<int> saved)
			{
				Program = program;
				Index = index;
				Saved = saved;
			}
			public _Fiber(_Fiber fiber, int index,IList<int> saved)
			{
				Program = fiber.Program;
				Index = index;
				Saved = saved;
			}
			public int[] Instruction { get { return Program[Index]; } }
		}
		
	}
}
