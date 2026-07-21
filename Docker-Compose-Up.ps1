docker compose build

docker run -it --rm `
  -e DOTNET_ENVIRONMENT=Development `
  -e ASPNETCORE_ENVIRONMENT=Development `
  -e HashiCorpVaultOptions__Address=https://host.docker.internal `
  -e HashiCorpVaultOptions__HostHeader=vault.localhost `
  -v C:\Users\chenyard\AppData\Roaming\Microsoft\UserSecrets:/home/app/.microsoft/usersecrets:ro `
  --user "1654:1654" `
  hashicorp.vault:latest
  