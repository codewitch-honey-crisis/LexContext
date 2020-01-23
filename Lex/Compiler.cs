﻿using System;
using System.Collections.Generic;
using System.Text;

namespace L
{
	static class Compiler
	{
		#region Opcodes
		internal const int Match = 1; // match symbol
		internal const int Jmp = 2; // jmp addr1, addr2
		internal const int Switch = 3; // switch [ case <ranges>:<label> { , case <ranges>:<label> }] [, default: <label> {, <label> } ]
		internal const int Any = 4; // any
		internal const int Char = 5; // char ch
		internal const int Set = 6; // set packedRange1Left,packedRange1Right,packedRange2Left,packedRange2Right...
		internal const int NSet = 7; // nset packedRange1Left,packedRange1Right,packedRange2Left,packedRange2Right...
		internal const int UCode = 8; // ucode cat
		internal const int NUCode = 9; // nucode cat
		internal const int Save = 10; // save slot
		#endregion
		internal static List<int[]> Emit(Ast ast,int symbolId = -1)
		{
			var prog = new List<int[]>();
			EmitPart(ast,prog);
			if (-1 != symbolId)
			{
				var match = new int[2];
				match[0] = Match;
				match[1] = symbolId;
				prog.Add(match);
			}
			return prog;
		}
		internal static void EmitPart(string literal, IList<int[]> prog)
		{
			for (var i = 0; i < literal.Length; ++i)
			{
				int ch = literal[i];
				if (char.IsHighSurrogate(literal[i]))
				{
					if (i == literal.Length - 1)
						throw new ArgumentException("The literal contains an incomplete unicode surrogate.", nameof(literal));
					ch = char.ConvertToUtf32(literal, i);
					++i;
				}
				var lit = new int[2];
				lit[0] = Char;
				lit[1] = ch;
				prog.Add(lit);
			}
		}
		internal static void EmitPart(Ast ast, IList<int[]> prog)
		{
			
			int[] inst,jmp;
			switch(ast.Kind)
			{
				case Ast.Lit: // literal value
					// char <ast.Value>
					inst = new int[2];
					inst[0] = Char;
					inst[1] = ast.Value;
					prog.Add(inst);
					break;
				case Ast.Cat: // concatenation
					for(var i = 0;i<ast.Exprs.Length;i++)
						if(null!=ast.Exprs[i])
							EmitPart(ast.Exprs[i],prog);
					break;
				case Ast.Dot: // dot/any
					inst = new int[1];
					inst[0] = Any;
					prog.Add(inst);
					break;
				case Ast.Alt: // alternation
					
					// be sure to handle the cases where one
					// of the children is null such as
					// in (foo|) or (|foo)
					var exprs = new List<Ast>(ast.Exprs.Length);
					var firstNull = -1;
					for (var i = 0; i < ast.Exprs.Length; i++)
					{
						var e = ast.Exprs[i];
						if (null == e)
						{
							if (0 > firstNull)
							{
								firstNull = i;
								exprs.Add(null);
							}
							continue;
						}
						exprs.Add(e);
					}
					ast.Exprs = exprs.ToArray();
					var jjmp = new int[ast.Exprs.Length + 1];
					jjmp[0] = Jmp;
					prog.Add(jjmp);
					var jmpfixes = new List<int>(ast.Exprs.Length - 1);
					for (var i = 0; i < ast.Exprs.Length; ++i)
					{
						var e = ast.Exprs[i];
						if (null != e)
						{
							jjmp[i + 1] = prog.Count;
							EmitPart(e, prog);
							if (i == ast.Exprs.Length - 1)
								continue;
							if (i == ast.Exprs.Length - 2 && null == ast.Exprs[i + 1])
								continue;
							var j = new int[2];
							j[0] = Jmp;
							jmpfixes.Add(prog.Count);
							prog.Add(j);
						}
					}
					for (int ic = jmpfixes.Count, i = 0; i < ic; ++i)
					{
						var j = prog[jmpfixes[i]];
						j[1] = prog.Count;
					}
					if (-1 < firstNull)
					{
						jjmp[firstNull + 1] = prog.Count;
					}
					break;
					
				case Ast.NSet:
				case Ast.Set:
					// generate a set or nset instruction
					// with all the packed ranges
					// which we first sort to ensure they're 
					// all arranged from low to high
					// (n)set packedRange1Left, packedRange1Right, packedRange2Left, packedRange2Right...
					inst = new int[ast.Ranges.Length + 1];
					inst[0] = (ast.Kind==Ast.Set)?Set:NSet;
					SortRanges(ast.Ranges);
					Array.Copy(ast.Ranges, 0, inst, 1, ast.Ranges.Length);
					prog.Add(inst);
					break;
				case Ast.NUCode:
				case Ast.UCode:
					// generate a ucode or ncode instruction
					// with the given unicode category value
					// (n)ucode <ast.Value>
					inst = new int[2];
					inst[0] = (ast.Kind == Ast.UCode) ? UCode : NUCode;
					inst[1] = ast.Value;
					prog.Add(inst);
					break;
				case Ast.Opt:
					inst = new int[3];
					// we have to choose betweed Left or empty
					// jmp <pc>, <<next>>
					inst[0] = Jmp;
					prog.Add(inst);
					inst[1] = prog.Count;
					// emit 
					for (var i = 0; i < ast.Exprs.Length; i++)
						if (null != ast.Exprs[i])
							EmitPart(ast.Exprs[i], prog);
					inst[2] = prog.Count;
					if (ast.IsLazy)
					{
						// non-greedy, swap jmp
						var t = inst[1];
						inst[1] = inst[2];
						inst[2] = t;
					}
					break;
				// the next two forward to Rep
				case Ast.Star:
					ast.Min = 0;
					ast.Max = 0;
					goto case Ast.Rep;
				case Ast.Plus:
					ast.Min = 1;
					ast.Max = 0;
					goto case Ast.Rep;
				case Ast.Rep:
					// TODO: There's an optimization opportunity
					// here wherein we can make the rep instruction
					// take min and max values, or make a condition
					// branch instruction take a loop count. We don't
					//
					// we need to generate a series of matches
					// based on the min and max values
					// this gets complicated
					if (ast.Min > 0 && ast.Max > 0 && ast.Min > ast.Max)
						throw new ArgumentOutOfRangeException("Max");
					
					int idx;
					Ast opt;
					Ast rep;
					
					switch (ast.Min)
					{
						case -1:
						case 0:
							switch (ast.Max)
							{
								// kleene * ex: (foo)*
								case -1:
								case 0:
									idx = prog.Count;
									inst = new int[3];
									inst[0] = Jmp;
									prog.Add(inst);
									inst[1] = prog.Count;
									for (var i = 0; i < ast.Exprs.Length; i++)
										if (null != ast.Exprs[i])
											EmitPart(ast.Exprs[i], prog);
									jmp = new int[2];
									jmp[0] = Jmp;
									jmp[1] = idx;
									prog.Add(jmp);
									inst[2] = prog.Count;
									if (ast.IsLazy)
									{   // non-greedy - swap jmp
										var t = inst[1];
										inst[1] = inst[2];
										inst[2] = t;
									}
									return;
									// opt ex: (foo)?
								case 1:
									opt = new Ast();
									opt.Kind = Ast.Opt;
									opt.Exprs = ast.Exprs;
									opt.IsLazy = ast.IsLazy;
									EmitPart(opt,prog);
									return;
								default: // ex: (foo){,10}
									opt = new Ast();
									opt.Kind = Ast.Opt;
									opt.Exprs = ast.Exprs;
									opt.IsLazy = ast.IsLazy;
									EmitPart(opt, prog);
									for (var i = 1; i < ast.Max; ++i)
									{
										EmitPart(opt,prog);
									}
									return;
							}
						case 1:
							switch (ast.Max)
							{
								// plus ex: (foo)+
								case -1:
								case 0:
									idx = prog.Count;
									for (var i = 0; i < ast.Exprs.Length; i++)
										if (null != ast.Exprs[i])
											EmitPart(ast.Exprs[i], prog);
									inst = new int[3];
									inst[0] = Jmp;
									prog.Add(inst);
									inst[1] = idx;
									inst[2] = prog.Count;
									if (ast.IsLazy)
									{
										// non-greedy, swap jmp
										var t = inst[1];
										inst[1] = inst[2];
										inst[2] = t;
									}
									return;
								case 1:
									// no repeat ex: (foo)
									for (var i = 0; i < ast.Exprs.Length; i++)
										if (null != ast.Exprs[i])
											EmitPart(ast.Exprs[i], prog);
									return;
								default:
									// repeat ex: (foo){1,10}
									rep = new Ast();
									rep.Min = 0;
									rep.Max = ast.Max -1;
									rep.IsLazy = ast.IsLazy;
									rep.Exprs = ast.Exprs;
									for (var i = 0; i < ast.Exprs.Length; i++)
										if (null != ast.Exprs[i])
											EmitPart(ast.Exprs[i], prog);
									EmitPart(rep, prog);
									return;
							}
						default: // bounded minum
							switch (ast.Max)
							{
								// repeat ex: (foo) {10,}
								case -1:
								case 0:
									for (var j = 0; j < ast.Min; ++j)
									{
										for (var i = 0; i < ast.Exprs.Length; i++)
											if (null != ast.Exprs[i])
												EmitPart(ast.Exprs[i], prog);
									}
									rep = new Ast();
									rep.Kind = Ast.Star;
									rep.Exprs = ast.Exprs;
									rep.IsLazy = ast.IsLazy;
									EmitPart(rep,prog);
									return;
								case 1: // invalid or handled prior
									// should never get here
									throw new NotImplementedException();
								default: // repeat ex: (foo){10,12}
									for (var j = 0; j < ast.Min; ++j)
									{
										for (var i = 0; i < ast.Exprs.Length; i++)
											if (null != ast.Exprs[i])
												EmitPart(ast.Exprs[i], prog);
									}
									if (ast.Min== ast.Max)
										return;
									opt = new Ast();
									opt.Kind = Ast.Opt;
									opt.Exprs = ast.Exprs;
									opt.IsLazy = ast.IsLazy;
									rep = new Ast();
									rep.Kind = Ast.Rep;
									rep.Min = rep.Max = ast.Max - ast.Min;
									EmitPart(rep, prog);
									return;

							}
					}
					// should never get here
					throw new NotImplementedException();

			}
		}

