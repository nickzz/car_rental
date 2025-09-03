name: .NET API CI/CD

on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]

jobs:
  build:
    runs-on: ubuntu-latest

    services:
      postgres:
        image: postgres:14
        env:
          POSTGRES_USER: postgres
          POSTGRES_PASSWORD: ${{ secrets.DB_PASSWORD }}
          POSTGRES_DB: car_rentalDB
        ports:
          - 5432:5432
        options: >-
          --health-cmd pg_isready
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5

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

      - name: 🔧 Install EF Core CLI
        run: dotnet tool install --global dotnet-ef

      - name: 🧪 (Optional) EF Migrate DB
        working-directory: ./CarRentalAPI
        run: dotnet ef database update
        env:
          ConnectionStrings__DefaultConnection: ${{ secrets.RENDER_DB_CONNECTION }}
  deploy:
    needs: build
    runs-on: ubuntu-latest

    steps:
      - name: 🚀 Trigger Render Deploy
        run: |
          curl -X POST "https://api.render.com/v1/services/${{ secrets.RENDER_SERVICE_ID }}/deploys" \
          -H "Authorization: Bearer ${{ secrets.RENDER_API_KEY }}" \
          -H "Content-Type: application/json"
