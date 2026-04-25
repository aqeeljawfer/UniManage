# 🎓 UniManage

## 📌 Overview

**UniManage** is a university management system built using ASP.NET Core MVC.
It helps manage students, courses, assignments, and user authentication in a structured and scalable way.

---

## 🚀 Features

* 🔐 User Authentication (Login / Register)
* 👨‍🎓 Student Management
* 📚 Course Management
* 📝 Assignment Submission System
* 📊 Dashboard Interface
* 📩 Messaging / Communication Module

---

## 🛠️ Tech Stack

* **Backend:** ASP.NET Core MVC (C#)
* **Frontend:** Razor Views, HTML, CSS, Bootstrap
* **Database:** SQL Server
* **ORM:** Entity Framework Core

---

## 🏗️ Project Architecture

This project follows the **MVC (Model-View-Controller)** architecture:

* **Models** → Data & business logic
* **Views** → UI components
* **Controllers** → Handle requests & responses

---

## 📁 Folder Structure

```
UniManage/
│
├── Controllers/     # Application logic
├── Models/          # Data models
├── Views/           # UI pages
├── Data/            # Database context
├── Migrations/      # EF Core migrations
├── wwwroot/         # Static files (CSS, JS, images)
├── ViewModels/      # View-specific models
└── Properties/      # App configuration
```

---

## ⚙️ Installation & Setup

### 1️⃣ Clone the repository

```bash
git clone https://github.com/aqeeljawfer/UniManage.git
cd UniManage
```

### 2️⃣ Restore dependencies

```bash
dotnet restore
```

### 3️⃣ Configure database

* Open `appsettings.json`
* Update your SQL Server connection string

### 4️⃣ Apply migrations

```bash
dotnet ef database update
```

### 5️⃣ Run the project

```bash
dotnet run
```

---

## 📷 Screenshots

*(Add screenshots here — this is IMPORTANT for portfolio)*

* Login Page
* Dashboard
* Course Management
* Assignment Submission

---

## 🧪 Future Improvements

* Role-based access control (Admin / Student / Lecturer)
* REST API integration
* UI/UX improvements
* Deployment (Azure / Docker)

---

## 🤝 Contributing

Contributions are welcome. Feel free to fork this repo and submit a pull request.

---

## 📧 Contact

**Aqeel Jawfer**
📩 [akeeljawfer@gmail.com](mailto:akeeljawfer@gmail.com)

---

## ⭐ Support

If you find this project useful, consider giving it a star ⭐
