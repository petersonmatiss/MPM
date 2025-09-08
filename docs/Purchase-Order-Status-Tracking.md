# Purchase Order Status Tracking

This document describes the new Purchase Order status tracking functionality implemented in MPM.

## Overview

The Purchase Order system now includes comprehensive status tracking that allows users to monitor the progress of orders from initial draft through to completion.

## Status Types

The following status types are available for purchase orders:

| Status | Description |
|--------|-------------|
| **Draft** | Initial state when a purchase order is created but not yet sent |
| **Sent** | Order has been sent to the supplier |
| **Acknowledged** | Supplier has acknowledged receipt of the order |
| **In Production** | Supplier has started production |
| **Shipped** | Order has been shipped by the supplier |
| **Received** | Goods have been received and verified |
| **Cancelled** | Order has been cancelled |

## Key Features

### Status Management
- **Automatic Status Updates**: The system automatically updates the `IsConfirmed` flag based on status changes
- **Send to Supplier**: New action button to quickly move orders from Draft to Sent status
- **Sent Date Tracking**: Automatically records when orders are sent to suppliers

### UI Enhancements
- **Status Column**: Visual status indicators using color-coded chips
- **Sent Date Column**: Displays when orders were sent to suppliers
- **Status Dropdown**: Easy status selection in the create/edit dialog
- **Updated Summary Cards**: Shows counts by status instead of just confirmed/pending

### Document and Communication Tracking
The system includes new entities for comprehensive order tracking:

#### Purchase Order Documents
- File attachments (contracts, specifications, etc.)
- Document type classification
- Upload tracking (who uploaded, when)
- File metadata (size, content type)

#### Purchase Order Communications
- Communication records (emails, phone calls, meetings)
- Direction tracking (inbound/outbound)
- Contact person details
- Importance flagging

## Database Changes

### New Entities
- `PurchaseOrderDocument`: For file attachments
- `PurchaseOrderCommunication`: For communication records

### Enhanced PurchaseOrder Entity
- Added `Status` property (PurchaseOrderStatus enum)
- Added `SentDate` property
- Added navigation properties for Documents and Communications

## API/Service Methods

### Status Management
- `UpdateStatusAsync(int id, PurchaseOrderStatus status)`: Update order status
- `SendToSupplierAsync(int id)`: Mark order as sent to supplier

### Document Management
- `AddDocumentAsync(PurchaseOrderDocument document)`: Add document
- `GetDocumentsAsync(int purchaseOrderId)`: Get all documents for an order
- `DeleteDocumentAsync(int documentId)`: Remove document

### Communication Management
- `AddCommunicationAsync(PurchaseOrderCommunication communication)`: Add communication record
- `GetCommunicationsAsync(int purchaseOrderId)`: Get all communications for an order
- `UpdateCommunicationAsync(PurchaseOrderCommunication communication)`: Update communication

## Backward Compatibility

The implementation maintains backward compatibility:
- The existing `IsConfirmed` boolean flag is preserved
- `IsConfirmed` is automatically updated based on status changes
- Existing confirmed orders will work as before

## Testing

The implementation includes comprehensive unit tests covering:
- Status transitions
- Document management
- Communication tracking
- Service method functionality
- Backward compatibility scenarios

All tests are located in `tests/MPM.Tests/PurchaseOrderServiceTests.cs`.