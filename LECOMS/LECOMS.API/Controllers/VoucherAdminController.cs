using LECOMS.Common.Helper;
using LECOMS.Data.DTOs.Voucher;
using LECOMS.Data.Entities;
using LECOMS.RepositoryContract.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace LECOMS.API.Controllers
{
    [ApiController]
    [Route("api/admin/vouchers")]
    [Authorize(Roles = "Admin")]
    public class VoucherAdminController : ControllerBase
    {
        private readonly IUnitOfWork _uow;

        public VoucherAdminController(IUnitOfWork uow)
        {
            _uow = uow;
        }

        // ============================================================
        // GET: ALL VOUCHERS
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var response = new APIResponse();
            try
            {
                var list = await _uow.Vouchers.GetAllAsync();

                response.Result = list;
                response.StatusCode = HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add(ex.Message);
            }

            return StatusCode((int)response.StatusCode, response);
        }


        // ============================================================
        // GET BY ID
        // ============================================================
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var response = new APIResponse();
            try
            {
                var voucher = await _uow.Vouchers.GetAsync(v => v.Id == id);

                if (voucher == null)
                {
                    response.IsSuccess = false;
                    response.StatusCode = HttpStatusCode.NotFound;
                    response.ErrorMessages.Add("Voucher không tìm thấy.");
                    return NotFound(response);
                }

                response.Result = voucher;
                response.StatusCode = HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.ErrorMessages.Add(ex.Message);
                response.StatusCode = HttpStatusCode.InternalServerError;
            }

            return StatusCode((int)response.StatusCode, response);
        }


        // ============================================================
        // CREATE VOUCHER
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] VoucherCreateDTO dto)
        {
            var response = new APIResponse();

            try
            {
                var exists = await _uow.Vouchers.GetByCodeAsync(dto.Code);
                if (exists != null)
                    throw new InvalidOperationException("Voucher code already exists.");

                var voucher = new Voucher
                {
                    Code = dto.Code.Trim(),
                    DiscountType = dto.DiscountType,
                    DiscountValue = dto.DiscountValue,
                    MaxDiscountAmount = dto.MaxDiscountAmount,
                    MinOrderAmount = dto.MinOrderAmount,
                    UsageLimitPerUser = dto.UsageLimitPerUser,
                    QuantityAvailable = dto.QuantityAvailable,
                    StartDate = dto.StartDate,
                    EndDate = dto.EndDate,
                    IsActive = dto.IsActive
                };

                await _uow.Vouchers.AddAsync(voucher);
                await _uow.CompleteAsync();

                response.StatusCode = HttpStatusCode.Created;
                response.Result = voucher;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.BadRequest;
                response.ErrorMessages.Add(ex.Message);
            }

            return StatusCode((int)response.StatusCode, response);
        }


        // ============================================================
        // UPDATE VOUCHER
        // ============================================================
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] VoucherUpdateDTO dto)
        {
            var response = new APIResponse();

            try
            {
                var voucher = await _uow.Vouchers.GetAsync(v => v.Id == id);
                if (voucher == null)
                    throw new InvalidOperationException("Voucher không tìm thấy.");

                // apply update
                if (dto.DiscountType.HasValue) voucher.DiscountType = dto.DiscountType.Value;
                if (dto.DiscountValue.HasValue) voucher.DiscountValue = dto.DiscountValue.Value;
                if (dto.MaxDiscountAmount.HasValue) voucher.MaxDiscountAmount = dto.MaxDiscountAmount;
                if (dto.MinOrderAmount.HasValue) voucher.MinOrderAmount = dto.MinOrderAmount;
                if (dto.UsageLimitPerUser.HasValue) voucher.UsageLimitPerUser = dto.UsageLimitPerUser;
                if (dto.QuantityAvailable.HasValue) voucher.QuantityAvailable = dto.QuantityAvailable.Value;
                if (dto.StartDate.HasValue) voucher.StartDate = dto.StartDate.Value;
                if (dto.EndDate.HasValue) voucher.EndDate = dto.EndDate;
                if (dto.IsActive.HasValue) voucher.IsActive = dto.IsActive.Value;

                await _uow.Vouchers.UpdateAsync(voucher);
                await _uow.CompleteAsync();

                response.StatusCode = HttpStatusCode.OK;
                response.Result = voucher;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.BadRequest;
                response.ErrorMessages.Add(ex.Message);
            }

            return StatusCode((int)response.StatusCode, response);
        }


        // ============================================================
        // DELETE VOUCHER
        // ============================================================
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var response = new APIResponse();

            try
            {
                var voucher = await _uow.Vouchers.GetAsync(v => v.Id == id);
                if (voucher == null)
                    throw new InvalidOperationException("Voucher không tìm thấy.");

                await _uow.Vouchers.DeleteAsync(voucher);
                await _uow.CompleteAsync();

                response.StatusCode = HttpStatusCode.OK;
                response.Result = "Deleted.";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.BadRequest;
                response.ErrorMessages.Add(ex.Message);
            }

            return StatusCode((int)response.StatusCode, response);
        }


        // ============================================================
        // Voucher đã hết hạn
        // ============================================================
        [HttpGet("expired")]
        public async Task<IActionResult> GetExpiredVouchers()
        {
            var response = new APIResponse();

            try
            {
                var now = DateTime.UtcNow;
                var vouchers = await _uow.Vouchers.GetAllAsync(
                    v => v.EndDate.HasValue && v.EndDate.Value < now,
                    includeProperties: "UserVouchers"
                );

                response.StatusCode = HttpStatusCode.OK;
                response.Result = vouchers;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add(ex.Message);
            }

            return StatusCode((int)response.StatusCode, response);
        }


        // ============================================================
        // Voucher sắp hết hạn (expiring soon)
        // ============================================================
        [HttpGet("expiring")]
        public async Task<IActionResult> GetExpiringSoon( [FromQuery] int days = 7)
        {
            var response = new APIResponse();

            try
            {
                var now = DateTime.UtcNow;
                var soon = now.AddDays(days);

                var vouchers = await _uow.Vouchers.GetAllAsync(
                    v =>
                        v.IsActive &&
                        v.EndDate.HasValue &&
                        v.EndDate.Value >= now &&
                        v.EndDate.Value <= soon,
                    includeProperties: "UserVouchers"
                );

                response.StatusCode = HttpStatusCode.OK;
                response.Result = vouchers;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add(ex.Message);
            }

            return StatusCode((int)response.StatusCode, response);
        }

    }
}
