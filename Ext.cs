using System.Text;

namespace RecursiveParsing;

partial class Ext
{
    extension(StringBuilder sb)
    {
        public StringBuilder AppendLineLinux(string text)
        => sb.AppendFormat("{0}\n", text.Replace("\r\n", "\n"));
    }
}
