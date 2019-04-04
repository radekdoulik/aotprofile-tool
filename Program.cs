using System;
using System.IO;
using System.Text.RegularExpressions;
using Mono.Options;
using Mono.Profiler.Aot;

using static System.Console;

namespace aotprofiletool {
	class MainClass {
		static readonly string Name = "aotprofile-tool";

		static bool Methods;
		static bool Modules;
		static bool Summary;
		static bool Types;
		static bool Verbose;

		static Regex FilterMethod;
		static Regex FilterModule;
		static Regex FilterType;

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
				{ "a|all",
					"Show modules, types and methods in the profile",
				  v => Modules = Types = Methods = true },
				{ "d|modules",
					"Show modules in the profile",
				  v => Modules = true },
				{ "filter-method=",
					"Filter by method with regex VALUE",
				  v => FilterMethod = new Regex (v) },
				{ "filter-module=",
					"Filter by module with regex VALUE",
				  v => FilterModule = new Regex (v) },
				{ "filter-type=",
					"Filter by type with regex VALUE",
				  v => FilterType = new Regex (v) },
				{ "m|methods",
					"Show methods in the profile",
				  v => Methods = true },
				{ "s|summary",
					"Show summary of the profile",
				  v => Summary = true },
				{ "t|types",
					"Show types in the profile",
				  v => Types = true },
				{ "v|verbose",
					"Output information about progress during the run of the tool",
				  v => Verbose = true },
				"",
				"If no other option than -v is used then --all is used by default"
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

			if (!File.Exists (path)) {
				Error ($"'{path}' doesn't exist.");
				Environment.Exit (3);
			}

			if (args.Length == 1 || (args.Length == 2 && Verbose)) {
				Modules = Types = Methods = true;
			}

			var reader = new ProfileReader ();
			ProfileData pd;

			using (var stream = new FileStream (path, FileMode.Open)) {
				if (Verbose)
					ColorWriteLine ($"Reading '{path}'...", ConsoleColor.Yellow);

				pd = reader.Read (stream);
			}



			if (Modules)
				ColorWriteLine ($"Modules:", ConsoleColor.Green);

			int modules = FilterModule == null ? pd.Modules.Count : 0;

			foreach (var module in pd.Modules) {
				if (FilterModule != null) {
					var match = FilterModule.Match (module.Name);

					if (!match.Success)
						continue;

					modules++;
				}

				if (Modules)
					WriteLine ($"\t{module.Mvid} {module}");
			}

			if (Types)
				ColorWriteLine ($"Types:", ConsoleColor.Green);

			int types = (FilterType == null && FilterModule == null) ? pd.Types.Count : 0;

			foreach (var type in pd.Types) {
				if (FilterModule != null) {
					var match = FilterModule.Match (type.Module.Name);

					if (!match.Success)
						continue;
				}

				if (FilterType != null) {
					var match = FilterType.Match (type.FullName);

					if (!match.Success)
						continue;
				}

				if (FilterType != null || FilterModule != null)
					types++;

				if (Types)
					WriteLine ($"\t{type}");
			}

			if (Methods)
				ColorWriteLine ($"Methods:", ConsoleColor.Green);

			int methods = (FilterMethod == null && FilterType == null && FilterModule == null) ? pd.Methods.Count : 0;

			foreach (var method in pd.Methods) {

				if (FilterModule != null) {
					var match = FilterModule.Match (method.Type.Module.ToString ());

					if (!match.Success)
						continue;
				}

				if (FilterType != null) {
					var match = FilterType.Match (method.Type.ToString ());

					if (!match.Success)
						continue;
				}

				if (FilterMethod != null) {
					var match = FilterMethod.Match (method.ToString ());

					if (!match.Success)
						continue;
				}

				if (FilterMethod != null || FilterType != null || FilterModule != null)
					methods++;

				if (Methods)
					WriteLine ($"\t{method}");
			}

			if (Summary) {
				ColorWriteLine ($"Summary:", ConsoleColor.Green);
				WriteLine ($"\tModules: {modules.ToString ("N0"),10}");
				WriteLine ($"\tTypes:   {types.ToString ("N0"),10}");
				WriteLine ($"\tMethods: {methods.ToString ("N0"),10}");
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
