using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using MatheHero.Shared.Helper;
using MatheHero.Shared.Models;
using MatheHero.Shared.Shared.Interface;

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

            var fields = documentElement.GetProperty("document").GetProperty("fields");

            UserModel user = new UserModel
            {
                AvatarPfad = fields.GetProperty("AvatarPfad").GetProperty("stringValue").GetString() ?? "",
                Email = fields.GetProperty("Email").GetProperty("stringValue").GetString() ?? "",
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
