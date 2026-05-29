docker compose build

docker run -it --rm `
  --add-host "vault.localhost:host-gateway" `
  -e DOTNET_ENVIRONMENT=Development `
  -e ASPNETCORE_ENVIRONMENT=Development `
  -v C:\Users\chenyard\AppData\Roaming\Microsoft\UserSecrets:/home/app/.microsoft/usersecrets:ro `
  --user "1654:1654" `
  hashicorp.vault:latest
  