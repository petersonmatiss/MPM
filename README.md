# MPM - MetalProjekts Management

[![CI/CD Pipeline](https://github.com/petersonmatiss/MPM/actions/workflows/ci-cd.yml/badge.svg)](https://github.com/petersonmatiss/MPM/actions/workflows/ci-cd.yml)

> A comprehensive steel inventory management system for MetalProjekts, designed to evolve into a full MRP/ERP solution with CRM capabilities.

## 🎯 Overview

MPM (MetalProjekts Management) is a modern .NET 8 application that manages steel inventory, projects, quotations, manufacturing orders, and shop-floor operations. Built for a Latvian steel fabrication company, it handles the complete lifecycle from procurement to project delivery.

### Key Features

- **📦 Inventory Management**: Sheets, Profiles, and Profile Remnants tracking
- **🏭 Project Management**: Manufacturing timelines, statuses, and workflows  
- **💰 Quotation System**: Multi-level assemblies and pricing
- **🔧 Manufacturing Orders**: Work order management with drawing attachments
- **⏱️ Time Tracking**: Shop-floor kiosk integration via Power Apps
- **📊 Reporting**: Comprehensive inventory, usage, and cost analytics
- **🔔 Notifications**: Email, SMS, and Teams integration
- **🏷️ QR/Labeling**: Automated label generation and tracking

## 🏗️ Architecture

### Tech Stack

- **Frontend**: Blazor Server (.NET 8) with MudBlazor UI framework
- **Backend**: ASP.NET Core Web API with clean architecture
- **Database**: Azure SQL Database with Entity Framework Core
- **Authentication**: Microsoft Entra ID with role-based access
- **Real-time**: SignalR for live updates
- **Integration**: Power Automate, Power Apps, Azure services

### Project Structure

```
src/
├── Mpm.Domain/          # Domain entities, enums, and constants
├── Mpm.Data/            # EF Core context, configurations, and migrations  
├── Mpm.Services/        # Business logic and application services
├── Mpm.Web/             # Blazor Server UI application
├── Mpm.Api/             # REST API for external integrations
└── Mpm.Edge/            # Edge services and background processing

tests/
└── MPM.Tests/           # Unit and integration tests
```

### Core Domain Models

#### Inventory Management
- **Supplier**: Vendor information with VAT and contact details
- **Invoice**: Purchase invoices linking to inventory items
- **Sheet**: Steel sheet inventory with dimensions and steel grades
- **Profile**: Steel profile inventory with lengths and types
- **ProfileRemnant**: Leftover pieces from profile usage

#### Project & Manufacturing
- **Customer**: Client information and contacts
- **Project**: Manufacturing projects with timelines and statuses
- **Quotation**: Multi-level pricing with assemblies and parts
- **WorkOrder**: Manufacturing work orders with operations
- **BillOfMaterial**: Project BOMs and material requirements

#### Quality & Compliance
- **Certificate**: Material certificates and documentation
- **NonConformanceReport**: Quality issue tracking
- **DeclarationOfPerformance**: CE compliance documentation

#### Operations
- **MaterialReservation**: Inventory allocation to projects
- **UsageLog**: Material consumption tracking
- **WorkOrderOperation**: Time tracking and labor operations

## 🚀 Getting Started

### Prerequisites

- .NET 8.0 SDK
- SQL Server or Azure SQL Database
- Visual Studio 2022 or VS Code

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/petersonmatiss/MPM.git
   cd MPM
   ```

2. **Restore packages**
   ```bash
   dotnet restore
   ```

3. **Update database connection**
   - Configure connection string in `appsettings.json`
   - Update for Development, Staging, and Production environments

4. **Apply database migrations**
   ```bash
   cd src/Mpm.Api
   dotnet ef database update
   ```

5. **Build and run**
   ```bash
   dotnet build
   dotnet run --project src/Mpm.Web
   ```

### Development Setup

1. **Build the solution**
   ```bash
   dotnet build
   ```

2. **Run tests**
   ```bash
   dotnet test
   ```

3. **Start multiple projects** (recommended for full functionality)
   - Web App: `dotnet run --project src/Mpm.Web` (https://localhost:7089)
   - API: `dotnet run --project src/Mpm.Api` (https://localhost:7234)

## 🛠️ Configuration

### Database

The system uses Entity Framework Core with Azure SQL Database. Key configurations:

- **Multi-tenancy**: All entities inherit from `TenantEntity` 
- **Audit Trail**: Automatic CreatedAt/UpdatedAt/CreatedBy/UpdatedBy tracking
- **Soft Delete**: Query filters for `IsDeleted` properties
- **Optimistic Concurrency**: Row version tokens for conflict detection

### Authentication & Authorization

- **Microsoft Entra ID** integration for authentication
- **Role-based access control** with predefined roles:
  - Admin, Manager, Operator, Viewer
- **Tenant isolation** for multi-customer scenarios

### Business Rules

- **Measurements**: All lengths in millimeters (integers)
- **Currency**: EUR with 2 decimal precision  
- **Timezone**: Europe/Riga (auto clock-out at 17:01)
- **Steel Grades**: S355, S235, etc. with material certificates
- **QR Codes**: Include both plain text and numeric IDs

## 📝 Usage

### Inventory Management

1. **Register Suppliers**: Manage vendor information and VAT numbers
2. **Process Invoices**: Link purchase invoices to inventory items
3. **Track Inventory**: Monitor sheets, profiles, and remnants
4. **Material Usage**: Log consumption against projects

### Project Workflow

1. **Create Quotations**: Build multi-level assemblies with pricing
2. **Convert to Projects**: Generate manufacturing projects from quotes
3. **Generate Work Orders**: Create detailed manufacturing instructions
4. **Track Progress**: Monitor project status and timelines
5. **Quality Control**: Handle NCRs and compliance documentation

### Shop Floor Operations

1. **Time Tracking**: Use Power Apps kiosk for labor recording
2. **Material Consumption**: Scan QR codes to log usage
3. **Progress Updates**: Real-time status updates via SignalR
4. **Quality Checks**: Document inspections and certifications

## 🧪 Testing

The project includes comprehensive test coverage:

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test category
dotnet test --filter Category=Unit
```

### Test Categories

- **Unit Tests**: Business logic and service layer testing
- **Integration Tests**: Database and API endpoint testing  
- **Domain Model Tests**: Entity validation and relationships

Current test metrics: **54 tests passing**

## 🔧 Development Guidelines

### Code Standards

- **C# 12** with latest language features
- **Nullable reference types** enabled
- **Implicit usings** for common namespaces
- **EditorConfig** enforces consistent styling
- **Warnings as errors** in production builds

### Architecture Principles

- **Clean Architecture**: Clear separation of concerns
- **Domain-Driven Design**: Rich domain models
- **CQRS Pattern**: Separate read/write operations where beneficial
- **Repository Pattern**: Abstracted data access
- **Unit of Work**: Transactional consistency

### Performance Considerations

- **Async/await** throughout the application
- **EF Core optimization**: Eager loading, split queries
- **Caching strategies**: Response caching for read-heavy operations
- **SignalR scaling**: Azure SignalR service for multi-instance deployments

## 🚢 Deployment

### CI/CD Pipeline

The project includes GitHub Actions workflow for:

- **Build verification**: Compile all projects
- **Test execution**: Run full test suite  
- **Code quality**: Static analysis and linting
- **Security scanning**: Dependency vulnerability checks

### Environment Configuration

- **Development**: Local SQL Server with seed data
- **Staging**: Azure SQL with production-like data
- **Production**: Azure App Service with managed identity

### Infrastructure as Code

Terraform scripts for Azure resources:
- App Service hosting
- Azure SQL Database  
- Application Insights monitoring
- Key Vault for secrets management

## 📊 Monitoring & Observability

### Application Insights

- **Performance monitoring**: Response times and throughput
- **Error tracking**: Exception logging and alerting
- **Custom telemetry**: Business-specific metrics
- **Dashboards**: Real-time operational views

### Structured Logging

- **Serilog** with structured event logging
- **Log correlation**: Request tracking across services
- **Custom events**: Inventory mutations and business transactions

## 🗺️ Roadmap

### Phase 1: Foundation ✅
- [x] Core inventory management
- [x] Basic project tracking  
- [x] Quotation system
- [x] Authentication & authorization

### Phase 2: Manufacturing (In Progress)
- [ ] Advanced work order management
- [ ] Shop floor integration
- [ ] Quality management system
- [ ] Advanced reporting

### Phase 3: ERP Evolution
- [ ] Financial integration
- [ ] Purchase order automation
- [ ] Advanced MRP capabilities
- [ ] CRM features

### Phase 4: Analytics & AI
- [ ] Predictive analytics
- [ ] Demand forecasting
- [ ] Optimization algorithms
- [ ] Business intelligence

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

### Pull Request Checklist

- [ ] Story acceptance criteria met
- [ ] EF migration applied locally
- [ ] Unit/integration tests pass
- [ ] Telemetry added (log scopes, event names)
- [ ] Documentation updated

## 📄 License

This project is proprietary software developed for MetalProjekts. All rights reserved.

## 📞 Support

For technical support or questions:

- **Project Owner**: Peterson Matiss
- **Repository**: [petersonmatiss/MPM](https://github.com/petersonmatiss/MPM)
- **Issues**: GitHub Issues for bug reports and feature requests

---

**Built with ❤️ in Latvia for the steel fabrication industry**