using System;
using KS.Fiks.ASiC_E.Model;

namespace KS.Fiks.ASiC_E.Sign
{
    public static class CadesSignature
    {
        public static SignatureFileRef CreateSignatureRef()
        {
            var uuid = Guid.NewGuid().ToString();
            return new SignatureFileRef($"META-INF/signature-{uuid}.p7s");
        }
    }
}