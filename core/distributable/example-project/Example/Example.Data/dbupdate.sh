dotnet ef migrations add $2
dotnet ef database update --connection $1 

# Step 1: Start Docker
# docker run --name example -p 6002:5432 -e POSTGRES_PASSWORD=pgpass -e POSTGRES_USER=pgadmin -d postgres

# Step 2.a: Make Bash file executable
# chmod +x dbupdate.sh

# Step 2.b: Migrate and update 
# ./dbupdate.sh "Host=localhost:6002;Database=example;Username=pgadmin;Password=pgpass;ApplicationName=example" migration

# Dev, Update
# dotnet ef database update --connection 'Host=localhost:6002;Database=example;Username=pgadmin;Password=pgpass;ApplicationName=example'


# Production
# docker run --name example -p 6002:5432 -e POSTGRES_PASSWORD=RZdP_XV9E -e POSTGRES_USER=pgadmin -d --restart always --network my_network postgres
# dotnet ef database update --connection 'Host=104.248.6.25:6002;Database=example;Username=pgadmin;Password=RZdP_XV9E;ApplicationName=example'