#!/bin/bash
# Export Swagger JSON for Hotshot Logistics API
# This script starts the API, downloads the swagger.json, and saves it for mobile developers

echo "Starting Hotshot Logistics API..."

# Start the API in the background
dotnet run &
API_PID=$!

# Wait for API to be ready (max 30 seconds)
MAX_ATTEMPTS=30
ATTEMPT=0
API_URL="https://localhost:7060/swagger/v1/swagger.json"

echo "Waiting for API to start..."

while [ $ATTEMPT -lt $MAX_ATTEMPTS ]; do
    if curl -k -s -f -o /dev/null "$API_URL"; then
        echo "API is ready!"
        break
    fi
    sleep 1
    ATTEMPT=$((ATTEMPT + 1))
done

if [ $ATTEMPT -eq $MAX_ATTEMPTS ]; then
    echo "ERROR: API did not start in time"
    kill $API_PID
    exit 1
fi

# Download the swagger.json
echo "Downloading swagger.json..."

OUTPUT_PATH="$(dirname "$0")/swagger.json"

if curl -k -s -o "$OUTPUT_PATH" "$API_URL"; then
    echo "SUCCESS: Swagger file exported to: $OUTPUT_PATH"
    echo ""
    echo "You can now share this file with mobile developers!"
else
    echo "ERROR: Failed to download swagger.json"
fi

# Stop the API
echo "Stopping API..."
kill $API_PID
echo "Done!"
