using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbstractDataTypes
{
    class PushableTokenizer : ITokenizer
    {
        ITokenizer tok;
        Stack<Token> pushedTokens = new Stack<Token>();

        public PushableTokenizer(ITokenizer tok)
        {
            this.tok = tok;
        }

        public Token take()
        {
            return pushedTokens.Any() ? pushedTokens.Pop() : tok.take();
        }

        public Token currentToken
        {
            get 
            {
                return pushedTokens.Any() ? pushedTokens.Peek() : tok.currentToken;
            }
        }

        public bool EndOfStream
        {
            get { return pushedTokens.Any() || tok.EndOfStream; }
        }

        public bool next()
        {
            if (pushedTokens.Any())
            {
                pushedTokens.Pop();
                return true;
            }
            else
            {
                return tok.next();
            }
        }

        public void push(Token token)
        {
            pushedTokens.Push(token);
        }
    }
}
