## Requirements
kubectl
helm
docker desktop
openssl

## Create Docker Kubernetes Cluster
Create Kubernetes cluster in docker

## Add HashiCorp Helm Repo
- k create namespace vault
- k create namespace vault-secrets-operator
- k create namespace demo-app
- helm repo add hashicorp https://helm.releases.hashicorp.com
- helm search repo hashicorp/vault --versions

## Install Gateway CRDs
- kubectl apply -f https://github.com/kubernetes-sigs/gateway-api/releases/download/v1.5.1/standard-install.yaml

## Install HashiCorp Vault and Secrets Operator
- helm install --version 0.32.0 vault hashicorp/vault -n vault --wait
- helm install --version 1.4.0 vault-secrets-operator hashicorp/vault-secrets-operator -n vault-secrets-operator --wait

## Init Vault
- kubectl exec -ti vault-0 -n vault -- sh
- vault operator init

Save keys and root token

## Initialize Vault With Key Threshold
- kubectl exec -ti vault-0 -n vault -- vault operator unseal # ... Unseal Key 1
- kubectl exec -ti vault-0 -n vault -- vault operator unseal # ... Unseal Key 2
- kubectl exec -ti vault-0 -n vault -- vault operator unseal # ... Unseal Key 3

## Vault Login and Enable Secrets and Kubernetes Auth
- kubectl exec -ti vault-0 -n vault -- vault login # ... use the root token listed in the unseal output
- vault secrets enable kv-v2
- vault auth enable kubernetes
- vault write auth/kubernetes/config \
  kubernetes_host="https://$KUBERNETES_PORT_443_TCP_ADDR:443" \
  kubernetes_ca_cert=@/var/run/secrets/kubernetes.io/serviceaccount/ca.crt \
  disable_iss_validation=true

## Add Secret
- vault auth enable approle
- vault kv put kv-v2/demo-app/config \
  SqlConnectionString="Server=.;Database=Demo;Trusted_Connection=True;" \
  ApiKey="super-secret-value"

## Get Secret
- vault kv get kv-v2/demo-app/config

## Add App Policy
- vault policy write demo-app-policy - <<EOF
path "kv-v2/*" {
capabilities = ["create", "read", "update", "list"]
}
EOF

## Add App Role
- vault write auth/approle/role/demo-app-role \
  token_policies=default,demo-app-policy \
  token_ttl=30d \
  token_max_ttl=30d

## Read App Role
- vault read auth/approle/role/demo-app-role

## Add Kubernetes Role
- vault write auth/kubernetes/role/demo-app-role \
  bound_service_account_names=default \
  bound_service_account_namespaces=demo-app \
  policies=default,demo-app-policy \
  audience=vault \
  ttl=30d \
  max_ttl=30d

## Read Kubernetes Policy
- vault read auth/kubernetes/role/demo-app-role

## Get App Role ID and Secret ID
- vault read auth/approle/role/demo-app-role/role-id
- vault write -f auth/approle/role/demo-app-role/secret-id

## Execute write operation to get auth token
- vault write auth/approle/login role_id="" secret_id=""

## Setup Traefik
- kubectl create namespace traefik
- helm repo add traefik https://traefik.github.io/charts
- helm repo update

## Generate a Self‑Signed Certificate Valid for *.docker.localhost
- openssl req -x509 -nodes -days 365 -newkey rsa:2048 -keyout tls.key -out tls.crt -subj "/CN=*.docker.localhost"
- kubectl create secret tls local-selfsigned-tls --cert=tls.crt --key=tls.key --namespace traefik
- kubectl create secret tls local-selfsigned-tls --cert=tls.crt --key=tls.key --namespace vault

## Install Traefik
- helm install traefik traefik/traefik --namespace traefik --values traefik-values.yml --wait

## Apply Manifests
- kubectl apply -f ./vault-httproute.yml
- kubectl apply -f vault-connection.yml
- kubectl apply -f vault-auth.yml
- kubectl apply -f kv-secret.yml
- kubectl apply -f deployment.yml
