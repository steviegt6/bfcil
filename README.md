# BF2CIL

**BF2CIL** is a [Brainfuck](https://en.wikipedia.org/wiki/Brainfuck) compiler written in C#, which can compile Brainfuck to .NET CIL.

## Building

```bash
git clone https://github.com/steviegt6/bfcil.git
cd ./bfcil/src/
dotnet build -c "Release"
```

## Usage

BF2CIL has an exposed public API as well as a private CLI infrastructure which can be accessed through `dotnet BF2CIL.dll` or by running the produced executable directly.

An example of the public API lies within the `Program.cs` file.

Command parameters and options are documented through the `--help` flag, a brief rundown is as follows:

### Parameters

|  Index  |  Name   |   Description   |
|---------|---------|-----------------|
|     `0` | `input` | Input file path |

### Options

|             Name            |   C  |                        Description                       |
|-----------------------------|------|----------------------------------------------------------|
| `--output`                  | `-o` | Output file path                                         |
| `--name`                    | `-n` | Assembly name                                            |
| `--version`                 | `-v` | Assembly version                                         |
| `--cell-count`              | `-c` | Cell count                                               |
| `--overwrite`               | `-w` | Overwrite output files                                   |
| `--generate-runtime-config` | `-r` | Generate runtime config                                  |
| `--intercept-input`         | `-i` | Intercept input (don't display user input when prompted) |
