import argparse
import os
import sys

from .client import VaultClient
from .commands import list_cmd, add_cmd, update_cmd


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

    args = parser.parse_args()

    if not args.token:
        print("Missing token. Pass --token or set VAULT_TOKEN.", file=sys.stderr)
        sys.exit(1)

    client = VaultClient(args.addr, args.token)

    try:
        args.func(args, client)
    except Exception as ex:
        print(ex, file=sys.stderr)
        sys.exit(1)


if __name__ == "__main__":
    main()

