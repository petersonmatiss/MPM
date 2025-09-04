# MPM Entity Relationship Diagram

This directory contains the Entity Relationship Diagram (ERD) for the MPM (MetalProjekts Management) system.

## Files

- `mpm-erd.puml` - PlantUML source file for the ERD
- `mpm-erd.png` - Generated PNG diagram
- `mpm-erd.vsdx` - Visio format (placeholder - use PlantUML source for actual diagram)

## Overview

The MPM system manages steel inventory, projects, quotations, manufacturing orders, and quality documentation. The ERD shows all entities and their relationships.

## Key Entity Groups

### Customer Management
- **Customer** - Main customer entity
- **CustomerContact** - Customer contact persons

### Project Management  
- **Project** - Main project entity
- **BillOfMaterial** - Project BOMs
- **BomItem** - Individual BOM items

### Material & Procurement
- **Material** - Material definitions
- **Supplier** - Supplier information
- **PurchaseOrder** / **PurchaseOrderLine** - Purchase orders
- **GoodsReceiptNote** / **GoodsReceiptNoteLine** - Goods receipts

### Inventory Management
- **InventoryLot** - Physical inventory lots
- **MaterialReservation** - Material reservations
- **Certificate** - Material certificates

### Work Management
- **WorkOrder** - Manufacturing work orders
- **WorkOrderOperation** - Time tracking operations

### Quality Management
- **NonConformanceReport** / **NCRPhoto** - Quality issues
- **DeclarationOfPerformance** / **DoPMaterial** / **DoPHeat** - CE compliance

### Quotation & Pricing Management
- **Quotation** / **QuotationLine** - Customer quotations
- **PriceRequest** / **PriceRequestLine** - Price request workflow management

## Indices and Performance

All entities inherit from TenantEntity which provides:
- Primary key `Id` (indexed)
- Audit fields `CreatedAt`, `UpdatedAt` (indexed) 
- Tenant isolation `TenantId` (indexed)

Additional indices are placed on:
- Foreign keys (ProjectId, CustomerId, etc.)
- Date fields used for filtering
- Invoice/order numbers
- Business identifiers

## Relationships and Cardinality

- Customer 1:N CustomerContact
- Customer 1:N Project  
- Customer 1:N Quotation
- Project 1:N BillOfMaterial
- Project 1:N WorkOrder
- Project 1:N DeclarationOfPerformance
- Project 0:N NonConformanceReport
- Supplier 1:N PurchaseOrder
- PurchaseOrder 1:N PurchaseOrderLine
- PurchaseOrder 1:N GoodsReceiptNote
- Material 1:N InventoryLot
- InventoryLot 1:N MaterialReservation
- WorkOrder 1:N WorkOrderOperation
- PriceRequest 1:N PriceRequestLine

## Entity Inheritance

All business entities inherit from `TenantEntity`:
```
BaseEntity
├── Id (PK)
├── CreatedAt  
├── UpdatedAt
├── CreatedBy
└── UpdatedBy

TenantEntity : BaseEntity
└── TenantId
```

This provides multi-tenancy support and audit trail capabilities.