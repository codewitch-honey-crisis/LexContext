using System;
using System.Collections.Generic;
using System.Text;

namespace L
{
	partial class CharCls
	{
		static Lazy<IDictionary<string, int[]>> _CharacterClasses = new Lazy<IDictionary<string, int[]>>(_GetCharacterClasses);
		static IDictionary<string,int[]> _GetCharacterClasses()
		{
			var result = new Dictionary<string, int[]>();
			result.Add("IsLetter", IsLetter);
			result.Add("IsDigit", IsDigit);
			result.Add("IsLetterOrDigit", IsLetterOrDigit);
			result.Add("IsWhiteSpace", IsWhiteSpace);
			return result;
		}
		public static IDictionary<string,int[]> CharacterClasses {  get { return _CharacterClasses.Value; } }
	}
}
