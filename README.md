# 📦 PharmaGo | Fullstack Medical Ecosystem

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)
![React](https://img.shields.io/badge/React-18-61DAFB?logo=react)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-336791?logo=postgresql)

**PharmaGo** is a high-load digital ecosystem designed to solve real-time medication shortages. It bridges the gap between individual customers, retail pharmacy chains, and wholesale depots (depots) within a unified information environment.

---

## 🚀 Key Capabilities

### 🛒 For Customers (Mobile & Bot)
- **Smart Search:** Search by brand name with automatic generic substitution logic.
- **Live Inventory Mapping:** Real-time visualization of drug availability in the nearest pharmacies.
- **Instant Booking:** Secure medication reservation with automated confirmation via Telegram.

### ⚕️ For Pharmacists (Web Dashboard)
- **Inventory Management:** Precise control over stock levels and expiration dates.
- **Depot Integration:** Direct synchronization with supplier warehouses for automated restocking of deficit items.
- **Real-time Alerts:** Instant SignalR notifications for new reservations and low-stock warnings.

---

## 🛠 Tech Stack

| Layer | Technologies |
| :--- | :--- |
| **Backend** | .NET 9 Web API, Entity Framework Core, SignalR, JWT Auth |
| **Database** | PostgreSQL, Redis (Caching & Sessions) |
| **Frontend** | React 18, Tailwind CSS, Framer Motion |
| **Mobile** | React Native (Expo) |
| **Bot** | Telegram.Bot API |

---

## 🏗 System Architecture

The project follows **Clean Architecture** principles to ensure scalability and maintainability:
1. **Domain**: Core entities (Medicines, Stocks, Pharmacies) and business rules.
2. **Application**: Interfaces, DTOs, and business use cases (CQRS pattern).
3. **Infrastructure**: Data persistence (EF Core) and external API integrations (Telegram).
4. **Presentation**: Unified REST API serving Web, Mobile, and Bot clients.

---

## 🚦 Quick Start (Development)

1. **Clone the repository:**
   ```bash
   git clone [https://github.com/bakhtiyarshirinov/PharmaGo.git](https://github.com/bakhtiyarshirinov/PharmaGo.git)
   ```
Developer: Bakhtiyar Shirinov

Email: bshirinovv@gmail.com


