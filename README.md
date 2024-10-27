# WhisperVote

WhisperVote is a voting application built with C# and ASP.NET Core. It uses MariaDB as the database and Docker for containerization.
This project was created as part of the course "IT-Projekt" at the Ostfalia University of Applied Sciences in Suderburg.

## Prerequisites

- [.NET 6 SDK](https://dotnet.microsoft.com/download/dotnet/6.0)
- [Docker](https://www.docker.com/get-started)

## Getting Started

### Clone the repository

```sh
git clone https://github.com/Drizein/WhisperVote.git
cd WhisperVote
```

### Set the environment variables

modify the `docker-compose.yml` file to set the environment variables for the MariaDB container and the .NET applicaation.

```sh
      - 'MARIADB_DATABASE=WhisperVoteVote'
      - 'MARIADB_PASSWORD=SuperSicheresPasswort123!'
      - 'MARIADB_ROOT_PASSWORD=SuperSicheresPasswort123!'
      - 'MARIADB_USER=WhisperVote'
      - 'ConnectionStrings__AuthServer=http://auth:9912'
      - 'ASPNETCORE_HTTP_PORTS=9913'
```

### Run the Docker container

```sh
docker compose up -d
```

### Access the application

Open your browser and navigate to `http://localhost:9913`.

