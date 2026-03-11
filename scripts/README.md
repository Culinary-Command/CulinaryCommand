# Getting Started

## Prerequisites

- Python 3.8+
- AWS Cognito credentials for the target environment

---

## Setup

**1. Create and activate a virtual environment**

```bash
python3 -m venv venv
source venv/bin/activate
```

**2. Install dependencies**

```bash
pip install -r requirements.txt
```

---

## Environment Variables

Create a `.env` file in the `scripts/` directory with the following:

```env
COGNITO_CLIENT_ID=your_client_id
COGNITO_CLIENT_SECRET=your_client_secret
COGNITO_USERNAME=your_username
COGNITO_PASSWORD=your_password
AWS_REGION=us-east-1
```

These credentials are used to authenticate against AWS Cognito before each load test run. The script calls `InitiateAuth` with `USER_PASSWORD_AUTH` flow and uses the returned `IdToken` as a Bearer token on all requests.

---

## Running Locust

Start the Locust web UI:

```bash
locust -f locustfile.py --host=https://culinary-command.com
```

Then open [http://localhost:8089](http://localhost:8089) to configure the number of users and spawn rate, and start the test.

To run headless (no UI):

```bash
locust -f locustfile.py --host=https://your-app-url.com --headless -u 50 -r 10 --run-time 1m
```

- `-u` — number of users
- `-r` — spawn rate (users per second)
- `--run-time` — how long to run the test
