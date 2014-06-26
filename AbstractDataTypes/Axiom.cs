using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbstractDataTypes
{
    class Axiom
    {
        public readonly IElement left;
        public readonly IElement right;

        //do we need a typename here?
        //public string typename;

        public Axiom(IElement left, IElement right)
        {
            this.left = left;
            this.right = right;
        }

        public bool apply(ref IElement element)
        {
            var context = new Dictionary<string,IElement>();
            if(left.match(element, context))
            {
                element = right.apply(context);
                return true;
            }
            return false;
        }

        public bool applyRecursive(ref IElement element)
        {
            throw new NotImplementedException();
        }
    }
}
