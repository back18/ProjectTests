using QuanLib.Commands;
using QuanLib.Commands.CommandLine;
using QuanLib.Consoles;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace QuanLib.Core.TestConsole
{
    public static class Program
    {
        private static void Main(string[] args)
        {
            //Console.Write(new string('\n', 10000));

            CharacterWidthMapping.LoadInstance(new(File.ReadAllBytes("cache.bin")));
            CommandManager commandManager = BuildNotNetRuntimeCommand();
            ConsoleCommandReader consoleCommandReader = new(commandManager);

            //Console.Write(new string('x', 32));

            while (true)
            {
                consoleCommandReader.Start();
                consoleCommandReader.WaitForStop();
                CommandReaderResult result = consoleCommandReader.GetResult();

                if (result.Command is null)
                {
                    Console.WriteLine("未知或不完整命令");
                }
                else
                {
                    try
                    {
                        string message = result.Command.Execute(new("back18", PrivilegeLevel.Root), result.Args.ToArray());
                        Console.WriteLine(message);
                    }
                    catch (AggregateException aggregateException)
                    {
                        foreach (Exception innerException in aggregateException.InnerExceptions)
                            Console.WriteLine(ObjectFormatter.Format(innerException));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ObjectFormatter.Format(ex));
                    }
                }
            }
        }

        private static CommandManager BuildNotNetRuntimeCommand()
        {
            CommandManager commandManager = new();
            Assembly assembly = typeof(string).Assembly;
            Type[] types = assembly.GetTypes();

            foreach (Type type in types)
            {
                if (!type.IsPublic)
                    continue;

                if (!type.IsClass && !type.IsValueType && !type.IsInterface)
                    continue;

                if (type.IsGenericType)
                    continue;

                FieldInfo[] fields = type.GetFields(BindingFlags.Static | BindingFlags.Public);
                PropertyInfo[] properties = type.GetProperties(BindingFlags.Static | BindingFlags.Public);
                MethodInfo[] methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public);

                List<string> baseWords = [];
                if (type.Namespace is not null)
                    baseWords.AddRange(type.Namespace.Split('.'));
                baseWords.Add(type.Name);

                foreach (FieldInfo field in fields)
                {
                    string[] words = new string[baseWords.Count + 1];
                    baseWords.CopyTo(words, 0);
                    words[^1] = field.Name;

                    commandManager.Register(
                    new CommandBuilder()
                    .On(string.Join(' ', words))
                    .Execute<Func<object?>>(() => field.GetValue(null))
                    .SetFormatMessageHandler(ObjectFormatter.Format)
                    .Build());
                }

                foreach (PropertyInfo property in properties)
                {
                    string[] words = new string[baseWords.Count + 1];
                    baseWords.CopyTo(words, 0);
                    words[^1] = property.Name;

                    commandManager.Register(
                    new CommandBuilder()
                    .On(string.Join(' ', words))
                    .Execute<Func<object?>>(() => property.GetValue(null))
                    .SetFormatMessageHandler(ObjectFormatter.Format)
                    .Build());
                }

                foreach (MethodInfo method in methods)
                {
                    if (method.Name.StartsWith("get_") ||
                        method.Name.StartsWith("set_") ||
                        method.Name.StartsWith("add_") ||
                        method.Name.StartsWith("remove_") ||
                        method.Name.StartsWith("op_"))
                        continue;

                    if (method.IsGenericMethod)
                        continue;

                    ParameterInfo[] parameters = method.GetParameters();
                    foreach (ParameterInfo parameter in parameters)
                    {
                        if (parameter.ParameterType == typeof(string))
                            continue;

                        if (!ParserBuilder.IsImplIParsable(parameter.ParameterType))
                            goto end;
                    }

                    string[] words = new string[baseWords.Count + 1];
                    baseWords.CopyTo(words, 0);

                    words[^1] = method.Name;
                    string identifiers = string.Join(' ', words);

                    if (commandManager.ContainsKey(identifiers))
                    {
                        words[^1] = $"{method.Name}_{string.Join('_', parameters.Select(s => s.ParameterType.Name))}";
                        identifiers = string.Join(' ', words);
                    }

                    commandManager.Register(
                    new CommandBuilder()
                    .On(identifiers)
                    .Execute(null, method)
                    .SetFormatMessageHandler(ObjectFormatter.Format)
                    .Build());

                    end:
                    continue;
                }
            }

            return commandManager;
        }
    }
}
