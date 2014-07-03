using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/* Grammar:
 * 
 * adt: 'type:' identifier 'sorts:' type-list 'operations:' operation-list 'axioms:' axiom-list
 * 
 * type-list: identifier (',' identifier)*
 * 
 * operation-list: operation*
 * 
 * operation: identifier '(' type-list? ')' '->' identifier
 * 
 * axiom-list: axiom*
 * 
 * axiom: identifier '(' arg-list? ')' '=' identifier ( '(' arg-list? ')' )? 
 * 
 */
namespace AbstractDataTypes
{
    public class Instance
    {
        public readonly string type;
        public IElement value;

        public Instance(string type)
        {
            this.type = type;
        }

        public Instance(string type, IElement value)
        {
            this.type = type;
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
                    return AbstractDataType.dataTypes[type].call(operation, p);
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
        internal static Dictionary<string, AbstractDataType> dataTypes = new Dictionary<string,AbstractDataType>();
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
            if(!operations.ContainsKey(name))
            {
                throw new Exception("Operation '" + name + "' not supported by type '" + this.name + "'");
            }

            Operation operation = operations[name];

            if(parameters.Length < operation.argTypes.Length)
            {
                throw new Exception("Too few arguments (" + parameters.Length + " vs. " + operation.argTypes.Length + ") in call to '" + name + "'.");
            }
            else if(parameters.Length > operation.argTypes.Length)
            {
                throw new Exception("Too many arguments (" + parameters.Length + " vs. " + operation.argTypes.Length + ") in call to '" + name + "'.");
            }
            
            var c = new Call(name, this.name);

            for (int i = 0; i < operation.argTypes.Length; ++i )
            {
                var parameter = parameters[i];

                if(!operation.argTypes[i].Equals(parameter.type))
                {
                    throw new Exception("Incompatible type of argument '" + i + "' in call to '" + name + "', expected: '" + operation.argTypes[i] + "' got: '" + parameter.type + "'.");
                }

                c.addArgument(parameter.value);
            }

            var result = new Instance(operation.resultType);
            result.value = c;

            applyAxioms(ref result.value);

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
            if(call != null)
            {
                anyChange = applyAxiomsToParameters(call) | anyChange;
            }
            return anyChange;
        }

