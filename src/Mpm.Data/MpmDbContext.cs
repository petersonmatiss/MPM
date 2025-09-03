using Microsoft.EntityFrameworkCore;
using Mpm.Domain.Entities;

namespace Mpm.Data;

public class MpmDbContext : DbContext
{
    public MpmDbContext(DbContextOptions<MpmDbContext> options) : base(options)
    {
    }

    public string TenantId { get; set; } = string.Empty;

    // Core entities
    public DbSet<Customer> Customers { get; set; }
    public DbSet<CustomerContact> CustomerContacts { get; set; }
    public DbSet<Project> Projects { get; set; }
    public DbSet<BillOfMaterial> BillOfMaterials { get; set; }
    public DbSet<BomItem> BomItems { get; set; }
    
    // Material and inventory
    public DbSet<Material> Materials { get; set; }
    public DbSet<Supplier> Suppliers { get; set; }
    public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
    public DbSet<PurchaseOrderLine> PurchaseOrderLines { get; set; }
    public DbSet<GoodsReceiptNote> GoodsReceiptNotes { get; set; }
    public DbSet<GoodsReceiptNoteLine> GoodsReceiptNoteLines { get; set; }
    public DbSet<InventoryLot> InventoryLots { get; set; }
    public DbSet<MaterialReservation> MaterialReservations { get; set; }
    public DbSet<Certificate> Certificates { get; set; }
    
    // Work orders and operations
    public DbSet<WorkOrder> WorkOrders { get; set; }
    public DbSet<WorkOrderOperation> WorkOrderOperations { get; set; }
    public DbSet<NonConformanceReport> NonConformanceReports { get; set; }
    public DbSet<NCRPhoto> NCRPhotos { get; set; }
    
    // Quality and quotations
    public DbSet<DeclarationOfPerformance> DeclarationsOfPerformance { get; set; }
    public DbSet<DoPMaterial> DoPMaterials { get; set; }
    public DbSet<DoPHeat> DoPHeats { get; set; }
    public DbSet<Quotation> Quotations { get; set; }
    public DbSet<QuotationLine> QuotationLines { get; set; }
    
