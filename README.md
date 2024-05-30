# lojalBackend

## Repository requirements
Every single addition to the `master` branch must be done from another branch via pull request and every commit in the branches must be signed.
For signing instructions please refer to this [link](https://docs.github.com/en/authentication/managing-commit-signature-verification/generating-a-new-gpg-key).
Recommended Windows GPG program is GPG4win with Kleopatra key manager.

## Requirements
To launch this project you will need:
- Microsoft Visual Studio 2022
- .NET 8.0 SDK
- Docker

## Getting started
There are several ways to launch this project:
### Docker Compose
First launch Docker and then run `composeStartup.bat` to startup the database and backend service.
After that the backend will be available on [http://localhost:8080/swagger](http://localhost:8080/swagger) 
and mysql database on [http://localhost:3306](http://localhost:3306).

### IIS Express
First launch Docker and then run `mysqlStartup.bat` to startup the database.
It will create volume named mysql-vol. If any changes are made to the scripts in Scripts directory then this volume will have to be manually removed.
After that launch project with default IIS Express run configuration. It will pass environment variable "Development".

### Docker
First launch Docker and then run `mysqlStartup.bat` to startup the database.
It will create volume named mysql-vol. If any changes are made to the scripts in Scripts directory then this volume will have to be manually removed.
After that launch project from the Dockerfile with environment variable `"ASPNETCORE_ENVIRONMENT": "Compose"` and docker argument `--network=loj-backend-network --rm`.