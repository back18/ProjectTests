using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuanLib.VisualObject.TestConsole
{
    public static class Program
    {
        private static void Main(string[] args)
        {
            File.WriteAllText("test.txt", "hello, world!");
            using FileStream fileStream = File.OpenRead("test.txt");
            string text = new TypeView(fileStream.GetType(), fileStream).ToString();
            Console.WriteLine(text);
        }
    }
}
