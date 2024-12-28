dotnet publish -c Release -r linux-x64 --self-contained

del .\bin\Release\net9.0\linux-x64.zip
tar -a -cf ./bin/Release/net9.0/linux-x64.zip -C ./bin/Release/net9.0/linux-x64/ .
ssh -i .\.dnu\sshKey.pem ubuntu@api.fishnation.xyz "pkill screen"
scp -i .\.dnu\sshKey.pem .\bin\Release\net9.0\linux-x64.zip ubuntu@api.fishnation.xyz:/home/ubuntu/
ssh -i .\.dnu\sshKey.pem ubuntu@api.fishnation.xyz "sudo ./unpack.sh"
ssh -i .\.dnu\sshKey.pem ubuntu@api.fishnation.xyz "screen -d -m sudo ./linux-x64/FishyAPI"
