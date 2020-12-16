using System;
using Service;

namespace Entry
{
    public class Program
    {
        static void Main(string[] args)
        {
            OpcuaManagement server = new OpcuaManagement();
            server.CreateServerInstance();
            Console.WriteLine("OPC UA服务已启动....");
            Console.WriteLine();
        }
    }
}
