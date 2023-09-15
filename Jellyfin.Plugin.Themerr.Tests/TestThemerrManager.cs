using Xunit.Abstractions;

namespace Jellyfin.Plugin.Themerr.Tests;

public class TestThemerrManager
{
    private readonly ITestOutputHelper _testOutputHelper;

    public TestThemerrManager(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public void TestSaveMp3()
    {
        // set destination with themerr_jellyfin_tests as the folder name
        var destinationFile = Path.Combine(
            // "themerr_jellyfin_tests",
            "theme.mp3"
            );
        
        // create a list of youtube urls
        var videoUrls = new List<string>
        {
            "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
            "https://www.youtube.com/watch?v=yPYZpwSpKmA",
            "https://www.youtube.com/watch?v=Ghmd4QzT9YY",
            "https://www.youtube.com/watch?v=LVEWkghDh9A"
        };
        foreach (var videoUrl in videoUrls)
        {
            // log
            _testOutputHelper.WriteLine($"Attempting to download {videoUrl}");
            
            // run and wait
            ThemerrManager.SaveMp3(destinationFile, videoUrl);
            
            // wait until the file is downloaded
            Thread.Sleep(5000); // 5 seconds
            
            // check if file exists
            Assert.True(File.Exists(destinationFile));
            
            // check if the file is an actual mp3
            // https://en.wikipedia.org/wiki/List_of_file_signatures
            var fileBytes = File.ReadAllBytes(destinationFile);
            var fileBytesHex = BitConverter.ToString(fileBytes);
            
            // make sure the file does is not WebM, starts with `1A 45 DF A3`
            var isNotWebM = !fileBytesHex.StartsWith("1A-45-DF-A3");
            Assert.True(isNotWebM);
            
            // valid mp3 signatures dictionary with offsets
            var validMp3Signatures = new Dictionary<string, int>
            {
                {"66-74-79-70-64-61-73-68", 4}, // Mp4 container?
                {"66-74-79-70-69-73-6F-6D", 4}, // Mp4 container
                {"49-44-33", 0}, // ID3
                {"FF-FB", 0}, // MPEG-1 Layer 3
                {"FF-F3", 0}, // MPEG-1 Layer 3
                {"FF-F2", 0} // MPEG-1 Layer 3
            };
            
            // log beginning of fileBytesHex
            _testOutputHelper.WriteLine($"Beginning of fileBytesHex: {fileBytesHex.Substring(0, 40)}");
            
            // check if the file is an actual mp3
            var isMp3 = false;
            
            // loop through validMp3Signatures
            foreach (var (signature, offset) in validMp3Signatures)
            {
                // log
                _testOutputHelper.WriteLine($"Checking for {signature} at offset of {offset} bytes");
                
                // remove the offset bytes
                var fileBytesHexWithoutOffset = fileBytesHex.Substring(offset * 3);
                
                // check if the beginning of the fileBytesHexWithoutOffset matches the signature
                var isSignature = fileBytesHexWithoutOffset.StartsWith(signature);
                if (isSignature)
                {
                    // log
                    _testOutputHelper.WriteLine($"Found {signature} at offset {offset}");
                
                    // set isMp3 to true
                    isMp3 = true;
                
                    // break out of loop
                    break;
                }

                // log
                _testOutputHelper.WriteLine($"Did not find {signature} at offset {offset}");
            }
            Assert.True(isMp3);
            
            // delete file
            File.Delete(destinationFile);
        }
    }
}
