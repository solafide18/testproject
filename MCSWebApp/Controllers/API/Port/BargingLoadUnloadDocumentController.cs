﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using DevExtreme.AspNet.Data;
using DevExtreme.AspNet.Mvc;
using DataAccess.EFCore;
using Microsoft.EntityFrameworkCore;
using NLog;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using DataAccess.DTO;
using Omu.ValueInjecter;
using DataAccess.EFCore.Repository;
using Microsoft.AspNetCore.Http;
using System.IO;
using BusinessLogic;
using Microsoft.AspNetCore.StaticFiles;
using WebApp.Extensions;

namespace MCSWebApp.Controllers.API.Port
{
    [Route("api/Port/[controller]")]
    [ApiController]
    public class BargingLoadUnloadDocumentController : ApiBaseController
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly mcsContext dbContext;

        public BargingLoadUnloadDocumentController(IConfiguration Configuration, IOptions<SysAdmin> SysAdminOption)
            : base(Configuration, SysAdminOption)
        {
            dbContext = new mcsContext(DbOptionBuilder.Options);
        }

        [HttpGet("DataGrid")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> DataGrid(DataSourceLoadOptions loadOptions, string Id)
        {
            try
            {
                return await DataSourceLoader.LoadAsync(
                    dbContext.barging_load_unload_document.Where(o => o.organization_id == CurrentUserContext.OrganizationId && o.barging_transaction_id == Id),
                    loadOptions);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("ByBargingTransactionId/{Id}")]
        public async Task<object> ByBargingTransactionId(string Id, DataSourceLoadOptions loadOptions)
        {
            return await DataSourceLoader.LoadAsync(
                dbContext.barging_load_unload_document.Where(o => o.barging_transaction_id == Id),
                loadOptions);
        }

        [HttpGet("DataDetail")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> DataDetail(string Id, DataSourceLoadOptions loadOptions)
        {
            return await DataSourceLoader.LoadAsync(
                dbContext.barging_load_unload_document.Where(o => o.barging_transaction_id == Id),
                loadOptions);
        }

        [HttpPost("InsertData")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> InsertData(dynamic FileDocument)
        {
            logger.Debug($"id = {FileDocument.bargingTransId}");
            logger.Debug($"activityId = {FileDocument.activityId}");
            logger.Debug($"documentTypeId = {FileDocument.documentTypeId}");
            logger.Debug($"remark = {FileDocument.remark}");
            logger.Debug($"quantity = {FileDocument.quantity}");
            logger.Debug($"quality = {FileDocument.quality}");
            logger.Debug($"filename = {FileDocument.fileName}");
            logger.Debug($"filesize = {FileDocument.fileSize}");
            logger.Debug($"return_cargo = {FileDocument.return_cargo}");
            var result = new StandardResult();

            if (FileDocument.bargingTransId == null)
            {
                throw new Exception("Id is null");
            }

            if (FileDocument.activityId == null)
            {
                throw new Exception("Id is null");
            }

            //if (FileDocument.documentTypeId == null)
            //{
            //    throw new Exception("Id is null");
            //}

            if (FileDocument.fileName == null)
            {
                throw new Exception("File name is empty");
            }

            //if (FileDocument.data == null)
            //{
            //    throw new Exception("File content is empty");
            //}

            try
            {
                if (await mcsContext.CanCreate(dbContext, nameof(barging_load_unload_document),
                    CurrentUserContext.AppUserId) || CurrentUserContext.IsSysAdmin)
                {
                    var bargingTransId = (string)FileDocument.bargingTransId;
                    var fileName = (string)FileDocument.fileName;
                    var data = (string)FileDocument.data;
                    var activityId = (string)FileDocument.activityId;
                    var documentTypeId = (string)FileDocument.documentTypeId;
                    var remark = (string)FileDocument.remark;
                    var quantity = (bool)FileDocument.quantity;
                    var quality = (bool)FileDocument.quality;
                    var return_cargo = (bool)FileDocument.return_cargo;
                    //save to server
                    var uploadPath = Path.Combine(configuration["Path:UploadPathBargingLoadUnload"].ToString(), configuration["Path:UploadUrlBargingLoadUnload"].ToString());
                    var filePath = uploadPath + bargingTransId;
                    if (!System.IO.Directory.Exists(filePath))
                    {
                        System.IO.Directory.CreateDirectory(filePath);
                    }

                    //save to folder
                    var fullPath = Path.Combine(filePath, fileName);

                    if (System.IO.File.Exists(fullPath))
                    {
                        System.IO.File.Delete(fullPath);
                    }

                    byte[] imageBytes = Convert.FromBase64String(data);

                    System.IO.File.WriteAllBytes(fullPath, imageBytes);


                    string savedFileName = configuration["Path:UploadUrlBargingLoadUnload"].ToString() + bargingTransId + "\\" + fileName;

                    //insert data to customer attachment
                    var bargingLoadUnloadDocument = new barging_load_unload_document()
                    {
                        id = Guid.NewGuid().ToString("N"),
                        created_by = CurrentUserContext.AppUserId,
                        created_on = DateTime.Now,
                        modified_by = null,
                        modified_on = null,
                        is_active = true,
                        is_locked = null,
                        is_default = null,
                        owner_id = CurrentUserContext.AppUserId,
                        organization_id = CurrentUserContext.OrganizationId,
                        entity_id = null,
                        filename = savedFileName,
                        barging_transaction_id = bargingTransId,
                        activity_id = activityId,
                        document_type_id = documentTypeId,
                        remark = remark,
                        quality = quality,
                        quantity = quantity,
                        return_cargo = return_cargo,
                    };
                    dbContext.barging_load_unload_document.Add(bargingLoadUnloadDocument);
                    await dbContext.SaveChangesAsync();

                    result.Success = true;
                    result.Message = "File is uploaded";
                }
                else
                {
                    return BadRequest("User is not authorized.");
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.ToString());
                result.Message = ex.Message;
            }

            return result;
        }

        [HttpDelete("DeleteData")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> DeleteData([FromForm] string key)
        {
            logger.Debug($"string key = {key}");

            try
            {
                var record = dbContext.barging_load_unload_document
                    .Where(o => o.id == key)
                    .FirstOrDefault();
                if (record != null)
                {

                    dbContext.barging_load_unload_document.Remove(record);
                    await dbContext.SaveChangesAsync();

                    var uploadPath = Path.Combine(configuration["Path:UploadPathBargingLoadUnload"].ToString(), record.filename);
                    if (System.IO.File.Exists(uploadPath))
                    {
                        System.IO.File.Delete(uploadPath);
                    }
                }

                return Ok();
            }
            catch (Exception ex)
            {
                logger.Error(ex.InnerException ?? ex);
                return BadRequest(ex.InnerException?.Message ?? ex.Message);
            }
        }

        [HttpGet("DownloadDocument/{Id}")]
        public async Task<IActionResult> DownloadDocument(string Id)
        {
            logger.Debug($"string Id = {Id}");

            try
            {
                var record = await dbContext.barging_load_unload_document
                    .Where(o => o.id == Id)
                    .FirstOrDefaultAsync();
                if (record != null)
                {
                    //get file 
                    var filePath = Path.Combine(configuration["Path:UploadPathBargingLoadUnload"].ToString(), record.filename);

                    if (System.IO.File.Exists(filePath))
                    {
                        var provider = new FileExtensionContentTypeProvider();
                        string mimeType;
                        if (provider.TryGetContentType(filePath, out mimeType))
                        {
                            var byteFile = await System.IO.File.ReadAllBytesAsync(filePath);
                            if (byteFile.Length > 0)
                            {
                                return new InlineFileContentResult(byteFile, mimeType)
                                {
                                    FileDownloadName = Path.GetFileName(filePath)
                                };
                            }
                        }
                    }
                }

                throw new Exception("File not found");
            }
            catch (Exception ex)
            {
                logger.Error(ex.InnerException ?? ex);
                return BadRequest(ex.InnerException?.Message ?? ex.Message);
            }
        }

    }
}
