using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CommonCode
{

	public static class Commands
	{
		static Commands()
		{
			var items =
			  typeof(Commands)
			  .GetFields(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)
			  .Where(x => x.FieldType == typeof(ICommandLineCommand))
			  .Select(x => x.GetValue(null) as ICommandLineCommand)
			  .ToDictionary(x => x.CommandKeyword);

			dics = new SortedDictionary<string, ICommandLineCommand>(
				items,
				Comparer<string>.Create(
					(x, y) =>
					string.Compare(x, y, StringComparison.InvariantCultureIgnoreCase)));

		}

		static SortedDictionary<string, ICommandLineCommand>
		   dics;

		public static ICommandLineCommand GetCommand(string commandName)
		{
			return dics[commandName];
		}

		public static readonly ICommandLineCommand DPGRP
		#region DPGRP
		= new CommandLineCommand(nameof(DPGRP), null)
		{

			OnExecute = args =>
			{
				if (args.Length < 3)
				{
					throw new IndexOutOfRangeException("Need at least 2 file path: from, to");

				}
				var f1 = args[1];
				var f2 = args[2];

				if (!File.Exists(f1))
				{
					throw new ArgumentException("from file not exists");
				}

				if (!File.Exists(f2))
				{
					throw new ArgumentException("to file not exists");
				}

				var elementFrag = XDocument.Load(f1)
				 .Descendants().Single(x => x.Name.LocalName == "packages");
				var framework = elementFrag.Elements().First().Attribute("targetFramework").Value;

				var docnusp = XDocument.Load(f2);
				var dependencies = docnusp
				.Descendants().Single(x => x.Name.LocalName  == "dependencies");

				var ns = dependencies.Name.NamespaceName;
				var gp = dependencies.Elements()
					  .Where(g => g.Name.LocalName == "group")
					  .Where(g => g.Attributes()
						 .Where(x => x.Name == "targetFramework")
						 .Single()
						 .Value == framework)
					  .FirstOrDefault();
				if (gp == null)
				{
					gp = new XElement(XName.Get("group" ,ns), new XAttribute("targetFramework", framework));
					dependencies.Add(gp);
				}
				else
				{
					gp.RemoveNodes();
				}

				foreach (var ele in elementFrag.Elements().Where(x => x.Name.LocalName == "package"))
				{
					ele.Name = XName.Get("dependency", ns);
					var remover = ele.Attributes().Where(
						x => x.Name == "targetFramework"
						);
					foreach (var item in remover)
					{
						Console.WriteLine(item.Name);
						item.Remove();
					}
					remover = ele.Attributes().Where(
						x => x.Name == "userInstalled"
						);
					foreach (var item in remover)
					{
						Console.WriteLine(item.Name);
						item.Remove();
					}


					var versions = ele.Attributes().Where(
						x => x.Name == "version");
					foreach (var version in versions)
					{

						if (version.Value.Trim(new[] { '(', ')', '[', ']' }) == version.Value)
						{
							version.Value = string.Format("[{0},{1})", version.Value, "100.0");
						}
					}


					gp.Add(ele);
				}

				docnusp.Save(f2);

			}


		};
		#endregion



	}
}