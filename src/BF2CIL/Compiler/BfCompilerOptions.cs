namespace BF2CIL.Compiler;

public class BfCompilerOptions {
    public required string Name { get; set; }

    public required string Version { get; set; }

    public required int CellCount { get; set; }

    public required bool InterceptInput { get; set; }

    public static BfCompilerOptions CreateDefault(string name, string version) {
        return new BfCompilerOptions {
            Name = name,
            Version = version,
            CellCount = 30000,
            InterceptInput = true,
        };
    }
}
