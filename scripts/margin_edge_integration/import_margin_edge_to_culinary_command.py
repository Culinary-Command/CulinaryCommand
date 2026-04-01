"""
import_margin_edge_to_culinary_command.py

Reads an Excel file produced by get_margin_edge_order_details.py and imports
the vendorItemName entries as Ingredients into the CulinaryCommand RDS MySQL instance
for the matching location.

Usage:
    python3 import_margin_edge_to_culinary_command.py --input-file <path>.xlsx [--dry-run]

Environment variables (set in .env):
    DB_HOST      - RDS hostname
    DB_PORT      - RDS port (default: 3306)
    DB_NAME      - Database name (default: CulinaryCommandDB)
    DB_USER      - Database username
    DB_PASSWORD  - Database password
"""
# AI-ASSISTED

import argparse
import os
import sys
from datetime import datetime, timezone
from typing import Optional

import openpyxl
import pymysql
from dotenv import load_dotenv

load_dotenv()

PRAIRIE_CANARY_LOCATION_NAME = "Prairie Canary"


# ---------------------------------------------------------------------------
# Database helpers
# ---------------------------------------------------------------------------

def get_db_connection() -> pymysql.Connection:
    return pymysql.connect(
        host=os.environ["DB_HOST"],
        port=int(os.getenv("DB_PORT", "3306")),
        database=os.environ["DB_NAME"],
        user=os.environ["DB_USER"],
        password=os.environ["DB_PASSWORD"],
        charset="utf8mb4",
        cursorclass=pymysql.cursors.DictCursor,
        autocommit=False,
    )


def fetch_location_id_by_name(conn: pymysql.Connection, location_name: str) -> Optional[int]:
    with conn.cursor() as cursor:
        cursor.execute(
            "SELECT id FROM Locations WHERE Name LIKE %s LIMIT 1",
            (f"%{location_name}%",),
        )
        row = cursor.fetchone()
    return row["id"] if row else None


def fetch_default_unit_id(conn: pymysql.Connection) -> Optional[int]:
    with conn.cursor() as cursor:
        cursor.execute("SELECT id FROM Units WHERE Abbreviation IN ('ea', 'each', 'unit') LIMIT 1")
        row = cursor.fetchone()
    return row["id"] if row else None


def fetch_vendor_id_by_name(conn: pymysql.Connection, vendor_name: str, location_id: int) -> Optional[int]:
    with conn.cursor() as cursor:
        cursor.execute(
            """SELECT v.id FROM Vendors v
               INNER JOIN LocationVendors lv ON lv.VendorId = v.id
               WHERE lv.LocationId = %s AND v.Name LIKE %s
               LIMIT 1""",
            (location_id, f"%{vendor_name}%"),
        )
        row = cursor.fetchone()
    return row["id"] if row else None


def upsert_ingredient(
    conn: pymysql.Connection,
    ingredient_name: str,
    location_id: int,
    vendor_id: Optional[int],
    default_unit_id: Optional[int],
    dry_run: bool,
) -> bool:
    """Insert the ingredient if it does not already exist. Returns True if inserted, False if skipped."""
    with conn.cursor() as cursor:
        cursor.execute(
            "SELECT IngredientId FROM Ingredients WHERE LocationId = %s AND Name = %s LIMIT 1",
            (location_id, ingredient_name),
        )
        if cursor.fetchone():
            print(f"  Skipping (already exists): {ingredient_name}")
            return False

        print(f"  {'[DRY RUN] ' if dry_run else ''}Inserting: {ingredient_name}")
        if not dry_run:
            now = datetime.now(timezone.utc)
            cursor.execute(
                """INSERT INTO Ingredients
                   (Name, LocationId, VendorId, UnitId, StockQuantity, ReorderLevel, Category, CreatedAt, UpdatedAt)
                   VALUES (%s, %s, %s, %s, 0, 0, '', %s, %s)""",
                (ingredient_name, location_id, vendor_id, default_unit_id, now, now),
            )
        return True


# ---------------------------------------------------------------------------
# Excel helpers
# ---------------------------------------------------------------------------

def read_line_items_from_excel(input_file: str) -> list[dict]:
    workbook = openpyxl.load_workbook(input_file)
    worksheet = workbook.active

    headers = [cell.value for cell in worksheet[1]]
    line_items = []
    for row in worksheet.iter_rows(min_row=2, values_only=True):
        row_data = dict(zip(headers, row))
        culinary_name = row_data.get("Culinary Command Ingredient", "")
        vendor_name = row_data.get("Vendor Name", "")
        if culinary_name and culinary_name != "(no line items)":
            line_items.append({
                "vendorItemName": str(culinary_name).strip(),
                "vendorName": str(vendor_name).strip() if vendor_name else "",
            })
    return line_items


# ---------------------------------------------------------------------------
# Entry point
# ---------------------------------------------------------------------------

def main() -> None:
    parser = argparse.ArgumentParser(description="Import Margin Edge line items as ingredients into CulinaryCommand")
    parser.add_argument("--input-file", type=str, required=True, help="Path to the order_line_items Excel file")
    parser.add_argument("--location-id", type=int, default=None, help="Location ID to import into (overrides name-based lookup)")
    parser.add_argument("--dry-run", action="store_true", help="Preview inserts without writing to the database")
    args = parser.parse_args()

    if not os.path.exists(args.input_file):
        print(f"ERROR: File not found: {args.input_file}", file=sys.stderr)
        sys.exit(1)

    print(f"Reading line items from {args.input_file}...")
    line_items = read_line_items_from_excel(args.input_file)

    if not line_items:
        print("No line items found in the Excel file.")
        sys.exit(0)

    # Deduplicate by vendorItemName — we only need one row per unique ingredient
    seen_ingredient_names: set[str] = set()
    unique_line_items = []
    for line_item in line_items:
        if line_item["vendorItemName"] not in seen_ingredient_names:
            seen_ingredient_names.add(line_item["vendorItemName"])
            unique_line_items.append(line_item)

    print(f"Found {len(line_items)} total row(s), {len(unique_line_items)} unique ingredient(s).\n")

    conn = get_db_connection()
    try:
        if args.location_id:
            location_id = args.location_id
            print(f"Using provided location id={location_id}.\n")
        else:
            location_id = fetch_location_id_by_name(conn, PRAIRIE_CANARY_LOCATION_NAME)
            if not location_id:
                print(f"ERROR: Could not find location '{PRAIRIE_CANARY_LOCATION_NAME}' in the database.", file=sys.stderr)
                sys.exit(1)
            print(f"Found location '{PRAIRIE_CANARY_LOCATION_NAME}' (id={location_id}).\n")

        default_unit_id = fetch_default_unit_id(conn)

        inserted_count = 0
        skipped_count = 0

        for line_item in unique_line_items:
            ingredient_name = line_item["vendorItemName"]
            vendor_name = line_item["vendorName"]
            vendor_id = fetch_vendor_id_by_name(conn, vendor_name, location_id) if vendor_name else None

            was_inserted = upsert_ingredient(conn, ingredient_name, location_id, vendor_id, default_unit_id, args.dry_run)
            if was_inserted:
                inserted_count += 1
            else:
                skipped_count += 1

        if not args.dry_run:
            conn.commit()

        print(f"\nDone. {inserted_count} inserted, {skipped_count} skipped (already existed).")
        if args.dry_run:
            print("Dry run — no changes were written to the database.")

    except Exception as error:
        conn.rollback()
        print(f"ERROR: {error}", file=sys.stderr)
        raise
    finally:
        conn.close()


if __name__ == "__main__":
    main()