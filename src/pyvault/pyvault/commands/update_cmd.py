from .add_cmd import parse_key_values


def register(subparsers):
    parser = subparsers.add_parser("update", help="Update an existing secret")
    parser.add_argument("--mount", default="kv-v2", required=False, help="KV v2 mount path")
    parser.add_argument("--path", default="demo-app/config", required=False, help="Secret path (e.g. 'app/config')")
    parser.add_argument("--data", action="append", required=True, help="key=value")
    parser.set_defaults(func=run)


def run(args, client):
    existing = client.read_secret(args.mount, args.path)
    changes = parse_key_values(args.data)

    existing.update(changes)

    client.write_secret(args.mount, args.path, existing)
    print(f"Updated secret: {args.mount}/{args.path}")

