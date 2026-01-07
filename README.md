# 🔍 LocalScout V2

A modern, location-based service marketplace platform built with **ASP.NET Core MVC** that connects users with trusted local service providers. LocalScout enables service providers to register, get verified, and offer their services while users can discover and book local professionals in their area.

## 📋 Table of Contents

- [Features](#-features)
- [Technology Stack](#-technology-stack)
- [Architecture](#-architecture)
- [Prerequisites](#-prerequisites)
- [Installation](#-installation)
- [Configuration](#-configuration)
- [Usage](#-usage)
- [Project Structure](#-project-structure)
- [Contributing](#-contributing)
- [License](#-license)

## ✨ Features

### 👥 User Management

- **Dual Registration System** - Separate registration flows for Users and Service Providers
- Multi-step registration wizard with progress indicators
- Email confirmation and account verification
- Profile management with profile pictures
- Location-based user profiles with address autocomplete

### 🏢 Service Provider Features

- **Business Profile Management** - Business name, description, and service details
- **Verification System** - Document upload and admin verification workflow
- Provider dashboard with statistics
- Service category management
- Verification status tracking (Pending, Approved, Rejected)

### 📍 Location Services

- **Geolocation Integration** - Automatic location detection using browser geolocation
- **Address Autocomplete** - Powered by LocationIQ API
- Reverse geocoding for coordinate-to-address conversion
- Latitude/Longitude storage for proximity-based searches

### 🔐 Authentication & Authorization

- ASP.NET Core Identity integration
- Role-based access control (Admin, ServiceProvider, User)
- Email confirmation workflow
- Secure password policies

### 👨‍💼 Admin Dashboard

- User and provider management (incl. Block/Unblock functionality)
- Provider verification workflow
- Service category approval
- Platform statistics and analytics

### 🔔 Notification System

- In-app notifications
- Read/unread status tracking
- Notification metadata support

### 💳 Payments & Booking

- **Secure Payments** - Integrated SSLCommerz payment gateway
- **Advanced Booking Lifecycle** - Complete flow: Request → Provider Review → Job Execution → Payment → Completion
- **Rescheduling System** - Negotiation-based rescheduling where users propose new times and providers accept or reject
- **Digital Receipts** - Automated PDF receipt generation
- **Transaction History** - Comprehensive payment logs for users and providers

### 🤖 AI-Powered Features

- **Smart Descriptions** - AI-generated professional descriptions for services and profiles (Powered by Hugging Face)
- **Context-Aware Generation** - Uses provider data to create tailored content

### 🛡️ Enhanced Security

- **Audit Logging** - Complete system activity tracking for compliance

### 📂 Service Categories

- Dynamic category management
- Category icons and descriptions
- Active/Inactive status
- Admin approval workflow

## 🛠 Technology Stack

| Category                 | Technology                              |
| ------------------------ | --------------------------------------- |
| **Backend**              | ASP. NET Core 8.0, C#                   |
| **Frontend**             | JavaScript, HTML, CSS                   |
| **CSS Framework**        | Tailwind CSS, Bootstrap 4               |
| **Admin Template**       | AdminLTE 3                              |
| **Database**             | Entity Framework Core, SQL Server       |
| **Authentication**       | ASP.NET Core Identity, HttpOnly Cookies |
| **Payment Gateway**      | SSLCommerz                              |
| **AI Services**          | Hugging Face Inference API              |
| **Location API**         | LocationIQ                              |
| **JavaScript Libraries** | jQuery, DataTables, Font Awesome        |

## 🏗 Architecture

The project follows **Clean Architecture** principles with clear separation of concerns:

```
LocalScout_V2/
├── LocalScout.Domain/           # Domain Layer (Entities & Enums)
│   ├── Entities/
│   │   ├── ApplicationUser.cs
│   │   ├── ServiceCategory.cs
│   │   ├── VerificationRequest.cs
│   │   └── Notification.cs
│   └── Enums/
│       └── VerificationStatus.cs
│
├── LocalScout.Application/      # Application Layer (DTOs & Interfaces)
│   ├── DTOs/
│   │   ├── UserDto.cs
│   │   ├── ServiceProviderDto.cs
│   │   ├── ProviderDashboardDto.cs
│   │   ├── UserDashboardDto.cs
│   │   ├── BookingDto.cs
│   │   └── NotificationDto.cs
│   └── Interfaces/
│       ├── IServiceProviderRepository.cs
│       ├── IVerificationRepository.cs
│       ├── ILocationService.cs
│       └── INotificationRepository.cs
│
├── LocalScout.Infrastructure/   # Infrastructure Layer
│   ├── Data/
│   │   ├── ApplicationDbContext.cs
│   │   └── DbSeeder.cs
│   ├── Repositories/
│   │   ├── ServiceProviderRepository.cs
│   │   ├── VerificationRepository.cs
│   │   ├── UserRepository.cs
│   │   ├── ServiceCategoryRepository.cs
│   │   └── NotificationRepository.cs
│   ├── Services/
│   │   └── LocationService.cs
│   ├── Constants/
│   │   └── RoleNames.cs
│   └── Migrations/
│
├── LocalScout. Web/              # Presentation Layer (MVC)
│   ├── Controllers/
│   │   ├── HomeController.cs
│   │   ├── AdminController.cs
│   │   ├── ProviderController.cs
│   │   ├── UserController. cs
│   │   └── LocationController.cs
│   ├── Areas/Identity/
│   │   └── Pages/Account/
│   │       ├── Register.cshtml
│   │       ├── RegisterProvider.cshtml
│   │       └── Login.cshtml
│   ├── Views/
│   ├── ViewModels/
│   └── wwwroot/
│       ├── js/modules/
│       │   └── location-service.js
│       ├── css/
│       └── images/
│
└── LocalScout.sln               # Solution File
```

### Domain Entities

| Entity                  | Description                                                        |
| ----------------------- | ------------------------------------------------------------------ |
| **ApplicationUser**     | Extended Identity user with profile, location, and business fields |
| **ServiceCategory**     | Service categories with approval workflow                          |
| **VerificationRequest** | Provider verification documents and status                         |
| **Booking**             | Service booking with negotiation and payment status                |
| **Review**              | User ratings and reviews for services                              |
| **AuditLog**            | System-wide administrative and security action logs                |
| **CategoryRequest**     | Provider submitted category proposals                              |
| **Notification**        | User notifications with read status                                |

## 📋 Prerequisites

- [. NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8. 0) or later
- [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) (LocalDB, Express, or full version)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or [VS Code](https://code.visualstudio.com/)
- [LocationIQ API Key](https://locationiq.com/) (for geolocation services)

## 🚀 Installation

1. **Clone the repository**

   ```bash
   git clone https://github.com/AlMamun09/LocalScout_V2.git
   cd LocalScout_V2
   ```

2. **Restore NuGet packages**

   ```bash
   dotnet restore
   ```

3. **Update the database connection string**

   Edit `appsettings.json` in the `LocalScout.Web` project:

   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=Your_Server_Name;Database=LocalScoutDb;Trusted_Connection=True;MultipleActiveResultSets=true"
     },
     "LocationIQ": {
       "ApiKey": "your-locationiq-api-key"
     }
   }
   ```

4. **Apply database migrations**

   ```bash
   cd LocalScout.Web
   dotnet ef database update --project ../LocalScout.Infrastructure
   ```

5. **Run the application**

   ```bash
   dotnet run
   ```

6. **Access the application**

   Open your browser and navigate to `https://localhost:5001` or `http://localhost:5000`

## ⚙ Configuration

### Database Configuration

The application uses Entity Framework Core with SQL Server. Migrations are stored in the `LocalScout.Infrastructure` project.

### LocationIQ API

The location service uses LocationIQ for geocoding. Configure your API key in `appsettings. json`:

```json
{
  "LocationIQ": {
    "ApiKey": "pk. your-api-key-here"
  }
}
```

### Identity Configuration

Password requirements are configured in `Program.cs`:

- Minimum length: 3 characters
- No special character requirements (configurable)
- Email confirmation required

### External Services

Add the following to your `appsettings.json` for full functionality:

```json
{
  "SSLCommerz": {
    "StoreId": "your-store-id",
    "StorePassword": "your-store-password",
    "IsSandbox": true
  },
  "HuggingFace": {
    "ApiKey": "your-hugging-face-token"
  }
}
```

## 📖 Usage

### For Users

1. **Register** - Navigate to Register and choose "User" registration
2. **Confirm Email** - Check your inbox and click the confirmation link
3. **Browse Services** - Explore local service providers by category
4. **Book Services** - Connect with verified service providers

### For Service Providers

1.  **Register as Provider** - Choose "Provider" during registration
2.  **Complete Business Profile** - Add business name and description
3.  **Submit Verification** - Upload required documents for verification
4.  **Wait for Approval** - Admin will review and approve your account
5.  **Start Offering Services** - Once verified, create service listings

### For Administrators

1. **Access Admin Dashboard** - Login with admin credentials
2. **Manage Users** - View and manage user accounts
3. **Verify Providers** - Review and approve/reject provider applications
4. **Manage Categories** - Create and manage service categories

## 🔑 Default Roles

| Role                | Description                                                     |
| ------------------- | --------------------------------------------------------------- |
| **Admin**           | Full platform access, user management, provider verification    |
| **ServiceProvider** | Provider dashboard, service management, verification submission |
| **User**            | Browse services, make bookings, manage profile                  |

## 📁 Key Files

| File                      | Purpose                                             |
| ------------------------- | --------------------------------------------------- |
| `Program.cs`              | Application configuration and dependency injection  |
| `ApplicationDbContext.cs` | EF Core database context with entity configurations |
| `DbSeeder.cs`             | Database seeding for roles and initial data         |
| `LocationService.cs`      | LocationIQ API integration for geocoding            |
| `location-service.js`     | Frontend location autocomplete and geolocation      |

## 🎨 UI Features

- **Responsive Design** - Mobile-first approach with Tailwind CSS
- **Modern Registration Flow** - Multi-step wizard with animations
- **AdminLTE Dashboard** - Professional admin interface
- **Real-time Validation** - Client-side form validation
- **Location Autocomplete** - Address suggestions as you type

## 🤝 Contributing

Contributions are welcome! Please follow these steps:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 📄 License

This project is open source. Please check the repository for license information.

---

## 👨‍💻 Author

**AlMamun09** - [GitHub Profile](https://github.com/AlMamun09)

---

⭐ If you find this project useful, please consider giving it a star on GitHub!
