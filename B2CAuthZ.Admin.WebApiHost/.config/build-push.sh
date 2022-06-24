docker build ../Dockerfile -t adminapi:latest
ACR_LOGIN=$(az acr login -n b2xve --expose-token | jq)
docker login $(echo $ACR_LOGIN | jq -r .loginServer) -u 00000000-0000-0000-0000-000000000000 -p $(echo $ACR_LOGIN | jq -r .accessToken)
docker tag adminapi:latest b2xve.azurecr.io/b2x/admin/api
docker push b2xve.azurecr.io/b2x/admin/api