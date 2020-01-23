using System;
using System.Collections.Generic;

namespace L
{
	sealed partial class FA
	{
		public readonly Dictionary<FA, int[]> InputTransitions = new Dictionary<FA, int[]>();
		public readonly HashSet<FA> EpsilonTransitions = new HashSet<FA>();
		public int AcceptSymbol = -1;
		public bool IsAccepting = false;
		public FA(bool isAccepting,int acceptSymbol=-1)
		{
			IsAccepting = isAccepting;
			AcceptSymbol = acceptSymbol;
		}
		public FA() : this(false)
		{

		}
		static FA[] _FromAsts(Ast[] asts,int match=0)
		{
			var result = new FA[asts.Length];
			for (var i = 0; i < result.Length; i++)
				result[i] = FromAst(asts[i], match);
			return result;
		}
		static bool _TryForwardNeutral(FA fa, out FA result)
		{
			if (!fa.IsNeutral)
			{
				result = fa;
				return false;
			}
			result = fa;
			foreach (var efa in fa.EpsilonTransitions)
			{
				result = efa;
				break;
			}
			return fa != result; // false if circular
		}
		static FA _ForwardNeutrals(FA fa)
		{
			if (null == fa)
				throw new ArgumentNullException(nameof(fa));
			var result = fa;

			while (_TryForwardNeutral(result, out result)) ;


			return result;
		}
		public static FA FromAst(Ast ast,int match=0)
		{
			if (null == ast)
				return null;
			if (ast.IsLazy)
				throw new NotSupportedException("The AST node cannot be lazy");
			switch(ast.Kind)
			{
				case Ast.Alt:
					return Or(_FromAsts(ast.Exprs, match), match);
				case Ast.Cat:
					if (1 == ast.Exprs.Length)
						return FromAst(ast.Exprs[0], match);
					return Concat(_FromAsts(ast.Exprs, match), match);
				case Ast.Dot:
					return Set(new int[] { 0, 0xd7ff, 0xe000, 0x10ffff },match);
				case Ast.Lit:
					return Literal(new int[] { ast.Value }, match);
				case Ast.NSet:
					var pairs = RangeUtility.ToPairs(ast.Ranges);
					RangeUtility.NormalizeRangeList(pairs);
					var pairl = new List<KeyValuePair<int, int>>(RangeUtility.NotRanges(pairs));
					return Set(RangeUtility.FromPairs(pairl), match);
				case Ast.NUCode:
					pairs = RangeUtility.ToPairs(CharCls.UnicodeCategories[ast.Value]);
					RangeUtility.NormalizeRangeList(pairs);
					pairl = new List<KeyValuePair<int, int>>(RangeUtility.NotRanges(pairs));
					return Set(RangeUtility.FromPairs(pairl), match);
				case Ast.Opt:
					return Optional(FromAst(ast.Exprs[0]), match);
				case Ast.Plus:
					return Repeat(FromAst(ast.Exprs[0]), 1, 0, match);
				case Ast.Rep:
					return Repeat(FromAst(ast.Exprs[0]), ast.Min, ast.Max, match);
				case Ast.Set:
					return Set(ast.Ranges, match);
				case Ast.Star:
					return Repeat(FromAst(ast.Exprs[0]), 0, 0, match);
				case Ast.UCode:
					return Set(CharCls.UnicodeCategories[ast.Value], match);
				default:
					throw new NotImplementedException();
			}
		}
		// assumes state is already finalized
		public FA ToGnfa()
		{
			var fa = Clone();
			// using the state removal method 
			// first convert to a GNFA
			var last = fa.FirstAcceptingState;
			if (!last.IsFinal)
			{
				// sometimes our last state isn't final,
				// so we have to extend the machine to have
				// a final last state
				last.IsAccepting = false;
				last.EpsilonTransitions.Add(new FA(true,last.AcceptSymbol));
			}
			if (!fa.IsNeutral) // should never be true but just in case
			{
				// add a neutral transition to the beginning
				var nfa = new FA();
				nfa.EpsilonTransitions.Add(fa);
				fa = nfa;
			}

			return fa;
		}
		public ICollection<FA> FillAcceptingStates()
		{
			var closure = FillClosure();
			var result = new HashSet<FA>();
			foreach (var fa in closure)
				if (fa.IsAccepting)
					result.Add(fa);
			return result;
		}
		public bool IsFinal {
			get {
				return 0 == InputTransitions.Count && 0 == EpsilonTransitions.Count;
			}
		}
		public bool IsNeutral {
			get {
				return !IsAccepting && 0 == InputTransitions.Count && 1 == EpsilonTransitions.Count;
			}
		}
		public void TrimNeutrals()
		{
			var cl = new List<FA>();
			FillClosure(cl);
			foreach (var s in cl)
			{
				var repls = new List<KeyValuePair<FA, FA>>();
				var td = new List<KeyValuePair<FA, int[]>>(s.InputTransitions);
				s.InputTransitions.Clear();
				foreach (var trns in td)
				{
					var fa = trns.Key;
					var fa2 = _ForwardNeutrals(fa);
					if (null == fa2)
						throw new InvalidProgramException("null in forward neutrals support code");
					s.InputTransitions.Add(fa2, trns.Value);
				}
				var el = new List<FA>(s.EpsilonTransitions);
				var ec = el.Count;
				s.EpsilonTransitions.Clear();
				for (int j = 0; j < ec; ++j)
					s.EpsilonTransitions.Add(_ForwardNeutrals(el[j]));
			}
		}
		public FA FirstAcceptingState {
			get {
				foreach(var fa in FillClosure())
				{
					if (fa.IsAccepting)
						return fa;
				}
				return null;
			}
		}
		public void AddInpTrans(int[] ranges,FA dst)
		{
			foreach(var trns in InputTransitions)
			{
				if(dst!=trns.Key)
				{
					if (RangeUtility.Intersects(trns.Value, ranges))
						throw new ArgumentException("There already is a transition to a different state on at least part of the specified input ranges");
				}
			}
			int[] currentRanges = null;
			if (InputTransitions.TryGetValue(dst, out currentRanges))
			{
				InputTransitions[dst] = RangeUtility.Merge(currentRanges, ranges);
			}
			else
				InputTransitions.Add(dst, ranges);
		}
		public ICollection<FA> FillClosure(ICollection<FA> result = null)
		{
			if (null == result) result = new HashSet<FA>();
			if (result.Contains(this))
				return result;
			result.Add(this);
			foreach (var trns in InputTransitions)
				trns.Key.FillClosure(result);
			foreach (var fa in EpsilonTransitions)
				fa.FillClosure(result);
			return result;
		}
		public ICollection<FA> FillEpsilonClosure(ICollection<FA> result = null)
		{
			if (null == result) result = new HashSet<FA>();
			if (result.Contains(this))
				return result;
			result.Add(this);
			foreach (var fa in EpsilonTransitions)
				fa.FillEpsilonClosure(result);
			return result;
		}
		public FA Clone()
		{
			var closure = new List<FA>();
			FillClosure(closure);
			var nclosure = new FA[closure.Count];
			for (var i = 0; i < nclosure.Length; i++)
			{
				var fa = closure[i];
				nclosure[i] = new FA(fa.IsAccepting, fa.AcceptSymbol);
			}
			for(var i = 0;i<nclosure.Length;i++)
			{
				var fa = closure[i];
				var nfa = nclosure[i];
				foreach(var trns in fa.InputTransitions)
				{
					var vals = new int[trns.Value.Length];
					Array.Copy(trns.Value, 0, vals, 0, vals.Length);
					nfa.InputTransitions.Add(nclosure[closure.IndexOf(trns.Key)], vals);
				}
				foreach(var efa in fa.EpsilonTransitions)
				{
					nfa.EpsilonTransitions.Add(nclosure[closure.IndexOf(efa)]);
				}
			}
			return nclosure[0];
		}
		public static FA Literal(IEnumerable<int> @string, int accept = -1)
		{
			var result = new FA();
			var current = result;
			foreach (var ch in @string)
			{
				current.IsAccepting = false;
				var fa = new FA(true, accept);
				current.AddInpTrans(new int[] { ch, ch }, fa);
				current = fa;
			}
			return result;
		}

