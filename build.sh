#!/bin/bash
# building script bc building is exhausting

dotnet build LCEAuth.csproj
mv ./bin/Debug/net10.0/LCEAuth.dll ../plugins/LCEAuth/LCEAuth.dll
cd ../
