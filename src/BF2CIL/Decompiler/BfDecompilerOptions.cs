namespace BF2CIL.Decompiler; 

/// <summary>
///     Options which control how a compiled Brainfuck program is decompiled.
/// </summary>
public class BfDecompilerOptions {
    /// <summary>
    ///     Initializes an instance of <see cref="BfDecompilerOptions"/> with
    ///     the standard, default options.
    /// </summary>
    /// <returns>
    ///     An initialized instance of <see cref="BfDecompilerOptions"/>
    ///     populated with default values.
    /// </returns>
    /// <remarks>
    ///     Useful for getting the standard defaults, which persists across API
    ///     versions.
    /// </remarks>
    public static BfDecompilerOptions CreateDefault() {
        return new BfDecompilerOptions();
    }
}
