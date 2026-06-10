def format_table(headers: list[str], rows: list[list[object]]) -> str:
    """Render rows as a simple ASCII table."""
    normalized_rows = [["" if cell is None else str(cell) for cell in row] for row in rows]
    column_count = len(headers)

    if any(len(row) != column_count for row in normalized_rows):
        raise ValueError("All rows must have the same number of columns as headers.")

    widths = [len(str(header)) for header in headers]

    for row in normalized_rows:
        for index, cell in enumerate(row):
            widths[index] = max(widths[index], len(cell))

    def border() -> str:
        return "+-" + "-+-".join("-" * width for width in widths) + "-+"

    def format_row(cells: list[str]) -> str:
        padded = [cell.ljust(widths[index]) for index, cell in enumerate(cells)]
        return "| " + " | ".join(padded) + " |"

    lines = [
        border(),
        format_row([str(header) for header in headers]),
        border(),
    ]

    for row in normalized_rows:
        lines.append(format_row(row))

    lines.append(border())
    return "\n".join(lines)