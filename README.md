# DRDO Canteen Management System

A professional full-stack application for managing canteen services at DRDO, featuring a modern React frontend and a robust .NET Core backend.

## 🚀 Features
- **Modern UI**: Clean, responsive design built with React, Vite, and Framer Motion.
- **Weekly Menu**: Dynamic fetching of the weekly meal plan.
- **Booking System**: Secure meal pre-booking with unique token generation.
- **Database Persistence**: SQLite integration for storing and managing bookings.
- **Professional Aesthetics**: Tailored for DRDO's professional environment.

## 🛠️ Technology Stack
- **Frontend**: React, Vite, Lucide Icons, Framer Motion, Axios
- **Backend**: ASP.NET Core 9.0 Web API, Entity Framework Core (SQLite)

## 🏃‍♂️ How to Run

### Prerequisites
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Node.js & npm](https://nodejs.org/)

### Simplified Launch
You can run the entire application (both frontend and backend) simultaneously using the provided PowerShell script:

```powershell
./run.ps1
```

### Manual Launch

#### 1. Start the Backend
```bash
cd "Canteen management/Canteen management/CanteenApi"
dotnet run
```
The backend will be available at `http://localhost:5170`.

#### 2. Start the Frontend
```bash
cd frontend
npm install
npm run dev
```
The frontend will be available at `http://localhost:5173`.

## 📁 Project Structure
- `frontend/`: React application source code.
- `Canteen management/.../CanteenApi/`: .NET Core API source code.
- `Canteen management/.../CanteenApi/wwwroot/menu.json`: Source of Truth for the weekly menu.
- `run.ps1`: Automation script for simultaneous execution.

---
© 2025 DRDO Canteen Management Project. Optimized for excellence.
