using LocalScout.Application.DTOs;
using LocalScout.Application.Interfaces;
using LocalScout.Domain.Entities;
using LocalScout.Infrastructure.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LocalScout.Web.Controllers
{
    [Authorize(Roles = RoleNames.Admin)]
    public class ServiceCategoryController : Controller
    {
        private readonly IServiceCategoryRepository _repo;

        public ServiceCategoryController(IServiceCategoryRepository repo)
        {
            _repo = repo;
        }

        // --- 1. Active Categories ---
        public async Task<IActionResult> ActiveCategories()
        {
            ViewData["Title"] = "Active Service Categories";
            var categories = await _repo.GetCategoriesByStatusAsync(
                isActive: true,
                isApproved: true
            );
            return View("~/Views/Admin/ServiceCategory/ActiveCategories.cshtml", categories);
        }

        // --- 2. Inactive Categories ---
        public async Task<IActionResult> InactiveCategories()
        {
            ViewData["Title"] = "Inactive Service Categories";
            var categories = await _repo.GetCategoriesByStatusAsync(
                isActive: false,
                isApproved: true
            );
            return View("~/Views/Admin/ServiceCategory/InactiveCategories.cshtml", categories);
        }

        // --- 3. Category Requests (Pending Approval) ---
        public async Task<IActionResult> CategoryRequests()
        {
            ViewData["Title"] = "Service Category Requests";
            var categories = await _repo.GetCategoriesByStatusAsync(
                isActive: true,
                isApproved: false
            );
            return View("~/Views/Admin/ServiceCategory/CategoryRequests.cshtml", categories);
        }

        // --- 4. Get Modal for Create/Edit ---
        [HttpGet]
        public async Task<IActionResult> GetCreateOrEditModal(Guid? id)
        {
            if (id == null)
            {
                return PartialView("~/Views/Admin/ServiceCategory/_CreateEditModal.cshtml", new ServiceCategoryDto());
            }

            var category = await _repo.GetCategoryByIdAsync(id.Value);
            if (category == null)
            {
                return NotFound();
            }

            var dto = new ServiceCategoryDto
            {
                ServiceCategoryId = category.ServiceCategoryId,
                CategoryName = category.CategoryName,
                Description = category.Description,
                IconPath = category.IconPath
            };

            return PartialView("~/Views/Admin/ServiceCategory/_CreateEditModal.cshtml", dto);
        }

        // --- 5. Save (Create/Edit) via AJAX ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveCategory(ServiceCategoryDto model)
        {
            if (ModelState.IsValid)
            {
                // Handle File Upload
                if (model.IconFile != null && model.IconFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "categories");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + model.IconFile.FileName;
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.IconFile.CopyToAsync(fileStream);
                    }

                    model.IconPath = "/images/categories/" + uniqueFileName;
                }

                if (model.ServiceCategoryId == Guid.Empty)
                {
                    // Create
                    var category = new ServiceCategory
                    {
                        CategoryName = model.CategoryName,
                        Description = model.Description,
                        IconPath = model.IconPath,
                        IsActive = true,
                        IsApproved = true
                    };
                    await _repo.AddCategoryAsync(category);
                    return Json(new { success = true, message = "Category created successfully!" });
                }
                else
                {
                    // Edit
                    var category = await _repo.GetCategoryByIdAsync(model.ServiceCategoryId);
                    if (category != null)
                    {
                        category.CategoryName = model.CategoryName;
                        category.Description = model.Description;
                        if (!string.IsNullOrEmpty(model.IconPath))
                        {
                            category.IconPath = model.IconPath;
                        }
                        await _repo.UpdateCategoryAsync(category);
                        return Json(new { success = true, message = "Category updated successfully!" });
                    }
                }
            }
            return BadRequest(new { message = "Invalid data submitted." });
        }

        // --- 6. Toggle Status (Activate/Deactivate) ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(Guid id)
        {
            var category = await _repo.GetCategoryByIdAsync(id);
            if (category != null)
            {
                category.IsActive = !category.IsActive;
                await _repo.UpdateCategoryAsync(category);
                return Json(new { success = true, message = category.IsActive ? "Category activated!" : "Category deactivated!" });
            }
            return NotFound(new { message = "Category not found." });
        }

        // --- 7. Approve Request ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveCategory(Guid id)
        {
            var category = await _repo.GetCategoryByIdAsync(id);
            if (category != null)
            {
                category.IsApproved = true;
                category.IsActive = true;
                await _repo.UpdateCategoryAsync(category);
                return Json(new { success = true, message = "Category approved and activated!" });
            }
            return NotFound(new { message = "Category not found." });
        }

        // --- 8. Reject Request ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectCategory(Guid id)
        {
            var category = await _repo.GetCategoryByIdAsync(id);
            if (category != null)
            {
                // Soft delete: Deactivate and Unapprove
                category.IsApproved = false;
                category.IsActive = false;
                await _repo.UpdateCategoryAsync(category);
                return Json(new { success = true, message = "Category rejected." });
            }
            return NotFound(new { message = "Category not found." });
        }
    }
}
