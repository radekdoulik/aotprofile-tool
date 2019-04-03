using System;
using System.IO;
using Mono.Options;
using Mono.Profiler.Aot;

using static System.Console;

namespace aotprofiletool {
	class MainClass {
		static bool Verbose;
		static bool Summary;
		static readonly string Name = "aotprofile-tool";

		static string ProcessArguments (string [] args)
		{
			var help = false;
			var options = new OptionSet {
				$"Usage: {Name}.exe OPTIONS* <aotprofile-file>",
				"",
				"Processes AOTPROFILE files created by Mono's AOT Profiler",
				"",
				"Copyright 2019 Microsoft Corporation",
				"",
				"Options:",
				{ "h|help|?",
					"Show this message and exit",
				  v => help = v != null },
				{ "s|summary",
					"Show summary of profile",
				  v => Summary = true },
				{ "v|verbose",
					"Output information about progress during the run of the tool",
				  v => Verbose = true },
			};

			var remaining = options.Parse (args);

			if (help || args.Length < 1) {
				options.WriteOptionDescriptions (Out);

				Environment.Exit (0);
			}

			if (remaining.Count != 1) {
				Error ("Please specify one <aotprofile-file> to process.");
				Environment.Exit (2);
			}

			return remaining [0];
		}

		public static void Main (string [] args)
		{
			var path = ProcessArguments (args);
			var reader = new ProfileReader ();
			ProfileData pd;

			using (var stream = new FileStream (path, FileMode.Open)) {
				if (Verbose)
					ColorWriteLine ($"Reading '{path}'...", ConsoleColor.Yellow);

				pd = reader.Read (stream);
			}

			if (Summary) {
				ColorWriteLine ($"Summary:", ConsoleColor.Green);
				WriteLine ($"\tmodules: {pd.Modules.Count}");
				WriteLine ($"\ttypes:   {pd.Types.Count}");
				WriteLine ($"\tmethods: {pd.Methods.Count}");
			}
		}

		static void ColorMessage (string message, ConsoleColor color, TextWriter writer, bool writeLine = true)
		{
			ForegroundColor = color;

			if (writeLine)
				writer.WriteLine (message);
			else
				writer.Write (message);

			ResetColor ();
		}

		public static void ColorWriteLine (string message, ConsoleColor color) => ColorMessage (message, color, Out);

		public static void ColorWrite (string message, ConsoleColor color) => ColorMessage (message, color, Out, false);

		public static void Error (string message) => ColorMessage ($"Error: {Name}: {message}", ConsoleColor.Red, Console.Error);

		public static void Warning (string message) => ColorMessage ($"Warning: {Name}: {message}", ConsoleColor.Yellow, Console.Error);
	}
}
