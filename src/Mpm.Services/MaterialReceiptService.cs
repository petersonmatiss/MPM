using Mpm.Data;
using Mpm.Domain.Entities;
using Mpm.Services.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Mpm.Services;

public class MaterialReceiptService : IMaterialReceiptService
{
    private readonly MpmDbContext _context;

    public MaterialReceiptService(MpmDbContext context)
    {
        _context = context;
    }

    public async Task<MaterialReceiptDto> CreateReceiptAsync(CreateMaterialReceiptDto dto)
    {
        // Use transaction only if supported (not in-memory database)
        var useTransaction = _context.Database.IsRelational();
        var transaction = useTransaction ? await _context.Database.BeginTransactionAsync() : null;
        
        try
        {
            // Validate purchase order exists
            var purchaseOrder = await _context.PurchaseOrders
                .Include(p => p.Lines)
                .ThenInclude(l => l.Material)
                .FirstOrDefaultAsync(p => p.Id == dto.PurchaseOrderId);

            if (purchaseOrder == null)
                throw new ArgumentException($"Purchase order with ID {dto.PurchaseOrderId} not found");

            // Create goods receipt note
            var grn = new GoodsReceiptNote
            {
                Number = dto.Number,
                PurchaseOrderId = dto.PurchaseOrderId,
                ReceiptDate = dto.ReceiptDate,
                DeliveryNoteNumber = dto.DeliveryNoteNumber,
                ReceivedBy = dto.ReceivedBy,
                Comments = dto.Comments,
                InvoiceNumber = dto.InvoiceNumber,
                PaymentTerms = dto.PaymentTerms,
                IsPartialDelivery = dto.IsPartialDelivery
            };

            _context.GoodsReceiptNotes.Add(grn);
            await _context.SaveChangesAsync();

            // Create receipt lines and inventory lots
            foreach (var lineDto in dto.Lines)
            {
                var poLine = purchaseOrder.Lines.FirstOrDefault(l => l.Id == lineDto.PurchaseOrderLineId);
                if (poLine == null)
                    throw new ArgumentException($"Purchase order line with ID {lineDto.PurchaseOrderLineId} not found");

                // Calculate quantity deviation
                var quantityDeviation = lineDto.ReceivedQuantity - poLine.Quantity;

                var grnLine = new GoodsReceiptNoteLine
                {
                    GoodsReceiptNoteId = grn.Id,
                    PurchaseOrderLineId = lineDto.PurchaseOrderLineId,
                    ReceivedQuantity = lineDto.ReceivedQuantity,
                    UnitOfMeasure = lineDto.UnitOfMeasure,
                    HeatNumber = lineDto.HeatNumber,
                    Notes = lineDto.Notes,
                    ActualUnitPrice = lineDto.ActualUnitPrice > 0 ? lineDto.ActualUnitPrice : poLine.UnitPrice,
                    QuantityDeviation = quantityDeviation,
                    DeviationReason = lineDto.DeviationReason
                };

                _context.GoodsReceiptNoteLines.Add(grnLine);
                await _context.SaveChangesAsync();

                // Create inventory lot if requested
                if (lineDto.CreateInventoryLot)
                {
                    var inventoryLot = new InventoryLot
                    {
                        MaterialId = poLine.MaterialId,
                        GoodsReceiptNoteLineId = grnLine.Id,
                        Quantity = lineDto.ReceivedQuantity,
                        HeatNumber = lineDto.HeatNumber,
                        ProfileType = poLine.ProfileType,
                        Location = lineDto.Location,
                        ArrivalDate = dto.ReceiptDate,
                        SupplierName = purchaseOrder.Supplier?.Name ?? "",
                        InvoiceNumber = dto.InvoiceNumber,
                        UnitPrice = grnLine.ActualUnitPrice
                    };

                    _context.InventoryLots.Add(inventoryLot);
                    await _context.SaveChangesAsync();

                    // Create audit log for inventory creation
                    await CreateAuditLogAsync(inventoryLot.Id, "Created", null, inventoryLot.Quantity, 
                        "Created from goods receipt", grn.Number);
                }
            }

            if (useTransaction && transaction != null)
                await transaction.CommitAsync();

            // Return created receipt
            return await GetReceiptByIdAsync(grn.Id);
        }
        catch
        {
            if (useTransaction && transaction != null)
                await transaction.RollbackAsync();
            throw;
        }
        finally
        {
            transaction?.Dispose();
        }
    }

