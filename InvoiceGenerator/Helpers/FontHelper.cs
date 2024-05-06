using InvoiceGenerator.Models.Google;
using QuestPDF.Drawing;

namespace InvoiceGenerator.Helpers
{
    public class FontHelper
    {
        public string FamilyName { get; set; }

        private FontListResponse fontList;

        private FontHelper(FontListResponse fontListResponse, string familyName)
        {
            fontList = fontListResponse;
            FamilyName = familyName;
        }

        private static async Task<FontHelper> FromFontFamilyNameAsync(string fontFamilyName)
        {
            FontListResponse fontListResponse = await ApiHelper.Instance.GetFontList(fontFamilyName);
            return new FontHelper(fontListResponse, fontFamilyName);
        }

        private async Task<byte[]?> GetVariationAsync(string variationName)
        {
            if (fontList == null || fontList.Manifest == null || fontList.Manifest.FilesRefs == null)
                return null;

            FileRef? fileRef = fontList.Manifest.FilesRefs.Find(x => x.VariationName == variationName.ToLower());

            if(fileRef == null || fileRef.Url == null)
                return null;

            return await ApiHelper.Instance.GetSingleFontVariationAsync(fileRef.Url);
        }

        public static async Task SetupFont(string name, params string[] variations)
        {
            FontHelper font = await FontHelper.FromFontFamilyNameAsync(name);

            foreach (string variation in variations)
            {
                byte[]? fontBytes = await font.GetVariationAsync(variation);

                if (fontBytes == null)
                    throw new Exception($"Font not found: {variation}");

                using (MemoryStream stream = new MemoryStream(fontBytes))
                    FontManager.RegisterFontWithCustomName($"{name}-{variation}", stream);
            }
        }
    }
}