		public static FA Concat(IEnumerable<FA> exprs, int accept = -1)
		{
			FA result = null,left = null, right = null;
			foreach (var val in exprs)
			{
				if (null == val) continue;
				//Debug.Assert(null != val.FirstAcceptingState);
				var nval = val.Clone();
				//Debug.Assert(null != nval.FirstAcceptingState);
				if (null == left)
				{
					if (null == result)
						result = nval;
					left = nval;
					//Debug.Assert(null != left.FirstAcceptingState);
					continue;
				}
				if (null == right)
				{
					right = nval;
				}

				//Debug.Assert(null != left.FirstAcceptingState);
				nval = right.Clone();
				_Concat(left, nval);
				right = null;
				left = nval;

				//Debug.Assert(null != left.FirstAcceptingState);

			}
			if (null != right)
			{
				right.FirstAcceptingState.AcceptSymbol = accept;
			}
			else
			{
				result.FirstAcceptingState.AcceptSymbol = accept;
			}
			return result;
		}
		static void _Concat(FA lhs, FA rhs)
		{
			//Debug.Assert(lhs != rhs);
			var f = lhs.FirstAcceptingState;
			//Debug.Assert(null != rhs.FirstAcceptingState);
			f.IsAccepting = false;
			f.EpsilonTransitions.Add(rhs);
			//Debug.Assert(null!= lhs.FirstAcceptingState);

		}
		public static FA Set(int[] ranges, int accept = -1)
		{
			var result = new FA();
			var final = new FA(true, accept);
			result.AddInpTrans(ranges, final);
			return result;
		}
		public static FA Or(IEnumerable<FA> exprs, int accept = -1)
		{
			var result = new FA();
			var final = new FA(true, accept);
			foreach (var fa in exprs)
			{
				if (null != fa)
				{
					var nfa = fa.Clone();
					result.EpsilonTransitions.Add(nfa);
					var nffa = nfa.FirstAcceptingState;
					nffa.IsAccepting = false;
					nffa.EpsilonTransitions.Add(final);
				}
				else if (!result.EpsilonTransitions.Contains(final))
					result.EpsilonTransitions.Add(final);
			}
			return result;
		}
		public static FA Repeat(FA expr, int minOccurs = -1, int maxOccurs = -1, int accept = -1)
		{
			expr = expr.Clone();
			if (minOccurs > 0 && maxOccurs > 0 && minOccurs > maxOccurs)
				throw new ArgumentOutOfRangeException(nameof(maxOccurs));
			FA result;
			switch (minOccurs)
			{
				case -1:
				case 0:
					switch (maxOccurs)
					{
						case -1:
						case 0:
							return Repeat(Optional(expr, accept),1,0,accept);
							/*result = new FA();
							var final = new FA(true, accept);
							final.EpsilonTransitions.Add(result);
							foreach (var afa in expr.FillAcceptingStates())
							{
								afa.IsAccepting = false;
								afa.EpsilonTransitions.Add(final);
							}
							result.EpsilonTransitions.Add(expr);
							result.EpsilonTransitions.Add(final);
							//Debug.Assert(null != result.FirstAcceptingState);
							return result;*/
						case 1:
							result = Optional(expr, accept);
							//Debug.Assert(null != result.FirstAcceptingState);
							return result;
						default:
							var l = new List<FA>();
							expr = Optional(expr);
							l.Add(expr);
							for (int i = 1; i < maxOccurs; ++i)
							{
								l.Add(expr.Clone());
							}
							result = Concat(l, accept);
							//Debug.Assert(null != result.FirstAcceptingState);
							return result;
					}
				case 1:
					switch (maxOccurs)
					{
						case -1:
						case 0:
							result = new FA();
							var final = new FA(true, accept);
							final.EpsilonTransitions.Add(result);
							foreach (var afa in expr.FillAcceptingStates())
							{
								afa.IsAccepting = false;
								afa.EpsilonTransitions.Add(final);
							}
							result.EpsilonTransitions.Add(expr);
							//Debug.Assert(null != result.FirstAcceptingState);
							return result;
						case 1:
							//Debug.Assert(null != expr.FirstAcceptingState);
							return expr;
						default:
							result = Concat(new FA[] { expr, Repeat(expr.Clone(), 0, maxOccurs - 1) }, accept);
							//Debug.Assert(null != result.FirstAcceptingState);
							return result;
					}
				default:
					switch (maxOccurs)
					{
						case -1:
						case 0:
							result = Concat(new FA[] { Repeat(expr, minOccurs, minOccurs, accept), Repeat(expr, 0, 0, accept) }, accept);
							//Debug.Assert(null != result.FirstAcceptingState);
							return result;
						case 1:
							throw new ArgumentOutOfRangeException(nameof(maxOccurs));
						default:
							if (minOccurs == maxOccurs)
							{
								var l = new List<FA>();
								l.Add(expr);
								//Debug.Assert(null != expr.FirstAcceptingState);
								for (int i = 1; i < minOccurs; ++i)
								{
									var e = expr.Clone();
									//Debug.Assert(null != e.FirstAcceptingState);
									l.Add(e);
								}
								result = Concat(l, accept);
								//Debug.Assert(null != result.FirstAcceptingState);
								return result;
							}
							result = Concat(new FA[] { Repeat(expr.Clone(), minOccurs, minOccurs, accept), Repeat(Optional(expr.Clone()), maxOccurs - minOccurs, maxOccurs - minOccurs, accept) }, accept);
							//Debug.Assert(null != result.FirstAcceptingState);
							return result;


					}
			}
			// should never get here
			throw new NotImplementedException();
		}
		public static FA Optional(FA expr, int accept = -1)
		{
			var result = expr.Clone();
			var f = result.FirstAcceptingState;
			f.AcceptSymbol = accept;
			result.EpsilonTransitions.Add(f);
			return result;
		}
		