    public async Task<MaterialReceiptDto> GetReceiptByIdAsync(int id)
    {
        var grn = await _context.GoodsReceiptNotes
            .Include(g => g.PurchaseOrder)
            .Include(g => g.Lines)
                .ThenInclude(l => l.PurchaseOrderLine)
                .ThenInclude(p => p.Material)
            .Include(g => g.Lines)
                .ThenInclude(l => l.InventoryLots)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (grn == null)
            throw new ArgumentException($"Goods receipt note with ID {id} not found");

        return new MaterialReceiptDto
        {
            Id = grn.Id,
            Number = grn.Number,
            PurchaseOrderId = grn.PurchaseOrderId,
            PurchaseOrderNumber = grn.PurchaseOrder.Number,
            ReceiptDate = grn.ReceiptDate,
            DeliveryNoteNumber = grn.DeliveryNoteNumber,
            ReceivedBy = grn.ReceivedBy,
            Comments = grn.Comments,
            InvoiceNumber = grn.InvoiceNumber,
            PaymentTerms = grn.PaymentTerms,
            IsPartialDelivery = grn.IsPartialDelivery,
            CreatedAt = grn.CreatedAt,
            CreatedBy = grn.CreatedBy,
            Lines = grn.Lines.Select(l => new MaterialReceiptLineDto
            {
                Id = l.Id,
                PurchaseOrderLineId = l.PurchaseOrderLineId,
                MaterialDescription = l.PurchaseOrderLine.Material.Description,
                MaterialGrade = l.PurchaseOrderLine.Material.Grade,
                MaterialDimension = l.PurchaseOrderLine.Material.Dimension,
                OrderedQuantity = l.PurchaseOrderLine.Quantity,
                ReceivedQuantity = l.ReceivedQuantity,
                UnitOfMeasure = l.UnitOfMeasure,
                HeatNumber = l.HeatNumber,
                Notes = l.Notes,
                ActualUnitPrice = l.ActualUnitPrice,
                OriginalUnitPrice = l.PurchaseOrderLine.UnitPrice,
                QuantityDeviation = l.QuantityDeviation,
                DeviationReason = l.DeviationReason,
                InventoryLots = l.InventoryLots.Select(i => new InventoryLotSummaryDto
                {
                    Id = i.Id,
                    Quantity = i.Quantity,
                    Length = i.Length,
                    Location = i.Location,
                    IsReserved = i.IsReserved,
                    CreatedAt = i.CreatedAt
                }).ToList()
            }).ToList()
        };
    }

    public async Task<IEnumerable<MaterialReceiptDto>> GetReceiptsAsync(MaterialReceiptSearchDto searchDto)
    {
        var query = _context.GoodsReceiptNotes
            .Include(g => g.PurchaseOrder)
            .Include(g => g.Lines)
                .ThenInclude(l => l.PurchaseOrderLine)
                .ThenInclude(p => p.Material)
            .AsQueryable();

        if (searchDto.PurchaseOrderId.HasValue)
            query = query.Where(g => g.PurchaseOrderId == searchDto.PurchaseOrderId.Value);

        if (searchDto.FromDate.HasValue)
            query = query.Where(g => g.ReceiptDate >= searchDto.FromDate.Value);

        if (searchDto.ToDate.HasValue)
            query = query.Where(g => g.ReceiptDate <= searchDto.ToDate.Value);

        if (!string.IsNullOrEmpty(searchDto.ReceivedBy))
            query = query.Where(g => g.ReceivedBy.Contains(searchDto.ReceivedBy));

        if (!string.IsNullOrEmpty(searchDto.InvoiceNumber))
            query = query.Where(g => g.InvoiceNumber.Contains(searchDto.InvoiceNumber));

        if (searchDto.IsPartialDelivery.HasValue)
            query = query.Where(g => g.IsPartialDelivery == searchDto.IsPartialDelivery.Value);

        var totalCount = await query.CountAsync();
        var receipts = await query
            .OrderByDescending(g => g.ReceiptDate)
            .Skip((searchDto.Page - 1) * searchDto.PageSize)
            .Take(searchDto.PageSize)
            .ToListAsync();

        return receipts.Select(grn => new MaterialReceiptDto
        {
            Id = grn.Id,
            Number = grn.Number,
            PurchaseOrderId = grn.PurchaseOrderId,
            PurchaseOrderNumber = grn.PurchaseOrder.Number,
            ReceiptDate = grn.ReceiptDate,
            DeliveryNoteNumber = grn.DeliveryNoteNumber,
            ReceivedBy = grn.ReceivedBy,
            Comments = grn.Comments,
            InvoiceNumber = grn.InvoiceNumber,
            PaymentTerms = grn.PaymentTerms,
            IsPartialDelivery = grn.IsPartialDelivery,
            CreatedAt = grn.CreatedAt,
            CreatedBy = grn.CreatedBy,
            Lines = grn.Lines.Select(l => new MaterialReceiptLineDto
            {
                Id = l.Id,
                PurchaseOrderLineId = l.PurchaseOrderLineId,
                MaterialDescription = l.PurchaseOrderLine.Material.Description,
                MaterialGrade = l.PurchaseOrderLine.Material.Grade,
                MaterialDimension = l.PurchaseOrderLine.Material.Dimension,
                OrderedQuantity = l.PurchaseOrderLine.Quantity,
                ReceivedQuantity = l.ReceivedQuantity,
                UnitOfMeasure = l.UnitOfMeasure,
                HeatNumber = l.HeatNumber,
                Notes = l.Notes,
                ActualUnitPrice = l.ActualUnitPrice,
                OriginalUnitPrice = l.PurchaseOrderLine.UnitPrice,
                QuantityDeviation = l.QuantityDeviation,
                DeviationReason = l.DeviationReason
            }).ToList()
        });
    }

