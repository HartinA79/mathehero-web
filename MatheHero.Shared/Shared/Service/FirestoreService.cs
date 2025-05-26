using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using MatheHero.Shared.Helper;
using MatheHero.Shared.Models;
using MatheHero.Shared.Shared.Interface;
using MatheHero.Shared.Shared.Pages.Authentifizierung.Components.Registration;
using MatheHero.Shared.Shared.Pages.Authentifizierung.Components.Settings;

namespace MatheHero.Shared.Shared.Service
{
    public class FirestoreService : IFirestoreService
    {
        private readonly HttpClient _httpClient;

        private readonly string _projectId = "masuhe-ee95d";

        public FirestoreService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<T> GetDocumentByIdAsync<T>(string idToken, string collection, string documentId)
        {
            //var idToken = await _authService.GetIdTokenAsync();

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", idToken);

            var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/{collection}/{documentId}";

            var response = await _httpClient.GetAsync(url);

            response.EnsureSuccessStatusCode();

            var firestoreDoc = await response.Content.ReadFromJsonAsync<FirestoreDocument<T>>();

            return firestoreDoc.Fields;
        }

        public async Task<UserModel> GetUserByUserIdAsync(string idToken, string value)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", idToken);

            var query = new
            {
                structuredQuery = new
                {
                    from = new[] { new { collectionId = "users" } },
                    where = new
                    {
                        fieldFilter = new
                        {
                            field = new { fieldPath = "UserId" },
                            op = "EQUAL",
                            value = new { stringValue = value }
                        }
                    }
                }
            };

            var response = await _httpClient.PostAsJsonAsync(
                    $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents:runQuery",
                    query
                );

            if (!response.IsSuccessStatusCode)
                return new UserModel();

            response.EnsureSuccessStatusCode();

            var stream = await response.Content.ReadAsStreamAsync();
            var jsonDoc = await JsonDocument.ParseAsync(stream);
            var root = jsonDoc.RootElement;

            // Suche nach dem ersten Treffer
            var documentElement = root.EnumerateArray().FirstOrDefault(x => x.TryGetProperty("document", out _));

            if (documentElement.ValueKind == JsonValueKind.Undefined)
                return new UserModel();

            var document = documentElement.GetProperty("document");
            var namePath = document.GetProperty("name").GetString();

            var dokumentId = namePath?.Split('/').Last() ?? "";

            var fields = document.GetProperty("fields");

            UserModel user = new UserModel
            {
                DokumentId = dokumentId,
                AvatarPfad = fields.GetProperty("AvatarPfad").GetProperty("stringValue").GetString() ?? "",
                Email = fields.GetProperty("Email").GetProperty("stringValue").GetString() ?? "",
                ClassCode = fields.GetProperty("KlassenCode").GetProperty("stringValue").GetString() ?? "",
                Klassenstufe = int.Parse(fields.GetProperty("KlassenStufe").GetProperty("integerValue").GetString() ?? "0"),
                Rolle = fields.GetProperty("Rolle").GetProperty("stringValue").GetString() ?? "",
                SchulId = fields.GetProperty("SchulId").GetProperty("stringValue").GetString() ?? "",
                UserId = fields.GetProperty("UserId").GetProperty("stringValue").GetString() ?? "",
                UserName = fields.GetProperty("UserName").GetProperty("stringValue").GetString() ?? "",
            };

            return user;
        }

