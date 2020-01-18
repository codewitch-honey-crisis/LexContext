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
				case Ast.Lit:
					// char <ast.Value>
					inst = new int[2];
					inst[0] = Char;
					inst[1] = ast.Value;
					prog.Add(inst);
					break;
				case Ast.Cat:
					if(null!=ast.Left)
						EmitPart(ast.Left,prog);
					if(null!=ast.Right)
						EmitPart(ast.Right,prog);
					break;
				case Ast.Alt:
					if (null == ast.Right)
					{
						if (null == ast.Left)
						{
							return;
						}
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
						// split <i>, <<next>>
						inst = new int[3];
						inst[0] = Split;
						prog.Add(inst);
						var i = prog.Count;
						EmitPart(ast.Right, prog);
						inst[1] = i;
						inst[2] = prog.Count;
						return;
					}
					else
					{
						// split <pc>, <<next>>
						inst = new int[3];
						inst[0] = Split;
						prog.Add(inst);
						inst[1] = prog.Count;
						EmitPart(ast.Left, prog);
						// jmp <<next>>
						jmp = new int[2];
						jmp[0] = Jmp;
						prog.Add(jmp);
						inst[2]= prog.Count;
						EmitPart(ast.Right,prog);
						jmp[1] = prog.Count;
					}
					break;
				case Ast.NSet:
				case Ast.Set:
					// (n)set packedRange1Left, packedRange1Right, packedRange2Left, packedRange2Right...
					inst = new int[ast.Ranges.Length + 1];
					inst[0] = (ast.Kind==Ast.Set)?Set:NSet;
					Array.Sort(ast.Ranges);
					Array.Copy(ast.Ranges, 0, inst, 1, ast.Ranges.Length);
					prog.Add(inst);
					break;
				case Ast.NUCode:
				case Ast.UCode:
					// (n)ucode <ast.Value>
					inst = new int[2];
					inst[0] = (ast.Kind == Ast.UCode) ? UCode : NUCode;
					inst[1] = ast.Value;
					prog.Add(inst);
					break;
				case Ast.Opt:
					if (null == ast.Left)
						return;
					inst = new int[3];
					// split <pc>, <<next>>
					inst[0] = Split;
					prog.Add(inst);
					inst[1] = prog.Count;
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
				case Ast.Star:
					ast.Min = 0;
					ast.Max = 0;
					goto case Ast.Rep;
				case Ast.Plus:
					ast.Min = 1;
					ast.Max = 0;
					goto case Ast.Rep;
				case Ast.Rep:
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
								case 1:
									opt = new Ast();
									opt.Kind = Ast.Opt;
									opt.Left = ast.Left;
									opt.IsLazy = ast.IsLazy;
									EmitPart(opt,prog);
									return;
								default:
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
									EmitPart(ast.Left, prog);
									return;
								default:
									rep = new Ast();
									rep.Min = 0;
									rep.Max = ast.Max -1;
									rep.IsLazy = ast.IsLazy;
									rep.Left = ast.Left;
									EmitPart(ast.Left, prog);
									EmitPart(rep, prog);
									return;
							}
						default:
							switch (ast.Max)
							{
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
								case 1:
									// should never get here
									throw new NotImplementedException();
								default:
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
			var sp = new int[expressions.Length + 2];
			sp[0] = Compiler.Split;
			prog.Add(sp);
			for (var i = 0; i < expressions.Length; i++)
			{
				sp[i + 1] = prog.Count;
				save = new int[2];
				save[0] = Save;
				save[1] = 0;
				prog.Add(save);
				EmitPart(expressions[i], prog);
				save = new int[2];
				save[0] = Save;
				save[1] = 1;
				prog.Add(save);
				match = new int[2];
				match[0] = Match;
				match[1] = i;
				prog.Add(match);
			}
			
			sp[sp.Length - 1] = prog.Count;
			save = new int[2];
			save[0] = Save;
			save[1] = 0;
			prog.Add(save);
			var any = new int[1];
			any[0] = Any;
			prog.Add(any);
			save = new int[2];
			save[0] =Save;
			save[1] = 1;
			prog.Add(save);
			match = new int[2];
			match[0] = Match;
			match[1] = -1;
			prog.Add(match);
			
			return prog.ToArray();
		}
		
	}
}
