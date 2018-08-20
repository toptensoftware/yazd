using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace yaza
{
    public class CodeException : Exception
    {
        public CodeException(SourcePosition position, string message)
        {
            _position = position;
            _message = message;
        }

        SourcePosition _position;
        string _message;

        public SourcePosition Position => _position;
        public override string Message => _message;
    }
}
