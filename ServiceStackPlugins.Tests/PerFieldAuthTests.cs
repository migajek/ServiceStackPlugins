using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStackPlugins.Interfaces;
using ServiceStackPlugins.Interfaces.Auth;
using ServiceStackPlugins.PerFieldAuth;

namespace ServiceStackPlugins.Tests
{
    internal interface IDTOBase
    {
        string GUID { get; set; }
        string Name { get; set; }        
        string SecretForAdmin { get; set; }        
        string EmailForLoggedIn { get; set; }
    }
    internal class PublicIdtoWithSecretField : IDTOBase
    {
        public string GUID { get; set; }
        public string Name { get; set; }

        [RestrictData("admin")]
        public string SecretForAdmin { get; set; }

        [RestrictData("@")]
        public string EmailForLoggedIn { get; set; }
    }

    [RestrictData("admin")]
    internal class PrivateIdtoWithWhiteListedName : IDTOBase
    {
        public string GUID { get; set; }

        [PermitField("*")]
        public  string Name { get; set; }

        [PermitField("admin")]
        public string SecretForAdmin { get; set; }

        [PermitField("@")]
        public  string EmailForLoggedIn { get; set; }
    }


    internal class ListResponse
    {
        public IEnumerable<IDTOBase> Dtos { get; set; }
    }
    [TestFixture]
    public class PerFieldAuthTests
    {

        private IDTOBase[] GetDTOs()
        {
            return new IDTOBase[]
            {
                new PublicIdtoWithSecretField()
                {
                    EmailForLoggedIn = "email",
                    GUID = "abc",
                    Name = "first dto",
                    SecretForAdmin = "secret"
                },
                new PrivateIdtoWithWhiteListedName()
                {
                    EmailForLoggedIn = "2email",
                    GUID = "2guid",
                    Name = "2name",
                    SecretForAdmin = "2secret"
                }
            };
        }

        [Test]
        public void BaseDTOTest()
        {                        
            var plugin = new PerFieldAuthFeature();
            plugin.GlobalIgnoredPropertyNames.Add("GUID");            

            var asAdmin = GetDTOs();
            plugin.ProcessDto(asAdmin, new [] {"admin"}, true);            

            var asLoggedIn = GetDTOs();
            plugin.ProcessDto(asLoggedIn, Enumerable.Empty<string>(), true);

            var asAnyone = GetDTOs();
            plugin.ProcessDto(asAnyone, Enumerable.Empty<string>(), false);

            var all = asAdmin.Union(asLoggedIn.Union(asAnyone)).ToList();

            // GUID visible for everyone, always (global ignore)
            Assert.That(all.Any(x => x.GUID != null));

            // name always visible for everyone
            Assert.That(all.Any(x => x.Name != null));

            // email visible for logged in, but not for "anyone"
            Assert.That(asAnyone.All(x => x.EmailForLoggedIn == null));
            Assert.That(asLoggedIn.Union(asAdmin).All(x => x.EmailForLoggedIn != null));

            // secret for admin visible only for admin
            Assert.That(asAdmin.All( x => x.SecretForAdmin != null));
            Assert.That(asLoggedIn.Union(asAnyone).All(x => x.SecretForAdmin == null));            
        }

        [Test]
        public void CustomExtractorTest()
        {
            var plugin = new PerFieldAuthFeature();
            plugin.GlobalIgnoredPropertyNames.Add("GUID");

            var asAnyone = new ListResponse() {Dtos = GetDTOs()};
            plugin.ProcessDto(asAnyone, null, false);    
            // no extractor yet
            Assert.That(asAnyone.Dtos.All(x => x.EmailForLoggedIn != null));            

            plugin.RegisterCustomTypeExtractor<ListResponse>(lst => lst.Dtos);

            plugin.ProcessDto(asAnyone, null, false);    
            Assert.That(asAnyone.Dtos.All(x => x.EmailForLoggedIn == null));            
        }
    }
}