        public async Task<UserModel> GetUserByIdAsync(string idToken, string documentId)
        {
            //var idToken = await _authService.GetIdTokenAsync();

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", idToken);

            var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/users/{documentId}";

            var response = await _httpClient.GetAsync(url);

            response.EnsureSuccessStatusCode() ;

            var stream = await response.Content.ReadAsStreamAsync();
            var json = await JsonDocument.ParseAsync(stream);

            var user = new UserModel();

            if (json.RootElement.TryGetProperty("documents", out var docs))
            {
                foreach (var doc in docs.EnumerateArray())
                {
                    // Document ID aus dem "name"-Feld extrahieren
                    if (doc.TryGetProperty("name", out var nameProp))
                    {
                        var fullName = nameProp.GetString();
                        user.DokumentId = fullName?.Split('/').Last() ?? "";
                    }

                    if (doc.TryGetProperty("fields", out var fields))
                    {
                        user.AvatarPfad = GetString(fields, "AvatarPfad");
                        user.Email = GetString(fields, "Email");
                        user.ClassCode = GetString(fields, "KlassenCode");
                        user.Klassenstufe = GetInt(fields, "KlassenStufe");
                        user.Rolle = GetString(fields, "Rolle");
                        user.SchulId = GetString(fields, "SchulId");
                        user.UserId = GetString(fields, "UserId");
                        user.UserName = GetString(fields, "UserName");
                    }
                }
            }

            return user;
        }

        public async Task<List<AvatarModel>> GetAllAvatarPathsAsync()
        {
            var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/avatars";

            var response = await _httpClient.GetAsync(url);
            
            response.EnsureSuccessStatusCode();

            var stream = await response.Content.ReadAsStreamAsync();
            var json = await JsonDocument.ParseAsync(stream);

            var avatarList = new List<AvatarModel>();

            if (json.RootElement.TryGetProperty("documents", out var docs))
            {
                foreach (var doc in docs.EnumerateArray())
                {
                    var avatar = new AvatarModel();

                    if (doc.TryGetProperty("name", out var nameProp))
                    {
                        var name = nameProp.GetString();
                        avatar.DocumentId = name?.Split('/').Last() ?? "";
                    }

                    if (doc.TryGetProperty("fields", out var fields) && fields.TryGetProperty("link", out var linkField) && linkField.TryGetProperty("stringValue", out var linkValue))
                    {
                        avatar.Link = linkValue.GetString() ?? "";
                    }

                    avatarList.Add(avatar);
                }
            }

            return avatarList;
        }

