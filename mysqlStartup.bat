@echo off
docker pull mysql
docker volume create mysql-vol
cls
set str="%cd%\Scripts:/docker-entrypoint-initdb.d"
docker run -itd --rm --name mysql -p 3306:3306 -v %str% --mount source=mysql-vol,target=/var/lib/mysql -e MYSQL_ROOT_PASSWORD=db_user_pass mysql:latest