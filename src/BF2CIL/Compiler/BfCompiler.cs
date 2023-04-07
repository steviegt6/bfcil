using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace BF2CIL.Compiler;

public static class BfCompiler {
    public static byte[] Compile(string source, BfCompilerOptions options) {
        return Compile(source.ToCharArray(), options);
    }

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

        var write = module.ImportReference(
            typeof(Console).GetMethod(
                "Write",
                new[] {
                    typeof(char),
                }
            )
        );
        var readKey = module.ImportReference(
            typeof(Console).GetMethod("ReadKey", Type.EmptyTypes)
        );
        var keyChar = module.ImportReference(
            typeof(ConsoleKeyInfo).GetProperty("KeyChar")!.GetMethod
        );

        var main = new MethodDefinition(
            "'<Main>$'",
            MethodAttributes.Private
          | MethodAttributes.HideBySig
          | MethodAttributes.Static,
            ts.Void
        );
        var mainIl = main.Body.GetILProcessor();
        var mainLocals = main.Body.Variables;

        var loopStarts = new Stack<Instruction>();
        var loopEnds = new Stack<Instruction>();

        // TODO: We can optimize by omitting this var entirely when allowed.
        var cells = new VariableDefinition(ts.Int32.MakeArrayType());
        mainLocals.Add(cells);

        var ptr = new VariableDefinition(ts.Int32);
        mainLocals.Add(ptr);

        // TODO: Configurable data type?
        // int[] cells = new int[options.CellCount];
        mainIl.Append(mainIl.Create(OpCodes.Ldc_I4, options.CellCount));
        mainIl.Append(mainIl.Create(OpCodes.Newarr, ts.Int32));
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
                    mainIl.Append(mainIl.Create(OpCodes.Ldelema, ts.Int32));
                    mainIl.Append(mainIl.Create(OpCodes.Dup));
                    mainIl.Append(mainIl.Create(OpCodes.Ldind_I4));
                    mainIl.Append(mainIl.Create(OpCodes.Ldc_I4_1));
                    mainIl.Append(mainIl.Create(OpCodes.Add));
                    mainIl.Append(mainIl.Create(OpCodes.Stind_I4));
                    break;

                case '-':
                    // cells[ptr]--;
                    mainIl.Append(mainIl.Create(OpCodes.Ldloc_0));
                    mainIl.Append(mainIl.Create(OpCodes.Ldloc_1));
                    mainIl.Append(mainIl.Create(OpCodes.Ldelema, ts.Int32));
                    mainIl.Append(mainIl.Create(OpCodes.Dup));
                    mainIl.Append(mainIl.Create(OpCodes.Ldind_I4));
                    mainIl.Append(mainIl.Create(OpCodes.Ldc_I4_1));
                    mainIl.Append(mainIl.Create(OpCodes.Sub));
                    mainIl.Append(mainIl.Create(OpCodes.Stind_I4));
                    break;

                case '.':
                    // Console.Write((char) cells[ptr]);
                    mainIl.Append(mainIl.Create(OpCodes.Ldloc_0));
                    mainIl.Append(mainIl.Create(OpCodes.Ldloc_1));
                    mainIl.Append(mainIl.Create(OpCodes.Ldelem_I4));
                    mainIl.Append(mainIl.Create(OpCodes.Conv_U2));
                    mainIl.Append(mainIl.Create(OpCodes.Call, write));
                    break;

                case ',':
                    // cells[ptr] = Console.ReadKey().KeyChar;
                    mainIl.Append(mainIl.Create(OpCodes.Ldloc_0));
                    mainIl.Append(mainIl.Create(OpCodes.Ldloc_1));
                    mainIl.Append(mainIl.Create(OpCodes.Call, readKey));
                    mainIl.Append(mainIl.Create(OpCodes.Stloc_2));
                    mainIl.Append(mainIl.Create(OpCodes.Ldloca_S, (byte) 2));
                    mainIl.Append(mainIl.Create(OpCodes.Call, keyChar));
                    mainIl.Append(mainIl.Create(OpCodes.Stelem_I4));
                    break;

                case '[': {
                    var loopStart = mainIl.Create(OpCodes.Nop);
                    var loopEnd = mainIl.Create(OpCodes.Nop);
                    loopStarts.Push(loopStart);
                    loopEnds.Push(loopEnd);

                    // if (cells[ptr] == 0) goto loopEnd
                    mainIl.Append(mainIl.Create(OpCodes.Ldloc_0));
                    mainIl.Append(mainIl.Create(OpCodes.Ldloc_1));
                    mainIl.Append(mainIl.Create(OpCodes.Ldelem_I4));
                    mainIl.Append(mainIl.Create(OpCodes.Brfalse, loopEnd));
                    mainIl.Append(loopStart);
                    break;
                }

                case ']': {
                    if (loopEnds.Count == 0)
                        throw new Exception("Unmatched ']' encountered");

                    var loopStart = loopStarts.Pop();
                    var loopEnd = loopEnds.Pop();

                    // if (cells[ptr] != 0) goto loopStart
                    mainIl.Append(mainIl.Create(OpCodes.Ldloc_0));
                    mainIl.Append(mainIl.Create(OpCodes.Ldloc_1));
                    mainIl.Append(mainIl.Create(OpCodes.Ldelem_I4));
                    mainIl.Append(mainIl.Create(OpCodes.Brtrue, loopStart));
                    mainIl.Append(loopEnd);
                    break;
                }
            }
        }

        mainIl.Append(mainIl.Create(OpCodes.Ret));

        program.Methods.Add(main);
        module.EntryPoint = main;

        using var ms = new MemoryStream();
        module.Write(ms);
        return ms.ToArray();
    }
}
