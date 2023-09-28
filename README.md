# drive2photo

A simple tool to download all photos from Google Drive, and then upload to Google Photos.

It will create a folder named as the folder name in Google Drive, and then upload all photos in the new Album to Google Photos.

Very draft version. Use at your own risk.

# 2 Packages

- [CasCap.Apis.GooglePhotos] (https://github.com/f2calv/CasCap.Apis.GooglePhotos)
```
    string _user = null;//e.g. "your.email@mydomain.com";
    string _clientId = null;//e.g. "012345678901-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.apps.googleusercontent.com";
    string _clientSecret = null;//e.g. "abcabcabcabcabcabcabcabc";
```
- [Google.Apis.Drive.v3/] (https://www.nuget.org/packages/Google.Apis.Drive.v3/)

Get the json from [Google API Console] (https://console.developers.google.com/apis/credentials)
```
    const string _credentialsJsonFile = "credentials-oauth.json";
```

# Setup Download Path

```
    const string _testFolder = "d:/temp/GooglePhotos/";
```
