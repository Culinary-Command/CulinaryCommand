#!/bin/bash
set -e
exec > /var/log/user-data.log 2>&1

# Boot up prechecks
echo "[1/8] Updating system packages..."
dnf update -y
dnf install -y unzip

# Install Dependencies
echo "[2/8] Installing .NET 9 ASP.NET Core Runtime..."
dnf install -y libicu
curl -Lo /tmp/dotnet-install.sh https://dot.net/v1/dotnet-install.sh
chmod +x /tmp/dotnet-install.sh
/tmp/dotnet-install.sh --channel 9.0 --runtime aspnetcore --install-dir /usr/share/dotnet
ln -sf /usr/share/dotnet/dotnet /usr/local/bin/dotnet
echo "dotnet version: $(dotnet --version)"

# Configure instance
echo "[3/8] Creating app user and directories..."
useradd -r -s /sbin/nologin culinarycommand || true
mkdir -p /opt/culinarycommand/app
chown culinarycommand:culinarycommand /opt/culinarycommand

# Fetch app
echo "[4/8] Pulling app binaries from S3..."
aws s3 cp s3://culinary-command-s3-bucket/releases/latest/app.zip /tmp/app.zip
unzip -o /tmp/app.zip -d /opt/culinarycommand/app
chown -R culinarycommand:culinarycommand /opt/culinarycommand/app
rm -f /tmp/app.zip

# Get secrets from AWS
echo "[5/8] Fetching secrets from SSM Parameter Store..."
DB_CONN=$(aws ssm get-parameter \
    --name "/culinarycommand/prod/ConnectionStrings__DefaultConnection" \
    --with-decryption \
    --region us-east-2 \
    --query "Parameter.Value" \
    --output text)

COGNITO_CLIENT_SECRET=$(aws ssm get-parameter \
    --name "/culinarycommand/prod/Cognito__ClientSecret" \
    --with-decryption \
    --region us-east-2 \
    --query "Parameter.Value" \
    --output text)

COGNITO_CLIENT_ID=$(aws ssm get-parameter \
    --name "/culinarycommand/prod/Cognito__ClientId" \
    --with-decryption \
    --region us-east-2 \
    --query "Parameter.Value" \
    --output text)

COGNITO_USER_POOL_ID=$(aws ssm get-parameter \
    --name "/culinarycommand/prod/Cognito__UserPoolId" \
    --with-decryption \
    --region us-east-2 \
    --query "Parameter.Value" \
    --output text)

COGNITO_DOMAIN=$(aws ssm get-parameter \
    --name "/culinarycommand/prod/Cognito__Domain" \
    --with-decryption \
    --region us-east-2 \
    --query "Parameter.Value" \
    --output text)

GOOGLE_API_KEY=$(aws ssm get-parameter \
    --name "/culinarycommand/prod/GOOGLE_API_KEY" \
    --with-decryption \
    --region us-east-2 \
    --query "Parameter.Value" \
    --output text)

LOGODEV_PUBLISHABLE_KEY=$(aws ssm get-parameter \
    --name "/culinarycommand/prod/LogoDev__PublishableKey" \
    --with-decryption \
    --region us-east-2 \
    --query "Parameter.Value" \
    --output text)

LOGODEV_SECRET_KEY=$(aws ssm get-parameter \
    --name "/culinarycommand/prod/LogoDev__SecretKey" \
    --with-decryption \
    --region us-east-2 \
    --query "Parameter.Value" \
    --output text)

RESEND_API_TOKEN=$(aws ssm get-parameter \
    --name "/culinarycommand/prod/Email__ResendApiToken" \
    --with-decryption \
    --region us-east-2 \
    --query "Parameter.Value" \
    --output text)

# SmartTask config
SMARTTASK_LAMBDA_FUNCTION_URL_ENDPOINT=$(aws ssm get-parameter \
    --name "/culinarycommand/prod/SmartTask__LambdaFunctionUrlEndpoint" \
    --with-decryption \
    --region us-east-2 \
    --query "Parameter.Value" \
    --output text)

