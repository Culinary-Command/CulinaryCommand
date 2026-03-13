from locust import HttpUser, task, between
from dotenv import load_dotenv
import boto3
import hashlib
import hmac
import base64
import os

load_dotenv()

# Fetch token once at startup and share across all simulated users
# to avoid Cognito rate-limiting (429) during user spawn
_shared_token: str | None = None

def get_shared_token() -> str:
    global _shared_token
    if _shared_token is None:
        _shared_token = get_cognito_token()
    return _shared_token

def get_secret_hash(username):
    client_id = os.getenv("COGNITO_CLIENT_ID")
    client_secret = os.getenv("COGNITO_CLIENT_SECRET")
    message = username + client_id

    dig = hmac.new(
        client_secret.encode("utf-8"),
        msg=message.encode("utf-8"),
        digestmod=hashlib.sha256
    ).digest()  # type: ignore
    return base64.b64encode(dig).decode()

def get_cognito_token():
    username = os.getenv("COGNITO_USERNAME")
    client = boto3.client("cognito-idp", region_name=os.getenv("AWS_REGION"))
    response = client.initiate_auth(
        AuthFlow="USER_PASSWORD_AUTH",
        AuthParameters={
            "USERNAME": os.getenv("COGNITO_USERNAME"),
            "PASSWORD": os.getenv("COGNITO_PASSWORD"),
            "SECRET_HASH": get_secret_hash(username)
        },
        ClientId=os.getenv("COGNITO_CLIENT_ID"),
    )
    return response["AuthenticationResult"]["IdToken"]

class LocustLoadTesting(HttpUser):
    wait_time = between(0.5, 1)

    def on_start(self):
        token = get_shared_token()

        self.client.headers.update({
            "Authorization": f"Bearer {token}"
        })

    @task(3)
    def browse_recipes(self):
        self.client.get("/recipes", name="Browse Recipes")
    
    @task(1)
    def view_recipe(self):
        recipe_id = 2
        self.client.get(f"/recipes/view/{recipe_id}", name="View Recipe")
    
    @task(1)
    def view_dashboard(self):
        self.client.get("/dashboard", name="View Dashboard")