using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace BF2CIL.Decompiler;

/// <summary>
///     Handles the decompilation of a compiled Brainfuck program to raw text.
/// </summary>
public static class BfDecompiler {
    /// <summary>
    ///     Decompiles the compiled Brainfuck program <paramref name="module"/>
    ///     to raw text.
    ///     <br />
    ///     This method only decompiles the entrypoint of a module. To decompile
    ///     individual <see cref="MethodDefinition"/>s, use
    ///     <see cref="Decompile(MethodDefinition,BfDecompilerOptions)"/>.
    /// </summary>
    /// <param name="module">The compiled Brainfuck program.</param>
    /// <param name="options">Decompilation options.</param>
    /// <returns>The decompilation data.</returns>
    public static BfDecompilation Decompile(
        ModuleDefinition module,
        BfDecompilerOptions options) {
        return Decompile(module.EntryPoint, options);
    }

    /// <summary>
    ///     Decompiles the compiled Brainfuck program <paramref name="method"/>
    ///     to raw text.
    /// </summary>
    /// <param name="method">The compiled Brainfuck program.</param>
    /// <param name="options">Decompilation options.</param>
    /// <returns>The decompilation data.</returns>
    public static BfDecompilation Decompile(
        MethodDefinition method,
        BfDecompilerOptions options) {
        _ = options;
        
        var cursor = new ILCursor(new ILContext(method));
        var decompilation = new BfDecompilation();
        var program = new List<char>();

        if (cursor.Next.OpCode != OpCodes.Ldc_I4)
            throw new Exception("Unexpected method body");

        decompilation.CellCount = (int) cursor.Next.Operand;

        cursor.Index += 5;

        Exception unexpectedOpcode(ILCursor c) {
            var name = c.Next.OpCode.Name;
            var pos = c.Index;
            return new Exception($"Unexpected opcode ({pos}): {name}");
        }

        while (cursor.Next != null) {
            if (cursor.Next.OpCode == OpCodes.Ldloc_1) {
                // >/<
                cursor.Index += 2;
                
                if (cursor.Next.OpCode == OpCodes.Add)
                    program.Add('>');
                else if (cursor.Next.OpCode == OpCodes.Sub)
                    program.Add('<');
                else
                    throw unexpectedOpcode(cursor);

                cursor.Index += 2;
            }
            else if (cursor.Next.OpCode == OpCodes.Ldloc_0) {
                // +/-/./,
                cursor.Index += 2;

                if (cursor.Next.OpCode == OpCodes.Ldelema) {
                    // +/-
                    cursor.Index += 4;
                    
                    if (cursor.Next.OpCode == OpCodes.Add)
                        program.Add('+');
                    else if (cursor.Next.OpCode == OpCodes.Sub)
                        program.Add('-');
                    else
                        throw unexpectedOpcode(cursor);
                    
                    cursor.Index += 3;
                }
                else if (cursor.Next.OpCode == OpCodes.Ldelem_U1) {
                    // .
                    program.Add('.');
                    cursor.Index += 2;
                }
                else if (cursor.Next.OpCode == OpCodes.Ldc_I4_1) {
                    // , (intercept true)
                    program.Add(',');
                    decompilation.InterceptInput = true;
                    cursor.Index += 7;
                }
                else if (cursor.Next.OpCode == OpCodes.Ldc_I4_0) {
                    // , (intercept false)
                    program.Add(',');
                    decompilation.InterceptInput = false;
                    cursor.Index += 7;
                }
                else {
                    throw unexpectedOpcode(cursor);
                }
            }
            else if (cursor.Next.OpCode == OpCodes.Br) {
                // [
                program.Add('[');
                cursor.Index += 2;
            }
            else if (cursor.Next.OpCode == OpCodes.Nop) {
                // ]
                program.Add(']');
                cursor.Index += 5;
            }
            else if (cursor.Next.OpCode == OpCodes.Ret) {
                // :thumbsup:
                break;
            }
            else {
                throw unexpectedOpcode(cursor);
            }
        }

        decompilation.Program = program.ToArray();
        return decompilation;
    }
}