        private bool applyAxiomsToParameters(Call call)
        {
            bool anyChange = false;
            for(int i = 0; i < call.Arguments.Count; ++i)
            {
                IElement argument = call.Arguments[i];

                AbstractDataType parameterType;
                if (dataTypes.TryGetValue(argument.Type, out parameterType))
                {
                    if(parameterType.applyAxioms(ref argument))
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

    public class AbstractDataTypeParser
    {
        HashSet<ITerminalTransformer> terminalTransformers;

        public AbstractDataTypeParser(HashSet<ITerminalTransformer> terminalTransformers = null)
        {
            if(terminalTransformers == null)
            {
                this.terminalTransformers = new HashSet<ITerminalTransformer>();
            }
            else
            {
                this.terminalTransformers = terminalTransformers;
            }
        }
        
        string[] sorts;

        string typename;

        Dictionary<string, Operation> operations;

        Axiom[] axioms;

        public AbstractDataType parse(TextReader s)
        {
            var tok = new Tokenizer(s);

            checkKeyword(tok.take(), "type:");
            typename = parseType(tok);

            checkKeyword(tok.take(), "sorts:");
            sorts = parseSorts(tok);

            checkKeyword(tok.take(), "operations:");
            operations = parseOperations(tok);
            checkOperations();

            checkKeyword(tok.take(), "axioms:");
            axioms = parseAxioms(tok);

            return new AbstractDataType(typename, sorts, operations, axioms);
        }

        public IElement parseExpression(TextReader s, string defaultTypename)
        {
            var tok = new Tokenizer(s);
            typename = defaultTypename;
            return parseSyntaxTree(tok, false);
        }

        private Axiom[] parseAxioms(Tokenizer tok)
        {
            List<Axiom> axioms = new List<Axiom>();
            
            while (!tok.EndOfStream && tok.currentToken.Type != TokenType.Keyword)
            {
                axioms.Add(parseAxiom(tok));
            }

            return axioms.ToArray();
        }

        private Axiom parseAxiom(Tokenizer tok)
        {
            IElement left = parseSyntaxTree(tok);
            //checkType(tok.take(), TokenType.Equal);
            checkName(tok.take(), "=");
            IElement right = parseSyntaxTree(tok);
            return new Axiom(left, right);
        }

        private IElement parseSyntaxTree(Tokenizer tok, bool check = true)
        {
            var typename = this.typename;

            var name = parseIdentifierWithType(tok, out typename, check);

            if(!tok.EndOfStream && tok.currentToken.Type == TokenType.LeftParenthesis)
            {
                tok.next();

                var call = new Call(name, typename);

                if (tok.currentToken.Type != TokenType.RightParenthesis)
                {
                    call.addArgument(parseSyntaxTree(tok, check));
                }

                while(tok.currentToken.Type != TokenType.RightParenthesis)
                {
                    checkType(tok.take(), TokenType.Comma);
                    call.addArgument(parseSyntaxTree(tok, check));
                }

                if(check)
                {
                    checkCall(call);
                }

                tok.next();

                return call;
            }
            else
            {
                var term = new Terminal(name, typename);
                IElement transformed;
                foreach (var transformer in terminalTransformers)
                {
                    if(transformer.transform(term, out transformed))
                    {
                        return transformed;
                    }
                }
                return term;
            }
        }

        private void checkCall(Call call)
        {
            if(!typename.Equals(call.Type))
                return;

            Operation operation;

            if (!operations.TryGetValue(call.Name, out operation))
            {
                throw new Exception("Unknown operation '" + call.Name + "'.");
            }

            if(call.Arguments.Count > operation.argTypes.Length)
            {
                throw new Exception("Too many arguments in call to operation '" + call.Name + "', expected " + operation.argTypes.Length + " got " + call.Arguments.Count + ".");
            }
            else if (call.Arguments.Count < operation.argTypes.Length)
            {
                throw new Exception("Too few arguments in call to operation '" + call.Name + "', expected " + operation.argTypes.Length + " got " + call.Arguments.Count + ".");
            }
        }

        private void checkOperations()
        {
            foreach (var operation in operations.Values)
            {
                foreach (var arg in operation.argTypes)
	            {
		            if(!sorts.Contains(arg) && !arg.Equals(typename))
                    {
                        throw new Exception("Error in operation '" + operation.name + "': argument type '" + arg + "' is unknown.");
                    }
	            }

                if (!sorts.Contains(operation.resultType) && !operation.resultType.Equals(typename))
                {
                    throw new Exception("Error in operation '" + operation.name + "': return type '" + operation.resultType + "' is unknown.");
                }
            }
        }

        private Dictionary<string, Operation> parseOperations(Tokenizer tok)
        {
            Dictionary<string, Operation> operations = new Dictionary<string, Operation>();

            while(tok.currentToken.Type != TokenType.Keyword)
            {
                var op = parseOperation(tok);
                if(operations.ContainsKey(op.name))
                    throw new Exception("Operation '" + op.name + "' is already defined.");

                operations[op.name] = op;
            }
            return operations;
        }

        private Operation parseOperation(Tokenizer tok)
        {
            checkType(tok.currentToken, TokenType.Identifier);
            Operation op = new Operation();
            op.name = tok.take().Value;

            checkType(tok.take(), TokenType.LeftParenthesis);
             
            op.argTypes = parseSorts(tok);

            checkType(tok.take(), TokenType.RightParenthesis);

            //checkType(tok.take(), TokenType.Map);
            checkName(tok.take(), "->");

            op.resultType = parseType(tok);

            return op;
        }

        private string[] parseSorts(Tokenizer tok)
        {
            var sorts = new List<string>();

            if(tok.currentToken.Type == TokenType.Identifier)
            {
                sorts.Add(tok.take().Value);

                while (tok.currentToken.Type == TokenType.Comma)
                {
                    tok.next();

                    sorts.Add(parseType(tok));
                }
            }
            
            return sorts.ToArray();
        }

        private string parseType(Tokenizer tok)
        {
            if(tok.currentToken.Type != TokenType.Identifier)
            {
                throw new Exception("Expected an identifier, but got " + tok.currentToken.Type.ToString());
            }

            return tok.take().Value;
        }

        private string parseIdentifierWithType(Tokenizer tok, out string type, bool check = true)
        {
            if (tok.currentToken.Type == TokenType.Identifier)
            {
                type = typename;
            }
            else if (tok.currentToken.Type == TokenType.IdentifierWithType)
            {
                var tempType = tok.currentToken.ValueType;
                if (check && !typename.Equals(tempType) && !sorts.Contains(tempType))
                {
                    throw new Exception("Type of identifier '" + tempType + ":" + tok.currentToken.Value + "' not defined in sorts.");
                }
                type = tempType;
            }
            else
            {
                throw new Exception("Expected an identifier, but got '" + tok.currentToken.Type.ToString() + "'.");
            }

            return tok.take().Value;
        }

        private void checkToken(Token t, string name, TokenType type = TokenType.Identifier)
        {
            checkType(t, type);
            checkName(t, name);
        }

        private void checkName(Token t, string name)
        {
            if (!t.Value.Equals(name))
            {
                throw new Exception("Wrong token name, expected '" + name + "' got: '" + t.Value + "'");
            }
        }

        private void checkType(Token t, TokenType type)
        {
            if (t.Type != type)
            {
                throw new Exception("Invalid token type, expected '" + type + "' got: '" + t.Type + "'");
            }
        }

        private void checkKeyword(Token t, string keyword)
        {
            checkType(t, TokenType.Keyword);
            checkName(t, keyword);
        }
    }

    class AbstractDataTypeSerializer
    {
        public AbstractDataTypeSerializer()
        {

        }

        public string serialize(AbstractDataType type)
        {
            var sb = new StringBuilder();
            sb.Append("type: ").AppendLine(type.name);

            sb.Append("sorts: ");
            serializeList(sb, type.sorts);
            sb.AppendLine();
            
            sb.Append("operations:");
            foreach(var operation in type.operations.Values)
            {
                serialize(sb, operation);
            }

            sb.Append("axioms:");
            foreach(var axiom in type.axioms)
            {
                serialize(sb, axiom, type.name);
            }

            return sb.ToString();
        }

        private void serialize(StringBuilder sb, Axiom axiom, string type)
        {
            sb.Append("\t");
            serializeElement(sb, axiom.left, type);
            sb.Append(" = ");
            serializeElement(sb, axiom.right, type);
            sb.AppendLine();
        }

        private void serializeElement(StringBuilder sb, IElement element, string type)
        {
            sb.Append(element.ToString(type));
        }

        private void serialize(StringBuilder sb, Operation operation)
        {
            sb.AppendFormat("\t{0}(", operation.name);
            serializeList(sb, operation.argTypes);
            sb.Append(") -> ").AppendLine(operation.resultType);
        }

        private void serializeList(StringBuilder sb, string[] items)
        {
            if (items.Length > 0)
            {
                sb.Append(items[0]);
            }

            foreach (var sort in items.Skip(1))
            {
                sb.Append(", ").Append(sort);
            }
        }
    }
}