SMARTTASK_AWS_REGION=$(aws ssm get-parameter \
    --name "/culinarycommand/prod/SmartTask__AwsRegion" \
    --with-decryption \
    --region us-east-2 \
    --query "Parameter.Value" \
    --output text)

SMARTTASK_DEFAULT_PREP_BUFFER_MINUTES=$(aws ssm get-parameter \
    --name "/culinarycommand/prod/SmartTask__DefaultPrepBufferMinutes" \
    --with-decryption \
    --region us-east-2 \
    --query "Parameter.Value" \
    --output text)

SMARTTASK_DEFAULT_LEAD_TIME_WHEN_UNKNOWN=$(aws ssm get-parameter \
    --name "/culinarycommand/prod/SmartTask__DefaultLeadTimeWhenUnknown" \
    --with-decryption \
    --region us-east-2 \
    --query "Parameter.Value" \
    --output text)

SMARTTASK_ENABLED=$(aws ssm get-parameter \
    --name "/culinarycommand/prod/SmartTask__Enabled" \
    --with-decryption \
    --region us-east-2 \
    --query "Parameter.Value" \
    --output text)

# Write environment variables to file
echo "[6/8] Writing environment file..."
cat > /etc/culinarycommand.env << EOF
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://0.0.0.0:5000
ConnectionStrings__DefaultConnection=${DB_CONN}
COGNITO_CLIENT_SECRET=${COGNITO_CLIENT_SECRET}
Authentication__Cognito__ClientId=${COGNITO_CLIENT_ID}
Authentication__Cognito__ClientSecret=${COGNITO_CLIENT_SECRET}
Authentication__Cognito__UserPoolId=${COGNITO_USER_POOL_ID}
Authentication__Cognito__Domain=${COGNITO_DOMAIN}
Authentication__Cognito__CallbackPath=/signin-oidc
Authentication__Cognito__SignedOutCallbackPath=/signout-callback-oidc
AWS__Region=us-east-2
GOOGLE_API_KEY=${GOOGLE_API_KEY}
LogoDev__PublishableKey=${LOGODEV_PUBLISHABLE_KEY}
LogoDev__SecretKey=${LOGODEV_SECRET_KEY}
Email__ResendApiToken=${RESEND_API_TOKEN}

# SmartTask
SmartTask__LambdaFunctionUrlEndpoint=${SMARTTASK_LAMBDA_FUNCTION_URL_ENDPOINT}
SmartTask__AwsRegion=${SMARTTASK_AWS_REGION}
SmartTask__DefaultPrepBufferMinutes=${SMARTTASK_DEFAULT_PREP_BUFFER_MINUTES}
SmartTask__DefaultLeadTimeWhenUnknown=${SMARTTASK_DEFAULT_LEAD_TIME_WHEN_UNKNOWN}
SmartTask__Enabled=${SMARTTASK_ENABLED}
EOF
chmod 600 /etc/culinarycommand.env

# Create systemd service to host app
echo "[7/8] Creating systemd service..."
cat > /etc/systemd/system/culinarycommand.service << 'EOF'
[Unit]
Description=Culinary Command Blazor App
After=network.target

[Service]
WorkingDirectory=/opt/culinarycommand/app
ExecStart=/usr/local/bin/dotnet /opt/culinarycommand/app/CulinaryCommand.dll
Restart=always
RestartSec=10
EnvironmentFile=/etc/culinarycommand.env
User=culinarycommand
Group=culinarycommand
StandardOutput=journal
StandardError=journal
SyslogIdentifier=culinarycommand

[Install]
WantedBy=multi-user.target
EOF

# Enable and start the app
echo "[8/8] Enabling and starting Culinary Command service..."
systemctl daemon-reload
systemctl enable culinarycommand
systemctl start culinarycommand

echo "Culinary Command bootstrap complete."
