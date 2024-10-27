#!/bin/bash
# This script is used to start the application
# Make db migrations
cd /src
dotnet ef database update --project Infrastructure
cd /app
dotnet Presentation.dll