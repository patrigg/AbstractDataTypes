using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbstractDataTypes
{
    public class AbstractDataTypeParser
    {
        HashSet<ITerminalTransformer> terminalTransformers;

        public AbstractDataTypeParser(HashSet<ITerminalTransformer> terminalTransformers = null)
        {
            if (terminalTransformers == null)
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
            checkName(tok.take(), "=");
            IElement right = parseSyntaxTree(tok);
            return new Axiom(left, right);
        }

        private IElement parseSyntaxTree(Tokenizer tok, bool check = true)
        {
            var typename = this.typename;

            var name = parseIdentifierWithType(tok, out typename, check);

            if (!tok.EndOfStream && tok.currentToken.Type == TokenType.LeftParenthesis)
            {
                tok.next();

                var call = new Call(name, typename);

                if (tok.currentToken.Type != TokenType.RightParenthesis)
                {
                    call.addArgument(parseSyntaxTree(tok, check));
                }

                while (tok.currentToken.Type != TokenType.RightParenthesis)
                {
                    checkType(tok.take(), TokenType.Comma);
                    call.addArgument(parseSyntaxTree(tok, check));
                }

                if (check)
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
                    if (transformer.transform(term, out transformed))
                    {
                        return transformed;
                    }
                }
                return term;
            }
        }

        private void checkCall(Call call)
        {
            if (!typename.Equals(call.Type))
                return;

            Operation operation;

            if (!operations.TryGetValue(call.Name, out operation))
            {
                throw new Exception("Unknown operation '" + call.Name + "'.");
            }

            if (call.Arguments.Count > operation.argTypes.Length)
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
                    if (!sorts.Contains(arg) && !arg.Equals(typename))
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

            while (tok.currentToken.Type != TokenType.Keyword)
            {
                var op = parseOperation(tok);
                if (operations.ContainsKey(op.name))
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

            checkName(tok.take(), "->");

            op.resultType = parseType(tok);

            return op;
        }

        private string[] parseSorts(Tokenizer tok)
        {
            var sorts = new List<string>();

            if (tok.currentToken.Type == TokenType.Identifier)
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
            if (tok.currentToken.Type != TokenType.Identifier)
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
}
