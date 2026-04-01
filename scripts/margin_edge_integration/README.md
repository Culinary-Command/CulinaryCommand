# MarginEdge integration scripts

This folder contains small Python utilities for pulling data from the **MarginEdge Public API** and exporting/inspecting it locally.

## What these scripts do

Typical workflow:

1. Create an authenticated HTTP session using the MarginEdge API key.
2. Call MarginEdge endpoints under:

   - Base URL: `https://api.marginedge.com/public`
   - Auth header: `X-API-KEY: <api_key>`

3. Pass required query parameters (commonly `restaurantUnitId`, plus optional date filters like `startDate`/`endDate`).
4. Handle pagination when the API returns a `nextPage` cursor.
5. Normalize responses (some endpoints return arrays, others wrap arrays in properties like `orders`, `content`, etc.).
6. Print summaries and/or export results (for example to Excel).

## Setup

### 1) Create a virtual environment (recommended)

```bash
python3 -m venv .venv
source .venv/bin/activate
```

### 2) Install dependencies

If you have a `requirements.txt` in the parent `scripts/` folder, install from there:

```bash
pip install -r ../requirements.txt
```

(If dependencies change in the future, keep `../requirements.txt` as the source of truth.)

### 3) Configure environment variables

These scripts expect an API key in your environment (often via a local `.env` file).

Create a `.env` file **in the repo root** or in this folder (wherever you run the script from) with:

```ini
ME_API_KEY=YOUR_MARGINEDGE_KEY
# optional
ME_API_BASE_URL=https://api.marginedge.com/public
```

- `ME_API_KEY` is required.
- `ME_API_BASE_URL` is optional and defaults to `https://api.marginedge.com/public`.

## Scripts

### `get_prairie_canary_restaurant_orders.py`

Fetches orders for a given `restaurantUnitId` over a recent date range, then exports a simplified list to an Excel file.

Key behaviors:

- Uses a shared `requests.Session()` with headers:
  - `X-API-KEY`
  - `Accept: application/json`
- Calls `GET /orders` with:
  - `restaurantUnitId`
  - `startDate` / `endDate` (formatted as `YYYY-MM-DD`)
- Handles rate limiting (`HTTP 429`) by respecting `Retry-After`.
- Handles cursor pagination with `nextPage`.
- Exports to `orders_<restaurantUnitId>_<yyyymmdd>.xlsx`.

Run it:

```bash
python3 get_prairie_canary_restaurant_orders.py --restaurant-id <restaurantUnitId>
```

Optional JSON output:

```bash
python3 get_prairie_canary_restaurant_orders.py --restaurant-id <restaurantUnitId> --json
```

## Notes / troubleshooting

- `restaurantUnitId` is commonly a GUID/UUID. If you pass an invalid value, the API may respond with errors (sometimes even `500`).
- If you want to reproduce calls in Insomnia/Postman:
  - Method: `GET`
  - URL: `https://api.marginedge.com/public/<endpoint>`
  - Header: `X-API-KEY: <your_api_key>`
  - Query params: e.g. `restaurantUnitId`, `startDate`, `endDate`
