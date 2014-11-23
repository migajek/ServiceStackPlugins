ServiceStackPlugins
===================


Per-Field auth
====

Plugin for removing (unsetting) DTO attributes depending on user role.
Support both black & white-listing, which means you can either mark specific fields to be removed when the user does not meet the requirements,
or restrict the whole DTO & white-list specific fields only.

#### Features

##### GlobalIgnors
Global ignores is the default white-list of field names to ignore when black-listing the whole DTO Imagine you have a field named GUID on almost every DTO which has to be ALWAYS available in order for the API to work properly. Now you restrict some of the DTOs with RestrictData attribute. Instead of explicitly white-listing GUID field on every of the DTOs, just add the "GUID" name to GlobalIgnores list.
You can still disable respecting GlobalIgnores list for a specific RestrictData attribute.

#### Custom extractors
Currently the PerFieldAuth doesn't follow object graph, which means it works on top-level DTO object only. In case your DTO has any nested objects you want to be stripped from properties, you need to register custom extractor which returns flattened enumerable of every object (including the root one).
See https://github.com/migajek/ServiceStackPlugins/blob/master/ServiceStackPlugins.Tests/PerFieldAuthTests.cs#L119

##### Limitations

 * works only as the response filter, not on the request (yet)
 * doesn't support permission checking, only role checking
 * does not follow the object graph yet, needs custom extractors

##### Usage

There are two separate packages:
* ServiceStackPlugins.Interfaces contains the attributes only
* ServiceStackPlugins.PerFieldAuth is the actual IPlugin implementation

Given you have the separate, dependency-free ServiceModel project, install the first package there, mark your DTOs as in the example code below.

Install ``ServiceStackPlugins.PerFieldAuth`` in your ServiceStack project and register it as follows:
```c#
Plugins.Add(new PerFieldAuthFeature());
```



##### Example code
 ```c#
    class PublicWithSecretFields
    {
        public string GUID { get; set; }
        public string Name { get; set; }

        [RestrictData("admin")]
        public string SecretForAdmin { get; set; }

        [RestrictData("@")] // @ is authenticated user, * is anyone
        public string EmailForLoggedIn { get; set; }
    }
 ```
