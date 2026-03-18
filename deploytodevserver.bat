dotnet publish -c Release -r linux-x64 --self-contained

del .\bin\Release\net9.0\linux-x64.zip
tar -a -cf ./bin/Release/net9.0/linux-x64.zip -C ./bin/Release/net9.0/linux-x64/ .
ssh -i .\.dnu\DevSQLServer.pem ubuntu@dev.api.fishnation.xyz "pkill screen"
scp -i .\.dnu\DevSQLServer.pem .\bin\Release\net9.0\linux-x64.zip ubuntu@dev.api.fishnation.xyz:/home/ubuntu/
scp -i .\.dnu\DevSQLServer.pem .\DeploymentResourcesDev\* ubuntu@dev.api.fishnation.xyz:/home/ubuntu/
ssh -i .\.dnu\DevSQLServer.pem ubuntu@dev.api.fishnation.xyz "tr -d '\r' < ./unpack.sh > ./unpack.sh.lf && mv ./unpack.sh.lf ./unpack.sh"
ssh -i .\.dnu\DevSQLServer.pem ubuntu@dev.api.fishnation.xyz "sudo chmod +x ./unpack.sh"
ssh -i .\.dnu\DevSQLServer.pem ubuntu@dev.api.fishnation.xyz "sudo ./unpack.sh"
ssh -i .\.dnu\DevSQLServer.pem ubuntu@dev.api.fishnation.xyz "screen -d -m sudo ./linux-x64/FishyAPI"
