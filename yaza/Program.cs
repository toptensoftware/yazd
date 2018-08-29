using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using yazd;

namespace yaza
{
    class Program
    {
        public bool ProcessArg(string arg)
        {
            if (arg == null)
                return true;

            if (arg.StartsWith("#"))
                return true;

            // Response file
            if (arg.StartsWith("@"))
            {
                // Get the fully qualified response file name
                string strResponseFile = System.IO.Path.GetFullPath(arg.Substring(1));

                // Load and parse the response file
                var args = Utils.ParseCommandLine(System.IO.File.ReadAllText(strResponseFile));

                // Set the current directory
                string OldCurrentDir = System.IO.Directory.GetCurrentDirectory();
                System.IO.Directory.SetCurrentDirectory(System.IO.Path.GetDirectoryName(strResponseFile));

                // Load the file
                bool bRetv = ProcessArgs(args);

                // Restore current directory
                System.IO.Directory.SetCurrentDirectory(OldCurrentDir);

                return bRetv;
            }

            // Args are in format [/-]<switchname>[:<value>];
            if (arg.StartsWith("/") || arg.StartsWith("-"))
            {
                string SwitchName = arg.Substring(arg.StartsWith("--") ? 2 : 1);
                string Value = null;

                int colonpos = SwitchName.IndexOf(':');
                if (colonpos >= 0)
                {
                    // Split it
                    Value = SwitchName.Substring(colonpos + 1);
                    SwitchName = SwitchName.Substring(0, colonpos);
                }

                switch (SwitchName)
                {
                    case "help":
                    case "h":
                    case "?":
                        ShowLogo();
                        ShowHelp();
                        return false;

                    case "v":
                        ShowLogo();
                        return false;

                    case "instructionSet":
                        InstructionSet.DumpAll();
                        return false;

                    case "ast":
                        if (Value != null)
                        {
                            _astFile = Value;
                        }
                        else
                        {
                            _astFile = ":default";
                        }
                        break;

                    case "sym":
                        if (Value != null)
                        {
                            _symbolsFile = Value;
                        }
                        else
                        {
                            _symbolsFile = ":default";
                        }
                        break;

                    case "list":
                        if (Value != null)
                        {
                            _listFile = Value;
                        }
                        else
                        {
                            _listFile = ":default";
                        }
                        break;

                    case "define":
                        if (Value == null)
                        {
                            throw new InvalidOperationException("--define: requires argument value");
                        }
                        else
                        {
                            var eqPos = Value.IndexOf('=');
                            if (eqPos < 0)
                                _userDefines.Add(Value, null);
                            else
                                _userDefines.Add(Value.Substring(0, eqPos), Value.Substring(eqPos + 1));
                        }
                        break;

                    case "output":
                        if (Value == null)
                        {
                            throw new InvalidOperationException("--output: requires argument value");
                        }
                        else
                        {
                            _outputFile = Value;
                        }
                        break;

                    case "include":
                        if (Value == null)
                        {
                            throw new InvalidOperationException("--include: requires argument value");
                        }
                        else
                        {
                            try
                            {
                                _includePaths.Add(System.IO.Path.GetFullPath(Value));
                            }
                            catch (Exception x)
                            {
                                throw new InvalidOperationException($"Invalid include path: {Value} - {x.Message}");
                            }
                        }
                        break;

                    default:
                        throw new InvalidOperationException(string.Format("Unknown switch '{0}'", arg));
                }
            }
            else
            {
                if (_inputFile == null)
                    _inputFile = arg;
                else
                    throw new InvalidOperationException(string.Format("Too many command line arguments, don't know what to do with '{0}'", arg));
            }

            return true;
        }

        string _inputFile;
        string _outputFile;
        List<string> _includePaths = new List<string>();
        string _astFile;
        string _symbolsFile;
        string _listFile;
        Dictionary<string, string> _userDefines = new Dictionary<string, string>();

        public bool ProcessArgs(IEnumerable<string> args)
        {
            if (args == null)
                return true;

            // Parse args
            foreach (var a in args)
            {
                if (!ProcessArg(a))
                    return false;
            }

            return true;
        }


        public void ShowLogo()
        {
            System.Version v = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            Console.WriteLine("yaza v{0} - Yet Another Z80 Assembler", v);
            Console.WriteLine("Copyright (C) 2012-2018 Topten Software. All Rights Reserved.");

            Console.WriteLine("");
        }

