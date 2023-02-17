//#define KEEP_TEMP_FILES // <<IP>> uncomment it to don't remove generated files if you need them for some reason
using System;
using System.IO;

namespace Uniya.UnitTests
{
    internal class TestFiles
    {
        public static string GetFullPath(string path)
        {
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(basePath, @"TestFiles\" + path);
        }

        public static string GetTempFileName(string fileName)
        {
            // MS suggest to use GUIDs to get unique file name in a dir
            // if we need to look at results manually and KEEP_TEMP_FILES is defined, the temp file name won't be changed.
            // if KEEP_TEMP_FILES is not defined, the file name will look like "TFS200400_test-rep_3798db7e-ed02-49b4-bba9-a8b3f8bb0acb.xls"
#if !KEEP_TEMP_FILES
            fileName = $"{Path.GetFileNameWithoutExtension(fileName)}_{Guid.NewGuid()}{Path.GetExtension(fileName)}";
#endif
            return Path.Combine(Path.GetTempPath(), fileName);
        }
    }

    internal sealed class TempFile : IDisposable
    {
        readonly string _tempFileName;
        readonly string _testFileName;

        public TempFile() : this("temp.pdf")
        {
        }

        public TempFile(string fileName)
        {
            _tempFileName = TestFiles.GetTempFileName(fileName);
            _testFileName = TestFiles.GetFullPath(fileName);
        }

        public TempFile(string testFile, string tempFile)
        {
            _tempFileName = TestFiles.GetTempFileName(tempFile);
            _testFileName = TestFiles.GetFullPath(testFile);
        }

        public string TempFileName
        {
            get { return _tempFileName; }
        }

        public string TestFileName
        {
            get { return _testFileName; }
        }

        public void Dispose()
        {
#if (!KEEP_TEMP_FILES)
            {
                if (File.Exists(_tempFileName))
                    File.Delete(_tempFileName);
            }
#endif
        }
    }
}