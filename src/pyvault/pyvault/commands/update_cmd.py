from .add_cmd import parse_key_values


def register(subparsers):
    parser = subparsers.add_parser("update", help="Update an existing secret")
    parser.add_argument("--mount", required=True)
    parser.add_argument("--path", required=True)
    parser.add_argument("--data", action="append", required=True, help="key=value")
    parser.set_defaults(func=run)


def run(args, client):
    existing = client.read_secret(args.mount, args.path)
    changes = parse_key_values(args.data)

    existing.update(changes)

    client.write_secret(args.mount, args.path, existing)
    print(f"Updated secret: {args.mount}/{args.path}")

