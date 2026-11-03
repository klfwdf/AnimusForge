using System;
using System.IO;
using System.Text;

namespace AnimusForge.ReplyTool;

// Linear per-response decoder. No reparsing of the accumulated argument string.
internal sealed class ReplyStringDecoder
{
    private int _stage, _keyPosition, _escape, _digits, _unicode;
    private char _high;
    internal bool Complete => _stage == 8;
    internal string Push(string fragment)
    {
        var output = new StringBuilder();
        foreach (char c in fragment)
        {
            if (_stage != 6)
            {
                if ((c == ' ' || c == '\t' || c == '\r' || c == '\n') && _stage != 2) continue;
                switch (_stage)
                {
                    case 0: Expect(c, '{'); _stage = 1; break;
                    case 1: Expect(c, '"'); _stage = 2; break;
                    case 2:
                        if (_keyPosition < 7) Expect(c, "content"[_keyPosition++]);
                        else { Expect(c, '"'); _stage = 4; }
                        break;
                    case 4: Expect(c, ':'); _stage = 5; break;
                    case 5: Expect(c, '"'); _stage = 6; break;
                    case 7: Expect(c, '}'); _stage = 8; break;
                    default: throw Bad();
                }
                continue;
            }
            if (_escape == 2)
            {
                int digit = c >= '0' && c <= '9' ? c - '0' : c >= 'a' && c <= 'f' ? c - 'a' + 10 : c >= 'A' && c <= 'F' ? c - 'A' + 10 : -1;
                if (digit < 0) throw Bad();
                _unicode = (_unicode << 4) | digit;
                if (++_digits == 4) { Emit((char)_unicode, output); _escape = 0; }
                continue;
            }
            if (_escape == 1)
            {
                _escape = 0;
                switch (c)
                {
                    case 'u': _escape = 2; _digits = 0; _unicode = 0; break;
                    case '"': case '\\': case '/': Emit(c, output); break;
                    case 'b': Emit('\b', output); break;
                    case 'f': Emit('\f', output); break;
                    case 'n': Emit('\n', output); break;
                    case 'r': Emit('\r', output); break;
                    case 't': Emit('\t', output); break;
                    default: throw Bad();
                }
                continue;
            }
            if (c == '\\') { _escape = 1; continue; }
            if (c == '"') { if (_high != 0) throw Bad(); _stage = 7; continue; }
            if (c < 32) throw Bad();
            Emit(c, output);
        }
        return output.ToString();
    }
    private void Emit(char c, StringBuilder output)
    {
        if (_high != 0)
        {
            if (!char.IsLowSurrogate(c)) throw Bad();
            output.Append(_high).Append(c); _high = (char)0;
        }
        else if (char.IsHighSurrogate(c)) _high = c;
        else if (char.IsLowSurrogate(c)) throw Bad();
        else output.Append(c);
    }
    internal void Finish() { if (!Complete || _escape != 0 || _high != 0) throw Bad(); }
    private static void Expect(char c, char expected) { if (c != expected) throw Bad(); }
    private static InvalidDataException Bad() => new InvalidDataException("reply_tool_invalid_or_incomplete_json");
}
