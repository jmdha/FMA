using CommandLine;
using CommandLine.Text;

namespace Tools
{
    public abstract class BaseCLI
    {
        public static void HandleParseError(IEnumerable<Error> errs)
        {
            var sentenceBuilder = SentenceBuilder.Create();
            foreach (var error in errs)
                if (error is not HelpRequestedError)
                    ConsoleHelper.WriteLineColor(sentenceBuilder.FormatError(error), ConsoleColor.Red);
        }

        public static void DisplayHelp<T>(ParserResult<T> result, IEnumerable<Error> errs)
        {
            var helpText = HelpText.AutoBuild(result, h =>
            {
                h.AddEnumValuesToHelpText = true;
                return h;
            }, e => e, verbsIndex: true);
            Console.WriteLine(helpText);
            HandleParseError(errs);
        }
    }
}
