using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbstractDataTypes
{
    class Program
    {
        static void Main(string[] args)
        {
            var stack = AbstractDataType.load("stack.txt");
            var queue = AbstractDataType.load("queue.txt");
            var number = AbstractDataType.load("number.txt");
            AbstractDataType.load("bool.txt");
            AbstractDataType.prettyPrinters.Add("Number", new NumberPrinter());
            AbstractDataType.prettyPrinters.Add("bool", new BoolPrinter());

            var test = stack["empty"]();
            System.Console.WriteLine(test);
            test = test["push"](number["zero"]());
            System.Console.WriteLine(test);
            test = test["pop"]();
            System.Console.WriteLine(test);
            test = test["is_empty"]();
            System.Console.WriteLine(test);


            test = queue["empty"]();
            System.Console.WriteLine(test);
            test = test["enqueue"](number["zero"]()["inc"]());
            test = test["enqueue"](number["zero"]()["inc"]()["inc"]());
            System.Console.WriteLine(test);
            test = test["dequeue"]();
            System.Console.WriteLine(test);
            System.Console.WriteLine(test["is_empty"]());
            test = test["dequeue"]();
            System.Console.WriteLine(test);
            System.Console.WriteLine(test["is_empty"]());
        }
    }
}
