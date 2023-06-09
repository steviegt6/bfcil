﻿using System;
using System.Collections.Generic;
using System.IO;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace BF2CIL.Compiler;

/// <summary>
///     Handles the compilation of a Brainfuck program to a .NET assembly.
/// </summary>
public static class BfCompiler {
    /// <summary>
    ///     Compiles the Brainfuck program <paramref name="source"/> to a .NET
    ///     assembly.
    /// </summary>
    /// <param name="source">The Brainfuck program.</param>
    /// <param name="options">Compilation options.</param>
    /// <returns>The compiled .NET assembly, as a byte array,</returns>
    public static byte[] Compile(string source, BfCompilerOptions options) {
        return Compile(source.ToCharArray(), options);
    }

    /// <summary>
    ///     Compiles the Brainfuck program <paramref name="source"/> to a .NET
    ///     assembly.
    /// </summary>
    /// <param name="source">The Brainfuck program.</param>
    /// <param name="options">Compilation options.</param>
    /// <returns>The compiled .NET assembly, as a byte array,</returns>
    public static byte[] Compile(char[] source, BfCompilerOptions options) {
        var name = options.Name;
        var nameDef = new AssemblyNameDefinition(
            name,
            Version.Parse(options.Version)
        );
        var asmDef = AssemblyDefinition.CreateAssembly(
            nameDef,
            name,
            ModuleKind.Console
        );
        var module = asmDef.MainModule;
        var ts = module.TypeSystem;

        var program = new TypeDefinition(
            name,
            "Program",
            TypeAttributes.Class
          | TypeAttributes.AutoLayout
          | TypeAttributes.AnsiClass
          | TypeAttributes.BeforeFieldInit,
            ts.Object
        );
        module.Types.Add(program);

        var ctor = new MethodDefinition(
            ".ctor",
            MethodAttributes.Public
          | MethodAttributes.HideBySig
          | MethodAttributes.SpecialName
          | MethodAttributes.RTSpecialName,
            ts.Void
        );
        var ctorIl = ctor.Body.GetILProcessor();
        var objCtor = module.ImportReference(
            typeof(object).GetConstructor(Type.EmptyTypes)
        );
        ctorIl.Append(ctorIl.Create(OpCodes.Ldarg_0));
        ctorIl.Append(ctorIl.Create(OpCodes.Call, objCtor));
        ctorIl.Append(ctorIl.Create(OpCodes.Nop));
        ctorIl.Append(ctorIl.Create(OpCodes.Ret));
        program.Methods.Add(ctor);

        var main = CompileBrainfuck("'<Main>$'", source, options, module);

        program.Methods.Add(main);
        module.EntryPoint = main;

        using var ms = new MemoryStream();
        module.Write(ms);
        return ms.ToArray();
    }

