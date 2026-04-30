echo INCREASE VERSION IN Setup\app64.debspec
pause
del /F /S /Q publish\linux-arm64
dotnet publish Src\ModbusMqttPublisher.sln -c Release --sc false -r linux-arm64 -p:PublishSingleFile=true -o publish\linux-arm64 -p:OptimizationPreference=Speed
dotnet tool restore
dotnet make-deb Setup\app64.debspec
pause
