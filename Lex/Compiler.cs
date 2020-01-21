using System;
using System.Collections.Generic;
using System.Text;

namespace L
{
	static class Compiler
	{
		#region Opcodes
		internal const int Match = 1; // match symbol
		internal const int Jmp = 2; // jmp addr
		internal const int Split = 3; // split addr1, addr2
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
					break;
				case Ast.Alt: // alternation
					// be sure to handle the cases where one
					// of the children is null such as
					// in (foo|) or (|foo)
					var exprs = new List<Ast>(ast.Exprs.Length);
					var firstNull = -1;
					for (var i = 0; i < ast.Exprs.Length; ++i)
					{
						if (null == ast.Exprs[i])
						{
							if (0 > firstNull)
							{
								firstNull = i;
								exprs.Add(null);
							}
							continue;
						}
						exprs.Add(ast.Exprs[i]);
					}
					ast.Exprs = exprs.ToArray();
					var split = new int[ast.Exprs.Length + 1];
					split[0] = Split;
					prog.Add(split);
					var jmpfixes = new List<int>(ast.Exprs.Length - 1);
					for(var i = 0;i<ast.Exprs.Length;++i)
					{
						var e = ast.Exprs[i];
						if(null!=e)
						{
							split[i + 1] = prog.Count;
							EmitPart(e, prog);
							if(i==ast.Exprs.Length-1)
								continue;
							if (i == ast.Exprs.Length - 2 && null == ast.Exprs[i + 1])
								continue;
							var j = new int[2];
							j[0] = Jmp;
							jmpfixes.Add(prog.Count);
							prog.Add(j);
						}
					}
					for(int ic=jmpfixes.Count,i=0;i<ic;++i)
					{
						var j = prog[jmpfixes[i]];
						j[1] = prog.Count;
					}
					if(-1<firstNull)
					{
						split[firstNull + 1] = prog.Count;
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
					// split <pc>, <<next>>
					inst[0] = Split;
					prog.Add(inst);
					inst[1] = prog.Count;
					// emit 
					for (var i = 0; i < ast.Exprs.Length; i++)
						if (null != ast.Exprs[i])
							EmitPart(ast.Exprs[i], prog);
					inst[2] = prog.Count;
					if (ast.IsLazy)
					{
						// non-greedy, swap split
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
									inst[0] = Split;
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
									{   // non-greedy - swap split
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
									inst[0] = Split;
									prog.Add(inst);
									inst[1] = idx;
									inst[2] = prog.Count;
									if (ast.IsLazy)
									{
										// non-greedy, swap split
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
		static string _ToStr(char ch)
		{
			return string.Concat('\"', _EscChar(ch), '\"');
		}
		static string _EscChar(char ch)
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
					return string.Concat("\\", ch.ToString());
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
					if (!char.IsLetterOrDigit(ch) && !char.IsSeparator(ch) && !char.IsPunctuation(ch) && !char.IsSymbol(ch))
					{

						return string.Concat("\\x", unchecked((ushort)ch).ToString("x4"));

					}
					else
						return string.Concat(ch);
			}
		}
		public static string ToString(int[] inst)
		{
			switch (inst[0])
			{
				case Split:
					var sb = new StringBuilder();
					sb.Append("split ");
					sb.Append(_FmtLbl(inst[1]));
					for (var i = 2; i < inst.Length; i++)
						sb.Append(", " + _FmtLbl(inst[i]));
					return sb.ToString();
				case Jmp:
					return "jmp " + _FmtLbl(inst[1]);
				case Char:
					if (2==inst.Length)// for testing
						return "char " + _ToStr((char)inst[1]);
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
							sb.Append(_ToStr((char)inst[i]));
						else
						{
							sb.Append(_ToStr((char)inst[i]));
							sb.Append("..");
							sb.Append(_ToStr((char)inst[i+1]));
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
		internal static int[][] EmitLexer(params Ast[] expressions)
		{
			var parts = new KeyValuePair<int, int[][]>[expressions.Length];
			for (var i = 0;i<expressions.Length;++i)
			{
				var l = new List<int[]>();
				EmitPart(expressions[i], l);
				parts[i] = new KeyValuePair<int,int[][]>(i,l.ToArray());
			}
			return EmitLexer(parts);
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

			// generate the primary split instruction
			var split = new int[l.Count+ 2];
			split[0] = Compiler.Split;
			prog.Add(split);
			// for each expressions, render a save 0
			// followed by the the instructions
			// followed by save 1, and then match <i>
			for (int ic=l.Count,i = 0; i < ic; ++i)
			{
				split[i + 1] = prog.Count;
				
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
			split[split.Length - 1] = prog.Count;
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
					case Jmp:
						inst[1] += offset;
						break;
					case Split:
						for (var j = 1; j < inst.Length; j++)
							inst[j] += offset;
						break;
				}
			}
		}
	}
}
