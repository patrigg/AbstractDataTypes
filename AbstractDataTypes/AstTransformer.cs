using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbstractDataTypes
{
    public interface ITerminalTransformer
    {
        bool transform(Terminal terminal, out IElement transformed);
    }

    public class PeanoNumberLiteralTransformer : ITerminalTransformer
    {
        public bool transform(Terminal terminal, out IElement transformed)
        {
            uint result;
            if(uint.TryParse(terminal.Name, out result))
            {
                var number = new Call("zero", "Number");
                while(result > 0)
                {
                    var newNumber = new Call("inc", "Number");
                    newNumber.addArgument(number);
                    number = newNumber;
                    --result;
                }
                transformed = number;
                return true;
            }
            else
            {
                transformed = terminal;
                return false;
            }
        }
    }

    public class BoolLiteralTransformer : ITerminalTransformer
    {
        public bool transform(Terminal terminal, out IElement transformed)
        {
            if (terminal.Name == "true" || terminal.Name == "false")
            {
                transformed = new Call(terminal.Name, "bool");
                return true;
            }
            else
            {
                transformed = terminal;
                return false;
            }
        }
    }
}
