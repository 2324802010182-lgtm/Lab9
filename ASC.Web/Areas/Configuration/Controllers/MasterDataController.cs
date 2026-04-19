using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using ASC.Business.Interfaces;
using ASC.Web.Areas.Configuration.Models;
using ASC.Model.Models;
using ASC.Web.Controllers;
using ASC.Web.Extensions;

namespace ASC.Web.Areas.Configuration.Controllers
{
    [Area("Configuration")]
    [Authorize(Roles = "Admin")]
    public class MasterDataController : BaseController
    {
        private readonly IMasterDataOperations _masterData;
        private readonly IMapper _mapper;

        public MasterDataController(IMasterDataOperations masterData, IMapper mapper)
        {
            _masterData = masterData;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> MasterKeys()
        {
            var masterKeys = await _masterData.GetAllMasterKeysAsync();
            var masterKeysViewModel = _mapper.Map<List<MasterDataKey>, List<MasterDataKeyViewModel>>(masterKeys);

            // Hold all Master Keys in session
            HttpContext.Session.SetSession("MasterKeys", masterKeysViewModel);

            return View(new MasterKeysViewModel
            {
                MasterKeys = masterKeysViewModel?.ToList() ?? new List<MasterDataKeyViewModel>(),
                IsEdit = false,
                MasterKeyInContext = new MasterDataKeyViewModel()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MasterKeys(MasterKeysViewModel masterKeys)
        {
            masterKeys.MasterKeys = HttpContext.Session.GetSession<List<MasterDataKeyViewModel>>("MasterKeys");

            if (!ModelState.IsValid)
            {
                return View(masterKeys);
            }

            var masterKey = _mapper.Map<MasterDataKeyViewModel, MasterDataKey>(masterKeys.MasterKeyInContext);
            var userName = HttpContext.User.GetCurrentUserDetails().Name;

            if (masterKeys.IsEdit)
            {
                // Update Master Key
                masterKey.UpdatedBy = userName;
                await _masterData.UpdateMasterKeyAsync(masterKeys.MasterKeyInContext.PartitionKey, masterKey);
            }
            else
            {
                // Insert Master Key
                masterKey.RowKey = Guid.NewGuid().ToString();
                masterKey.PartitionKey = masterKey.Name;
                masterKey.CreatedBy = userName;
                masterKey.UpdatedBy = userName;
                await _masterData.InsertMasterKeyAsync(masterKey);
            }

            return RedirectToAction("MasterKeys");
        }

        [HttpGet]
        public async Task<IActionResult> MasterValues()
        {
            // Get All Master Keys and hold them in ViewBag for Select tag
            ViewBag.MasterKeys = await _masterData.GetAllMasterKeysAsync();

            return View(new MasterValuesViewModel
            {
                MasterValues = new List<MasterDataValueViewModel>(),
                IsEdit = false,
                MasterValueInContext = new MasterDataValueViewModel()
            });
        }

        [HttpGet]
        public async Task<IActionResult> MasterValuesByKey(string key)
        {
            var data = string.IsNullOrWhiteSpace(key)
                ? new List<MasterDataValue>()
                : await _masterData.GetAllMasterValuesByKeyAsync(key);

            // PascalCase so DataTables column "data": "RowKey" matches (default Json() uses camelCase).
            return Json(new { data }, new JsonSerializerOptions { PropertyNamingPolicy = null });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MasterValues(bool isEdit, MasterDataValueViewModel masterValue)
        {
            if (!ModelState.IsValid)
            {
                return Json("Error");
            }

            var masterDataValue = _mapper.Map<MasterDataValueViewModel, MasterDataValue>(masterValue);

            if (isEdit)
            {
                // Update Master Value
                await _masterData.UpdateMasterValueAsync(masterDataValue.PartitionKey, masterDataValue.RowKey, masterDataValue);
            }
            else
            {
                // Insert Master Value
                masterDataValue.RowKey = Guid.NewGuid().ToString();
                masterDataValue.CreatedBy = HttpContext.User.GetCurrentUserDetails().Name;
                await _masterData.InsertMasterValueAsync(masterDataValue);
            }

            return Json(true);
        }

        private async Task<List<MasterDataValue>> ParseMasterDataExcel(IFormFile excelFile)
        {
            var masterValueList = new List<MasterDataValue>();

            using (var memoryStream = new MemoryStream())
            {
                await excelFile.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                using (ExcelPackage package = new ExcelPackage(memoryStream))
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets[0];
                    if (worksheet?.Dimension == null)
                    {
                        return masterValueList;
                    }

                    int rowCount = worksheet.Dimension.Rows;

                    for (int row = 2; row <= rowCount; row++)
                    {
                        var pk = worksheet.Cells[row, 1].Value?.ToString();
                        var name = worksheet.Cells[row, 2].Value?.ToString();
                        var activeRaw = worksheet.Cells[row, 3].Value?.ToString();
                        if (string.IsNullOrWhiteSpace(pk) || string.IsNullOrWhiteSpace(name))
                        {
                            continue;
                        }

                        var masterDataValue = new MasterDataValue
                        {
                            RowKey = Guid.NewGuid().ToString(),
                            PartitionKey = pk.Trim(),
                            Name = name.Trim(),
                            IsActive = bool.TryParse(activeRaw, out var active) && active
                        };

                        masterValueList.Add(masterDataValue);
                    }
                }
            }

            return masterValueList;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadExcel()
        {
            var files = Request.Form.Files;

            // Validations
            if (!files.Any())
            {
                return Json(new { Error = true, Text = "Upload a file" });
            }

            var excelFile = files.First();

            if (excelFile.Length <= 0)
            {
                return Json(new { Error = true, Text = "Upload a file" });
            }

            var masterData = await ParseMasterDataExcel(excelFile);
            var userName = HttpContext.User.GetCurrentUserDetails().Name;
            foreach (var item in masterData)
            {
                item.CreatedBy = userName;
            }

            var result = await _masterData.UploadBulkMasterData(masterData);

            return Json(new { Success = result });
        }
    }
}