using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbstractDataTypes
{
    public class Instance
    {
        public readonly AbstractDataType type;
        public IElement value;

        public Instance(AbstractDataType type)
        {
            this.type = type;
        }

        public Instance(AbstractDataType type, IElement value)
        {
            this.type = type;
            type.applyAxioms(ref value);
            this.value = value;
        }

        public AbstractDataType.OperationCall this[string operation]
        {
            get 
            {
                var test = this;
                return parameters => {
                    var p = new Instance[parameters.Length + 1];
                    p[0] = test;
                    Array.Copy(parameters, 0, p, 1, parameters.Length);
                    return type.call(operation, p);
                }; 
            }
        }

        public override string ToString()
        {
            return value.ToString() ?? "";
        }
    }

    public class AbstractDataType
    {
        internal static Dictionary<string, AbstractDataType> dataTypes = new Dictionary<string, AbstractDataType>();
        internal static Dictionary<string, IPrettyPrinter> prettyPrinters = new Dictionary<string, IPrettyPrinter>();

        public static AbstractDataType lookup(string name)
        {
            return dataTypes[name];
        }

        internal string[] sorts;

        public readonly string name;

        internal Dictionary<string, Operation> operations;

        internal Axiom[] axioms;

        internal AbstractDataType(string name, string[] sorts, Dictionary<string, Operation> operations, Axiom[] axioms)
        {
            this.name = name;
            this.sorts = sorts;
            this.operations = operations;
            this.axioms = axioms;
        }

        public delegate Instance OperationCall(params Instance[] parameters);

        public Instance call(string name, params Instance[] parameters)
        {
            if (!operations.ContainsKey(name))
            {
                throw new Exception("Operation '" + name + "' not supported by type '" + this.name + "'");
            }

            Operation operation = operations[name];

            if (parameters.Length < operation.argTypes.Length)
            {
                throw new Exception("Too few arguments (" + parameters.Length + " vs. " + operation.argTypes.Length + ") in call to '" + name + "'.");
            }
            else if (parameters.Length > operation.argTypes.Length)
            {
                throw new Exception("Too many arguments (" + parameters.Length + " vs. " + operation.argTypes.Length + ") in call to '" + name + "'.");
            }

            var c = new Call(name, this.name);

            for (int i = 0; i < operation.argTypes.Length; ++i)
            {
                var parameter = parameters[i];

                if (!operation.argTypes[i].Equals(parameter.type))
                {
                    throw new Exception("Incompatible type of argument '" + i + "' in call to '" + name + "', expected: '" + operation.argTypes[i] + "' got: '" + parameter.type + "'.");
                }

                c.addArgument(parameter.value);
            }

            var result = new Instance(AbstractDataType.lookup(operation.resultType), c);

            return result;
        }

        public bool applyAxioms(ref IElement element, bool recurse = true)
        {
            bool anyChange = false;
            bool changed;
            do
            {
                changed = false;
                foreach (var axiom in axioms)
                {
                    if (element.Type != this.name)
                    {
                        AbstractDataType type;
                        if (dataTypes.TryGetValue(element.Type, out type))
                        {
                            bool result = type.applyAxioms(ref element, recurse);
                            return anyChange || result;
                        }
                        else
                        {
                            throw new Exception("Undefined type '" + element.Type + "'");
                        }
                    }

                    if (axiom.apply(ref element))
                    {
                        changed = true;
                        anyChange = true;
                        break;
                    }
                }
            } while (changed);

            var call = element as Call;
            if (call != null)
            {
                anyChange = applyAxiomsToParameters(call) | anyChange;
            }
            return anyChange;
        }

        private bool applyAxiomsToParameters(Call call)
        {
            bool anyChange = false;
            for (int i = 0; i < call.Arguments.Count; ++i)
            {
                IElement argument = call.Arguments[i];

                AbstractDataType parameterType;
                if (dataTypes.TryGetValue(argument.Type, out parameterType))
                {
                    if (parameterType.applyAxioms(ref argument))
                    {
                        anyChange = true;
                    }
                    call.Arguments[i] = argument;
                }
                else
                {
                    throw new Exception("Undefined type '" + argument.Type + "'.");
                }
            }
            return anyChange;
        }

        public OperationCall this[string operation]
        {
            get { return parameters => call(operation, parameters); }
        }

        public static AbstractDataType load(string filename)
        {
            using (FileStream file = new FileStream(filename, FileMode.Open))
            {
                return load(new StreamReader(file));
            }
        }

        public static AbstractDataType load(TextReader stream)
        {
            var parser = new AbstractDataTypeParser();
            var result = parser.parse(stream);
            AbstractDataType.dataTypes[result.name] = result;
            return result;
        }

        public static bool unload(string typename)
        {
            return AbstractDataType.dataTypes.Remove(typename);
        }

        public static void addPrettyPrinter(string type, IPrettyPrinter printer)
        {
            prettyPrinters[type] = printer;
        }

        public static void removePrettyPrinter(string type)
        {
            prettyPrinters.Remove(type);
        }

        public override string ToString()
        {
            var serializer = new AbstractDataTypeSerializer();
            return serializer.serialize(this);
        }
    }
}
