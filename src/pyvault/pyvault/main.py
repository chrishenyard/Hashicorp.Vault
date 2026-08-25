import argparse
import json
import os
import sys

from .client import VaultApiError, VaultClient
from .commands import add_cmd, delete_cmd, env_cmd, list_cmd, update_cmd


def main():
    parser = argparse.ArgumentParser(prog="pyvault")

    parser.add_argument(
        "--addr",
        default=os.getenv("VAULT_ADDR", "http://vault.localhost"),
        help="Vault server address. Defaults to VAULT_ADDR.",
    )

    parser.add_argument(
        "--token",
        default=os.getenv("VAULT_TOKEN"),
        help="Vault token. Defaults to VAULT_TOKEN.",
    )

    subparsers = parser.add_subparsers(dest="command", required=True)

    list_cmd.register(subparsers)
    add_cmd.register(subparsers)
    update_cmd.register(subparsers)
    delete_cmd.register(subparsers)
    env_cmd.register(subparsers)

    args = parser.parse_args()

    if not args.token:
        print("Missing token. Pass --token or set VAULT_TOKEN.", file=sys.stderr)
        sys.exit(1)

    client = VaultClient(args.addr, args.token)

    try:
        args.func(args, client)
    except VaultApiError as ex:
        print(str(ex), file=sys.stderr)
        if ex.details is not None:
            print(json.dumps(ex.details, indent=2), file=sys.stderr)
        raise SystemExit(1) from ex

if __name__ == "__main__":
    main()

