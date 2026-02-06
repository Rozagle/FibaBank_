FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY . .


RUN dotnet restore "FibaPlus_Bank/FibaPlus_Bank.csproj"

RUN dotnet publish "FibaPlus_Bank/FibaPlus_Bank.csproj" -c Release -o /app/out

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out .

ENTRYPOINT ["dotnet", "FibaPlus_Bank.dll"]