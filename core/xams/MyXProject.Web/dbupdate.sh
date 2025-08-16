# dotnet tool install --global dotnet-ef
# dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet ef migrations add $2
dotnet ef database update --connection $1 

# Step 1: Start Docker
# docker run --name myxproject -p 6001:5432 -e POSTGRES_PASSWORD=pgpass -e POSTGRES_USER=pgadmin -d postgres

# Step 2.a: Make Bash file executable
# chmod +x dbupdate.sh

# Step 2.b: Migrate and update 
# ./dbupdate.sh "Host=localhost:6001;Database=myxproject;Username=pgadmin;Password=pgpass;ApplicationName=myxproject" migration

# Dev, Update
# dotnet ef database update --connection 'Host=localhost:6001;Database=myxproject;Username=postgres;Password=pgpass;ApplicationName=myxproject'