		#region _SetComparer
		private sealed class _SetComparer :
			IEqualityComparer<ICollection<FA>>,
			IEqualityComparer<int[]>
		{
		
			// unordered comparison
			public bool Equals(ICollection<FA> lhs, ICollection<FA> rhs)
			{
				if (ReferenceEquals(lhs, rhs))
					return true;
				else if (ReferenceEquals(null, lhs) || ReferenceEquals(null, rhs))
					return false;
				if (lhs.Count != rhs.Count)
					return false;
				using (var xe = lhs.GetEnumerator())
				using (var ye = rhs.GetEnumerator())
					while (xe.MoveNext() && ye.MoveNext())
						if (!rhs.Contains(xe.Current) || !lhs.Contains(ye.Current))
							return false;
				return true;
			}
			
			public int GetHashCode(ICollection<FA> lhs)
			{
				var result = 0;
				foreach (var fa in lhs)
					if (null != fa)
						result ^= fa.GetHashCode();
				return result;
			}

			public bool Equals(int[] x, int[] y)
			{
				if (ReferenceEquals(x, y)) return true;
				if (null == x) return false;
				if (null == y) return false;
				if (x.Length != y.Length) return false;
				for (var i = 0; i < x.Length; i++)
					if (x[i] != y[i])
						return false;
				return true;
			}

			public int GetHashCode(int[] obj)
			{
				if (null == obj) return 0;
				var result = 0;
				for (var i = 0; i < obj.Length; i++)
					result ^= obj[i];
				return result;
			}

			public static readonly _SetComparer Default = new _SetComparer();
		}
		#endregion
	}
}
