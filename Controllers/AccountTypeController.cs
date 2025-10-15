using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MicrDbChequeProcessingSystem.Data;
using MicrDbChequeProcessingSystem.Models;
using MicrDbChequeProcessingSystem.ViewModels;

namespace MicrDbChequeProcessingSystem.Controllers;

public class AccountTypeController : Controller
{
    private readonly MicrDbContext _context;

    public AccountTypeController(MicrDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var systemTypes = await _context.AccountTypes
            .AsNoTracking()
            .OrderBy(a => a.AccountTypeName)
            .Select(a => new AccountTypeListItem
            {
                Name = a.AccountTypeName,
                Code = a.AccountTypeCode,
                Source = "Core",
                Description = null,
                Created = a.CreatedDate.ToString("dd MMM yyyy")
            })
            .ToListAsync();

        var customTypes = await _context.AccountTypeCustoms
            .AsNoTracking()
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new AccountTypeListItem
            {
                Name = a.AccountTypeName,
                Code = null,
                Source = "Custom",
                Description = a.Description,
                Created = a.CreatedAt.ToLocalTime().ToString("dd MMM yyyy HH:mm")
            })
            .ToListAsync();

        var viewModel = new AccountTypeIndexViewModel
        {
            Items = systemTypes
                .Concat(customTypes)
                .OrderByDescending(i => i.Source == "Custom")
                .ThenBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
                .ToList()
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AccountTypeCreateRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Please provide the required details." });
        }

        var entry = new AccountTypeCustom
        {
            AccountTypeName = request.AccountTypeName.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            _context.AccountTypeCustoms.Add(entry);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return StatusCode(500, new { success = false, message = "We couldn't save the record. Please try again." });
        }

        return Json(new
        {
            success = true,
            data = new
            {
                id = entry.Id,
                accountTypeName = entry.AccountTypeName,
                code = (string?)null,
                description = entry.Description,
                source = "Custom",
                created = entry.CreatedAt.ToLocalTime().ToString("dd MMM yyyy HH:mm")
            }
        });
    }
}
