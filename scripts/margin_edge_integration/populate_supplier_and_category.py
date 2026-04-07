"""
populate_supplier_and_category.py

Reads a cleaned recipe-ingredients Excel file (e.g. hometown_heroes_recipe_ingredients_cleaned.xlsx
or prairie_canary_recipe_ingredients_cleaned.xlsx) and:

  1. Updates the Category field on matching Ingredients rows.
  2. Fetches a logo from logo.dev for each unique vendor that does not yet have a LogoUrl,
     then stores the result on the Vendors row.

Usage:
    python3 populate_supplier_and_category.py \\
        --input-file hometown_heroes_recipe_ingredients_cleaned.xlsx \\
        --location-name "Hometown Heroes" \\
        [--dry-run]

    python3 populate_supplier_and_category.py \\
        --input-file prairie_canary_recipe_ingredients_cleaned.xlsx \\
        --location-name "Prairie Canary" \\
        [--dry-run]

Environment variables (set in .env):
    DB_HOST               - RDS hostname
    DB_PORT               - RDS port (default: 3306)
    DB_NAME               - Database name (default: CulinaryCommandDB)
    DB_USER               - Database username
    DB_PASSWORD           - Database password
    LOGO_DEV_SECRET_KEY       - logo.dev secret key  (used to search for company logos)
    LOGO_DEV_PUBLISHABLE_KEY  - logo.dev publishable key (used to build the img URL)
"""
# AI-ASSISTED

import argparse
import os
import sys
import time
from typing import Optional

import openpyxl
import pymysql
import requests
from dotenv import load_dotenv

load_dotenv()

LOGO_DEV_SEARCH_URL = "https://api.logo.dev/search"
LOGO_DEV_IMG_URL = "https://img.logo.dev/{domain}?token={token}&size=64&format=webp"
LOGO_DEV_REQUEST_DELAY = 0.5  # seconds between logo.dev API calls


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


def fetch_vendor_by_name(conn: pymysql.Connection, vendor_name: str, location_id: int) -> Optional[dict]:
    """Returns the vendor row (id, Name, LogoUrl) for the given vendor name scoped to location."""
    with conn.cursor() as cursor:
        cursor.execute(
            """SELECT v.id, v.Name, v.LogoUrl
               FROM Vendors v
               INNER JOIN LocationVendors lv ON lv.VendorId = v.id
               WHERE lv.LocationId = %s AND v.Name LIKE %s
               LIMIT 1""",
            (location_id, f"%{vendor_name}%"),
        )
        return cursor.fetchone()


def update_vendor_logo(conn: pymysql.Connection, vendor_id: int, logo_url: str, dry_run: bool) -> None:
    print(f"    {'[DRY RUN] ' if dry_run else ''}Setting LogoUrl = {logo_url}")
    if not dry_run:
        with conn.cursor() as cursor:
            cursor.execute(
                "UPDATE Vendors SET LogoUrl = %s WHERE id = %s",
                (logo_url, vendor_id),
            )


def update_ingredient_category(
    conn: pymysql.Connection,
    ingredient_name: str,
    location_id: int,
    category: str,
    dry_run: bool,
) -> bool:
    """Updates Category on the ingredient row. Returns True if a row was found and updated."""
    with conn.cursor() as cursor:
        cursor.execute(
            "SELECT IngredientId, Category FROM Ingredients WHERE LocationId = %s AND Name = %s LIMIT 1",
            (location_id, ingredient_name),
        )
        row = cursor.fetchone()

    if not row:
        print(f"  NOT FOUND: {ingredient_name!r} (location_id={location_id})")
        return False

    current_category = row.get("Category") or ""
    if current_category == category:
        print(f"  Unchanged category ({category!r}): {ingredient_name}")
        return False

    print(f"  {'[DRY RUN] ' if dry_run else ''}Updating category {current_category!r} → {category!r}: {ingredient_name}")
    if not dry_run:
        with conn.cursor() as cursor:
            cursor.execute(
                "UPDATE Ingredients SET Category = %s, UpdatedAt = NOW() WHERE IngredientId = %s",
                (category, row["IngredientId"]),
            )
    return True


