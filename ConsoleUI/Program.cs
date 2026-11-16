using Microsoft.Extensions.DependencyInjection;
using POS.ConsoleUI;

var services = new ServiceCollection();

services.AddPosServices();

using var serviceProvider = services.BuildServiceProvider();

var controller = serviceProvider
    .GetRequiredService<ConsoleController>();

try
{
    while (true)
    {
        Console.WriteLine();
        controller.Run();

        Console.WriteLine();
        if (!ConsoleInput.ReadYesNo("Вернуться в главное меню? (д/н): "))
            break;
    }
}
catch (EndOfStreamException)
{
    Console.WriteLine("\nВвод завершён.");
}

Console.WriteLine("Работа завершена.");
