def register(subparsers):
    parser = subparsers.add_parser("env", help="Export all secrets")
    parser.add_argument("--mount", default="kv-v2", required=False, help="KV v2 mount path")
    parser.add_argument("--path", default="demo-app/config", help="Optional folder path")
    parser.set_defaults(func=run)


def run(args, client):
    secrets = client.list_secrets(args.mount, args.path)
    # Export secrets as environment variables in the format KEY=VALUE to the current shell
    for key, value in secrets.items():
        escaped_value = str(value).replace("'", "'\\''")
        print(f"export {key.upper()}='{escaped_value}'")

