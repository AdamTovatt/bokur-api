using InvoiceGenerator.Models.Google;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvoiceGenerator.Helpers
{
    internal class FontHelper
    {
        public string FamilyName { get; set; }

        private FontListResponse fontList;

        private FontHelper(FontListResponse fontListResponse, string familyName)
        {
            fontList = fontListResponse;
            FamilyName = familyName;
        }

        public static async Task<FontHelper> FromFontFamilyNameAsync(string fontFamilyName)
        {
            FontListResponse fontListResponse = await ApiHelper.Instance.GetFontList(fontFamilyName);
            return new FontHelper(fontListResponse, fontFamilyName);
        }

        public async Task<byte[]?> GetVariationAsync(string variationName)
        {
            if (fontList == null || fontList.Manifest == null || fontList.Manifest.FilesRefs == null)
                return null;

            FileRef? fileRef = fontList.Manifest.FilesRefs.Find(x => x.VariationName == variationName.ToLower());

            if(fileRef == null || fileRef.Url == null)
                return null;

            return await ApiHelper.Instance.GetSingleFontVariationAsync(fileRef.Url);
        }
    }
}
