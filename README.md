# ♻️ CleanCity-BinSense

<div align="center">

### Smart Waste Management System

ASP.NET Core MVC platform for **IoT-enabled waste monitoring**, **route optimization**, and **public bin visibility**.

![.NET](https://img.shields.io/badge/.NET-9.0-purple)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET-Core-blue)
![SQL Server](https://img.shields.io/badge/Database-SQL%20Server-red)
![Project](https://img.shields.io/badge/Status-Active-success)

</div>

---

# 🌍 Overview

**CleanCity-BinSense** is a **Smart Waste Management System** designed to improve city waste collection efficiency using **IoT-enabled smart bins** and **role-based dashboards**.

The system collects **sensor data from smart bins**, calculates **fill levels**, and helps authorities optimize waste collection operations.

### Goals

* ♻️ Reduce unnecessary waste collection trips
* 📊 Monitor bin fill levels in real time
* 🚛 Optimize driver routes
* 🗺 Provide public visibility of bin locations
* 📡 Integrate hardware sensor data

---

# 👥 User Roles

The system provides **role-based access control** with different dashboards.

| Role            | Description                                           |
| --------------- | ----------------------------------------------------- |
| 👑 **Admin**    | System configuration, user management, bin management |
| 🖥 **Operator** | Monitor bins and schedule waste collection            |
| 🚛 **Driver**   | View assigned routes and perform waste pickup         |

---

# 📡 Hardware Status

The smart bin hardware is already implemented and generating **fill-level logs**.

Example device log:

```text
2025-12-14 22:13:56 | Distance: 34.8 cm | Fill: 55%
```

These logs are used to calculate:

* Bin fill percentage
* Collection priority
* Historical waste trends

---

# ✨ Features

## ♻️ Smart Bin Monitoring

Real-time monitoring of waste bin fill levels using IoT sensors.

## 📊 Role-Based Dashboards

Separate dashboards for **Admin**, **Operator**, and **Driver**.

## 🗺 Public Waste Map

Public users can see **bin locations and status** on a map.

## 🚛 Driver Route Management

Drivers can view **assigned waste collection routes**.

## 📡 Hardware Log Integration

The system reads and processes sensor-generated fill logs.

---

# 🛠 Technology Stack

| Technology            | Purpose                   |
| --------------------- | ------------------------- |
| ASP.NET Core MVC      | Web application framework |
| Entity Framework Core | Database ORM              |
| SQL Server            | Data storage              |
| Razor Views           | Frontend UI rendering     |
| IoT Sensors           | Bin fill-level detection  |

---

# ⚙️ Prerequisites

Ensure the following are installed:

* **.NET SDK 9.0+**
* **SQL Server**
* **Visual Studio / VS Code**

Check .NET version:

```bash
dotnet --version
```

---

# 🚀 Run the Project Locally

Clone the repository:

```bash
git clone https://github.com/RX-kabir/CleanCity-BinSense.git
cd CleanCity-BinSense
```

Restore dependencies:

```bash
dotnet restore
```

Run the application:

```bash
dotnet run
```

The application will start using URLs configured in:

```
Properties/launchSettings.json
```

---

# 🏗 Build the Project

Compile the project using:

```bash
dotnet build
```

---

# 📂 Project Structure

```
CleanCity-BinSense
│
├── Areas/                 Role-based MVC areas
│   ├── Admin
│   ├── Operator
│   ├── Driver
│   └── Identity
│
├── Controllers/           Main application controllers
├── Data/                  EF Core ApplicationDbContext and migrations
├── Models/                View models and DTOs
├── Views/                 Razor views and layouts
├── wwwroot/               Static assets (CSS, JS, libraries)
│
└── database_scripts/      SQL schema, seeds, procedures, helpers
```

---

# 🔧 Configuration

Configuration files:

```
appsettings.json
appsettings.Development.json
```

Update the following before running:

* Database connection strings
* Environment-specific settings
* Hardware integration settings

Example connection string:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=.;Database=CleanCityDB;Trusted_Connection=True;"
}
```

---

# 🧠 System Workflow

```
Smart Bin Sensor
      │
      ▼
Distance Data Logged
      │
      ▼
Fill Percentage Calculated
      │
      ▼
Stored in Database
      │
      ▼
Displayed in Operator Dashboard & Public Map
```

---


---

<div align="center">

### 🌱 Smart Cities Start With Smart Waste Management

**CleanCity-BinSense**

</div>


