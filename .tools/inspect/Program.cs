using System;
using System.IO;
using System.Linq;
using System.Reflection;

class Program
{
    static int Main(string[] args)
    {
        string defaultPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages", "microsoft.openapi", "3.8.0", "lib", "netstandard2.0", "Microsoft.OpenApi.dll");
        string path;
        string mode = string.Empty;
        string typeName = string.Empty;

        if (args.Length > 0 && File.Exists(args[0]))
        {
            path = args[0];
            if (args.Length > 1)
            {
                mode = args[1];
                if (args.Length > 2) typeName = args[2];
            }
        }
        else if (args.Length > 0 && args[0].Equals("members", StringComparison.OrdinalIgnoreCase))
        {
            path = defaultPath;
            mode = args[0];
            if (args.Length > 1) typeName = args[1];
        }
        else
        {
            path = defaultPath;
        }

        if (!File.Exists(path))
        {
            Console.Error.WriteLine($"Assembly not found: {path}");
            return 2;
        }
        var asm = Assembly.LoadFrom(path);
        var types = asm.GetExportedTypes();
        if (mode.Equals("members", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(typeName))
        {
            var name = typeName;
            var t = types.FirstOrDefault(tt => tt.Name.Equals(name, StringComparison.OrdinalIgnoreCase) || tt.FullName!.EndsWith(name, StringComparison.OrdinalIgnoreCase));
            if (t == null)
            {
                Console.Error.WriteLine($"Type not found: {name}");
                return 3;
            }
            Console.WriteLine($"Type: {t.FullName}");
            Console.WriteLine("Properties:");
            foreach (var p in t.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
                Console.WriteLine($"  {p.PropertyType.FullName} {p.Name}");
            Console.WriteLine("Fields:");
            foreach (var f in t.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
                Console.WriteLine($"  {f.FieldType.FullName} {f.Name}");
            Console.WriteLine("Methods:");
            foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static).Where(m => !m.IsSpecialName))
                Console.WriteLine($"  {m.ReturnType.FullName} {m.Name}({string.Join(", ", m.GetParameters().Select(p => p.ParameterType.FullName + " " + p.Name))})");
        }
        else
        {
            foreach (var t in types.OrderBy(t => t.Namespace).ThenBy(t => t.Name))
            {
                Console.WriteLine($"{t.Namespace}.{t.Name}");
            }
        }
        return 0;
    }
}