    // New entities for MPM
    public DbSet<Invoice> Invoices { get; set; }
    public DbSet<InvoiceLine> InvoiceLines { get; set; }
    public DbSet<Sheet> Sheets { get; set; }
    public DbSet<Profile> Profiles { get; set; }
    public DbSet<ProfileRemnant> ProfileRemnants { get; set; }
    public DbSet<SteelGrade> SteelGrades { get; set; }
    public DbSet<ProfileType> ProfileTypes { get; set; }
    public DbSet<ManufacturingOrder> ManufacturingOrders { get; set; }
    public DbSet<MoDrawing> MoDrawings { get; set; }
    public DbSet<SheetUsage> SheetUsages { get; set; }
    public DbSet<ProfileUsage> ProfileUsages { get; set; }
    public DbSet<TimeLog> TimeLogs { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    
    // Procurement entities
    public DbSet<PriceRequest> PriceRequests { get; set; }
    public DbSet<PriceRequestLine> PriceRequestLines { get; set; }
    public DbSet<PriceRequestSupplier> PriceRequestSuppliers { get; set; }
    public DbSet<PriceRequestQuote> PriceRequestQuotes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Apply tenant filter to all tenant entities and soft delete filter
        modelBuilder.Entity<Customer>().HasQueryFilter(e => e.TenantId == TenantId && !e.IsDeleted);
        modelBuilder.Entity<CustomerContact>().HasQueryFilter(e => e.TenantId == TenantId && !e.IsDeleted);
        modelBuilder.Entity<Project>().HasQueryFilter(e => e.TenantId == TenantId && !e.IsDeleted);
        modelBuilder.Entity<BillOfMaterial>().HasQueryFilter(e => e.TenantId == TenantId && !e.IsDeleted);
        modelBuilder.Entity<BomItem>().HasQueryFilter(e => e.TenantId == TenantId && !e.IsDeleted);
        modelBuilder.Entity<Material>().HasQueryFilter(e => e.TenantId == TenantId && !e.IsDeleted);
        modelBuilder.Entity<Supplier>().HasQueryFilter(e => e.TenantId == TenantId && !e.IsDeleted);
        modelBuilder.Entity<PurchaseOrder>().HasQueryFilter(e => e.TenantId == TenantId && !e.IsDeleted);
        modelBuilder.Entity<PurchaseOrderLine>().HasQueryFilter(e => e.TenantId == TenantId && !e.IsDeleted);
        modelBuilder.Entity<GoodsReceiptNote>().HasQueryFilter(e => e.TenantId == TenantId && !e.IsDeleted);
        modelBuilder.Entity<GoodsReceiptNoteLine>().HasQueryFilter(e => e.TenantId == TenantId && !e.IsDeleted);
        modelBuilder.Entity<InventoryLot>().HasQueryFilter(e => e.TenantId == TenantId && !e.IsDeleted);
        modelBuilder.Entity<MaterialReservation>().HasQueryFilter(e => e.TenantId == TenantId && !e.IsDeleted);
        modelBuilder.Entity<Certificate>().HasQueryFilter(e => e.TenantId == TenantId && !e.IsDeleted);
        modelBuilder.Entity<WorkOrder>().HasQueryFilter(e => e.TenantId == TenantId && !e.IsDeleted);
        modelBuilder.Entity<WorkOrderOperation>().HasQueryFilter(e => e.TenantId == TenantId && !e.IsDeleted);
        modelBuilder.Entity<NonConformanceReport>().HasQueryFilter(e => e.TenantId == TenantId && !e.IsDeleted);
        modelBuilder.Entity<NCRPhoto>().HasQueryFilter(e => e.TenantId == TenantId && !e.IsDeleted);
        modelBuilder.Entity<DeclarationOfPerformance>().HasQueryFilter(e => e.TenantId == TenantId && !e.IsDeleted);
        modelBuilder.Entity<DoPMaterial>().HasQueryFilter(e => e.TenantId == TenantId && !e.IsDeleted);
        modelBuilder.Entity<DoPHeat>().HasQueryFilter(e => e.TenantId == TenantId && !e.IsDeleted);
        modelBuilder.Entity<Quotation>().HasQueryFilter(e => e.TenantId == TenantId && !e.IsDeleted);
        modelBuilder.Entity<QuotationLine>().HasQueryFilter(e => e.TenantId == TenantId && !e.IsDeleted);
        
        // New entities query filters
        modelBuilder.Entity<Invoice>().HasQueryFilter(e => e.TenantId == TenantId && !e.IsDeleted);
        modelBuilder.Entity<InvoiceLine>().HasQueryFilter(e => e.TenantId == TenantId && !e.IsDeleted);
        modelBuilder.Entity<Sheet>().HasQueryFilter(e => e.TenantId == TenantId && !e.IsDeleted);
        modelBuilder.Entity<Profile>().HasQueryFilter(e => e.TenantId == TenantId && !e.IsDeleted);
        modelBuilder.Entity<ProfileRemnant>().HasQueryFilter(e => e.TenantId == TenantId && !e.IsDeleted);
        modelBuilder.Entity<SteelGrade>().HasQueryFilter(e => e.TenantId == TenantId && !e.IsDeleted);
        modelBuilder.Entity<ProfileType>().HasQueryFilter(e => e.TenantId == TenantId && !e.IsDeleted);
        modelBuilder.Entity<ManufacturingOrder>().HasQueryFilter(e => e.TenantId == TenantId && !e.IsDeleted);
        modelBuilder.Entity<MoDrawing>().HasQueryFilter(e => e.TenantId == TenantId && !e.IsDeleted);
        modelBuilder.Entity<SheetUsage>().HasQueryFilter(e => e.TenantId == TenantId && !e.IsDeleted);
        modelBuilder.Entity<ProfileUsage>().HasQueryFilter(e => e.TenantId == TenantId && !e.IsDeleted);
        modelBuilder.Entity<TimeLog>().HasQueryFilter(e => e.TenantId == TenantId && !e.IsDeleted);
        modelBuilder.Entity<Notification>().HasQueryFilter(e => e.TenantId == TenantId && !e.IsDeleted);
        
        // Procurement entities query filters
        modelBuilder.Entity<PriceRequest>().HasQueryFilter(e => e.TenantId == TenantId && !e.IsDeleted);
        modelBuilder.Entity<PriceRequestLine>().HasQueryFilter(e => e.TenantId == TenantId && !e.IsDeleted);
        modelBuilder.Entity<PriceRequestSupplier>().HasQueryFilter(e => e.TenantId == TenantId && !e.IsDeleted);
        modelBuilder.Entity<PriceRequestQuote>().HasQueryFilter(e => e.TenantId == TenantId && !e.IsDeleted);

        // Configure precision for decimal properties
        modelBuilder.Entity<BomItem>()
            .Property(e => e.Quantity)
            .HasPrecision(18, 4);
        
        modelBuilder.Entity<BomItem>()
            .Property(e => e.Weight)
            .HasPrecision(18, 4);
            
        modelBuilder.Entity<Material>()
            .Property(e => e.UnitWeight)
            .HasPrecision(18, 4);

        // Configure new entities
        ConfigureNewEntities(modelBuilder);
            
        // Configure string lengths
        modelBuilder.Entity<Customer>()
            .Property(e => e.Name)
            .HasMaxLength(200);
            
        modelBuilder.Entity<Project>()
            .Property(e => e.Code)
            .HasMaxLength(50);
            
        modelBuilder.Entity<Project>()
            .Property(e => e.Name)
            .HasMaxLength(200);

        // Configure indexes for performance
        modelBuilder.Entity<Customer>()
            .HasIndex(e => new { e.TenantId, e.Name });
            
        modelBuilder.Entity<Project>()
            .HasIndex(e => new { e.TenantId, e.Code })
            .IsUnique();
            
        modelBuilder.Entity<Material>()
            .HasIndex(e => new { e.TenantId, e.Grade, e.Dimension });
    }