		internal static void EmitPart(FA gnfa,IList<int[]> prog)
		{
			// TODO: Make sure this is an actual GNFA and not just an NFA
			// NFA that is not a GNFA will not work
			gnfa = gnfa.ToGnfa();
			gnfa.TrimNeutrals();
			var rendered = new Dictionary<FA, int>();
			var swFixups = new Dictionary<FA, int>();
			var jmpFixups = new Dictionary<FA, int>();
			var l = new List<FA>();
			gnfa.FillClosure(l);
			// move the accepting state to the end
			var fas = gnfa.FirstAcceptingState;
			var afai = l.IndexOf(fas);
			l.RemoveAt(afai);
			l.Add(fas);
			for(int ic=l.Count,i=0;i<ic;++i)
			{
				var fa = l[i];
				rendered.Add(fa, prog.Count);
				if (!fa.IsFinal)
				{
					int swfixup = prog.Count;
					prog.Add(null);
					swFixups.Add(fa, swfixup);
				} 
				/*if(ic-1!=i)
				{
					if (0==fa.EpsilonTransitions.Count)
					{
						jmpFixups.Add(fa, prog.Count);
						prog.Add(null);
					}
				}*/
			}
			for(int ic=l.Count,i=0;i<ic;++i)
			{
				var fa = l[i];
				if (!fa.IsFinal)
				{
					var sw = new List<int>();
					sw.Add(Switch);
					int[] simple = null;
					if(1==fa.InputTransitions.Count && 0==fa.EpsilonTransitions.Count)
					{
						foreach(var trns in fa.InputTransitions)
						{
							if (l.IndexOf(trns.Key)==i+1)
							{
								simple = trns.Value;
							}
						}
					}
					if (null!=simple)
					{
						if (2 < simple.Length || simple[0] != simple[1])
						{
							sw[0] = Set;
							sw.AddRange(simple);
						}
						else
						{
							sw[0] = Char;
							sw.Add(simple[0]);
						}
						

					}
					else
					{
						foreach (var trns in fa.InputTransitions)
						{
							var dst = rendered[trns.Key];
							sw.AddRange(trns.Value);
							sw.Add(-1);
							sw.Add(dst);
						}
						if (0 < fa.InputTransitions.Count && 0 < fa.EpsilonTransitions.Count)
							sw.Add(-2);
						else if (0 == fa.InputTransitions.Count)
							sw[0] = Jmp;
						foreach (var efa in fa.EpsilonTransitions)
						{
							var dst = rendered[efa];
							sw.Add(dst);
						}
						
					}
					prog[swFixups[fa]] = sw.ToArray();
				}
				
				var jfi = -1;
				if (jmpFixups.TryGetValue(fa, out jfi))
				{
					var jmp = new int[2];
					jmp[0] = Jmp;
					jmp[1] = prog.Count;
					prog[jfi] = jmp;
				}
				
				
			}
			
			
		}
		static void _EmitPart(FA fa,IDictionary<FA,int> rendered, IList<int[]> prog)
		{
			if (fa.IsFinal)
				return;
			int swfixup = prog.Count;
			var sw = new List<int>();
			sw.Add(Switch);
			prog.Add(null);
			foreach (var trns in fa.InputTransitions)
			{
				var dst = -1;
				if(!rendered.TryGetValue(trns.Key,out dst))
				{
					dst = prog.Count;
					rendered.Add(trns.Key, dst);
					_EmitPart(trns.Key, rendered, prog);

				}
				sw.AddRange(trns.Value);
				sw.Add(-1);
				sw.Add(dst);
			}
			if(0<fa.InputTransitions.Count && 0<fa.EpsilonTransitions.Count)
				sw.Add(-2);
			else if (0==fa.InputTransitions.Count)
				sw[0] = Jmp;
			foreach(var efa in fa.EpsilonTransitions)
			{
				var dst = -1;
				if (!rendered.TryGetValue(efa, out dst))
				{
					dst = prog.Count;
					rendered.Add(efa, dst);
					_EmitPart(efa, rendered, prog);
				}
				sw.Add(dst);
			}
			prog[swfixup] = sw.ToArray();
		}
		static string _FmtLbl(int i)
		{
			return string.Format("L{0,4:000#}", i);
		}
		public static string ToString(IEnumerable<int[]> prog)
		{
			var sb = new StringBuilder();
			var i = 0;
			foreach(var inst in prog)
			{
				sb.Append(_FmtLbl(i));
				sb.Append(": ");
				sb.AppendLine(ToString(inst));
				++i;
			}
			return sb.ToString();
		}
		static string _ToStr(int ch)
		{
			return string.Concat('\"', _EscChar(ch), '\"');
		}
		static string _EscChar(int ch)
		{
			switch (ch)
			{
				case '.':
				case '/': // js expects this
				case '(':
				case ')':
				case '[':
				case ']':
				case '<': // flex expects this
				case '>':
				case '|':
				case ';': // flex expects this
				case '\'': // pck expects this
				case '\"':
				case '{':
				case '}':
				case '?':
				case '*':
				case '+':
				case '$':
				case '^':
				case '\\':
					return "\\"+char.ConvertFromUtf32(ch);
				case '\t':
					return "\\t";
				case '\n':
					return "\\n";
				case '\r':
					return "\\r";
				case '\0':
					return "\\0";
				case '\f':
					return "\\f";
				case '\v':
					return "\\v";
				case '\b':
					return "\\b";
				default:
					var s = char.ConvertFromUtf32(ch);
					if (!char.IsLetterOrDigit(s,0) && !char.IsSeparator(s,0) && !char.IsPunctuation(s,0) && !char.IsSymbol(s,0))
					{
						if (1 == s.Length)
							return string.Concat(@"\u", unchecked((ushort)ch).ToString("x4"));
						else
							return string.Concat(@"\U" + ch.ToString("x8"));
						

					}
					else
						return s;
			}
		}
		static int _AppendRanges(StringBuilder sb, int[] inst,int index)
		{
			var i = index;
			for (i = index; i < inst.Length - 1; i++)
			{
				if (-1 == inst[i])
					return i;
				if (index != i)
					sb.Append(", ");
				if (inst[i] == inst[i + 1])
					sb.Append(_ToStr(inst[i]));
				else
				{
					sb.Append(_ToStr(inst[i]));
					sb.Append("..");
					sb.Append(_ToStr(inst[i + 1]));
				}

				++i;
			}
			return i;
		}
		public static string ToString(int[] inst)
		{
			switch (inst[0])
			{
				case Jmp:
					var sb = new StringBuilder();
					sb.Append("jmp ");
					sb.Append(_FmtLbl(inst[1]));
					for (var i = 2; i < inst.Length; i++)
						sb.Append(", " + _FmtLbl(inst[i]));
					return sb.ToString();
				case Switch:
					sb = new StringBuilder();
					sb.Append("switch ");
					var j = 1;
					for(;j<inst.Length;)
					{
						if (-2 == inst[j])
							break;
						if (j != 1)
							sb.Append(", ");
						sb.Append("case ");
						j = _AppendRanges(sb, inst, j);
						++j;
						sb.Append(":");
						sb.Append(_FmtLbl(inst[j]));
						++j;
					}
					if(j<inst.Length && -2==inst[j])
					{
						sb.Append(", default:");
						var delim = "";
						for(++j;j<inst.Length;j++)
						{
							sb.Append(delim);
							sb.Append(_FmtLbl(inst[j]));
							delim = ", ";
						}
					}
					return sb.ToString();
				case Char:
					if (2==inst.Length)// for testing
						return "char " + _ToStr(inst[1]);
					else return "char";
				case UCode:
				case NUCode:
					return (UCode == inst[0] ? "ucode " : "nucode ") + inst[1];
				case Set:
				case NSet:
					sb = new StringBuilder();
					if (Set == inst[0])
						sb.Append("set ");
					else
						sb.Append("nset ");
					for(var i = 1;i<inst.Length-1;i++)
					{
						if (1 != i)
							sb.Append(", ");
						if (inst[i] == inst[i + 1])
							sb.Append(_ToStr(inst[i]));
						else
						{
							sb.Append(_ToStr(inst[i]));
							sb.Append("..");
							sb.Append(_ToStr(inst[i+1]));
						}
							
						++i;
					}
					return sb.ToString();
				case Any:
					return "any";
				case Match:
					return "match " + inst[1].ToString();
				case Save:
					return "save " + inst[1].ToString();
				default:
					throw new InvalidProgramException("The instruction is not valid");
			}
		}
		internal static int[][] EmitLexer(bool optimize,params Ast[] expressions)
		{
			var parts = new KeyValuePair<int, int[][]>[expressions.Length];
			for (var i = 0;i<expressions.Length;++i)
			{
				var l = new List<int[]>();
				FA fa = null;
				if (optimize)
				{
					try
					{
						fa = FA.FromAst(expressions[i]);
					}
					// we can't do lazy expressions
					catch (NotSupportedException) { }
				}
				//fa = null;// for testing
				if (null != fa)
				{
					EmitPart(fa, l);
				} else
				{
					EmitPart(expressions[i], l);

				}
				parts[i] = new KeyValuePair<int,int[][]>(i,l.ToArray());
			}
			var result =  EmitLexer(parts);
			if(optimize)
			{
				result = _RemoveDeadCode(result);
			}
			return result;
		}
		static int[][] _RemoveDeadCode(int[][] prog)
		{
			var done = false;
			while(!done)
			{
				done = true;
				var toRemove = -1;
				for(var i = 0;i<prog.Length;++i)
				{
					var pc = prog[i];
					// remove L0001: jmp L0002
					if(Jmp==pc[0] && i+1==pc[1] && 2==pc.Length)
					{
						toRemove = i;
						break;
					}
				}
				if(-1!=toRemove)
				{
					done = false;
					var newProg = new List<int[]>(prog.Length-1);
					for(var i = 0;i<toRemove;++i)
					{
						var inst = prog[i];
						switch(inst[0])
						{
							case Switch:
								var inDef = false;
								for (var j = 0; j < inst.Length; j++)
								{
									if (inDef)
									{
										if(inst[j]>toRemove)
											--inst[j];
									}
									else
									{
										if (-1 == inst[j])
										{
											++j;
											if (inst[j] > toRemove)
												--inst[j];
										}
										else if (-2 == inst[j])
											inDef = true;
									}
								}
								break;
							case Jmp:
								for (var j = 1; j < inst.Length; j++)
									if (inst[j] > toRemove)
										--inst[j];
								break;
						}
						newProg.Add(prog[i]);
					}
					var progNext = new List<int[]>(prog.Length - toRemove - 1);
					for(var i = toRemove+1;i<prog.Length;i++)
					{
						progNext.Add(prog[i]);
					}
					var pna = progNext.ToArray();
					Fixup(pna, -1);
					newProg.AddRange(pna);
					prog = newProg.ToArray();
				}
			}
			return prog;
		}
		internal static int[][] EmitLexer(IEnumerable<KeyValuePair<int,int[][]>> parts)
		{
			var l = new List<KeyValuePair<int, int[][]>>(parts);
			var prog = new List<int[]>();
			int[] match, save;
			// save 0
			save = new int[2];
			save[0] = Save;
			save[1] = 0;
			prog.Add(save);

			// generate the primary jmp instruction
			var jmp = new int[l.Count+ 2];
			jmp[0] = Compiler.Jmp;
			prog.Add(jmp);
			// for each expressions, render a save 0
			// followed by the the instructions
			// followed by save 1, and then match <i>
			for (int ic=l.Count,i = 0; i < ic; ++i)
			{
				jmp[i + 1] = prog.Count;
				
				// expr
				Fixup(l[i].Value, prog.Count);
				prog.AddRange(l[i].Value);
				// save 1
				save = new int[2];
				save[0] = Save;
				save[1] = 1;
				prog.Add(save);
				// match <l[i].Key>
				match = new int[2];
				match[0] = Match;
				match[1] = l[i].Key;
				prog.Add(match);
			}
			// generate the error condition
			// handling
			jmp[jmp.Length - 1] = prog.Count;
			// any
			var any = new int[1];
			any[0] = Any;
			prog.Add(any);
			// save 1
			save = new int[2];
			save[0] =Save;
			save[1] = 1;
			prog.Add(save);
			// match -1
			match = new int[2];
			match[0] = Match;
			match[1] = -1;
			prog.Add(match);
			
			return prog.ToArray();
		}
		internal static void SortRanges(int[] ranges)
		{
			var result = new List<KeyValuePair<int, int>>(ranges.Length / 2);
			for (var i = 0; i < ranges.Length - 1; ++i)
			{
				var ch = ranges[i];
				++i;
				result.Add(new KeyValuePair<int, int>(ch, ranges[i]));
			}
			result.Sort((x, y) => { return x.Key.CompareTo(y.Key); });
			for (int ic = result.Count, i = 0; i < ic; ++i)
			{
				var j = i * 2;
				var kvp = result[i];
				ranges[j] = kvp.Key;
				ranges[j + 1] = kvp.Value;
			}
		}
		internal static void Fixup(int[][] program, int offset)
		{
			for(var i = 0;i<program.Length;i++)
			{
				var inst = program[i];
				var op = inst[0];
				switch(op)
				{
					case Switch:
						var inDef = false;
						for(var j = 0;j<inst.Length;j++)
						{
							if (inDef)
							{
								inst[j] += offset;
							}
							else
							{
								if (-1 == inst[j])
								{
									++j;
									inst[j] += offset;
								}
								else if (-2 == inst[j])
									inDef = true;
							}
						}
					break;
					case Jmp:
						for (var j = 1; j < inst.Length; j++)
							inst[j] += offset;
						break;
				}
			}
		}
	}
}
