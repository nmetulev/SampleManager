# SampleManager

To add the sample manager to your showcase/sample app, add the following code to your **App.xaml.cs** OnActivated/OnActivated event

``` csharp
SampleManager.Sample.Instance.Init(localDescriptionFilename: "LOCAL_MARKDOWN_FILE (required)", 
                                   remoteDescriptionUri: "REMOTE_MARKDOWN_FILE (optional)", 
                                   gaTrackerId: "GOOGLE_ANALYTICS_TRACKING_ID (optional)");
```

For a sample description file, see https://github.com/Microsoft/uwp-experiences/blob/news/apps/News/description.md
