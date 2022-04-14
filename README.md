# Building a DotNet client to make REST API calls to the SDS Service

| :loudspeaker: **Notice**: Samples have been updated to reflect that they work on AVEVA Data Hub. The samples also work on OSIsoft Cloud Services unless otherwise noted. |
| -----------------------------------------------------------------------------------------------|  

**Version:** 1.1.23

[![Build Status](https://dev.azure.com/osieng/engineering/_apis/build/status/product-readiness/ADH/aveva.sample-adh-time_series-dotnet?branchName=main)](https://dev.azure.com/osieng/engineering/_build/latest?definitionId=2623&branchName=main)

The sample code in this topic demonstrates how to invoke the ADH client library. By examining the code, you will see how to create an SdsType and SdsStream, and how to create, read, update, and delete values in SDS. You will also see the summaries value call, and how to do bulk streams calls.

When working in .NET, it is recommended that you use the ADH Client Libraries metapackage, OSIsoft.OCSClients. The metapackage is a NuGet package available from [https://api.nuget.org/v3/index.json](https://api.nuget.org/v3/index.json). The libraries offer a framework of classes that make client development easier.

[SDS documentation](https://ocs-docs.osisoft.com/Content_Portal/Documentation/SequentialDataStore/Data_Store_and_SDS.html)

Developed against DotNet 5.0.

## Getting Started

In this example we assume that you have the dotnet core CLI.

To run this example from the commandline run

```shell
cd SDS_TS_DotNet
dotnet restore
dotnet run
```

to test this program change directories to the test and run

```shell
cd SDS_TS_DotNetTests
dotnet restore
dotnet test
```

## Configure constants for connecting and authentication

The sample is configured using the file [appsettings.placeholder.json](SdsTsDotNet/appsettings.placeholder.json). Before editing, rename this file to `appsettings.json`. This repository's `.gitignore` rules should prevent the file from ever being checked in to any fork or branch, to ensure credentials are not compromised.

The SDS Service is secured by obtaining tokens from Azure Active Directory. Such clients provide a client application Id and an associated secret (or key) that are authenticated against the directory. You must replace the placeholders in the `appsettings.json` file with the authentication-related values you received from AVEVA.

```json
{
  "NamespaceId": "PLACEHOLDER_REPLACE_WITH_NAMESPACE_ID",
  "TenantId": "PLACEHOLDER_REPLACE_WITH_TENANT_ID",
  "Resource": "https://uswe.datahub.connect.aveva.com",
  "ClientId": "PLACEHOLDER_REPLACE_WITH_CLIENT_ID",
  "ClientSecret": "PLACEHOLDER_REPLACE_WITH_CLIENT_SECRET"
}
```

## Try .NET

This sample is also provided in Try .NET. Currently using this in Try .NET is available only by self-hosting it.

To get to the Try .MD file that walks over the sample click here:

- [Sample](SdsTsDotNet/SAMPLE.md)

To learn more about Try .NET and to learn how to install and use it please see [Try .NET](https://github.com/dotnet/try). Included below are the abbreviated steps to do this.

1. from anywhere `dotnet tool install --global dotnet-try --version 1.0.19264.11`
2. from this directory run `dotnet try`

Note: you do not have to use this sample in Try .NET. It will work like any other dotnet application.

---

For the main ADH time series samples page [ReadMe](https://github.com/osisoft/OSI-Samples-OCS/blob/main/docs/SDS_TIME_SERIES.md)  
For the main ADH samples page [ReadMe](https://github.com/osisoft/OSI-Samples-OCS)  
For the main AVEVA samples page [ReadMe](https://github.com/osisoft/OSI-Samples)
