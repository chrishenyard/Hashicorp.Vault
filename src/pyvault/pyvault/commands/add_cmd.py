def register(subparsers):
    parser = subparsers.add_parser("add", help="Add a new secret")
    parser.add_argument("--mount", required=True)
    parser.add_argument("--path", required=True)
    parser.add_argument("--data", action="append", required=True, help="key=value")
    parser.set_defaults(func=run)


def run(args, client):
    values = parse_key_values(args.data)
    client.write_secret(args.mount, args.path, values)
    print(f"Added secret: {args.mount}/{args.path}")


def parse_key_values(items: list[str]) -> dict:
    values = {}

    for item in items:
        if "=" not in item:
            raise ValueError(f"Invalid --data value: {item}. Expected key=value.")

        key, value = item.split("=", 1)
        values[key] = value

    return values

