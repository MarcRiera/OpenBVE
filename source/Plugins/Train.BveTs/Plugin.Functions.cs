using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Train.BveTs
{
	public static class Functions
	{
		public class BvetsHeader
		{
			public readonly bool IsValid;
			public readonly string Type;
			public readonly Version Version;

			public BvetsHeader(bool isValid, string type, Version version)
			{
				IsValid = isValid;
				Type = type;
				Version = version;
			}
		}

		/// <summary>Parses the header of a BveTs config file</summary>
		/// <param name="file">The  path of the file to parse</param>
		/// <returns>The parsed header</returns>
		public static BvetsHeader ReadBvetsHeader(string file)
		{
			string[] lines = File.ReadAllLines(file, Encoding.UTF8);
			lines = RemoveComments(lines);

			bool isValid = false;
			string type = string.Empty;
			Version version = new Version();

			for (int i = 0; i < lines.Length; i++)
			{
				if (lines[i].StartsWith(@"bvets ", StringComparison.InvariantCultureIgnoreCase))
				{
					int typeStart = lines[i].IndexOf(' ');
					int typeEnd = lines[i].LastIndexOf(' ');
					if (typeStart == typeEnd)
					{
						// We need at least three elements (two spaces): type of file and version number
						continue;
					}

					type = lines[i].Substring(typeStart, typeEnd - typeStart).Trim();

					string[] versionEncoding = lines[i].Substring(typeEnd, lines[i].Length - typeEnd).Split(':');
					Version.TryParse(versionEncoding[0], out version);

					isValid = true;
					break;
				}
			}

			return new BvetsHeader(isValid, type, version);
		}

		public static string[] RemoveComments(string[] str)
		{
			for (int i = 0; i < str.Length; i++)
			{
				str[i] = Regex.Replace(str[i], "[;#]" + ".+", string.Empty).Trim();
			}
			return str;
		}
	}
}
