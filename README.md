# NewYou

A full-stack health and fitness app with a React frontend and .NET backend.

---

## Table of Contents
- [Features](#features)
- [Project Structure](#project-structure)
- [Getting Started](#getting-started)
  - [Frontend Setup](#frontend-setup)
  - [Backend Setup](#backend-setup)
- [Building for Production](#building-for-production)
- [Deployment](#deployment)
- [Configuration & Environment Variables](#configuration--environment-variables)
- [Contributing](#contributing)

---

## Features
- User authentication (login/register)
- Diet and exercise plans
- Social feed with posts, likes, and comments
- Admin panel for managing users, diets, and exercises

---

## Project Structure
```
NewYou/
  backend/      # .NET backend API
  frontend/     # React frontend app
```

---

## Getting Started

### Frontend Setup
1. **Install dependencies:**
   ```sh
   cd frontend
   npm install
   ```
2. **Start the development server:**
   ```sh
   npm start
   ```
   - The app will run at [http://localhost:3000](http://localhost:3000)

### Backend Setup
1. **Install .NET dependencies:**
   ```sh
   cd backend
   dotnet restore
   ```
2. **Run the backend server:**
   ```sh
   dotnet run
   ```
   - The API will run at [http://localhost:5068](http://localhost:5068) (default)

---

## Building for Production

### Frontend
1. **Build the static site:**
   ```sh
   cd frontend
   npm run build
   ```
   - Output will be in `frontend/build/`.

### Backend
1. **Publish the backend:**
   ```sh
   cd backend
   dotnet publish -c Release -o ./publish
   ```
   - Output will be in `backend/publish/`.

---

## Deployment
- **Frontend:** Deploy the contents of `frontend/build/` to any static hosting (Netlify, Vercel, GitHub Pages, etc.).
- **Backend:** Deploy the contents of `backend/publish/` to your server or cloud provider (Azure, AWS, etc.).

---

## Configuration & Environment Variables
- **Frontend:**
  - API endpoint is currently hardcoded as `http://localhost:5068`. For production, update this in your code or use environment variables (see React docs for `.env` usage).
- **Backend:**
  - Configure connection strings and secrets in `backend/appsettings.json` or `backend/appsettings.Development.json`.

---

## Contributing
1. Fork the repo
2. Create a new branch (`git checkout -b feature/your-feature`)
3. Commit your changes (`git commit -am 'Add new feature'`)
4. Push to the branch (`git push origin feature/your-feature`)
5. Open a Pull Request

---

**Questions?** Open an issue or contact the maintainer. 