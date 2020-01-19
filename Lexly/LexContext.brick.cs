using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
namespace LC{partial class LexContext{/// <summary>
/// Attempts to read whitespace from the current input, capturing it
/// </summary>
/// <returns>True if whitespace was read, otherwise false</returns>
public bool TryReadWhiteSpace(){EnsureStarted();if(-1==Current||!char.IsWhiteSpace((char)Current))return false;Capture();while(-1!=Advance()&&char.IsWhiteSpace((char)Current))
Capture();return true;}/// <summary>
/// Attempts to skip whitespace in the current input without capturing it
/// </summary>
/// <returns>True if whitespace was skipped, otherwise false</returns>
public bool TrySkipWhiteSpace(){EnsureStarted();if(-1==Current||!char.IsWhiteSpace((char)Current))return false;while(-1!=Advance()&&char.IsWhiteSpace((char)Current))
;return true;}/// <summary>
/// Attempts to read up until the specified character, optionally consuming it
/// </summary>
/// <param name="character">The character to halt at</param>
/// <param name="readCharacter">True if the character should be consumed, otherwise false</param>
/// <returns>True if the character was found, otherwise false</returns>
public bool TryReadUntil(int character,bool readCharacter=true){EnsureStarted();if(0>character)character=-1;Capture();if(Current==character){return true;
}while(-1!=Advance()&&Current!=character)Capture(); if(Current==character){if(readCharacter){Capture();Advance();}return true;}return false;}/// <summary>
/// Attempts to skip up until the specified character, optionally consuming it
/// </summary>
/// <param name="character">The character to halt at</param>
/// <param name="skipCharacter">True if the character should be consumed, otherwise false</param>
/// <returns>True if the character was found, otherwise false</returns>
public bool TrySkipUntil(int character,bool skipCharacter=true){EnsureStarted();if(0>character)character=-1;if(Current==character)return true;while(-1
!=Advance()&&Current!=character);if(Current==character){if(skipCharacter)Advance();return true;}return false;}/// <summary>
/// Attempts to read up until the specified character, using the specified escape, optionally consuming it
/// </summary>
/// <param name="character">The character to halt at</param>
/// <param name="escapeChar">The escape indicator character to use</param>
/// <param name="readCharacter">True if the character should be consumed, otherwise false</param>
/// <returns>True if the character was found, otherwise false</returns>
public bool TryReadUntil(int character,int escapeChar,bool readCharacter=true){EnsureStarted();if(0>character)character=-1;if(-1==Current)return false;
if(Current==character){if(readCharacter){Capture();Advance();}return true;}do{if(escapeChar==Current){Capture();if(-1==Advance())return false;Capture();
}else{if(character==Current){if(readCharacter){Capture();Advance();}return true;}else Capture();}}while(-1!=Advance());return false;}/// <summary>
/// Attempts to skip up until the specified character, using the specified escape, optionally consuming it
/// </summary>
/// <param name="character">The character to halt at</param>
/// <param name="escapeChar">The escape indicator character to use</param>
/// <param name="skipCharacter">True if the character should be consumed, otherwise false</param>
/// <returns>True if the character was found, otherwise false</returns>
public bool TrySkipUntil(int character,int escapeChar,bool skipCharacter=true){EnsureStarted();if(0>character)character=-1;if(Current==character)return
 true;while(-1!=Advance()&&Current!=character){if(character==escapeChar)if(-1==Advance())break;}if(Current==character){if(skipCharacter)Advance();return
 true;}return false;}private static bool _ContainsChar(char[]chars,char ch){foreach(char cmp in chars)if(cmp==ch)return true;return false;}/// <summary>
/// Attempts to read until any of the specified characters, optionally consuming it
/// </summary>
/// <param name="readCharacter">True if the character should be consumed, otherwise false</param>
/// <param name="anyOf">A list of characters that signal the end of the scan</param>
/// <returns>True if one of the characters was found, otherwise false</returns>
public bool TryReadUntil(bool readCharacter=true,params char[]anyOf){EnsureStarted();if(null==anyOf)anyOf=Array.Empty<char>();Capture();if(-1!=Current
&&_ContainsChar(anyOf,(char)Current)){if(readCharacter){Capture();Advance();}return true;}while(-1!=Advance()&&!_ContainsChar(anyOf,(char)Current))Capture();
if(-1!=Current&&_ContainsChar(anyOf,(char)Current)){if(readCharacter){Capture();Advance();}return true;}return false;}/// <summary>
/// Attempts to skip until any of the specified characters, optionally consuming it
/// </summary>
/// <param name="skipCharacter">True if the character should be consumed, otherwise false</param>
/// <param name="anyOf">A list of characters that signal the end of the scan</param>
/// <returns>True if one of the characters was found, otherwise false</returns>
public bool TrySkipUntil(bool skipCharacter=true,params char[]anyOf){EnsureStarted();if(null==anyOf)anyOf=Array.Empty<char>();if(-1!=Current&&_ContainsChar(anyOf,
(char)Current)){if(skipCharacter)Advance();return true;}while(-1!=Advance()&&!_ContainsChar(anyOf,(char)Current));if(-1!=Current&&_ContainsChar(anyOf,
(char)Current)){if(skipCharacter)Advance();return true;}return false;}/// <summary>
/// Reads up to the specified text string, consuming it
/// </summary>
/// <param name="text">The text to read until</param>
/// <returns>True if the text was found, otherwise false</returns>
public bool TryReadUntil(string text){EnsureStarted();if(string.IsNullOrEmpty(text))return false;while(-1!=Current&&TryReadUntil(text[0],false)){bool found
=true;for(int i=1;i<text.Length;++i){if(Advance()!=text[i]){found=false;break;}Capture();}if(found){Advance();return true;}}return false;}/// <summary>
/// Skips up to the specified text string, consuming it
/// </summary>
/// <param name="text">The text to skip until</param>
/// <returns>True if the text was found, otherwise false</returns>
public bool TrySkipUntil(string text){EnsureStarted();if(string.IsNullOrEmpty(text))return false;while(-1!=Current&&TrySkipUntil(text[0],false)){bool found
=true;for(int i=1;i<text.Length;++i){if(Advance()!=text[i]){found=false;break;}}if(found){Advance();return true;}}return false;}/// <summary>
/// Attempts to read a series of digits, consuming them
/// </summary>
/// <returns>True if digits were consumed, otherwise false</returns>
public bool TryReadDigits(){EnsureStarted();if(-1==Current||!char.IsDigit((char)Current))return false;Capture();while(-1!=Advance()&&char.IsDigit((char)Current))
Capture();return true;}/// <summary>
/// Attempts to skip a series of digits, consuming them
/// </summary>
/// <returns>True if digits were consumed, otherwise false</returns>
public bool TrySkipDigits(){EnsureStarted();if(-1==Current||!char.IsDigit((char)Current))return false;while(-1!=Advance()&&char.IsDigit((char)Current))
;return true;}/// <summary>
/// Attempts to read a series of letters, consuming them
/// </summary>
/// <returns>True if letters were consumed, otherwise false</returns>
public bool TryReadLetters(){EnsureStarted();if(-1==Current||!char.IsLetter((char)Current))return false;Capture();while(-1!=Advance()&&char.IsLetter((char)Current))
Capture();return true;}/// <summary>
/// Attempts to skip a series of letters, consuming them
/// </summary>
/// <returns>True if letters were consumed, otherwise false</returns>
public bool TrySkipLetters(){EnsureStarted();if(-1==Current||!char.IsLetter((char)Current))return false;while(-1!=Advance()&&char.IsLetter((char)Current))
;return true;}/// <summary>
/// Attempts to read a series of letters or digits, consuming them
/// </summary>
/// <returns>True if letters or digits were consumed, otherwise false</returns>
public bool TryReadLettersOrDigits(){EnsureStarted();if(-1==Current||!char.IsLetterOrDigit((char)Current))return false;Capture();while(-1!=Advance()&&
char.IsLetterOrDigit((char)Current))Capture();return true;}/// <summary>
/// Attempts to skip a series of letters or digits, consuming them
/// </summary>
/// <returns>True if letters or digits were consumed, otherwise false</returns>
public bool TrySkipLettersOrDigits(){EnsureStarted();if(-1==Current||!char.IsLetterOrDigit((char)Current))return false;while(-1!=Advance()&&char.IsLetterOrDigit((char)Current))
;return true;}}}namespace LC{/// <summary>
/// Represents an exception encountered while lexing or parsing from an input source
/// </summary>
#if LCLIB
public
#endif
sealed class ExpectingException:Exception{/// <summary>
/// Creates a new <see cref="ExpectingException" />
/// </summary>
/// <param name="message">The error message - this will be appended with the location information</param>
/// <param name="line">The one based line number where the exception occured</param>
/// <param name="column">The one based column number where the exception occured</param>
/// <param name="position">The zero based position where the exception occured</param>
/// <param name="fileOrUrl">The file or URL in which the exception occured</param>
/// <param name="expecting">A list of expected symbols or characters</param>
public ExpectingException(string message,int line,int column,long position,string fileOrUrl,params string[]expecting):base(_GetMessage(message,line,column,position,fileOrUrl))
{Line=line;Column=column;Position=position;FileOrUrl=fileOrUrl;Expecting=expecting;}/// <summary>
/// Indicates the one based line where the exception occured
/// </summary>
public int Line{get;}/// <summary>
/// Indicates the one based column where the exception occured
/// </summary>
public int Column{get;}/// <summary>
/// Indicates the zero based position where the exception occured
/// </summary>
public long Position{get;}/// <summary>
/// Indicates the file or URL in which the exception occured
/// </summary>
public string FileOrUrl{get;}/// <summary>
/// Indicates a list of expecting characters or symbols
/// </summary>
public string[]Expecting{get;}static string _GetMessage(string m,int l,int c,long p,string f){var sb=new StringBuilder();sb.Append(m);sb.Append(" at line ");
sb.Append(l.ToString());sb.Append(", column ");sb.Append(c.ToString());sb.Append(", position ");sb.Append(p.ToString());if(!string.IsNullOrEmpty(f)){sb.Append(", in ");
sb.Append(f);}return sb.ToString();}}/// <summary>
/// Provides error reporting, location tracking, lifetime and start/end management over an input cursor
/// </summary>
public abstract partial class LexContext:IDisposable{/// <summary>
/// Indicates the default tab width of an input device
/// </summary>
public const int DefaultTabWidth=4;/// <summary>
/// Represents the end of input symbol
/// </summary>
public const int EndOfInput=-1;/// <summary>
/// Represents the before input symbol
/// </summary>
public const int BeforeInput=-2;/// <summary>
/// Represents a symbol for the disposed state
/// </summary>
public const int Disposed=-3;int _current=BeforeInput;int _line;int _column;long _position;string _fileOrUrl;/// <summary>
/// Indicates the tab width of the input device
/// </summary>
public int TabWidth{get;set;}=DefaultTabWidth;/// <summary>
/// Indicates the current one based line number
/// </summary>
public int Line{get{return _line;}}/// <summary>
/// Indicates the current one based column number
/// </summary>
public int Column{get{return _column;}}/// <summary>
/// Indicates the current zero based position
/// </summary>
public long Position{get{return _position;}}/// <summary>
/// Indicates the current filename or URL, if any could be discerned
/// </summary>
public string FileOrUrl{get{return _fileOrUrl;}}/// <summary>
/// Provides access to the capture buffer, a <see cref="StringBuilder" />
/// </summary>
public StringBuilder CaptureBuffer{get;}=new StringBuilder();/// <summary>
/// Gets the current character under the cursor or <see cref="BeforeInput"/>, <see cref="EndOfInput" />, or <see cref="Disposed" />
/// </summary>
public int Current{get{return _current;}}internal LexContext(){_line=1;_column=0;_position=0L;}~LexContext(){Close();}/// <summary>
/// Creates a <see cref="LexContext" /> over an enumeration of characters, which can be a string, character array, or other source
/// </summary>
/// <param name="input">The input characters</param>
/// <returns>A new <see cref="LexContext" /> over the input</returns>
public static LexContext Create(IEnumerable<char>input){return new _CharEnumeratorLexContext(input);}/// <summary>
/// Creates a <see cref="LexContext" /> over a <see cref="TextReader"/>
/// </summary>
/// <param name="input">The input reader</param>
/// <returns>A new <see cref="LexContext" /> over the input</returns>
public static LexContext CreateFrom(TextReader input){ string fn=null;var sr=input as StreamReader;if(null!=sr){var fstm=sr.BaseStream as FileStream;if(null!=fstm)
{fn=fstm.Name;}}var result=new _TextReaderLexContext(input);if(!string.IsNullOrEmpty(fn))result._fileOrUrl=fn;return result;}/// <summary>
/// Creates a <see cref="LexContext" /> over a file
/// </summary>
/// <param name="filename">The file</param>
/// <returns>A new <see cref="LexContext" /> over the file</returns>
public static LexContext CreateFrom(string filename){return CreateFrom(new StreamReader(filename));}/// <summary>
/// Creates a <see cref="LexContext" /> over an URL
/// </summary>
/// <param name="url">The URL</param>
/// <returns>A new <see cref="LexContext" /> over the URL</returns>
public static LexContext CreateFromUrl(string url){var wreq=WebRequest.Create(url);var wrsp=wreq.GetResponse();var result=CreateFrom(new StreamReader(wrsp.GetResponseStream()));
result._fileOrUrl=url;return result;}/// <summary>
/// Closes the current instance and releases any resources being held
/// </summary>
public void Close(){if(Disposed!=_current){_current=Disposed;GC.SuppressFinalize(this);CloseInner();CaptureBuffer.Clear();}}/// <summary>
/// Sets the location information for this instance
/// </summary>
/// <param name="line">The one based line number</param>
/// <param name="column">The one based column number</param>
/// <param name="position">The zero based position</param>
/// <param name="fileOrUrl">The file or URL</param>
public void SetLocation(int line,int column,long position,string fileOrUrl){_line=line;_column=column;_position=position;_fileOrUrl=fileOrUrl;}/// <summary>
/// Gets all or a subset of the current capture buffer
/// </summary>
/// <param name="startIndex">The start index</param>
/// <param name="length">The number of characters to retrieve, or zero to retrieve the remainder of the buffer</param>
/// <returns>A string containing the specified subset of the capture buffer</returns>
public string GetCapture(int startIndex=0,int length=0){_CheckDisposed();if(0==length)length=CaptureBuffer.Length-startIndex;return CaptureBuffer.ToString(startIndex,
length);}/// <summary>
/// Clears the capture buffer
/// </summary>
public void ClearCapture(){_CheckDisposed();CaptureBuffer.Clear();}/// <summary>
/// Captures the current character under the cursor, if any
/// </summary>
public void Capture(){_CheckDisposed();if(EndOfInput!=_current&&BeforeInput!=_current)CaptureBuffer.Append((char)_current);}/// <summary>
/// Verifies that one of the specified characters is under the input cursor. If it isn't, a <see cref="ExpectingException" /> is raised.
/// </summary>
/// <param name="expecting">The list of expected characters. If empty, anything but end of input is accepted. If <see cref="EndOfInput" /> is included, end of input is accepted.</param>
[System.Diagnostics.DebuggerHidden()]public void Expecting(params int[]expecting){_CheckDisposed();if(BeforeInput==_current)throw new ExpectingException("The cursor is before the beginning of the input",
_line,_column,_position,_fileOrUrl);switch(expecting.Length){case 0:if(EndOfInput==_current)throw new ExpectingException("Unexpected end of input",_line,_column,_position,_fileOrUrl);
break;case 1:if(expecting[0]!=_current)throw new ExpectingException(_GetErrorMessage(expecting),_line,_column,_position,_fileOrUrl,_GetErrorExpecting(expecting));
break;default:if(0>Array.IndexOf(expecting,_current))throw new ExpectingException(_GetErrorMessage(expecting),_line,_column,_position,_fileOrUrl,_GetErrorExpecting(expecting));
break;}}string[]_GetErrorExpecting(int[]expecting){var result=new string[expecting.Length];for(var i=0;i<expecting.Length;i++){if(-1!=expecting[i])result[i]
=Convert.ToString(expecting[i]);else result[i]="end of input";}return result;}string _GetErrorMessage(int[]expecting){StringBuilder sb=null;switch(expecting.Length)
{case 0:break;case 1:sb=new StringBuilder();if(-1==expecting[0])sb.Append("end of input");else{sb.Append("\"");sb.Append((char)expecting[0]);sb.Append("\"");
}break;case 2:sb=new StringBuilder();if(-1==expecting[0])sb.Append("end of input");else{sb.Append("\"");sb.Append((char)expecting[0]);sb.Append("\"");
}sb.Append(" or ");if(-1==expecting[1])sb.Append("end of input");else{sb.Append("\"");sb.Append((char)expecting[1]);sb.Append("\"");}break;default: sb
=new StringBuilder();if(-1==expecting[0])sb.Append("end of input");else{sb.Append("\"");sb.Append((char)expecting[0]);sb.Append("\"");}int l=expecting.Length
-1;int i=1;for(;i<l;++i){sb.Append(", ");if(-1==expecting[i])sb.Append("end of input");else{sb.Append("\"");sb.Append((char)expecting[i]);sb.Append("\"");
}}sb.Append(", or ");if(-1==expecting[i])sb.Append("end of input");else{sb.Append("\"");sb.Append((char)expecting[i]);sb.Append("\"");}break;}if(-1==Current)
{if(0==expecting.Length)return"Unexpected end of input";return string.Concat("Unexpected end of input. Expecting ",sb.ToString());}if(0==expecting.Length)
return string.Concat("Unexpected character \"",(char)Current,"\" in input");return string.Concat("Unexpected character \"",(char)Current,"\" in input. Expecting ",
sb.ToString());}void IDisposable.Dispose(){Close();}public void EnsureStarted(){_CheckDisposed();if(BeforeInput==_current)Advance();}public int Advance()
{_CheckDisposed();if(EndOfInput==_current)return EndOfInput;_current=AdvanceInner();switch(_current){case'\n':++_line;_column=0;break;case'\r':_column
=0;break;case'\t':_column+=TabWidth;break;default: if(!char.IsHighSurrogate(unchecked((char)_current)))++_column;break;}++_position;return _current;}protected
 abstract int AdvanceInner();protected abstract void CloseInner();void _CheckDisposed(){if(Disposed==_current)throw new ObjectDisposedException(typeof(LexContext).Name);
}
#region _TextReaderLexContext
private sealed class _TextReaderLexContext:LexContext{TextReader _inner;internal _TextReaderLexContext(TextReader inner){_inner=inner;}protected override
 int AdvanceInner(){return _inner.Read();}protected override void CloseInner(){_inner.Close();}}
#endregion
#region _CharEnumeratorLexContext
private sealed class _CharEnumeratorLexContext:LexContext{IEnumerator<char>_inner;internal _CharEnumeratorLexContext(IEnumerable<char>inner){_inner=inner.GetEnumerator();
}protected override int AdvanceInner(){if(!_inner.MoveNext())return EndOfInput;return _inner.Current;}protected override void CloseInner(){_inner.Dispose();
}}
#endregion
}}