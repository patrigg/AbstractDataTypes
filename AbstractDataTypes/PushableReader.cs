using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbstractDataTypes
{
    class PushableReader
    {
        TextReader r;
        Stack<char> pushedChars = new Stack<char>();

        public PushableReader(TextReader s)
        {
            r = s;
        }

        public char Read()
        {
            if(pushedChars.Count != 0)
            {
                return pushedChars.Pop();
            }

            return (char)r.Read();
        }

        public char Peek()
        {
            if (pushedChars.Count != 0)
            {
                return pushedChars.Peek();
            }

            return (char)r.Peek();
        }

        public bool EndOfStream
        {
            get
            {
                return pushedChars.Count == 0 && r.Peek() == -1;
            }
        }

        public void Push(char c)
        {
            pushedChars.Push(c);
        }
    }
}
