namespace BF2CIL.Compiler;

/// <summary>
///     Options which control how a Brainfuck program is compiled.
/// </summary>
public class BfCompilerOptions {
    /// <summary>
    ///     The name of the assembly and namespace which contains the main
    ///     <c>Program</c> type.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    ///     The version of the assembly.
    /// </summary>
    public required string Version { get; set; }

    /// <summary>
    ///     The number of cells to initialize.
    /// </summary>
    public required int CellCount { get; set; }

    /// <summary>
    ///     Whether user input should be intercepted (not displayed back to the
    ///     user).
    /// </summary>
    public required bool InterceptInput { get; set; }

    /// <summary>
    ///     Initializes an instance of <see cref="BfCompilerOptions"/> with the
    ///     standard, default options.
    /// </summary>
    /// <param name="name">The value of <see cref="Name"/>.</param>
    /// <param name="version">The value of <see cref="Version"/>.</param>
    /// <returns>
    ///     An initialized instance of <see cref="BfCompilerOptions"/> populated
    ///     with default values.
    /// </returns>
    /// <remarks>
    ///     Useful for getting the standard defaults, which persists across API
    ///     versions.
    /// </remarks>
    public static BfCompilerOptions CreateDefault(string name, string version) {
        return new BfCompilerOptions {
            Name = name,
            Version = version,
            CellCount = 30000,
            InterceptInput = true,
        };
    }
}
