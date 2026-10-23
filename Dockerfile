FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY publish/ .
USER app
ENTRYPOINT ["dotnet", "TechChallenger-fase1.dll"]
