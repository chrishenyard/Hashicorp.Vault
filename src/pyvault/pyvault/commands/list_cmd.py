from ..output import format_table

def register(subparsers):
    parser = subparsers.add_parser("list", help="List all secrets")
    parser.add_argument("--mount", default="kv-v2", required=False, help="KV v2 mount path")
    parser.add_argument("--path", default="demo-app/config", help="Optional folder path")
    parser.set_defaults(func=run)


def run(args, client):
    secrets = client.list_secrets(args.mount, args.path)
    headers = ["Secret Path", "Value"]
    rows = [[f"{args.mount}/{args.path}/{key}".strip("/"), value] for key, value in secrets.items()]
    print(format_table(headers, rows))

