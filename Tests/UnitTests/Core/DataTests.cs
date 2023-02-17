using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

using Uniya.Core;
using Uniya.Connectors.Sqlite;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uniya.UnitTests.UnitTests
{

    /// <summary>Complex main database tests.</summary>
    [TestClass]
    public class DataTests
    {
        ITransactedData data;

        internal void Init()
        {
            //Assert.AreEqual(width, Math.Round(sz.Width));
        }
        internal void Clear()
        {
            //Assert.AreEqual(width, Math.Round(sz.Width));
        }

        /// <summary>Initialization database.</summary>
        [TestInitialize]
        [Description("DB: Create database.")]
        public async Task Data_Create()
        {
            data = await XProvider.LocalDatabase("test", new SqliteLocal());
        }

        /// <summary></summary>
        [TestMethod]
        [Description("PDF: Measure Tests")]
        public void Data_Version()
        {
        }

        /// <summary></summary>
        [TestMethod]
        [Description("PDF: Measure Tests")]
        public void Data_MeasureTest()
        {
            MeasureTest("Arial", 10, "1234567890", 56);
        }

        internal void MeasureTest(string fontName, double fontSize, string text, double width)
        {
            //Assert.AreEqual(width, Math.Round(sz.Width));
        }

        [TestMethod]
        [Description("PDF: Draw Text Tests")]
        public void Pdf_DrawTextTest()
        {
            //var txt1 = "This is a text above the checkbox";
            //var txt2 = "This is a text below the checkbox";
        }

        [TestMethod]
        [Description("PDF: Draw Checkbox Tests")]
        public void Pdf_DrawCheckboxTest()
        {
        }
    }

    /*
    [TestFixture]
    public class C1PdfTest
    {
        [Test]
        [TestCase("Arial", 10, "1234567890", 55.6)]
        [TestCase("Tahoma", 10, "Digital: 0123456789", 88.87)]
        [TestCase("Times New Roman", 10, "Some text", 40.23)]
        [TestCase("Verdana", 10, "Digital: 0123456789", 103.64)]
        public void PdfMeasure(string fontName, double fontSize, string text, double width)
        {
            using (var file = new TempFile())
            {
                var pdf = new C1PdfDocument();
                var font = new System.Drawing.Font(fontName, (float)fontSize);
                var sz = pdf.MeasureString(text, font);
                Assert.AreEqual((float)width, sz.Width);
            }
        }

        [Test]
        [TestCase("Alignment.emf")]
        [TestCase("AreaClip.emf")]
        [TestCase("BadReport.emf")]
        [TestCase("BitCount.emf")]
        [TestCase("Clipping.emf")]
        [TestCase("Grade.wmf")]
        [TestCase("Graph.emf")]
        [TestCase("Hebrew.emf")]
        [TestCase("MetaData.emf")]
        [TestCase("MirrorImage.emf")]
        [TestCase("Pie.emf")]
        [TestCase("RoundClip.emf")]
        [TestCase("Tai.emf")]
        [TestCase("VanLook.emf")]
        [TestCase("Vertical.emf")]
        public void PdfMetafile(string metafileName)
        {
            using (var file = new TempFile(metafileName, "temp.pdf"))
            {
                var pdf = new C1PdfDocument();
                pdf.FontType = PdfFontType.Embedded;
                //pdf.FontType = PdfFontType.Standard;
                //pdf.Compression = CompressionLevel.NoCompression;

                // load metafile
                var meta = (Metafile)Metafile.FromFile(file.TestFileName);

                // get metafile size in points
                var szPage = GetImageSizeInPoints(meta);
                Console.WriteLine("Adding page {0:f2}\" x {1:f2}\"", szPage.Width / 72f, szPage.Height / 72f);

                // size page to metafile
                pdf.PageSize = szPage;

                // draw metafile on the page
                var rc = pdf.PageRectangle;
                //pdf.FillRectangle(Brushes.AntiqueWhite, rc);
                pdf.DrawImage(meta, rc);

                //pdf.DrawMetafile(file.TestFileName)
                pdf.Save(file.TempFileName);

                Assert.IsTrue(File.Exists(file.TempFileName));
            }
        }

        [Test]
        [TestCase("lists.htm")]
        [TestCase("paragraph.htm")]
        [TestCase("RexSwain.htm")]
        [TestCase("tables.htm")]
        [TestCase("wordwrap.htm")]
        public void PdfHtml(string metafileName)
        {
            using (var file = new TempFile(metafileName, "temp.pdf"))
            {
                var pdf = new C1PdfDocument();
                pdf.FontType = PdfFontType.Embedded;
                //pdf.FontType = PdfFontType.Standard;
                //pdf.Compression = CompressionLevel.NoCompression;

                // get Html to render
                var text = File.ReadAllText(file.TestFileName);

                // create one rectangle
                RectangleF rcPage = pdf.PageRectangle;
                rcPage.Inflate(-50, -50);
                RectangleF rc = rcPage;

                // print the HTML string spanning multiple pages
                Font font = new Font("Times New Roman", 12);
                Pen pen = new Pen(Color.LightCoral, 0.01f);
                for (int start = 0; ;)
                {
                    // render this part
                    start = pdf.DrawStringHtml(text, font, Brushes.Black, rc, start);
                    pdf.DrawRectangle(pen, rc);

                    // done?
                    if (start >= int.MaxValue)
                    {
                        break;
                    }

                    // next page
                    pdf.NewPage();
                }

                //pdf.DrawMetafile(file.TestFileName)
                pdf.Save(file.TempFileName);

                Assert.IsTrue(File.Exists(file.TempFileName));
            }
        }

        internal static SizeF GetImageSizeInPoints(Image img)
        {
            SizeF sz = SizeF.Empty;

            // PhysicalDimension returns hi-metric for Metafiles,
            // pixels for all other image types
            Metafile mf = img as Metafile;
            if (mf != null)
            {
                // always use 'logical' resolution of 96 dpi for display metafiles
                if (mf.GetMetafileHeader().IsDisplay())
                {
                    sz.Width = (float)Math.Round(img.Width * 72f / 96f, 2);
                    sz.Height = (float)Math.Round(img.Height * 72f / 96f, 2);
                    return sz;
                }

                // other metafiles have PhysicalDimension stored in HiMetric
                sz = mf.PhysicalDimension;
                sz.Width = (float)Math.Round(sz.Width * 72f / 2540f, 2);
                sz.Height = (float)Math.Round(sz.Height * 72f / 2540f, 2);
                return sz;
            }

            // other images have the resolution stored in them
            sz.Height = (float)Math.Round(sz.Height * 72f / img.VerticalResolution, 2);
            sz.Width = (float)Math.Round(sz.Width * 72f / img.HorizontalResolution, 2);
            return sz;
        }
        public static void SaveMetafile(string fileName, Metafile mf)
        {
            using (FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
            {
                SaveMetafile(fs, mf);
            }
        }
        public static void SaveMetafile(Stream stream, Metafile mf)
        {
            // metafile picture data
            using (Graphics gRef = Graphics.FromHwnd(IntPtr.Zero))
            {
                // create emf only metafile
                IntPtr hdcRef = gRef.GetHdc();
                RectangleF rc = new RectangleF(0, 0, mf.Width, mf.Height);
#if EMF_PLUS
                var metaType = mf.GetMetafileHeader().Type;
                var emfType = (metaType == MetafileType.Emf) ? EmfType.EmfOnly : EmfType.EmfPlusOnly;
                Metafile metaTemp = new Metafile(stream, hdcRef, rc, MetafileFrameUnit.Pixel, emfType);
#else
                Metafile metaTemp = new Metafile(stream, hdcRef, rc, MetafileFrameUnit.Pixel, EmfType.EmfOnly);
#endif
                // draw metafile
                using (Graphics g = Graphics.FromImage(metaTemp))
                {
                    g.DrawImage(mf, 0.0f, 0.0f);
                }
                gRef.ReleaseHdc(hdcRef);
                stream.Flush();
            }
        }
    }
    */
}
