using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AbstractDataTypes
{
    public interface IPrettyPrinter
    {
        string print(IElement element);
    }

    public class NumberPrinter : IPrettyPrinter
    {
    
        public string print(IElement element)
        {
            var call = (Call)element;
            int count = 0;
            while(call.Name.Equals("inc"))
            {
                call = (Call)call.Arguments[0];
                ++count;
            }
            if(call.Name.Equals("zero"))
            {
                return count.ToString();
            }
            else
            {
                return element.ToString(null, false);
            }
        }
    }

    public class BoolPrinter : IPrettyPrinter
    {
        public string print(IElement element)
        {
            var call = (Call)element;
            return call.Name;
        }
    }

}
