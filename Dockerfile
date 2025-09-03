name: .NET API CI/CD

on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]

jobs:
  build:
    runs-on: ubuntu-latest

    steps:
      - name: ⬇️ Checkout Code
        uses: actions/checkout@v3

      - name: 🧰 Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'

      - name: 📦 Restore Dependencies
        working-directory: ./CarRentalAPI
        run: dotnet restore

      - name: 🛠️ Build
        working-directory: ./CarRentalAPI
        run: dotnet build --no-restore

      - name: 🧪 Run Tests (if available)
        working-directory: ./CarRentalAPI
        run: dotnet test --no-build --verbosity normal

  deploy:
    needs: build
    runs-on: ubuntu-latest

    steps:
      - name: 🚀 Trigger Render Deploy
        run: |
          curl -X POST "https://api.render.com/v1/services/${{ secrets.RENDER_SERVICE_ID }}/deploys" \
          -H "Authorization: Bearer ${{ secrets.RENDER_API_KEY }}" \
          -H "Content-Type: application/json"
