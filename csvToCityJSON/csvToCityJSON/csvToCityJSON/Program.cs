using System;


namespace csvToCityJSON
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            csvTocityJsonConverter converter = new csvTocityJsonConverter();
            converter.start();
            
        }
    }
}
