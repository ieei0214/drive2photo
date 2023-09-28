using CasCap.Models;
using CasCap.Services;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using File = System.IO.File;

namespace drive2photo;

public class Drive2Photo
{
    private string[] _scopes =
    {
        DriveService.Scope.DriveReadonly,
    };
    private string _applicationName = "Drive to Photos Uploader";

    private UserCredential credential;
    private DriveService _driveService;
    private GooglePhotosService _googlePhotosSvc;

    string _user = null;//e.g. "your.email@mydomain.com";
    string _clientId = null;//e.g. "012345678901-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.apps.googleusercontent.com";
    string _clientSecret = null;//e.g. "abcabcabcabcabcabcabcabc";
    const string _testFolder = "d:/temp/GooglePhotos/";
    const string _credentialsJsonFile = "credentials-oauth.json";

    public async Task Run(string pathName)
    {
        credential = getUserCredential(_credentialsJsonFile);

        await initPhotosSvc();

        _driveService = new DriveService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential,
            ApplicationName = _applicationName,
        });

        var listRequest = _driveService.Files.List();
        listRequest.Fields = "nextPageToken, files(id, name)";
        listRequest.Q = $"name = '{pathName}' and mimeType = 'application/vnd.google-apps.folder'";
        var fileList = await listRequest.ExecuteAsync();

        // create targetDir folder in d:/temp/GooglePhotos
        var dTempGooglephotos = _testFolder + pathName;
        Directory.CreateDirectory(dTempGooglephotos);
        if (fileList.Files.Count != 1)
        {
            Console.WriteLine($"More than one folder found. {fileList.Files.Count}");
            return;
        }

        if (fileList.Files != null && fileList.Files.Count == 1)
        {
            // get the target folder id
            var targetFolderId = fileList.Files[0].Id;
            // get the list of files in the target folder
            listRequest.Q = $"'{targetFolderId}' in parents";
            // mimeType should be image and video
            listRequest.Q += " and (mimeType contains 'image/' or mimeType contains 'video/')";
            fileList = listRequest.Execute();
            if (fileList.Files != null && fileList.Files.Count > 0)
            {
                var albumId = await GetAlbum(pathName);
                // git the string from albumId

                foreach (var file in fileList.Files)
                {
                    // 1. Get the file from Google Drive
                    var request = _driveService.Files.Get(file.Id);
                    // download to file named dTempGooglephotosFile
                    var dTempGooglephotosFile = dTempGooglephotos + "/" + file.Name;
                    using (var stream = new FileStream(dTempGooglephotosFile, FileMode.Create, FileAccess.Write))
                    {
                        request.Download(stream);
                    }

                    try
                    {
                        await _googlePhotosSvc.UploadSingle(dTempGooglephotosFile, albumId);
                    }
                    catch (Exception e)
                    {
                        // move the file to d:/temp/GooglePhotos/failed
                        var dTempGooglephotosFailed = dTempGooglephotos + "/failed";
                        Directory.CreateDirectory(dTempGooglephotosFailed);
                        var dTempGooglephotosFailedFile = dTempGooglephotosFailed + "/" + file.Name;
                        File.Move(dTempGooglephotosFile, dTempGooglephotosFailedFile);
                    }
                }
            }
            else
            {
                Console.WriteLine("No files found.");
            }
        }
        else
        {
            Console.WriteLine("No folder found.");
        }
    }

    private async Task initPhotosSvc()
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            //builder.AddConfiguration(configuration.GetSection("Logging")).AddDebug().AddConsole();
        });
        var logger = loggerFactory.CreateLogger<GooglePhotosService>();

        var options = new GooglePhotosOptions
        {
            User = _user,
            ClientId = _clientId,
            ClientSecret = _clientSecret,
            //FileDataStoreFullPathOverride = _testFolder,
            Scopes = new[] { GooglePhotosScope.Access, GooglePhotosScope.Sharing }, //Access+Sharing == full access
        };

        //4) create a single HttpClient which will be pooled and re-used by GooglePhotosService
        var handler = new HttpClientHandler
            { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate };
        var client = new HttpClient(handler) { BaseAddress = new Uri(options.BaseAddress) };

        //5) new-up the GooglePhotosService passing in the previous references (in lieu of dependency injection)
        _googlePhotosSvc = new GooglePhotosService(logger, Options.Create(options), client);

        //6) log-in
        if (!await _googlePhotosSvc.LoginAsync()) throw new Exception($"login failed!");
    }

    private async Task<string> GetAlbum(string albumTitle)
    {
        var album = await _googlePhotosSvc.GetOrCreateAlbumAsync(albumTitle);
        if (album is null) throw new Exception("album creation failed!");
        Console.WriteLine($"{nameof(album)} '{album.title}' id is '{album.id}'");
        return album.id;
    }

    private UserCredential getUserCredential(string credentialJson)
    {
        UserCredential credential;
        using (var stream = new FileStream(credentialJson, FileMode.Open, FileAccess.Read))
        {
            string credPath = "token.json"; // This is where the tokens will be stored after the first authorization
            credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                GoogleClientSecrets.Load(stream).Secrets,
                _scopes,
                "user", // User identifier
                CancellationToken.None,
                new FileDataStore(credPath, true)).Result;
        }
        
        return credential;
    }
}