    public async Task<IEnumerable<MaterialReceiptDto>> GetReceiptsByPurchaseOrderAsync(int purchaseOrderId)
    {
        var searchDto = new MaterialReceiptSearchDto
        {
            PurchaseOrderId = purchaseOrderId,
            PageSize = 100 // Return all receipts for the PO
        };

        return await GetReceiptsAsync(searchDto);
    }

    public async Task<bool> DeleteReceiptAsync(int id)
    {
        var useTransaction = _context.Database.IsRelational();
        var transaction = useTransaction ? await _context.Database.BeginTransactionAsync() : null;
        
        try
        {
            var grn = await _context.GoodsReceiptNotes
                .Include(g => g.Lines)
                .ThenInclude(l => l.InventoryLots)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (grn == null)
                return false;

            // Check if any inventory lots are reserved
            var reservedLots = grn.Lines
                .SelectMany(l => l.InventoryLots)
                .Where(i => i.IsReserved)
                .ToList();

            if (reservedLots.Any())
                throw new InvalidOperationException("Cannot delete receipt: some inventory lots are reserved");

            // Delete inventory lots and audit logs
            foreach (var line in grn.Lines)
            {
                foreach (var lot in line.InventoryLots)
                {
                    // Create audit log for deletion
                    await CreateAuditLogAsync(lot.Id, "Deleted", lot.Quantity, null, 
                        "Deleted with goods receipt", grn.Number);
                    
                    // Delete related audit logs
                    var auditLogs = await _context.InventoryAuditLogs
                        .Where(a => a.InventoryLotId == lot.Id)
                        .ToListAsync();
                    _context.InventoryAuditLogs.RemoveRange(auditLogs);
                    
                    _context.InventoryLots.Remove(lot);
                }
            }

            // Delete receipt lines
            _context.GoodsReceiptNoteLines.RemoveRange(grn.Lines);

            // Delete receipt
            _context.GoodsReceiptNotes.Remove(grn);

            await _context.SaveChangesAsync();
            
            if (useTransaction && transaction != null)
                await transaction.CommitAsync();

            return true;
        }
        catch
        {
            if (useTransaction && transaction != null)
                await transaction.RollbackAsync();
            throw;
        }
        finally
        {
            transaction?.Dispose();
        }
    }

    public async Task<IEnumerable<InventoryAuditLogDto>> GetInventoryAuditLogAsync(int inventoryLotId)
    {
        var auditLogs = await _context.InventoryAuditLogs
            .Where(a => a.InventoryLotId == inventoryLotId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        return auditLogs.Select(a => new InventoryAuditLogDto
        {
            Id = a.Id,
            InventoryLotId = a.InventoryLotId,
            Action = a.Action,
            PreviousQuantity = a.PreviousQuantity,
            NewQuantity = a.NewQuantity,
            ChangeReason = a.ChangeReason,
            ReferenceDocument = a.ReferenceDocument,
            Details = a.Details,
            CreatedAt = a.CreatedAt,
            CreatedBy = a.CreatedBy
        });
    }

    public async Task<IEnumerable<InventoryAuditLogDto>> GetAllInventoryAuditLogsAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = _context.InventoryAuditLogs.AsQueryable();

        if (fromDate.HasValue)
            query = query.Where(a => a.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(a => a.CreatedAt <= toDate.Value);

        var auditLogs = await query
            .OrderByDescending(a => a.CreatedAt)
            .Take(1000) // Limit to prevent too much data
            .ToListAsync();

        return auditLogs.Select(a => new InventoryAuditLogDto
        {
            Id = a.Id,
            InventoryLotId = a.InventoryLotId,
            Action = a.Action,
            PreviousQuantity = a.PreviousQuantity,
            NewQuantity = a.NewQuantity,
            ChangeReason = a.ChangeReason,
            ReferenceDocument = a.ReferenceDocument,
            Details = a.Details,
            CreatedAt = a.CreatedAt,
            CreatedBy = a.CreatedBy
        });
    }

    private async Task CreateAuditLogAsync(int inventoryLotId, string action, decimal? previousQuantity, 
        decimal? newQuantity, string changeReason, string referenceDocument, string details = "")
    {
        var auditLog = new InventoryAuditLog
        {
            InventoryLotId = inventoryLotId,
            Action = action,
            PreviousQuantity = previousQuantity,
            NewQuantity = newQuantity,
            ChangeReason = changeReason,
            ReferenceDocument = referenceDocument,
            Details = details
        };

        _context.InventoryAuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();
    }
}