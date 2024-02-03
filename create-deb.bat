del /F /S /Q publish\linux-arm
dotnet publish Src\ModbusMqttPublisher.sln -c Release --sc false -r linux-arm -p:PublishSingleFile=true -o publish\linux-arm -p:OptimizationPreference=Speed
dotnet tool restore
dotnet make-deb Setup\app.debspec
pause
