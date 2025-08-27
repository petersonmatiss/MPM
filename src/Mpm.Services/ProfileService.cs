using Mpm.Data;
using Mpm.Domain.Entities;
using Mpm.Services.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Text.RegularExpressions;

namespace Mpm.Services;

public interface IProfileService
{
    Task<IEnumerable<Profile>> GetAllAsync(int? profileTypeId = null, int? steelGradeId = null, string? searchFilter = null);
    Task<Profile?> GetByIdAsync(int id);
    Task<Profile?> GetByLotIdAsync(string lotId);
    Task<Profile> CreateAsync(Profile profile);
    Task<Profile> UpdateAsync(Profile profile);
    Task DeleteAsync(int id);
    Task<bool> CanDeleteAsync(int id);
    Task<IEnumerable<Profile>> GetAvailableProfilesAsync(int? profileTypeId = null, int? steelGradeId = null);
    Task<IEnumerable<ProfileRemnant>> GetRemnantsAsync(int profileId);
    Task<ProfileUsage> UseProfileAsync(string lotId, ProfileUsageRequest request);
}

public class ProfileService : IProfileService
{
    private readonly MpmDbContext _context;
    private static readonly Regex LotIdPattern = new Regex(@"^[A-Z]\d+$", RegexOptions.Compiled);

