dotnet ef dbcontext scaffold "Host=172.16.10.93;Port=5432;Database=mcs;Username=admin;Password=Admin123#;Timeout=20;CommandTimeout=60;Pooling=false;No Reset On Close=true;" Npgsql.EntityFrameworkCore.PostgreSQL --use-database-names --force -o Repository
pause