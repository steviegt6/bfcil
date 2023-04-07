#!/bin/bash

cd ../src/BF2CIL/ || exit

dotnet build -c "Debug"

cd ../../samples/ || exit

samples=( "Cat.Esolang" "CellSize.Esolang" "HelloWorld.Complex.Esolang" "HelloWorld.Esolang" "HelloWorld.Minimized.Esolang" "HelloWorld.Wikipedia" "ROT13.Wikipedia" )

for sample in "${samples[@]}"
do
    dotnet "../src/BF2CIL/bin/Debug/net7.0/BF2CIL.dll" "$sample".bf -w
done

for sample in "${samples[@]}"
do
    dotnet "../src/BF2CIL/bin/Debug/net7.0/BF2CIL.dll" decompile "$sample".dll -w -o "$sample"-decompiled.bf
done
