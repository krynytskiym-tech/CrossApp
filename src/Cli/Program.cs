using System.Runtime.InteropServices;
using System.Text.Json;

var info = new
{
    App = "CrossApp - практикум з крос-платформного програмування",
    Student = "Криницький Максим, група ФЕІ-21",
    OSDescription = RuntimeInformation.OSDescription,
    OSVersion = Environment.OSVersion.ToString(),
    ProcessArchitecture = RuntimeInformation.ProcessArchitecture.ToString(),
    DotNetVersion = Environment.Version.ToString(),
    Runtime = RuntimeInformation.FrameworkDescription,
    BaseDirectory = AppContext.BaseDirectory,
    CurrentDirectory = Environment.CurrentDirectory,
    Domain = "Склад (товари, партії, залишки, переміщення)"
};

if (args.Contains("--json"))
{
    var options = new JsonSerializerOptions { WriteIndented = true };
    Console.WriteLine(JsonSerializer.Serialize(info, options));
}
else
{
    Console.WriteLine(info.App);
    Console.WriteLine($"Студент: {info.Student}");
    Console.WriteLine(new string('-', 52));
    Console.WriteLine($"ОС (OSDescription)  : {info.OSDescription}");
    Console.WriteLine($"ОС (Environment)    : {info.OSVersion}");
    Console.WriteLine($"Архітектура процесу : {info.ProcessArchitecture}");
    Console.WriteLine($"Версія .NET (CLR)   : {info.DotNetVersion}");
    Console.WriteLine($"Runtime             : {info.Runtime}");
    Console.WriteLine($"Каталог застосунку  : {info.BaseDirectory}");
    Console.WriteLine($"Поточний каталог    : {info.CurrentDirectory}");
    Console.WriteLine(new string('-', 52));
    Console.WriteLine($"Предметна область: {info.Domain}");
}