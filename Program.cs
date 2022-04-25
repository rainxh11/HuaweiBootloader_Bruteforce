using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HuaweiBootloader.Bruteforce
{
    internal class Program
    {
        static void ExitFastboot()
        {
            Process
                .GetProcesses()
                .Where(x => x.ProcessName.Contains("fastboot", StringComparison.CurrentCultureIgnoreCase))
                .ToList()
                .ForEach(x => x.Kill());
        }
        static async Task Main(string[] args)
        {
            ExitFastboot();

            Console.CancelKeyPress += (o, e) =>
            {
                ExitFastboot();
                Process.GetCurrentProcess().Kill();
            };

            var font = FigletFont.Load(new MemoryStream(Properties.Resources.figletfont));

            args = new string[]
            {
                "unlock",
                "--imei=359489090277833"
            };

            AnsiConsole.Write(
                new FigletText(font, "XH")
                    .Centered()
                    .Color(Color.Red)
            );

            var app = new CommandApp();
            app.Configure(config =>
            {
                config.AddCommand<UnlockCommand>("unlock")
                    .WithExample(new string[]
                    {
                        "unlock",
                        "--imei=359489090277833",
                        "--delay=750",
                        "--fastboot-path=.\\Fastboot\\fastboot.exe"
                    });
            });

            await app.RunAsync(args);
            ExitFastboot();
        }
    }
}
