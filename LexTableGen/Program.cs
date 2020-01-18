using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
namespace LexTableGen
{
	using CU = CD.CodeDomUtility;
	class Program
	{
		static void Main(string[] args)
		{
			
			var fn = (args.Length>0)?args[0]:null;
			using (var sw =null==fn?Console.Out:new StreamWriter(File.OpenWrite(fn)))
			{
				var ccu = new CodeCompileUnit();
				var ns = new CodeNamespace("L");
				ccu.Namespaces.Add(ns);
				var td = CU.Class("CharCls");
				ns.Types.Add(td);
				td.IsPartial = true;
				var uc = new List<char>[30];
				var isLetter = new List<char>();
				var isLetterOrDigit = new List<char>();
				var isDigit = new List<char>();
				var isWhiteSpace = new List<char>();
				for (var i = 0; i < uc.Length; i++)
					uc[i] = new List<char>();
				for (var i = 0; i < 65536; i++)
				{
					var ch = (char)i;
					uc[(int)char.GetUnicodeCategory(ch)].Add(ch);
					if (char.IsLetter(ch))
						isLetter.Add(ch);
					if (char.IsDigit(ch))
						isDigit.Add(ch);
					if (char.IsLetterOrDigit(ch))
						isLetterOrDigit.Add(ch);
					if (char.IsWhiteSpace(ch))
						isWhiteSpace.Add(ch);
						
				}
				var uca = new int[30][];
				for(var i = 0;i<uca.Length;i++)
					uca[i] = _GetRanges(uc[i]);
				td.Members.Add(CU.Field(uca.GetType(), "UnicodeCategories", MemberAttributes.Public | MemberAttributes.Static, CU.Literal(uca)));
				td.Members.Add(CU.Field(typeof(int[]), "IsLetter", MemberAttributes.Public | MemberAttributes.Static, CU.Literal(_GetRanges(isLetter))));
				td.Members.Add(CU.Field(typeof(int[]), "IsDigit", MemberAttributes.Public | MemberAttributes.Static, CU.Literal(_GetRanges(isDigit))));
				td.Members.Add(CU.Field(typeof(int[]), "IsLetterOrDigit", MemberAttributes.Public | MemberAttributes.Static, CU.Literal(_GetRanges(isLetterOrDigit))));
				td.Members.Add(CU.Field(typeof(int[]), "IsWhiteSpace", MemberAttributes.Public | MemberAttributes.Static, CU.Literal(_GetRanges(isWhiteSpace))));
				sw.Write(CU.ToString(ccu));	
			}
		}
		static int[] _GetRanges(IEnumerable<char> chars)
		{
			var result = new List<int>();
			char first = '\0';
			char last = '\0';
			using (IEnumerator<char> e = chars.GetEnumerator())
			{
				bool moved = e.MoveNext();
				while (moved)
				{
					first = last = e.Current;
					while ((moved = e.MoveNext()) && (e.Current == last || e.Current == last + 1))
					{
						last = e.Current;
					}
					result.Add(first);
					result.Add(last);
				}
			}
			return result.ToArray();
		}
	}
}
