using Microsoft.AspNetCore.Http;
using VarsityLoop.Models.Common;
using VarsityLoop.Models.Entities;
namespace VarsityLoop.Services.Interfaces;
public interface ILandlordVerificationService { Task<LandlordApplication?> GetApplicationAsync(string userId); Task<OperationResult> SubmitAsync(ApplicationUser user,IFormFile identity,IFormFile ownership,IFormFile? accreditation,IFormFile? supporting); Task<List<LandlordApplication>> GetAllAsync(string? status); Task<OperationResult> TransitionAsync(string id,LandlordVerificationStatus status,string? feedback,string actorId,string actorName); Task<(Stream Stream,string ContentType,string FileName)?> DownloadDocumentAsync(string id,int index); }
