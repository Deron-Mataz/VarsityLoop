using VarsityLoop.Models.Entities;
namespace VarsityLoop.Repositories.Interfaces;
public interface ILandlordApplicationRepository : IFirestoreRepository<LandlordApplication> { Task<List<LandlordApplication>> GetAllApplicationsAsync(); }
