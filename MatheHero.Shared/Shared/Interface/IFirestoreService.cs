using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MatheHero.Shared.Helper;
using MatheHero.Shared.Models;

namespace MatheHero.Shared.Shared.Interface
{
    public interface IFirestoreService
    {
        Task<T> GetDocumentByIdAsync<T>(string idToken, string collection, string documentId);

        Task<UserModel> GetUserByIdAsync(string idToken, string documentId);

        Task<UserModel> GetUserByUserIdAsync(string idToken, string value);

        Task<List<LandModel>> GetAllCountriesByLanguageAsync(string language);

        Task<List<RegionModel>> GetAllRegionsByCountryAsync(string country, string language);

        Task<List<AvatarModel>> GetAllAvatarPathsAsync();

        Task<List<SchuleModel>> GetSchoolsByCountryAndRegionAsync(string countryId, string regionId);

        Task<List<SchuleModel>> GetAllSchoolsAsync();

        Task<List<ExerciseModel>> GetAllExerciseCategoriesAsync();

        Task<SchuleModel> GetSchoolByIdAsync(string idToken, string schulId);

        Task<string> GetClassByIdAsync(string idToken, string classId);

        Task<(List<SchuleModel> Schulen, string LastName)> GetSchoolsPageAsync(string? startAfterName = null, int pageSize = 100);

        Task<FirestoreResult> AddNewUser(UserModel user);

        Task<FirestoreResult> UpdateUserAsync(string idToken, UserModel user);

        Task<FirestoreResult> JoinClassAsync(string userId, string classCode);

        Task<List<T>> GetDocumentByFieldAsync<T>(string idToken, string collection, string field, string value);

        Task SetDocumentAsync<T>(string idToken, string collection, string documentId, T data);

        Task UpdateDocumentAsync(string idToken, string collection, string documentId, Dictionary<string, object> fieldsToUpdate);

        Task DeleteDocumentAsync(string idToken, string collection, string documentId);
    }
}
