def register(subparsers):
    parser = subparsers.add_parser("delete", help="Delete a secret")
    parser.add_argument("--mount", default="kv-v2", required=False, help="KV v2 mount path")
    parser.add_argument("--path", default="demo-app/config", required=False, help="Secret path (e.g. 'app/config')")
    parser.set_defaults(func=run)


def run(args, client):
    client.delete_secret(args.mount, args.path)
    print(f"Deleted secret: {args.mount}/{args.path}")