using System;
using System.IO;


namespace CSS_Minifier
{
    class Program
    {
        /// <summary>
        /// Main function of the CSS minifier application
        /// </summary>
        /// <param name="args">accepted arguments</param>
        static void Main(string[] args)
        {
            Minifier cssMin = new Minifier();

            string strInputPath = string.Empty, strOutputPath = string.Empty, strHelp = string.Empty;
            bool bStartProcess = false;
            try
            {
                if (args.Length == 1 && (args[0] == "-Help" || args[0] == "/Help" || args[0] == "-?" || args[0] == "/?"))
                { DisplayHelp(); }
                else
                {
                    //Loop through arguments
                    strInputPath = args[0];
                    strOutputPath = args[1];
                    for (int i = 2, l = args.Length; i < l; i++)
                    {
                        switch (args[i].ToUpper())
                        {
                            case "-L":
                            case "/L":
                            case "-Log":
                            case "/Log":
                                cssMin.bWriteLog = true;
                                break;
                        }
                    }

                    //read file
                    string text = File.ReadAllText(strInputPath);
                    using (StreamWriter outfile = new StreamWriter(strOutputPath))
                    { outfile.Write(cssMin.processCSS(text)); }

                    if (cssMin.bWriteLog)
                    { Console.Write(cssMin.strLog); }
                }
            }
            catch (Exception ex)
            { Console.WriteLine("An unexpected error occurred please submit a report. Type -help or -? for command options."); }

            System.Console.ReadKey();
        }

        /// <summary>
        /// Displays the help text
        /// </summary>
        static void DisplayHelp() {
            Console.WriteLine("Minimizes CSS into a more compact file, but makes is less readiable.");
            Console.WriteLine("");
            Console.WriteLine("EXAMPLE:");
            Console.WriteLine("CSS_Minifier.exe \"Input CSS file path\" \"Output minimized CSS file path\" [/L]");
            Console.WriteLine("--------------------------------------------------------------------------------");
            Console.WriteLine("/L, /Log     Writes general log information out to the console.");
        }
    }
}
