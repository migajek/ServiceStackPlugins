ServiceStackPlugins
===================


Per-Field auth
====

Plugin for removing (unsetting) DTO attributes depending on user role.
Support both black & white-listing, which means you can either mark specific fields to be removed when the user does not meet the requirements,
or restrict the whole DTO & white-list specific fields only.


Limitations:

 * works only as the response filter, not on the request (yet)
 * doesn't support permission checking, only role checking

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