﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AbstractDataTypes
{
    interface ITokenizer
    {
        Token take();
        Token currentToken { get; }
        bool EndOfStream { get; }
        bool next();
    }

    enum TokenType
    {
        LeftParenthesis,
        RightParenthesis,
        Identifier,
        IdentifierWithType,
        Comma,
        Keyword
    }

    struct Token
    {
        readonly TokenType type;
        readonly string value;
        readonly string valueType;

        public Token(TokenType type, string value = null, string valueType = null)
        {
            this.type = type;
            this.value = value;
            this.valueType = valueType;
        }

        public TokenType Type { get { return type; } }
        public string Value { get { return value; } }
        public string ValueType { get { return valueType; } }
    }

    class Tokenizer : ITokenizer
    {
        PushableReader r;

        public Tokenizer(TextReader s)
        {
            r = new PushableReader(s);
            next();
        }

        private bool isWhitespace()
        {
            char c = (char)r.Peek();
            return c == ' ' || c == '\r' || c == '\n' || c == '\t';
        }

        private void skipWhitespaces()
        {
            while(!r.EndOfStream && isWhitespace())
            {
                r.Read();
            }
        }

        public Token take()
        {
            Token t = currentToken;
            next();
            return t;
        }

        private Token? _currentToken;
        public Token currentToken {
            get
            {
                try
                {
                    return _currentToken.Value;
                }
                catch(InvalidOperationException ex)
                {
                    throw new Exception("End of stream reached.", ex);
                }
            }
            private set { _currentToken = value; }
        }

        public bool EndOfStream
        {
            get
            {
                return _currentToken == null;
            }
        }

        public bool next()
        {
            while(!r.EndOfStream)
            {
                switch (r.Peek())
                {
                    case ' ':
                    case '\r':
                    case '\n':
                    case '\t':
                        r.Read();
                        break;
                    case ',':
                        r.Read();
                        currentToken = new Token(TokenType.Comma, ",");
                        return true;
                    case '(':
                        r.Read();
                        currentToken = new Token(TokenType.LeftParenthesis, "(");
                        return true;
                    case ')':
                        r.Read();
                        currentToken = new Token(TokenType.RightParenthesis, ")");
                        return true;
                    default:
                        currentToken = createIdentifierOrKeyword(parseIdentifier());
                        return true;
                }
            }
            _currentToken = null;
            return false;
        }

        private Token createIdentifierOrKeyword(string name)
        {
            if(name.EndsWith(":"))
            {
                return new Token(TokenType.Keyword, name);
            }
            else
            {
                if(name.Contains(':'))
                {
                    var identifier = name.Split(':');
                    if(identifier.Length != 2)
                    {
                        throw new Exception("Too many type qualifiers in identifier: '" + name + "'");
                    }

                    return new Token(TokenType.IdentifierWithType, identifier[1], identifier[0]);
                }
                return new Token(TokenType.Identifier, name);
            }
        }

        private string parseIdentifier(string prefix = "")
        {
            StringBuilder sb = new StringBuilder(prefix);
            while (!r.EndOfStream)
            {
                switch (r.Peek())
                {
                    case ' ':
                    case '\r':
                    case '\n':
                    case '\t':
                    case '(':
                    case ')':
                    case ',':
                        return sb.ToString();
                    default:
                        sb.Append(r.Read());
                        break;
                }
            }
            return sb.ToString();
        }
    }
}
