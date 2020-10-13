using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Castle.DynamicProxy.Generators.Emitters.SimpleAST;
using FluentAssertions;
using KS.Fiks.ASiC_E.Manifest;
using KS.Fiks.ASiC_E.Model;
using KS.Fiks.ASiC_E.Xsd;
using Xunit;

namespace KS.Fiks.ASiC_E.Test.Manifest
{
    public class CadesManifestCreatorTest
    {
        [Fact(DisplayName = "Create CAdES manifest without signature")]
        public void CreateCadesManifest()
        {
            var cadesManifestCreator = CadesManifestCreator.CreateWithoutSignatureFile();
            var digestAlgorithm = MessageDigestAlgorithm.SHA256Desig;
            var fileEntry = new AsicePackageEntry("my.pdf", MimeType.ForString("application/pdf"), digestAlgorithm);
            fileEntry.Digest = new DigestContainer(new byte[] { 0, 0, 1 }, digestAlgorithm);
            var entries = new[] { fileEntry };
            var manifest = cadesManifestCreator.CreateManifest(entries);
            manifest.Should().NotBeNull()
                .And
                .BeOfType<ManifestContainer>();
            manifest.Data.Should().NotBeNull();
            manifest.FileName.Should().Be(AsiceConstants.CadesManifestFilename);

            var xmlManifest = DeserializeManifest(manifest.Data.ToArray());
            xmlManifest.Should().NotBeNull();
            xmlManifest.SigReference.Should().BeNull();
            xmlManifest.DataObjectReference.Should().HaveCount(1);
            var dataObjectRef = xmlManifest.DataObjectReference[0];
            dataObjectRef.Should().NotBeNull();
            dataObjectRef.MimeType.Should().Be(fileEntry.Type.ToString());
            dataObjectRef.DigestValue.Should().Equal(fileEntry.Digest.GetDigest());
            dataObjectRef.URI.Should().Be(fileEntry.FileName);
        }

        [Fact(DisplayName = "Create CAdES manifest with signature")]
        public void CreateCadesManifestIncludingSignature()
        {
            var cadesManifestCreator = CadesManifestCreator.CreateWithSignatureFile();
            var fileEntry = new AsicePackageEntry("P.00987654321.001.P001.65013_File1.xml", MimeType.ForString("application/xml"), MessageDigestAlgorithm.SHA256Enc);
            fileEntry.Digest = new DigestContainer(new byte[] { 0, 0, 1 }, MessageDigestAlgorithm.SHA256Enc);
            var fileEntry2 = new AsicePackageEntry("ApprovalData/ApprovalData1.xml", MimeType.ForString("application/xml"), MessageDigestAlgorithm.SHA256Desig);
            fileEntry2.Digest = new DigestContainer(new byte[] { 0, 0, 1 }, MessageDigestAlgorithm.SHA256Desig);
            var manifest = cadesManifestCreator.CreateManifest(new[] { fileEntry, fileEntry2 });
            manifest.Should().NotBeNull()
                .And
                .BeOfType<ManifestContainer>();
            manifest.FileName.Should().Be(AsiceConstants.CadesManifestFilename);

            File.WriteAllBytes(@"c:\temp\manifest.xml", manifest.Data.ToArray());
            var xmlManifest = DeserializeManifest(manifest.Data.ToArray());
            xmlManifest.Should().NotBeNull();
            xmlManifest.SigReference.Should().NotBeNull();
            xmlManifest.SigReference.MimeType.Should().Be(AsiceConstants.ContentTypeSignature);
            xmlManifest.DataObjectReference.Should().HaveCount(2);
        }

        private static ASiCManifestType DeserializeManifest(byte[] data)
        {
            using (var xmlStream = new MemoryStream(data))
            {
                var xmlSerializer = new XmlSerializer(typeof(ASiCManifestType));
                var xmlObj = xmlSerializer.Deserialize(xmlStream);
                return (ASiCManifestType)xmlObj;
            }
        }
    }
}