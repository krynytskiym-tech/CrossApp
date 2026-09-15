using System.Text.Json;
using Core;

var env = EnvironmentInfo.Collect();

var info = new
{
    App = "CrossApp - практикум з крос-платформного програмування",
    Student = "Криницький Максим, група ФЕІ-33",
    env.OsDescription,
    env.OsVersion,
    env.ProcessArchitecture,
    env.DotNetVersion,
    env.Runtime,
    env.BaseDirectory,
    env.CurrentDirectory,
    env.DetectedRid,
    env.ReportedRid,
    env.BuildNote,
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
    Console.WriteLine($"ОС (OSDescription)  : {info.OsDescription}");
    Console.WriteLine($"ОС (Environment)    : {info.OsVersion}");
    Console.WriteLine($"Архітектура процесу : {info.ProcessArchitecture}");
    Console.WriteLine($"Версія .NET (CLR)   : {info.DotNetVersion}");
    Console.WriteLine($"Runtime             : {info.Runtime}");
    Console.WriteLine($"Каталог застосунку  : {info.BaseDirectory}");
    Console.WriteLine($"Поточний каталог    : {info.CurrentDirectory}");
    Console.WriteLine($"RID (визначено)     : {info.DetectedRid}");
    Console.WriteLine($"RID (від .NET)      : {info.ReportedRid}");
    Console.WriteLine($"Збірка              : {info.BuildNote}");
    Console.WriteLine(new string('-', 52));
    Console.WriteLine($"Предметна область: {info.Domain}");
}