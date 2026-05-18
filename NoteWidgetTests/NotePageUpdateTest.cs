using Microsoft.VisualStudio.TestTools.UnitTesting;
using NoteWidgetAddIn.Model;
using System.Linq;
using System.Xml.Linq;

namespace NoteWidgetAddIn
{
    [TestClass]
    public class NotePageUpdateTest
    {
        private static readonly XNamespace OneNs = "http://schemas.microsoft.com/office/onenote/2013/onenote";

        [TestMethod]
        public void UpdateContent_SingleLine_ReplacesPageContent()
        {
            var page = CreateTestPage("Old content");
            page.UpdateContent("New content");

            var texts = GetOeTexts(page);
            Assert.AreEqual(1, texts.Count);
            Assert.AreEqual("New content", texts[0]);
        }

        [TestMethod]
        public void UpdateContent_MultipleLines_CreatesMultipleOE()
        {
            var page = CreateTestPage("Old");
            page.UpdateContent("Line 1\nLine 2\nLine 3");

            var texts = GetOeTexts(page);
            Assert.AreEqual(3, texts.Count);
            Assert.AreEqual("Line 1", texts[0]);
            Assert.AreEqual("Line 2", texts[1]);
            Assert.AreEqual("Line 3", texts[2]);
        }

        [TestMethod]
        public void UpdateContent_WindowsLineEndings_CreatesMultipleOE()
        {
            var page = CreateTestPage("Old");
            page.UpdateContent("Line 1\r\nLine 2");

            var texts = GetOeTexts(page);
            Assert.AreEqual(2, texts.Count);
            Assert.AreEqual("Line 1", texts[0]);
            Assert.AreEqual("Line 2", texts[1]);
        }

        [TestMethod]
        public void UpdateContent_MarkdownSyntax_PreservedInText()
        {
            var page = CreateTestPage("Plain text");
            page.UpdateContent("# Heading\n**bold** and *italic*\n- list item");

            var texts = GetOeTexts(page);
            Assert.AreEqual("# Heading", texts[0]);
            Assert.AreEqual("**bold** and *italic*", texts[1]);
            Assert.AreEqual("- list item", texts[2]);
        }

        [TestMethod]
        public void UpdateContent_EmptyString_ClearsAllContent()
        {
            var page = CreateTestPage("Existing content");
            page.UpdateContent("");

            var texts = GetOeTexts(page);
            Assert.AreEqual(1, texts.Count);
            Assert.AreEqual("", texts[0]);
        }

        private NotePage CreateTestPage(string content)
        {
            var xml = $@"<?xml version=""1.0""?>
<one:Page xmlns:one=""http://schemas.microsoft.com/office/onenote/2013/onenote""
          ID=""{{test-page-id}}"" lastModifiedTime=""2026-01-01T00:00:00Z"">
  <one:Title>
    <one:OE>
      <one:T><![CDATA[Test Page]]></one:T>
    </one:OE>
  </one:Title>
  <one:Outline>
    <one:OEChildren>
      <one:OE>
        <one:T><![CDATA[{content}]]></one:T>
      </one:OE>
    </one:OEChildren>
  </one:Outline>
</one:Page>";
            return new NotePage(XElement.Parse(xml));
        }

        private System.Collections.Generic.List<string> GetOeTexts(NotePage page)
        {
            return page.Root
                .Descendants(OneNs + "Outline")
                .Elements(OneNs + "OEChildren")
                .Elements(OneNs + "OE")
                .Elements(OneNs + "T")
                .Select(t => t.Value)
                .ToList();
        }
    }
}