# ---------------------------------------------------------------------------
# logo.dev helpers
# ---------------------------------------------------------------------------

def search_logo_dev(query: str, secret_key: str) -> Optional[dict]:
    """Calls the logo.dev search API and returns the first matching result, or None."""
    if not query or len(query) < 2:
        return None
    try:
        response = requests.get(
            LOGO_DEV_SEARCH_URL,
            params={"q": query, "strategy": "suggest"},
            headers={"Authorization": f"Bearer {secret_key}"},
            timeout=10,
        )
        if not response.ok:
            print(f"    logo.dev search returned {response.status_code} for {query!r}")
            return None
        results = response.json()
        return results[0] if results else None
    except Exception as exc:
        print(f"    logo.dev search error for {query!r}: {exc}")
        return None


def build_logo_url(domain: str, publishable_key: str) -> str:
    return LOGO_DEV_IMG_URL.format(domain=domain, token=publishable_key)


# ---------------------------------------------------------------------------
# Excel helpers
# ---------------------------------------------------------------------------

def read_excel(input_file: str) -> list[dict]:
    """Returns a list of dicts with keys: vendor_name, ingredient_name, category."""
    workbook = openpyxl.load_workbook(input_file)
    worksheet = workbook.active

    headers = [cell.value for cell in worksheet[1]]
    rows = []
    for row in worksheet.iter_rows(min_row=2, values_only=True):
        row_data = dict(zip(headers, row))
        ingredient_name = row_data.get("Culinary Command Ingredient", "")
        vendor_name = row_data.get("Vendor Name", "")
        category = row_data.get("Category", "")
        if ingredient_name:
            rows.append({
                "ingredient_name": str(ingredient_name).strip(),
                "vendor_name": str(vendor_name).strip() if vendor_name else "",
                "category": str(category).strip() if category else "",
            })
    return rows


# ---------------------------------------------------------------------------
# Entry point
# ---------------------------------------------------------------------------

