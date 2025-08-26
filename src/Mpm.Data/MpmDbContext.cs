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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Apply tenant filter to all tenant entities
        modelBuilder.Entity<Customer>().HasQueryFilter(e => e.TenantId == TenantId);
        modelBuilder.Entity<CustomerContact>().HasQueryFilter(e => e.TenantId == TenantId);
        modelBuilder.Entity<Project>().HasQueryFilter(e => e.TenantId == TenantId);
        modelBuilder.Entity<BillOfMaterial>().HasQueryFilter(e => e.TenantId == TenantId);
        modelBuilder.Entity<BomItem>().HasQueryFilter(e => e.TenantId == TenantId);
        modelBuilder.Entity<Material>().HasQueryFilter(e => e.TenantId == TenantId);
        modelBuilder.Entity<Supplier>().HasQueryFilter(e => e.TenantId == TenantId);
        modelBuilder.Entity<PurchaseOrder>().HasQueryFilter(e => e.TenantId == TenantId);
        modelBuilder.Entity<PurchaseOrderLine>().HasQueryFilter(e => e.TenantId == TenantId);
        modelBuilder.Entity<GoodsReceiptNote>().HasQueryFilter(e => e.TenantId == TenantId);
        modelBuilder.Entity<GoodsReceiptNoteLine>().HasQueryFilter(e => e.TenantId == TenantId);
        modelBuilder.Entity<InventoryLot>().HasQueryFilter(e => e.TenantId == TenantId);
        modelBuilder.Entity<MaterialReservation>().HasQueryFilter(e => e.TenantId == TenantId);
        modelBuilder.Entity<Certificate>().HasQueryFilter(e => e.TenantId == TenantId);
        modelBuilder.Entity<WorkOrder>().HasQueryFilter(e => e.TenantId == TenantId);
        modelBuilder.Entity<WorkOrderOperation>().HasQueryFilter(e => e.TenantId == TenantId);
        modelBuilder.Entity<NonConformanceReport>().HasQueryFilter(e => e.TenantId == TenantId);
        modelBuilder.Entity<NCRPhoto>().HasQueryFilter(e => e.TenantId == TenantId);
        modelBuilder.Entity<DeclarationOfPerformance>().HasQueryFilter(e => e.TenantId == TenantId);
        modelBuilder.Entity<DoPMaterial>().HasQueryFilter(e => e.TenantId == TenantId);
        modelBuilder.Entity<DoPHeat>().HasQueryFilter(e => e.TenantId == TenantId);
        modelBuilder.Entity<Quotation>().HasQueryFilter(e => e.TenantId == TenantId);
        modelBuilder.Entity<QuotationLine>().HasQueryFilter(e => e.TenantId == TenantId);

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
}
