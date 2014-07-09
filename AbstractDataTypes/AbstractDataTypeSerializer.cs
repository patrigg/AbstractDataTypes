using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbstractDataTypes
{
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
            foreach (var operation in type.operations.Values)
            {
                serialize(sb, operation);
            }

            sb.Append("axioms:");
            foreach (var axiom in type.axioms)
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