    /// <summary>
    ///     Compiles a Brainfuck program to a .NET method.
    /// </summary>
    /// <param name="methodName">The name of the method to compile to.</param>
    /// <param name="source">The Brainfuck program.</param>
    /// <param name="options">Compilation options.</param>
    /// <param name="module">
    ///     The module supplying the type system and various other type utilities.
    /// </param>
    /// <returns>The compiled Brainfuck program.</returns>
    /// <exception cref="Exception"></exception>
    public static MethodDefinition CompileBrainfuck(
        string methodName,
        char[] source,
        BfCompilerOptions options,
        ModuleDefinition module
    ) {
        var ts = module.TypeSystem;
        var write = module.ImportReference(
            typeof(Console).GetMethod("Write", new[] { typeof(char) })
        );
        var readKey = module.ImportReference(
            typeof(Console).GetMethod("ReadKey", new[] { typeof(bool) })
        );
        var interceptKey = options.InterceptInput
            ? OpCodes.Ldc_I4_1
            : OpCodes.Ldc_I4_0;
        var keyChar = module.ImportReference(
            typeof(ConsoleKeyInfo).GetProperty("KeyChar")!.GetMethod
        );

        var main = new MethodDefinition(
            methodName,
            MethodAttributes.Private
          | MethodAttributes.HideBySig
          | MethodAttributes.Static,
            ts.Void
        );
        var mainIl = main.Body.GetILProcessor();
        var mainLocals = main.Body.Variables;

        var loopHeads = new Stack<Instruction>();
        var loopBodies = new Stack<Instruction>();

        // TODO: We can optimize by omitting this var entirely when allowed.
        var cells = new VariableDefinition(ts.Byte.MakeArrayType());
        mainLocals.Add(cells);

        var ptr = new VariableDefinition(ts.Int32);
        mainLocals.Add(ptr);

        // TODO: Configurable data type?
        // byte[] cells = new byte[options.CellCount];
        mainIl.Append(mainIl.Create(OpCodes.Ldc_I4, options.CellCount));
        mainIl.Append(mainIl.Create(OpCodes.Newarr, ts.Byte));
        mainIl.Append(mainIl.Create(OpCodes.Stloc_0));

        // int ptr = 0;
        mainIl.Append(mainIl.Create(OpCodes.Ldc_I4_0));
        mainIl.Append(mainIl.Create(OpCodes.Stloc_1));

        // ConsoleKeyInfo keyBuf;
        var keyBuf = new VariableDefinition(
            module.ImportReference(typeof(ConsoleKeyInfo))
        );
        mainLocals.Add(keyBuf);

        foreach (var c in source) {
            switch (c) {
                case '>':
                    // ptr++;
                    mainIl.Append(mainIl.Create(OpCodes.Ldloc_1));
                    mainIl.Append(mainIl.Create(OpCodes.Ldc_I4_1));
                    mainIl.Append(mainIl.Create(OpCodes.Add));
                    mainIl.Append(mainIl.Create(OpCodes.Stloc_1));
                    break;

                case '<':
                    // ptr--;
                    mainIl.Append(mainIl.Create(OpCodes.Ldloc_1));
                    mainIl.Append(mainIl.Create(OpCodes.Ldc_I4_1));
                    mainIl.Append(mainIl.Create(OpCodes.Sub));
                    mainIl.Append(mainIl.Create(OpCodes.Stloc_1));
                    break;

                case '+':
                    // cells[ptr]++;
                    mainIl.Append(mainIl.Create(OpCodes.Ldloc_0));
                    mainIl.Append(mainIl.Create(OpCodes.Ldloc_1));
                    mainIl.Append(mainIl.Create(OpCodes.Ldelema, ts.Byte));
                    mainIl.Append(mainIl.Create(OpCodes.Dup));
                    mainIl.Append(mainIl.Create(OpCodes.Ldind_U1));
                    mainIl.Append(mainIl.Create(OpCodes.Ldc_I4_1));
                    mainIl.Append(mainIl.Create(OpCodes.Add));
                    mainIl.Append(mainIl.Create(OpCodes.Conv_U1));
                    mainIl.Append(mainIl.Create(OpCodes.Stind_I1));
                    break;

                case '-':
                    // cells[ptr]--;
                    mainIl.Append(mainIl.Create(OpCodes.Ldloc_0));
                    mainIl.Append(mainIl.Create(OpCodes.Ldloc_1));
                    mainIl.Append(mainIl.Create(OpCodes.Ldelema, ts.Byte));
                    mainIl.Append(mainIl.Create(OpCodes.Dup));
                    mainIl.Append(mainIl.Create(OpCodes.Ldind_U1));
                    mainIl.Append(mainIl.Create(OpCodes.Ldc_I4_1));
                    mainIl.Append(mainIl.Create(OpCodes.Sub));
                    mainIl.Append(mainIl.Create(OpCodes.Conv_U1));
                    mainIl.Append(mainIl.Create(OpCodes.Stind_I1));
                    break;

                case '.':
                    // Console.Write((char) cells[ptr]);
                    mainIl.Append(mainIl.Create(OpCodes.Ldloc_0));
                    mainIl.Append(mainIl.Create(OpCodes.Ldloc_1));
                    mainIl.Append(mainIl.Create(OpCodes.Ldelem_U1));
                    mainIl.Append(mainIl.Create(OpCodes.Call, write));
                    break;

                case ',':
                    // cells[ptr] = Console.ReadKey().KeyChar;
                    mainIl.Append(mainIl.Create(OpCodes.Ldloc_0));
                    mainIl.Append(mainIl.Create(OpCodes.Ldloc_1));
                    mainIl.Append(mainIl.Create(interceptKey));
                    mainIl.Append(mainIl.Create(OpCodes.Call, readKey));
                    mainIl.Append(mainIl.Create(OpCodes.Stloc_2));
                    mainIl.Append(mainIl.Create(OpCodes.Ldloca_S, (byte) 2));
                    mainIl.Append(mainIl.Create(OpCodes.Call, keyChar));
                    mainIl.Append(mainIl.Create(OpCodes.Conv_U1));
                    mainIl.Append(mainIl.Create(OpCodes.Stelem_I1));
                    break;

                case '[': {
                    var loopHead = mainIl.Create(OpCodes.Nop);
                    var loopBody = mainIl.Create(OpCodes.Nop);
                    loopHeads.Push(loopHead);
                    loopBodies.Push(loopBody);

                    // goto loopHead; loopBody: ...;
                    mainIl.Append(mainIl.Create(OpCodes.Br, loopHead));
                    mainIl.Append(loopBody);
                    break;
                }

                case ']': {
                    if (loopHeads.Count == 0)
                        throw new Exception("Unmatched ']' encountered");

                    var loopHead = loopHeads.Pop();
                    var loopBody = loopBodies.Pop();

                    // loopHead: if (cells[ptr] != 0) goto loopBody;
                    mainIl.Append(loopHead);
                    mainIl.Append(mainIl.Create(OpCodes.Ldloc_0));
                    mainIl.Append(mainIl.Create(OpCodes.Ldloc_1));
                    mainIl.Append(mainIl.Create(OpCodes.Ldelem_U1));
                    mainIl.Append(mainIl.Create(OpCodes.Brtrue, loopBody));
                    break;
                }
            }
        }

        if (loopBodies.Count != 0 || loopHeads.Count != 0)
            throw new Exception("Unmatched '[' encountered");

        mainIl.Append(mainIl.Create(OpCodes.Ret));

        return main;
    }
}
