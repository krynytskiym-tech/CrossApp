using Cli;

// Без аргументів — демонстрація доменної моделі (лабораторна 4).
// З шляхом до файлу — імпорт (лабораторна 3): dotnet run --project src/Cli -- data/sample.csv
return args.Length == 0
    ? DomainDemo.Run()
    : ImportCommand.Run(args[0]);
