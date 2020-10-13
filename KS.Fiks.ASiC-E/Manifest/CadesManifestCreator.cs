using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using KS.Fiks.ASiC_E.Model;
using KS.Fiks.ASiC_E.Sign;
using KS.Fiks.ASiC_E.Xsd;

namespace KS.Fiks.ASiC_E.Manifest
{
    public class CadesManifestCreator : IManifestCreator
    {
        private static readonly Encoding Encoding = new UTF8Encoding(false);

        private readonly bool addSignatureFile;

        private CadesManifestCreator(bool addSignatureFile)
        {
            this.addSignatureFile = addSignatureFile;
        }

        public static CadesManifestCreator CreateWithSignatureFile()
        {
            return new CadesManifestCreator(true);
        }

        public static CadesManifestCreator CreateWithoutSignatureFile()
        {
            return new CadesManifestCreator(false);
        }

        public ManifestContainer CreateManifest(IEnumerable<AsicePackageEntry> entries)
        {
            var manifest = new ASiCManifestType { DataObjectReference = entries.Select(ToDataObject).ToArray() };
            SignatureFileRef signatureFileRef = null;
            if (addSignatureFile)
            {
                signatureFileRef = CadesSignature.CreateSignatureRef();
                manifest.SigReference = new SigReferenceType
                {
                    MimeType = signatureFileRef.MimeType.ToString(), URI = signatureFileRef.FileName
                };
            }

            using (var outStream = new MemoryStream())
            using (var xmlWriter = CreateXmlWriter(outStream))
            {
                new XmlSerializer(typeof(ASiCManifestType)).Serialize(xmlWriter, manifest, CreateNamespaces());

                //Stupid whitespace manipulation to please DNB
                var resultString = Encoding.UTF8.GetString(outStream.ToArray());
                resultString = resultString.Replace(" />", "/>").Replace(" xmlns=\"http://uri.etsi.org/02918/v1.2.1#\"", "") + "\n";
                var x = Encoding.UTF8.GetBytes(resultString);


                return new ManifestContainer(AsiceConstants.CadesManifestFilename, x, signatureFileRef, ManifestSpec.Cades);
            }
        }

        private static XmlWriter CreateXmlWriter(Stream outStream)
        {
            var writer = XmlWriter.Create(outStream, CreateXmlWriterSettings());
            writer.WriteRaw($"<?xml version=\"1.0\" encoding=\"{Encoding.HeaderName.ToUpper(CultureInfo.CurrentCulture)}\" standalone=\"yes\"?>\n");
            return writer;
        }

        private static XmlWriterSettings CreateXmlWriterSettings()
        {
            return new XmlWriterSettings
            {
                Encoding = Encoding,
                OmitXmlDeclaration = true,
                ConformanceLevel = ConformanceLevel.Document,
                Indent = true,
                IndentChars = "    ",
                NewLineChars = "\n"
            };
        }

        private static XmlSerializerNamespaces CreateNamespaces()
        {
            var ns = new XmlSerializerNamespaces();
            ns.Add("", Namespaces.CadesAsicNamespace);
            ns.Add("ns2", Namespaces.XmlSignatureNamespace);
            return ns;
        }

        private static DataObjectReferenceType ToDataObject(AsicePackageEntry packageEntry)
        {
            if (packageEntry == null)
            {
                return null;
            }

            return new DataObjectReferenceType
            {
                MimeType = packageEntry.Type.ToString(),
                DigestMethod = new DigestMethodType
                {
                    Algorithm = packageEntry.MessageDigestAlgorithm.Uri.ToString()
                },
                DigestValue = packageEntry.Digest.GetDigest(),
                URI = packageEntry.FileName
            };
        }
    }
}