        public void ShowHelp()
        {
            Console.WriteLine("usage: yaza source.asm [options] [@responsefile]");
            Console.WriteLine();

            Console.WriteLine("Options:");
            Console.WriteLine("  --ast[:<filename>]         Dump the AST");
            Console.WriteLine("  --define:<symbol>[=<expr>] Define a symbol with optional value");
            Console.WriteLine("  --help                     Show these help instruction");
            Console.WriteLine("  --include:<directory>      Specifies an additional include/incbin directory");
            Console.WriteLine("  --instructionSet           Display a list of all support instructions");
            Console.WriteLine("  --list[:<filename>]        Generate a list file");
            Console.WriteLine("  --output:<filename>        Output file");
            Console.WriteLine("  --sym[:<filename>]         Generate a symbols file");
            Console.WriteLine("  --v                        Show version information");

            Console.WriteLine();
            Console.WriteLine("Numeric arguments can be in decimal (no prefix) or hex if prefixed with '0x'.");

            Console.WriteLine();
            Console.WriteLine("Response file containing arguments can be specified using the @ prefix");

            Console.WriteLine();
            Console.WriteLine("Example:");
            Console.WriteLine("    yaza input.asm");
            Console.WriteLine();
        }

        public TextWriter OpenTextWriter(string filename, string defExt)
        {
            if (filename == ":default")
                return new StreamWriter(System.IO.Path.ChangeExtension(_inputFile, defExt));
            else
                return new StreamWriter(filename);
        }

        public int Run(string[] args)
        {
            // Process command line
            if (!ProcessArgs(args))
                return 0;

            // Was there an input file specified?
            if (_inputFile == null)
            {
                ShowLogo();
                ShowHelp();
                return 7;
            }

            // Create the root scope
            var root = new AstScope("global");
            var exprNodeIP = new ExprNodeIP(true);
            root.Define("$", exprNodeIP);
            var exprNodeIP2 = new ExprNodeIP(false);
            root.Define("$$", exprNodeIP2);
            var exprNodeOP = new ExprNodeOFS(true);
            root.Define("$ofs", exprNodeOP);
            var exprNodeOP2 = new ExprNodeOFS(false);
            root.Define("$$ofs", exprNodeOP2);
            root.AddElement(new AstTypeByte());
            root.AddElement(new AstTypeWord());

            // Define user specified symbols
            foreach (var kv in _userDefines)
            {
                var defParser = new Parser();
                if (kv.Value != null)
                {
                    try
                    {
                        var exprNode = defParser.ParseExpression(kv.Value);
                        root.Define(kv.Key, exprNode);
                    }
                    catch (CodeException x)
                    {
                        throw new InvalidOperationException(x.Message + " in command line symbol definition");
                    }
                }
                else
                {
                    root.Define(kv.Key, new ExprNodeNumberLiteral(null, 1));
                }
            }

            // Step 1 - Parse the input file
            var p = new Parser();
            p.IncludePaths = _includePaths;
            var file = p.Parse(_inputFile, System.IO.Path.GetFullPath(_inputFile));
            root.AddElement(file);

            // Run the "Define Symbols" pass
            root.DefineSymbols(null);

            if (_astFile != null)
            {
                using (var w = OpenTextWriter(_astFile, ".ast.txt"))
                {
                    root.Dump(w, 0);
                }
            }

            // Step 2 - Layout
            var layoutContext = new LayoutContext();
            exprNodeIP.SetContext(layoutContext);
            exprNodeIP2.SetContext(layoutContext);
            exprNodeOP.SetContext(layoutContext);
            exprNodeOP2.SetContext(layoutContext);
            root.Layout(null, layoutContext);

            if (Log.ErrorCount == 0)
            {
                // Step 3 - Generate
                var generateContext = new GenerateContext(layoutContext);

                exprNodeIP.SetContext(generateContext);
                exprNodeIP2.SetContext(generateContext);
                exprNodeOP.SetContext(generateContext);
                exprNodeOP2.SetContext(generateContext);

                if (_listFile != null)
                    generateContext.ListFile = OpenTextWriter(_listFile, "lst");

                generateContext.EnterSourceFile(file.SourcePosition);
                root.Generate(null, generateContext);
                generateContext.LeaveSourceFile();

                if (_listFile != null)
                {
                    generateContext.ListFile.Dispose();
                    generateContext.ListFile = null;
                }

                if (Log.ErrorCount == 0)
                {
                    var code = generateContext.GetGeneratedBytes();
                    var outputFile = _outputFile == null ? System.IO.Path.ChangeExtension(_inputFile, ".bin") : _outputFile;
                    System.IO.File.WriteAllBytes(outputFile, code);
                }
            }


            // Write symbols file
            if (_symbolsFile != null && Log.ErrorCount == 0)
            {
                using (var w = OpenTextWriter(_symbolsFile, "sym"))
                    root.DumpSymbols(w);
            }

            Log.DumpSummary();

            return 0;
        }

        static int Main(string[] args)
        {
            try
            {
                return new Program().Run(args);
            }
            catch (InvalidOperationException x)
            {
                Console.WriteLine("{0}", x.Message);
                return 7;
            }
            catch (IOException x)
            {
                Console.WriteLine("File Error - {0}", x.Message);
                return 7;
            }
        }
    }
}
