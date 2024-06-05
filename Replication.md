# Commands for replication
This page lists the commands used for replication. The data 

Assuming you use a clean Ubuntu Server 24.04 and put data to `/data`, with Docker and .NET SDK 8.0 installed. Also, if you run the replication on a remote machine, forward 18080 port to access the web service.

## Start database server

```
docker network create my_net
```

```
docker run -d \
--name forecast-mongo \
--net my_net \
--net-alias forecast-mongo \
-p 27017:27017 \
-v /data/mongodb:/data/db \
mongo:5 --wiredTigerCacheSizeGB=2
```

```
docker run -d \
--name forecast-pg \
--net my_net \
--net-alias forecast-pg \
--user 1000:1000 \
-p 13339:13339 \
-v "/data/postgresql/data:/var/lib/postgresql/data" \
postgres:13 \
-c port=13339
```

## Build and run web application

```
dotnet publish ./appendix/replication/src/BuildAcceleration/BuildAcceleration.WebApp/BuildAcceleration.WebApp.csproj -c Release -o bin/web
```

```
docker run -d --name forecast \
-v /data/bin/web:/app \
-w /app \
-p 18080:80 \
--restart always \
--net my_net \
--net-alias forecast \
mcr.microsoft.com/dotnet/aspnet:7.0 \
dotnet /app/BuildAcceleration.WebApp.dll
```

## Replication of Figures

### Figure 2
```
dotnet run --project appendix/replication/src/BuildAcceleration/BuildAcceleration/BuildAcceleration.csproj -- long-tail --path .
```

<!-- docker run -it --rm \
-v /data:/replication \
-w /replication \
--net my_net \
mcr.microsoft.com/dotnet/sdk:7.0 \
dotnet run --project /replication/appendix/replication/src/BuildAcceleration/BuildAcceleration/BuildAcceleration.csproj -- long-tail --path /replication -->

Then, manually process it in Excel.

### Figure 3

http://localhost:18080/TimeSeries/ByNothing?repo=spotify/scio&job=build_scalafix_rules_212

### Figure 5

Head to line 270--284 in the file `replication/src/BuildAcceleration/BuildAcceleration/AccelerationSampling/KMeansCluster.cs`, change the .dll/.so/.dylib path.

```
sudo apt install python3-pip
pip3 install numpy
pip3 install scikit-learn
```

```
dotnet run --project appendix/replication/src/BuildAcceleration/BuildAcceleration/BuildAcceleration.csproj -- cluster
```

Uncomment `appendix/replication/src/BuildAcceleration/BuildAcceleration/Program.cs` Line 26 and comment Line 27, then we can produce recall and precision by the following command:

```
cp -a appendix/replication/rq1_inspection_agreed.csv build_accel/inspection_agreed.csv
dotnet run --project appendix/replication/src/BuildAcceleration/BuildAcceleration/BuildAcceleration.csproj
```

To produce the clustering-only evaluation, remove `hitManualRules ? 0 :` on line 205--208 in the file `appendix/replication/src/BuildAcceleration/BuildAcceleration/AccelerationSampling/KMeansCluster.cs` and re-run the `dotnet` commands.

Do not forget to revert changes to `Program.cs`.

### Figure 6
```
dotnet run --project appendix/replication/src/BuildAcceleration/BuildAcceleration/BuildAcceleration.csproj -- detect --all
dotnet run --project appendix/replication/src/BuildAcceleration/BuildAcceleration/BuildAcceleration.csproj -- cluster --all
```

This produce `k-means-all.csv` and `ruled_detection.csv`. Manually process them using Excel. Then sort and generate the figure.

Then manually process them using Excel.
