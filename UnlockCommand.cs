using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace HuaweiBootloader.Bruteforce
{
    internal sealed class UnlockCommand : AsyncCommand<UnlockCommand.Settings>
    {
        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            var unlocker = ImeiUnlocker.Create(settings.FastbootPath, settings.Imei, settings.StartFrom, settings.TimeOut);
            var totalCount = unlocker.EnumerateOemCodes().Count();

            await AnsiConsole.Progress()
                //.AutoRefresh(false) // Turn off auto refresh
                //.AutoClear(false)   // Do not remove the task list when done
                //.HideCompleted(false)   // Hide tasks as they are completed
                .Columns(new ProgressColumn[]
                {
                    new TaskDescriptionColumn(), // Task description
                    new ProgressBarColumn(), // Progress bar
                    new PercentageColumn(), // Percentage
                    new SpinnerColumn(),
                })
                .StartAsync(async ctx =>
                {
                    var task = ctx.AddTask($"[green]Starting...[/]");

                    await foreach (var response in unlocker.EnumerateUnlock())
                    {
                        task.Description = $"[green]Trying... '{response.OemCode}'[/]";
                        var increment = 100 / (double)totalCount;
                        task.Increment(increment);
                        //AnsiConsole
                        //    .Status()
                        //    .Start("Working...", status =>
                        //    {
                        //        status.Status(response.FastbootStdOutput);
                        //        status.Spinner(Spinner.Known.Aesthetic);
                        //    });
                    };

                });

            return 1;
        }

        public sealed class Settings : CommandSettings
        {
            [CommandOption("--imei|-i")]
            [Description("Phone IMEI")]
            public string Imei { get; init; }

            [CommandOption("--start|-s")]
            [Description("Initial OEM Code to Start Incrementing the bruteforce from")]
            [DefaultValue(1_000_000_000_000_000)]
            public long StartFrom { get; init; }

            [CommandOption("--fastboot-path")]
            [Description("Fastboot Path")]
            public string FastbootPath { get; init; } = Path.Combine(AppContext.BaseDirectory, "fastboot", "fastboot.exe");

            [CommandOption("--delay|-d")]
            [Description("Delay between each try (in Milliseconds)")]
            [DefaultValue(100)]
            public int Delay { get; init; }

            [CommandOption("--timeout|-t")]
            [Description("Timeout for each fastboot instance (in Milliseconds)")]
            [DefaultValue(2000)]
            public int TimeOut { get; init; }
        }

    }
}
