using Postulate.Orm.Abstract;
using Postulate.Orm.Attributes;
using Postulate.Orm.Merge;
using Postulate.Orm.MySql;
using Postulate.Orm.SqlServer;
using ReflectionHelper;
using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Postulate.Orm.Commands
{
    public enum Action
    {
        Preview,
        Execute,
        Validate,
        SaveScript
    }

    internal class Program
    {
        private static void Main(string[] args)
        {
            try
            {
                if (args.Length == 0) throw new InvalidOperationException("MergeModel requires an assembly file name argument.");

                string folder;
                string fileNameBase;
                string modelClassAssembly = FindModelAssembly(args, out folder, out fileNameBase);
                string connectionName = FindConnectionName(args);
                Action action = FindAction(args);

                Assembly assembly = Assembly.LoadFile(modelClassAssembly);
                var config = ConfigurationManager.OpenExeConfiguration(assembly.Location);                

                DefaultSqlSyntaxAttribute syntaxAttr;
                SupportedSyntax syntaxValue = (assembly.HasAttribute(out syntaxAttr)) ? syntaxAttr.Syntax : FindSyntax(args);

                switch (syntaxValue)
                {
                    case SupportedSyntax.MySql:
                        RunMerge<MySqlDb<int>, MySqlSyntax>(assembly, new MySqlDb<int>(config, connectionName), action);
                        break;

                    case SupportedSyntax.SqlServer:
                        RunMerge<SqlServerDb<int>, SqlServerSyntax>(assembly, new SqlServerDb<int>(config, connectionName), action);
                        break;
                }
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc.Message);
            }
            Console.ReadLine();
        }

        private static Action FindAction(string[] args)
        {
            string result;
            if (TryFindNameInArray(args, "action=", out result))
            {
                return (Action)Enum.Parse(typeof(Action), result, true);
            }
            return Action.Preview;
        }

        private static void RunMerge<TDb, TSyntax>(Assembly assembly, TDb db, Action action)
            where TDb : SqlDb<int>
            where TSyntax : SqlSyntax, new()
        {
            Console.WriteLine(action.ToString());

            db.CreateIfNotExists();

            var engine = new Engine<TSyntax>(assembly, new Progress<MergeProgress>(ShowProgress));
            using (var cn = db.GetConnection())
            {
                var actions = engine.CompareAsync(cn).Result;
                var script = engine.GetScript(cn, actions).ToString();

                SaveScript(assembly.Location, script);
                Console.WriteLine();
                Console.WriteLine(script);

                switch (action)
                {
                    case Action.Validate:
                        break;

                    case Action.Preview:                        
                        break;

                    case Action.Execute:
                        engine.ExecuteAsync(cn, actions).Wait();
                        Console.WriteLine("Script executed successfully.");
                        break;
                }
            }
        }

        private static void SaveScript(string assemblyPath, string script)
        {
            string fileName = Path.GetFileNameWithoutExtension(assemblyPath) + ".sql";
            string outputFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Model Merge", fileName);

            string folder = Path.GetDirectoryName(outputFile);
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            using (StreamWriter writer = File.CreateText(outputFile))
            {
                writer.Write(script);
            }
        }

        private static string FindConnectionName(string[] args)
        {
            string result;
            return (TryFindNameInArray(args, "connection=", out result)) ? result : "DefaultConnection";
        }

        private static void ShowProgress(MergeProgress obj)
        {
            Console.WriteLine(obj);
        }

        private static SupportedSyntax FindSyntax(string[] args)
        {
            SupportedSyntax result;

            const string syntaxKey = "syntax=";

            string output;
            if (TryFindNameInArray(args, syntaxKey, out output))
            {
                if (Enum.TryParse(output, out result)) return result;
            }

            throw new ArgumentException(
                @"Couldn't determine the SQL syntax to use. Please specify an argument starting with ""syntax="" followed by either SqlServer or MySql.
                Alternatively, you can add a [DefaultSqlSyntax] attribute to your assembly.");
        }

        private static string FindModelAssembly(string[] args, out string folder, out string fileNameBase)
        {
            string result = args[0];
            if (!File.Exists(result)) result = Path.Combine(Assembly.GetExecutingAssembly().Location, args[0]);
            if (!File.Exists(result)) throw new FileNotFoundException($"Assembly file '{result}' was not found.");

            folder = (File.Exists(result)) ? Path.GetDirectoryName(result) : null;
            fileNameBase = Path.GetFileNameWithoutExtension(result);

            return result;
        }

        private static bool TryFindNameInArray(string[] array, string startsWith, out string result)
        {
            result = array.FirstOrDefault(s => s.StartsWith(startsWith));
            if (result != null)
            {
                result = result.Substring(startsWith.Length).Trim();
                return true;
            }
            return false;
        }
    }
}