    public ProfileService(MpmDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Profile>> GetAllAsync(int? profileTypeId = null, int? steelGradeId = null, string? searchFilter = null)
    {
        var query = _context.Profiles
            .Include(p => p.SteelGrade)
            .Include(p => p.ProfileType)
            .Include(p => p.InvoiceLine)
            .Include(p => p.Project)
            .AsQueryable();

        // Apply profile type filter
        if (profileTypeId.HasValue)
        {
            query = query.Where(p => p.ProfileTypeId == profileTypeId.Value);
        }

        // Apply steel grade filter
        if (steelGradeId.HasValue)
        {
            query = query.Where(p => p.SteelGradeId == steelGradeId.Value);
        }

        // Apply search filter (search across LotId, Dimension, HeatNumber)
        if (!string.IsNullOrWhiteSpace(searchFilter))
        {
            query = query.Where(p => 
                EF.Functions.Like(p.LotId, $"%{searchFilter}%") ||
                (p.Dimension != null && EF.Functions.Like(p.Dimension, $"%{searchFilter}%")) ||
                (p.HeatNumber != null && EF.Functions.Like(p.HeatNumber, $"%{searchFilter}%")) ||
                (p.SteelGrade != null && EF.Functions.Like(p.SteelGrade.Code, $"%{searchFilter}%")) ||
                (p.ProfileType != null && EF.Functions.Like(p.ProfileType.Code, $"%{searchFilter}%")));
        }

        return await query
            .OrderBy(p => p.LotId)
            .ThenBy(p => p.ArrivalDate)
            .ToListAsync();
    }

    public async Task<Profile?> GetByIdAsync(int id)
    {
        return await _context.Profiles
            .Include(p => p.SteelGrade)
            .Include(p => p.ProfileType)
            .Include(p => p.InvoiceLine)
            .Include(p => p.Project)
            .Include(p => p.Certificate)
            .Include(p => p.Usages)
            .Include(p => p.Remnants)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Profile?> GetByLotIdAsync(string lotId)
    {
        return await _context.Profiles
            .Include(p => p.SteelGrade)
            .Include(p => p.ProfileType)
            .Include(p => p.InvoiceLine)
            .Include(p => p.Project)
            .Include(p => p.Certificate)
            .FirstOrDefaultAsync(p => p.LotId == lotId);
    }

    public async Task<Profile> CreateAsync(Profile profile)
    {
        // Validate required fields
        if (string.IsNullOrEmpty(profile.LotId))
        {
            throw new InvalidOperationException("LotId is required.");
        }

        // Validate LotId pattern: One uppercase letter + sequential number (e.g., A15)
        if (!LotIdPattern.IsMatch(profile.LotId))
        {
            throw new InvalidOperationException("LotId must follow the pattern: one uppercase letter followed by numbers (e.g., A15).");
        }

        if (profile.LengthMm <= 0)
        {
            throw new InvalidOperationException("Length must be greater than 0.");
        }

        if (profile.Weight <= 0)
        {
            throw new InvalidOperationException("Weight must be greater than 0.");
        }

        // Check for duplicate LotId
        var existingProfile = await _context.Profiles
            .FirstOrDefaultAsync(p => p.LotId == profile.LotId);
        
        if (existingProfile != null)
        {
            throw new InvalidOperationException($"A profile with LotId '{profile.LotId}' already exists.");
        }

        // Set AvailableLengthMm to LengthMm initially
        profile.AvailableLengthMm = profile.LengthMm;

        _context.Profiles.Add(profile);
        await _context.SaveChangesAsync();
        return profile;
    }

    public async Task<Profile> UpdateAsync(Profile profile)
    {
        // Validate required fields
        if (string.IsNullOrEmpty(profile.LotId))
        {
            throw new InvalidOperationException("LotId is required.");
        }

        // Validate LotId pattern
        if (!LotIdPattern.IsMatch(profile.LotId))
        {
            throw new InvalidOperationException("LotId must follow the pattern: one uppercase letter followed by numbers (e.g., A15).");
        }

        if (profile.LengthMm <= 0)
        {
            throw new InvalidOperationException("Length must be greater than 0.");
        }

        if (profile.Weight <= 0)
        {
            throw new InvalidOperationException("Weight must be greater than 0.");
        }

        // Check for duplicate LotId excluding current profile
        var existingProfile = await _context.Profiles
            .FirstOrDefaultAsync(p => p.LotId == profile.LotId && p.Id != profile.Id);
        
        if (existingProfile != null)
        {
            throw new InvalidOperationException($"A profile with LotId '{profile.LotId}' already exists.");
        }

        _context.Profiles.Update(profile);
        await _context.SaveChangesAsync();
        return profile;
    }

    public async Task DeleteAsync(int id)
    {
        var profile = await _context.Profiles.FindAsync(id);
        if (profile != null)
        {
            // Check if profile can be deleted
            if (!await CanDeleteAsync(id))
            {
                throw new InvalidOperationException("Cannot delete profile that has been used or has remnants.");
            }

            // Soft delete by setting IsDeleted to true (inherited from TenantEntity)
            profile.IsDeleted = true;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> CanDeleteAsync(int id)
    {
        var profile = await _context.Profiles
            .Include(p => p.Usages)
            .Include(p => p.Remnants)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (profile == null)
            return false;

        // Cannot delete if profile is reserved, has usage records, or has remnants
        return !profile.IsReserved && !profile.Usages.Any() && !profile.Remnants.Any();
    }

    public async Task<IEnumerable<Profile>> GetAvailableProfilesAsync(int? profileTypeId = null, int? steelGradeId = null)
    {
        var query = _context.Profiles
            .Include(p => p.SteelGrade)
            .Include(p => p.ProfileType)
            .Where(p => !p.IsReserved && p.AvailableLengthMm > 0);

        // Apply profile type filter
        if (profileTypeId.HasValue)
        {
            query = query.Where(p => p.ProfileTypeId == profileTypeId.Value);
        }

        // Apply steel grade filter
        if (steelGradeId.HasValue)
        {
            query = query.Where(p => p.SteelGradeId == steelGradeId.Value);
        }

        return await query
            .OrderBy(p => p.LotId)
            .ToListAsync();
    }

    public async Task<IEnumerable<ProfileRemnant>> GetRemnantsAsync(int profileId)
    {
        return await _context.ProfileRemnants
            .Include(r => r.Profile)
                .ThenInclude(p => p.SteelGrade)
            .Include(r => r.Profile)
                .ThenInclude(p => p.ProfileType)
            .Where(r => r.ProfileId == profileId)
            .OrderBy(r => r.CreatedDate)
            .ToListAsync();
    }

    public async Task<ProfileUsage> UseProfileAsync(string lotId, ProfileUsageRequest request)
    {
        // Validate input
        if (string.IsNullOrEmpty(lotId))
            throw new ArgumentException("LotId is required.", nameof(lotId));

        if (request.UsedLengthMm <= 0)
            throw new InvalidOperationException("Used length must be greater than 0.");

        if (request.PiecesUsed <= 0)
            throw new InvalidOperationException("Pieces used must be greater than 0.");

        if (string.IsNullOrEmpty(request.UsedBy))
            throw new InvalidOperationException("UsedBy is required.");

        // Attempt to use a transaction; fallback to direct processing if not supported
        try
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var result = await ProcessUsageAsync(lotId, request);
                await transaction.CommitAsync();
                return result;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        else
        {
            // Direct processing for in-memory tests
            return await ProcessUsageAsync(lotId, request);
        }
    }

    private async Task<ProfileUsage> ProcessUsageAsync(string lotId, ProfileUsageRequest request)
    {
        // Get profile with concurrency check
        var profile = await _context.Profiles
            .FirstOrDefaultAsync(p => p.LotId == lotId);

        if (profile == null)
            throw new InvalidOperationException($"Profile with LotId '{lotId}' not found.");

        // Calculate total length needed
        var totalLengthNeeded = request.UsedLengthMm * request.PiecesUsed;

        // Check if sufficient material is available
        if (profile.AvailableLengthMm < totalLengthNeeded)
        {
            throw new InvalidOperationException(
                $"Insufficient material available. Required: {totalLengthNeeded}mm, Available: {profile.AvailableLengthMm}mm");
        }

        // Update available length atomically
        profile.AvailableLengthMm -= totalLengthNeeded;

        // Create usage record
        var usage = new ProfileUsage
        {
            ProfileId = profile.Id,
            ProjectId = request.ProjectId,
            ManufacturingOrderId = request.ManufacturingOrderId,
            UsageDate = DateTime.UtcNow,
            UsedBy = request.UsedBy,
            UsedLengthMm = request.UsedLengthMm,
            PiecesUsed = request.PiecesUsed,
            RemnantFlag = request.RemnantLengthMm.HasValue && request.RemnantLengthMm.Value > 0,
            RemnantLengthMm = request.RemnantLengthMm,
            Notes = request.Notes
        };

        _context.ProfileUsages.Add(usage);

        // Create remnant if specified
        if (request.RemnantLengthMm.HasValue && request.RemnantLengthMm.Value > 0)
        {
            var remnant = new ProfileRemnant
            {
                ProfileId = profile.Id,
                RemnantId = $"{profile.LotId}-{request.RemnantLengthMm.Value}",
                LengthMm = request.RemnantLengthMm.Value,
                Weight = CalculateRemnantWeight(profile, request.RemnantLengthMm.Value),
                IsUsable = true,
                IsUsed = false,
                CreatedDate = DateTime.UtcNow,
                Notes = $"Created from usage: {request.Notes}"
            };

            _context.ProfileRemnants.Add(remnant);
        }

        // Save changes
        await _context.SaveChangesAsync();

        // Return the usage record with navigation properties loaded
        return await _context.ProfileUsages
            .Include(u => u.Profile)
                .ThenInclude(p => p.SteelGrade)
            .Include(u => u.Profile)
                .ThenInclude(p => p.ProfileType)
            .Include(u => u.Project)
            .Include(u => u.ManufacturingOrder)
            .FirstAsync(u => u.Id == usage.Id);
    }

    private decimal CalculateRemnantWeight(Profile profile, int remnantLengthMm)
    {
        // Calculate proportional weight based on the remnant length
        if (profile.LengthMm > 0)
        {
            return profile.Weight * remnantLengthMm / profile.LengthMm;
        }
        return 0;
    }
}