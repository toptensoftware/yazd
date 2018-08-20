using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace yaza
{
    public static class Log
    {
        static TextWriter _output = Console.Out;

        static int _warnings = 0;
        static int _errors = 0;

        public static int ErrorCount => _errors;

        public static void Warning(SourcePosition position, string message)
        {
            _warnings++;
            _output.WriteLine($"{position.Describe()}: warning: {message}");
        }

        public static void Error(SourcePosition position, string message)
        {
            _errors++;
            _output.WriteLine($"{position.Describe()}: {message}");

            if (_errors > 100)
            {
                throw new InvalidDataException("Error limit exceeded, aborting");
            }
        }


        public static void Error(string message)
        {
            _output.WriteLine($"{message}");
        }

        public static void Error(CodeException exception)
        {
            Error(exception.Position, exception.Message);
        }

        public static void DumpSummary()
        {
            _output.WriteLine($"Errors: {_errors} Warnings: {_warnings}");
        }
    }
}
