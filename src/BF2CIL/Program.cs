using System.IO;
using System.Threading.Tasks;
using BF2CIL.Compiler;
using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using JetBrains.Annotations;

namespace BF2CIL;

internal static class Program {
    public static async Task<int> Main(string[] args) {
        return await new CliApplicationBuilder()
                     .AddCommand<Command>()
                     .Build()
                     .RunAsync(args);
    }
}

[UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
[Command]
internal sealed class Command : ICommand {
    [UsedImplicitly(ImplicitUseKindFlags.Assign)]
    [CommandParameter(0, Name = "input", Description = "Input file path", IsRequired = true)]
    public string InputPath { get; init; } = null!;

    [UsedImplicitly(ImplicitUseKindFlags.Assign)]
    [CommandOption("output", 'o', Description = "Output file path")]
    public string? OutputPath { get; set; }

    [UsedImplicitly(ImplicitUseKindFlags.Assign)]
    [CommandOption("name", 'n', Description = "Assembly name")]
    public string? Name { get; set; }

    [UsedImplicitly(ImplicitUseKindFlags.Assign)]
    [CommandOption("version", 'v', Description = "Assembly version")]
    public string Version { get; set; } = "1.0.0";

    [UsedImplicitly(ImplicitUseKindFlags.Assign)]
    [CommandOption("cell-count", 'c', Description = "Cell count")]
    public int CellCount { get; set; } = 30000;

    [UsedImplicitly(ImplicitUseKindFlags.Assign)]
    [CommandOption("overwrite", 'w', Description = "Overwrite output files")]
    public bool Overwrite { get; set; }

    [UsedImplicitly(ImplicitUseKindFlags.Assign)]
    [CommandOption("generate-runtime-config", 'r', Description = "Generate runtime config")]
    public bool GenerateRuntimeConfig { get; set; } = true;

    ValueTask ICommand.ExecuteAsync(IConsole console) {
        if (!File.Exists(InputPath))
            throw new FileNotFoundException("Input file not found", InputPath);

        OutputPath ??= Path.GetFileNameWithoutExtension(InputPath) + ".dll";
        if (!Overwrite && File.Exists(OutputPath))
            throw new IOException("Output file already exists");

        Name ??= Path.GetFileNameWithoutExtension(InputPath);

        var input = File.ReadAllText(InputPath);
        var options = BfCompilerOptions.CreateDefault(Name, Version);
        options.CellCount = CellCount;
        var output = BfCompiler.Compile(
            input,
            options
        );
        File.WriteAllBytes(OutputPath, output);

        if (GenerateRuntimeConfig) {
            var runtimeConfigPath = Path.ChangeExtension(OutputPath, ".runtimeconfig.json");
            if (!Overwrite && File.Exists(runtimeConfigPath))
                throw new IOException("Runtime config file already exists");

            File.WriteAllText(
                runtimeConfigPath,
                """{"runtimeOptions": {"tfm": "net7.0","framework": {"name": "Microsoft.NETCore.App","version": "7.0.0"}}}"""
            );
        }

        return default;
    }
}
