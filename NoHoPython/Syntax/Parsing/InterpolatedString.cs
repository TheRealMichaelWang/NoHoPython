using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoHoPython.Syntax.Parsing
{
    partial class Scanner
    {
        private Stack<int> interpolationDepths = new();

        private Token ParseInterpolatedStart()
        {
            ScanChar();
            if (lastChar != '\"')
                throw new UnexpectedCharacterException('\"', lastChar, CurrentLocation);
            ScanChar();

            StringBuilder buffer = new();
            while (true)
            {
                if (lastChar == '\0')
                    throw new UnexpectedCharacterException('\0', lastChar, CurrentLocation);
                else if (lastChar == '\"') //regular string
                {
                    ScanChar();
                    return LastToken = new Token(TokenType.StringLiteral, buffer.ToString());
                }
                else if (lastChar == '{')
                {
                    ScanChar();
                    if (lastChar == '{') // {{ is an escaped {
                    {
                        ScanChar();
                        buffer.Append('{');
                    }
                    else //next will be tokens for interpolated values
                    {
                        interpolationDepths.Push(0);
                        return LastToken = new Token(TokenType.InterpolatedStart, buffer.ToString());
                    }
                }
                else if(lastChar == '}')
                {
                    ScanChar();
                    if (lastChar != '}')
                        throw new UnexpectedCharacterException('}', lastChar, CurrentLocation);
                    ScanChar();
                    buffer.Append('}');
                }
                else
                    buffer.Append(ScanCharLiteral());
            }
        }

        private Token? ContinueParseInterpolated()
        {
            if (interpolationDepths.Count == 0)
                return null;

            int depth = interpolationDepths.Peek();
            if (lastChar == '{')
            {
                interpolationDepths.Pop();
                interpolationDepths.Push(depth + 1);
                return null;
            }
            else if (lastChar == '}')
            {
                ScanChar();
                if (lastChar == '}')
                {
                    ScanChar();
                    return new Token(TokenType.OpenBrace, string.Empty);
                }

                if (depth == 0) //begin interpolation parsing
                {
                    StringBuilder buffer = new();
                    while (true)
                    {
                        if (lastChar == '\0')
                            throw new UnexpectedCharacterException('\0', lastChar, CurrentLocation);
                        else if (lastChar == '\"') //regular string
                        {
                            ScanChar();
                            interpolationDepths.Pop();
                            return LastToken = new Token(TokenType.InterpolatedEnd, buffer.ToString());
                        }
                        else if (lastChar == '{')
                        {
                            ScanChar();
                            if (lastChar == '{') // {{ is an escaped {
                            {
                                ScanChar();
                                buffer.Append('{');
                            }
                            else //next will be tokens for interpolated values
                            {
                                interpolationDepths.Push(0);
                                return LastToken = new Token(TokenType.InterpolatedMiddle, buffer.ToString());
                            }
                        }
                        else
                            buffer.Append(ScanCharLiteral());
                    }
                }
                else
                {
                    interpolationDepths.Pop();
                    interpolationDepths.Push(depth - 1);
                    return new Token(TokenType.OpenBrace, string.Empty);
                }
            }
            else
                return null;
        }
    }
}
