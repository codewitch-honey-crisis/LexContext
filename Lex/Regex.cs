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
			_Sub sub, matched;

			matched = null;
			sub = new _Sub();
			clist = new List<_Fiber>(prog.Length);
			nlist = new List<_Fiber>(prog.Length);
			_AddThread(clist, new _Fiber(prog,0, sub), 0);
			matched = null;
			while(0<clist.Count)
			{
				bool ok = false;
				for (i = 0; i < clist.Count; ++i)
				{
					var t = clist[i];
					pc = t.Instruction;
					sub = t.Sub;
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
							ok = true;
							_AddThread(nlist, new _Fiber(t, t.Index+1, sub), sp+1);
							++sp;
							break;
						case Compiler.Match:
							matched = sub;
							match = pc[1];
							
							// break the for loop:
							i = clist.Count;
							break;
							
					}
				}
				if (ok)
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
				var start = matched.Indices[0];
				var end = matched.Indices[1];
				input.CaptureBuffer.Append(sb.ToString(start, end - start));
				//for (i = 0; i < nsubp; i++)
				//	subp[i] = matched->sub[i];
				//decref(matched);
				System.Diagnostics.Debug.WriteLine("Read: " + sb.ToString());
				return match;
			}
			System.Diagnostics.Debug.WriteLine("Read: " + sb.ToString());
			return -1;
		}
		private sealed class _Sub
		{
			public List<int> Indices = new List<int>();
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
		static void _AddThread(IList<_Fiber> l, _Fiber t, int sp)
		{
			l.Add(t);
			switch (t.Instruction[0])
			{
				case Compiler.Jmp:
					_AddThread(l, new _Fiber(t, t.Instruction[1],t.Sub),sp);
					break;
				case Compiler.Split:
					for (var j = 1; j < t.Instruction.Length; j++)
						_AddThread(l, new _Fiber(t.Program, t.Instruction[j],t.Sub),sp);
					break;
				case Compiler.Save:
					var sub = new _Sub();
					for (int ic = t.Sub.Indices.Count, i = 0; i < ic; ++i)
						sub.Indices.Add(t.Sub.Indices[i]);
					var slot = t.Instruction[1];
					while (sub.Indices.Count < (slot + 1))
						sub.Indices.Add(0);
					sub.Indices[slot] = sp;
					_AddThread(l, new _Fiber(t,t.Index+1, sub), sp);
					break;
				/*default:
					l.Add(t);
					break;*/
			}
		}
		private struct _Fiber
		{
			public readonly int[][] Program;
			public readonly int Index;
			public _Sub Sub;
			public _Fiber(int[][] program, int index,_Sub sub)
			{
				Program = program;
				Index = index;
				Sub = sub;
			}
			public _Fiber(_Fiber fiber, int index,_Sub sub)
			{
				Program = fiber.Program;
				Index = index;
				Sub = sub;
			}
			public int[] Instruction { get { return Program[Index]; } }
		}
		
	}
}
