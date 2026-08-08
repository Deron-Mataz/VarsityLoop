using Google.Cloud.Firestore;
using VarsityLoop.Models.Entities;
using VarsityLoop.Repositories.Interfaces;
namespace VarsityLoop.Repositories.Implementations;
public class LandlordApplicationRepository : FirestoreRepository<LandlordApplication>, ILandlordApplicationRepository { private readonly FirestoreDb _db; private const string Collection="LandlordApplications"; public LandlordApplicationRepository(FirestoreDb db):base(db,Collection)=>_db=db; public async Task<List<LandlordApplication>> GetAllApplicationsAsync(){var s=await _db.Collection(Collection).GetSnapshotAsync();return s.Documents.Select(d=>d.ConvertTo<LandlordApplication>()).OrderByDescending(a=>a.SubmittedAt).ToList();} }
