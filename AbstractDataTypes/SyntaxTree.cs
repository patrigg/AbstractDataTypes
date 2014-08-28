using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbstractDataTypes
{
    public interface IElement
    {
        string Name { get; }
        string Type { get; }
        bool match(IElement other, Dictionary<string, IElement> context);
        IElement apply(Dictionary<string, IElement> context);
        IElement clone();
        string ToString(string type, bool prettyPrint = true);
    }

    public class Call : IElement
    {
        readonly string name;
        readonly string type;
        List<IElement> arguments = new List<IElement>();

        public Call(string name, string type)
        {
            this.name = name;
            this.type = type;
        }

        public void addArgument(IElement argument)
        {
            arguments.Add(argument);
        }

        public List<IElement> Arguments
        {
            get { return arguments; }
        }

        public bool match(IElement other, Dictionary<string, IElement> context)
        {
            if(other is Call && other.Type.Equals(Type) && other.Name.Equals(Name))
            {
                Call otherCall = (Call)other;
                if(arguments.Count == otherCall.arguments.Count)
                {
                    for(int i = 0; i < arguments.Count; ++i)
                    {
                        if(!arguments[i].match(otherCall.arguments[i], context))
                        {
                            context.Clear();
                            return false;
                        }
                    }
                    return true;
                }
            }
            context.Clear();
            return false;
        }

        public string Name
        {
            get { return name; }
        }

        public string Type
        {
            get { return type; }
        }


        public IElement apply(Dictionary<string, IElement> context)
        {
            Call result = new Call(Name, Type);
            result.arguments = arguments.ConvertAll(element => element.apply(context));
            
            return result;
        }


        public IElement clone()
        {
            Call result = new Call(Name, Type);
            result.arguments = arguments.ConvertAll(element => element.clone());
            return result;
        }

        public override string ToString()
        {
            return ToString(null);
        }

        public string print(string omitType)
        {
            var sb = new StringBuilder();
            if(omitType == null || omitType != type)
            {
                sb.Append(type);
                sb.Append(":");
            }
            
            sb.Append(name);
            sb.Append("(");
            if (arguments.Count > 0)
            {
                sb.Append(arguments.First().ToString(omitType));
            }

            for (int i = 1; i < arguments.Count; ++i)
            {
                sb.Append(", ");
                if (arguments[i] != null)
                {
                    sb.Append(arguments[i].ToString(omitType));
                }
                else
                {
                    sb.Append("null");
                }
            }
            sb.Append(")");
            return sb.ToString();
        }

        public string ToString(string omitType, bool prettyPrint = true)
        {
            IPrettyPrinter printer;
            if (prettyPrint && AbstractDataType.prettyPrinters.TryGetValue(type, out printer))
            {
                return printer.print(this);
            }
            else
            {
                return print(omitType);
            }
        }
    }

    public class Terminal : IElement
    {
        readonly string name;
        readonly string type;

        public Terminal(string name, string type)
        {
            this.name = name;
            this.type = type;
        }

        public bool match(IElement other, Dictionary<string, IElement> context)
        {
            if(context.ContainsKey(Name))
            {
                if (!context[Name].match(other, new Dictionary<string, IElement>()))
                {
                    context.Clear();
                    return false;
                }
                else
                {
                    return true;
                }
            }
            context[Name] = other;
            return true;
        }

        public string Name
        {
            get { return name; }
        }


        public IElement apply(Dictionary<string, IElement> context)
        {
            return context[Name].clone();
        }

        public string Type
        {
            get { return type; }
        }

        public IElement clone()
        {
            return new Terminal(Name, Type);
        }

        public override string ToString()
        {
            return Type + ":" + Name;
        }

        public string ToString(string omitType, bool prettyPrint)
        {
            if (omitType == null || omitType == type)
            {
                return ToString();
            }
            else
            {
                return Type + ":" + Name;
            }
        }
    }

    public class Assignment : IElement
    {
        readonly string name;
        readonly IElement expression;

        public Assignment(string name, IElement expression)
        {
            this.name = name;
            this.expression = expression;
        }

        public bool match(IElement other, Dictionary<string, IElement> context)
        {
            throw new NotSupportedException("assignment does not support matching");
            /*if (context.ContainsKey(Name))
            {
                if (!context[Name].match(other, new Dictionary<string, IElement>()))
                {
                    context.Clear();
                    return false;
                }
                else
                {
                    return true;
                }
            }
            context[Name] = other;
            return true;*/
        }

        public string Name
        {
            get { return name; }
        }


        public IElement apply(Dictionary<string, IElement> context)
        {
            var expr = expression.apply(context);
            context[Name] = expr;
            return expr.clone();
        }

        public string Type
        {
            get { return expression.Type; }
        }

        public IElement clone()
        {
            return new Assignment(Name, expression);
        }

        public override string ToString()
        {
            return Name + " = " + expression.ToString();
        }

        public string ToString(string omitType, bool prettyPrint)
        {
            return Name + " = " + expression.ToString(omitType, prettyPrint);
        }
    }


}
