//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace LexlyDemo {
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    
    ///  <summary>
    ///  Reference implementation for generated shared code
    ///  </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Lexly", "0.1.0.0")]
    internal struct Token {
        ///  <summary>
        ///  Indicates the line where the token occurs
        ///  </summary>
        public int Line;
        ///  <summary>
        ///  Indicates the column where the token occurs
        ///  </summary>
        public int Column;
        ///  <summary>
        ///  Indicates the position where the token occurs
        ///  </summary>
        public long Position;
        ///  <summary>
        ///  Indicates the symbol id or -1 for the error symbol
        ///  </summary>
        public int SymbolId;
        ///  <summary>
        ///  Indicates the value of the token
        ///  </summary>
        public string Value;
        ///  <summary>
        ///  Always null in Lexly
        ///  </summary>
        public Token[] Skipped;
    }
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Lexly", "0.1.0.0")]
    internal partial class Tokenizer : object, IEnumerable<Token> {
        public const int DefaultTabWidth = 4;
        public const int ErrorSymbol = -1;
        private int[][] _program;
        private string[] _blockEnds;
        //  our node flags. Currently only used for the hidden attribute
        private int[] _nodeFlags;
        private IEnumerable<char> _input;
        private int _tabWidth;
        private int _line;
        private int _column;
        private long _position;
        public Tokenizer(int[][] program, string[] blockEnds, int[] nodeFlags, IEnumerable<char> input) : 
                this(program, blockEnds, nodeFlags, input, 1, 0, 0, Tokenizer.DefaultTabWidth) {
        }
        public Tokenizer(int[][] program, string[] blockEnds, int[] nodeFlags, IEnumerable<char> input, int line, int column, long position, int tabWidth) {
            this._program = program;
            this._blockEnds = blockEnds;
            this._nodeFlags = nodeFlags;
            this._input = input;
            this._line = line;
            this._column = column;
            this._position = position;
            this._tabWidth = tabWidth;
        }
        public IEnumerator<Token> GetEnumerator() {
            return new TokenizerEnumerator(this._program, this._blockEnds, this._nodeFlags, this._input.GetEnumerator(), this._tabWidth, this._line, this._column, this._position);
        }
        IEnumerator IEnumerable.GetEnumerator() {
            return this.GetEnumerator();
        }
    }
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Lexly", "0.1.0.0")]
    internal class TokenizerEnumerator : object, IEnumerator<Token> {
        public const int ErrorSymbol = -1;
        private const int _EosSymbol = -2;
        #region Opcodes
        const int _Match = 1;
        //  match symbol
        // const int _Jmp = 2; // jmp addr
        const int _Jmp = 2;
        //  jmp addr1 {, addrN }
        const int _Any = 4;
        //  any
        const int _Char = 5;
        //  char ch
        const int _Set = 6;
        //  set packedRange1Left,packedRange1Right,packedRange2Left,packedRange2Right...
        const int _NSet = 7;
        //  nset packedRange1Left,packedRange1Right,packedRange2Left,packedRange2Right...
        const int _UCode = 8;
        //  ucode cat
        const int _NUCode = 9;
        //  nucode cat
        const int _Save = 10;
        #endregion
        //  save slot
        private const int _Disposed = -3;
        private const int _BeforeBegin = -2;
        private const int _EndOfInput = -1;
        private int[][] _program;
        private string[] _blockEnds;
        private int[] _nodeFlags;
        private TokenizerFiber[] _currentFibers;
        private TokenizerFiber[] _nextFibers;
        private IEnumerator<char> _input;
        private StringBuilder _capture;
        private int _state = TokenizerEnumerator._BeforeBegin;
        private int _ch;
        private int _line;
        private int _column;
        private long _position;
        private int _oline;
        private int _ocolumn;
        private long _oposition;
        private string _value;
        private int _tabWidth;
        private Token _current;
        public TokenizerEnumerator(int[][] program, string[] blockEnds, int[] nodeFlags, IEnumerator<char> input, int tabWidth, int line, int column, long position) {
            this._program = program;
            this._blockEnds = blockEnds;
            this._nodeFlags = nodeFlags;
            this._input = input;
            this._tabWidth = tabWidth;
            this._line = line;
            this._column = column;
            this._position = position;
            this._oline = line;
            this._ocolumn = column;
            this._oposition = position;
            this._capture = new StringBuilder(64);
            this._currentFibers = new TokenizerFiber[this._program.Length];
            this._nextFibers = new TokenizerFiber[this._program.Length];
            this._value = null;
            this._ch = TokenizerEnumerator._BeforeBegin;
            this._current.SymbolId = -1;
        }
        public Token Current {
            get {
                if ((0 > this._state)) {
                    if ((TokenizerEnumerator._BeforeBegin == this._state)) {
                        throw new InvalidOperationException("The cursor is before the beginning of the input.");
                    }
                    if ((TokenizerEnumerator._EndOfInput == this._state)) {
                        throw new InvalidOperationException("The cursor is after the end of the input.");
                    }
                    throw new ObjectDisposedException("TokenizerEnumerator");
                }
                return this._current;
            }
        }
        object IEnumerator.Current {
            get {
                return this.Current;
            }
        }
        void IDisposable.Dispose() {
            if ((false 
                        == (TokenizerEnumerator._Disposed == this._state))) {
                this._input.Dispose();
                this._ch = TokenizerEnumerator._Disposed;
                this._state = TokenizerEnumerator._Disposed;
            }
        }
        bool IEnumerator.MoveNext() {
            if ((0 > this._state)) {
                if ((TokenizerEnumerator._EndOfInput == this._state)) {
                    return false;
                }
                if ((TokenizerEnumerator._Disposed == this._state)) {
                    throw new ObjectDisposedException("TokenizerEnumerator");
                }
            }
            if ((this._ch == TokenizerEnumerator._EndOfInput)) {
                this._state = TokenizerEnumerator._EndOfInput;
                return false;
            }
            this._current = default(Token);
            this._current.Line = this._line;
            this._current.Column = this._column;
            this._current.Position = this._position;
            this._current.Skipped = null;
            this._capture.Clear();
            this._current.SymbolId = this._Lex();
            this._current.Value = this._value;
            bool done = false;
            for (
            ; (false == done); 
            ) {
                done = true;
                if ((TokenizerEnumerator.ErrorSymbol < this._current.SymbolId)) {
                    string be = this._blockEnds[this._current.SymbolId];
                    if (((null != be) 
                                && (false 
                                == (0 == be.Length)))) {
                        if ((false == this._TryReadUntilBlockEnd(be))) {
                            this._current.SymbolId = TokenizerEnumerator.ErrorSymbol;
                        }
                    }
                    if (((TokenizerEnumerator.ErrorSymbol < this._current.SymbolId) 
                                && (false 
                                == (0 
                                == (this._nodeFlags[this._current.SymbolId] & 1))))) {
                        done = false;
                        if ((this._ch == TokenizerEnumerator._EndOfInput)) {
                            this._state = TokenizerEnumerator._EndOfInput;
                            return false;
                        }
                        this._current.Line = this._line;
                        this._current.Column = this._column;
                        this._current.Position = this._position;
                        this._capture.Clear();
                        this._current.SymbolId = this._Lex();
                    }
                }
            }
            this._current.Value = this._value;
            return (false 
                        == (TokenizerEnumerator._EndOfInput == this._state));
        }
        //  moves to the next position, updates the state accordingly, and tracks the cursor position
        bool _MoveNextInput() {
            if ((this._ch == TokenizerEnumerator._EndOfInput)) {
                return false;
            }
            if (this._input.MoveNext()) {
                if ((false 
                            == (TokenizerEnumerator._BeforeBegin == this._state))) {
                    this._position = (this._position + 1);
                    if (('\n' == this._input.Current)) {
                        this._column = 1;
                        this._line = (this._line + 1);
                    }
                    else {
                        if (('\t' == this._input.Current)) {
                            this._column = (this._column + this._tabWidth);
                        }
                        else {
                            this._column = (this._column + 1);
                        }
                    }
                }
                else {
                    if (('\n' == this._input.Current)) {
                        this._column = 1;
                        this._line = (this._line + 1);
                    }
                    else {
                        if (('\t' == this._input.Current)) {
                            this._column = (this._column 
                                        + (this._tabWidth - 1));
                        }
                    }
                }
                this._ch = this._input.Current;
                this._state = 0;
                return true;
            }
            this._ch = TokenizerEnumerator._EndOfInput;
            return false;
        }
        //  reads until the specified character, consuming it, returning false if it wasn't found
        bool _TryReadUntil(char character) {
            char ch = this._input.Current;
            this._capture.Append(ch);
            if ((ch == character)) {
                return true;
            }
            for (
            ; (this._MoveNextInput() 
                        && (false 
                        == (this._input.Current == character))); 
            ) {
                this._capture.Append(this._input.Current);
            }
            if ((false 
                        == (this._ch == TokenizerEnumerator._EndOfInput))) {
                this._capture.Append(this._input.Current);
                return (this._input.Current == character);
            }
            return false;
        }
        //  reads until the string is encountered, capturing it.
        bool _TryReadUntilBlockEnd(string blockEnd) {
            int ll = this._capture.Length;
            for (
            ; ((false 
                        == (TokenizerEnumerator._EndOfInput == this._ch)) 
                        && this._TryReadUntil(blockEnd[0])); 
            ) {
                bool found = true;
                for (int i = 1; (found 
                            && (i < blockEnd.Length)); i = (i + 1)) {
                    if (((false == this._MoveNextInput()) 
                                || (false 
                                == (this._input.Current == blockEnd[i])))) {
                        found = false;
                    }
                    else {
                        if ((false 
                                    == (TokenizerEnumerator._EndOfInput == this._ch))) {
                            this._capture.Append(this._input.Current);
                        }
                    }
                }
                if (found) {
                    this._MoveNextInput();
                    this._value = this._capture.ToString(ll, (this._capture.Length - ll));
                    return true;
                }
            }
            return false;
        }
        void IEnumerator.Reset() {
            if ((false 
                        == (TokenizerEnumerator._BeforeBegin == this._state))) {
                if ((TokenizerEnumerator._Disposed == this._state)) {
                    throw new ObjectDisposedException("TokenizerEnumerator");
                }
                this._input.Reset();
                this._ch = TokenizerEnumerator._BeforeBegin;
                this._line = this._oline;
                this._column = this._ocolumn;
                this._position = this._oposition;
                this._state = TokenizerEnumerator._BeforeBegin;
            }
        }
        static void _EnqueueFiber(ref int lcount, ref TokenizerFiber[] l, TokenizerFiber t, int sp) {
            if ((l.Length <= lcount)) {
                TokenizerFiber[] newarr = new TokenizerFiber[(l.Length * 2)];
                System.Array.Copy(l, 0, newarr, 0, l.Length);
                l = newarr;
            }
            l[lcount] = t;
            lcount = (lcount + 1);
            int[] pc = t.Program[t.Index];
            int op = pc[0];
            if ((TokenizerEnumerator._Jmp == op)) {
                for (int j = 1; (j < pc.Length); j = (j + 1)) {
                    TokenizerEnumerator._EnqueueFiber(ref lcount, ref l, new TokenizerFiber(t.Program, pc[j], t.Saved), sp);
                }
                return;
            }
            if ((TokenizerEnumerator._Save == op)) {
                int slot = pc[1];
                int max = t.Saved.Length;
                if ((slot > max)) {
                    max = slot;
                }
                int[] saved = new int[max];
                for (int i = 0; (i < t.Saved.Length); i = (i + 1)) {
                    saved[i] = t.Saved[i];
                }
                saved[slot] = sp;
                TokenizerEnumerator._EnqueueFiber(ref lcount, ref l, new TokenizerFiber(t, (t.Index + 1), saved), sp);
                return;
            }
        }
        static bool _InRanges(int[] pc, int ch) {
            bool found = false;
            for (int j = 1; (j < pc.Length); j = (j + 1)) {
                int first = pc[j];
                j = (j + 1);
                int last = pc[j];
                if ((ch <= last)) {
                    if ((first <= ch)) {
                        found = true;
                    }
                    j = pc.Length;
                }
            }
            return found;
        }
        int _Lex() {
            this._capture.Clear();
            if ((this._state == TokenizerEnumerator._BeforeBegin)) {
                this._MoveNextInput();
            }
            int i;
            int match = -1;
            TokenizerFiber[] tmp;
            int currentFiberCount = 0;
            int nextFiberCount = 0;
            int[] pc;
            int sp = 0;
            int[] saved;
            int[] matched;
            saved = new int[2];
            TokenizerEnumerator._EnqueueFiber(ref currentFiberCount, ref this._currentFibers, new TokenizerFiber(this._program, 0, saved), 0);
            matched = null;
            int cur = -1;
            if ((false 
                        == (TokenizerEnumerator._EndOfInput == this._ch))) {
                char ch1 = ((char)(this._ch));
                if (char.IsHighSurrogate(ch1)) {
                    if ((false == this._MoveNextInput())) {
                        throw new IOException(string.Format("Expecting low surrogate in unicode stream. The input source is corrupt or not val" +
                                    "id Unicode at line {0}, column {1}, position {2}", this._line, this._column, this._position));
                    }
                    this._column = (this._column - 1);
                    char ch2 = ((char)(this._ch));
                    cur = char.ConvertToUtf32(ch1, ch2);
                }
                else {
                    cur = ch1;
                }
            }
            else {
                cur = -1;
            }
            for (
            ; (0 < currentFiberCount); 
            ) {
                bool passed = false;
                for (i = 0; (i < currentFiberCount); i = (i + 1)) {
                    TokenizerFiber t = this._currentFibers[i];
                    pc = t.Program[t.Index];
                    saved = t.Saved;
                    int op = pc[0];
                    if ((TokenizerEnumerator._Char == op)) {
                        if ((cur == pc[1])) {
                            passed = true;
                            TokenizerEnumerator._EnqueueFiber(ref nextFiberCount, ref this._nextFibers, new TokenizerFiber(t, (t.Index + 1), saved), (sp + 1));
                        }
                    }
                    else {
                        if ((TokenizerEnumerator._Set == op)) {
                            if (TokenizerEnumerator._InRanges(pc, cur)) {
                                passed = true;
                                TokenizerEnumerator._EnqueueFiber(ref nextFiberCount, ref this._nextFibers, new TokenizerFiber(t, (t.Index + 1), saved), (sp + 1));
                            }
                        }
                        else {
                            if ((TokenizerEnumerator._NSet == op)) {
                                if (((false == TokenizerEnumerator._InRanges(pc, cur)) 
                                            && (false 
                                            == (TokenizerEnumerator._EndOfInput == this._ch)))) {
                                    passed = true;
                                    TokenizerEnumerator._EnqueueFiber(ref nextFiberCount, ref this._nextFibers, new TokenizerFiber(t, (t.Index + 1), saved), (sp + 1));
                                }
                            }
                            else {
                                if ((TokenizerEnumerator._UCode == op)) {
                                    string str = char.ConvertFromUtf32(cur);
                                    if ((((int)(char.GetUnicodeCategory(str, 0))) == pc[1])) {
                                        passed = true;
                                        TokenizerEnumerator._EnqueueFiber(ref nextFiberCount, ref this._nextFibers, new TokenizerFiber(t, (t.Index + 1), saved), (sp + 1));
                                    }
                                }
                                else {
                                    if ((TokenizerEnumerator._NUCode == op)) {
                                        string str = char.ConvertFromUtf32(cur);
                                        if (((false 
                                                    == (((int)(char.GetUnicodeCategory(str, 0))) == pc[1])) 
                                                    && (false 
                                                    == (TokenizerEnumerator._EndOfInput == this._ch)))) {
                                            passed = true;
                                            TokenizerEnumerator._EnqueueFiber(ref nextFiberCount, ref this._nextFibers, new TokenizerFiber(t, (t.Index + 1), saved), (sp + 1));
                                        }
                                    }
                                    else {
                                        if ((TokenizerEnumerator._Any == op)) {
                                            if ((false 
                                                        == (TokenizerEnumerator._EndOfInput == this._ch))) {
                                                passed = true;
                                                TokenizerEnumerator._EnqueueFiber(ref nextFiberCount, ref this._nextFibers, new TokenizerFiber(t, (t.Index + 1), saved), (sp + 1));
                                            }
                                        }
                                        else {
                                            if ((TokenizerEnumerator._Match == op)) {
                                                matched = saved;
                                                match = pc[1];
                                                i = currentFiberCount;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                if (passed) {
                    this._capture.Append(char.ConvertFromUtf32(cur));
                    this._MoveNextInput();
                    if ((false 
                                == (TokenizerEnumerator._EndOfInput == this._ch))) {
                        char ch1 = ((char)(this._ch));
                        if (char.IsHighSurrogate(ch1)) {
                            if ((false == this._MoveNextInput())) {
                                throw new IOException(string.Format("Expecting low surrogate in unicode stream. The input source is corrupt or not val" +
                                            "id Unicode at line {0}, column {1}, position {2}", this._line, this._column, this._position));
                            }
                            this._column = (this._column - 1);
                            sp = (sp + 1);
                            char ch2 = ((char)(this._ch));
                            cur = char.ConvertToUtf32(ch1, ch2);
                        }
                        else {
                            cur = ch1;
                        }
                    }
                    else {
                        cur = -1;
                    }
                    sp = (sp + 1);
                }
                tmp = this._currentFibers;
                this._currentFibers = this._nextFibers;
                this._nextFibers = tmp;
                currentFiberCount = nextFiberCount;
                nextFiberCount = 0;
            }
            if ((null != matched)) {
                int start = matched[0];
                int len = matched[1];
                this._value = this._capture.ToString(start, (len - start));
                return match;
            }
            return -1;
        }
    }
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Lexly", "0.1.0.0")]
    internal struct TokenizerFiber {
        public int[][] Program;
        public int Index;
        public int[] Saved;
        public TokenizerFiber(int[][] program, int index, int[] saved) {
            this.Program = program;
            this.Index = index;
            this.Saved = saved;
        }
        public TokenizerFiber(TokenizerFiber fiber, int index, int[] saved) {
            this.Program = fiber.Program;
            this.Index = index;
            this.Saved = saved;
        }
    }
    internal partial class ExampleTokenizer : Tokenizer {
        internal static int[][] Program = new int[][] {
                new int[] {
                        10,
                        0},
                new int[] {
                        2,
                        2,
                        8,
                        15,
                        18},
                new int[] {
                        6,
                        65,
                        90,
                        95,
                        95,
                        97,
                        122},
                new int[] {
                        2,
                        4,
                        6},
                new int[] {
                        6,
                        48,
                        57,
                        65,
                        90,
                        95,
                        95,
                        97,
                        122},
                new int[] {
                        2,
                        3},
                new int[] {
                        10,
                        1},
                new int[] {
                        1,
                        0},
                new int[] {
                        2,
                        9,
                        11},
                new int[] {
                        3,
                        48,
                        48,
                        -1,
                        10},
                new int[] {
                        3,
                        48,
                        48,
                        -1,
                        10},
                new int[] {
                        3,
                        45,
                        45,
                        -1,
                        12,
                        -2,
                        12},
                new int[] {
                        3,
                        45,
                        45,
                        -1,
                        12,
                        -2,
                        12},
                new int[] {
                        10,
                        1},
                new int[] {
                        1,
                        1},
                new int[] {
                        6,
                        9,
                        9,
                        10,
                        10,
                        11,
                        11,
                        12,
                        12,
                        13,
                        13,
                        32,
                        32},
                new int[] {
                        10,
                        1},
                new int[] {
                        1,
                        2},
                new int[] {
                        4},
                new int[] {
                        10,
                        1},
                new int[] {
                        1,
                        -1}};
        internal static string[] BlockEnds = new string[] {
                null,
                null,
                null};
        internal static int[] NodeFlags = new int[] {
                0,
                0,
                0};
        public ExampleTokenizer(IEnumerable<char> input) : 
                base(ExampleTokenizer.Program, ExampleTokenizer.BlockEnds, ExampleTokenizer.NodeFlags, input) {
        }
        public const int id = 0;
        public const int @int = 1;
        public const int space = 2;
    }
}
