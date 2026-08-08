using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Http;
using VarsityLoop.Models.Common;
using VarsityLoop.Models.Entities;
using VarsityLoop.Repositories.Interfaces;
using VarsityLoop.Services.Interfaces;

namespace VarsityLoop.Services.Implementations;
public class LandlordVerificationService : ILandlordVerificationService
{
 private readonly ILandlordApplicationRepository _apps; private readonly IUserRepository _users; private readonly IStorageService _storage; private readonly IActivityLogService _logs; private readonly ILogger<LandlordVerificationService> _logger;
 private static readonly HashSet<string> Types = new(StringComparer.OrdinalIgnoreCase){"application/pdf","image/jpeg","image/png","image/webp"};
 public LandlordVerificationService(ILandlordApplicationRepository apps, IUserRepository users, IStorageService storage, IActivityLogService logs, ILogger<LandlordVerificationService> logger){_apps=apps;_users=users;_storage=storage;_logs=logs;_logger=logger;}
 public Task<LandlordApplication?> GetApplicationAsync(string userId)=>_apps.GetByIdAsync(userId);
 public async Task<OperationResult> SubmitAsync(ApplicationUser user,IFormFile identity,IFormFile ownership,IFormFile? accreditation,IFormFile? supporting)
 {
  var files=new[]{(identity,LandlordDocumentType.IdentityDocument),(ownership,LandlordDocumentType.ProofOfOwnership),(accreditation,LandlordDocumentType.AccreditationDocumentation),(supporting,LandlordDocumentType.SupportingDocumentation)};
  if(files.Take(2).Any(x=>x.Item1==null||x.Item1.Length==0)) return OperationResult.Fail("Identity and proof of ownership documents are required.");
  if(files.Any(x=>x.Item1!=null && (x.Item1.Length>10*1024*1024 || !Types.Contains(x.Item1.ContentType)))) return OperationResult.Fail("Documents must be PDF, JPG, PNG, or WEBP files under 10 MB.");
  try { var docs=new List<LandlordDocument>(); foreach(var (file,type) in files) if(file!=null&&file.Length>0) using(var stream=file.OpenReadStream()) docs.Add(new LandlordDocument{DocumentType=type.ToString(),FileName=Path.GetFileName(file.FileName),StoragePath=await _storage.UploadPrivateFileAsync(stream,file.FileName,file.ContentType,$"landlord-documents/{user.Id}")}); var app=new LandlordApplication{Id=user.Id,UserId=user.Id,UserName=user.FullName,UserEmail=user.Email,Status=nameof(LandlordVerificationStatus.Pending),Documents=docs,SubmittedAt=Timestamp.GetCurrentTimestamp()}; await _apps.UpdateAsync(user.Id,app); await _users.UpdateFieldsAsync(user.Id,new(){["landlordVerificationStatus"]="Pending"}); return OperationResult.Ok(); } catch(Exception ex){_logger.LogError(ex,"Landlord application submission failed for {UserId}",user.Id);return OperationResult.Fail("We could not submit your application. Please try again.");}
 }
 public async Task<List<LandlordApplication>> GetAllAsync(string? status){var all=await _apps.GetAllApplicationsAsync();return (string.IsNullOrWhiteSpace(status)?all:all.Where(a=>a.Status==status)).OrderBy(a=>a.Status is "Pending" or "UnderReview"?0:1).ThenByDescending(a=>a.SubmittedAt).ToList();}
 public async Task<OperationResult> TransitionAsync(string id,LandlordVerificationStatus status,string? feedback,string actorId,string actorName){if(status==LandlordVerificationStatus.Rejected&&string.IsNullOrWhiteSpace(feedback))return OperationResult.Fail("Feedback is required when rejecting an application.");var app=await _apps.GetByIdAsync(id);if(app==null)return OperationResult.Fail("Application not found.");try{app.Status=status.ToString();app.AdminFeedback=status==LandlordVerificationStatus.Rejected?feedback:null;app.ReviewedAt=Timestamp.GetCurrentTimestamp();app.ReviewedBy=actorName;await _apps.UpdateAsync(id,app);await _users.UpdateFieldsAsync(id,new(){["landlordVerificationStatus"]=status.ToString()});await _logs.LogAsync(actorId,actorName,$"Set landlord status to {status}","LandlordApplication",id,feedback);return OperationResult.Ok();}catch(Exception ex){_logger.LogError(ex,"Landlord transition failed for {ApplicationId}",id);return OperationResult.Fail("The status change could not be saved.");}}
 public async Task<(Stream Stream,string ContentType,string FileName)?> DownloadDocumentAsync(string id,int index){var app=await _apps.GetByIdAsync(id);if(app==null||index<0||index>=app.Documents.Count)return null;var doc=app.Documents[index];var stream=await _storage.DownloadPrivateFileAsync(doc.StoragePath);var ext=Path.GetExtension(doc.FileName).ToLowerInvariant();return(stream,ext switch{".pdf"=>"application/pdf",".jpg" or ".jpeg"=>"image/jpeg",".png"=>"image/png",".webp"=>"image/webp",_=>"application/octet-stream"},doc.FileName);}
}
