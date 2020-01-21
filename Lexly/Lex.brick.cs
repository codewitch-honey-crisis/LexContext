using LC;
using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.IO;
namespace L{static class Assembler{internal static List<Inst>Parse(LexContext l){var result=new List<Inst>();while(-1!=l.Current&&'}'!=l.Current){result.Add(Inst.Parse(l));
}return result;}internal static List<int[]>Emit(IList<Inst>instructions){var ic=instructions.Count;var result=new List<int[]>(ic);var lmap=new Dictionary<string,
int>();var pc=0;var regm=new Dictionary<Inst,int[][]>();for(var i=0;i<ic;++i){var inst=instructions[i];if(inst.Opcode==Inst.Label){if(lmap.ContainsKey(inst.Name))
throw new InvalidProgramException("Duplicate label "+inst.Name+" found at line "+inst.Line.ToString());lmap.Add(inst.Name,pc);}else if(inst.Opcode==Inst.Regex)
{var reg=new List<int[]>();Compiler.EmitPart(inst.Expr,reg);regm.Add(inst,reg.ToArray());pc+=reg.Count;}else++pc;}pc=0;for(var i=0;i<ic;++i){int dst;var
 inst=instructions[i];int[]code=null;switch(inst.Opcode){case Inst.Regex:Compiler.Fixup(regm[inst],pc);result.AddRange(regm[inst]);break;case Inst.Label:
break;case Inst.Any:code=new int[1];code[0]=inst.Opcode;break;case Inst.Char:case Inst.UCode:case Inst.NUCode:case Inst.Save:case Inst.Match:code=new int[2];
code[0]=inst.Opcode;code[1]=inst.Value;break;case Inst.Set:case Inst.NSet:var set=new List<int>(inst.Ranges.Length+1);set.Add(inst.Opcode);Compiler.SortRanges(inst.Ranges);
set.AddRange(inst.Ranges);code=set.ToArray();break;case Inst.Jmp:code=new int[2];code[0]=inst.Opcode;if(!lmap.TryGetValue(inst.Name,out dst))throw new
 InvalidProgramException("Jmp references undefined label "+inst.Name+" at line "+inst.Line.ToString());code[1]=dst;break;case Inst.Split:var split=new
 List<int>(inst.Labels.Length+1);split.Add(inst.Opcode);for(var j=0;j<inst.Labels.Length;j++){var lbl=inst.Labels[j];if(!lmap.TryGetValue(lbl,out dst))
throw new InvalidProgramException("Split references undefined label "+inst.Name+" at line "+inst.Line.ToString());split.Add(dst);}code=split.ToArray();
break;}if(null!=code){result.Add(code);}pc=result.Count;}return result;}}class Inst{
#region Opcodes
internal const int Regex=-2; internal const int Label=-1; internal const int Match=1; internal const int Jmp=2; internal const int Split=3; internal const
 int Any=4; internal const int Char=5; internal const int Set=6; internal const int NSet=7; internal const int UCode=8; internal const int NUCode=9; internal
 const int Save=10;
#endregion
public int Opcode; public int[]Ranges;public string[]Labels;public int Value;public string Name;public int Line;public Ast Expr;internal static Inst Parse(LexContext
 input){Inst result=new Inst();_SkipCommentsAndWhiteSpace(input);var l=input.Line;var c=input.Column;var p=input.Position;result.Line=l;var id=_ParseIdentifier(input);
switch(id){case"regex":_SkipWhiteSpace(input);result.Opcode=Regex;input.Expecting('(');input.Advance();result.Expr=Ast.Parse(input);input.Expecting(')');
input.Advance();_SkipToNextInstruction(input);break;case"match":_SkipWhiteSpace(input);var ll=input.CaptureBuffer.Length;var neg=false;if('-'==input.Current)
{neg=true;input.Advance();}if(!input.TryReadDigits())throw new ExpectingException("Illegal operand in match instruction. Expecting integer",input.Line,
input.Column,input.Position,input.FileOrUrl,"integer");var i=int.Parse(input.GetCapture(ll));if(neg)i=-i;result.Opcode=Match;result.Value=i;_SkipToNextInstruction(input);
break;case"jmp":_SkipWhiteSpace(input);var lbl=_ParseIdentifier(input);result.Opcode=Jmp;result.Name=lbl;_SkipToNextInstruction(input);break;case"split":
_SkipWhiteSpace(input);result.Opcode=Split;result.Labels=_ParseLabels(input);_SkipToNextInstruction(input);break;case"any":result.Opcode=Any;_SkipToNextInstruction(input);
break;case"char":_SkipWhiteSpace(input);result.Opcode=Char;result.Value=_ParseChar(input);_SkipToNextInstruction(input);break;case"set":_SkipWhiteSpace(input);
result.Opcode=Set;result.Ranges=_ParseRanges(input);_SkipToNextInstruction(input);break;case"nset":_SkipWhiteSpace(input);result.Opcode=NSet;result.Ranges
=_ParseRanges(input);_SkipToNextInstruction(input);break;case"ucode":_SkipWhiteSpace(input);ll=input.CaptureBuffer.Length;if(!input.TryReadDigits())throw
 new ExpectingException("Illegal operand in ucode instruction. Expecting integer",input.Line,input.Column,input.Position,input.FileOrUrl,"integer");i=
int.Parse(input.GetCapture(ll));result.Opcode=UCode;result.Value=i;_SkipToNextInstruction(input);break;case"nucode":_SkipWhiteSpace(input);ll=input.CaptureBuffer.Length;
if(!input.TryReadDigits())throw new ExpectingException("Illegal operand in nucode instruction. Expecting integer",input.Line,input.Column,input.Position,
input.FileOrUrl,"integer");i=int.Parse(input.GetCapture(ll));result.Opcode=NUCode;result.Value=i;_SkipToNextInstruction(input);break;case"save":_SkipWhiteSpace(input);
ll=input.CaptureBuffer.Length;if(!input.TryReadDigits())throw new ExpectingException("Illegal operand in save instruction. Expecting integer",input.Line,
input.Column,input.Position,input.FileOrUrl,"integer");i=int.Parse(input.GetCapture(ll));result.Opcode=Save;result.Value=i;_SkipToNextInstruction(input);
break;default:if(':'!=input.Current)throw new ExpectingException("Expecting instruction or label",l,c,p,input.FileOrUrl,"match","jmp","split","any","char",
"set","nset","ucode","nucode","save","label");input.Advance();result.Opcode=Label;result.Name=id;break;}_SkipCommentsAndWhiteSpace(input);return result;
}static void _SkipWhiteSpace(LexContext l){l.EnsureStarted();while(-1!=l.Current&&'\n'!=l.Current&&char.IsWhiteSpace((char)l.Current))l.Advance();}static
 void _SkipToNextInstruction(LexContext l){l.EnsureStarted();while(-1!=l.Current){if(';'==l.Current){_SkipCommentsAndWhiteSpace(l);return;}else if('\n'
==l.Current){_SkipCommentsAndWhiteSpace(l);return;}else if(char.IsWhiteSpace((char)l.Current))l.Advance();else throw new ExpectingException("Unexpected token in input",
l.Line,l.Column,l.Position,l.FileOrUrl,"newline","comment");}}static void _SkipCommentsAndWhiteSpace(LexContext l){l.TrySkipWhiteSpace();while(';'==l.Current)
{l.TrySkipUntil('\n',true);l.TrySkipWhiteSpace();}}static string _ParseIdentifier(LexContext l){l.EnsureStarted();var ll=l.CaptureBuffer.Length;if(-1!=l.Current
&&'_'==l.Current||char.IsLetter((char)l.Current)){l.Capture();l.Advance();while(-1!=l.Current&&'_'==l.Current||char.IsLetterOrDigit((char)l.Current)){
l.Capture();l.Advance();}return l.GetCapture(ll);}throw new ExpectingException("Expecting identifier",l.Line,l.Column,l.Position,"identifier");}static
 KeyValuePair<int,int>_ParseRange(LexContext l){l.EnsureStarted();var first=_ParseChar(l);if('.'!=l.Current){_SkipWhiteSpace(l);l.Expecting(',','\n',-1);
_SkipWhiteSpace(l);return new KeyValuePair<int,int>(first,first);}l.Advance();l.Expecting('.');l.Advance();var last=_ParseChar(l);_SkipWhiteSpace(l);l.Expecting(',',
'\n',-1);_SkipWhiteSpace(l);return new KeyValuePair<int,int>(first,last);}static int[]_ParseRanges(LexContext l){var result=new List<int>();while(-1!=l.Current
&&'\n'!=l.Current){_SkipWhiteSpace(l);var kvp=_ParseRange(l);result.Add(kvp.Key);result.Add(kvp.Value);if(','==l.Current)l.Advance();}result.Sort();return
 result.ToArray();}static string[]_ParseLabels(LexContext l){var result=new List<string>();while(-1!=l.Current&&'\n'!=l.Current){var name=_ParseIdentifier(l);
_SkipWhiteSpace(l);result.Add(name);if(','==l.Current){l.Advance();_SkipWhiteSpace(l);}}return result.ToArray();}static int _ParseChar(LexContext l){var
 line=l.Line;var column=l.Column;var position=l.Position;l.EnsureStarted();l.Expecting('\"');l.Advance();var ll=l.CaptureBuffer.Length;if(!l.TryReadUntil('\"',
'\\',false))throw new ExpectingException("Unterminated character literal",line,column,position,l.FileOrUrl,"\"");var s=l.GetCapture(ll);int result;if('\\'==
s[0]){var e=s.GetEnumerator();e.MoveNext();result=char.ConvertToUtf32(_ParseEscapeChar(e,l),0);l.Expecting('\"');l.Advance();return result;}result=char.ConvertToUtf32(s,
0);l.Expecting('\"');l.Advance();return result;}static string _ParseEscapeChar(IEnumerator<char>e,LexContext pc){if(e.MoveNext()){switch(e.Current){case
'r':e.MoveNext();return"\r";case'n':e.MoveNext();return"\n";case't':e.MoveNext();return"\t";case'a':e.MoveNext();return"\a";case'b':e.MoveNext();return
"\b";case'f':e.MoveNext();return"\f";case'v':e.MoveNext();return"\v";case'0':e.MoveNext();return"\0";case'\\':e.MoveNext();return"\\";case'\'':e.MoveNext();
return"\'";case'\"':e.MoveNext();return"\"";case'u':var acc=0L;if(!e.MoveNext())break;if(!_IsHexChar(e.Current))break;acc<<=4;acc|=_FromHexChar(e.Current);
if(!e.MoveNext())break;if(!_IsHexChar(e.Current))break;acc<<=4;acc|=_FromHexChar(e.Current);if(!e.MoveNext())break;if(!_IsHexChar(e.Current))break;acc
<<=4;acc|=_FromHexChar(e.Current);if(!e.MoveNext())break;if(!_IsHexChar(e.Current))break;acc<<=4;acc|=_FromHexChar(e.Current);e.MoveNext();return unchecked((char)acc).ToString();
case'x':acc=0;if(!e.MoveNext())break;if(!_IsHexChar(e.Current))break;acc<<=4;acc|=_FromHexChar(e.Current);if(e.MoveNext()&&_IsHexChar(e.Current)){acc<<=
4;acc|=_FromHexChar(e.Current);if(e.MoveNext()&&_IsHexChar(e.Current)){acc<<=4;acc|=_FromHexChar(e.Current);if(e.MoveNext()&&_IsHexChar(e.Current)){acc
<<=4;acc|=_FromHexChar(e.Current);e.MoveNext();}}}return unchecked((char)acc).ToString();case'U':acc=0;if(!e.MoveNext())break;if(!_IsHexChar(e.Current))
break;acc<<=4;acc|=_FromHexChar(e.Current);if(!e.MoveNext())break;if(!_IsHexChar(e.Current))break;acc<<=4;acc|=_FromHexChar(e.Current);if(!e.MoveNext())
break;if(!_IsHexChar(e.Current))break;acc<<=4;acc|=_FromHexChar(e.Current);if(!e.MoveNext())break;if(!_IsHexChar(e.Current))break;acc<<=4;acc|=_FromHexChar(e.Current);
if(!e.MoveNext())break;if(!_IsHexChar(e.Current))break;acc<<=4;acc|=_FromHexChar(e.Current);if(!e.MoveNext())break;if(!_IsHexChar(e.Current))break;acc
<<=4;acc|=_FromHexChar(e.Current);if(!e.MoveNext())break;if(!_IsHexChar(e.Current))break;acc<<=4;acc|=_FromHexChar(e.Current);if(!e.MoveNext())break;if
(!_IsHexChar(e.Current))break;acc<<=4;acc|=_FromHexChar(e.Current);e.MoveNext();return char.ConvertFromUtf32(unchecked((int)acc));default:throw new NotSupportedException(string.Format("Unsupported escape sequence \\{0}",
e.Current));}}throw new ExpectingException("Unterminated escape sequence",pc.Line,pc.Column,pc.Position,pc.FileOrUrl);}static bool _IsHexChar(char hex)
{return(':'>hex&&'/'<hex)||('G'>hex&&'@'<hex)||('g'>hex&&'`'<hex);}static byte _FromHexChar(char hex){if(':'>hex&&'/'<hex)return(byte)(hex-'0');if('G'
>hex&&'@'<hex)return(byte)(hex-'7'); if('g'>hex&&'`'<hex)return(byte)(hex-'W'); throw new ArgumentException("The value was not hex.","hex");}}}namespace
 L{sealed class Ast{
#region Kinds
public const int None=0;public const int Lit=1;public const int Set=2;public const int NSet=3;public const int Cls=4;public const int Cat=5;public const
 int Opt=6;public const int Alt=7;public const int Star=8;public const int Plus=9;public const int Rep=10;public const int UCode=11;public const int NUCode
=12;internal const int Dot=13;
#endregion Kinds
public int Kind=None;public bool IsLazy=false;public Ast[]Exprs=null;public int Value='\0';public int[]Ranges;public int Min=0;public int Max=0;internal
 static Ast Parse(LexContext pc){Ast result=null,next=null;int ich;pc.EnsureStarted();while(true){switch(pc.Current){case-1:return result;case'.':var dot
=new Ast();dot.Kind=Ast.Dot;if(null==result)result=dot;else{var cat=new Ast();cat.Kind=Ast.Cat;cat.Exprs=new Ast[]{result,dot};result=cat;}pc.Advance();
result=_ParseModifier(result,pc);break;case'\\':pc.Advance();pc.Expecting();var isNot=false;switch(pc.Current){case'P':isNot=true;goto case'p';case'p':
pc.Advance();pc.Expecting('{');var uc=new StringBuilder();int uli=pc.Line;int uco=pc.Column;long upo=pc.Position;while(-1!=pc.Advance()&&'}'!=pc.Current)
uc.Append((char)pc.Current);pc.Expecting('}');pc.Advance();int uci=0;switch(uc.ToString()){case"Pe":uci=21;break;case"Pc":uci=19;break;case"Cc":uci=14;
break;case"Sc":uci=26;break;case"Pd":uci=19;break;case"Nd":uci=8;break;case"Me":uci=7;break;case"Pf":uci=23;break;case"Cf":uci=15;break;case"Pi":uci=22;
break;case"Nl":uci=9;break;case"Zl":uci=12;break;case"Ll":uci=1;break;case"Sm":uci=25;break;case"Lm":uci=3;break;case"Sk":uci=27;break;case"Mn":uci=5;
break;case"Ps":uci=20;break;case"Lo":uci=4;break;case"Cn":uci=29;break;case"No":uci=10;break;case"Po":uci=24;break;case"So":uci=28;break;case"Zp":uci=
13;break;case"Co":uci=17;break;case"Zs":uci=11;break;case"Mc":uci=6;break;case"Cs":uci=16;break;case"Lt":uci=2;break;case"Lu":uci=0;break;}next=new Ast();
next.Value=uci;next.Kind=isNot?Ast.NUCode:Ast.UCode;break;case'd':next=new Ast();next.Kind=Ast.Set;next.Ranges=new int[]{'0','9'};pc.Advance();break;case
'D':next=new Ast();next.Kind=Ast.NSet;next.Ranges=new int[]{'0','9'};pc.Advance();break;case's':next=new Ast();next.Kind=Ast.Set;next.Ranges=new int[]
{'\t','\t',' ',' ','\r','\r','\n','\n','\f','\f'};pc.Advance();break;case'S':next=new Ast();next.Kind=Ast.NSet;next.Ranges=new int[]{'\t','\t',' ',' ',
'\r','\r','\n','\n','\f','\f'};pc.Advance();break;case'w':next=new Ast();next.Kind=Ast.Set;next.Ranges=new int[]{'_','_','0','9','A','Z','a','z',};pc.Advance();
break;case'W':next=new Ast();next.Kind=Ast.NSet;next.Ranges=new int[]{'_','_','0','9','A','Z','a','z',};pc.Advance();break;default:if(-1!=(ich=_ParseEscapePart(pc)))
{next=new Ast();next.Kind=Ast.Lit;next.Value=ich;}else{pc.Expecting(); return null;}break;}next=_ParseModifier(next,pc);if(null!=result){var cat=new Ast();
cat.Kind=Ast.Cat;cat.Exprs=new Ast[]{result,next};result=cat;}else result=next;break;case')':return result;case'(':pc.Advance();pc.Expecting();next=Parse(pc);
pc.Expecting(')');pc.Advance();next=_ParseModifier(next,pc);if(null==result)result=next;else{var cat=new Ast();cat.Kind=Ast.Cat;cat.Exprs=new Ast[]{result,
next};result=cat;}break;case'|':if(-1!=pc.Advance()){next=Parse(pc);if(null!=result&&Ast.Lit==result.Kind&&Ast.Lit==next.Kind){var set=new Ast();set.Kind
=Set;set.Ranges=new int[]{result.Value,result.Value,next.Value,next.Value};result=set;}else if(null!=result&&Ast.Lit==result.Kind&&Ast.Set==next.Kind)
{var set=new Ast();set.Kind=Ast.Set;set.Ranges=new int[next.Ranges.Length+2];set.Ranges[0]=result.Value;set.Ranges[1]=result.Value;Array.Copy(next.Ranges,
0,set.Ranges,2,next.Ranges.Length);result=set;}else if(null!=result&&Ast.Alt==result.Kind){var exprs=new Ast[result.Exprs.Length+1];Array.Copy(result.Exprs,
0,exprs,0,result.Exprs.Length);exprs[exprs.Length-1]=next;result.Exprs=exprs;}else{var alt=new Ast();alt.Kind=Ast.Alt;if(null==next||next.Kind!=Alt){alt.Exprs
=new Ast[]{result,next};result=alt;}else{var exprs=new Ast[1+next.Exprs.Length];Array.Copy(next.Exprs,0,exprs,1,next.Exprs.Length);exprs[0]=result;alt.Exprs
=exprs;result=alt;}}}else{var opt=new Ast();opt.Kind=Ast.Opt;opt.Exprs=new Ast[]{result};result=opt;}break;case'[':var seti=_ParseSet(pc);next=new Ast();
next.Kind=(seti.Key)?NSet:Set;next.Ranges=seti.Value;next=_ParseModifier(next,pc);if(null==result)result=next;else{var cat=new Ast();cat.Kind=Ast.Cat;
cat.Exprs=new Ast[]{result,next};result=cat;}break;default:ich=pc.Current;if(char.IsHighSurrogate((char)ich)){if(-1==pc.Advance())throw new ExpectingException("Expecting low surrogate in Unicode stream",
pc.Line,pc.Column,pc.Position,pc.FileOrUrl,"low-surrogate");ich=char.ConvertToUtf32((char)ich,(char)pc.Current);}next=new Ast();next.Kind=Ast.Lit;next.Value
=ich;pc.Advance();next=_ParseModifier(next,pc);if(null==result)result=next;else{var cat=new Ast();cat.Kind=Ast.Cat;cat.Exprs=new Ast[]{result,next};result
=cat;}break;}}}static KeyValuePair<bool,int[]>_ParseSet(LexContext pc){var result=new List<int>();pc.EnsureStarted();pc.Expecting('[');pc.Advance();pc.Expecting();
var isNot=false;if('^'==pc.Current){isNot=true;pc.Advance();pc.Expecting();}var firstRead=true;int firstChar='\0';var readFirstChar=false;var wantRange
=false;while(-1!=pc.Current&&(firstRead||']'!=pc.Current)){if(!wantRange){ if('['==pc.Current){pc.Advance();pc.Expecting();if(':'!=pc.Current){firstChar
='[';readFirstChar=true;}else{pc.Advance();pc.Expecting();var ll=pc.CaptureBuffer.Length;if(!pc.TryReadUntil(':',false))throw new ExpectingException("Expecting character class",
pc.Line,pc.Column,pc.Position,pc.FileOrUrl);pc.Expecting(':');pc.Advance();pc.Expecting(']');pc.Advance();var cls=pc.GetCapture(ll);result.AddRange(Lex.GetCharacterClass(cls));
readFirstChar=false;wantRange=false;firstRead=false;continue;}}if(!readFirstChar){if(char.IsHighSurrogate((char)pc.Current)){var chh=(char)pc.Current;
pc.Advance();pc.Expecting();firstChar=char.ConvertToUtf32(chh,(char)pc.Current);pc.Advance();pc.Expecting();}else if('\\'==pc.Current){pc.Advance();firstChar
=_ParseRangeEscapePart(pc);}else{firstChar=pc.Current;pc.Advance();pc.Expecting();}readFirstChar=true;}else{if('-'==pc.Current){pc.Advance();pc.Expecting();
wantRange=true;}else{result.Add(firstChar);result.Add(firstChar);readFirstChar=false;}}firstRead=false;}else{if('\\'!=pc.Current){var ch=0;if(char.IsHighSurrogate((char)pc.Current))
{var chh=(char)pc.Current;pc.Advance();pc.Expecting();ch=char.ConvertToUtf32(chh,(char)pc.Current);}else ch=(char)pc.Current;pc.Advance();pc.Expecting();
result.Add(firstChar);result.Add(ch);}else{result.Add(firstChar);pc.Advance();result.Add(_ParseRangeEscapePart(pc));}wantRange=false;readFirstChar=false;
}}if(readFirstChar){result.Add(firstChar);result.Add(firstChar);if(wantRange){result.Add('-');result.Add('-');}}pc.Expecting(']');pc.Advance();return new
 KeyValuePair<bool,int[]>(isNot,result.ToArray());}static int[]_ParseRanges(LexContext pc){pc.EnsureStarted();var result=new List<int>();int[]next=null;
bool readDash=false;while(-1!=pc.Current&&']'!=pc.Current){switch(pc.Current){case'[': if(null!=next){result.Add(next[0]);result.Add(next[1]);if(readDash)
{result.Add('-');result.Add('-');}}pc.Advance();pc.Expecting(':');pc.Advance();var l=pc.CaptureBuffer.Length;var lin=pc.Line;var col=pc.Column;var pos
=pc.Position;pc.TryReadUntil(':',false);var n=pc.GetCapture(l);pc.Advance();pc.Expecting(']');pc.Advance();int[]rngs;if(!CharCls.CharacterClasses.TryGetValue(n,
out rngs)){var sa=new string[CharCls.CharacterClasses.Count];CharCls.CharacterClasses.Keys.CopyTo(sa,0);throw new ExpectingException("Invalid character class "
+n,lin,col,pos,pc.FileOrUrl,sa);}result.AddRange(rngs);readDash=false;next=null;break;case'\\':pc.Advance();pc.Expecting();switch(pc.Current){case'h':
_ParseCharClassEscape(pc,"space",result,ref next,ref readDash);break;case'd':_ParseCharClassEscape(pc,"digit",result,ref next,ref readDash);break;case
'D':_ParseCharClassEscape(pc,"^digit",result,ref next,ref readDash);break;case'l':_ParseCharClassEscape(pc,"lower",result,ref next,ref readDash);break;
case's':_ParseCharClassEscape(pc,"space",result,ref next,ref readDash);break;case'S':_ParseCharClassEscape(pc,"^space",result,ref next,ref readDash);break;
case'u':_ParseCharClassEscape(pc,"upper",result,ref next,ref readDash);break;case'w':_ParseCharClassEscape(pc,"word",result,ref next,ref readDash);break;
case'W':_ParseCharClassEscape(pc,"^word",result,ref next,ref readDash);break;default:var ch=(char)_ParseRangeEscapePart(pc);if(null==next)next=new int[]
{ch,ch};else if(readDash){result.Add(next[0]);result.Add(ch);next=null;readDash=false;}else{result.AddRange(next);next=new int[]{ch,ch};}break;}break;
case'-':pc.Advance();if(null==next){next=new int[]{'-','-'};readDash=false;}else{if(readDash)result.AddRange(next);readDash=true;}break;default:if(null
==next){next=new int[]{pc.Current,pc.Current};}else{if(readDash){result.Add(next[0]);result.Add((char)pc.Current);next=null;readDash=false;}else{result.AddRange(next);
next=new int[]{pc.Current,pc.Current};}}pc.Advance();break;}}if(null!=next){result.AddRange(next);if(readDash){result.Add('-');result.Add('-');}}return
 result.ToArray();}static void _ParseCharClassEscape(LexContext pc,string cls,List<int>result,ref int[]next,ref bool readDash){if(null!=next){result.AddRange(next);
if(readDash){result.Add('-');result.Add('-');}result.Add('-');result.Add('-');}pc.Advance();int[]rngs;if(!CharCls.CharacterClasses.TryGetValue(cls,out
 rngs)){var sa=new string[CharCls.CharacterClasses.Count];CharCls.CharacterClasses.Keys.CopyTo(sa,0);throw new ExpectingException("Invalid character class "
+cls,pc.Line,pc.Column,pc.Position,pc.FileOrUrl,sa);}result.AddRange(rngs);next=null;readDash=false;}static Ast _ParseModifier(Ast expr,LexContext pc)
{var line=pc.Line;var column=pc.Column;var position=pc.Position;switch(pc.Current){case'*':var rep=new Ast();rep.Kind=Ast.Star;rep.Exprs=new Ast[]{expr
};expr=rep;pc.Advance();if('?'==pc.Current){rep.IsLazy=true;pc.Advance();}break;case'+':rep=new Ast();rep.Kind=Ast.Plus;rep.Exprs=new Ast[]{expr};expr
=rep;pc.Advance();if('?'==pc.Current){rep.IsLazy=true;pc.Advance();}break;case'?':var opt=new Ast();opt.Kind=Ast.Opt;opt.Exprs=new Ast[]{expr};expr=opt;
pc.Advance();if('?'==pc.Current){opt.IsLazy=true;pc.Advance();}break;case'{':pc.Advance();pc.TrySkipWhiteSpace();pc.Expecting('0','1','2','3','4','5',
'6','7','8','9',',','}');var min=-1;var max=-1;if(','!=pc.Current&&'}'!=pc.Current){var l=pc.CaptureBuffer.Length;pc.TryReadDigits();min=int.Parse(pc.GetCapture(l));
pc.TrySkipWhiteSpace();}if(','==pc.Current){pc.Advance();pc.TrySkipWhiteSpace();pc.Expecting('0','1','2','3','4','5','6','7','8','9','}');if('}'!=pc.Current)
{var l=pc.CaptureBuffer.Length;pc.TryReadDigits();max=int.Parse(pc.GetCapture(l));pc.TrySkipWhiteSpace();}}else{max=min;}pc.Expecting('}');pc.Advance();
rep=new Ast();rep.Exprs=new Ast[]{expr};rep.Kind=Ast.Rep;rep.Min=min;rep.Max=max;expr=rep;if('?'==pc.Current){rep.IsLazy=true;pc.Advance();}break;}return
 expr;}static byte _FromHexChar(char hex){if(':'>hex&&'/'<hex)return(byte)(hex-'0');if('G'>hex&&'@'<hex)return(byte)(hex-'7'); if('g'>hex&&'`'<hex)return
(byte)(hex-'W'); throw new ArgumentException("The value was not hex.","hex");}static bool _IsHexChar(char hex){if(':'>hex&&'/'<hex)return true;if('G'>
hex&&'@'<hex)return true;if('g'>hex&&'`'<hex)return true;return false;} static int _ParseEscapePart(LexContext pc){if(-1==pc.Current)return-1;switch(pc.Current)
{case'f':pc.Advance();return'\f';case'v':pc.Advance();return'\v';case't':pc.Advance();return'\t';case'n':pc.Advance();return'\n';case'r':pc.Advance();
return'\r';case'x':if(-1==pc.Advance()||!_IsHexChar((char)pc.Current))return'x';byte b=_FromHexChar((char)pc.Current);if(-1==pc.Advance()||!_IsHexChar((char)pc.Current))
return unchecked((char)b);b<<=4;b|=_FromHexChar((char)pc.Current);if(-1==pc.Advance()||!_IsHexChar((char)pc.Current))return unchecked((char)b);b<<=4;b
|=_FromHexChar((char)pc.Current);if(-1==pc.Advance()||!_IsHexChar((char)pc.Current))return unchecked((char)b);b<<=4;b|=_FromHexChar((char)pc.Current);
return unchecked((char)b);case'u':if(-1==pc.Advance())return'u';ushort u=_FromHexChar((char)pc.Current);u<<=4;if(-1==pc.Advance())return unchecked((char)u);
u|=_FromHexChar((char)pc.Current);u<<=4;if(-1==pc.Advance())return unchecked((char)u);u|=_FromHexChar((char)pc.Current);u<<=4;if(-1==pc.Advance())return
 unchecked((char)u);u|=_FromHexChar((char)pc.Current);return unchecked((char)u);default:int i=pc.Current;pc.Advance();if(char.IsHighSurrogate((char)i))
{i=char.ConvertToUtf32((char)i,(char)pc.Current);pc.Advance();}return(char)i;}}static int _ParseRangeEscapePart(LexContext pc){if(-1==pc.Current)return
-1;switch(pc.Current){case'f':pc.Advance();return'\f';case'v':pc.Advance();return'\v';case't':pc.Advance();return'\t';case'n':pc.Advance();return'\n';
case'r':pc.Advance();return'\r';case'x':if(-1==pc.Advance()||!_IsHexChar((char)pc.Current))return'x';byte b=_FromHexChar((char)pc.Current);if(-1==pc.Advance()
||!_IsHexChar((char)pc.Current))return unchecked((char)b);b<<=4;b|=_FromHexChar((char)pc.Current);if(-1==pc.Advance()||!_IsHexChar((char)pc.Current))return
 unchecked((char)b);b<<=4;b|=_FromHexChar((char)pc.Current);if(-1==pc.Advance()||!_IsHexChar((char)pc.Current))return unchecked((char)b);b<<=4;b|=_FromHexChar((char)pc.Current);
return unchecked((char)b);case'u':if(-1==pc.Advance())return'u';ushort u=_FromHexChar((char)pc.Current);u<<=4;if(-1==pc.Advance())return unchecked((char)u);
u|=_FromHexChar((char)pc.Current);u<<=4;if(-1==pc.Advance())return unchecked((char)u);u|=_FromHexChar((char)pc.Current);u<<=4;if(-1==pc.Advance())return
 unchecked((char)u);u|=_FromHexChar((char)pc.Current);return unchecked((char)u);default:int i=pc.Current;pc.Advance();if(char.IsHighSurrogate((char)i))
{i=char.ConvertToUtf32((char)i,(char)pc.Current);pc.Advance();}return(char)i;}}}}namespace L{partial class CharCls{static Lazy<IDictionary<string,int[]>>
_CharacterClasses=new Lazy<IDictionary<string,int[]>>(_GetCharacterClasses);static IDictionary<string,int[]>_GetCharacterClasses(){var result=new Dictionary<string,
int[]>();var fa=typeof(CharCls).GetFields();for(var i=0;i<fa.Length;i++){var f=fa[i];if(f.FieldType==typeof(int[])){result.Add(f.Name,(int[])f.GetValue(null));
}}return result;}public static IDictionary<string,int[]>CharacterClasses{get{return _CharacterClasses.Value;}}}}namespace L{internal partial class CharCls
{public static int[][]UnicodeCategories=new int[][]{new int[]{65,90,192,214,216,222,256,256,258,258,260,260,262,262,264,264,266,266,268,268,270,270,272,
272,274,274,276,276,278,278,280,280,282,282,284,284,286,286,288,288,290,290,292,292,294,294,296,296,298,298,300,300,302,302,304,304,306,306,308,308,310,
310,313,313,315,315,317,317,319,319,321,321,323,323,325,325,327,327,330,330,332,332,334,334,336,336,338,338,340,340,342,342,344,344,346,346,348,348,350,
350,352,352,354,354,356,356,358,358,360,360,362,362,364,364,366,366,368,368,370,370,372,372,374,374,376,377,379,379,381,381,385,386,388,388,390,391,393,
395,398,401,403,404,406,408,412,413,415,416,418,418,420,420,422,423,425,425,428,428,430,431,433,435,437,437,439,440,444,444,452,452,455,455,458,458,461,
461,463,463,465,465,467,467,469,469,471,471,473,473,475,475,478,478,480,480,482,482,484,484,486,486,488,488,490,490,492,492,494,494,497,497,500,500,502,
504,506,506,508,508,510,510,512,512,514,514,516,516,518,518,520,520,522,522,524,524,526,526,528,528,530,530,532,532,534,534,536,536,538,538,540,540,542,
542,544,544,546,546,548,548,550,550,552,552,554,554,556,556,558,558,560,560,562,562,570,571,573,574,577,577,579,582,584,584,586,586,588,588,590,590,880,
880,882,882,886,886,895,895,902,902,904,906,908,908,910,911,913,929,931,939,975,975,978,980,984,984,986,986,988,988,990,990,992,992,994,994,996,996,998,
998,1000,1000,1002,1002,1004,1004,1006,1006,1012,1012,1015,1015,1017,1018,1021,1071,1120,1120,1122,1122,1124,1124,1126,1126,1128,1128,1130,1130,1132,1132,
1134,1134,1136,1136,1138,1138,1140,1140,1142,1142,1144,1144,1146,1146,1148,1148,1150,1150,1152,1152,1162,1162,1164,1164,1166,1166,1168,1168,1170,1170,
1172,1172,1174,1174,1176,1176,1178,1178,1180,1180,1182,1182,1184,1184,1186,1186,1188,1188,1190,1190,1192,1192,1194,1194,1196,1196,1198,1198,1200,1200,
1202,1202,1204,1204,1206,1206,1208,1208,1210,1210,1212,1212,1214,1214,1216,1217,1219,1219,1221,1221,1223,1223,1225,1225,1227,1227,1229,1229,1232,1232,
1234,1234,1236,1236,1238,1238,1240,1240,1242,1242,1244,1244,1246,1246,1248,1248,1250,1250,1252,1252,1254,1254,1256,1256,1258,1258,1260,1260,1262,1262,
1264,1264,1266,1266,1268,1268,1270,1270,1272,1272,1274,1274,1276,1276,1278,1278,1280,1280,1282,1282,1284,1284,1286,1286,1288,1288,1290,1290,1292,1292,
1294,1294,1296,1296,1298,1298,1300,1300,1302,1302,1304,1304,1306,1306,1308,1308,1310,1310,1312,1312,1314,1314,1316,1316,1318,1318,1320,1320,1322,1322,
1324,1324,1326,1326,1329,1366,4256,4293,4295,4295,4301,4301,5024,5109,7680,7680,7682,7682,7684,7684,7686,7686,7688,7688,7690,7690,7692,7692,7694,7694,
7696,7696,7698,7698,7700,7700,7702,7702,7704,7704,7706,7706,7708,7708,7710,7710,7712,7712,7714,7714,7716,7716,7718,7718,7720,7720,7722,7722,7724,7724,
7726,7726,7728,7728,7730,7730,7732,7732,7734,7734,7736,7736,7738,7738,7740,7740,7742,7742,7744,7744,7746,7746,7748,7748,7750,7750,7752,7752,7754,7754,
7756,7756,7758,7758,7760,7760,7762,7762,7764,7764,7766,7766,7768,7768,7770,7770,7772,7772,7774,7774,7776,7776,7778,7778,7780,7780,7782,7782,7784,7784,
7786,7786,7788,7788,7790,7790,7792,7792,7794,7794,7796,7796,7798,7798,7800,7800,7802,7802,7804,7804,7806,7806,7808,7808,7810,7810,7812,7812,7814,7814,
7816,7816,7818,7818,7820,7820,7822,7822,7824,7824,7826,7826,7828,7828,7838,7838,7840,7840,7842,7842,7844,7844,7846,7846,7848,7848,7850,7850,7852,7852,
7854,7854,7856,7856,7858,7858,7860,7860,7862,7862,7864,7864,7866,7866,7868,7868,7870,7870,7872,7872,7874,7874,7876,7876,7878,7878,7880,7880,7882,7882,
7884,7884,7886,7886,7888,7888,7890,7890,7892,7892,7894,7894,7896,7896,7898,7898,7900,7900,7902,7902,7904,7904,7906,7906,7908,7908,7910,7910,7912,7912,
7914,7914,7916,7916,7918,7918,7920,7920,7922,7922,7924,7924,7926,7926,7928,7928,7930,7930,7932,7932,7934,7934,7944,7951,7960,7965,7976,7983,7992,7999,
8008,8013,8025,8025,8027,8027,8029,8029,8031,8031,8040,8047,8120,8123,8136,8139,8152,8155,8168,8172,8184,8187,8450,8450,8455,8455,8459,8461,8464,8466,
8469,8469,8473,8477,8484,8484,8486,8486,8488,8488,8490,8493,8496,8499,8510,8511,8517,8517,8579,8579,11264,11310,11360,11360,11362,11364,11367,11367,11369,
11369,11371,11371,11373,11376,11378,11378,11381,11381,11390,11392,11394,11394,11396,11396,11398,11398,11400,11400,11402,11402,11404,11404,11406,11406,
11408,11408,11410,11410,11412,11412,11414,11414,11416,11416,11418,11418,11420,11420,11422,11422,11424,11424,11426,11426,11428,11428,11430,11430,11432,
11432,11434,11434,11436,11436,11438,11438,11440,11440,11442,11442,11444,11444,11446,11446,11448,11448,11450,11450,11452,11452,11454,11454,11456,11456,
11458,11458,11460,11460,11462,11462,11464,11464,11466,11466,11468,11468,11470,11470,11472,11472,11474,11474,11476,11476,11478,11478,11480,11480,11482,
11482,11484,11484,11486,11486,11488,11488,11490,11490,11499,11499,11501,11501,11506,11506,42560,42560,42562,42562,42564,42564,42566,42566,42568,42568,
42570,42570,42572,42572,42574,42574,42576,42576,42578,42578,42580,42580,42582,42582,42584,42584,42586,42586,42588,42588,42590,42590,42592,42592,42594,
42594,42596,42596,42598,42598,42600,42600,42602,42602,42604,42604,42624,42624,42626,42626,42628,42628,42630,42630,42632,42632,42634,42634,42636,42636,
42638,42638,42640,42640,42642,42642,42644,42644,42646,42646,42648,42648,42650,42650,42786,42786,42788,42788,42790,42790,42792,42792,42794,42794,42796,
42796,42798,42798,42802,42802,42804,42804,42806,42806,42808,42808,42810,42810,42812,42812,42814,42814,42816,42816,42818,42818,42820,42820,42822,42822,
42824,42824,42826,42826,42828,42828,42830,42830,42832,42832,42834,42834,42836,42836,42838,42838,42840,42840,42842,42842,42844,42844,42846,42846,42848,
42848,42850,42850,42852,42852,42854,42854,42856,42856,42858,42858,42860,42860,42862,42862,42873,42873,42875,42875,42877,42878,42880,42880,42882,42882,
42884,42884,42886,42886,42891,42891,42893,42893,42896,42896,42898,42898,42902,42902,42904,42904,42906,42906,42908,42908,42910,42910,42912,42912,42914,
42914,42916,42916,42918,42918,42920,42920,42922,42925,42928,42932,42934,42934,65313,65338,66560,66599,68736,68786,71840,71871,119808,119833,119860,119885,
119912,119937,119964,119964,119966,119967,119970,119970,119973,119974,119977,119980,119982,119989,120016,120041,120068,120069,120071,120074,120077,120084,
120086,120092,120120,120121,120123,120126,120128,120132,120134,120134,120138,120144,120172,120197,120224,120249,120276,120301,120328,120353,120380,120405,
120432,120457,120488,120512,120546,120570,120604,120628,120662,120686,120720,120744,120778,120778},new int[]{97,122,170,170,181,181,186,186,223,246,248,
255,257,257,259,259,261,261,263,263,265,265,267,267,269,269,271,271,273,273,275,275,277,277,279,279,281,281,283,283,285,285,287,287,289,289,291,291,293,
293,295,295,297,297,299,299,301,301,303,303,305,305,307,307,309,309,311,312,314,314,316,316,318,318,320,320,322,322,324,324,326,326,328,329,331,331,333,
333,335,335,337,337,339,339,341,341,343,343,345,345,347,347,349,349,351,351,353,353,355,355,357,357,359,359,361,361,363,363,365,365,367,367,369,369,371,
371,373,373,375,375,378,378,380,380,382,384,387,387,389,389,392,392,396,397,402,402,405,405,409,411,414,414,417,417,419,419,421,421,424,424,426,427,429,
429,432,432,436,436,438,438,441,442,445,447,454,454,457,457,460,460,462,462,464,464,466,466,468,468,470,470,472,472,474,474,476,477,479,479,481,481,483,
483,485,485,487,487,489,489,491,491,493,493,495,496,499,499,501,501,505,505,507,507,509,509,511,511,513,513,515,515,517,517,519,519,521,521,523,523,525,
525,527,527,529,529,531,531,533,533,535,535,537,537,539,539,541,541,543,543,545,545,547,547,549,549,551,551,553,553,555,555,557,557,559,559,561,561,563,
569,572,572,575,576,578,578,583,583,585,585,587,587,589,589,591,659,661,687,881,881,883,883,887,887,891,893,912,912,940,974,976,977,981,983,985,985,987,
987,989,989,991,991,993,993,995,995,997,997,999,999,1001,1001,1003,1003,1005,1005,1007,1011,1013,1013,1016,1016,1019,1020,1072,1119,1121,1121,1123,1123,
1125,1125,1127,1127,1129,1129,1131,1131,1133,1133,1135,1135,1137,1137,1139,1139,1141,1141,1143,1143,1145,1145,1147,1147,1149,1149,1151,1151,1153,1153,
1163,1163,1165,1165,1167,1167,1169,1169,1171,1171,1173,1173,1175,1175,1177,1177,1179,1179,1181,1181,1183,1183,1185,1185,1187,1187,1189,1189,1191,1191,
1193,1193,1195,1195,1197,1197,1199,1199,1201,1201,1203,1203,1205,1205,1207,1207,1209,1209,1211,1211,1213,1213,1215,1215,1218,1218,1220,1220,1222,1222,
1224,1224,1226,1226,1228,1228,1230,1231,1233,1233,1235,1235,1237,1237,1239,1239,1241,1241,1243,1243,1245,1245,1247,1247,1249,1249,1251,1251,1253,1253,
1255,1255,1257,1257,1259,1259,1261,1261,1263,1263,1265,1265,1267,1267,1269,1269,1271,1271,1273,1273,1275,1275,1277,1277,1279,1279,1281,1281,1283,1283,
1285,1285,1287,1287,1289,1289,1291,1291,1293,1293,1295,1295,1297,1297,1299,1299,1301,1301,1303,1303,1305,1305,1307,1307,1309,1309,1311,1311,1313,1313,
1315,1315,1317,1317,1319,1319,1321,1321,1323,1323,1325,1325,1327,1327,1377,1415,5112,5117,7424,7467,7531,7543,7545,7578,7681,7681,7683,7683,7685,7685,
7687,7687,7689,7689,7691,7691,7693,7693,7695,7695,7697,7697,7699,7699,7701,7701,7703,7703,7705,7705,7707,7707,7709,7709,7711,7711,7713,7713,7715,7715,
7717,7717,7719,7719,7721,7721,7723,7723,7725,7725,7727,7727,7729,7729,7731,7731,7733,7733,7735,7735,7737,7737,7739,7739,7741,7741,7743,7743,7745,7745,
7747,7747,7749,7749,7751,7751,7753,7753,7755,7755,7757,7757,7759,7759,7761,7761,7763,7763,7765,7765,7767,7767,7769,7769,7771,7771,7773,7773,7775,7775,
7777,7777,7779,7779,7781,7781,7783,7783,7785,7785,7787,7787,7789,7789,7791,7791,7793,7793,7795,7795,7797,7797,7799,7799,7801,7801,7803,7803,7805,7805,
7807,7807,7809,7809,7811,7811,7813,7813,7815,7815,7817,7817,7819,7819,7821,7821,7823,7823,7825,7825,7827,7827,7829,7837,7839,7839,7841,7841,7843,7843,
7845,7845,7847,7847,7849,7849,7851,7851,7853,7853,7855,7855,7857,7857,7859,7859,7861,7861,7863,7863,7865,7865,7867,7867,7869,7869,7871,7871,7873,7873,
7875,7875,7877,7877,7879,7879,7881,7881,7883,7883,7885,7885,7887,7887,7889,7889,7891,7891,7893,7893,7895,7895,7897,7897,7899,7899,7901,7901,7903,7903,
7905,7905,7907,7907,7909,7909,7911,7911,7913,7913,7915,7915,7917,7917,7919,7919,7921,7921,7923,7923,7925,7925,7927,7927,7929,7929,7931,7931,7933,7933,
7935,7943,7952,7957,7968,7975,7984,7991,8000,8005,8016,8023,8032,8039,8048,8061,8064,8071,8080,8087,8096,8103,8112,8116,8118,8119,8126,8126,8130,8132,
8134,8135,8144,8147,8150,8151,8160,8167,8178,8180,8182,8183,8458,8458,8462,8463,8467,8467,8495,8495,8500,8500,8505,8505,8508,8509,8518,8521,8526,8526,
8580,8580,11312,11358,11361,11361,11365,11366,11368,11368,11370,11370,11372,11372,11377,11377,11379,11380,11382,11387,11393,11393,11395,11395,11397,11397,
11399,11399,11401,11401,11403,11403,11405,11405,11407,11407,11409,11409,11411,11411,11413,11413,11415,11415,11417,11417,11419,11419,11421,11421,11423,
11423,11425,11425,11427,11427,11429,11429,11431,11431,11433,11433,11435,11435,11437,11437,11439,11439,11441,11441,11443,11443,11445,11445,11447,11447,
11449,11449,11451,11451,11453,11453,11455,11455,11457,11457,11459,11459,11461,11461,11463,11463,11465,11465,11467,11467,11469,11469,11471,11471,11473,
11473,11475,11475,11477,11477,11479,11479,11481,11481,11483,11483,11485,11485,11487,11487,11489,11489,11491,11492,11500,11500,11502,11502,11507,11507,
11520,11557,11559,11559,11565,11565,42561,42561,42563,42563,42565,42565,42567,42567,42569,42569,42571,42571,42573,42573,42575,42575,42577,42577,42579,
42579,42581,42581,42583,42583,42585,42585,42587,42587,42589,42589,42591,42591,42593,42593,42595,42595,42597,42597,42599,42599,42601,42601,42603,42603,
42605,42605,42625,42625,42627,42627,42629,42629,42631,42631,42633,42633,42635,42635,42637,42637,42639,42639,42641,42641,42643,42643,42645,42645,42647,
42647,42649,42649,42651,42651,42787,42787,42789,42789,42791,42791,42793,42793,42795,42795,42797,42797,42799,42801,42803,42803,42805,42805,42807,42807,
42809,42809,42811,42811,42813,42813,42815,42815,42817,42817,42819,42819,42821,42821,42823,42823,42825,42825,42827,42827,42829,42829,42831,42831,42833,
42833,42835,42835,42837,42837,42839,42839,42841,42841,42843,42843,42845,42845,42847,42847,42849,42849,42851,42851,42853,42853,42855,42855,42857,42857,
42859,42859,42861,42861,42863,42863,42865,42872,42874,42874,42876,42876,42879,42879,42881,42881,42883,42883,42885,42885,42887,42887,42892,42892,42894,
42894,42897,42897,42899,42901,42903,42903,42905,42905,42907,42907,42909,42909,42911,42911,42913,42913,42915,42915,42917,42917,42919,42919,42921,42921,
42933,42933,42935,42935,43002,43002,43824,43866,43872,43877,43888,43967,64256,64262,64275,64279,65345,65370,66600,66639,68800,68850,71872,71903,119834,
119859,119886,119892,119894,119911,119938,119963,119990,119993,119995,119995,119997,120003,120005,120015,120042,120067,120094,120119,120146,120171,120198,
120223,120250,120275,120302,120327,120354,120379,120406,120431,120458,120485,120514,120538,120540,120545,120572,120596,120598,120603,120630,120654,120656,
120661,120688,120712,120714,120719,120746,120770,120772,120777,120779,120779},new int[]{453,453,456,456,459,459,498,498,8072,8079,8088,8095,8104,8111,
8124,8124,8140,8140,8188,8188},new int[]{688,705,710,721,736,740,748,748,750,750,884,884,890,890,1369,1369,1600,1600,1765,1766,2036,2037,2042,2042,2074,
2074,2084,2084,2088,2088,2417,2417,3654,3654,3782,3782,4348,4348,6103,6103,6211,6211,6823,6823,7288,7293,7468,7530,7544,7544,7579,7615,8305,8305,8319,
8319,8336,8348,11388,11389,11631,11631,11823,11823,12293,12293,12337,12341,12347,12347,12445,12446,12540,12542,40981,40981,42232,42237,42508,42508,42623,
42623,42652,42653,42775,42783,42864,42864,42888,42888,43000,43001,43471,43471,43494,43494,43632,43632,43741,43741,43763,43764,43868,43871,65392,65392,
65438,65439,92992,92995,94099,94111},new int[]{443,443,448,451,660,660,1488,1514,1520,1522,1568,1599,1601,1610,1646,1647,1649,1747,1749,1749,1774,1775,
1786,1788,1791,1791,1808,1808,1810,1839,1869,1957,1969,1969,1994,2026,2048,2069,2112,2136,2208,2228,2308,2361,2365,2365,2384,2384,2392,2401,2418,2432,
2437,2444,2447,2448,2451,2472,2474,2480,2482,2482,2486,2489,2493,2493,2510,2510,2524,2525,2527,2529,2544,2545,2565,2570,2575,2576,2579,2600,2602,2608,
2610,2611,2613,2614,2616,2617,2649,2652,2654,2654,2674,2676,2693,2701,2703,2705,2707,2728,2730,2736,2738,2739,2741,2745,2749,2749,2768,2768,2784,2785,
2809,2809,2821,2828,2831,2832,2835,2856,2858,2864,2866,2867,2869,2873,2877,2877,2908,2909,2911,2913,2929,2929,2947,2947,2949,2954,2958,2960,2962,2965,
2969,2970,2972,2972,2974,2975,2979,2980,2984,2986,2990,3001,3024,3024,3077,3084,3086,3088,3090,3112,3114,3129,3133,3133,3160,3162,3168,3169,3205,3212,
3214,3216,3218,3240,3242,3251,3253,3257,3261,3261,3294,3294,3296,3297,3313,3314,3333,3340,3342,3344,3346,3386,3389,3389,3406,3406,3423,3425,3450,3455,
3461,3478,3482,3505,3507,3515,3517,3517,3520,3526,3585,3632,3634,3635,3648,3653,3713,3714,3716,3716,3719,3720,3722,3722,3725,3725,3732,3735,3737,3743,
3745,3747,3749,3749,3751,3751,3754,3755,3757,3760,3762,3763,3773,3773,3776,3780,3804,3807,3840,3840,3904,3911,3913,3948,3976,3980,4096,4138,4159,4159,
4176,4181,4186,4189,4193,4193,4197,4198,4206,4208,4213,4225,4238,4238,4304,4346,4349,4680,4682,4685,4688,4694,4696,4696,4698,4701,4704,4744,4746,4749,
4752,4784,4786,4789,4792,4798,4800,4800,4802,4805,4808,4822,4824,4880,4882,4885,4888,4954,4992,5007,5121,5740,5743,5759,5761,5786,5792,5866,5873,5880,
5888,5900,5902,5905,5920,5937,5952,5969,5984,5996,5998,6000,6016,6067,6108,6108,6176,6210,6212,6263,6272,6312,6314,6314,6320,6389,6400,6430,6480,6509,
6512,6516,6528,6571,6576,6601,6656,6678,6688,6740,6917,6963,6981,6987,7043,7072,7086,7087,7098,7141,7168,7203,7245,7247,7258,7287,7401,7404,7406,7409,
7413,7414,8501,8504,11568,11623,11648,11670,11680,11686,11688,11694,11696,11702,11704,11710,11712,11718,11720,11726,11728,11734,11736,11742,12294,12294,
12348,12348,12353,12438,12447,12447,12449,12538,12543,12543,12549,12589,12593,12686,12704,12730,12784,12799,13312,19893,19968,40917,40960,40980,40982,
42124,42192,42231,42240,42507,42512,42527,42538,42539,42606,42606,42656,42725,42895,42895,42999,42999,43003,43009,43011,43013,43015,43018,43020,43042,
43072,43123,43138,43187,43250,43255,43259,43259,43261,43261,43274,43301,43312,43334,43360,43388,43396,43442,43488,43492,43495,43503,43514,43518,43520,
43560,43584,43586,43588,43595,43616,43631,43633,43638,43642,43642,43646,43695,43697,43697,43701,43702,43705,43709,43712,43712,43714,43714,43739,43740,
43744,43754,43762,43762,43777,43782,43785,43790,43793,43798,43808,43814,43816,43822,43968,44002,44032,55203,55216,55238,55243,55291,63744,64109,64112,
64217,64285,64285,64287,64296,64298,64310,64312,64316,64318,64318,64320,64321,64323,64324,64326,64433,64467,64829,64848,64911,64914,64967,65008,65019,
65136,65140,65142,65276,65382,65391,65393,65437,65440,65470,65474,65479,65482,65487,65490,65495,65498,65500,65536,65547,65549,65574,65576,65594,65596,
65597,65599,65613,65616,65629,65664,65786,66176,66204,66208,66256,66304,66335,66352,66368,66370,66377,66384,66421,66432,66461,66464,66499,66504,66511,
66640,66717,66816,66855,66864,66915,67072,67382,67392,67413,67424,67431,67584,67589,67592,67592,67594,67637,67639,67640,67644,67644,67647,67669,67680,
67702,67712,67742,67808,67826,67828,67829,67840,67861,67872,67897,67968,68023,68030,68031,68096,68096,68112,68115,68117,68119,68121,68147,68192,68220,
68224,68252,68288,68295,68297,68324,68352,68405,68416,68437,68448,68466,68480,68497,68608,68680,69635,69687,69763,69807,69840,69864,69891,69926,69968,
70002,70006,70006,70019,70066,70081,70084,70106,70106,70108,70108,70144,70161,70163,70187,70272,70278,70280,70280,70282,70285,70287,70301,70303,70312,
70320,70366,70405,70412,70415,70416,70419,70440,70442,70448,70450,70451,70453,70457,70461,70461,70480,70480,70493,70497,70784,70831,70852,70853,70855,
70855,71040,71086,71128,71131,71168,71215,71236,71236,71296,71338,71424,71449,71935,71935,72384,72440,73728,74649,74880,75075,77824,78894,82944,83526,
92160,92728,92736,92766,92880,92909,92928,92975,93027,93047,93053,93071,93952,94020,94032,94032,110592,110593,113664,113770,113776,113788,113792,113800,
113808,113817,124928,125124,126464,126467,126469,126495,126497,126498,126500,126500,126503,126503,126505,126514,126516,126519,126521,126521,126523,126523,
126530,126530,126535,126535,126537,126537,126539,126539,126541,126543,126545,126546,126548,126548,126551,126551,126553,126553,126555,126555,126557,126557,
126559,126559,126561,126562,126564,126564,126567,126570,126572,126578,126580,126583,126585,126588,126590,126590,126592,126601,126603,126619,126625,126627,
126629,126633,126635,126651,131072,173782,173824,177972,177984,178205,178208,183969,194560,195101},new int[]{768,879,1155,1159,1425,1469,1471,1471,1473,
1474,1476,1477,1479,1479,1552,1562,1611,1631,1648,1648,1750,1756,1759,1764,1767,1768,1770,1773,1809,1809,1840,1866,1958,1968,2027,2035,2070,2073,2075,
2083,2085,2087,2089,2093,2137,2139,2275,2306,2362,2362,2364,2364,2369,2376,2381,2381,2385,2391,2402,2403,2433,2433,2492,2492,2497,2500,2509,2509,2530,
2531,2561,2562,2620,2620,2625,2626,2631,2632,2635,2637,2641,2641,2672,2673,2677,2677,2689,2690,2748,2748,2753,2757,2759,2760,2765,2765,2786,2787,2817,
2817,2876,2876,2879,2879,2881,2884,2893,2893,2902,2902,2914,2915,2946,2946,3008,3008,3021,3021,3072,3072,3134,3136,3142,3144,3146,3149,3157,3158,3170,
3171,3201,3201,3260,3260,3263,3263,3270,3270,3276,3277,3298,3299,3329,3329,3393,3396,3405,3405,3426,3427,3530,3530,3538,3540,3542,3542,3633,3633,3636,
3642,3655,3662,3761,3761,3764,3769,3771,3772,3784,3789,3864,3865,3893,3893,3895,3895,3897,3897,3953,3966,3968,3972,3974,3975,3981,3991,3993,4028,4038,
4038,4141,4144,4146,4151,4153,4154,4157,4158,4184,4185,4190,4192,4209,4212,4226,4226,4229,4230,4237,4237,4253,4253,4957,4959,5906,5908,5938,5940,5970,
5971,6002,6003,6068,6069,6071,6077,6086,6086,6089,6099,6109,6109,6155,6157,6313,6313,6432,6434,6439,6440,6450,6450,6457,6459,6679,6680,6683,6683,6742,
6742,6744,6750,6752,6752,6754,6754,6757,6764,6771,6780,6783,6783,6832,6845,6912,6915,6964,6964,6966,6970,6972,6972,6978,6978,7019,7027,7040,7041,7074,
7077,7080,7081,7083,7085,7142,7142,7144,7145,7149,7149,7151,7153,7212,7219,7222,7223,7376,7378,7380,7392,7394,7400,7405,7405,7412,7412,7416,7417,7616,
7669,7676,7679,8400,8412,8417,8417,8421,8432,11503,11505,11647,11647,11744,11775,12330,12333,12441,12442,42607,42607,42612,42621,42654,42655,42736,42737,
43010,43010,43014,43014,43019,43019,43045,43046,43204,43204,43232,43249,43302,43309,43335,43345,43392,43394,43443,43443,43446,43449,43452,43452,43493,
43493,43561,43566,43569,43570,43573,43574,43587,43587,43596,43596,43644,43644,43696,43696,43698,43700,43703,43704,43710,43711,43713,43713,43756,43757,
43766,43766,44005,44005,44008,44008,44013,44013,64286,64286,65024,65039,65056,65071,66045,66045,66272,66272,66422,66426,68097,68099,68101,68102,68108,
68111,68152,68154,68159,68159,68325,68326,69633,69633,69688,69702,69759,69761,69811,69814,69817,69818,69888,69890,69927,69931,69933,69940,70003,70003,
70016,70017,70070,70078,70090,70092,70191,70193,70196,70196,70198,70199,70367,70367,70371,70378,70400,70401,70460,70460,70464,70464,70502,70508,70512,
70516,70835,70840,70842,70842,70847,70848,70850,70851,71090,71093,71100,71101,71103,71104,71132,71133,71219,71226,71229,71229,71231,71232,71339,71339,
71341,71341,71344,71349,71351,71351,71453,71455,71458,71461,71463,71467,92912,92916,92976,92982,94095,94098,113821,113822,119143,119145,119163,119170,
119173,119179,119210,119213,119362,119364,121344,121398,121403,121452,121461,121461,121476,121476,121499,121503,121505,121519,125136,125142,917760,917999},
new int[]{2307,2307,2363,2363,2366,2368,2377,2380,2382,2383,2434,2435,2494,2496,2503,2504,2507,2508,2519,2519,2563,2563,2622,2624,2691,2691,2750,2752,
2761,2761,2763,2764,2818,2819,2878,2878,2880,2880,2887,2888,2891,2892,2903,2903,3006,3007,3009,3010,3014,3016,3018,3020,3031,3031,3073,3075,3137,3140,
3202,3203,3262,3262,3264,3268,3271,3272,3274,3275,3285,3286,3330,3331,3390,3392,3398,3400,3402,3404,3415,3415,3458,3459,3535,3537,3544,3551,3570,3571,
3902,3903,3967,3967,4139,4140,4145,4145,4152,4152,4155,4156,4182,4183,4194,4196,4199,4205,4227,4228,4231,4236,4239,4239,4250,4252,6070,6070,6078,6085,
6087,6088,6435,6438,6441,6443,6448,6449,6451,6456,6681,6682,6741,6741,6743,6743,6753,6753,6755,6756,6765,6770,6916,6916,6965,6965,6971,6971,6973,6977,
6979,6980,7042,7042,7073,7073,7078,7079,7082,7082,7143,7143,7146,7148,7150,7150,7154,7155,7204,7211,7220,7221,7393,7393,7410,7411,12334,12335,43043,43044,
43047,43047,43136,43137,43188,43203,43346,43347,43395,43395,43444,43445,43450,43451,43453,43456,43567,43568,43571,43572,43597,43597,43643,43643,43645,
43645,43755,43755,43758,43759,43765,43765,44003,44004,44006,44007,44009,44010,44012,44012,69632,69632,69634,69634,69762,69762,69808,69810,69815,69816,
69932,69932,70018,70018,70067,70069,70079,70080,70188,70190,70194,70195,70197,70197,70368,70370,70402,70403,70462,70463,70465,70468,70471,70472,70475,
70477,70487,70487,70498,70499,70832,70834,70841,70841,70843,70846,70849,70849,71087,71089,71096,71099,71102,71102,71216,71218,71227,71228,71230,71230,
71340,71340,71342,71343,71350,71350,71456,71457,71462,71462,94033,94078,119141,119142,119149,119154},new int[]{1160,1161,6846,6846,8413,8416,8418,8420,
42608,42610},new int[]{48,57,1632,1641,1776,1785,1984,1993,2406,2415,2534,2543,2662,2671,2790,2799,2918,2927,3046,3055,3174,3183,3302,3311,3430,3439,3558,
3567,3664,3673,3792,3801,3872,3881,4160,4169,4240,4249,6112,6121,6160,6169,6470,6479,6608,6617,6784,6793,6800,6809,6992,7001,7088,7097,7232,7241,7248,
7257,42528,42537,43216,43225,43264,43273,43472,43481,43504,43513,43600,43609,44016,44025,65296,65305,66720,66729,69734,69743,69872,69881,69942,69951,70096,
70105,70384,70393,70864,70873,71248,71257,71360,71369,71472,71481,71904,71913,92768,92777,93008,93017,120782,120831},new int[]{5870,5872,8544,8578,8581,
8584,12295,12295,12321,12329,12344,12346,42726,42735,65856,65908,66369,66369,66378,66378,66513,66517,74752,74862},new int[]{178,179,185,185,188,190,2548,
2553,2930,2935,3056,3058,3192,3198,3440,3445,3882,3891,4969,4988,6128,6137,6618,6618,8304,8304,8308,8313,8320,8329,8528,8543,8585,8585,9312,9371,9450,
9471,10102,10131,11517,11517,12690,12693,12832,12841,12872,12879,12881,12895,12928,12937,12977,12991,43056,43061,65799,65843,65909,65912,65930,65931,66273,
66299,66336,66339,67672,67679,67705,67711,67751,67759,67835,67839,67862,67867,68028,68029,68032,68047,68050,68095,68160,68167,68221,68222,68253,68255,
68331,68335,68440,68447,68472,68479,68521,68527,68858,68863,69216,69246,69714,69733,70113,70132,71482,71483,71914,71922,93019,93025,119648,119665,125127,
125135,127232,127244},new int[]{32,32,160,160,5760,5760,8192,8202,8239,8239,8287,8287,12288,12288},new int[]{8232,8232},new int[]{8233,8233},new int[]
{0,31,127,159},new int[]{1536,1541,1564,1564,1757,1757,1807,1807,6158,6158,8203,8207,8234,8238,8288,8292,8294,8303,65279,65279,65529,65531,69821,69821,
113824,113827,119155,119162,917505,917505,917536,917631},new int[0],new int[]{57344,63743,983040,1048573,1048576,1114109},new int[]{95,95,8255,8256,8276,
8276,65075,65076,65101,65103,65343,65343},new int[]{45,45,173,173,1418,1418,1470,1470,5120,5120,6150,6150,8208,8213,11799,11799,11802,11802,11834,11835,
11840,11840,12316,12316,12336,12336,12448,12448,65073,65074,65112,65112,65123,65123,65293,65293},new int[]{40,40,91,91,123,123,3898,3898,3900,3900,5787,
5787,8218,8218,8222,8222,8261,8261,8317,8317,8333,8333,8968,8968,8970,8970,9001,9001,10088,10088,10090,10090,10092,10092,10094,10094,10096,10096,10098,
10098,10100,10100,10181,10181,10214,10214,10216,10216,10218,10218,10220,10220,10222,10222,10627,10627,10629,10629,10631,10631,10633,10633,10635,10635,
10637,10637,10639,10639,10641,10641,10643,10643,10645,10645,10647,10647,10712,10712,10714,10714,10748,10748,11810,11810,11812,11812,11814,11814,11816,
11816,11842,11842,12296,12296,12298,12298,12300,12300,12302,12302,12304,12304,12308,12308,12310,12310,12312,12312,12314,12314,12317,12317,64831,64831,
65047,65047,65077,65077,65079,65079,65081,65081,65083,65083,65085,65085,65087,65087,65089,65089,65091,65091,65095,65095,65113,65113,65115,65115,65117,
65117,65288,65288,65339,65339,65371,65371,65375,65375,65378,65378},new int[]{41,41,93,93,125,125,3899,3899,3901,3901,5788,5788,8262,8262,8318,8318,8334,
8334,8969,8969,8971,8971,9002,9002,10089,10089,10091,10091,10093,10093,10095,10095,10097,10097,10099,10099,10101,10101,10182,10182,10215,10215,10217,10217,
10219,10219,10221,10221,10223,10223,10628,10628,10630,10630,10632,10632,10634,10634,10636,10636,10638,10638,10640,10640,10642,10642,10644,10644,10646,
10646,10648,10648,10713,10713,10715,10715,10749,10749,11811,11811,11813,11813,11815,11815,11817,11817,12297,12297,12299,12299,12301,12301,12303,12303,
12305,12305,12309,12309,12311,12311,12313,12313,12315,12315,12318,12319,64830,64830,65048,65048,65078,65078,65080,65080,65082,65082,65084,65084,65086,
65086,65088,65088,65090,65090,65092,65092,65096,65096,65114,65114,65116,65116,65118,65118,65289,65289,65341,65341,65373,65373,65376,65376,65379,65379},
new int[]{171,171,8216,8216,8219,8220,8223,8223,8249,8249,11778,11778,11780,11780,11785,11785,11788,11788,11804,11804,11808,11808},new int[]{187,187,8217,
8217,8221,8221,8250,8250,11779,11779,11781,11781,11786,11786,11789,11789,11805,11805,11809,11809},new int[]{33,35,37,39,42,42,44,44,46,47,58,59,63,64,
92,92,161,161,183,183,191,191,894,894,903,903,1370,1375,1417,1417,1472,1472,1475,1475,1478,1478,1523,1524,1545,1546,1548,1549,1563,1563,1566,1567,1642,
1645,1748,1748,1792,1805,2039,2041,2096,2110,2142,2142,2404,2405,2416,2416,2800,2800,3572,3572,3663,3663,3674,3675,3844,3858,3860,3860,3973,3973,4048,
4052,4057,4058,4170,4175,4347,4347,4960,4968,5741,5742,5867,5869,5941,5942,6100,6102,6104,6106,6144,6149,6151,6154,6468,6469,6686,6687,6816,6822,6824,
6829,7002,7008,7164,7167,7227,7231,7294,7295,7360,7367,7379,7379,8214,8215,8224,8231,8240,8248,8251,8254,8257,8259,8263,8273,8275,8275,8277,8286,11513,
11516,11518,11519,11632,11632,11776,11777,11782,11784,11787,11787,11790,11798,11800,11801,11803,11803,11806,11807,11818,11822,11824,11833,11836,11839,
11841,11841,12289,12291,12349,12349,12539,12539,42238,42239,42509,42511,42611,42611,42622,42622,42738,42743,43124,43127,43214,43215,43256,43258,43260,
43260,43310,43311,43359,43359,43457,43469,43486,43487,43612,43615,43742,43743,43760,43761,44011,44011,65040,65046,65049,65049,65072,65072,65093,65094,
65097,65100,65104,65106,65108,65111,65119,65121,65128,65128,65130,65131,65281,65283,65285,65287,65290,65290,65292,65292,65294,65295,65306,65307,65311,
65312,65340,65340,65377,65377,65380,65381,65792,65794,66463,66463,66512,66512,66927,66927,67671,67671,67871,67871,67903,67903,68176,68184,68223,68223,
68336,68342,68409,68415,68505,68508,69703,69709,69819,69820,69822,69825,69952,69955,70004,70005,70085,70089,70093,70093,70107,70107,70109,70111,70200,
70205,70313,70313,70854,70854,71105,71127,71233,71235,71484,71486,74864,74868,92782,92783,92917,92917,92983,92987,92996,92996,113823,113823,121479,121483},
new int[]{43,43,60,62,124,124,126,126,172,172,177,177,215,215,247,247,1014,1014,1542,1544,8260,8260,8274,8274,8314,8316,8330,8332,8472,8472,8512,8516,
8523,8523,8592,8596,8602,8603,8608,8608,8611,8611,8614,8614,8622,8622,8654,8655,8658,8658,8660,8660,8692,8959,8992,8993,9084,9084,9115,9139,9180,9185,
9655,9655,9665,9665,9720,9727,9839,9839,10176,10180,10183,10213,10224,10239,10496,10626,10649,10711,10716,10747,10750,11007,11056,11076,11079,11084,64297,
64297,65122,65122,65124,65126,65291,65291,65308,65310,65372,65372,65374,65374,65506,65506,65513,65516,120513,120513,120539,120539,120571,120571,120597,
120597,120629,120629,120655,120655,120687,120687,120713,120713,120745,120745,120771,120771,126704,126705},new int[]{36,36,162,165,1423,1423,1547,1547,
2546,2547,2555,2555,2801,2801,3065,3065,3647,3647,6107,6107,8352,8382,43064,43064,65020,65020,65129,65129,65284,65284,65504,65505,65509,65510},new int[]
{94,94,96,96,168,168,175,175,180,180,184,184,706,709,722,735,741,747,749,749,751,767,885,885,900,901,8125,8125,8127,8129,8141,8143,8157,8159,8173,8175,
8189,8190,12443,12444,42752,42774,42784,42785,42889,42890,43867,43867,64434,64449,65342,65342,65344,65344,65507,65507,127995,127999},new int[]{166,167,
169,169,174,174,176,176,182,182,1154,1154,1421,1422,1550,1551,1758,1758,1769,1769,1789,1790,2038,2038,2554,2554,2928,2928,3059,3064,3066,3066,3199,3199,
3449,3449,3841,3843,3859,3859,3861,3863,3866,3871,3892,3892,3894,3894,3896,3896,4030,4037,4039,4044,4046,4047,4053,4056,4254,4255,5008,5017,6464,6464,
6622,6655,7009,7018,7028,7036,8448,8449,8451,8454,8456,8457,8468,8468,8470,8471,8478,8483,8485,8485,8487,8487,8489,8489,8494,8494,8506,8507,8522,8522,
8524,8525,8527,8527,8586,8587,8597,8601,8604,8607,8609,8610,8612,8613,8615,8621,8623,8653,8656,8657,8659,8659,8661,8691,8960,8967,8972,8991,8994,9000,
9003,9083,9085,9114,9140,9179,9186,9210,9216,9254,9280,9290,9372,9449,9472,9654,9656,9664,9666,9719,9728,9838,9840,10087,10132,10175,10240,10495,11008,
11055,11077,11078,11085,11123,11126,11157,11160,11193,11197,11208,11210,11217,11244,11247,11493,11498,11904,11929,11931,12019,12032,12245,12272,12283,
12292,12292,12306,12307,12320,12320,12342,12343,12350,12351,12688,12689,12694,12703,12736,12771,12800,12830,12842,12871,12880,12880,12896,12927,12938,
12976,12992,13054,13056,13311,19904,19967,42128,42182,43048,43051,43062,43063,43065,43065,43639,43641,65021,65021,65508,65508,65512,65512,65517,65518,
65532,65533,65847,65855,65913,65929,65932,65932,65936,65947,65952,65952,66000,66044,67703,67704,68296,68296,71487,71487,92988,92991,92997,92997,113820,
113820,118784,119029,119040,119078,119081,119140,119146,119148,119171,119172,119180,119209,119214,119272,119296,119361,119365,119365,119552,119638,120832,
121343,121399,121402,121453,121460,121462,121475,121477,121478,126976,127019,127024,127123,127136,127150,127153,127167,127169,127183,127185,127221,127248,
127278,127280,127339,127344,127386,127462,127490,127504,127546,127552,127560,127568,127569,127744,127994,128000,128377,128379,128419,128421,128720,128736,
128748,128752,128755,128768,128883,128896,128980,129024,129035,129040,129095,129104,129113,129120,129159,129168,129197,129296,129304,129408,129412,129472,
129472},new int[]{888,889,896,899,907,907,909,909,930,930,1328,1328,1367,1368,1376,1376,1416,1416,1419,1420,1424,1424,1480,1487,1515,1519,1525,1535,1565,
1565,1806,1806,1867,1868,1970,1983,2043,2047,2094,2095,2111,2111,2140,2141,2143,2207,2229,2274,2436,2436,2445,2446,2449,2450,2473,2473,2481,2481,2483,
2485,2490,2491,2501,2502,2505,2506,2511,2518,2520,2523,2526,2526,2532,2533,2556,2560,2564,2564,2571,2574,2577,2578,2601,2601,2609,2609,2612,2612,2615,
2615,2618,2619,2621,2621,2627,2630,2633,2634,2638,2640,2642,2648,2653,2653,2655,2661,2678,2688,2692,2692,2702,2702,2706,2706,2729,2729,2737,2737,2740,
2740,2746,2747,2758,2758,2762,2762,2766,2767,2769,2783,2788,2789,2802,2808,2810,2816,2820,2820,2829,2830,2833,2834,2857,2857,2865,2865,2868,2868,2874,
2875,2885,2886,2889,2890,2894,2901,2904,2907,2910,2910,2916,2917,2936,2945,2948,2948,2955,2957,2961,2961,2966,2968,2971,2971,2973,2973,2976,2978,2981,
2983,2987,2989,3002,3005,3011,3013,3017,3017,3022,3023,3025,3030,3032,3045,3067,3071,3076,3076,3085,3085,3089,3089,3113,3113,3130,3132,3141,3141,3145,
3145,3150,3156,3159,3159,3163,3167,3172,3173,3184,3191,3200,3200,3204,3204,3213,3213,3217,3217,3241,3241,3252,3252,3258,3259,3269,3269,3273,3273,3278,
3284,3287,3293,3295,3295,3300,3301,3312,3312,3315,3328,3332,3332,3341,3341,3345,3345,3387,3388,3397,3397,3401,3401,3407,3414,3416,3422,3428,3429,3446,
3448,3456,3457,3460,3460,3479,3481,3506,3506,3516,3516,3518,3519,3527,3529,3531,3534,3541,3541,3543,3543,3552,3557,3568,3569,3573,3584,3643,3646,3676,
3712,3715,3715,3717,3718,3721,3721,3723,3724,3726,3731,3736,3736,3744,3744,3748,3748,3750,3750,3752,3753,3756,3756,3770,3770,3774,3775,3781,3781,3783,
3783,3790,3791,3802,3803,3808,3839,3912,3912,3949,3952,3992,3992,4029,4029,4045,4045,4059,4095,4294,4294,4296,4300,4302,4303,4681,4681,4686,4687,4695,
4695,4697,4697,4702,4703,4745,4745,4750,4751,4785,4785,4790,4791,4799,4799,4801,4801,4806,4807,4823,4823,4881,4881,4886,4887,4955,4956,4989,4991,5018,
5023,5110,5111,5118,5119,5789,5791,5881,5887,5901,5901,5909,5919,5943,5951,5972,5983,5997,5997,6001,6001,6004,6015,6110,6111,6122,6127,6138,6143,6159,
6159,6170,6175,6264,6271,6315,6319,6390,6399,6431,6431,6444,6447,6460,6463,6465,6467,6510,6511,6517,6527,6572,6575,6602,6607,6619,6621,6684,6685,6751,
6751,6781,6782,6794,6799,6810,6815,6830,6831,6847,6911,6988,6991,7037,7039,7156,7163,7224,7226,7242,7244,7296,7359,7368,7375,7415,7415,7418,7423,7670,
7675,7958,7959,7966,7967,8006,8007,8014,8015,8024,8024,8026,8026,8028,8028,8030,8030,8062,8063,8117,8117,8133,8133,8148,8149,8156,8156,8176,8177,8181,
8181,8191,8191,8293,8293,8306,8307,8335,8335,8349,8351,8383,8399,8433,8447,8588,8591,9211,9215,9255,9279,9291,9311,11124,11125,11158,11159,11194,11196,
11209,11209,11218,11243,11248,11263,11311,11311,11359,11359,11508,11512,11558,11558,11560,11564,11566,11567,11624,11630,11633,11646,11671,11679,11687,
11687,11695,11695,11703,11703,11711,11711,11719,11719,11727,11727,11735,11735,11743,11743,11843,11903,11930,11930,12020,12031,12246,12271,12284,12287,
12352,12352,12439,12440,12544,12548,12590,12592,12687,12687,12731,12735,12772,12783,12831,12831,13055,13055,19894,19903,40918,40959,42125,42127,42183,
42191,42540,42559,42744,42751,42926,42927,42936,42998,43052,43055,43066,43071,43128,43135,43205,43213,43226,43231,43262,43263,43348,43358,43389,43391,
43470,43470,43482,43485,43519,43519,43575,43583,43598,43599,43610,43611,43715,43738,43767,43776,43783,43784,43791,43792,43799,43807,43815,43815,43823,
43823,43878,43887,44014,44015,44026,44031,55204,55215,55239,55242,55292,55295,64110,64111,64218,64255,64263,64274,64280,64284,64311,64311,64317,64317,
64319,64319,64322,64322,64325,64325,64450,64466,64832,64847,64912,64913,64968,65007,65022,65023,65050,65055,65107,65107,65127,65127,65132,65135,65141,
65141,65277,65278,65280,65280,65471,65473,65480,65481,65488,65489,65496,65497,65501,65503,65511,65511,65519,65528,65534,65535,65548,65548,65575,65575,
65595,65595,65598,65598,65614,65615,65630,65663,65787,65791,65795,65798,65844,65846,65933,65935,65948,65951,65953,65999,66046,66175,66205,66207,66257,
66271,66300,66303,66340,66351,66379,66383,66427,66431,66462,66462,66500,66503,66518,66559,66718,66719,66730,66815,66856,66863,66916,66926,66928,67071,
67383,67391,67414,67423,67432,67583,67590,67591,67593,67593,67638,67638,67641,67643,67645,67646,67670,67670,67743,67750,67760,67807,67827,67827,67830,
67834,67868,67870,67898,67902,67904,67967,68024,68027,68048,68049,68100,68100,68103,68107,68116,68116,68120,68120,68148,68151,68155,68158,68168,68175,
68185,68191,68256,68287,68327,68330,68343,68351,68406,68408,68438,68439,68467,68471,68498,68504,68509,68520,68528,68607,68681,68735,68787,68799,68851,
68857,68864,69215,69247,69631,69710,69713,69744,69758,69826,69839,69865,69871,69882,69887,69941,69941,69956,69967,70007,70015,70094,70095,70112,70112,
70133,70143,70162,70162,70206,70271,70279,70279,70281,70281,70286,70286,70302,70302,70314,70319,70379,70383,70394,70399,70404,70404,70413,70414,70417,
70418,70441,70441,70449,70449,70452,70452,70458,70459,70469,70470,70473,70474,70478,70479,70481,70486,70488,70492,70500,70501,70509,70511,70517,70783,
70856,70863,70874,71039,71094,71095,71134,71167,71237,71247,71258,71295,71352,71359,71370,71423,71450,71452,71468,71471,71488,71839,71923,71934,71936,
72383,72441,73727,74650,74751,74863,74863,74869,74879,75076,77823,78895,82943,83527,92159,92729,92735,92767,92767,92778,92781,92784,92879,92910,92911,
92918,92927,92998,93007,93018,93018,93026,93026,93048,93052,93072,93951,94021,94031,94079,94094,94112,110591,110594,113663,113771,113775,113789,113791,
113801,113807,113818,113819,113828,118783,119030,119039,119079,119080,119273,119295,119366,119551,119639,119647,119666,119807,119893,119893,119965,119965,
119968,119969,119971,119972,119975,119976,119981,119981,119994,119994,119996,119996,120004,120004,120070,120070,120075,120076,120085,120085,120093,120093,
120122,120122,120127,120127,120133,120133,120135,120137,120145,120145,120486,120487,120780,120781,121484,121498,121504,121504,121520,124927,125125,125126,
125143,126463,126468,126468,126496,126496,126499,126499,126501,126502,126504,126504,126515,126515,126520,126520,126522,126522,126524,126529,126531,126534,
126536,126536,126538,126538,126540,126540,126544,126544,126547,126547,126549,126550,126552,126552,126554,126554,126556,126556,126558,126558,126560,126560,
126563,126563,126565,126566,126571,126571,126579,126579,126584,126584,126589,126589,126591,126591,126602,126602,126620,126624,126628,126628,126634,126634,
126652,126703,126706,126975,127020,127023,127124,127135,127151,127152,127168,127168,127184,127184,127222,127231,127245,127247,127279,127279,127340,127343,
127387,127461,127491,127503,127547,127551,127561,127567,127570,127743,128378,128378,128420,128420,128721,128735,128749,128751,128756,128767,128884,128895,
128981,129023,129036,129039,129096,129103,129114,129119,129160,129167,129198,129295,129305,129407,129413,129471,129473,131071,173783,173823,177973,177983,
178206,178207,183970,194559,195102,917504,917506,917535,917632,917759,918000,983039,1048574,1048575,1114110,1114111}};public static int[][]NotUnicodeCategories
=new int[][]{new int[]{0,64,91,191,215,215,223,255,257,257,259,259,261,261,263,263,265,265,267,267,269,269,271,271,273,273,275,275,277,277,279,279,281,
281,283,283,285,285,287,287,289,289,291,291,293,293,295,295,297,297,299,299,301,301,303,303,305,305,307,307,309,309,311,312,314,314,316,316,318,318,320,
320,322,322,324,324,326,326,328,329,331,331,333,333,335,335,337,337,339,339,341,341,343,343,345,345,347,347,349,349,351,351,353,353,355,355,357,357,359,
359,361,361,363,363,365,365,367,367,369,369,371,371,373,373,375,375,378,378,380,380,382,384,387,387,389,389,392,392,396,397,402,402,405,405,409,411,414,
414,417,417,419,419,421,421,424,424,426,427,429,429,432,432,436,436,438,438,441,443,445,451,453,454,456,457,459,460,462,462,464,464,466,466,468,468,470,
470,472,472,474,474,476,477,479,479,481,481,483,483,485,485,487,487,489,489,491,491,493,493,495,496,498,499,501,501,505,505,507,507,509,509,511,511,513,
513,515,515,517,517,519,519,521,521,523,523,525,525,527,527,529,529,531,531,533,533,535,535,537,537,539,539,541,541,543,543,545,545,547,547,549,549,551,
551,553,553,555,555,557,557,559,559,561,561,563,569,572,572,575,576,578,578,583,583,585,585,587,587,589,589,591,879,881,881,883,885,887,894,896,901,903,
903,907,907,909,909,912,912,930,930,940,974,976,977,981,983,985,985,987,987,989,989,991,991,993,993,995,995,997,997,999,999,1001,1001,1003,1003,1005,1005,
1007,1011,1013,1014,1016,1016,1019,1020,1072,1119,1121,1121,1123,1123,1125,1125,1127,1127,1129,1129,1131,1131,1133,1133,1135,1135,1137,1137,1139,1139,
1141,1141,1143,1143,1145,1145,1147,1147,1149,1149,1151,1151,1153,1161,1163,1163,1165,1165,1167,1167,1169,1169,1171,1171,1173,1173,1175,1175,1177,1177,
1179,1179,1181,1181,1183,1183,1185,1185,1187,1187,1189,1189,1191,1191,1193,1193,1195,1195,1197,1197,1199,1199,1201,1201,1203,1203,1205,1205,1207,1207,
1209,1209,1211,1211,1213,1213,1215,1215,1218,1218,1220,1220,1222,1222,1224,1224,1226,1226,1228,1228,1230,1231,1233,1233,1235,1235,1237,1237,1239,1239,
1241,1241,1243,1243,1245,1245,1247,1247,1249,1249,1251,1251,1253,1253,1255,1255,1257,1257,1259,1259,1261,1261,1263,1263,1265,1265,1267,1267,1269,1269,
1271,1271,1273,1273,1275,1275,1277,1277,1279,1279,1281,1281,1283,1283,1285,1285,1287,1287,1289,1289,1291,1291,1293,1293,1295,1295,1297,1297,1299,1299,
1301,1301,1303,1303,1305,1305,1307,1307,1309,1309,1311,1311,1313,1313,1315,1315,1317,1317,1319,1319,1321,1321,1323,1323,1325,1325,1327,1328,1367,4255,
4294,4294,4296,4300,4302,5023,5110,7679,7681,7681,7683,7683,7685,7685,7687,7687,7689,7689,7691,7691,7693,7693,7695,7695,7697,7697,7699,7699,7701,7701,
7703,7703,7705,7705,7707,7707,7709,7709,7711,7711,7713,7713,7715,7715,7717,7717,7719,7719,7721,7721,7723,7723,7725,7725,7727,7727,7729,7729,7731,7731,
7733,7733,7735,7735,7737,7737,7739,7739,7741,7741,7743,7743,7745,7745,7747,7747,7749,7749,7751,7751,7753,7753,7755,7755,7757,7757,7759,7759,7761,7761,
7763,7763,7765,7765,7767,7767,7769,7769,7771,7771,7773,7773,7775,7775,7777,7777,7779,7779,7781,7781,7783,7783,7785,7785,7787,7787,7789,7789,7791,7791,
7793,7793,7795,7795,7797,7797,7799,7799,7801,7801,7803,7803,7805,7805,7807,7807,7809,7809,7811,7811,7813,7813,7815,7815,7817,7817,7819,7819,7821,7821,
7823,7823,7825,7825,7827,7827,7829,7837,7839,7839,7841,7841,7843,7843,7845,7845,7847,7847,7849,7849,7851,7851,7853,7853,7855,7855,7857,7857,7859,7859,
7861,7861,7863,7863,7865,7865,7867,7867,7869,7869,7871,7871,7873,7873,7875,7875,7877,7877,7879,7879,7881,7881,7883,7883,7885,7885,7887,7887,7889,7889,
7891,7891,7893,7893,7895,7895,7897,7897,7899,7899,7901,7901,7903,7903,7905,7905,7907,7907,7909,7909,7911,7911,7913,7913,7915,7915,7917,7917,7919,7919,
7921,7921,7923,7923,7925,7925,7927,7927,7929,7929,7931,7931,7933,7933,7935,7943,7952,7959,7966,7975,7984,7991,8000,8007,8014,8024,8026,8026,8028,8028,
8030,8030,8032,8039,8048,8119,8124,8135,8140,8151,8156,8167,8173,8183,8188,8449,8451,8454,8456,8458,8462,8463,8467,8468,8470,8472,8478,8483,8485,8485,
8487,8487,8489,8489,8494,8495,8500,8509,8512,8516,8518,8578,8580,11263,11311,11359,11361,11361,11365,11366,11368,11368,11370,11370,11372,11372,11377,11377,
11379,11380,11382,11389,11393,11393,11395,11395,11397,11397,11399,11399,11401,11401,11403,11403,11405,11405,11407,11407,11409,11409,11411,11411,11413,
11413,11415,11415,11417,11417,11419,11419,11421,11421,11423,11423,11425,11425,11427,11427,11429,11429,11431,11431,11433,11433,11435,11435,11437,11437,
11439,11439,11441,11441,11443,11443,11445,11445,11447,11447,11449,11449,11451,11451,11453,11453,11455,11455,11457,11457,11459,11459,11461,11461,11463,
11463,11465,11465,11467,11467,11469,11469,11471,11471,11473,11473,11475,11475,11477,11477,11479,11479,11481,11481,11483,11483,11485,11485,11487,11487,
11489,11489,11491,11498,11500,11500,11502,11505,11507,42559,42561,42561,42563,42563,42565,42565,42567,42567,42569,42569,42571,42571,42573,42573,42575,
42575,42577,42577,42579,42579,42581,42581,42583,42583,42585,42585,42587,42587,42589,42589,42591,42591,42593,42593,42595,42595,42597,42597,42599,42599,
42601,42601,42603,42603,42605,42623,42625,42625,42627,42627,42629,42629,42631,42631,42633,42633,42635,42635,42637,42637,42639,42639,42641,42641,42643,
42643,42645,42645,42647,42647,42649,42649,42651,42785,42787,42787,42789,42789,42791,42791,42793,42793,42795,42795,42797,42797,42799,42801,42803,42803,
42805,42805,42807,42807,42809,42809,42811,42811,42813,42813,42815,42815,42817,42817,42819,42819,42821,42821,42823,42823,42825,42825,42827,42827,42829,
42829,42831,42831,42833,42833,42835,42835,42837,42837,42839,42839,42841,42841,42843,42843,42845,42845,42847,42847,42849,42849,42851,42851,42853,42853,
42855,42855,42857,42857,42859,42859,42861,42861,42863,42872,42874,42874,42876,42876,42879,42879,42881,42881,42883,42883,42885,42885,42887,42890,42892,
42892,42894,42895,42897,42897,42899,42901,42903,42903,42905,42905,42907,42907,42909,42909,42911,42911,42913,42913,42915,42915,42917,42917,42919,42919,
42921,42921,42926,42927,42933,42933,42935,55295,57344,65312,65339,66559,66600,68735,68787,71839,71872,119807,119834,119859,119886,119911,119938,119963,
119965,119965,119968,119969,119971,119972,119975,119976,119981,119981,119990,120015,120042,120067,120070,120070,120075,120076,120085,120085,120093,120119,
120122,120122,120127,120127,120133,120133,120135,120137,120145,120171,120198,120223,120250,120275,120302,120327,120354,120379,120406,120431,120458,120487,
120513,120545,120571,120603,120629,120661,120687,120719,120745,120777,120779,1114111},new int[]{0,96,123,169,171,180,182,185,187,222,247,247,256,256,258,
258,260,260,262,262,264,264,266,266,268,268,270,270,272,272,274,274,276,276,278,278,280,280,282,282,284,284,286,286,288,288,290,290,292,292,294,294,296,
296,298,298,300,300,302,302,304,304,306,306,308,308,310,310,313,313,315,315,317,317,319,319,321,321,323,323,325,325,327,327,330,330,332,332,334,334,336,
336,338,338,340,340,342,342,344,344,346,346,348,348,350,350,352,352,354,354,356,356,358,358,360,360,362,362,364,364,366,366,368,368,370,370,372,372,374,
374,376,377,379,379,381,381,385,386,388,388,390,391,393,395,398,401,403,404,406,408,412,413,415,416,418,418,420,420,422,423,425,425,428,428,430,431,433,
435,437,437,439,440,443,444,448,453,455,456,458,459,461,461,463,463,465,465,467,467,469,469,471,471,473,473,475,475,478,478,480,480,482,482,484,484,486,
486,488,488,490,490,492,492,494,494,497,498,500,500,502,504,506,506,508,508,510,510,512,512,514,514,516,516,518,518,520,520,522,522,524,524,526,526,528,
528,530,530,532,532,534,534,536,536,538,538,540,540,542,542,544,544,546,546,548,548,550,550,552,552,554,554,556,556,558,558,560,560,562,562,570,571,573,
574,577,577,579,582,584,584,586,586,588,588,590,590,660,660,688,880,882,882,884,886,888,890,894,911,913,939,975,975,978,980,984,984,986,986,988,988,990,
990,992,992,994,994,996,996,998,998,1000,1000,1002,1002,1004,1004,1006,1006,1012,1012,1014,1015,1017,1018,1021,1071,1120,1120,1122,1122,1124,1124,1126,
1126,1128,1128,1130,1130,1132,1132,1134,1134,1136,1136,1138,1138,1140,1140,1142,1142,1144,1144,1146,1146,1148,1148,1150,1150,1152,1152,1154,1162,1164,
1164,1166,1166,1168,1168,1170,1170,1172,1172,1174,1174,1176,1176,1178,1178,1180,1180,1182,1182,1184,1184,1186,1186,1188,1188,1190,1190,1192,1192,1194,
1194,1196,1196,1198,1198,1200,1200,1202,1202,1204,1204,1206,1206,1208,1208,1210,1210,1212,1212,1214,1214,1216,1217,1219,1219,1221,1221,1223,1223,1225,
1225,1227,1227,1229,1229,1232,1232,1234,1234,1236,1236,1238,1238,1240,1240,1242,1242,1244,1244,1246,1246,1248,1248,1250,1250,1252,1252,1254,1254,1256,
1256,1258,1258,1260,1260,1262,1262,1264,1264,1266,1266,1268,1268,1270,1270,1272,1272,1274,1274,1276,1276,1278,1278,1280,1280,1282,1282,1284,1284,1286,
1286,1288,1288,1290,1290,1292,1292,1294,1294,1296,1296,1298,1298,1300,1300,1302,1302,1304,1304,1306,1306,1308,1308,1310,1310,1312,1312,1314,1314,1316,
1316,1318,1318,1320,1320,1322,1322,1324,1324,1326,1326,1328,1376,1416,5111,5118,7423,7468,7530,7544,7544,7579,7680,7682,7682,7684,7684,7686,7686,7688,
7688,7690,7690,7692,7692,7694,7694,7696,7696,7698,7698,7700,7700,7702,7702,7704,7704,7706,7706,7708,7708,7710,7710,7712,7712,7714,7714,7716,7716,7718,
7718,7720,7720,7722,7722,7724,7724,7726,7726,7728,7728,7730,7730,7732,7732,7734,7734,7736,7736,7738,7738,7740,7740,7742,7742,7744,7744,7746,7746,7748,
7748,7750,7750,7752,7752,7754,7754,7756,7756,7758,7758,7760,7760,7762,7762,7764,7764,7766,7766,7768,7768,7770,7770,7772,7772,7774,7774,7776,7776,7778,
7778,7780,7780,7782,7782,7784,7784,7786,7786,7788,7788,7790,7790,7792,7792,7794,7794,7796,7796,7798,7798,7800,7800,7802,7802,7804,7804,7806,7806,7808,
7808,7810,7810,7812,7812,7814,7814,7816,7816,7818,7818,7820,7820,7822,7822,7824,7824,7826,7826,7828,7828,7838,7838,7840,7840,7842,7842,7844,7844,7846,
7846,7848,7848,7850,7850,7852,7852,7854,7854,7856,7856,7858,7858,7860,7860,7862,7862,7864,7864,7866,7866,7868,7868,7870,7870,7872,7872,7874,7874,7876,
7876,7878,7878,7880,7880,7882,7882,7884,7884,7886,7886,7888,7888,7890,7890,7892,7892,7894,7894,7896,7896,7898,7898,7900,7900,7902,7902,7904,7904,7906,
7906,7908,7908,7910,7910,7912,7912,7914,7914,7916,7916,7918,7918,7920,7920,7922,7922,7924,7924,7926,7926,7928,7928,7930,7930,7932,7932,7934,7934,7944,
7951,7958,7967,7976,7983,7992,7999,8006,8015,8024,8031,8040,8047,8062,8063,8072,8079,8088,8095,8104,8111,8117,8117,8120,8125,8127,8129,8133,8133,8136,
8143,8148,8149,8152,8159,8168,8177,8181,8181,8184,8457,8459,8461,8464,8466,8468,8494,8496,8499,8501,8504,8506,8507,8510,8517,8522,8525,8527,8579,8581,
11311,11359,11360,11362,11364,11367,11367,11369,11369,11371,11371,11373,11376,11378,11378,11381,11381,11388,11392,11394,11394,11396,11396,11398,11398,
11400,11400,11402,11402,11404,11404,11406,11406,11408,11408,11410,11410,11412,11412,11414,11414,11416,11416,11418,11418,11420,11420,11422,11422,11424,
11424,11426,11426,11428,11428,11430,11430,11432,11432,11434,11434,11436,11436,11438,11438,11440,11440,11442,11442,11444,11444,11446,11446,11448,11448,
11450,11450,11452,11452,11454,11454,11456,11456,11458,11458,11460,11460,11462,11462,11464,11464,11466,11466,11468,11468,11470,11470,11472,11472,11474,
11474,11476,11476,11478,11478,11480,11480,11482,11482,11484,11484,11486,11486,11488,11488,11490,11490,11493,11499,11501,11501,11503,11506,11508,11519,
11558,11558,11560,11564,11566,42560,42562,42562,42564,42564,42566,42566,42568,42568,42570,42570,42572,42572,42574,42574,42576,42576,42578,42578,42580,
42580,42582,42582,42584,42584,42586,42586,42588,42588,42590,42590,42592,42592,42594,42594,42596,42596,42598,42598,42600,42600,42602,42602,42604,42604,
42606,42624,42626,42626,42628,42628,42630,42630,42632,42632,42634,42634,42636,42636,42638,42638,42640,42640,42642,42642,42644,42644,42646,42646,42648,
42648,42650,42650,42652,42786,42788,42788,42790,42790,42792,42792,42794,42794,42796,42796,42798,42798,42802,42802,42804,42804,42806,42806,42808,42808,
42810,42810,42812,42812,42814,42814,42816,42816,42818,42818,42820,42820,42822,42822,42824,42824,42826,42826,42828,42828,42830,42830,42832,42832,42834,
42834,42836,42836,42838,42838,42840,42840,42842,42842,42844,42844,42846,42846,42848,42848,42850,42850,42852,42852,42854,42854,42856,42856,42858,42858,
42860,42860,42862,42862,42864,42864,42873,42873,42875,42875,42877,42878,42880,42880,42882,42882,42884,42884,42886,42886,42888,42891,42893,42893,42895,
42896,42898,42898,42902,42902,42904,42904,42906,42906,42908,42908,42910,42910,42912,42912,42914,42914,42916,42916,42918,42918,42920,42920,42922,42932,
42934,42934,42936,43001,43003,43823,43867,43871,43878,43887,43968,55295,57344,64255,64263,64274,64280,65344,65371,66599,66640,68799,68851,71871,71904,
119833,119860,119885,119893,119893,119912,119937,119964,119989,119994,119994,119996,119996,120004,120004,120016,120041,120068,120093,120120,120145,120172,
120197,120224,120249,120276,120301,120328,120353,120380,120405,120432,120457,120486,120513,120539,120539,120546,120571,120597,120597,120604,120629,120655,
120655,120662,120687,120713,120713,120720,120745,120771,120771,120778,120778,120780,1114111},new int[]{0,452,454,455,457,458,460,497,499,8071,8080,8087,
8096,8103,8112,8123,8125,8139,8141,8187,8189,55295,57344,1114111},new int[]{0,687,706,709,722,735,741,747,749,749,751,883,885,889,891,1368,1370,1599,1601,
1764,1767,2035,2038,2041,2043,2073,2075,2083,2085,2087,2089,2416,2418,3653,3655,3781,3783,4347,4349,6102,6104,6210,6212,6822,6824,7287,7294,7467,7531,
7543,7545,7578,7616,8304,8306,8318,8320,8335,8349,11387,11390,11630,11632,11822,11824,12292,12294,12336,12342,12346,12348,12444,12447,12539,12543,40980,
40982,42231,42238,42507,42509,42622,42624,42651,42654,42774,42784,42863,42865,42887,42889,42999,43002,43470,43472,43493,43495,43631,43633,43740,43742,
43762,43765,43867,43872,55295,57344,65391,65393,65437,65440,92991,92996,94098,94112,1114111},new int[]{0,442,444,447,452,659,661,1487,1515,1519,1523,1567,
1600,1600,1611,1645,1648,1648,1748,1748,1750,1773,1776,1785,1789,1790,1792,1807,1809,1809,1840,1868,1958,1968,1970,1993,2027,2047,2070,2111,2137,2207,
2229,2307,2362,2364,2366,2383,2385,2391,2402,2417,2433,2436,2445,2446,2449,2450,2473,2473,2481,2481,2483,2485,2490,2492,2494,2509,2511,2523,2526,2526,
2530,2543,2546,2564,2571,2574,2577,2578,2601,2601,2609,2609,2612,2612,2615,2615,2618,2648,2653,2653,2655,2673,2677,2692,2702,2702,2706,2706,2729,2729,
2737,2737,2740,2740,2746,2748,2750,2767,2769,2783,2786,2808,2810,2820,2829,2830,2833,2834,2857,2857,2865,2865,2868,2868,2874,2876,2878,2907,2910,2910,
2914,2928,2930,2946,2948,2948,2955,2957,2961,2961,2966,2968,2971,2971,2973,2973,2976,2978,2981,2983,2987,2989,3002,3023,3025,3076,3085,3085,3089,3089,
3113,3113,3130,3132,3134,3159,3163,3167,3170,3204,3213,3213,3217,3217,3241,3241,3252,3252,3258,3260,3262,3293,3295,3295,3298,3312,3315,3332,3341,3341,
3345,3345,3387,3388,3390,3405,3407,3422,3426,3449,3456,3460,3479,3481,3506,3506,3516,3516,3518,3519,3527,3584,3633,3633,3636,3647,3654,3712,3715,3715,
3717,3718,3721,3721,3723,3724,3726,3731,3736,3736,3744,3744,3748,3748,3750,3750,3752,3753,3756,3756,3761,3761,3764,3772,3774,3775,3781,3803,3808,3839,
3841,3903,3912,3912,3949,3975,3981,4095,4139,4158,4160,4175,4182,4185,4190,4192,4194,4196,4199,4205,4209,4212,4226,4237,4239,4303,4347,4348,4681,4681,
4686,4687,4695,4695,4697,4697,4702,4703,4745,4745,4750,4751,4785,4785,4790,4791,4799,4799,4801,4801,4806,4807,4823,4823,4881,4881,4886,4887,4955,4991,
5008,5120,5741,5742,5760,5760,5787,5791,5867,5872,5881,5887,5901,5901,5906,5919,5938,5951,5970,5983,5997,5997,6001,6015,6068,6107,6109,6175,6211,6211,
6264,6271,6313,6313,6315,6319,6390,6399,6431,6479,6510,6511,6517,6527,6572,6575,6602,6655,6679,6687,6741,6916,6964,6980,6988,7042,7073,7085,7088,7097,
7142,7167,7204,7244,7248,7257,7288,7400,7405,7405,7410,7412,7415,8500,8505,11567,11624,11647,11671,11679,11687,11687,11695,11695,11703,11703,11711,11711,
11719,11719,11727,11727,11735,11735,11743,12293,12295,12347,12349,12352,12439,12446,12448,12448,12539,12542,12544,12548,12590,12592,12687,12703,12731,
12783,12800,13311,19894,19967,40918,40959,40981,40981,42125,42191,42232,42239,42508,42511,42528,42537,42540,42605,42607,42655,42726,42894,42896,42998,
43000,43002,43010,43010,43014,43014,43019,43019,43043,43071,43124,43137,43188,43249,43256,43258,43260,43260,43262,43273,43302,43311,43335,43359,43389,
43395,43443,43487,43493,43494,43504,43513,43519,43519,43561,43583,43587,43587,43596,43615,43632,43632,43639,43641,43643,43645,43696,43696,43698,43700,
43703,43704,43710,43711,43713,43713,43715,43738,43741,43743,43755,43761,43763,43776,43783,43784,43791,43792,43799,43807,43815,43815,43823,43967,44003,
44031,55204,55215,55239,55242,55292,55295,57344,63743,64110,64111,64218,64284,64286,64286,64297,64297,64311,64311,64317,64317,64319,64319,64322,64322,
64325,64325,64434,64466,64830,64847,64912,64913,64968,65007,65020,65135,65141,65141,65277,65381,65392,65392,65438,65439,65471,65473,65480,65481,65488,
65489,65496,65497,65501,65535,65548,65548,65575,65575,65595,65595,65598,65598,65614,65615,65630,65663,65787,66175,66205,66207,66257,66303,66336,66351,
66369,66369,66378,66383,66422,66431,66462,66463,66500,66503,66512,66639,66718,66815,66856,66863,66916,67071,67383,67391,67414,67423,67432,67583,67590,
67591,67593,67593,67638,67638,67641,67643,67645,67646,67670,67679,67703,67711,67743,67807,67827,67827,67830,67839,67862,67871,67898,67967,68024,68029,
68032,68095,68097,68111,68116,68116,68120,68120,68148,68191,68221,68223,68253,68287,68296,68296,68325,68351,68406,68415,68438,68447,68467,68479,68498,
68607,68681,69634,69688,69762,69808,69839,69865,69890,69927,69967,70003,70005,70007,70018,70067,70080,70085,70105,70107,70107,70109,70143,70162,70162,
70188,70271,70279,70279,70281,70281,70286,70286,70302,70302,70313,70319,70367,70404,70413,70414,70417,70418,70441,70441,70449,70449,70452,70452,70458,
70460,70462,70479,70481,70492,70498,70783,70832,70851,70854,70854,70856,71039,71087,71127,71132,71167,71216,71235,71237,71295,71339,71423,71450,71934,
71936,72383,72441,73727,74650,74879,75076,77823,78895,82943,83527,92159,92729,92735,92767,92879,92910,92927,92976,93026,93048,93052,93072,93951,94021,
94031,94033,110591,110594,113663,113771,113775,113789,113791,113801,113807,113818,124927,125125,126463,126468,126468,126496,126496,126499,126499,126501,
126502,126504,126504,126515,126515,126520,126520,126522,126522,126524,126529,126531,126534,126536,126536,126538,126538,126540,126540,126544,126544,126547,
126547,126549,126550,126552,126552,126554,126554,126556,126556,126558,126558,126560,126560,126563,126563,126565,126566,126571,126571,126579,126579,126584,
126584,126589,126589,126591,126591,126602,126602,126620,126624,126628,126628,126634,126634,126652,131071,173783,173823,177973,177983,178206,178207,183970,
194559,195102,1114111},new int[]{0,767,880,1154,1160,1424,1470,1470,1472,1472,1475,1475,1478,1478,1480,1551,1563,1610,1632,1647,1649,1749,1757,1758,1765,
1766,1769,1769,1774,1808,1810,1839,1867,1957,1969,2026,2036,2069,2074,2074,2084,2084,2088,2088,2094,2136,2140,2274,2307,2361,2363,2363,2365,2368,2377,
2380,2382,2384,2392,2401,2404,2432,2434,2491,2493,2496,2501,2508,2510,2529,2532,2560,2563,2619,2621,2624,2627,2630,2633,2634,2638,2640,2642,2671,2674,
2676,2678,2688,2691,2747,2749,2752,2758,2758,2761,2764,2766,2785,2788,2816,2818,2875,2877,2878,2880,2880,2885,2892,2894,2901,2903,2913,2916,2945,2947,
3007,3009,3020,3022,3071,3073,3133,3137,3141,3145,3145,3150,3156,3159,3169,3172,3200,3202,3259,3261,3262,3264,3269,3271,3275,3278,3297,3300,3328,3330,
3392,3397,3404,3406,3425,3428,3529,3531,3537,3541,3541,3543,3632,3634,3635,3643,3654,3663,3760,3762,3763,3770,3770,3773,3783,3790,3863,3866,3892,3894,
3894,3896,3896,3898,3952,3967,3967,3973,3973,3976,3980,3992,3992,4029,4037,4039,4140,4145,4145,4152,4152,4155,4156,4159,4183,4186,4189,4193,4208,4213,
4225,4227,4228,4231,4236,4238,4252,4254,4956,4960,5905,5909,5937,5941,5969,5972,6001,6004,6067,6070,6070,6078,6085,6087,6088,6100,6108,6110,6154,6158,
6312,6314,6431,6435,6438,6441,6449,6451,6456,6460,6678,6681,6682,6684,6741,6743,6743,6751,6751,6753,6753,6755,6756,6765,6770,6781,6782,6784,6831,6846,
6911,6916,6963,6965,6965,6971,6971,6973,6977,6979,7018,7028,7039,7042,7073,7078,7079,7082,7082,7086,7141,7143,7143,7146,7148,7150,7150,7154,7211,7220,
7221,7224,7375,7379,7379,7393,7393,7401,7404,7406,7411,7413,7415,7418,7615,7670,7675,7680,8399,8413,8416,8418,8420,8433,11502,11506,11646,11648,11743,
11776,12329,12334,12440,12443,42606,42608,42611,42622,42653,42656,42735,42738,43009,43011,43013,43015,43018,43020,43044,43047,43203,43205,43231,43250,
43301,43310,43334,43346,43391,43395,43442,43444,43445,43450,43451,43453,43492,43494,43560,43567,43568,43571,43572,43575,43586,43588,43595,43597,43643,
43645,43695,43697,43697,43701,43702,43705,43709,43712,43712,43714,43755,43758,43765,43767,44004,44006,44007,44009,44012,44014,55295,57344,64285,64287,
65023,65040,65055,65072,66044,66046,66271,66273,66421,66427,68096,68100,68100,68103,68107,68112,68151,68155,68158,68160,68324,68327,69632,69634,69687,
69703,69758,69762,69810,69815,69816,69819,69887,69891,69926,69932,69932,69941,70002,70004,70015,70018,70069,70079,70089,70093,70190,70194,70195,70197,
70197,70200,70366,70368,70370,70379,70399,70402,70459,70461,70463,70465,70501,70509,70511,70517,70834,70841,70841,70843,70846,70849,70849,70852,71089,
71094,71099,71102,71102,71105,71131,71134,71218,71227,71228,71230,71230,71233,71338,71340,71340,71342,71343,71350,71350,71352,71452,71456,71457,71462,
71462,71468,92911,92917,92975,92983,94094,94099,113820,113823,119142,119146,119162,119171,119172,119180,119209,119214,119361,119365,121343,121399,121402,
121453,121460,121462,121475,121477,121498,121504,121504,121520,125135,125143,917759,918000,1114111},new int[]{0,2306,2308,2362,2364,2365,2369,2376,2381,
2381,2384,2433,2436,2493,2497,2502,2505,2506,2509,2518,2520,2562,2564,2621,2625,2690,2692,2749,2753,2760,2762,2762,2765,2817,2820,2877,2879,2879,2881,
2886,2889,2890,2893,2902,2904,3005,3008,3008,3011,3013,3017,3017,3021,3030,3032,3072,3076,3136,3141,3201,3204,3261,3263,3263,3269,3270,3273,3273,3276,
3284,3287,3329,3332,3389,3393,3397,3401,3401,3405,3414,3416,3457,3460,3534,3538,3543,3552,3569,3572,3901,3904,3966,3968,4138,4141,4144,4146,4151,4153,
4154,4157,4181,4184,4193,4197,4198,4206,4226,4229,4230,4237,4238,4240,4249,4253,6069,6071,6077,6086,6086,6089,6434,6439,6440,6444,6447,6450,6450,6457,
6680,6683,6740,6742,6742,6744,6752,6754,6754,6757,6764,6771,6915,6917,6964,6966,6970,6972,6972,6978,6978,6981,7041,7043,7072,7074,7077,7080,7081,7083,
7142,7144,7145,7149,7149,7151,7153,7156,7203,7212,7219,7222,7392,7394,7409,7412,12333,12336,43042,43045,43046,43048,43135,43138,43187,43204,43345,43348,
43394,43396,43443,43446,43449,43452,43452,43457,43566,43569,43570,43573,43596,43598,43642,43644,43644,43646,43754,43756,43757,43760,43764,43766,44002,
44005,44005,44008,44008,44011,44011,44013,55295,57344,69631,69633,69633,69635,69761,69763,69807,69811,69814,69817,69931,69933,70017,70019,70066,70070,
70078,70081,70187,70191,70193,70196,70196,70198,70367,70371,70401,70404,70461,70464,70464,70469,70470,70473,70474,70478,70486,70488,70497,70500,70831,
70835,70840,70842,70842,70847,70848,70850,71086,71090,71095,71100,71101,71103,71215,71219,71226,71229,71229,71231,71339,71341,71341,71344,71349,71351,
71455,71458,71461,71463,94032,94079,119140,119143,119148,119155,1114111},new int[]{0,1159,1162,6845,6847,8412,8417,8417,8421,42607,42611,55295,57344,1114111},
new int[]{0,47,58,1631,1642,1775,1786,1983,1994,2405,2416,2533,2544,2661,2672,2789,2800,2917,2928,3045,3056,3173,3184,3301,3312,3429,3440,3557,3568,3663,
3674,3791,3802,3871,3882,4159,4170,4239,4250,6111,6122,6159,6170,6469,6480,6607,6618,6783,6794,6799,6810,6991,7002,7087,7098,7231,7242,7247,7258,42527,
42538,43215,43226,43263,43274,43471,43482,43503,43514,43599,43610,44015,44026,55295,57344,65295,65306,66719,66730,69733,69744,69871,69882,69941,69952,
70095,70106,70383,70394,70863,70874,71247,71258,71359,71370,71471,71482,71903,71914,92767,92778,93007,93018,120781,120832,1114111},new int[]{0,5869,5873,
8543,8579,8580,8585,12294,12296,12320,12330,12343,12347,42725,42736,55295,57344,65855,65909,66368,66370,66377,66379,66512,66518,74751,74863,1114111},new
 int[]{0,177,180,184,186,187,191,2547,2554,2929,2936,3055,3059,3191,3199,3439,3446,3881,3892,4968,4989,6127,6138,6617,6619,8303,8305,8307,8314,8319,8330,
8527,8544,8584,8586,9311,9372,9449,9472,10101,10132,11516,11518,12689,12694,12831,12842,12871,12880,12880,12896,12927,12938,12976,12992,43055,43062,55295,
57344,65798,65844,65908,65913,65929,65932,66272,66300,66335,66340,67671,67680,67704,67712,67750,67760,67834,67840,67861,67868,68027,68030,68031,68048,
68049,68096,68159,68168,68220,68223,68252,68256,68330,68336,68439,68448,68471,68480,68520,68528,68857,68864,69215,69247,69713,69734,70112,70133,71481,
71484,71913,71923,93018,93026,119647,119666,125126,125136,127231,127245,1114111},new int[]{0,31,33,159,161,5759,5761,8191,8203,8238,8240,8286,8288,12287,
12289,55295,57344,1114111},new int[]{0,8231,8233,55295,57344,1114111},new int[]{0,8232,8234,55295,57344,1114111},new int[]{32,126,160,55295,57344,1114111},
new int[]{0,1535,1542,1563,1565,1756,1758,1806,1808,6157,6159,8202,8208,8233,8239,8287,8293,8293,8304,55295,57344,65278,65280,65528,65532,69820,69822,
113823,113828,119154,119163,917504,917506,917535,917632,1114111},new int[]{0,55295,57344,1114111},new int[]{0,55295,63744,983039,1048574,1048575,1114110,
1114111},new int[]{0,94,96,8254,8257,8275,8277,55295,57344,65074,65077,65100,65104,65342,65344,1114111},new int[]{0,44,46,172,174,1417,1419,1469,1471,
5119,5121,6149,6151,8207,8214,11798,11800,11801,11803,11833,11836,11839,11841,12315,12317,12335,12337,12447,12449,55295,57344,65072,65075,65111,65113,
65122,65124,65292,65294,1114111},new int[]{0,39,41,90,92,122,124,3897,3899,3899,3901,5786,5788,8217,8219,8221,8223,8260,8262,8316,8318,8332,8334,8967,
8969,8969,8971,9000,9002,10087,10089,10089,10091,10091,10093,10093,10095,10095,10097,10097,10099,10099,10101,10180,10182,10213,10215,10215,10217,10217,
10219,10219,10221,10221,10223,10626,10628,10628,10630,10630,10632,10632,10634,10634,10636,10636,10638,10638,10640,10640,10642,10642,10644,10644,10646,
10646,10648,10711,10713,10713,10715,10747,10749,11809,11811,11811,11813,11813,11815,11815,11817,11841,11843,12295,12297,12297,12299,12299,12301,12301,
12303,12303,12305,12307,12309,12309,12311,12311,12313,12313,12315,12316,12318,55295,57344,64830,64832,65046,65048,65076,65078,65078,65080,65080,65082,
65082,65084,65084,65086,65086,65088,65088,65090,65090,65092,65094,65096,65112,65114,65114,65116,65116,65118,65287,65289,65338,65340,65370,65372,65374,
65376,65377,65379,1114111},new int[]{0,40,42,92,94,124,126,3898,3900,3900,3902,5787,5789,8261,8263,8317,8319,8333,8335,8968,8970,8970,8972,9001,9003,10088,
10090,10090,10092,10092,10094,10094,10096,10096,10098,10098,10100,10100,10102,10181,10183,10214,10216,10216,10218,10218,10220,10220,10222,10222,10224,
10627,10629,10629,10631,10631,10633,10633,10635,10635,10637,10637,10639,10639,10641,10641,10643,10643,10645,10645,10647,10647,10649,10712,10714,10714,
10716,10748,10750,11810,11812,11812,11814,11814,11816,11816,11818,12296,12298,12298,12300,12300,12302,12302,12304,12304,12306,12308,12310,12310,12312,
12312,12314,12314,12316,12317,12320,55295,57344,64829,64831,65047,65049,65077,65079,65079,65081,65081,65083,65083,65085,65085,65087,65087,65089,65089,
65091,65091,65093,65095,65097,65113,65115,65115,65117,65117,65119,65288,65290,65340,65342,65372,65374,65375,65377,65378,65380,1114111},new int[]{0,170,
172,8215,8217,8218,8221,8222,8224,8248,8250,11777,11779,11779,11781,11784,11786,11787,11789,11803,11805,11807,11809,55295,57344,1114111},new int[]{0,186,
188,8216,8218,8220,8222,8249,8251,11778,11780,11780,11782,11785,11787,11788,11790,11804,11806,11808,11810,55295,57344,1114111},new int[]{0,32,36,36,40,
41,43,43,45,45,48,57,60,62,65,91,93,160,162,182,184,190,192,893,895,902,904,1369,1376,1416,1418,1471,1473,1474,1476,1477,1479,1522,1525,1544,1547,1547,
1550,1562,1564,1565,1568,1641,1646,1747,1749,1791,1806,2038,2042,2095,2111,2141,2143,2403,2406,2415,2417,2799,2801,3571,3573,3662,3664,3673,3676,3843,
3859,3859,3861,3972,3974,4047,4053,4056,4059,4169,4176,4346,4348,4959,4969,5740,5743,5866,5870,5940,5943,6099,6103,6103,6107,6143,6150,6150,6155,6467,
6470,6685,6688,6815,6823,6823,6830,7001,7009,7163,7168,7226,7232,7293,7296,7359,7368,7378,7380,8213,8216,8223,8232,8239,8249,8250,8255,8256,8260,8262,
8274,8274,8276,8276,8287,11512,11517,11517,11520,11631,11633,11775,11778,11781,11785,11786,11788,11789,11799,11799,11802,11802,11804,11805,11808,11817,
11823,11823,11834,11835,11840,11840,11842,12288,12292,12348,12350,12538,12540,42237,42240,42508,42512,42610,42612,42621,42623,42737,42744,43123,43128,
43213,43216,43255,43259,43259,43261,43309,43312,43358,43360,43456,43470,43485,43488,43611,43616,43741,43744,43759,43762,44010,44012,55295,57344,65039,
65047,65048,65050,65071,65073,65092,65095,65096,65101,65103,65107,65107,65112,65118,65122,65127,65129,65129,65132,65280,65284,65284,65288,65289,65291,
65291,65293,65293,65296,65305,65308,65310,65313,65339,65341,65376,65378,65379,65382,65791,65795,66462,66464,66511,66513,66926,66928,67670,67672,67870,
67872,67902,67904,68175,68185,68222,68224,68335,68343,68408,68416,68504,68509,69702,69710,69818,69821,69821,69826,69951,69956,70003,70006,70084,70090,
70092,70094,70106,70108,70108,70112,70199,70206,70312,70314,70853,70855,71104,71128,71232,71236,71483,71487,74863,74869,92781,92784,92916,92918,92982,
92988,92995,92997,113822,113824,121478,121484,1114111},new int[]{0,42,44,59,63,123,125,125,127,171,173,176,178,214,216,246,248,1013,1015,1541,1545,8259,
8261,8273,8275,8313,8317,8329,8333,8471,8473,8511,8517,8522,8524,8591,8597,8601,8604,8607,8609,8610,8612,8613,8615,8621,8623,8653,8656,8657,8659,8659,
8661,8691,8960,8991,8994,9083,9085,9114,9140,9179,9186,9654,9656,9664,9666,9719,9728,9838,9840,10175,10181,10182,10214,10223,10240,10495,10627,10648,10712,
10715,10748,10749,11008,11055,11077,11078,11085,55295,57344,64296,64298,65121,65123,65123,65127,65290,65292,65307,65311,65371,65373,65373,65375,65505,
65507,65512,65517,120512,120514,120538,120540,120570,120572,120596,120598,120628,120630,120654,120656,120686,120688,120712,120714,120744,120746,120770,
120772,126703,126706,1114111},new int[]{0,35,37,161,166,1422,1424,1546,1548,2545,2548,2554,2556,2800,2802,3064,3066,3646,3648,6106,6108,8351,8383,43063,
43065,55295,57344,65019,65021,65128,65130,65283,65285,65503,65506,65508,65511,1114111},new int[]{0,93,95,95,97,167,169,174,176,179,181,183,185,705,710,
721,736,740,748,748,750,750,768,884,886,899,902,8124,8126,8126,8130,8140,8144,8156,8160,8172,8176,8188,8191,12442,12445,42751,42775,42783,42786,42888,
42891,43866,43868,55295,57344,64433,64450,65341,65343,65343,65345,65506,65508,127994,128000,1114111},new int[]{0,165,168,168,170,173,175,175,177,181,183,
1153,1155,1420,1423,1549,1552,1757,1759,1768,1770,1788,1791,2037,2039,2553,2555,2927,2929,3058,3065,3065,3067,3198,3200,3448,3450,3840,3844,3858,3860,
3860,3864,3865,3872,3891,3893,3893,3895,3895,3897,4029,4038,4038,4045,4045,4048,4052,4057,4253,4256,5007,5018,6463,6465,6621,6656,7008,7019,7027,7037,
8447,8450,8450,8455,8455,8458,8467,8469,8469,8472,8477,8484,8484,8486,8486,8488,8488,8490,8493,8495,8505,8508,8521,8523,8523,8526,8526,8528,8585,8588,
8596,8602,8603,8608,8608,8611,8611,8614,8614,8622,8622,8654,8655,8658,8658,8660,8660,8692,8959,8968,8971,8992,8993,9001,9002,9084,9084,9115,9139,9180,
9185,9211,9215,9255,9279,9291,9371,9450,9471,9655,9655,9665,9665,9720,9727,9839,9839,10088,10131,10176,10239,10496,11007,11056,11076,11079,11084,11124,
11125,11158,11159,11194,11196,11209,11209,11218,11243,11248,11492,11499,11903,11930,11930,12020,12031,12246,12271,12284,12291,12293,12305,12308,12319,
12321,12341,12344,12349,12352,12687,12690,12693,12704,12735,12772,12799,12831,12841,12872,12879,12881,12895,12928,12937,12977,12991,13055,13055,13312,
19903,19968,42127,42183,43047,43052,43061,43064,43064,43066,43638,43642,55295,57344,65020,65022,65507,65509,65511,65513,65516,65519,65531,65534,65846,
65856,65912,65930,65931,65933,65935,65948,65951,65953,65999,66045,67702,67705,68295,68297,71486,71488,92987,92992,92996,92998,113819,113821,118783,119030,
119039,119079,119080,119141,119145,119149,119170,119173,119179,119210,119213,119273,119295,119362,119364,119366,119551,119639,120831,121344,121398,121403,
121452,121461,121461,121476,121476,121479,126975,127020,127023,127124,127135,127151,127152,127168,127168,127184,127184,127222,127247,127279,127279,127340,
127343,127387,127461,127491,127503,127547,127551,127561,127567,127570,127743,127995,127999,128378,128378,128420,128420,128721,128735,128749,128751,128756,
128767,128884,128895,128981,129023,129036,129039,129096,129103,129114,129119,129160,129167,129198,129295,129305,129407,129413,129471,129473,1114111},new
 int[]{0,887,890,895,900,906,908,908,910,929,931,1327,1329,1366,1369,1375,1377,1415,1417,1418,1421,1423,1425,1479,1488,1514,1520,1524,1536,1564,1566,1805,
1807,1866,1869,1969,1984,2042,2048,2093,2096,2110,2112,2139,2142,2142,2208,2228,2275,2435,2437,2444,2447,2448,2451,2472,2474,2480,2482,2482,2486,2489,
2492,2500,2503,2504,2507,2510,2519,2519,2524,2525,2527,2531,2534,2555,2561,2563,2565,2570,2575,2576,2579,2600,2602,2608,2610,2611,2613,2614,2616,2617,
2620,2620,2622,2626,2631,2632,2635,2637,2641,2641,2649,2652,2654,2654,2662,2677,2689,2691,2693,2701,2703,2705,2707,2728,2730,2736,2738,2739,2741,2745,
2748,2757,2759,2761,2763,2765,2768,2768,2784,2787,2790,2801,2809,2809,2817,2819,2821,2828,2831,2832,2835,2856,2858,2864,2866,2867,2869,2873,2876,2884,
2887,2888,2891,2893,2902,2903,2908,2909,2911,2915,2918,2935,2946,2947,2949,2954,2958,2960,2962,2965,2969,2970,2972,2972,2974,2975,2979,2980,2984,2986,
2990,3001,3006,3010,3014,3016,3018,3021,3024,3024,3031,3031,3046,3066,3072,3075,3077,3084,3086,3088,3090,3112,3114,3129,3133,3140,3142,3144,3146,3149,
3157,3158,3160,3162,3168,3171,3174,3183,3192,3199,3201,3203,3205,3212,3214,3216,3218,3240,3242,3251,3253,3257,3260,3268,3270,3272,3274,3277,3285,3286,
3294,3294,3296,3299,3302,3311,3313,3314,3329,3331,3333,3340,3342,3344,3346,3386,3389,3396,3398,3400,3402,3406,3415,3415,3423,3427,3430,3445,3449,3455,
3458,3459,3461,3478,3482,3505,3507,3515,3517,3517,3520,3526,3530,3530,3535,3540,3542,3542,3544,3551,3558,3567,3570,3572,3585,3642,3647,3675,3713,3714,
3716,3716,3719,3720,3722,3722,3725,3725,3732,3735,3737,3743,3745,3747,3749,3749,3751,3751,3754,3755,3757,3769,3771,3773,3776,3780,3782,3782,3784,3789,
3792,3801,3804,3807,3840,3911,3913,3948,3953,3991,3993,4028,4030,4044,4046,4058,4096,4293,4295,4295,4301,4301,4304,4680,4682,4685,4688,4694,4696,4696,
4698,4701,4704,4744,4746,4749,4752,4784,4786,4789,4792,4798,4800,4800,4802,4805,4808,4822,4824,4880,4882,4885,4888,4954,4957,4988,4992,5017,5024,5109,
5112,5117,5120,5788,5792,5880,5888,5900,5902,5908,5920,5942,5952,5971,5984,5996,5998,6000,6002,6003,6016,6109,6112,6121,6128,6137,6144,6158,6160,6169,
6176,6263,6272,6314,6320,6389,6400,6430,6432,6443,6448,6459,6464,6464,6468,6509,6512,6516,6528,6571,6576,6601,6608,6618,6622,6683,6686,6750,6752,6780,
6783,6793,6800,6809,6816,6829,6832,6846,6912,6987,6992,7036,7040,7155,7164,7223,7227,7241,7245,7295,7360,7367,7376,7414,7416,7417,7424,7669,7676,7957,
7960,7965,7968,8005,8008,8013,8016,8023,8025,8025,8027,8027,8029,8029,8031,8061,8064,8116,8118,8132,8134,8147,8150,8155,8157,8175,8178,8180,8182,8190,
8192,8292,8294,8305,8308,8334,8336,8348,8352,8382,8400,8432,8448,8587,8592,9210,9216,9254,9280,9290,9312,11123,11126,11157,11160,11193,11197,11208,11210,
11217,11244,11247,11264,11310,11312,11358,11360,11507,11513,11557,11559,11559,11565,11565,11568,11623,11631,11632,11647,11670,11680,11686,11688,11694,
11696,11702,11704,11710,11712,11718,11720,11726,11728,11734,11736,11742,11744,11842,11904,11929,11931,12019,12032,12245,12272,12283,12288,12351,12353,
12438,12441,12543,12549,12589,12593,12686,12688,12730,12736,12771,12784,12830,12832,13054,13056,19893,19904,40917,40960,42124,42128,42182,42192,42539,
42560,42743,42752,42925,42928,42935,42999,43051,43056,43065,43072,43127,43136,43204,43214,43225,43232,43261,43264,43347,43359,43388,43392,43469,43471,
43481,43486,43518,43520,43574,43584,43597,43600,43609,43612,43714,43739,43766,43777,43782,43785,43790,43793,43798,43808,43814,43816,43822,43824,43877,
43888,44013,44016,44025,44032,55203,55216,55238,55243,55291,57344,64109,64112,64217,64256,64262,64275,64279,64285,64310,64312,64316,64318,64318,64320,
64321,64323,64324,64326,64449,64467,64831,64848,64911,64914,64967,65008,65021,65024,65049,65056,65106,65108,65126,65128,65131,65136,65140,65142,65276,
65279,65279,65281,65470,65474,65479,65482,65487,65490,65495,65498,65500,65504,65510,65512,65518,65529,65533,65536,65547,65549,65574,65576,65594,65596,
65597,65599,65613,65616,65629,65664,65786,65792,65794,65799,65843,65847,65932,65936,65947,65952,65952,66000,66045,66176,66204,66208,66256,66272,66299,
66304,66339,66352,66378,66384,66426,66432,66461,66463,66499,66504,66517,66560,66717,66720,66729,66816,66855,66864,66915,66927,66927,67072,67382,67392,
67413,67424,67431,67584,67589,67592,67592,67594,67637,67639,67640,67644,67644,67647,67669,67671,67742,67751,67759,67808,67826,67828,67829,67835,67867,
67871,67897,67903,67903,67968,68023,68028,68047,68050,68099,68101,68102,68108,68115,68117,68119,68121,68147,68152,68154,68159,68167,68176,68184,68192,
68255,68288,68326,68331,68342,68352,68405,68409,68437,68440,68466,68472,68497,68505,68508,68521,68527,68608,68680,68736,68786,68800,68850,68858,68863,
69216,69246,69632,69709,69714,69743,69759,69825,69840,69864,69872,69881,69888,69940,69942,69955,69968,70006,70016,70093,70096,70111,70113,70132,70144,
70161,70163,70205,70272,70278,70280,70280,70282,70285,70287,70301,70303,70313,70320,70378,70384,70393,70400,70403,70405,70412,70415,70416,70419,70440,
70442,70448,70450,70451,70453,70457,70460,70468,70471,70472,70475,70477,70480,70480,70487,70487,70493,70499,70502,70508,70512,70516,70784,70855,70864,
70873,71040,71093,71096,71133,71168,71236,71248,71257,71296,71351,71360,71369,71424,71449,71453,71467,71472,71487,71840,71922,71935,71935,72384,72440,
73728,74649,74752,74862,74864,74868,74880,75075,77824,78894,82944,83526,92160,92728,92736,92766,92768,92777,92782,92783,92880,92909,92912,92917,92928,
92997,93008,93017,93019,93025,93027,93047,93053,93071,93952,94020,94032,94078,94095,94111,110592,110593,113664,113770,113776,113788,113792,113800,113808,
113817,113820,113827,118784,119029,119040,119078,119081,119272,119296,119365,119552,119638,119648,119665,119808,119892,119894,119964,119966,119967,119970,
119970,119973,119974,119977,119980,119982,119993,119995,119995,119997,120003,120005,120069,120071,120074,120077,120084,120086,120092,120094,120121,120123,
120126,120128,120132,120134,120134,120138,120144,120146,120485,120488,120779,120782,121483,121499,121503,121505,121519,124928,125124,125127,125142,126464,
126467,126469,126495,126497,126498,126500,126500,126503,126503,126505,126514,126516,126519,126521,126521,126523,126523,126530,126530,126535,126535,126537,
126537,126539,126539,126541,126543,126545,126546,126548,126548,126551,126551,126553,126553,126555,126555,126557,126557,126559,126559,126561,126562,126564,
126564,126567,126570,126572,126578,126580,126583,126585,126588,126590,126590,126592,126601,126603,126619,126625,126627,126629,126633,126635,126651,126704,
126705,126976,127019,127024,127123,127136,127150,127153,127167,127169,127183,127185,127221,127232,127244,127248,127278,127280,127339,127344,127386,127462,
127490,127504,127546,127552,127560,127568,127569,127744,128377,128379,128419,128421,128720,128736,128748,128752,128755,128768,128883,128896,128980,129024,
129035,129040,129095,129104,129113,129120,129159,129168,129197,129296,129304,129408,129412,129472,129472,131072,173782,173824,177972,177984,178205,178208,
183969,194560,195101,917505,917505,917536,917631,917760,917999,983040,1048573,1048576,1114109}};public static int[]IsLetter=new int[]{65,90,97,122,170,
170,181,181,186,186,192,214,216,246,248,705,710,721,736,740,748,748,750,750,880,884,886,887,890,893,895,895,902,902,904,906,908,908,910,929,931,1013,1015,
1153,1162,1327,1329,1366,1369,1369,1377,1415,1488,1514,1520,1522,1568,1610,1646,1647,1649,1747,1749,1749,1765,1766,1774,1775,1786,1788,1791,1791,1808,
1808,1810,1839,1869,1957,1969,1969,1994,2026,2036,2037,2042,2042,2048,2069,2074,2074,2084,2084,2088,2088,2112,2136,2208,2228,2308,2361,2365,2365,2384,
2384,2392,2401,2417,2432,2437,2444,2447,2448,2451,2472,2474,2480,2482,2482,2486,2489,2493,2493,2510,2510,2524,2525,2527,2529,2544,2545,2565,2570,2575,
2576,2579,2600,2602,2608,2610,2611,2613,2614,2616,2617,2649,2652,2654,2654,2674,2676,2693,2701,2703,2705,2707,2728,2730,2736,2738,2739,2741,2745,2749,
2749,2768,2768,2784,2785,2809,2809,2821,2828,2831,2832,2835,2856,2858,2864,2866,2867,2869,2873,2877,2877,2908,2909,2911,2913,2929,2929,2947,2947,2949,
2954,2958,2960,2962,2965,2969,2970,2972,2972,2974,2975,2979,2980,2984,2986,2990,3001,3024,3024,3077,3084,3086,3088,3090,3112,3114,3129,3133,3133,3160,
3162,3168,3169,3205,3212,3214,3216,3218,3240,3242,3251,3253,3257,3261,3261,3294,3294,3296,3297,3313,3314,3333,3340,3342,3344,3346,3386,3389,3389,3406,
3406,3423,3425,3450,3455,3461,3478,3482,3505,3507,3515,3517,3517,3520,3526,3585,3632,3634,3635,3648,3654,3713,3714,3716,3716,3719,3720,3722,3722,3725,
3725,3732,3735,3737,3743,3745,3747,3749,3749,3751,3751,3754,3755,3757,3760,3762,3763,3773,3773,3776,3780,3782,3782,3804,3807,3840,3840,3904,3911,3913,
3948,3976,3980,4096,4138,4159,4159,4176,4181,4186,4189,4193,4193,4197,4198,4206,4208,4213,4225,4238,4238,4256,4293,4295,4295,4301,4301,4304,4346,4348,
4680,4682,4685,4688,4694,4696,4696,4698,4701,4704,4744,4746,4749,4752,4784,4786,4789,4792,4798,4800,4800,4802,4805,4808,4822,4824,4880,4882,4885,4888,
4954,4992,5007,5024,5109,5112,5117,5121,5740,5743,5759,5761,5786,5792,5866,5873,5880,5888,5900,5902,5905,5920,5937,5952,5969,5984,5996,5998,6000,6016,
6067,6103,6103,6108,6108,6176,6263,6272,6312,6314,6314,6320,6389,6400,6430,6480,6509,6512,6516,6528,6571,6576,6601,6656,6678,6688,6740,6823,6823,6917,
6963,6981,6987,7043,7072,7086,7087,7098,7141,7168,7203,7245,7247,7258,7293,7401,7404,7406,7409,7413,7414,7424,7615,7680,7957,7960,7965,7968,8005,8008,
8013,8016,8023,8025,8025,8027,8027,8029,8029,8031,8061,8064,8116,8118,8124,8126,8126,8130,8132,8134,8140,8144,8147,8150,8155,8160,8172,8178,8180,8182,
8188,8305,8305,8319,8319,8336,8348,8450,8450,8455,8455,8458,8467,8469,8469,8473,8477,8484,8484,8486,8486,8488,8488,8490,8493,8495,8505,8508,8511,8517,
8521,8526,8526,8579,8580,11264,11310,11312,11358,11360,11492,11499,11502,11506,11507,11520,11557,11559,11559,11565,11565,11568,11623,11631,11631,11648,
11670,11680,11686,11688,11694,11696,11702,11704,11710,11712,11718,11720,11726,11728,11734,11736,11742,11823,11823,12293,12294,12337,12341,12347,12348,
12353,12438,12445,12447,12449,12538,12540,12543,12549,12589,12593,12686,12704,12730,12784,12799,13312,19893,19968,40917,40960,42124,42192,42237,42240,
42508,42512,42527,42538,42539,42560,42606,42623,42653,42656,42725,42775,42783,42786,42888,42891,42925,42928,42935,42999,43009,43011,43013,43015,43018,
43020,43042,43072,43123,43138,43187,43250,43255,43259,43259,43261,43261,43274,43301,43312,43334,43360,43388,43396,43442,43471,43471,43488,43492,43494,
43503,43514,43518,43520,43560,43584,43586,43588,43595,43616,43638,43642,43642,43646,43695,43697,43697,43701,43702,43705,43709,43712,43712,43714,43714,
43739,43741,43744,43754,43762,43764,43777,43782,43785,43790,43793,43798,43808,43814,43816,43822,43824,43866,43868,43877,43888,44002,44032,55203,55216,
55238,55243,55291,63744,64109,64112,64217,64256,64262,64275,64279,64285,64285,64287,64296,64298,64310,64312,64316,64318,64318,64320,64321,64323,64324,
64326,64433,64467,64829,64848,64911,64914,64967,65008,65019,65136,65140,65142,65276,65313,65338,65345,65370,65382,65470,65474,65479,65482,65487,65490,
65495,65498,65500,65536,65547,65549,65574,65576,65594,65596,65597,65599,65613,65616,65629,65664,65786,66176,66204,66208,66256,66304,66335,66352,66368,
66370,66377,66384,66421,66432,66461,66464,66499,66504,66511,66560,66717,66816,66855,66864,66915,67072,67382,67392,67413,67424,67431,67584,67589,67592,
67592,67594,67637,67639,67640,67644,67644,67647,67669,67680,67702,67712,67742,67808,67826,67828,67829,67840,67861,67872,67897,67968,68023,68030,68031,
68096,68096,68112,68115,68117,68119,68121,68147,68192,68220,68224,68252,68288,68295,68297,68324,68352,68405,68416,68437,68448,68466,68480,68497,68608,
68680,68736,68786,68800,68850,69635,69687,69763,69807,69840,69864,69891,69926,69968,70002,70006,70006,70019,70066,70081,70084,70106,70106,70108,70108,
70144,70161,70163,70187,70272,70278,70280,70280,70282,70285,70287,70301,70303,70312,70320,70366,70405,70412,70415,70416,70419,70440,70442,70448,70450,
70451,70453,70457,70461,70461,70480,70480,70493,70497,70784,70831,70852,70853,70855,70855,71040,71086,71128,71131,71168,71215,71236,71236,71296,71338,
71424,71449,71840,71903,71935,71935,72384,72440,73728,74649,74880,75075,77824,78894,82944,83526,92160,92728,92736,92766,92880,92909,92928,92975,92992,
92995,93027,93047,93053,93071,93952,94020,94032,94032,94099,94111,110592,110593,113664,113770,113776,113788,113792,113800,113808,113817,119808,119892,
119894,119964,119966,119967,119970,119970,119973,119974,119977,119980,119982,119993,119995,119995,119997,120003,120005,120069,120071,120074,120077,120084,
120086,120092,120094,120121,120123,120126,120128,120132,120134,120134,120138,120144,120146,120485,120488,120512,120514,120538,120540,120570,120572,120596,
120598,120628,120630,120654,120656,120686,120688,120712,120714,120744,120746,120770,120772,120779,124928,125124,126464,126467,126469,126495,126497,126498,
126500,126500,126503,126503,126505,126514,126516,126519,126521,126521,126523,126523,126530,126530,126535,126535,126537,126537,126539,126539,126541,126543,
126545,126546,126548,126548,126551,126551,126553,126553,126555,126555,126557,126557,126559,126559,126561,126562,126564,126564,126567,126570,126572,126578,
126580,126583,126585,126588,126590,126590,126592,126601,126603,126619,126625,126627,126629,126633,126635,126651,131072,173782,173824,177972,177984,178205,
178208,183969,194560,195101};public static int[]IsDigit=new int[]{48,57,1632,1641,1776,1785,1984,1993,2406,2415,2534,2543,2662,2671,2790,2799,2918,2927,
3046,3055,3174,3183,3302,3311,3430,3439,3558,3567,3664,3673,3792,3801,3872,3881,4160,4169,4240,4249,6112,6121,6160,6169,6470,6479,6608,6617,6784,6793,
6800,6809,6992,7001,7088,7097,7232,7241,7248,7257,42528,42537,43216,43225,43264,43273,43472,43481,43504,43513,43600,43609,44016,44025,65296,65305,66720,
66729,69734,69743,69872,69881,69942,69951,70096,70105,70384,70393,70864,70873,71248,71257,71360,71369,71472,71481,71904,71913,92768,92777,93008,93017,
120782,120831};public static int[]IsLetterOrDigit=new int[]{48,57,65,90,97,122,170,170,181,181,186,186,192,214,216,246,248,705,710,721,736,740,748,748,
750,750,880,884,886,887,890,893,895,895,902,902,904,906,908,908,910,929,931,1013,1015,1153,1162,1327,1329,1366,1369,1369,1377,1415,1488,1514,1520,1522,
1568,1610,1632,1641,1646,1647,1649,1747,1749,1749,1765,1766,1774,1788,1791,1791,1808,1808,1810,1839,1869,1957,1969,1969,1984,2026,2036,2037,2042,2042,
2048,2069,2074,2074,2084,2084,2088,2088,2112,2136,2208,2228,2308,2361,2365,2365,2384,2384,2392,2401,2406,2415,2417,2432,2437,2444,2447,2448,2451,2472,
2474,2480,2482,2482,2486,2489,2493,2493,2510,2510,2524,2525,2527,2529,2534,2545,2565,2570,2575,2576,2579,2600,2602,2608,2610,2611,2613,2614,2616,2617,
2649,2652,2654,2654,2662,2671,2674,2676,2693,2701,2703,2705,2707,2728,2730,2736,2738,2739,2741,2745,2749,2749,2768,2768,2784,2785,2790,2799,2809,2809,
2821,2828,2831,2832,2835,2856,2858,2864,2866,2867,2869,2873,2877,2877,2908,2909,2911,2913,2918,2927,2929,2929,2947,2947,2949,2954,2958,2960,2962,2965,
2969,2970,2972,2972,2974,2975,2979,2980,2984,2986,2990,3001,3024,3024,3046,3055,3077,3084,3086,3088,3090,3112,3114,3129,3133,3133,3160,3162,3168,3169,
3174,3183,3205,3212,3214,3216,3218,3240,3242,3251,3253,3257,3261,3261,3294,3294,3296,3297,3302,3311,3313,3314,3333,3340,3342,3344,3346,3386,3389,3389,
3406,3406,3423,3425,3430,3439,3450,3455,3461,3478,3482,3505,3507,3515,3517,3517,3520,3526,3558,3567,3585,3632,3634,3635,3648,3654,3664,3673,3713,3714,
3716,3716,3719,3720,3722,3722,3725,3725,3732,3735,3737,3743,3745,3747,3749,3749,3751,3751,3754,3755,3757,3760,3762,3763,3773,3773,3776,3780,3782,3782,
3792,3801,3804,3807,3840,3840,3872,3881,3904,3911,3913,3948,3976,3980,4096,4138,4159,4169,4176,4181,4186,4189,4193,4193,4197,4198,4206,4208,4213,4225,
4238,4238,4240,4249,4256,4293,4295,4295,4301,4301,4304,4346,4348,4680,4682,4685,4688,4694,4696,4696,4698,4701,4704,4744,4746,4749,4752,4784,4786,4789,
4792,4798,4800,4800,4802,4805,4808,4822,4824,4880,4882,4885,4888,4954,4992,5007,5024,5109,5112,5117,5121,5740,5743,5759,5761,5786,5792,5866,5873,5880,
5888,5900,5902,5905,5920,5937,5952,5969,5984,5996,5998,6000,6016,6067,6103,6103,6108,6108,6112,6121,6160,6169,6176,6263,6272,6312,6314,6314,6320,6389,
6400,6430,6470,6509,6512,6516,6528,6571,6576,6601,6608,6617,6656,6678,6688,6740,6784,6793,6800,6809,6823,6823,6917,6963,6981,6987,6992,7001,7043,7072,
7086,7141,7168,7203,7232,7241,7245,7293,7401,7404,7406,7409,7413,7414,7424,7615,7680,7957,7960,7965,7968,8005,8008,8013,8016,8023,8025,8025,8027,8027,
8029,8029,8031,8061,8064,8116,8118,8124,8126,8126,8130,8132,8134,8140,8144,8147,8150,8155,8160,8172,8178,8180,8182,8188,8305,8305,8319,8319,8336,8348,
8450,8450,8455,8455,8458,8467,8469,8469,8473,8477,8484,8484,8486,8486,8488,8488,8490,8493,8495,8505,8508,8511,8517,8521,8526,8526,8579,8580,11264,11310,
11312,11358,11360,11492,11499,11502,11506,11507,11520,11557,11559,11559,11565,11565,11568,11623,11631,11631,11648,11670,11680,11686,11688,11694,11696,
11702,11704,11710,11712,11718,11720,11726,11728,11734,11736,11742,11823,11823,12293,12294,12337,12341,12347,12348,12353,12438,12445,12447,12449,12538,
12540,12543,12549,12589,12593,12686,12704,12730,12784,12799,13312,19893,19968,40917,40960,42124,42192,42237,42240,42508,42512,42539,42560,42606,42623,
42653,42656,42725,42775,42783,42786,42888,42891,42925,42928,42935,42999,43009,43011,43013,43015,43018,43020,43042,43072,43123,43138,43187,43216,43225,
43250,43255,43259,43259,43261,43261,43264,43301,43312,43334,43360,43388,43396,43442,43471,43481,43488,43492,43494,43518,43520,43560,43584,43586,43588,
43595,43600,43609,43616,43638,43642,43642,43646,43695,43697,43697,43701,43702,43705,43709,43712,43712,43714,43714,43739,43741,43744,43754,43762,43764,
43777,43782,43785,43790,43793,43798,43808,43814,43816,43822,43824,43866,43868,43877,43888,44002,44016,44025,44032,55203,55216,55238,55243,55291,63744,
64109,64112,64217,64256,64262,64275,64279,64285,64285,64287,64296,64298,64310,64312,64316,64318,64318,64320,64321,64323,64324,64326,64433,64467,64829,
64848,64911,64914,64967,65008,65019,65136,65140,65142,65276,65296,65305,65313,65338,65345,65370,65382,65470,65474,65479,65482,65487,65490,65495,65498,
65500,65536,65547,65549,65574,65576,65594,65596,65597,65599,65613,65616,65629,65664,65786,66176,66204,66208,66256,66304,66335,66352,66368,66370,66377,
66384,66421,66432,66461,66464,66499,66504,66511,66560,66717,66720,66729,66816,66855,66864,66915,67072,67382,67392,67413,67424,67431,67584,67589,67592,
67592,67594,67637,67639,67640,67644,67644,67647,67669,67680,67702,67712,67742,67808,67826,67828,67829,67840,67861,67872,67897,67968,68023,68030,68031,
68096,68096,68112,68115,68117,68119,68121,68147,68192,68220,68224,68252,68288,68295,68297,68324,68352,68405,68416,68437,68448,68466,68480,68497,68608,
68680,68736,68786,68800,68850,69635,69687,69734,69743,69763,69807,69840,69864,69872,69881,69891,69926,69942,69951,69968,70002,70006,70006,70019,70066,
70081,70084,70096,70106,70108,70108,70144,70161,70163,70187,70272,70278,70280,70280,70282,70285,70287,70301,70303,70312,70320,70366,70384,70393,70405,
70412,70415,70416,70419,70440,70442,70448,70450,70451,70453,70457,70461,70461,70480,70480,70493,70497,70784,70831,70852,70853,70855,70855,70864,70873,
71040,71086,71128,71131,71168,71215,71236,71236,71248,71257,71296,71338,71360,71369,71424,71449,71472,71481,71840,71913,71935,71935,72384,72440,73728,
74649,74880,75075,77824,78894,82944,83526,92160,92728,92736,92766,92768,92777,92880,92909,92928,92975,92992,92995,93008,93017,93027,93047,93053,93071,
93952,94020,94032,94032,94099,94111,110592,110593,113664,113770,113776,113788,113792,113800,113808,113817,119808,119892,119894,119964,119966,119967,119970,
119970,119973,119974,119977,119980,119982,119993,119995,119995,119997,120003,120005,120069,120071,120074,120077,120084,120086,120092,120094,120121,120123,
120126,120128,120132,120134,120134,120138,120144,120146,120485,120488,120512,120514,120538,120540,120570,120572,120596,120598,120628,120630,120654,120656,
120686,120688,120712,120714,120744,120746,120770,120772,120779,120782,120831,124928,125124,126464,126467,126469,126495,126497,126498,126500,126500,126503,
126503,126505,126514,126516,126519,126521,126521,126523,126523,126530,126530,126535,126535,126537,126537,126539,126539,126541,126543,126545,126546,126548,
126548,126551,126551,126553,126553,126555,126555,126557,126557,126559,126559,126561,126562,126564,126564,126567,126570,126572,126578,126580,126583,126585,
126588,126590,126590,126592,126601,126603,126619,126625,126627,126629,126633,126635,126651,131072,173782,173824,177972,177984,178205,178208,183969,194560,
195101};public static int[]IsWhiteSpace=new int[]{9,13,32,32,133,133,160,160,5760,5760,8192,8202,8232,8233,8239,8239,8287,8287,12288,12288};public static
 int[]alnum=new int[]{48,57,65,90,97,122,170,170,181,181,186,186,192,214,216,246,248,705,710,721,736,740,748,748,750,750,880,884,886,887,890,893,895,895,
902,902,904,906,908,908,910,929,931,1013,1015,1153,1162,1327,1329,1366,1369,1369,1377,1415,1488,1514,1520,1522,1568,1610,1632,1641,1646,1647,1649,1747,
1749,1749,1765,1766,1774,1788,1791,1791,1808,1808,1810,1839,1869,1957,1969,1969,1984,2026,2036,2037,2042,2042,2048,2069,2074,2074,2084,2084,2088,2088,
2112,2136,2208,2228,2308,2361,2365,2365,2384,2384,2392,2401,2406,2415,2417,2432,2437,2444,2447,2448,2451,2472,2474,2480,2482,2482,2486,2489,2493,2493,
2510,2510,2524,2525,2527,2529,2534,2545,2565,2570,2575,2576,2579,2600,2602,2608,2610,2611,2613,2614,2616,2617,2649,2652,2654,2654,2662,2671,2674,2676,
2693,2701,2703,2705,2707,2728,2730,2736,2738,2739,2741,2745,2749,2749,2768,2768,2784,2785,2790,2799,2809,2809,2821,2828,2831,2832,2835,2856,2858,2864,
2866,2867,2869,2873,2877,2877,2908,2909,2911,2913,2918,2927,2929,2929,2947,2947,2949,2954,2958,2960,2962,2965,2969,2970,2972,2972,2974,2975,2979,2980,
2984,2986,2990,3001,3024,3024,3046,3055,3077,3084,3086,3088,3090,3112,3114,3129,3133,3133,3160,3162,3168,3169,3174,3183,3205,3212,3214,3216,3218,3240,
3242,3251,3253,3257,3261,3261,3294,3294,3296,3297,3302,3311,3313,3314,3333,3340,3342,3344,3346,3386,3389,3389,3406,3406,3423,3425,3430,3439,3450,3455,
3461,3478,3482,3505,3507,3515,3517,3517,3520,3526,3558,3567,3585,3632,3634,3635,3648,3654,3664,3673,3713,3714,3716,3716,3719,3720,3722,3722,3725,3725,
3732,3735,3737,3743,3745,3747,3749,3749,3751,3751,3754,3755,3757,3760,3762,3763,3773,3773,3776,3780,3782,3782,3792,3801,3804,3807,3840,3840,3872,3881,
3904,3911,3913,3948,3976,3980,4096,4138,4159,4169,4176,4181,4186,4189,4193,4193,4197,4198,4206,4208,4213,4225,4238,4238,4240,4249,4256,4293,4295,4295,
4301,4301,4304,4346,4348,4680,4682,4685,4688,4694,4696,4696,4698,4701,4704,4744,4746,4749,4752,4784,4786,4789,4792,4798,4800,4800,4802,4805,4808,4822,
4824,4880,4882,4885,4888,4954,4992,5007,5024,5109,5112,5117,5121,5740,5743,5759,5761,5786,5792,5866,5870,5880,5888,5900,5902,5905,5920,5937,5952,5969,
5984,5996,5998,6000,6016,6067,6103,6103,6108,6108,6112,6121,6160,6169,6176,6263,6272,6312,6314,6314,6320,6389,6400,6430,6470,6509,6512,6516,6528,6571,
6576,6601,6608,6617,6656,6678,6688,6740,6784,6793,6800,6809,6823,6823,6917,6963,6981,6987,6992,7001,7043,7072,7086,7141,7168,7203,7232,7241,7245,7293,
7401,7404,7406,7409,7413,7414,7424,7615,7680,7957,7960,7965,7968,8005,8008,8013,8016,8023,8025,8025,8027,8027,8029,8029,8031,8061,8064,8116,8118,8124,
8126,8126,8130,8132,8134,8140,8144,8147,8150,8155,8160,8172,8178,8180,8182,8188,8305,8305,8319,8319,8336,8348,8450,8450,8455,8455,8458,8467,8469,8469,
8473,8477,8484,8484,8486,8486,8488,8488,8490,8493,8495,8505,8508,8511,8517,8521,8526,8526,8544,8584,11264,11310,11312,11358,11360,11492,11499,11502,11506,
11507,11520,11557,11559,11559,11565,11565,11568,11623,11631,11631,11648,11670,11680,11686,11688,11694,11696,11702,11704,11710,11712,11718,11720,11726,
11728,11734,11736,11742,11823,11823,12293,12295,12321,12329,12337,12341,12344,12348,12353,12438,12445,12447,12449,12538,12540,12543,12549,12589,12593,
12686,12704,12730,12784,12799,13312,19893,19968,40917,40960,42124,42192,42237,42240,42508,42512,42539,42560,42606,42623,42653,42656,42735,42775,42783,
42786,42888,42891,42925,42928,42935,42999,43009,43011,43013,43015,43018,43020,43042,43072,43123,43138,43187,43216,43225,43250,43255,43259,43259,43261,
43261,43264,43301,43312,43334,43360,43388,43396,43442,43471,43481,43488,43492,43494,43518,43520,43560,43584,43586,43588,43595,43600,43609,43616,43638,
43642,43642,43646,43695,43697,43697,43701,43702,43705,43709,43712,43712,43714,43714,43739,43741,43744,43754,43762,43764,43777,43782,43785,43790,43793,
43798,43808,43814,43816,43822,43824,43866,43868,43877,43888,44002,44016,44025,44032,55203,55216,55238,55243,55291,63744,64109,64112,64217,64256,64262,
64275,64279,64285,64285,64287,64296,64298,64310,64312,64316,64318,64318,64320,64321,64323,64324,64326,64433,64467,64829,64848,64911,64914,64967,65008,
65019,65136,65140,65142,65276,65296,65305,65313,65338,65345,65370,65382,65470,65474,65479,65482,65487,65490,65495,65498,65500,65536,65547,65549,65574,
65576,65594,65596,65597,65599,65613,65616,65629,65664,65786,65856,65908,66176,66204,66208,66256,66304,66335,66352,66378,66384,66421,66432,66461,66464,
66499,66504,66511,66513,66517,66560,66717,66720,66729,66816,66855,66864,66915,67072,67382,67392,67413,67424,67431,67584,67589,67592,67592,67594,67637,
67639,67640,67644,67644,67647,67669,67680,67702,67712,67742,67808,67826,67828,67829,67840,67861,67872,67897,67968,68023,68030,68031,68096,68096,68112,
68115,68117,68119,68121,68147,68192,68220,68224,68252,68288,68295,68297,68324,68352,68405,68416,68437,68448,68466,68480,68497,68608,68680,68736,68786,
68800,68850,69635,69687,69734,69743,69763,69807,69840,69864,69872,69881,69891,69926,69942,69951,69968,70002,70006,70006,70019,70066,70081,70084,70096,
70106,70108,70108,70144,70161,70163,70187,70272,70278,70280,70280,70282,70285,70287,70301,70303,70312,70320,70366,70384,70393,70405,70412,70415,70416,
70419,70440,70442,70448,70450,70451,70453,70457,70461,70461,70480,70480,70493,70497,70784,70831,70852,70853,70855,70855,70864,70873,71040,71086,71128,
71131,71168,71215,71236,71236,71248,71257,71296,71338,71360,71369,71424,71449,71472,71481,71840,71913,71935,71935,72384,72440,73728,74649,74752,74862,
74880,75075,77824,78894,82944,83526,92160,92728,92736,92766,92768,92777,92880,92909,92928,92975,92992,92995,93008,93017,93027,93047,93053,93071,93952,
94020,94032,94032,94099,94111,110592,110593,113664,113770,113776,113788,113792,113800,113808,113817,119808,119892,119894,119964,119966,119967,119970,119970,
119973,119974,119977,119980,119982,119993,119995,119995,119997,120003,120005,120069,120071,120074,120077,120084,120086,120092,120094,120121,120123,120126,
120128,120132,120134,120134,120138,120144,120146,120485,120488,120512,120514,120538,120540,120570,120572,120596,120598,120628,120630,120654,120656,120686,
120688,120712,120714,120744,120746,120770,120772,120779,120782,120831,124928,125124,126464,126467,126469,126495,126497,126498,126500,126500,126503,126503,
126505,126514,126516,126519,126521,126521,126523,126523,126530,126530,126535,126535,126537,126537,126539,126539,126541,126543,126545,126546,126548,126548,
126551,126551,126553,126553,126555,126555,126557,126557,126559,126559,126561,126562,126564,126564,126567,126570,126572,126578,126580,126583,126585,126588,
126590,126590,126592,126601,126603,126619,126625,126627,126629,126633,126635,126651,131072,173782,173824,177972,177984,178205,178208,183969,194560,195101};
public static int[]alpha=new int[]{65,90,97,122,170,170,181,181,186,186,192,214,216,246,248,705,710,721,736,740,748,748,750,750,880,884,886,887,890,893,
895,895,902,902,904,906,908,908,910,929,931,1013,1015,1153,1162,1327,1329,1366,1369,1369,1377,1415,1488,1514,1520,1522,1568,1610,1646,1647,1649,1747,1749,
1749,1765,1766,1774,1775,1786,1788,1791,1791,1808,1808,1810,1839,1869,1957,1969,1969,1994,2026,2036,2037,2042,2042,2048,2069,2074,2074,2084,2084,2088,
2088,2112,2136,2208,2228,2308,2361,2365,2365,2384,2384,2392,2401,2417,2432,2437,2444,2447,2448,2451,2472,2474,2480,2482,2482,2486,2489,2493,2493,2510,
2510,2524,2525,2527,2529,2544,2545,2565,2570,2575,2576,2579,2600,2602,2608,2610,2611,2613,2614,2616,2617,2649,2652,2654,2654,2674,2676,2693,2701,2703,
2705,2707,2728,2730,2736,2738,2739,2741,2745,2749,2749,2768,2768,2784,2785,2809,2809,2821,2828,2831,2832,2835,2856,2858,2864,2866,2867,2869,2873,2877,
2877,2908,2909,2911,2913,2929,2929,2947,2947,2949,2954,2958,2960,2962,2965,2969,2970,2972,2972,2974,2975,2979,2980,2984,2986,2990,3001,3024,3024,3077,
3084,3086,3088,3090,3112,3114,3129,3133,3133,3160,3162,3168,3169,3205,3212,3214,3216,3218,3240,3242,3251,3253,3257,3261,3261,3294,3294,3296,3297,3313,
3314,3333,3340,3342,3344,3346,3386,3389,3389,3406,3406,3423,3425,3450,3455,3461,3478,3482,3505,3507,3515,3517,3517,3520,3526,3585,3632,3634,3635,3648,
3654,3713,3714,3716,3716,3719,3720,3722,3722,3725,3725,3732,3735,3737,3743,3745,3747,3749,3749,3751,3751,3754,3755,3757,3760,3762,3763,3773,3773,3776,
3780,3782,3782,3804,3807,3840,3840,3904,3911,3913,3948,3976,3980,4096,4138,4159,4159,4176,4181,4186,4189,4193,4193,4197,4198,4206,4208,4213,4225,4238,
4238,4256,4293,4295,4295,4301,4301,4304,4346,4348,4680,4682,4685,4688,4694,4696,4696,4698,4701,4704,4744,4746,4749,4752,4784,4786,4789,4792,4798,4800,
4800,4802,4805,4808,4822,4824,4880,4882,4885,4888,4954,4992,5007,5024,5109,5112,5117,5121,5740,5743,5759,5761,5786,5792,5866,5870,5880,5888,5900,5902,
5905,5920,5937,5952,5969,5984,5996,5998,6000,6016,6067,6103,6103,6108,6108,6176,6263,6272,6312,6314,6314,6320,6389,6400,6430,6480,6509,6512,6516,6528,
6571,6576,6601,6656,6678,6688,6740,6823,6823,6917,6963,6981,6987,7043,7072,7086,7087,7098,7141,7168,7203,7245,7247,7258,7293,7401,7404,7406,7409,7413,
7414,7424,7615,7680,7957,7960,7965,7968,8005,8008,8013,8016,8023,8025,8025,8027,8027,8029,8029,8031,8061,8064,8116,8118,8124,8126,8126,8130,8132,8134,
8140,8144,8147,8150,8155,8160,8172,8178,8180,8182,8188,8305,8305,8319,8319,8336,8348,8450,8450,8455,8455,8458,8467,8469,8469,8473,8477,8484,8484,8486,
8486,8488,8488,8490,8493,8495,8505,8508,8511,8517,8521,8526,8526,8544,8584,11264,11310,11312,11358,11360,11492,11499,11502,11506,11507,11520,11557,11559,
11559,11565,11565,11568,11623,11631,11631,11648,11670,11680,11686,11688,11694,11696,11702,11704,11710,11712,11718,11720,11726,11728,11734,11736,11742,
11823,11823,12293,12295,12321,12329,12337,12341,12344,12348,12353,12438,12445,12447,12449,12538,12540,12543,12549,12589,12593,12686,12704,12730,12784,
12799,13312,19893,19968,40917,40960,42124,42192,42237,42240,42508,42512,42527,42538,42539,42560,42606,42623,42653,42656,42735,42775,42783,42786,42888,
42891,42925,42928,42935,42999,43009,43011,43013,43015,43018,43020,43042,43072,43123,43138,43187,43250,43255,43259,43259,43261,43261,43274,43301,43312,
43334,43360,43388,43396,43442,43471,43471,43488,43492,43494,43503,43514,43518,43520,43560,43584,43586,43588,43595,43616,43638,43642,43642,43646,43695,
43697,43697,43701,43702,43705,43709,43712,43712,43714,43714,43739,43741,43744,43754,43762,43764,43777,43782,43785,43790,43793,43798,43808,43814,43816,
43822,43824,43866,43868,43877,43888,44002,44032,55203,55216,55238,55243,55291,63744,64109,64112,64217,64256,64262,64275,64279,64285,64285,64287,64296,
64298,64310,64312,64316,64318,64318,64320,64321,64323,64324,64326,64433,64467,64829,64848,64911,64914,64967,65008,65019,65136,65140,65142,65276,65313,
65338,65345,65370,65382,65470,65474,65479,65482,65487,65490,65495,65498,65500,65536,65547,65549,65574,65576,65594,65596,65597,65599,65613,65616,65629,
65664,65786,65856,65908,66176,66204,66208,66256,66304,66335,66352,66378,66384,66421,66432,66461,66464,66499,66504,66511,66513,66517,66560,66717,66816,
66855,66864,66915,67072,67382,67392,67413,67424,67431,67584,67589,67592,67592,67594,67637,67639,67640,67644,67644,67647,67669,67680,67702,67712,67742,
67808,67826,67828,67829,67840,67861,67872,67897,67968,68023,68030,68031,68096,68096,68112,68115,68117,68119,68121,68147,68192,68220,68224,68252,68288,
68295,68297,68324,68352,68405,68416,68437,68448,68466,68480,68497,68608,68680,68736,68786,68800,68850,69635,69687,69763,69807,69840,69864,69891,69926,
69968,70002,70006,70006,70019,70066,70081,70084,70106,70106,70108,70108,70144,70161,70163,70187,70272,70278,70280,70280,70282,70285,70287,70301,70303,
70312,70320,70366,70405,70412,70415,70416,70419,70440,70442,70448,70450,70451,70453,70457,70461,70461,70480,70480,70493,70497,70784,70831,70852,70853,
70855,70855,71040,71086,71128,71131,71168,71215,71236,71236,71296,71338,71424,71449,71840,71903,71935,71935,72384,72440,73728,74649,74752,74862,74880,
75075,77824,78894,82944,83526,92160,92728,92736,92766,92880,92909,92928,92975,92992,92995,93027,93047,93053,93071,93952,94020,94032,94032,94099,94111,
110592,110593,113664,113770,113776,113788,113792,113800,113808,113817,119808,119892,119894,119964,119966,119967,119970,119970,119973,119974,119977,119980,
119982,119993,119995,119995,119997,120003,120005,120069,120071,120074,120077,120084,120086,120092,120094,120121,120123,120126,120128,120132,120134,120134,
120138,120144,120146,120485,120488,120512,120514,120538,120540,120570,120572,120596,120598,120628,120630,120654,120656,120686,120688,120712,120714,120744,
120746,120770,120772,120779,124928,125124,126464,126467,126469,126495,126497,126498,126500,126500,126503,126503,126505,126514,126516,126519,126521,126521,
126523,126523,126530,126530,126535,126535,126537,126537,126539,126539,126541,126543,126545,126546,126548,126548,126551,126551,126553,126553,126555,126555,
126557,126557,126559,126559,126561,126562,126564,126564,126567,126570,126572,126578,126580,126583,126585,126588,126590,126590,126592,126601,126603,126619,
126625,126627,126629,126633,126635,126651,131072,173782,173824,177972,177984,178205,178208,183969,194560,195101};public static int[]cntrl=CharCls.UnicodeCategories[14];
public static int[]digit=CharCls.UnicodeCategories[8];public static int[]graph=new int[]{33,126,161,5759,5761,8191,8203,8231,8234,8238,8240,8286,8288,
12287,12289,55295,57344,1114111};public static int[]ascii=new int[]{0,127};public static int[]blank=new int[]{9,9,32,32,160,160,5760,5760,8192,8202,8239,
8239,8287,8287,12288,12288};public static int[]lower=CharCls.UnicodeCategories[1];public static int[]print=CharCls.NotUnicodeCategories[14];public static
 int[]punct=new int[]{41,41,93,93,125,125,3899,3899,3901,3901,5788,5788,8262,8262,8318,8318,8334,8334,8969,8969,8971,8971,9002,9002,10089,10089,10091,
10091,10093,10093,10095,10095,10097,10097,10099,10099,10101,10101,10182,10182,10215,10215,10217,10217,10219,10219,10221,10221,10223,10223,10628,10628,
10630,10630,10632,10632,10634,10634,10636,10636,10638,10638,10640,10640,10642,10642,10644,10644,10646,10646,10648,10648,10713,10713,10715,10715,10749,
10749,11811,11811,11813,11813,11815,11815,11817,11817,12297,12297,12299,12299,12301,12301,12303,12303,12305,12305,12309,12309,12311,12311,12313,12313,
12315,12315,12318,12319,64830,64830,65048,65048,65078,65078,65080,65080,65082,65082,65084,65084,65086,65086,65088,65088,65090,65090,65092,65092,65096,
65096,65114,65114,65116,65116,65118,65118,65289,65289,65341,65341,65373,65373,65376,65376,65379,65379,95,95,8255,8256,8276,8276,65075,65076,65101,65101,
65103,65103,65343,65343,45,45,173,173,1418,1418,1470,1470,5120,5120,6150,6150,8208,8208,8213,8213,11799,11799,11802,11802,11834,11835,11840,11840,12316,
12316,12336,12336,12448,12448,65073,65074,65112,65112,65123,65123,65293,65293,187,187,8217,8217,8221,8221,8250,8250,11779,11779,11781,11781,11786,11786,
11789,11789,11805,11805,11809,11809,171,171,8216,8216,8219,8220,8223,8223,8249,8249,11778,11778,11780,11780,11785,11785,11788,11788,11804,11804,11808,
11808,40,40,91,91,123,123,3898,3898,3900,3900,5787,5787,8218,8218,8222,8222,8261,8261,8317,8317,8333,8333,8968,8968,8970,8970,9001,9001,10088,10088,10090,
10090,10092,10092,10094,10094,10096,10096,10098,10098,10100,10100,10181,10181,10214,10214,10216,10216,10218,10218,10220,10220,10222,10222,10627,10627,
10629,10629,10631,10631,10633,10633,10635,10635,10637,10637,10639,10639,10641,10641,10643,10643,10645,10645,10647,10647,10712,10712,10714,10714,10748,
10748,11810,11810,11812,11812,11814,11814,11816,11816,11842,11842,12296,12296,12298,12298,12300,12300,12302,12302,12304,12304,12308,12308,12310,12310,
12312,12312,12314,12314,12317,12317,64831,64831,65047,65047,65077,65077,65079,65079,65081,65081,65083,65083,65085,65085,65087,65087,65089,65089,65091,
65091,65095,65095,65113,65113,65115,65115,65117,65117,65288,65288,65339,65339,65371,65371,65375,65375,65378,65378,33,33,35,35,37,37,39,39,42,42,44,44,
46,47,58,59,63,64,92,92,161,161,183,183,191,191,894,894,903,903,1370,1370,1375,1375,1417,1417,1472,1472,1475,1475,1478,1478,1523,1524,1545,1546,1548,1549,
1563,1563,1566,1567,1642,1642,1645,1645,1748,1748,1792,1792,1805,1805,2039,2039,2041,2041,2096,2096,2110,2110,2142,2142,2404,2405,2416,2416,2800,2800,
3572,3572,3663,3663,3674,3675,3844,3844,3858,3858,3860,3860,3973,3973,4048,4048,4052,4052,4057,4058,4170,4170,4175,4175,4347,4347,4960,4960,4968,4968,
5741,5742,5867,5867,5869,5869,5941,5942,6100,6100,6102,6102,6104,6104,6106,6106,6144,6144,6149,6149,6151,6151,6154,6154,6468,6469,6686,6687,6816,6816,
6822,6822,6824,6824,6829,6829,7002,7002,7008,7008,7164,7164,7167,7167,7227,7227,7231,7231,7294,7295,7360,7360,7367,7367,7379,7379,8214,8215,8224,8224,
8231,8231,8240,8240,8248,8248,8251,8251,8254,8254,8257,8257,8259,8259,8263,8263,8273,8273,8275,8275,8277,8277,8286,8286,11513,11513,11516,11516,11518,
11519,11632,11632,11776,11777,11782,11782,11784,11784,11787,11787,11790,11790,11798,11798,11800,11801,11803,11803,11806,11807,11818,11818,11822,11822,
11824,11824,11833,11833,11836,11836,11839,11839,11841,11841,12289,12289,12291,12291,12349,12349,12539,12539,42238,42239,42509,42509,42511,42511,42611,
42611,42622,42622,42738,42738,42743,42743,43124,43124,43127,43127,43214,43215,43256,43256,43258,43258,43260,43260,43310,43311,43359,43359,43457,43457,
43469,43469,43486,43487,43612,43612,43615,43615,43742,43743,43760,43761,44011,44011,65040,65040,65046,65046,65049,65049,65072,65072,65093,65094,65097,
65097,65100,65100,65104,65104,65106,65106,65108,65108,65111,65111,65119,65119,65121,65121,65128,65128,65130,65131,65281,65281,65283,65283,65285,65285,
65287,65287,65290,65290,65292,65292,65294,65295,65306,65307,65311,65312,65340,65340,65377,65377,65380,65381,65792,65792,65794,65794,66463,66463,66512,
66512,66927,66927,67671,67671,67871,67871,67903,67903,68176,68176,68184,68184,68223,68223,68336,68336,68342,68342,68409,68409,68415,68415,68505,68505,
68508,68508,69703,69703,69709,69709,69819,69820,69822,69822,69825,69825,69952,69952,69955,69955,70004,70005,70085,70085,70089,70089,70093,70093,70107,
70107,70109,70109,70111,70111,70200,70200,70205,70205,70313,70313,70854,70854,71105,71105,71127,71127,71233,71233,71235,71235,71484,71484,71486,71486,
74864,74864,74868,74868,92782,92783,92917,92917,92983,92983,92987,92987,92996,92996,113823,113823,121479,121479,121483,121483};public static int[]space
=CharCls.IsWhiteSpace;public static int[]upper=CharCls.UnicodeCategories[0];public static int[]word=new int[]{65,90,97,122,170,170,181,181,186,186,192,
214,216,246,248,705,710,721,736,740,748,748,750,750,880,884,886,887,890,893,895,895,902,902,904,906,908,908,910,929,931,1013,1015,1153,1162,1327,1329,
1366,1369,1369,1377,1415,1488,1514,1520,1522,1568,1610,1646,1647,1649,1747,1749,1749,1765,1766,1774,1775,1786,1788,1791,1791,1808,1808,1810,1839,1869,
1957,1969,1969,1994,2026,2036,2037,2042,2042,2048,2069,2074,2074,2084,2084,2088,2088,2112,2136,2208,2228,2308,2361,2365,2365,2384,2384,2392,2401,2417,
2432,2437,2444,2447,2448,2451,2472,2474,2480,2482,2482,2486,2489,2493,2493,2510,2510,2524,2525,2527,2529,2544,2545,2565,2570,2575,2576,2579,2600,2602,
2608,2610,2611,2613,2614,2616,2617,2649,2652,2654,2654,2674,2676,2693,2701,2703,2705,2707,2728,2730,2736,2738,2739,2741,2745,2749,2749,2768,2768,2784,
2785,2809,2809,2821,2828,2831,2832,2835,2856,2858,2864,2866,2867,2869,2873,2877,2877,2908,2909,2911,2913,2929,2929,2947,2947,2949,2954,2958,2960,2962,
2965,2969,2970,2972,2972,2974,2975,2979,2980,2984,2986,2990,3001,3024,3024,3077,3084,3086,3088,3090,3112,3114,3129,3133,3133,3160,3162,3168,3169,3205,
3212,3214,3216,3218,3240,3242,3251,3253,3257,3261,3261,3294,3294,3296,3297,3313,3314,3333,3340,3342,3344,3346,3386,3389,3389,3406,3406,3423,3425,3450,
3455,3461,3478,3482,3505,3507,3515,3517,3517,3520,3526,3585,3632,3634,3635,3648,3654,3713,3714,3716,3716,3719,3720,3722,3722,3725,3725,3732,3735,3737,
3743,3745,3747,3749,3749,3751,3751,3754,3755,3757,3760,3762,3763,3773,3773,3776,3780,3782,3782,3804,3807,3840,3840,3904,3911,3913,3948,3976,3980,4096,
4138,4159,4159,4176,4181,4186,4189,4193,4193,4197,4198,4206,4208,4213,4225,4238,4238,4256,4293,4295,4295,4301,4301,4304,4346,4348,4680,4682,4685,4688,
4694,4696,4696,4698,4701,4704,4744,4746,4749,4752,4784,4786,4789,4792,4798,4800,4800,4802,4805,4808,4822,4824,4880,4882,4885,4888,4954,4992,5007,5024,
5109,5112,5117,5121,5740,5743,5759,5761,5786,5792,5866,5873,5880,5888,5900,5902,5905,5920,5937,5952,5969,5984,5996,5998,6000,6016,6067,6103,6103,6108,
6108,6176,6263,6272,6312,6314,6314,6320,6389,6400,6430,6480,6509,6512,6516,6528,6571,6576,6601,6656,6678,6688,6740,6823,6823,6917,6963,6981,6987,7043,
7072,7086,7087,7098,7141,7168,7203,7245,7247,7258,7293,7401,7404,7406,7409,7413,7414,7424,7615,7680,7957,7960,7965,7968,8005,8008,8013,8016,8023,8025,
8025,8027,8027,8029,8029,8031,8061,8064,8116,8118,8124,8126,8126,8130,8132,8134,8140,8144,8147,8150,8155,8160,8172,8178,8180,8182,8188,8305,8305,8319,
8319,8336,8348,8450,8450,8455,8455,8458,8467,8469,8469,8473,8477,8484,8484,8486,8486,8488,8488,8490,8493,8495,8505,8508,8511,8517,8521,8526,8526,8579,
8580,11264,11310,11312,11358,11360,11492,11499,11502,11506,11507,11520,11557,11559,11559,11565,11565,11568,11623,11631,11631,11648,11670,11680,11686,11688,
11694,11696,11702,11704,11710,11712,11718,11720,11726,11728,11734,11736,11742,11823,11823,12293,12294,12337,12341,12347,12348,12353,12438,12445,12447,
12449,12538,12540,12543,12549,12589,12593,12686,12704,12730,12784,12799,13312,19893,19968,40917,40960,42124,42192,42237,42240,42508,42512,42527,42538,
42539,42560,42606,42623,42653,42656,42725,42775,42783,42786,42888,42891,42925,42928,42935,42999,43009,43011,43013,43015,43018,43020,43042,43072,43123,
43138,43187,43250,43255,43259,43259,43261,43261,43274,43301,43312,43334,43360,43388,43396,43442,43471,43471,43488,43492,43494,43503,43514,43518,43520,
43560,43584,43586,43588,43595,43616,43638,43642,43642,43646,43695,43697,43697,43701,43702,43705,43709,43712,43712,43714,43714,43739,43741,43744,43754,
43762,43764,43777,43782,43785,43790,43793,43798,43808,43814,43816,43822,43824,43866,43868,43877,43888,44002,44032,55203,55216,55238,55243,55291,63744,
64109,64112,64217,64256,64262,64275,64279,64285,64285,64287,64296,64298,64310,64312,64316,64318,64318,64320,64321,64323,64324,64326,64433,64467,64829,
64848,64911,64914,64967,65008,65019,65136,65140,65142,65276,65313,65338,65345,65370,65382,65470,65474,65479,65482,65487,65490,65495,65498,65500,65536,
65547,65549,65574,65576,65594,65596,65597,65599,65613,65616,65629,65664,65786,66176,66204,66208,66256,66304,66335,66352,66368,66370,66377,66384,66421,
66432,66461,66464,66499,66504,66511,66560,66717,66816,66855,66864,66915,67072,67382,67392,67413,67424,67431,67584,67589,67592,67592,67594,67637,67639,
67640,67644,67644,67647,67669,67680,67702,67712,67742,67808,67826,67828,67829,67840,67861,67872,67897,67968,68023,68030,68031,68096,68096,68112,68115,
68117,68119,68121,68147,68192,68220,68224,68252,68288,68295,68297,68324,68352,68405,68416,68437,68448,68466,68480,68497,68608,68680,68736,68786,68800,
68850,69635,69687,69763,69807,69840,69864,69891,69926,69968,70002,70006,70006,70019,70066,70081,70084,70106,70106,70108,70108,70144,70161,70163,70187,
70272,70278,70280,70280,70282,70285,70287,70301,70303,70312,70320,70366,70405,70412,70415,70416,70419,70440,70442,70448,70450,70451,70453,70457,70461,
70461,70480,70480,70493,70497,70784,70831,70852,70853,70855,70855,71040,71086,71128,71131,71168,71215,71236,71236,71296,71338,71424,71449,71840,71903,
71935,71935,72384,72440,73728,74649,74880,75075,77824,78894,82944,83526,92160,92728,92736,92766,92880,92909,92928,92975,92992,92995,93027,93047,93053,
93071,93952,94020,94032,94032,94099,94111,110592,110593,113664,113770,113776,113788,113792,113800,113808,113817,119808,119892,119894,119964,119966,119967,
119970,119970,119973,119974,119977,119980,119982,119993,119995,119995,119997,120003,120005,120069,120071,120074,120077,120084,120086,120092,120094,120121,
120123,120126,120128,120132,120134,120134,120138,120144,120146,120485,120488,120512,120514,120538,120540,120570,120572,120596,120598,120628,120630,120654,
120656,120686,120688,120712,120714,120744,120746,120770,120772,120779,124928,125124,126464,126467,126469,126495,126497,126498,126500,126500,126503,126503,
126505,126514,126516,126519,126521,126521,126523,126523,126530,126530,126535,126535,126537,126537,126539,126539,126541,126543,126545,126546,126548,126548,
126551,126551,126553,126553,126555,126555,126557,126557,126559,126559,126561,126562,126564,126564,126567,126570,126572,126578,126580,126583,126585,126588,
126590,126590,126592,126601,126603,126619,126625,126627,126629,126633,126635,126651,131072,173782,173824,177972,177984,178205,178208,183969,194560,195101,
5870,5870,5872,5872,8544,8544,8578,8578,8581,8581,8584,8584,12295,12295,12321,12321,12329,12329,12344,12344,12346,12346,42726,42726,42735,42735,65856,
65856,65908,65908,66369,66369,66378,66378,66513,66513,66517,66517,74752,74752,74862,74862,95,95,8255,8256,8276,8276,65075,65076,65101,65101,65103,65103,
65343,65343,48,48,57,57,1632,1632,1641,1641,1776,1776,1785,1785,1984,1984,1993,1993,2406,2406,2415,2415,2534,2534,2543,2543,2662,2662,2671,2671,2790,2790,
2799,2799,2918,2918,2927,2927,3046,3046,3055,3055,3174,3174,3183,3183,3302,3302,3311,3311,3430,3430,3439,3439,3558,3558,3567,3567,3664,3664,3673,3673,
3792,3792,3801,3801,3872,3872,3881,3881,4160,4160,4169,4169,4240,4240,4249,4249,6112,6112,6121,6121,6160,6160,6169,6169,6470,6470,6479,6479,6608,6608,
6617,6617,6784,6784,6793,6793,6800,6800,6809,6809,6992,6992,7001,7001,7088,7088,7097,7097,7232,7232,7241,7241,7248,7248,7257,7257,42528,42528,42537,42537,
43216,43216,43225,43225,43264,43264,43273,43273,43472,43472,43481,43481,43504,43504,43513,43513,43600,43600,43609,43609,44016,44016,44025,44025,65296,
65296,65305,65305,66720,66720,66729,66729,69734,69734,69743,69743,69872,69872,69881,69881,69942,69942,69951,69951,70096,70096,70105,70105,70384,70384,
70393,70393,70864,70864,70873,70873,71248,71248,71257,71257,71360,71360,71369,71369,71472,71472,71481,71481,71904,71904,71913,71913,92768,92768,92777,
92777,93008,93008,93017,93017,120782,120782,120831,120831};public static int[]xdigit=new int[]{48,48,57,57,65,65,70,70,97,97,102,102};}}namespace L{static
 class Compiler{
#region Opcodes
internal const int Match=1; internal const int Jmp=2; internal const int Split=3; internal const int Any=4; internal const int Char=5; internal const int
 Set=6; internal const int NSet=7; internal const int UCode=8; internal const int NUCode=9; internal const int Save=10;
#endregion
internal static List<int[]>Emit(Ast ast,int symbolId=-1){var prog=new List<int[]>();EmitPart(ast,prog);if(-1!=symbolId){var match=new int[2];match[0]=
Match;match[1]=symbolId;prog.Add(match);}return prog;}internal static void EmitPart(string literal,IList<int[]>prog){for(var i=0;i<literal.Length;++i)
{int ch=literal[i];if(char.IsHighSurrogate(literal[i])){if(i==literal.Length-1)throw new ArgumentException("The literal contains an incomplete unicode surrogate.",
nameof(literal));ch=char.ConvertToUtf32(literal,i);++i;}var lit=new int[2];lit[0]=Char;lit[1]=ch;prog.Add(lit);}}internal static void EmitPart(Ast ast,
IList<int[]>prog){int[]inst,jmp;switch(ast.Kind){case Ast.Lit: inst=new int[2];inst[0]=Char;inst[1]=ast.Value;prog.Add(inst);break;case Ast.Cat: for(var
 i=0;i<ast.Exprs.Length;i++)if(null!=ast.Exprs[i])EmitPart(ast.Exprs[i],prog);break;case Ast.Dot: inst=new int[1];inst[0]=Any;break;case Ast.Alt: var exprs
=new List<Ast>(ast.Exprs.Length);var firstNull=-1;for(var i=0;i<ast.Exprs.Length;++i){if(null==ast.Exprs[i]){if(0>firstNull){firstNull=i;exprs.Add(null);
}continue;}exprs.Add(ast.Exprs[i]);}ast.Exprs=exprs.ToArray();var split=new int[ast.Exprs.Length+1];split[0]=Split;prog.Add(split);var jmpfixes=new List<int>(ast.Exprs.Length
-1);for(var i=0;i<ast.Exprs.Length;++i){var e=ast.Exprs[i];if(null!=e){split[i+1]=prog.Count;EmitPart(e,prog);if(i==ast.Exprs.Length-1)continue;if(i==
ast.Exprs.Length-2&&null==ast.Exprs[i+1])continue;var j=new int[2];j[0]=Jmp;jmpfixes.Add(prog.Count);prog.Add(j);}}for(int ic=jmpfixes.Count,i=0;i<ic;++i)
{var j=prog[jmpfixes[i]];j[1]=prog.Count;}if(-1<firstNull){split[firstNull+1]=prog.Count;}break;case Ast.NSet:case Ast.Set: inst=new int[ast.Ranges.Length
+1];inst[0]=(ast.Kind==Ast.Set)?Set:NSet;SortRanges(ast.Ranges);Array.Copy(ast.Ranges,0,inst,1,ast.Ranges.Length);prog.Add(inst);break;case Ast.NUCode:
case Ast.UCode: inst=new int[2];inst[0]=(ast.Kind==Ast.UCode)?UCode:NUCode;inst[1]=ast.Value;prog.Add(inst);break;case Ast.Opt:inst=new int[3]; inst[0]
=Split;prog.Add(inst);inst[1]=prog.Count; for(var i=0;i<ast.Exprs.Length;i++)if(null!=ast.Exprs[i])EmitPart(ast.Exprs[i],prog);inst[2]=prog.Count;if(ast.IsLazy)
{ var t=inst[1];inst[1]=inst[2];inst[2]=t;}break; case Ast.Star:ast.Min=0;ast.Max=0;goto case Ast.Rep;case Ast.Plus:ast.Min=1;ast.Max=0;goto case Ast.Rep;
case Ast.Rep: if(ast.Min>0&&ast.Max>0&&ast.Min>ast.Max)throw new ArgumentOutOfRangeException("Max");int idx;Ast opt;Ast rep;switch(ast.Min){case-1:case
 0:switch(ast.Max){ case-1:case 0:idx=prog.Count;inst=new int[3];inst[0]=Split;prog.Add(inst);inst[1]=prog.Count;for(var i=0;i<ast.Exprs.Length;i++)if
(null!=ast.Exprs[i])EmitPart(ast.Exprs[i],prog);jmp=new int[2];jmp[0]=Jmp;jmp[1]=idx;prog.Add(jmp);inst[2]=prog.Count;if(ast.IsLazy){ var t=inst[1];inst[1]
=inst[2];inst[2]=t;}return; case 1:opt=new Ast();opt.Kind=Ast.Opt;opt.Exprs=ast.Exprs;opt.IsLazy=ast.IsLazy;EmitPart(opt,prog);return;default: opt=new
 Ast();opt.Kind=Ast.Opt;opt.Exprs=ast.Exprs;opt.IsLazy=ast.IsLazy;EmitPart(opt,prog);for(var i=1;i<ast.Max;++i){EmitPart(opt,prog);}return;}case 1:switch
(ast.Max){ case-1:case 0:idx=prog.Count;for(var i=0;i<ast.Exprs.Length;i++)if(null!=ast.Exprs[i])EmitPart(ast.Exprs[i],prog);inst=new int[3];inst[0]=Split;
prog.Add(inst);inst[1]=idx;inst[2]=prog.Count;if(ast.IsLazy){ var t=inst[1];inst[1]=inst[2];inst[2]=t;}return;case 1: for(var i=0;i<ast.Exprs.Length;i++)
if(null!=ast.Exprs[i])EmitPart(ast.Exprs[i],prog);return;default: rep=new Ast();rep.Min=0;rep.Max=ast.Max-1;rep.IsLazy=ast.IsLazy;rep.Exprs=ast.Exprs;
for(var i=0;i<ast.Exprs.Length;i++)if(null!=ast.Exprs[i])EmitPart(ast.Exprs[i],prog);EmitPart(rep,prog);return;}default: switch(ast.Max){ case-1:case 0:
for(var j=0;j<ast.Min;++j){for(var i=0;i<ast.Exprs.Length;i++)if(null!=ast.Exprs[i])EmitPart(ast.Exprs[i],prog);}rep=new Ast();rep.Kind=Ast.Star;rep.Exprs
=ast.Exprs;rep.IsLazy=ast.IsLazy;EmitPart(rep,prog);return;case 1: throw new NotImplementedException();default: for(var j=0;j<ast.Min;++j){for(var i=0;
i<ast.Exprs.Length;i++)if(null!=ast.Exprs[i])EmitPart(ast.Exprs[i],prog);}if(ast.Min==ast.Max)return;opt=new Ast();opt.Kind=Ast.Opt;opt.Exprs=ast.Exprs;
opt.IsLazy=ast.IsLazy;rep=new Ast();rep.Kind=Ast.Rep;rep.Min=rep.Max=ast.Max-ast.Min;EmitPart(rep,prog);return;}} throw new NotImplementedException();
}}static string _FmtLbl(int i){return string.Format("L{0,4:000#}",i);}public static string ToString(IEnumerable<int[]>prog){var sb=new StringBuilder();
var i=0;foreach(var inst in prog){sb.Append(_FmtLbl(i));sb.Append(": ");sb.AppendLine(ToString(inst));++i;}return sb.ToString();}static string _ToStr(char
 ch){return string.Concat('\"',_EscChar(ch),'\"');}static string _EscChar(char ch){switch(ch){case'.':case'/': case'(':case')':case'[':case']':case'<':
 case'>':case'|':case';': case'\'': case'\"':case'{':case'}':case'?':case'*':case'+':case'$':case'^':case'\\':return string.Concat("\\",ch.ToString());
case'\t':return"\\t";case'\n':return"\\n";case'\r':return"\\r";case'\0':return"\\0";case'\f':return"\\f";case'\v':return"\\v";case'\b':return"\\b";default:
if(!char.IsLetterOrDigit(ch)&&!char.IsSeparator(ch)&&!char.IsPunctuation(ch)&&!char.IsSymbol(ch)){return string.Concat("\\x",unchecked((ushort)ch).ToString("x4"));
}else return string.Concat(ch);}}public static string ToString(int[]inst){switch(inst[0]){case Split:var sb=new StringBuilder();sb.Append("split ");sb.Append(_FmtLbl(inst[1]));
for(var i=2;i<inst.Length;i++)sb.Append(", "+_FmtLbl(inst[i]));return sb.ToString();case Jmp:return"jmp "+_FmtLbl(inst[1]);case Char:if(2==inst.Length)
 return"char "+_ToStr((char)inst[1]);else return"char";case UCode:case NUCode:return(UCode==inst[0]?"ucode ":"nucode ")+inst[1];case Set:case NSet:sb=
new StringBuilder();if(Set==inst[0])sb.Append("set ");else sb.Append("nset ");for(var i=1;i<inst.Length-1;i++){if(1!=i)sb.Append(", ");if(inst[i]==inst[i
+1])sb.Append(_ToStr((char)inst[i]));else{sb.Append(_ToStr((char)inst[i]));sb.Append("..");sb.Append(_ToStr((char)inst[i+1]));}++i;}return sb.ToString();
case Any:return"any";case Match:return"match "+inst[1].ToString();case Save:return"save "+inst[1].ToString();default:throw new InvalidProgramException("The instruction is not valid");
}}internal static int[][]EmitLexer(params Ast[]expressions){var parts=new KeyValuePair<int,int[][]>[expressions.Length];for(var i=0;i<expressions.Length;++i)
{var l=new List<int[]>();EmitPart(expressions[i],l);parts[i]=new KeyValuePair<int,int[][]>(i,l.ToArray());}return EmitLexer(parts);}internal static int[][]
EmitLexer(IEnumerable<KeyValuePair<int,int[][]>>parts){var l=new List<KeyValuePair<int,int[][]>>(parts);var prog=new List<int[]>();int[]match,save; save
=new int[2];save[0]=Save;save[1]=0;prog.Add(save); var split=new int[l.Count+2];split[0]=Compiler.Split;prog.Add(split); for(int ic=l.Count,i=0;i<ic;++i)
{split[i+1]=prog.Count; Fixup(l[i].Value,prog.Count);prog.AddRange(l[i].Value); save=new int[2];save[0]=Save;save[1]=1;prog.Add(save); match=new int[2];
match[0]=Match;match[1]=l[i].Key;prog.Add(match);} split[split.Length-1]=prog.Count; var any=new int[1];any[0]=Any;prog.Add(any); save=new int[2];save[0]
=Save;save[1]=1;prog.Add(save); match=new int[2];match[0]=Match;match[1]=-1;prog.Add(match);return prog.ToArray();}internal static void SortRanges(int[]
ranges){var result=new List<KeyValuePair<int,int>>(ranges.Length/2);for(var i=0;i<ranges.Length-1;++i){var ch=ranges[i];++i;result.Add(new KeyValuePair<int,
int>(ch,ranges[i]));}result.Sort((x,y)=>{return x.Key.CompareTo(y.Key);});for(int ic=result.Count,i=0;i<ic;++i){var j=i*2;var kvp=result[i];ranges[j]=
kvp.Key;ranges[j+1]=kvp.Value;}}internal static void Fixup(int[][]program,int offset){for(var i=0;i<program.Length;i++){var inst=program[i];var op=inst[0];
switch(op){case Jmp:inst[1]+=offset;break;case Split:for(var j=1;j<inst.Length;j++)inst[j]+=offset;break;}}}}}namespace L{/// <summary>
/// Provides services for assembling and disassembling lexers, and for compiling regular expressions into lexers
/// </summary>
#if LLIB
public
#endif
static class Lex{public static int[]GetCharacterClass(string name){if(null==name)throw new ArgumentNullException(nameof(name));if(0==name.Length)throw
 new ArgumentException("The character class name must not be empty.",nameof(name));int[]result;if(!CharCls.CharacterClasses.TryGetValue(name,out result))
throw new ArgumentException("The character class "+name+" was not found",nameof(name));return result;}/// <summary>
/// Assembles the assembly code into a program
/// </summary>
/// <param name="asmCode">The code to assemble</param>
/// <returns>A program</returns>
public static int[][]Assemble(LexContext asmCode){return Assembler.Emit(Assembler.Parse(asmCode)).ToArray();}/// <summary>
/// Assembles the assembly code into a program
/// </summary>
/// <param name="asmCode">The code to assemble</param>
/// <returns>A program</returns>
public static int[][]Assemble(string asmCode){var lc=LexContext.Create(asmCode);return Assembler.Emit(Assembler.Parse(lc)).ToArray();}/// <summary>
/// Assembles the assembly code from the <see cref="TextReader"/>
/// </summary>
/// <param name="asmCodeReader">A reader that will read the assembly code</param>
/// <returns>A program</returns>
public static int[][]AssembleFrom(TextReader asmCodeReader){var lc=LexContext.CreateFrom(asmCodeReader);return Assembler.Emit(Assembler.Parse(lc)).ToArray();
}/// <summary>
/// Assembles the assembly code from the specified file
/// </summary>
/// <param name="asmFile">A file containing the assembly code</param>
/// <returns>A program</returns>
public static int[][]AssembleFrom(string asmFile){var lc=LexContext.CreateFrom(asmFile);return Assembler.Emit(Assembler.Parse(lc)).ToArray();}/// <summary>
/// Assembles the assembly code from the specified url
/// </summary>
/// <param name="asmUrl">An URL that points to the assembly code</param>
/// <returns>A program</returns>
public static int[][]AssembleFromUrl(string asmUrl){var lc=LexContext.CreateFromUrl(asmUrl);return Assembler.Emit(Assembler.Parse(lc)).ToArray();}/// <summary>
/// Compiles a single regular expression into a program segment
/// </summary>
/// <param name="input">The expression to compile</param>
/// <returns>A part of a program</returns>
public static int[][]CompileRegexPart(LexContext input){var ast=Ast.Parse(input);var prog=new List<int[]>();Compiler.EmitPart(ast,prog);return prog.ToArray();
}/// <summary>
/// Compiles a single regular expression into a program segment
/// </summary>
/// <param name="expression">The expression to compile</param>
/// <returns>A part of a program</returns>
public static int[][]CompileRegexPart(string expression){return CompileRegexPart(LexContext.Create(expression));}/// <summary>
/// Compiles a single literal expression into a program segment
/// </summary>
/// <param name="input">The expression to compile</param>
/// <returns>A part of a program</returns>
public static int[][]CompileLiteralPart(LexContext input){var ll=input.CaptureBuffer.Length;while(-1!=input.Current)input.Capture();return CompileLiteralPart(input.GetCapture(ll));
}/// <summary>
/// Compiles a single literal expression into a program segment
/// </summary>
/// <param name="expression">The expression to compile</param>
/// <returns>A part of a program</returns>
public static int[][]CompileLiteralPart(string expression){var prog=new List<int[]>();Compiler.EmitPart(expression,prog);return prog.ToArray();}/// <summary>
/// Compiles a series of regular expressions into a program
/// </summary>
/// <param name="expressions">The expressions</param>
/// <returns>A program</returns>
public static int[][]CompileLexerRegex(params string[]expressions){var asts=new Ast[expressions.Length];for(var i=0;i<expressions.Length;++i)asts[i]=Ast.Parse(LexContext.Create(expressions[i]));
return Compiler.EmitLexer(asts);}/// <summary>
/// Links a series of partial programs together into single lexer program
/// </summary>
/// <param name="parts">The parts</param>
/// <returns>A program</returns>
public static int[][]LinkLexerParts(IEnumerable<KeyValuePair<int,int[][]>>parts){return Compiler.EmitLexer(parts);}/// <summary>
/// Disassembles the specified program
/// </summary>
/// <param name="program">The program</param>
/// <returns>A string containing the assembly code for the program</returns>
public static string Disassemble(int[][]program){return Compiler.ToString(program);}/// <summary>
/// Indicates whether or not the program matches the entire input specified
/// </summary>
/// <param name="prog">The program</param>
/// <param name="input">The input to check</param>
/// <returns>True if the input was matched, otherwise false</returns>
public static bool IsMatch(int[][]prog,LexContext input){return-1!=Run(prog,input)&&input.Current==LexContext.EndOfInput;}/// <summary>
/// Runs the specified program over the specified input
/// </summary>
/// <param name="prog">The program to run</param>
/// <param name="input">The input to match</param>
/// <returns>The id of the match, or -1 for an error. <see cref="LexContext.CaptureBuffer"/> contains the captured value.</returns>
public static int Run(int[][]prog,LexContext input){input.EnsureStarted();int i,match=-1;_Fiber[]currentFibers,nextFibers,tmp;int currentFiberCount=0,
nextFiberCount=0;int[]pc; int sp=0; var sb=new StringBuilder(64);int[]saved,matched;saved=new int[2];currentFibers=new _Fiber[prog.Length];nextFibers=
new _Fiber[prog.Length];_EnqueueFiber(ref currentFiberCount,ref currentFibers,new _Fiber(prog,0,saved),0);matched=null;var cur=-1;if(LexContext.EndOfInput!=input.Current)
{var ch1=unchecked((char)input.Current);if(char.IsHighSurrogate(ch1)){if(-1==input.Advance())throw new ExpectingException("Expecting low surrogate in unicode stream. The input source is corrupt or not valid Unicode",input.Line,input.Column,input.Position,input.FileOrUrl)
;var ch2=unchecked((char)input.Current);cur=char.ConvertToUtf32(ch1,ch2);}else cur=ch1;}while(0<currentFiberCount){bool passed=false;for(i=0;i<currentFiberCount;
++i){var t=currentFibers[i];pc=t.Program[t.Index];saved=t.Saved;switch(pc[0]){case Compiler.Char:if(cur!=pc[1]){break;}goto case Compiler.Any;case Compiler.Set:
if(!_InRanges(pc,cur)){break;}goto case Compiler.Any;case Compiler.NSet:if(_InRanges(pc,cur)){break;}goto case Compiler.Any;case Compiler.UCode:var str
=char.ConvertFromUtf32(cur);if(unchecked((int)char.GetUnicodeCategory(str,0)!=pc[1])){break;}goto case Compiler.Any;case Compiler.NUCode:str=char.ConvertFromUtf32(cur);
if(unchecked((int)char.GetUnicodeCategory(str,0))==pc[1]){break;}goto case Compiler.Any;case Compiler.Any:if(LexContext.EndOfInput==input.Current){break;
}passed=true;_EnqueueFiber(ref nextFiberCount,ref nextFibers,new _Fiber(t,t.Index+1,saved),sp+1);break;case Compiler.Match:matched=saved;match=pc[1]; i
=currentFiberCount;break;}}if(passed){sb.Append(char.ConvertFromUtf32(cur));input.Advance();if(LexContext.EndOfInput!=input.Current){var ch1=unchecked((char)input.Current);
if(char.IsHighSurrogate(ch1)){input.Advance();if(-1==input.Advance())throw new ExpectingException("Expecting low surrogate in unicode stream. The input source is corrupt or not valid Unicode",
input.Line,input.Column,input.Position,input.FileOrUrl);++sp;var ch2=unchecked((char)input.Current);cur=char.ConvertToUtf32(ch1,ch2);}else cur=ch1;}++sp;
}tmp=currentFibers;currentFibers=nextFibers;nextFibers=tmp;currentFiberCount=nextFiberCount;nextFiberCount=0;}if(null!=matched){var start=matched[0]; var
 len=matched[1];input.CaptureBuffer.Append(sb.ToString(start,len-start));return match;};return-1;}static bool _InRanges(int[]pc,int ch){var found=false;
 for(var j=1;j<pc.Length;++j){ var first=pc[j];++j;var last=pc[j]; if(ch<=last){if(first<=ch)found=true;break;}}return found;}static void _EnqueueFiber(ref
 int lcount,ref _Fiber[]l,_Fiber t,int sp){ if(l.Length<=lcount){var newarr=new _Fiber[l.Length*2];Array.Copy(l,0,newarr,0,l.Length);l=newarr;}l[lcount]
=t;++lcount;var pc=t.Program[t.Index];switch(pc[0]){case Compiler.Jmp:_EnqueueFiber(ref lcount,ref l,new _Fiber(t,pc[1],t.Saved),sp);break;case Compiler.Split:
for(var j=1;j<pc.Length;j++)_EnqueueFiber(ref lcount,ref l,new _Fiber(t.Program,pc[j],t.Saved),sp);break;case Compiler.Save:var slot=pc[1];var max=slot
>t.Saved.Length?slot:t.Saved.Length;var saved=new int[max];for(var i=0;i<t.Saved.Length;++i)saved[i]=t.Saved[i];saved[slot]=sp;_EnqueueFiber(ref lcount,ref
 l,new _Fiber(t,t.Index+1,saved),sp);break;}}private struct _Fiber{public readonly int[][]Program;public readonly int Index;public int[]Saved;public _Fiber(int[][]
program,int index,int[]saved){Program=program;Index=index;Saved=saved;}public _Fiber(_Fiber fiber,int index,int[]saved){Program=fiber.Program;Index=index;
Saved=saved;}}}}