        public async Task<(List<SchuleModel> Schulen, string LastName)> GetSchoolsPageAsync(string? startAfterName = null, int pageSize = 100)
        {
            var schools = new List<SchuleModel>();

            var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/schools?pageSize={pageSize}&orderBy=fields.Name.stringValue";

            if (!string.IsNullOrEmpty(startAfterName))
            {
                // Beachte: stringValue muss in doppelte Anführungszeichen für Firestore-Strings
                url += $"&startAfter={Uri.EscapeDataString($"\"{startAfterName}\"")}";
            }

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync();
            var json = await JsonDocument.ParseAsync(stream);

            string? lastName = null;

            if (json.RootElement.TryGetProperty("documents", out var docs))
            {
                foreach (var doc in docs.EnumerateArray())
                {
                    var schule = new SchuleModel();

                    if (doc.TryGetProperty("name", out var nameProp))
                        schule.DocumentId = nameProp.GetString()?.Split('/').Last() ?? "";

                    if (doc.TryGetProperty("fields", out var fields))
                    {
                        schule.Name = GetString(fields, "Name");
                        schule.Adresse = GetString(fields, "Adresse");
                        schule.Land = GetString(fields, "Land");
                        schule.Ort = GetString(fields, "Ort");
                        schule.Region = GetString(fields, "Region");
                        schule.SchulNummer = GetInt(fields, "SchulNummer");
                        schule.ZipCode = GetInt(fields, "ZipCode");
                    }

                    schools.Add(schule);
                    lastName = schule.Name; // Merke den letzten Namen für die nächste Seite
                }
            }

            return (schools, lastName ?? "");
        }

        public async Task<List<LandModel>> GetAllCountriesByLanguageAsync(string language)
        {
            var countries = new List<LandModel>();

            var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/countries";

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var stream = await response.Content.ReadAsStreamAsync();
            var json = await JsonDocument.ParseAsync(stream);

            if (json.RootElement.TryGetProperty("documents", out var docs))
            {
                foreach (var doc in docs.EnumerateArray())
                {
                    var LandModel = new LandModel();

                    if (doc.TryGetProperty("name", out var docId))
                    {
                        var fullId = docId.GetString();
                        LandModel.DokumentId = fullId?.Split('/').Last() ?? "";
                    }
                    if (doc.TryGetProperty("fields", out var fields))
                    {
                        switch (language)
                        {
                            case "de-DE":
                                LandModel.Name = GetString(fields, "nameDE");
                                break;
                            case "en-US":
                                LandModel.Name = GetString(fields, "nameEN");
                                break;
                            case "it-IT":
                                LandModel.Name = GetString(fields, "nameIT");
                                break;
                        }
                    }

                    countries.Add(LandModel);
                }
            }

            return countries;
        }

        public async Task<List<SchuleModel>> GetSchoolsByCountryAndRegionAsync(string countryId, string regionId)
        {
            var schools = new List<SchuleModel>();

            var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents:runQuery";

            var query = new
            {
                structuredQuery = new
                {
                    from = new[]
                    {
                        new { collectionId = "schools" }
                    },  
                    where = new
                    {
                        compositeFilter = new
                        {
                            op = "AND",
                            filters = new[]
                            {
                                new
                                {
                                    fieldFilter = new
                                    {
                                        field = new { fieldPath = "countryId" },
                                        op = "EQUAL",
                                        value = new { stringValue = countryId }
                                    }
                                },
                                new
                                {
                                    fieldFilter = new
                                    {
                                        field = new { fieldPath = "regionId" },
                                        op = "EQUAL",
                                        value = new { stringValue = regionId }
                                    }
                                }
                            }
                        }
                    }
                }
            };

            var content = new StringContent(JsonSerializer.Serialize(query), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            var stream = await response.Content.ReadAsStreamAsync();
            var json = await JsonDocument.ParseAsync(stream);

            foreach (var element in json.RootElement.EnumerateArray())
            {
                if (element.TryGetProperty("document", out var doc))
                {
                    var school = new SchuleModel();

                    if (doc.TryGetProperty("name", out var nameProp))
                    {
                        var fullName = nameProp.GetString();
                        school.DocumentId = fullName?.Split('/').Last() ?? "";

                        if (doc.TryGetProperty("fields", out var fields))
                        {
                            school.Name = GetString(fields, "name");
                            school.Adresse = GetString(fields, "address");
                            school.Land = GetString(fields, "country");
                            school.Ort = GetString(fields, "location");
                            school.Region = GetString(fields, "region");
                            school.SchulNummer = GetInt(fields, "schoolNumber");
                            school.ZipCode = GetInt(fields, "zipCode");
                        }
                    }

                    schools.Add(school);
                }
            }

            return schools;
        }

        public async Task<List<RegionModel>> GetAllRegionsByCountryAsync(string idCountry, string language)
        {
            var regions = new List<RegionModel>();

            var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents:runQuery";

            var query = new
            {
                structuredQuery = new
                {
                    from = new[]
                    {
                        new { collectionId = "regions" }
                    },
                    where = new
                    {
                        fieldFilter = new
                        {
                            field = new { fieldPath = "countryID" },
                            op = "EQUAL",
                            value = new { stringValue = idCountry }
                        }
                    }
                }
            };

            var content = new StringContent(JsonSerializer.Serialize(query), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            var stream = await response.Content.ReadAsStreamAsync();
            var json = await JsonDocument.ParseAsync(stream);

            foreach (var element in json.RootElement.EnumerateArray())
            {
                if (element.TryGetProperty("document", out var doc))
                {
                    var region = new RegionModel();

                    // Document ID extrahieren
                    if (doc.TryGetProperty("name", out var nameProp))
                    {
                        var fullName = nameProp.GetString();
                        region.DokumentId = fullName?.Split('/').Last() ?? "";
                    }

                    if (doc.TryGetProperty("fields", out var fields))
                    {
                        region.LandId = GetString(fields, "countryID");

                        switch (language)
                        {
                            case "de-DE":
                                region.Name = GetString(fields, "nameDE");
                                break;
                            case "en-US":
                                region.Name = GetString(fields, "nameEN");
                                break;
                            case "it-IT":
                                region.Name = GetString(fields, "nameIT");
                                break;
                        }
                    }

                    regions.Add(region);
                }
            }

            return regions;
        }

        public async Task<List<SchuleModel>> GetAllSchoolsAsync()
        {
            var schools = new List<SchuleModel>();

            
            var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/schools?pageSize=1000";

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var stream = await response.Content.ReadAsStreamAsync();
            var json = await JsonDocument.ParseAsync(stream);

            if (json.RootElement.TryGetProperty("documents", out var docs))
            {
                foreach (var doc in docs.EnumerateArray())
                {
                    var schule = new SchuleModel();

                    // Document ID aus dem "name"-Feld extrahieren
                    if (doc.TryGetProperty("name", out var nameProp))
                    {
                        var fullName = nameProp.GetString();
                        schule.DocumentId = fullName?.Split('/').Last() ?? "";
                    }

                    if (doc.TryGetProperty("fields", out var fields))
                    {
                        schule.Name = GetString(fields, "Name");
                        schule.Adresse = GetString(fields, "Adresse");
                        schule.Land = GetString(fields, "Land");
                        schule.Ort = GetString(fields, "Ort");
                        schule.Region = GetString(fields, "Region");
                        schule.SchulNummer = GetInt(fields, "SchulNummer");
                        schule.ZipCode = GetInt(fields, "ZipCode");
                    }

                    schools.Add(schule);
                }
            }

            return schools;
        }

        public async Task<List<ExerciseModel>> GetAllExerciseCategoriesAsync()
        {
            var categories = new List<ExerciseModel>();

            var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/category";

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var stream = await response.Content.ReadAsStreamAsync();
            var json = await JsonDocument.ParseAsync(stream);

            if (json.RootElement.TryGetProperty("documents", out var docs))
            {
                foreach (var doc in docs.EnumerateArray())
                {
                    var cat = new ExerciseModel();

                    if (doc.TryGetProperty("name", out var nameProp))
                    {
                        var fullName = nameProp.GetString();
                        cat.DokumentId = fullName?.Split('/').Last() ?? "";
                    }

                    if (doc.TryGetProperty("fields", out var fields))
                    {
                        cat.Name = GetString(fields, "name");
                        cat.Index = GetInt(fields, "index");
                        cat.Icon = GetString(fields, "icon");

                        if (fields.TryGetProperty("createdAt", out var createdAtProp) &&
                            createdAtProp.TryGetProperty("timestampValue", out var timestamp))
                        {
                            cat.CreatedAt = DateTime.Parse(timestamp.GetString() ?? "");
                        }
                    }

                    categories.Add(cat);
                }
            }

            var urlSubcategories = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/subcategory";

            var responseSubcategories = await _httpClient.GetAsync(urlSubcategories);
            responseSubcategories.EnsureSuccessStatusCode();

            var streamSubcategories = await responseSubcategories.Content.ReadAsStreamAsync();
            var jsonSubcategories = await JsonDocument.ParseAsync(streamSubcategories);

            var allSubcategories = new List<SubcategoryModel>();

            if (jsonSubcategories.RootElement.TryGetProperty("documents", out var docsSub))
            {
                foreach (var doc in docsSub.EnumerateArray())
                {
                    var sub = new SubcategoryModel();

                    if (doc.TryGetProperty("name", out var nameProp))
                    {
                        var fullName = nameProp.GetString();
                        sub.DokumentId = fullName?.Split('/').Last() ?? "";
                    }

                    if (doc.TryGetProperty("fields", out var fields))
                    {
                        var categoryId = GetString(fields, "categoryId");

                        sub.Name = GetString(fields, "name");
                        sub.Index = GetInt(fields, "index");

                        var matchingCategory = categories.FirstOrDefault(c => c.DokumentId == categoryId);
                        if (matchingCategory != null)
                        {
                            matchingCategory.Subcategories.Add(sub);
                        }
                    }
                }
            }

            foreach (var category in categories)
            {
                category.Subcategories = category.Subcategories.OrderBy(s => s.Index).ToList();
            }

            return categories;
        }

        public async Task<SchuleModel> GetSchoolByIdAsync(string idToken, string schulId)
        {
            var schule = new SchuleModel();

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", idToken);

            var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/schools/{schulId}";

            var response = await _httpClient.GetAsync(url);

            response.EnsureSuccessStatusCode();

            var stream = await response.Content.ReadAsStreamAsync();
            var json = await JsonDocument.ParseAsync(stream);

            var root = json.RootElement;

            if (root.TryGetProperty("name", out var nameProp))
            {
                var fullName = nameProp.GetString();
                schule.DocumentId = fullName?.Split('/').Last() ?? "";
            }

            if (root.TryGetProperty("fields", out var fields))
            {
                schule.Name = GetString(fields, "name");
                schule.Adresse = GetString(fields, "address");
                schule.Land = GetString(fields, "country");
                schule.Ort = GetString(fields, "location");
                schule.Region = GetString(fields, "region");
                schule.SchulNummer = GetInt(fields, "schoolNumber");
                schule.ZipCode = GetInt(fields, "zipCode");
            }

            return schule;
        }

        public async Task<string> GetClassByIdAsync(string idToken, string classId)
        {
            string className = "";

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", idToken);

            var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/classes/{classId}";

            var response = await _httpClient.GetAsync(url);

            response.EnsureSuccessStatusCode();

            var stream = await response.Content.ReadAsStreamAsync();
            var json = await JsonDocument.ParseAsync(stream);

            var root = json.RootElement;

            if (root.TryGetProperty("fields", out var fields))
            {
                className = GetString(fields, "name");
            }

            return className;
        }

        public async Task<List<T>> GetDocumentByFieldAsync<T>(string idToken, string collection, string field, string value)
        {
            //var idToken = await _authService.GetIdTokenAsync();

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", idToken);

            var query = new
            {
                structuredQuery = new
                {
                    from = new[] { new { collectionId = collection } },
                    where = new
                    {
                        fieldFilter = new
                        {
                            field = new { fieldPath = field },
                            op = "EQUAL",
                            value = new { stringValue = value }
                        }
                    }
                }
            };

            var response = await _httpClient.PostAsJsonAsync(
                    $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents:runQuery",
                    query
                );

            response.EnsureSuccessStatusCode();
            var results = await response.Content.ReadFromJsonAsync<List<FirestoreQueryResult<T>>>();

            return results
                .Where(r => r.Document != null)
                .Select(r => r.Document.Fields)
                .ToList();
        }

        public async Task<FirestoreResult> AddNewUser(UserModel user)
        {
            var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/users";

            var payload = new
            {
                fields = new Dictionary<string, object>
                {
                    { "AvatarPfad", new { stringValue = user.AvatarPfad ?? "" } },
                    { "Email", new { stringValue = user.Email ?? "" } },
                    { "KlassenCode", new { stringValue = user.ClassCode ?? "" } },
                    { "KlassenStufe", new { integerValue = user.Klassenstufe } },
                    { "Rolle", new { stringValue = user.Rolle ?? "" } },
                    { "SchulId", new { stringValue = user.SchulId ?? "" } },
                    { "UserId", new { stringValue = user.UserId ?? "" } },
                    { "UserName", new { stringValue = user.UserName ?? "" } },
                    { "LizenzId", new { stringValue = user.LizenzId ?? "" } },
                },
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync(url, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    var error = JsonSerializer.Deserialize<JsonElement>(responseBody);
                    var errorMessage = error.GetProperty("error").GetProperty("message").GetString();
                    return new FirestoreResult
                    {
                        Success = false,
                        Message = $"AddNewUser: {errorMessage}"
                    };
                }

                var result = JsonSerializer.Deserialize<JsonElement>(responseBody);
                var fullDocName = result.GetProperty("name").GetString();
                var docId = fullDocName?.Split('/').Last();

                return new FirestoreResult
                {
                    Success = true,
                    Message = "Dokument erfolgreich erstellt",
                    DocumentId = docId
                };
            }
            catch (Exception ex)
            {
                return new FirestoreResult
                {
                    Success = false,
                    Message = $"{ex.Message}"
                };
            }
        }

        public async Task<FirestoreResult> CreateUserAsync(UserModel newUser)
        {
            var userDoc = new Dictionary<string, object>
            {
                { "avatarPfad", newUser.AvatarPfad },
                { "email", newUser.Email },
                { "klassenstufe", newUser.Klassenstufe },
                { "rolle", newUser.Rolle },
                { "schulId", newUser.SchulId },
                { "classCode", newUser.ClassCode },
                { "userId", newUser.UserId },
                { "userName", newUser.UserName },
                { "lizenzId", newUser.LizenzId },
                { "progress", FirestoreHelper.ToFirestoreMap(newUser.Progress) }
            };

            try
            {
                var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/users/{newUser.UserId}";

                var content = new StringContent(JsonSerializer.Serialize(new { fields = FirestoreHelper.ToFirestoreFields(userDoc) }), Encoding.UTF8, "application/json");
                var response = await _httpClient.PatchAsync(url, content);
                response.EnsureSuccessStatusCode();

                return new FirestoreResult
                {
                    Success = true,
                    Message = "Erfolgreich erstellt"
                };
            } 
            catch (Exception ex)
            {
                return new FirestoreResult
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<FirestoreResult> UpdateUserAsync(string idToken, UserModel user)
        {
            var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/users/{user.DokumentId}?updateMask.fieldPaths=AvatarPfad&updateMask.fieldPaths=UserName&updateMask.fieldPaths=KlassenStufe&updateMask.fieldPaths=SchulId";


            var payload = new
            {
                fields = new Dictionary<string, object>
                {
                    { "AvatarPfad", new { stringValue = user.AvatarPfad ?? "" } },
                    { "UserName", new { stringValue = user.UserName ?? "" } },
                    { "KlassenStufe", new {integerValue = user.Klassenstufe} },
                    { "SchulId", new {stringValue = user.SchulId ?? ""} }
                },
             };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Patch, url);
                request.Headers.Add("Authorization", $"Bearer {idToken}");
                request.Content = content;

                var response = await _httpClient.SendAsync(request);

                //var response = await _httpClient.PutAsync(url, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    var error = JsonSerializer.Deserialize<JsonElement>(responseBody);
                    var errorMessage = error.GetProperty("error").GetProperty("message").GetString();
                    return new FirestoreResult
                    {
                        Success = false,
                        Message = $"UpdateUser: {errorMessage}"
                    };
                }

                //var result = JsonSerializer.Deserialize<JsonElement>(responseBody);
                //var fullDocName = result.GetProperty("name").GetString();
                //var docId = fullDocName?.Split('/').Last();

                return new FirestoreResult
                {
                    Success = true,
                    Message = "User erfolgreich aktualisiert",
                    DocumentId = user.DokumentId
                };
            }
            catch (Exception ex)
            {
                return new FirestoreResult
                {
                    Success = false,
                    Message = $"{ex.Message}"
                };
            }
        }

        public async Task<FirestoreResult> JoinClassAsync(string userId, string classCode)
        {
            try
            {
                // Prüfen, ob der Klassencode existiert
                var classUrl = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/classes/{classCode}";

                var response = await _httpClient.GetAsync(classUrl);
                var json = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return new FirestoreResult
                    {
                        Success = false,
                        Message = "Klasse nicht gefunden."
                    };
                }

                var jsonDoc = JsonDocument.Parse(json);
                var root = jsonDoc.RootElement;

                var classId = root.GetProperty("name").GetString()?.Split('/').Last() ?? "";

                // Update des Users
                var userUpdateUrl = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/users/{userId}?updateMask.fieldPaths=KlassenCode";
                var updatePayload = new
                {
                    fields = new Dictionary<string, object>
                {
                    { "KlassenCode", new { stringValue = classId } }
                }
                };

                var updateJson = JsonSerializer.Serialize(updatePayload);
                var updateContent = new StringContent(updateJson, Encoding.UTF8, "application/json");
                var updateRequest = new HttpRequestMessage(new HttpMethod("PATCH"), userUpdateUrl)
                {
                    Content = updateContent
                };

                var updateResponse = await _httpClient.SendAsync(updateRequest);

                if (!updateResponse.IsSuccessStatusCode)
                {
                    var error = await updateResponse.Content.ReadAsStringAsync();
                    return new FirestoreResult
                    {
                        Success = false,
                        Message = $"Fehler beim Aktualisieren des Benutzers: {error}"
                    };
                }

                return new FirestoreResult
                {
                    Success = true,
                    Message = "Erfolgreich beigetreten."
                };
            }
            catch (Exception ex)
            {
                return new FirestoreResult
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<FirestoreResult> CreateClassAsync(ClassModel newClass)
        {
            var classDoc = new Dictionary<string, object>
            {
                { "name", newClass.Name },
                { "teacherId", newClass.TeacherId },
                { "createdAt", new Dictionary<string, object> { { "timestampValue", newClass.CreatedAt.ToUniversalTime().ToString("o") } } },
                { "unlockedCategories", FirestoreHelper.ToFirestoreArray(newClass.UnlockedCategories) },
                { "unlockedSubcategories", FirestoreHelper.ToFirestoreArray(newClass.UnlockedSubcategories) }
            };

            try
            {
                var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/classes/{newClass.DokumentId}";

                var content = new StringContent(JsonSerializer.Serialize(new { fields = FirestoreHelper.ToFirestoreFields(classDoc) }), Encoding.UTF8, "application/json");
                var response = await _httpClient.PatchAsync(url, content);

                response.EnsureSuccessStatusCode();

                return new FirestoreResult
                {
                    Success = true,
                    Message = "Erfolgreich erstellt"
                };
            }
            catch (Exception ex)
            {
                return new FirestoreResult
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        public async Task SetDocumentAsync<T>(string idToken, string collection, string documentId, T data)
        {
            //var idToken = await _authService.GetIdTokenAsync();

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", idToken);

            var firestoreData = new { fields = data };

            var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/{collection}/{documentId}";

            var response = await _httpClient.PatchAsync(
                url,
                JsonContent.Create(firestoreData)
            );

            response.EnsureSuccessStatusCode();
        }

        public async Task UpdateDocumentAsync(string idToken, string collection, string documentId, Dictionary<string, object> fieldsToUpdate)
        {
            //var idToken = await _authService.GetIdTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", idToken);

            var fields = new Dictionary<string, object>();
            foreach (var kvp in fieldsToUpdate)
            {
                // Firestore verlangt typisierte Werte – hier nur string als Beispiel
                fields[kvp.Key] = new Dictionary<string, object>
                {
                    { "stringValue", kvp.Value?.ToString() }
                };
            }

            var body = new Dictionary<string, object> { { "fields", fields } };

            var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/{collection}/{documentId}";
            var response = await _httpClient.PatchAsJsonAsync(url, body);
            response.EnsureSuccessStatusCode();
        }

        public async Task DeleteDocumentAsync(string idToken, string collection, string documentId)
        {
            //var idToken = await _authService.GetIdTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", idToken);

            var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/{collection}/{documentId}";
            var response = await _httpClient.DeleteAsync(url);

            response.EnsureSuccessStatusCode();
        }

        private string GetString(JsonElement fields, string fieldName)
        {
            if (fields.TryGetProperty(fieldName, out var val) &&
                val.TryGetProperty("stringValue", out var strVal))
            {
                return strVal.GetString() ?? "";
            }
            return "";
        }

        private int GetInt(JsonElement fields, string fieldName)
        {
            if (fields.TryGetProperty(fieldName, out var val) &&
                val.TryGetProperty("integerValue", out var intVal) &&
                int.TryParse(intVal.GetString(), out var result))
            {
                return result;
            }
            return 0;
        }

    }
}
