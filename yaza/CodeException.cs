using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace yaza
{
    public class CodeException : Exception
    {
        public CodeException(string message, SourcePosition position = null)
        {
            Position = position;
            _message = message;
        }

        string _message;

        public SourcePosition Position { get; set; }
        public override string Message => _message;
    }
}
