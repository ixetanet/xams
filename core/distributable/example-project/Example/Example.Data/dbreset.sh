rm -r Migrations

docker stop example
docker rm example
docker run --name example -p 6001:5432 -e POSTGRES_PASSWORD=pgpass -e POSTGRES_USER=pgadmin -d postgres

export ASPNETCORE_ENVIRONMENT=Local
dotnet ef migrations add migration01
dotnet ef database update -- --environment Local