FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . ./
RUN dotnet restore ./Presentation/Presentation.csproj
RUN dotnet tool install --global dotnet-ef --version 8.0.7
RUN dotnet add Presentation package Microsoft.EntityFrameworkCore.Design --version 8.0.7
ENV PATH $PATH:/root/.dotnet/tools
RUN dotnet ef migrations add init --project Infrastructure
RUN dotnet publish Presentation -c Release -o /app

WORKDIR /app
RUN cp /src/init.sh ./
RUN chmod +x init.sh

ENTRYPOINT ["./init.sh"]
