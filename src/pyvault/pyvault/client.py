import json
import ssl
import urllib.request
import urllib.error


class VaultApiError(RuntimeError):
    def __init__(self, status_code: int, message: str, details: object | None = None):
        super().__init__(message)
        self.status_code = status_code
        self.details = details


class VaultClient:
    def __init__(self, address: str, token: str):
        self.address = address.rstrip("/")
        self.token = token

    def request(self, method: str, path: str, payload: dict | None = None) -> dict:
        url = f"{self.address}/v1/{path.lstrip('/')}"
        data = None

        headers = {
            "X-Vault-Token": self.token,
            "Content-Type": "application/json",
        }

        if payload is not None:
            data = json.dumps(payload).encode("utf-8")

        req = urllib.request.Request(
            url=url,
            data=data,
            headers=headers,
            unverifiable=True,  # Disable SSL verification for simplicity; not recommended for production
            method=method,
        )

        context = ssl._create_unverified_context()

        try:
            with urllib.request.urlopen(req, context=context) as response:
                body = response.read().decode("utf-8")
                return json.loads(body) if body else {}
        except urllib.error.HTTPError as ex:
            body = ex.read().decode("utf-8")
            message = f"Vault API error {ex.code}"
            details: object | None = None

            try:
                details = json.loads(body) if body else None

                if isinstance(details, dict):
                    errors = details.get("errors")
                    if isinstance(errors, list) and errors:
                        error_text = "; ".join(str(item) for item in errors)
                        message = f"{message}: {error_text}"
            except json.JSONDecodeError:
                if body.strip():
                    message = f"{message}: {body.strip()}"

            raise VaultApiError(ex.code, message, details) from ex

    def list_secrets(self, mount: str, secret_path: str = "") -> dict[str, object]:
        api_path = f"{mount}/data/{secret_path}".rstrip("/")
        result = self.request("GET", api_path)
        return result.get("data", {}).get("data", {})

    def read_secret(self, mount: str, secret_path: str) -> dict:
        result = self.request("GET", f"{mount}/data/{secret_path}")
        return result.get("data", {}).get("data", {})

    def write_secret(self, mount: str, secret_path: str, values: dict) -> None:
        self.request("POST", f"{mount}/data/{secret_path}", {"data": values})

    def delete_secret(self, mount: str, secret_path: str) -> None:
        self.request("DELETE", f"{mount}/data/{secret_path}")

