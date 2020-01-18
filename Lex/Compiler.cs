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
					if(null!=ast.Left)
						EmitPart(ast.Left,prog);
					if(null!=ast.Right)
						EmitPart(ast.Right,prog);
					break;
				case Ast.Alt: // alternation
					// first handle the cases where one
					// of the children is null such as
					// in (foo|) or (|foo)
					if (null == ast.Right)
					{
						if (null == ast.Left)
						{
							// both are null ex: (|) - do nothing
							return;
						}
						// we have to choose empty or Left
						// split <i>, <<next>>
						inst = new int[3];
						inst[0] = Split;
						prog.Add(inst);
						var i = prog.Count;
						EmitPart(ast.Left, prog);
						inst[1] = i;
						inst[2] = prog.Count;
						return;
					}
					if (null == ast.Left)
					{
						// we have to choose empty or Right
						// split <i>, <<next>>
						inst = new int[3];
						inst[0] = Split;
						prog.Add(inst);
						var i = prog.Count;
						// emit the right part
						EmitPart(ast.Right, prog);
						inst[1] = i;
						inst[2] = prog.Count;
						return;
					}
					else // both Left and Right are filled ex: (foo|bar)
					{
						// we have to choose/split between left and right
						// split <pc>, <<next>>
						inst = new int[3];
						inst[0] = Split;
						prog.Add(inst);
						inst[1] = prog.Count;
						// emit the left hand side
						EmitPart(ast.Left, prog);
						// we have to skip past the alternate
						// that comes next, so we jump
						// jmp <<next>>
						jmp = new int[2];
						jmp[0] = Jmp;
						prog.Add(jmp);
						inst[2]= prog.Count;
						// emit the right hand side
						EmitPart(ast.Right,prog);
						jmp[1] = prog.Count;
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
					Array.Sort(ast.Ranges);
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
					if (null == ast.Left)
						return; // empty ex: ()? do nothing
					inst = new int[3];
					// we have to choose betweed Left or empty
					// split <pc>, <<next>>
					inst[0] = Split;
					prog.Add(inst);
					inst[1] = prog.Count;
					// emit Left
					EmitPart(ast.Left, prog);
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
					if (null == ast.Left)
						return;
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
									EmitPart(ast.Left, prog);
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
									opt.Left = ast.Left;
									opt.IsLazy = ast.IsLazy;
									EmitPart(opt,prog);
									return;
								default: // ex: (foo){,10}
									opt = new Ast();
									opt.Kind = Ast.Opt;
									opt.Left = ast.Left;
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
									EmitPart(ast.Left, prog);
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
									EmitPart(ast.Left, prog);
									return;
								default:
									// repeat ex: (foo){1,10}
									rep = new Ast();
									rep.Min = 0;
									rep.Max = ast.Max -1;
									rep.IsLazy = ast.IsLazy;
									rep.Left = ast.Left;
									EmitPart(ast.Left, prog);
									EmitPart(rep, prog);
									return;
							}
						default: // bounded minum
							switch (ast.Max)
							{
								// repeat ex: (foo) {10,}
								case -1:
								case 0:
									for (var i = 0; i < ast.Min; ++i)
										EmitPart(ast.Left,prog);
									rep = new Ast();
									rep.Kind = Ast.Star;
									rep.Left = ast.Left;
									rep.IsLazy = ast.IsLazy;
									EmitPart(rep,prog);
									return;
								case 1: // invalid or handled prior
									// should never get here
									throw new NotImplementedException();
								default: // repeat ex: (foo){10,12}
									for (var i = 0; i < ast.Min; ++i)
										EmitPart(ast.Left, prog);
									if (ast.Min== ast.Max)
										return;
									opt = new Ast();
									opt.Kind = Ast.Opt;
									opt.Left = ast.Left;
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
			var prog = new List<int[]>();
			int[] match, save;
			// generate the primary split instruction
			var split = new int[expressions.Length + 2];
			split[0] = Compiler.Split;
			prog.Add(split);
			// for each expressions, render a save 0
			// followed by the the instructions
			// followed by save 1, and then match <i>
			for (var i = 0; i < expressions.Length; i++)
			{
				split[i + 1] = prog.Count;
				// save 0
				save = new int[2];
				save[0] = Save;
				save[1] = 0;
				prog.Add(save);
				// expr
				EmitPart(expressions[i], prog);
				// save 1
				save = new int[2];
				save[0] = Save;
				save[1] = 1;
				prog.Add(save);
				// match <i>
				match = new int[2];
				match[0] = Match;
				match[1] = i;
				prog.Add(match);
			}
			// generate the error condition
			// handling
			split[split.Length - 1] = prog.Count;
			// save 0
			save = new int[2];
			save[0] = Save;
			save[1] = 0;
			prog.Add(save);
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
		
	}
}