    public override int SaveChanges()
    {
        SetTenantId();
        SetTimestamps();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SetTenantId();
        SetTimestamps();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void SetTenantId()
    {
        var entries = ChangeTracker.Entries<TenantEntity>()
            .Where(e => e.State == EntityState.Added);

        foreach (var entry in entries)
        {
            entry.Entity.TenantId = TenantId;
        }
    }

    private void SetTimestamps()
    {
        var entries = ChangeTracker.Entries<BaseEntity>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
            }
            entry.Entity.UpdatedAt = DateTime.UtcNow;
        }
    }

    private void ConfigureNewEntities(ModelBuilder modelBuilder)
    {
        // Configure concurrency tokens
        modelBuilder.Entity<Invoice>().Property(e => e.RowVersion).IsRowVersion();
        modelBuilder.Entity<InvoiceLine>().Property(e => e.RowVersion).IsRowVersion();
        modelBuilder.Entity<Sheet>().Property(e => e.RowVersion).IsRowVersion();
        modelBuilder.Entity<Profile>().Property(e => e.RowVersion).IsRowVersion();
        modelBuilder.Entity<ProfileRemnant>().Property(e => e.RowVersion).IsRowVersion();
        modelBuilder.Entity<SteelGrade>().Property(e => e.RowVersion).IsRowVersion();
        modelBuilder.Entity<ProfileType>().Property(e => e.RowVersion).IsRowVersion();
        modelBuilder.Entity<ManufacturingOrder>().Property(e => e.RowVersion).IsRowVersion();
        modelBuilder.Entity<MoDrawing>().Property(e => e.RowVersion).IsRowVersion();
        modelBuilder.Entity<SheetUsage>().Property(e => e.RowVersion).IsRowVersion();
        modelBuilder.Entity<ProfileUsage>().Property(e => e.RowVersion).IsRowVersion();
        modelBuilder.Entity<TimeLog>().Property(e => e.RowVersion).IsRowVersion();
        modelBuilder.Entity<Notification>().Property(e => e.RowVersion).IsRowVersion();

        // Configure string lengths
        modelBuilder.Entity<Invoice>()
            .Property(e => e.Number)
            .HasMaxLength(50);
        modelBuilder.Entity<Invoice>()
            .Property(e => e.Currency)
            .HasMaxLength(3);
        
        modelBuilder.Entity<Sheet>()
            .Property(e => e.SheetId)
            .HasMaxLength(50);
        modelBuilder.Entity<Sheet>()
            .Property(e => e.Grade)
            .HasMaxLength(50);
        modelBuilder.Entity<Sheet>()
            .Property(e => e.HeatNumber)
            .HasMaxLength(50);
            
        modelBuilder.Entity<Profile>()
            .Property(e => e.LotId)
            .HasMaxLength(10);
        modelBuilder.Entity<Profile>()
            .Property(e => e.HeatNumber)
            .HasMaxLength(50);
            
        modelBuilder.Entity<SteelGrade>()
            .Property(e => e.Code)
            .HasMaxLength(20);
        modelBuilder.Entity<SteelGrade>()
            .Property(e => e.Name)
            .HasMaxLength(100);
        modelBuilder.Entity<SteelGrade>()
            .Property(e => e.Standard)
            .HasMaxLength(20);
            
        modelBuilder.Entity<ProfileType>()
            .Property(e => e.Code)
            .HasMaxLength(20);
        modelBuilder.Entity<ProfileType>()
            .Property(e => e.Name)
            .HasMaxLength(100);
        modelBuilder.Entity<ProfileType>()
            .Property(e => e.Category)
            .HasMaxLength(50);

        // Configure decimal precision
        modelBuilder.Entity<Invoice>()
            .Property(e => e.SubTotal)
            .HasPrecision(18, 2);
        modelBuilder.Entity<Invoice>()
            .Property(e => e.TaxAmount)
            .HasPrecision(18, 2);
        modelBuilder.Entity<Invoice>()
            .Property(e => e.TotalAmount)
            .HasPrecision(18, 2);
            
        modelBuilder.Entity<InvoiceLine>()
            .Property(e => e.Quantity)
            .HasPrecision(18, 4);
        modelBuilder.Entity<InvoiceLine>()
            .Property(e => e.UnitPrice)
            .HasPrecision(18, 4);
        modelBuilder.Entity<InvoiceLine>()
            .Property(e => e.TotalPrice)
            .HasPrecision(18, 2);
            
        modelBuilder.Entity<Sheet>()
            .Property(e => e.Weight)
            .HasPrecision(18, 4);
        modelBuilder.Entity<Sheet>()
            .Property(e => e.UnitPrice)
            .HasPrecision(18, 4);
            
        modelBuilder.Entity<Profile>()
            .Property(e => e.Weight)
            .HasPrecision(18, 4);
        modelBuilder.Entity<Profile>()
            .Property(e => e.UnitPrice)
            .HasPrecision(18, 4);
            
        modelBuilder.Entity<ProfileRemnant>()
            .Property(e => e.Weight)
            .HasPrecision(18, 4);
            
        modelBuilder.Entity<SteelGrade>()
            .Property(e => e.DensityKgPerM3)
            .HasPrecision(18, 2);
        modelBuilder.Entity<SteelGrade>()
            .Property(e => e.YieldStrengthMPa)
            .HasPrecision(18, 2);
        modelBuilder.Entity<SteelGrade>()
            .Property(e => e.TensileStrengthMPa)
            .HasPrecision(18, 2);
            
        modelBuilder.Entity<ProfileType>()
            .Property(e => e.StandardWeight)
            .HasPrecision(18, 4);
            
        modelBuilder.Entity<ManufacturingOrder>()
            .Property(e => e.EstimatedHours)
            .HasPrecision(18, 2);
        modelBuilder.Entity<ManufacturingOrder>()
            .Property(e => e.ActualHours)
            .HasPrecision(18, 2);
            
        modelBuilder.Entity<SheetUsage>()
            .Property(e => e.AreaUsed)
            .HasPrecision(18, 2);
            
        modelBuilder.Entity<TimeLog>()
            .Property(e => e.HoursWorked)
            .HasPrecision(18, 2);

        // Configure unique indexes
        modelBuilder.Entity<Invoice>()
            .HasIndex(e => new { e.TenantId, e.Number })
            .IsUnique();
            
        modelBuilder.Entity<Sheet>()
            .HasIndex(e => new { e.TenantId, e.SheetId })
            .IsUnique();
            
        modelBuilder.Entity<Profile>()
            .HasIndex(e => new { e.TenantId, e.LotId })
            .IsUnique();
            
        modelBuilder.Entity<SteelGrade>()
            .HasIndex(e => new { e.TenantId, e.Code })
            .IsUnique();
            
        modelBuilder.Entity<ProfileType>()
            .HasIndex(e => new { e.TenantId, e.Code })
            .IsUnique();
            
        modelBuilder.Entity<ManufacturingOrder>()
            .HasIndex(e => new { e.TenantId, e.Number })
            .IsUnique();

        // Configure performance indexes
        modelBuilder.Entity<Invoice>()
            .HasIndex(e => new { e.TenantId, e.SupplierId, e.InvoiceDate });
            
        modelBuilder.Entity<Sheet>()
            .HasIndex(e => new { e.TenantId, e.ProjectId, e.IsUsed });
            
        modelBuilder.Entity<Profile>()
            .HasIndex(e => new { e.TenantId, e.ProjectId, e.ProfileTypeId });
            
        modelBuilder.Entity<SheetUsage>()
            .HasIndex(e => new { e.TenantId, e.ProjectId, e.UsageDate });
            
        modelBuilder.Entity<ProfileUsage>()
            .HasIndex(e => new { e.TenantId, e.ProjectId, e.UsageDate });
            
        modelBuilder.Entity<TimeLog>()
            .HasIndex(e => new { e.TenantId, e.ProjectId, e.ClockInTime });
            
        modelBuilder.Entity<Notification>()
            .HasIndex(e => new { e.TenantId, e.RecipientUserId, e.IsRead });

        // Configure relationships
        modelBuilder.Entity<Invoice>()
            .HasOne(e => e.Supplier)
            .WithMany()
            .HasForeignKey(e => e.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);
            
        modelBuilder.Entity<InvoiceLine>()
            .HasOne(e => e.Invoice)
            .WithMany(e => e.Lines)
            .HasForeignKey(e => e.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);
            
        modelBuilder.Entity<Sheet>()
            .HasOne(e => e.InvoiceLine)
            .WithMany()
            .HasForeignKey(e => e.InvoiceLineId)
            .OnDelete(DeleteBehavior.SetNull);
            
        modelBuilder.Entity<Sheet>()
            .HasOne(e => e.Project)
            .WithMany()
            .HasForeignKey(e => e.ProjectId)
            .OnDelete(DeleteBehavior.SetNull);
            
        modelBuilder.Entity<Profile>()
            .HasOne(e => e.InvoiceLine)
            .WithMany()
            .HasForeignKey(e => e.InvoiceLineId)
            .OnDelete(DeleteBehavior.SetNull);
            
        modelBuilder.Entity<Profile>()
            .HasOne(e => e.Project)
            .WithMany()
            .HasForeignKey(e => e.ProjectId)
            .OnDelete(DeleteBehavior.SetNull);
            
        modelBuilder.Entity<Profile>()
            .HasOne(e => e.SteelGrade)
            .WithMany(e => e.Profiles)
            .HasForeignKey(e => e.SteelGradeId)
            .OnDelete(DeleteBehavior.SetNull);
            
        modelBuilder.Entity<Profile>()
            .HasOne(e => e.ProfileType)
            .WithMany(e => e.Profiles)
            .HasForeignKey(e => e.ProfileTypeId)
            .OnDelete(DeleteBehavior.SetNull);
            
        modelBuilder.Entity<ProfileRemnant>()
            .HasOne(e => e.Profile)
            .WithMany(e => e.Remnants)
            .HasForeignKey(e => e.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);
            
        modelBuilder.Entity<ManufacturingOrder>()
            .HasOne(e => e.Project)
            .WithMany()
            .HasForeignKey(e => e.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);
            
        modelBuilder.Entity<MoDrawing>()
            .HasOne(e => e.ManufacturingOrder)
            .WithMany(e => e.Drawings)
            .HasForeignKey(e => e.ManufacturingOrderId)
            .OnDelete(DeleteBehavior.Cascade);
            
        modelBuilder.Entity<SheetUsage>()
            .HasOne(e => e.Sheet)
            .WithMany(e => e.Usages)
            .HasForeignKey(e => e.SheetId)
            .OnDelete(DeleteBehavior.Restrict);
            
        modelBuilder.Entity<ProfileUsage>()
            .HasOne(e => e.Profile)
            .WithMany(e => e.Usages)
            .HasForeignKey(e => e.ProfileId)
            .OnDelete(DeleteBehavior.Restrict);
            
        modelBuilder.Entity<ProfileUsage>()
            .HasOne(e => e.ProfileRemnant)
            .WithMany(e => e.Usages)
            .HasForeignKey(e => e.ProfileRemnantId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure procurement entities
        ConfigureProcurementEntities(modelBuilder);
    }

    private void ConfigureProcurementEntities(ModelBuilder modelBuilder)
    {
        // Configure concurrency tokens
        modelBuilder.Entity<PriceRequest>().Property(e => e.RowVersion).IsRowVersion();
        modelBuilder.Entity<PriceRequestLine>().Property(e => e.RowVersion).IsRowVersion();
        modelBuilder.Entity<PriceRequestSupplier>().Property(e => e.RowVersion).IsRowVersion();
        modelBuilder.Entity<PriceRequestQuote>().Property(e => e.RowVersion).IsRowVersion();

        // Configure string lengths
        modelBuilder.Entity<PriceRequest>()
            .Property(e => e.Number)
            .HasMaxLength(50);
        modelBuilder.Entity<PriceRequest>()
            .Property(e => e.Description)
            .HasMaxLength(500);
        modelBuilder.Entity<PriceRequest>()
            .Property(e => e.Notes)
            .HasMaxLength(1000);
        modelBuilder.Entity<PriceRequest>()
            .Property(e => e.RequestedBy)
            .HasMaxLength(100);

        modelBuilder.Entity<PriceRequestLine>()
            .Property(e => e.Description)
            .HasMaxLength(200);
        modelBuilder.Entity<PriceRequestLine>()
            .Property(e => e.Dimension)
            .HasMaxLength(100);
        modelBuilder.Entity<PriceRequestLine>()
            .Property(e => e.Notes)
            .HasMaxLength(500);

        modelBuilder.Entity<PriceRequestSupplier>()
            .Property(e => e.Notes)
            .HasMaxLength(500);

        modelBuilder.Entity<PriceRequestQuote>()
            .Property(e => e.Currency)
            .HasMaxLength(3);
        modelBuilder.Entity<PriceRequestQuote>()
            .Property(e => e.Notes)
            .HasMaxLength(500);

        // Configure decimal precision
        modelBuilder.Entity<PriceRequestLine>()
            .Property(e => e.TotalLength)
            .HasPrecision(18, 2);

        modelBuilder.Entity<PriceRequestQuote>()
            .Property(e => e.UnitPrice)
            .HasPrecision(18, 4);

        // Configure unique indexes
        modelBuilder.Entity<PriceRequest>()
            .HasIndex(e => new { e.TenantId, e.Number })
            .IsUnique();

        // Configure performance indexes
        modelBuilder.Entity<PriceRequest>()
            .HasIndex(e => new { e.TenantId, e.Status, e.RequestDate });

        modelBuilder.Entity<PriceRequestLine>()
            .HasIndex(e => new { e.TenantId, e.PriceRequestId, e.MaterialType });

        modelBuilder.Entity<PriceRequestSupplier>()
            .HasIndex(e => new { e.TenantId, e.PriceRequestId, e.SupplierId })
            .IsUnique();

        modelBuilder.Entity<PriceRequestQuote>()
            .HasIndex(e => new { e.TenantId, e.PriceRequestLineId, e.SupplierId })
            .IsUnique();

        // Configure relationships
        modelBuilder.Entity<PriceRequestLine>()
            .HasOne(e => e.PriceRequest)
            .WithMany(e => e.Lines)
            .HasForeignKey(e => e.PriceRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PriceRequestLine>()
            .HasOne(e => e.SteelGrade)
            .WithMany()
            .HasForeignKey(e => e.SteelGradeId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<PriceRequestLine>()
            .HasOne(e => e.ProfileType)
            .WithMany(e => e.PriceRequestLines)
            .HasForeignKey(e => e.ProfileTypeId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<PriceRequestSupplier>()
            .HasOne(e => e.PriceRequest)
            .WithMany(e => e.Suppliers)
            .HasForeignKey(e => e.PriceRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PriceRequestSupplier>()
            .HasOne(e => e.Supplier)
            .WithMany()
            .HasForeignKey(e => e.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PriceRequestQuote>()
            .HasOne(e => e.PriceRequestLine)
            .WithMany(e => e.Quotes)
            .HasForeignKey(e => e.PriceRequestLineId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PriceRequestQuote>()
            .HasOne(e => e.Supplier)
            .WithMany()
            .HasForeignKey(e => e.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
