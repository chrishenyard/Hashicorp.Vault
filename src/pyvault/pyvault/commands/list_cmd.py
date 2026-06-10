def register(subparsers):
    parser = subparsers.add_parser("list", help="List all secrets")
    parser.add_argument("--mount", required=True, help="KV v2 mount path")
    parser.add_argument("--path", default="", help="Optional folder path")
    parser.set_defaults(func=run)


def run(args, client):
    walk(client, args.mount, args.path)


def walk(client, mount: str, path: str):
    keys = client.list_secrets(mount, path)

    for key in keys:
        full_path = f"{path}/{key}".strip("/")

        if key.endswith("/"):
            walk(client, mount, full_path)
        else:
            print(full_path)

