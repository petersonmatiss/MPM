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
    Task<IEnumerable<ProfileRemnant>> GetAllRemnantsAsync(bool availableOnly = true);
    Task<ProfileRemnant?> GetRemnantByIdAsync(int remnantId);
    Task<ProfileUsage> UseProfileAsync(string lotId, ProfileUsageRequest request);
    Task<ProfileUsage> UseRemnantAsync(int remnantId, RemnantUsageRequest request);
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

        if (profile.PieceLength <= 0)
        {
            throw new InvalidOperationException("Piece length must be greater than 0.");
        }

        // Check for duplicate LotId
        var existingProfile = await _context.Profiles
            .FirstOrDefaultAsync(p => p.LotId == profile.LotId);
        
        if (existingProfile != null)
        {
            throw new InvalidOperationException($"A profile with LotId '{profile.LotId}' already exists.");
        }

        // Calculate pieces available based on total length and piece length
        if (profile.PiecesAvailable == 0)
        {
            profile.PiecesAvailable = profile.LengthMm / profile.PieceLength;
        }

        // Set AvailableLengthMm for backward compatibility
        profile.AvailableLengthMm = profile.PiecesAvailable * profile.PieceLength;

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

        if (profile.PieceLength <= 0)
        {
            throw new InvalidOperationException("Piece length must be greater than 0.");
        }

        // Check for duplicate LotId excluding current profile
        var existingProfile = await _context.Profiles
            .FirstOrDefaultAsync(p => p.LotId == profile.LotId && p.Id != profile.Id);
        
        if (existingProfile != null)
        {
            throw new InvalidOperationException($"A profile with LotId '{profile.LotId}' already exists.");
        }

        // Update AvailableLengthMm for backward compatibility
        profile.AvailableLengthMm = profile.PiecesAvailable * profile.PieceLength;

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
            .Where(p => !p.IsReserved && p.PiecesAvailable > 0);

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

    public async Task<IEnumerable<ProfileRemnant>> GetAllRemnantsAsync(bool availableOnly = true)
    {
        var query = _context.ProfileRemnants
            .Include(r => r.Profile)
                .ThenInclude(p => p.SteelGrade)
            .Include(r => r.Profile)
                .ThenInclude(p => p.ProfileType)
            .AsQueryable();

        if (availableOnly)
        {
            query = query.Where(r => r.IsUsable && r.PiecesAvailable > 0);
        }

        return await query
            .OrderBy(r => r.Profile.LotId)
            .ThenBy(r => r.CreatedDate)
            .ToListAsync();
    }

    public async Task<ProfileRemnant?> GetRemnantByIdAsync(int remnantId)
    {
        return await _context.ProfileRemnants
            .Include(r => r.Profile)
                .ThenInclude(p => p.SteelGrade)
            .Include(r => r.Profile)
                .ThenInclude(p => p.ProfileType)
            .FirstOrDefaultAsync(r => r.Id == remnantId);
    }

    public async Task<ProfileUsage> UseProfileAsync(string lotId, ProfileUsageRequest request)
    {
        // Validate input
        if (string.IsNullOrEmpty(lotId))
            throw new ArgumentException("LotId is required.", nameof(lotId));

        if (request.UsedPieceLength <= 0)
            throw new InvalidOperationException("Used piece length must be greater than 0.");

        if (request.PiecesUsed <= 0)
            throw new InvalidOperationException("Pieces used must be greater than 0.");

        if (string.IsNullOrEmpty(request.UsedBy))
            throw new InvalidOperationException("UsedBy is required.");

        if (request.RemnantPieceLength.HasValue && request.RemnantPieceLength.Value <= 0)
            throw new InvalidOperationException("Remnant piece length must be greater than 0 if specified.");

        if (request.RemnantPiecesCreated.HasValue && request.RemnantPiecesCreated.Value <= 0)
            throw new InvalidOperationException("Remnant pieces created must be greater than 0 if specified.");

        // Attempt to use a transaction; fallback to direct processing if not supported
        try
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var result = await ProcessProfileUsageAsync(lotId, request);
                await transaction.CommitAsync();
                return result;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (InvalidOperationException) when (_context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
        {
            // Direct processing for in-memory tests
            return await ProcessProfileUsageAsync(lotId, request);
        }
    }

    public async Task<ProfileUsage> UseRemnantAsync(int remnantId, RemnantUsageRequest request)
    {
        // Validate input
        if (request.UsedPieceLength <= 0)
            throw new InvalidOperationException("Used piece length must be greater than 0.");

        if (request.PiecesUsed <= 0)
            throw new InvalidOperationException("Pieces used must be greater than 0.");

        if (string.IsNullOrEmpty(request.UsedBy))
            throw new InvalidOperationException("UsedBy is required.");

        if (request.NewRemnantPieceLength.HasValue && request.NewRemnantPieceLength.Value <= 0)
            throw new InvalidOperationException("New remnant piece length must be greater than 0 if specified.");

        if (request.NewRemnantPiecesCreated.HasValue && request.NewRemnantPiecesCreated.Value <= 0)
            throw new InvalidOperationException("New remnant pieces created must be greater than 0 if specified.");

        // Attempt to use a transaction; fallback to direct processing if not supported
        try
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var result = await ProcessRemnantUsageAsync(remnantId, request);
                await transaction.CommitAsync();
                return result;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (InvalidOperationException) when (_context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
        {
            // Direct processing for in-memory tests
            return await ProcessRemnantUsageAsync(remnantId, request);
        }
    }

    private async Task<ProfileUsage> ProcessProfileUsageAsync(string lotId, ProfileUsageRequest request)
    {
        // Get profile with concurrency check
        var profile = await _context.Profiles
            .FirstOrDefaultAsync(p => p.LotId == lotId);

        if (profile == null)
            throw new InvalidOperationException($"Profile with LotId '{lotId}' not found.");

        // Validate that pieces match the profile's piece length
        if (request.UsedPieceLength != profile.PieceLength)
            throw new InvalidOperationException($"Used piece length ({request.UsedPieceLength}mm) must match profile piece length ({profile.PieceLength}mm).");

        // Check if sufficient pieces are available
        if (profile.PiecesAvailable < request.PiecesUsed)
        {
            throw new InvalidOperationException(
                $"Insufficient pieces available. Required: {request.PiecesUsed} pieces, Available: {profile.PiecesAvailable} pieces");
        }

        // Update available pieces atomically
        profile.PiecesAvailable -= request.PiecesUsed;
        profile.AvailableLengthMm = profile.PiecesAvailable * profile.PieceLength; // Update legacy field

        // Create usage record
        var usage = new ProfileUsage
        {
            ProfileId = profile.Id,
            ProjectId = request.ProjectId,
            ManufacturingOrderId = request.ManufacturingOrderId,
            UsageDate = DateTime.UtcNow,
            UsedBy = request.UsedBy,
            UsedPieceLength = request.UsedPieceLength,
            PiecesUsed = request.PiecesUsed,
            RemnantFlag = request.RemnantPieceLength.HasValue && request.RemnantPiecesCreated.HasValue,
            RemnantPieceLength = request.RemnantPieceLength,
            RemnantPiecesCreated = request.RemnantPiecesCreated,
            Notes = request.Notes
        };

        _context.ProfileUsages.Add(usage);

        // Create remnant if specified
        if (request.RemnantPieceLength.HasValue && request.RemnantPiecesCreated.HasValue)
        {
            var remnant = new ProfileRemnant
            {
                ProfileId = profile.Id,
                RemnantId = $"{profile.LotId}-{request.RemnantPieceLength.Value}-{Guid.NewGuid().ToString("N")[..4]}",
                LengthMm = request.RemnantPieceLength.Value * request.RemnantPiecesCreated.Value,
                PieceLength = request.RemnantPieceLength.Value,
                PiecesAvailable = request.RemnantPiecesCreated.Value,
                Weight = CalculateRemnantWeight(profile, request.RemnantPieceLength.Value, request.RemnantPiecesCreated.Value),
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

    private async Task<ProfileUsage> ProcessRemnantUsageAsync(int remnantId, RemnantUsageRequest request)
    {
        // Get remnant with profile information
        var remnant = await _context.ProfileRemnants
            .Include(r => r.Profile)
            .FirstOrDefaultAsync(r => r.Id == remnantId);

        if (remnant?.Profile == null)
            throw new InvalidOperationException($"Remnant with ID '{remnantId}' not found.");

        if (!remnant.IsUsable)
            throw new InvalidOperationException($"Remnant '{remnant.RemnantId}' is not usable.");

        // Validate that pieces match the remnant's piece length
        if (request.UsedPieceLength != remnant.PieceLength)
            throw new InvalidOperationException($"Used piece length ({request.UsedPieceLength}mm) must match remnant piece length ({remnant.PieceLength}mm).");

        // Check if sufficient pieces are available
        if (remnant.PiecesAvailable < request.PiecesUsed)
        {
            throw new InvalidOperationException(
                $"Insufficient remnant pieces available. Required: {request.PiecesUsed} pieces, Available: {remnant.PiecesAvailable} pieces");
        }

        // Update available pieces atomically
        remnant.PiecesAvailable -= request.PiecesUsed;
        remnant.LengthMm = remnant.PiecesAvailable * remnant.PieceLength; // Update total length

        // Mark as used if no pieces remain
        if (remnant.PiecesAvailable == 0)
        {
            remnant.IsUsed = true;
        }

        // Create usage record
        var usage = new ProfileUsage
        {
            ProfileRemnantId = remnant.Id,
            ProjectId = request.ProjectId,
            ManufacturingOrderId = request.ManufacturingOrderId,
            UsageDate = DateTime.UtcNow,
            UsedBy = request.UsedBy,
            UsedPieceLength = request.UsedPieceLength,
            PiecesUsed = request.PiecesUsed,
            RemnantFlag = request.NewRemnantPieceLength.HasValue && request.NewRemnantPiecesCreated.HasValue,
            RemnantPieceLength = request.NewRemnantPieceLength,
            RemnantPiecesCreated = request.NewRemnantPiecesCreated,
            Notes = request.Notes
        };

        _context.ProfileUsages.Add(usage);

        // Create new remnant if specified
        if (request.NewRemnantPieceLength.HasValue && request.NewRemnantPiecesCreated.HasValue)
        {
            var newRemnant = new ProfileRemnant
            {
                ProfileId = remnant.ProfileId,
                RemnantId = $"{remnant.Profile!.LotId}-{request.NewRemnantPieceLength.Value}-{Guid.NewGuid().ToString("N")[..4]}",
                LengthMm = request.NewRemnantPieceLength.Value * request.NewRemnantPiecesCreated.Value,
                PieceLength = request.NewRemnantPieceLength.Value,
                PiecesAvailable = request.NewRemnantPiecesCreated.Value,
                Weight = CalculateRemnantWeight(remnant.Profile!, request.NewRemnantPieceLength.Value, request.NewRemnantPiecesCreated.Value),
                IsUsable = true,
                IsUsed = false,
                CreatedDate = DateTime.UtcNow,
                Notes = $"Created from remnant usage: {request.Notes}"
            };

            _context.ProfileRemnants.Add(newRemnant);
        }

        // Save changes
        await _context.SaveChangesAsync();

        // Return the usage record with navigation properties loaded
        return await _context.ProfileUsages
            .Include(u => u.ProfileRemnant)
                .ThenInclude(r => r.Profile)
                    .ThenInclude(p => p.SteelGrade)
            .Include(u => u.ProfileRemnant)
                .ThenInclude(r => r.Profile)
                    .ThenInclude(p => p.ProfileType)
            .Include(u => u.Project)
            .Include(u => u.ManufacturingOrder)
            .FirstAsync(u => u.Id == usage.Id);
    }

    private decimal CalculateRemnantWeight(Profile profile, int remnantPieceLength, int remnantPieces)
    {
        if (profile != null && profile.PieceLength > 0)
        {
            // Calculate weight per piece, then multiply by remnant piece length and number of pieces
            var weightPerMm = profile.Weight / (profile.LengthMm);
            return weightPerMm * remnantPieceLength * remnantPieces;
        }
        return 0;
    }
}