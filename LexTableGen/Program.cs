using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Globalization;
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
				var uc = new List<int>[30];
				var isLetter = new List<int>();
				var isLetterOrDigit = new List<int>();
				var isDigit = new List<int>();
				var isWhiteSpace = new List<int>();
				var graph = new List<int>();
				for (var i = 0; i < uc.Length; i++)
					uc[i] = new List<int>();
				for (var i = 0; i < 0x110000; i++)
				{
					if (i >= 0x00d800 && i <= 0x00dfff)
						continue;
					var ch = char.ConvertFromUtf32(i);
					uc[(int)char.GetUnicodeCategory(ch,0)].Add(i);
					if (char.IsLetter(ch,0))
						isLetter.Add(i);
					if (char.IsDigit(ch,0))
						isDigit.Add(i);
					if (char.IsLetterOrDigit(ch,0))
						isLetterOrDigit.Add(i);
					if (char.IsWhiteSpace(ch, 0))
						isWhiteSpace.Add(i);
					if (!char.IsWhiteSpace(ch, 0) && !char.IsControl(ch, 0))
						graph.Add(i);
				}
				var uca = new int[30][];
				for(var i = 0;i<uca.Length;i++)
					uca[i] = _GetRanges(uc[i]);
				var alnum = new List<int>();
				alnum.AddRange(uc[(int)UnicodeCategory.LetterNumber]);
				alnum.AddRange(isLetter);
				alnum.AddRange(uc[(int)UnicodeCategory.DecimalDigitNumber]);
				alnum.Sort();
				
				var asciiRanges = new int[] { 0, 0x7F };

				
				/*
				 * [:alnum:]	Alphanumeric characters	[a-zA-Z0-9]	[\p{L}\p{Nl}
 \p{Nd}]		\p{Alnum}
[:alpha:]	Alphabetic characters	[a-zA-Z]	\p{L}\p{Nl}		\p{Alpha}
[:ascii:]	ASCII characters	[\x00-\x7F]	\p{InBasicLatin}		\p{ASCII}
[:blank:]	Space and tab	[ \t]	[\p{Zs}\t]	\h	\p{Blank}
[:cntrl:]	Control characters	[\x00-\x1F\x7F]	\p{Cc}		\p{Cntrl}
[:digit:]	Digits	[0-9]	\p{Nd}	\d	\p{Digit}
[:graph:]	Visible characters (anything except spaces and control characters)	[\x21-\x7E]	[^\p{Z}\p{C}]		\p{Graph}
[:lower:]	Lowercase letters	[a-z]	\p{Ll}	\l	\p{Lower}
[:print:]	Visible characters and spaces (anything except control characters)	[\x20-\x7E]	\P{C}		\p{Print}
[:punct:]	Punctuation (and symbols).	[!"\#$%&'()*+,
\-./:;<=>?@\[
\\\]^_‘{|}~]	\p{P}		\p{Punct}
[:space:]	All whitespace characters, including line breaks	[ \t\r\n\v\f]	[\p{Z}\t\r\n\v\f]	\s	\p{Space}
[:upper:]	Uppercase letters	[A-Z]	\p{Lu}	\u	\p{Upper}
[:word:]	Word characters (letters, numbers and underscores)	[A-Za-z0-9_]	[\p{L}\p{Nl}
 \p{Nd}\p{Pc}]	\w	\p{IsWord}
[:xdigit:]	Hexadecimal digits	[A-Fa-f0-9]	[A-Fa-f0-9]		\p{XDigit}
				 */

				td.Members.Add(CU.Field(uca.GetType(), "UnicodeCategories", MemberAttributes.Public | MemberAttributes.Static, CU.Literal(uca)));
				td.Members.Add(CU.Field(typeof(int[]), "IsLetter", MemberAttributes.Public | MemberAttributes.Static, CU.Literal(_GetRanges(isLetter))));
				td.Members.Add(CU.Field(typeof(int[]), "IsDigit", MemberAttributes.Public | MemberAttributes.Static, CU.Literal(_GetRanges(isDigit))));
				td.Members.Add(CU.Field(typeof(int[]), "IsLetterOrDigit", MemberAttributes.Public | MemberAttributes.Static, CU.Literal(_GetRanges(isLetterOrDigit))));
				td.Members.Add(CU.Field(typeof(int[]), "IsWhiteSpace", MemberAttributes.Public | MemberAttributes.Static, CU.Literal(_GetRanges(isWhiteSpace))));
				sw.Write(CU.ToString(ccu));	
			}
		}
		static int[] _GetRanges(IEnumerable<int> chars)
		{
			var result = new List<int>();
			int first = '\0';
			int last = '\0';
			using (IEnumerator<int> e = chars.GetEnumerator())
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