def main() -> None:
    parser = argparse.ArgumentParser(
        description="Populate Category on Ingredients and LogoUrl on Vendors from a cleaned recipe-ingredients Excel file."
    )
    parser.add_argument("--input-file", type=str, required=True, help="Path to the cleaned recipe-ingredients Excel file")
    parser.add_argument("--location-name", type=str, default=None, help="Location name to scope updates (e.g. 'Prairie Canary')")
    parser.add_argument("--location-id", type=int, default=None, help="Location ID (overrides --location-name lookup)")
    parser.add_argument("--dry-run", action="store_true", help="Preview changes without writing to the database")
    parser.add_argument("--skip-logos", action="store_true", help="Skip logo.dev lookups and only update categories")
    args = parser.parse_args()

    if not args.location_id and not args.location_name:
        parser.error("You must provide either --location-id or --location-name.")

    if not os.path.exists(args.input_file):
        print(f"ERROR: File not found: {args.input_file}", file=sys.stderr)
        sys.exit(1)

    logo_dev_secret_key = os.getenv("LOGO_DEV_SECRET_KEY", "")
    logo_dev_publishable_key = os.getenv("LOGO_DEV_PUBLISHABLE_KEY", "")

    if not args.skip_logos and (not logo_dev_secret_key or not logo_dev_publishable_key):
        print(
            "WARNING: LOGO_DEV_SECRET_KEY or LOGO_DEV_PUBLISHABLE_KEY not set — skipping logo updates.\n"
            "         Set both env vars or pass --skip-logos to suppress this warning.",
        )
        args.skip_logos = True

    print(f"Reading rows from {args.input_file}...")
    rows = read_excel(args.input_file)
    if not rows:
        print("No data rows found in the Excel file.")
        sys.exit(0)

    print(f"Found {len(rows)} row(s).\n")

    conn = get_db_connection()
    try:
        # Resolve location
        if args.location_id:
            location_id = args.location_id
            print(f"Using provided location id={location_id}.\n")
        else:
            location_id = fetch_location_id_by_name(conn, args.location_name)
            if not location_id:
                print(f"ERROR: Could not find location '{args.location_name}' in the database.", file=sys.stderr)
                sys.exit(1)
            print(f"Found location '{args.location_name}' (id={location_id}).\n")


        # ----------------------------------------------------------------
        # Phase 1: Update Categories on Ingredients
        # ----------------------------------------------------------------
        print("=" * 60)
        print("Phase 1: Updating ingredient categories")
        print("=" * 60)

        category_updated = 0
        category_skipped = 0
        category_not_found = 0

        for row in rows:
            ingredient_name = row["ingredient_name"]
            category = row["category"]

            if not category:
                print(f"  Skipping (no category): {ingredient_name}")
                category_skipped += 1
                continue

            updated = update_ingredient_category(conn, ingredient_name, location_id, category, args.dry_run)
            if updated:
                category_updated += 1
            else:
                # distinguish not-found from unchanged
                with conn.cursor() as cursor:
                    cursor.execute(
                        "SELECT IngredientId FROM Ingredients WHERE LocationId = %s AND Name = %s LIMIT 1",
                        (location_id, ingredient_name),
                    )
                    exists = cursor.fetchone()
                if not exists:
                    category_not_found += 1
                else:
                    category_skipped += 1

        print(f"\nCategories: {category_updated} updated, {category_skipped} unchanged/skipped, {category_not_found} not found in DB.\n")

        # ----------------------------------------------------------------
        # Phase 2: Update Vendor Logos
        # ----------------------------------------------------------------
        if not args.skip_logos:
            print("=" * 60)
            print("Phase 2: Updating vendor logos via logo.dev")
            print("=" * 60)

            # Collect unique vendor names from the file
            unique_vendor_names = list(dict.fromkeys(
                row["vendor_name"] for row in rows if row["vendor_name"]
            ))

            logos_updated = 0
            logos_skipped = 0
            logos_not_found = 0

            for vendor_name in unique_vendor_names:
                print(f"\nVendor: {vendor_name!r}")
                vendor = fetch_vendor_by_name(conn, vendor_name, location_id)
                if not vendor:
                    print(f"  NOT FOUND in DB for location_id={location_id}")
                    logos_not_found += 1
                    continue

                if vendor.get("LogoUrl"):
                    print(f"  Already has LogoUrl — skipping: {vendor['LogoUrl']}")
                    logos_skipped += 1
                    continue

                result = search_logo_dev(vendor_name, logo_dev_secret_key)
                time.sleep(LOGO_DEV_REQUEST_DELAY)

                if not result:
                    print(f"  No logo.dev result found for {vendor_name!r}")
                    logos_not_found += 1
                    continue

                domain = result.get("domain") or result.get("Domain") or ""
                if not domain:
                    print(f"  logo.dev result has no domain: {result}")
                    logos_not_found += 1
                    continue

                logo_url = build_logo_url(domain, logo_dev_publishable_key)
                print(f"  Found domain={domain!r}")
                update_vendor_logo(conn, vendor["id"], logo_url, args.dry_run)
                logos_updated += 1

            print(f"\nLogos: {logos_updated} updated, {logos_skipped} already had logo, {logos_not_found} not found.\n")

        # ----------------------------------------------------------------
        # Commit
        # ----------------------------------------------------------------
        if not args.dry_run:
            conn.commit()
            print("Changes committed to database.")
        else:
            print("Dry run — no changes were written to the database.")

    except Exception as error:
        conn.rollback()
        print(f"ERROR: {error}", file=sys.stderr)
        raise
    finally:
        conn.close()


if __name__ == "__main__":
    main()
