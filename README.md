Demo web app for DynamicOrm using the SQLite adapter

This demo is a minimal blog application that uses DynamicOrm to persist posts to an SQLite database. It includes a simple admin panel for creating, editing and deleting posts and uploading cover images.

Run locally:

1. Build the solution

   dotnet build

2. Run the web app (use a free port if 5000/5011 already used):

   dotnet run --project DynamicOrmDemo/DynamicOrmDemo.csproj --urls "http://127.0.0.1:5011"

3. Open in a browser: http://127.0.0.1:5011

Admin panel:

- Login at `/admin` (default password: `admin123`)
- Create posts and upload images. Images are stored under `wwwroot/uploads` and served statically.

Notes:

- The app seeds a sample post the first time it runs.
- The demo uses `demo.db` in the demo project folder for the SQLite datastore.

