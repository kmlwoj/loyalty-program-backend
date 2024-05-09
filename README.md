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
In order to run the project you will first need to launch MySQL database in Docker container.
To do that launch `mysqlStartup.bat` file in the main directory.
After that you will have to fill connection string in the `appsettings.json` for the desired docker location.
Example format for local Docker run with Web API launched in IIS Express:
```
"ConnectionStrings": {
    "MainConn": "server=localhost;database=LojClientDB;user=root;password=db_user_pass",
    "ShopConn": "server=localhost;database=LojShopDB;user=root;password=db_user_pass"
  },
```
Example format for local Docker run with Web API launched in Docker container:
```
"ConnectionStrings": {
    "MainConn": "server=mysql;database=LojClientDB;user=root;password=db_user_pass",
    "ShopConn": "server=mysql;database=LojShopDB;user=root;password=db_user_pass"
  },
```
With all of the configuration done the API should work by launching it in the Visual Studio by IIS Express or Docker container with Dockerfile definition.