## Requirements
kubectl
helm
docker desktop
openssl

## Create Docker Kubernetes Cluster
Create Kubernetes cluster in docker

## Add HashiCorp Helm Repo
- k create namespace vault
- helm repo add hashicorp https://helm.releases.hashicorp.com

## Install Gateway CRDs
- kubectl apply -f https://github.com/kubernetes-sigs/gateway-api/releases/download/v1.5.1/standard-install.yaml

## Install HashiCorp Vault
- helm install vault hashicorp/vault -n vault --wait

## Init Vault
- kubectl exec -ti vault-0 -n vault -- sh
- vault operator init

Save keys and root token

## Initialize Vault With Key Threshold
- kubectl exec -ti vault-0 -n vault -- vault operator unseal # ... Unseal Key 1
- kubectl exec -ti vault-0 -n vault -- vault operator unseal # ... Unseal Key 2
- kubectl exec -ti vault-0 -n vault -- vault operator unseal # ... Unseal Key 3

## Vault Login
- kubectl exec -ti vault-0 -n vault -- vault login # ... use the root token listed in the unseal output

## Setup Traefik
- kubectl create namespace traefik
- helm repo add traefik https://traefik.github.io/charts
- helm repo update

## 1) Generate a Self‑Signed Certificate Valid for *.docker.localhost
- openssl req -x509 -nodes -days 365 -newkey rsa:2048 -keyout tls.key -out tls.crt -subj "/CN=*.docker.localhost"
- kubectl create secret tls local-selfsigned-tls --cert=tls.crt --key=tls.key --namespace traefik
- kubectl create secret tls local-selfsigned-tls --cert=tls.crt --key=tls.key --namespace vault

## Install Traefik
- helm install traefik traefik/traefik --namespace traefik --values traefik-values.yml --wait

## Define Vault Traefik Gateway
- kubectl apply -f ./traefik-gateway-vault.yml

## Define Vault HttpRoute
- kubectl apply -f ./vault-httproute.yml

## Enable KV-V2 Secrets Engin
- vault secrets enable kv-v2

## Add Secret
- vault auth enable approle
- vault kv put kv-v2/demo-app \
  SqlConnectionString="Server=.;Database=Demo;Trusted_Connection=True;" \
  ApiKey="super-secret-value"

## Get Secret
- vault kv get kv-v2/demo-app

## Add Policy
- vault policy write demo-app-policy - <<EOF
path "kv-v2/*" {
capabilities = ["create", "read", "update", "list"]
}
EOF

## Add Role
- vault write auth/approle/role/demo-app \
  token_policies="demo-app-policy" \
  token_ttl=24h \
  token_max_ttl=24h

## Read Policy
- vault read auth/approle/role/demo-app

## Get Role ID and Secret ID
vault read auth/approle/role/demo-app/role-id
vault write -f auth/approle/role/demo-app/secret-id

role_id:
secret_id:

vault write auth/approle/login role_id="" secret_id=""
